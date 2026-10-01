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

            var isSentToKitchen = order.OrderItems.Any(oi => oi.KitchenStatus >= (int)KitchenStatus.Processing);
            if (isSentToKitchen && order.PaymentStatus == (int)PaymentStatus.Unpaid)
            {
                // Check if user has manager authorization to cancel an order already in preparation
                var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || (user.Role.Name != "Manager" && user.Role.Name != "Admin"))
                {
                    // Create ApprovalRequest for Manager approval
                    var appReq = new ApprovalRequest
                    {
                        Type = (int)ApprovalType.CancelOrder,
                        ReferenceId = order.Id,
                        Amount = order.TotalAmount,
                        Reason = reason,
                        Note = $"Yêu cầu hủy đơn hàng {order.OrderCode} đã gửi bếp",
                        RequestedByUserId = userId,
                        RequestedAt = DateTime.UtcNow,
                        Status = (int)ApprovalStatus.PendingApproval
                    };
                    _context.ApprovalRequests.Add(appReq);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return false; // Approval request created, awaiting manager approval
                }
            }

            var wasPaid = order.PaymentStatus == (int)PaymentStatus.Paid || order.Status == (int)OrderStatus.Completed;
            
            if (!wasPaid)
            {
                order.Status = (int)OrderStatus.Cancelled;
            }
            
            var cancelText = string.Format(_localizer["CANCEL_ORDER_NOTE"] ?? "[CANCELLED]: {0}", reason);
            order.Note = string.IsNullOrEmpty(order.Note) ? cancelText : $"{order.Note} | {cancelText}";

            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Available;
            }

            // 1. Rollback Inventory (find any BOM deducted via OrderItems for this Order)
            var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
            var saleDeductions = await _context.InventoryTransactions
                .Where(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId.HasValue && orderItemIds.Contains(t.ReferenceId.Value) && t.TransactionType == (int)InventoryTransactionType.SaleDeduction)
                .ToListAsync();

            foreach (var deduction in saleDeductions)
            {
                deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;
                var wasteText = string.Format(_localizer["CANCELLATION_WASTE_NOTE"] ?? "[WASTE] Cancelled Order {0} | Origin: {1}", order.OrderCode, deduction.Note);
                deduction.Note = wasteText;
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
