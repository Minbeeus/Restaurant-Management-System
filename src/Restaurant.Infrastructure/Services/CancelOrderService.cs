using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Domain.Common;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class CancelOrderService : ICancelOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IRestaurantNotificationService _notificationService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public CancelOrderService(
        ApplicationDbContext context,
        ILoyaltyService loyaltyService,
        IRestaurantNotificationService notificationService,
        IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _notificationService = notificationService;
        _localizer = localizer;
    }

    public async Task<bool> CancelOrderAsync(int userId, int orderId, string reason)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == (int)OrderStatus.Cancelled) return false;

            // Check if user has manager authorization to cancel an order
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            var isManagerOrAdmin = user != null && (user.Role.Name == "Manager" || user.Role.Name == "Admin");

            if (!isManagerOrAdmin)
            {
                // Create ApprovalRequest for Manager approval in Approval Center
                var isPaidOrder = order.PaymentStatus == (int)PaymentStatus.Paid || order.Status == (int)OrderStatus.Completed;
                var appReq = new ApprovalRequest
                {
                    Type = isPaidOrder ? (int)ApprovalType.Refund : (int)ApprovalType.CancelOrder,
                    ReferenceId = order.Id,
                    Amount = order.TotalAmount,
                    Reason = reason,
                    Note = $"Yêu cầu {(isPaidOrder ? "hoàn tiền" : "hủy")} đơn hàng {order.OrderCode}",
                    RequestedByUserId = userId,
                    RequestedAt = DateTime.UtcNow,
                    Status = (int)ApprovalStatus.PendingApproval
                };
                _context.ApprovalRequests.Add(appReq);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return false; // Approval request created, awaiting manager approval in Approval Center
            }

            var wasPaid = order.PaymentStatus == (int)PaymentStatus.Paid || order.Status == (int)OrderStatus.Completed;
            
            order.Status = (int)OrderStatus.Cancelled;
            
            var cancelText = string.Format(_localizer["CANCEL_ORDER_NOTE"] ?? "[CANCELLED]: {0}", reason);
            order.Note = string.IsNullOrEmpty(order.Note) ? cancelText : $"{order.Note} | {cancelText}";

            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Available;
            }

            // 1. Record Cancellation Waste in InventoryTransactions (preserve original SaleDeduction for audit immutability)
            var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
            var saleDeductions = await _context.InventoryTransactions
                .Where(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId.HasValue && orderItemIds.Contains(t.ReferenceId.Value) && t.TransactionType == (int)InventoryTransactionType.SaleDeduction)
                .ToListAsync();

            foreach (var deduction in saleDeductions)
            {
                // Create a distinct CancellationWaste entry preserving the immutable ledger
                var wasteText = string.Format(_localizer["CANCELLATION_WASTE_NOTE"] ?? "[WASTE] Cancelled Order {0} | Origin: {1}", order.OrderCode, deduction.Note);
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = deduction.MaterialId,
                    TransactionType = (int)InventoryTransactionType.CancellationWaste,
                    Quantity = 0, // CurrentStock was already deducted when cooked; cancellation does not return waste to physical stock
                    BalanceAfter = deduction.BalanceAfter,
                    ReferenceType = SystemConstants.ReferenceTypes.Order,
                    ReferenceId = order.Id,
                    Note = wasteText,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. Rollback Loyalty Points & Record Refund Transaction if order was paid
            if (wasPaid)
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == order.Id && p.Status == (int)PaymentStatus.Paid);

                var refund = new Refund
                {
                    OrderId = order.Id,
                    PaymentId = payment?.Id,
                    Amount = order.TotalAmount,
                    Reason = reason,
                    RefundedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Refunds.Add(refund);
                order.PaymentStatus = (int)PaymentStatus.Refunded;
                await _loyaltyService.RollbackLoyaltyForCancelledOrderAsync(order.Id);
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Broadcast SignalR Table Status Sync & Kitchen Notifications
            if (order.TableId.HasValue)
            {
                await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, "Available");
            }

            foreach (var item in order.OrderItems)
            {
                await _notificationService.NotifyOrderItemStatusChangedAsync(order.TableId ?? 0, order.Id, item.Id, item.KitchenStatus, "Cancelled");
            }

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
