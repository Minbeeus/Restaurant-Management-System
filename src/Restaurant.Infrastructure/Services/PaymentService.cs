using System.Security.Cryptography;
using System.Text;
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

    public PaymentService(
        ApplicationDbContext context,
        IQrCodeService qrCodeService,
        IRestaurantNotificationService notificationService,
        ILoyaltyService loyaltyService,
        IMediator mediator,
        IConfiguration configuration,
        IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _notificationService = notificationService;
        _loyaltyService = loyaltyService;
        _mediator = mediator;
        _configuration = configuration;
        _localizer = localizer;
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
        var qrText = $"STK:{request.BankAccountNo}|NH:{request.BankName}|ND:{sepayContent}|ST:{order.TotalAmount:F0}";

        var qrBytes = _qrCodeService.GenerateQrCodeImage(qrText);
        var qrBase64 = Convert.ToBase64String(qrBytes);
        var qrImageUrl = $"data:image/png;base64,{qrBase64}";

        return new VietQrResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = order.TotalAmount,
            SepayContent = sepayContent,
            QrImageUrl = qrImageUrl
        };
    }

    public async Task<bool> ProcessSepayWebhookAsync(SepayWebhookPayload payload, string? signatureHeader)
    {
        // 1. Signature Security Check
        var apiKey = _configuration["SePay:ApiKey"] ?? "SePaySecretApiKey123!";
        if (!VerifySePaySignature(payload, signatureHeader, apiKey))
        {
            return false;
        }

        // 2. Idempotency Check via SepayWebhookLogs
        var existingLog = await _context.SepayWebhookLogs
            .FirstOrDefaultAsync(l => l.TransactionId == payload.id.ToString());

        if (existingLog != null && existingLog.IsProcessed)
        {
            // Already processed -> Return true immediately (Idempotent 200 OK)
            return true;
        }

        if (existingLog == null)
        {
            existingLog = new SepayWebhookLog
            {
                TransactionId = payload.id.ToString(),
                Amount = payload.transferAmount,
                Content = payload.content,
                BankCode = payload.gateway,
                AccountNumber = payload.accountNumber,
                RawPayload = payload.description ?? string.Empty,
                IsProcessed = false,
                ReceivedAt = DateTime.UtcNow
            };

            _context.SepayWebhookLogs.Add(existingLog);
            await _context.SaveChangesAsync();
        }

        // Parse OrderId from content (e.g., POS123 -> OrderId = 123)
        int? orderId = ParseOrderIdFromContent(payload.content);
        if (!orderId.HasValue)
        {
            existingLog.ErrorLog = "Cannot parse OrderId from SePay content: " + payload.content;
            await _context.SaveChangesAsync();
            return true;
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId.Value);

            if (order == null)
            {
                existingLog.ErrorLog = $"Order ID {orderId} not found.";
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }

            // Conflict Check: If Order is ALREADY Paid by Cash or completed earlier
            if (order.Status == (int)OrderStatus.Paid)
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
                existingLog.ErrorLog = $"Amount Mismatch: Received {payload.transferAmount:N0}đ but Order requires {order.TotalAmount:N0}đ.";
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

            var payment = new Payment
            {
                OrderId = order.Id,
                ShiftId = activeShift?.Id ?? 1,
                Amount = payload.transferAmount,
                PaymentMethod = (int)PaymentMethod.BankTransfer,
                Status = (int)PaymentStatus.Completed,
                TransactionRef = payload.referenceCode,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            order.Status = (int)OrderStatus.Paid;
            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Available;
            }

            existingLog.IsProcessed = true;
            await _context.SaveChangesAsync();

            // Accumulate Loyalty Points if Customer exists
            if (order.CustomerId.HasValue)
            {
                await _loyaltyService.AccumulatePointsAsync(order.CustomerId.Value, order.Id, payload.transferAmount);
            }

            // OrderPaidDomainEvent removed (BOM Inventory Deduction is now handled by Kitchen Item Done event)

            await dbTransaction.CommitAsync();

            // Realtime Broadcasts
            await _notificationService.NotifyPaymentCompletedAsync(order.Id, order.OrderCode, payload.transferAmount, "VietQR");
            if (order.TableId.HasValue)
            {
                await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, Enum.GetName(typeof(TableStatus), (int)TableStatus.Available) ?? "Unknown");
            }

            return true;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            existingLog.ErrorLog = "Exception during Webhook processing: " + ex.Message;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    private static bool VerifySePaySignature(SepayWebhookPayload payload, string? signatureHeader, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader)) return true;

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
            var rawData = $"{payload.id}{payload.transferAmount}{payload.referenceCode}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var expectedSignature = Convert.ToHexString(hash).ToLower();

            return signatureHeader.Trim().ToLower().Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static int? ParseOrderIdFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var upper = content.ToUpper();
        int idx = upper.IndexOf("POS");
        if (idx >= 0)
        {
            var numberStr = new string(upper.Substring(idx + 3).TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numberStr, out var id)) return id;
        }

        var digitsOnly = new string(content.Where(char.IsDigit).ToArray());
        if (int.TryParse(digitsOnly, out var fallbackId)) return fallbackId;

        return null;
    }
}
