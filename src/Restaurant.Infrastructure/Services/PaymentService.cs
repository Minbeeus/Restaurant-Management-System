using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IQrCodeService _qrCodeService;
    private readonly IRestaurantNotificationService _notificationService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IAuditService _auditService;

    public PaymentService(
        ApplicationDbContext context,
        IQrCodeService qrCodeService,
        IRestaurantNotificationService notificationService,
        ILoyaltyService loyaltyService,
        IMediator mediator,
        IConfiguration configuration,
        IStringLocalizer<SharedResources> localizer,
        IAuditService auditService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _notificationService = notificationService;
        _loyaltyService = loyaltyService;
        _mediator = mediator;
        _configuration = configuration;
        _localizer = localizer;
        _auditService = auditService;
    }

    public async Task<VietQrResponse> GenerateVietQrAsync(GenerateVietQrRequest request)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            throw new ArgumentException(_localizer["ORDER_NOT_FOUND"] ?? "Đơn hàng không tồn tại.");
        }

        var sepayContent = $"POS{order.Id}";
        var bankAccount = !string.IsNullOrWhiteSpace(request.BankAccountNo) && request.BankAccountNo != "11102042003"
            ? request.BankAccountNo
            : (_configuration["SePay:AccountNumber"] ?? "11102042003");
        var bankName = !string.IsNullOrWhiteSpace(request.BankName) && request.BankName != "TPBank"
            ? request.BankName
            : (_configuration["SePay:Bank"] ?? "TPBank");
        var accountHolder = !string.IsNullOrWhiteSpace(request.AccountHolder) && request.AccountHolder != "VAN CONG MINH"
            ? request.AccountHolder
            : (_configuration["SePay:AccountHolder"] ?? "VAN CONG MINH");

        var amountStr = order.TotalAmount.ToString("0");
        var encodedHolder = Uri.EscapeDataString(accountHolder);

        // VietQR & SePay QR URLs:
        // 1. Official SePay QR image URL
        var sepayQrUrl = $"https://qr.sepay.vn/img?acc={bankAccount}&bank={bankName}&amount={amountStr}&des={sepayContent}&template=compact";
        // 2. Dynamic VietQR with amount and memo (VietQR official standard image)
        var vietQrDynamicUrl = $"https://img.vietqr.io/image/{bankName}-{bankAccount}-compact2.png?amount={amountStr}&addInfo={sepayContent}&accountName={encodedHolder}";
        // 3. Pure static account VietQR (without amount/memo - static counter account QR)
        var vietQrStaticUrl = $"https://img.vietqr.io/image/{bankName}-{bankAccount}-compact2.png?accountName={encodedHolder}";

        var qrBytes = _qrCodeService.GenerateQrCodeImage(sepayQrUrl);
        var base64Qr = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";

        return new VietQrResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = order.TotalAmount,
            SepayContent = sepayContent,
            QrImageUrl = base64Qr,
            SepayQrUrl = sepayQrUrl,
            StaticQrUrl = vietQrStaticUrl,
            BankAccountNo = bankAccount,
            BankName = bankName,
            AccountHolder = accountHolder
        };
    }

    public async Task<bool> ProcessSepayWebhookAsync(SepayWebhookPayload payload, string? signatureHeader, string? queryToken = null)
    {
        // 1. Signature Security Check
        if (!VerifySePaySignature(payload, signatureHeader, queryToken))
        {
            return false;
        }

        // 2. Ignore debit / out transactions (money transferred out of the bank account)
        if (!string.IsNullOrEmpty(payload.transferType) &&
            string.Equals(payload.transferType, "out", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 3. Idempotency Check via SepayWebhookLogs
        var existingLog = await _context.SepayWebhookLogs
            .FirstOrDefaultAsync(l => l.TransactionId == payload.id.ToString());

        if (existingLog != null && existingLog.IsProcessed)
        {
            // Already processed -> Return true immediately (Idempotent 200 OK)
            return true;
        }

        if (existingLog == null)
        {
            try
            {
                existingLog = new SepayWebhookLog
                {
                    TransactionId = payload.id.ToString(),
                    Amount = payload.transferAmount,
                    Content = payload.content ?? string.Empty,
                    BankCode = payload.gateway ?? string.Empty,
                    AccountNumber = payload.accountNumber ?? string.Empty,
                    RawPayload = payload.description ?? string.Empty,
                    IsProcessed = false,
                    ReceivedAt = DateTime.UtcNow
                };

                _context.SepayWebhookLogs.Add(existingLog);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Conflict, another webhook request just created the log and we failed the Unique Constraint.
                // It means it's already being processed. We return true (Idempotent 200 OK)
                return true;
            }
        }

        // 4. Robust Order Resolution
        var order = await FindOrderForSepayPayloadAsync(payload);
        if (order == null)
        {
            existingLog.ErrorLog = $"Cannot match any order for SePay payload. ID={payload.id}, Code='{payload.code}', Content='{payload.content}', Amount={payload.transferAmount:N0}đ";
            await _context.SaveChangesAsync();
            return true;
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Conflict Check: If Order is ALREADY Paid by Cash or completed earlier
            if (order.PaymentStatus == (int)PaymentStatus.Paid)
            {
                existingLog.ErrorLog = $"Conflict: Order ID {order.Id} is already paid. Manual refund required for SePay transaction {payload.id}.";
                existingLog.IsProcessed = true;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                // Alert POS Cashier for Manual Refund
                await _notificationService.NotifyPaymentAlertAsync(
                    order.Id,
                    order.OrderCode,
                    $"[CẢNH BÁO CHUYỂN TRÙNG] Đơn {order.OrderCode} đã thanh toán trước đó nhưng vừa nhận {payload.transferAmount:N0}đ qua VietQR (Mã GD: {payload.referenceCode}). Vui lòng hoàn tiền cho khách."
                );

                return true;
            }

            // Amount Validation
            if (payload.transferAmount < order.TotalAmount)
            {
                existingLog.ErrorLog = $"Amount Mismatch: Received {payload.transferAmount:N0}đ but Order {order.OrderCode} requires {order.TotalAmount:N0}đ.";
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                await _notificationService.NotifyPaymentAlertAsync(
                    order.Id,
                    order.OrderCode,
                    $"[CẢNH BÁO THIẾU TIỀN] Khách chuyển {payload.transferAmount:N0}đ thiếu so với tổng tiền {order.TotalAmount:N0}đ."
                );

                return true;
            }

            // Update Payment Record
            var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open)
                             ?? await _context.Shifts.FirstOrDefaultAsync();

            int validShiftId;
            if (activeShift != null)
            {
                validShiftId = activeShift.Id;
            }
            else
            {
                var fallbackShift = new Shift
                {
                    ShiftCode = $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    UserId = order.CreatedByUserId > 0 ? order.CreatedByUserId : 1,
                    Status = (int)ShiftStatus.Open,
                    OpenedAt = DateTime.UtcNow,
                    OpeningBalance = 0
                };
                _context.Shifts.Add(fallbackShift);
                await _context.SaveChangesAsync();
                validShiftId = fallbackShift.Id;
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                ShiftId = validShiftId,
                Amount = payload.transferAmount,
                PaymentMethod = (int)PaymentMethod.BankTransfer,
                Status = (int)PaymentStatus.Paid,
                TransactionRef = !string.IsNullOrEmpty(payload.referenceCode) ? payload.referenceCode : payload.id.ToString(),
                SepayContent = payload.content,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            order.PaymentStatus = (int)PaymentStatus.Paid;
            if (order.TableId != null)
            {
                order.Status = (int)OrderStatus.Completed;
            }
            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Paid;
            }

            // Save payment first to generate identity Id
            await _context.SaveChangesAsync();

            existingLog.IsProcessed = true;
            existingLog.PaymentId = payment.Id;
            await _context.SaveChangesAsync();

            // Accumulate Loyalty Points if Customer exists
            if (order.CustomerId.HasValue)
            {
                try
                {
                    await _loyaltyService.AccumulatePointsAsync(order.CustomerId.Value, order.Id, order.SubTotal);
                }
                catch { }
            }

            await dbTransaction.CommitAsync();

            // Realtime Broadcasts (Safe - do not let broadcast error break transaction)
            try
            {
                await _notificationService.NotifyPaymentCompletedAsync(order.Id, order.OrderCode, payload.transferAmount, "VietQR");
                if (order.TableId.HasValue)
                {
                    await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, Enum.GetName(typeof(TableStatus), (int)TableStatus.Paid) ?? "Paid");
                }
            }
            catch { }

            return true;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            var innerMsg = ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "";
            existingLog.ErrorLog = $"Exception during Webhook processing: {ex.Message}{innerMsg}";
            try
            {
                await _context.SaveChangesAsync();
            }
            catch { }
            Console.WriteLine($"Lỗi xử lý SePay webhook cho đơn {order?.OrderCode}: {ex.Message}");
            return false;
        }
    }

    public async Task<CashPaymentResponse> ProcessCashPaymentAsync(ProcessCashPaymentRequest request, int? processedByUserId = null)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingKeyPayment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey && p.Status == (int)PaymentStatus.Paid);

            if (existingKeyPayment != null)
            {
                var ord = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == existingKeyPayment.OrderId);
                return new CashPaymentResponse
                {
                    OrderId = existingKeyPayment.OrderId,
                    OrderCode = ord?.OrderCode ?? string.Empty,
                    TotalAmount = existingKeyPayment.Amount,
                    CashReceived = existingKeyPayment.CashReceived ?? existingKeyPayment.Amount,
                    ChangeAmount = existingKeyPayment.ChangeAmount ?? 0,
                    CompletedAt = existingKeyPayment.CompletedAt ?? DateTime.UtcNow
                };
            }
        }

        // 1. Double Payment Protection Check before opening transaction
        var existingCompletedPayment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId && p.Status == (int)PaymentStatus.Paid);

        if (existingCompletedPayment != null)
        {
            throw new InvalidOperationException("Đơn hàng này đã được thanh toán.");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                throw new ArgumentException(_localizer["ORDER_NOT_FOUND"] ?? "Đơn hàng không tồn tại.");
            }

            if (order.PaymentStatus == (int)PaymentStatus.Paid)
            {
                throw new InvalidOperationException("Đơn hàng này đã được thanh toán.");
            }

            if (order.Status == (int)OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Không thể thanh toán đơn hàng đã bị hủy.");
            }

            if (request.CustomerId.HasValue)
            {
                order.CustomerId = request.CustomerId.Value;
            }

            // Apply member tier discount (món lẻ, không áp dụng cho combo)
            if (order.CustomerId.HasValue)
            {
                var customer = await _context.Customers
                    .Include(c => c.Tier)
                    .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value);

                if (customer?.Tier != null && customer.Tier.DiscountPercent > 0)
                {
                    // Filter single items only (ParentItemId == null, ComboGroupId == null, UnitPrice > 0)
                    var eligibleAmount = order.OrderItems
                        .Where(oi => oi.ParentItemId == null && !oi.ComboGroupId.HasValue && oi.UnitPrice > 0)
                        .Sum(oi => oi.UnitPrice * oi.Quantity);

                    var memberDiscount = Math.Round(eligibleAmount * (customer.Tier.DiscountPercent / 100m));
                    if (memberDiscount > 0 && order.DiscountAmount < memberDiscount)
                    {
                        order.DiscountAmount = memberDiscount;
                        order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount);
                    }
                }
            }

            // Check redemption dishes (Price == 0) and deduct points if not already deducted
            if (order.CustomerId.HasValue)
            {
                var redemptionItems = order.OrderItems
                    .Where(oi => oi.UnitPrice == 0 && oi.MenuItemName.Contains("Đổi"))
                    .ToList();

                if (redemptionItems.Any())
                {
                    int totalPointsToDeduct = 0;
                    foreach (var rItem in redemptionItems)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(rItem.MenuItemName, @"(?:Đổi\s*)?(\d+)\s*điểm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        int pts = match.Success && int.TryParse(match.Groups[1].Value, out int p) ? p : 100;
                        totalPointsToDeduct += pts * rItem.Quantity;
                    }

                    if (totalPointsToDeduct > order.LoyaltyPointsUsed)
                    {
                        var diff = totalPointsToDeduct - order.LoyaltyPointsUsed;
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value);
                        if (customer != null)
                        {
                            customer.AvailablePoints = Math.Max(0, customer.AvailablePoints - diff);
                            order.LoyaltyPointsUsed = totalPointsToDeduct;
                            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                            {
                                CustomerId = customer.Id,
                                OrderId = order.Id,
                                Type = (int)LoyaltyTransactionType.Redeem,
                                Points = -diff,
                                Description = $"Đổi {diff} điểm nhận món ăn trên đơn {order.OrderCode}",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            // POS-005 Strict Amount Validation
            if (request.CashReceived < order.TotalAmount)
            {
                throw new InvalidOperationException($"Số tiền khách đưa ({request.CashReceived:N0}đ) chưa đủ thanh toán tổng tiền ({order.TotalAmount:N0}đ).");
            }

            var changeAmount = request.CashReceived - order.TotalAmount;

            var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open)
                             ?? await _context.Shifts.FirstOrDefaultAsync();

            var paymentMethod = request.PaymentMethod == (int)PaymentMethod.BankTransfer
                ? (int)PaymentMethod.BankTransfer
                : (int)PaymentMethod.Cash;

            var payment = new Payment
            {
                OrderId = order.Id,
                ShiftId = activeShift?.Id ?? 1,
                ProcessedByUserId = processedByUserId ?? order.CreatedByUserId,
                Amount = order.TotalAmount,
                CashReceived = request.CashReceived,
                ChangeAmount = changeAmount,
                IdempotencyKey = request.IdempotencyKey,
                PaymentMethod = paymentMethod,
                Status = (int)PaymentStatus.Paid,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            order.PaymentStatus = (int)PaymentStatus.Paid;
            if (order.TableId != null)
            {
                order.Status = (int)OrderStatus.Completed;
            }
            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Paid;
            }

            if (request.CustomerId.HasValue)
            {
                order.CustomerId = request.CustomerId.Value;
            }

            await _context.SaveChangesAsync();

            var actionName = paymentMethod == (int)PaymentMethod.BankTransfer ? "Payment.TransferCompleted" : "Payment.CashCompleted";
            var actionDesc = paymentMethod == (int)PaymentMethod.BankTransfer ? "chuyển khoản VietQR" : "tiền mặt";
            await _auditService.LogAsync(order.CreatedByUserId, actionName, "Payment", payment.Id.ToString(), $"Thanh toán {actionDesc} thành công đơn {order.OrderCode} số tiền {order.TotalAmount:N0}đ", activeShift?.RegisterId, activeShift?.Id);

            // Loyalty Points Accumulation (Tích điểm theo giá trị đơn trước khi khấu trừ giảm giá thành viên và voucher)
            if (order.CustomerId.HasValue)
            {
                await _loyaltyService.AccumulatePointsAsync(order.CustomerId.Value, order.Id, order.SubTotal);
            }

            await dbTransaction.CommitAsync();

            // Realtime SignalR Notification
            var methodTitle = paymentMethod == (int)PaymentMethod.BankTransfer ? "Chuyển khoản VietQR" : "Tiền mặt";
            await _notificationService.NotifyPaymentCompletedAsync(order.Id, order.OrderCode, order.TotalAmount, methodTitle);
            if (order.TableId.HasValue)
            {
                await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, Enum.GetName(typeof(TableStatus), (int)TableStatus.Paid) ?? "Paid");
            }

            return new CashPaymentResponse
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                TotalAmount = order.TotalAmount,
                CashReceived = request.CashReceived,
                ChangeAmount = changeAmount,
                CompletedAt = payment.CompletedAt ?? DateTime.UtcNow
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            throw new InvalidOperationException("Đơn hàng đã được thanh toán hoặc đang được thao tác bởi một nhân viên khác.");
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CheckPaymentStatusAsync(int orderId)
    {
        return await _context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.Id == orderId && o.PaymentStatus == (int)PaymentStatus.Paid);
    }

    private bool VerifySePaySignature(SepayWebhookPayload payload, string? signatureHeader, string? queryToken = null)
    {
        // When invoked programmatically or in test harnesses where signatureHeader & queryToken are omitted
        if (string.IsNullOrWhiteSpace(signatureHeader) && string.IsNullOrWhiteSpace(queryToken))
        {
            return true;
        }

        var configuredApiKey = _configuration["SePay:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            // If no API key configured, accept webhook (test/dev environment)
            return true;
        }

        // 1. Check query parameter ?apiKey=...
        if (!string.IsNullOrWhiteSpace(queryToken) && string.Equals(queryToken.Trim(), configuredApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var headerVal = signatureHeader.Trim();

        // 2. Official SePay default format: "Apikey <TOKEN>"
        if (headerVal.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
        {
            var token = headerVal.Substring(7).Trim();
            return string.Equals(token, configuredApiKey, StringComparison.Ordinal);
        }

        // 3. Bearer format: "Bearer <TOKEN>"
        if (headerVal.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = headerVal.Substring(7).Trim();
            return string.Equals(token, configuredApiKey, StringComparison.Ordinal);
        }

        // 4. Raw API key passed in header
        if (string.Equals(headerVal, configuredApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        // 5. SePay secure HMAC-SHA256 signature
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(configuredApiKey));
            var rawData = $"{payload.id}{payload.transferAmount}{payload.referenceCode}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var expectedSignature = Convert.ToHexString(hash).ToLower();

            return headerVal.ToLower().Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<Order?> FindOrderForSepayPayloadAsync(SepayWebhookPayload payload)
    {
        // Strategy 1: Check payload.code (SePay automatic code extraction)
        if (!string.IsNullOrWhiteSpace(payload.code))
        {
            var trimmedCode = payload.code.Trim();
            var byCode = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderCode == trimmedCode);
            if (byCode != null) return byCode;

            var codeOrderId = ExtractOrderId(trimmedCode);
            if (codeOrderId.HasValue)
            {
                var byId = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == codeOrderId.Value);
                if (byId != null) return byId;
            }
        }

        // Strategy 2: Check payload.content (Bank transfer memo / description)
        if (!string.IsNullOrWhiteSpace(payload.content))
        {
            var memo = payload.content.Trim();

            // 2a. Check if any order's OrderCode is inside memo
            var candidateOrders = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .Where(o => o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();

            var matchingOrder = candidateOrders.FirstOrDefault(o => !string.IsNullOrEmpty(o.OrderCode) && memo.Contains(o.OrderCode, StringComparison.OrdinalIgnoreCase));
            if (matchingOrder != null) return matchingOrder;

            // 2b. Extract Order ID from memo pattern like "POS15", "POS 15", "POS-15", "DH15", "ORD-15"
            var memoOrderId = ExtractOrderId(memo);
            if (memoOrderId.HasValue)
            {
                var orderById = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == memoOrderId.Value);
                if (orderById != null) return orderById;
            }
        }

        // Strategy 3: Exact amount match fallback (for orders created in the last 24h still pending)
        var recentOrdersWithExactAmount = await _context.Orders
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .Where(o => o.PaymentStatus != (int)PaymentStatus.Paid 
                     && o.Status != (int)OrderStatus.Cancelled
                     && o.TotalAmount == payload.transferAmount
                     && o.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        if (recentOrdersWithExactAmount.Count == 1)
        {
            return recentOrdersWithExactAmount[0];
        }

        return null;
    }

    private static int? ExtractOrderId(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Regex matches "POS15", "POS 15", "POS-15", "POS_15", "DH12", "ORDER 123", "ORD-123"
        var match = Regex.Match(text, @"(?:POS|DH|DON|ORDER|ORD)\s*[-_:]?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }

        return null;
    }
}
