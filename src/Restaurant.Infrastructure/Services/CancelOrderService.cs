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

            var wasPaid = order.Status == (int)OrderStatus.Paid;
            order.Status = (int)OrderStatus.Cancelled;
            
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
                // Thay vì cộng lại kho (Rollback), hệ thống giữ nguyên trạng thái kho đã trừ
                // và chỉ chuyển đổi (convert) nhãn giao dịch từ Bán hàng sang Hủy/Hao hụt.
                deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;
                var wasteText = string.Format(_localizer["CANCELLATION_WASTE_NOTE"] ?? "[WASTE] Cancelled Order {0} | Origin: {1}", order.OrderCode, deduction.Note);
                deduction.Note = wasteText;
            }

            // 2. Rollback Loyalty Points if order was paid
            if (wasPaid)
            {
                await _loyaltyService.RollbackLoyaltyForCancelledOrderAsync(order.Id);
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Broadcast SignalR Table Status Sync
            if (order.TableId.HasValue)
            {
                await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, "Available");
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
