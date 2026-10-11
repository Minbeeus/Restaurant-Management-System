using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Common;
using MediatR;
using Restaurant.Domain.Events;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class KitchenService : IKitchenService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<RestaurantHub, IRestaurantHubClient> _hubContext;
    private readonly IMediator _mediator;
    private readonly IAuditService? _auditService;
    private readonly ICurrentUserService? _currentUserService;

    public KitchenService(
        ApplicationDbContext context, 
        IHubContext<RestaurantHub, IRestaurantHubClient> hubContext,
        IMediator mediator,
        IAuditService? auditService = null,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _hubContext = hubContext;
        _mediator = mediator;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<List<KitchenTicketDto>> GetActiveTicketsAsync(bool includeCompleted = false, int? stationId = null)
    {
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;

        var query = _context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                    .ThenInclude(m => m.KitchenStation)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ParentItem)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .AsQueryable();

        if (!includeCompleted)
        {
            query = query.Where(o => o.Status != (int)OrderStatus.Cancelled && o.OrderItems.Any(oi => oi.KitchenStatus != (int)KitchenStatus.Done && oi.Status == (int)OrderItemStatus.Active));
        }
        else
        {
            query = query.Where(o => o.CreatedAt >= today && o.Status != (int)OrderStatus.Cancelled);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new KitchenTicketDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            TableId = o.TableId,
            TableName = o.Table?.Name ?? "Mang về",
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems
                .Where(oi => oi.Status == (int)OrderItemStatus.Active && (!stationId.HasValue || oi.MenuItem == null || oi.MenuItem.KitchenStationId == null || oi.MenuItem.KitchenStationId == stationId.Value))
                .Select(oi =>
                {
                    var waitingTime = (utcNow - (oi.SentToKitchenAt ?? oi.CreatedAt)).TotalMinutes;
                    return new KitchenItemDto
                    {
                        OrderItemId = oi.Id,
                        OrderId = oi.OrderId,
                        MenuItemId = oi.MenuItemId,
                        MenuItemName = oi.MenuItemName,
                        Quantity = oi.Quantity,
                        Note = oi.Note,
                        KitchenStatus = oi.KitchenStatus,
                        KitchenStatusName = Enum.GetName(typeof(KitchenStatus), oi.KitchenStatus) ?? "Unknown",
                        SentToKitchenAt = oi.SentToKitchenAt,
                        CompletedAt = oi.CompletedAt,
                        WaitingMinutes = (int)waitingTime,
                        IsSlaWarning = waitingTime > 15 && oi.KitchenStatus != (int)KitchenStatus.Done,
                        Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList(),
                        ParentItemId = oi.ParentItemId,
                        ParentItemName = oi.ParentItem?.MenuItemName,
                        IsComboHeader = oi.MenuItem != null && oi.MenuItem.IsCombo && !oi.ParentItemId.HasValue,
                        ComboGroupId = oi.ComboGroupId,
                        KitchenStationId = oi.MenuItem?.KitchenStationId,
                        KitchenStationName = oi.MenuItem?.KitchenStation?.Name,
                        Status = oi.Status
                    };
                }).ToList()
        }).Where(t => t.Items.Any()).ToList();
    }

    public async Task<KitchenItemDto?> UpdateItemStatusAsync(int orderItemId, int newStatus)
    {
        // 1. Validate status enum range
        if (!Enum.IsDefined(typeof(KitchenStatus), newStatus))
        {
            return null;
        }

        var item = await _context.OrderItems
            .Include(oi => oi.MenuItem)
                .ThenInclude(m => m.KitchenStation)
            .Include(oi => oi.ParentItem)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Table)
            .Include(oi => oi.OrderItemModifiers)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

        // 2. Reject if item missing, voided, or order cancelled
        if (item == null || item.Status == (int)OrderItemStatus.Voided || (item.Order != null && item.Order.Status == (int)OrderStatus.Cancelled))
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        var oldStatus = item.KitchenStatus;

        // 3. Idempotent check: if status is unchanged, return current state
        if (oldStatus == newStatus)
        {
            var existingWaitingTime = (utcNow - (item.SentToKitchenAt ?? item.CreatedAt)).TotalMinutes;
            return new KitchenItemDto
            {
                OrderItemId = item.Id,
                OrderId = item.OrderId,
                MenuItemId = item.MenuItemId,
                MenuItemName = item.MenuItemName,
                Quantity = item.Quantity,
                Note = item.Note,
                KitchenStatus = item.KitchenStatus,
                KitchenStatusName = Enum.GetName(typeof(KitchenStatus), item.KitchenStatus) ?? "Unknown",
                SentToKitchenAt = item.SentToKitchenAt,
                CompletedAt = item.CompletedAt,
                WaitingMinutes = (int)existingWaitingTime,
                IsSlaWarning = existingWaitingTime > 15 && item.KitchenStatus != (int)KitchenStatus.Done,
                Modifiers = item.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList(),
                ParentItemId = item.ParentItemId,
                ParentItemName = item.ParentItem?.MenuItemName,
                IsComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue,
                ComboGroupId = item.ComboGroupId,
                KitchenStationId = item.MenuItem?.KitchenStationId,
                KitchenStationName = item.MenuItem?.KitchenStation?.Name,
                Status = item.Status
            };
        }

        // 4. Validate transition rule: cannot revert from Done (2)
        if (oldStatus == (int)KitchenStatus.Done && newStatus != (int)KitchenStatus.Done)
        {
            return null;
        }

        item.KitchenStatus = newStatus;

        if (newStatus == (int)KitchenStatus.Processing && item.SentToKitchenAt == null)
        {
            item.SentToKitchenAt = utcNow;
        }
        else if (newStatus == (int)KitchenStatus.Done)
        {
            item.CompletedAt = utcNow;
        }

        // 5. If this item has a Parent (Combo Header), evaluate and update Combo Header KitchenStatus
        if (item.ParentItemId.HasValue)
        {
            var siblings = await _context.OrderItems
                .Where(oi => oi.ParentItemId == item.ParentItemId.Value && oi.Status == (int)OrderItemStatus.Active && oi.Id != item.Id)
                .ToListAsync();

            var allChildren = siblings.Concat(new[] { item }).ToList();
            var parentItem = await _context.OrderItems.FindAsync(item.ParentItemId.Value);

            if (parentItem != null && parentItem.Status == (int)OrderItemStatus.Active)
            {
                if (allChildren.All(c => c.KitchenStatus == (int)KitchenStatus.Done))
                {
                    parentItem.KitchenStatus = (int)KitchenStatus.Done;
                    parentItem.CompletedAt = utcNow;
                }
                else if (allChildren.Any(c => c.KitchenStatus == (int)KitchenStatus.Processing || c.KitchenStatus == (int)KitchenStatus.Done))
                {
                    parentItem.KitchenStatus = (int)KitchenStatus.Processing;
                    if (parentItem.SentToKitchenAt == null) parentItem.SentToKitchenAt = utcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        // 6. Audit Log logging
        if (_auditService != null)
        {
            var actingUserId = _currentUserService?.UserId ?? item.Order?.CreatedByUserId ?? 0;
            if (actingUserId > 0)
            {
                await _auditService.LogAsync(
                    userId: actingUserId,
                    action: "Kitchen.ItemStatusUpdate",
                    entityName: "OrderItem",
                    entityId: item.Id.ToString(),
                    details: $"Updated KitchenStatus from {oldStatus} to {newStatus} for {item.MenuItemName} (Order {item.Order.OrderCode})"
                );
            }
        }

        // 7. Domain Event publishing for BOM Inventory Deduction on Done (only for non-combo headers)
        if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
        {
            // Do not deduct for Combo header itself; deduct for child items and standalone items
            bool isComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue;
            if (!isComboHeader)
            {
                await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
            }
        }

        // 8. Check if all items in this order are completed
        var allItemsInOrder = await _context.OrderItems
            .Where(oi => oi.OrderId == item.OrderId && oi.Status == (int)OrderItemStatus.Active)
            .ToListAsync();

        var isAllDone = allItemsInOrder.All(oi => oi.KitchenStatus == (int)KitchenStatus.Done);
        if (isAllDone && item.Order.Status != (int)OrderStatus.Completed && item.Order.Status != (int)OrderStatus.Cancelled)
        {
            // Bếp hoàn thành toàn bộ -> Order.Status = Ready (Sẵn sàng phục vụ), KHÔNG tự động chuyển Completed trước thanh toán
            item.Order.Status = (int)OrderStatus.Ready;
            await _context.SaveChangesAsync();
        }

        var statusName = Enum.GetName(typeof(KitchenStatus), newStatus) ?? "Unknown";

        // 9. SignalR Broadcast to Cashiers, Chefs, and Table
        await _hubContext.Clients.Group(SystemConstants.SignalRGroups.Cashiers)
            .ReceiveItemStatusUpdate(item.Id, statusName, item.OrderId);

        await _hubContext.Clients.Group(SystemConstants.SignalRGroups.Chefs)
            .ReceiveItemStatusUpdate(item.Id, statusName, item.OrderId);

        if (item.MenuItem?.KitchenStationId.HasValue == true)
        {
            await _hubContext.Clients.Group(SystemConstants.SignalRGroups.StationGroup(item.MenuItem.KitchenStationId.Value))
                .ReceiveItemStatusUpdate(item.Id, statusName, item.OrderId);
        }

        if (item.Order.TableId.HasValue)
        {
            await _hubContext.Clients.Group(SystemConstants.SignalRGroups.TableGroup(item.Order.TableId.Value))
                .ReceiveItemStatusUpdate(item.Id, statusName, item.OrderId);

            await _hubContext.Clients.Group(SystemConstants.SignalRGroups.TableGroup(item.Order.TableId.Value))
                .ReceiveOrderItemStatusChanged(item.Order.TableId.Value, item.OrderId, item.Id, item.KitchenStatus, statusName);
        }

        var waitingTime = (utcNow - (item.SentToKitchenAt ?? item.CreatedAt)).TotalMinutes;
        return new KitchenItemDto
        {
            OrderItemId = item.Id,
            OrderId = item.OrderId,
            MenuItemId = item.MenuItemId,
            MenuItemName = item.MenuItemName,
            Quantity = item.Quantity,
            Note = item.Note,
            KitchenStatus = item.KitchenStatus,
            KitchenStatusName = statusName,
            SentToKitchenAt = item.SentToKitchenAt,
            CompletedAt = item.CompletedAt,
            WaitingMinutes = (int)waitingTime,
            IsSlaWarning = waitingTime > 15 && item.KitchenStatus != (int)KitchenStatus.Done,
            Modifiers = item.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList(),
            ParentItemId = item.ParentItemId,
            ParentItemName = item.ParentItem?.MenuItemName,
            IsComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue,
            ComboGroupId = item.ComboGroupId,
            KitchenStationId = item.MenuItem?.KitchenStationId,
            KitchenStationName = item.MenuItem?.KitchenStation?.Name,
            Status = item.Status
        };
    }

    public async Task<bool> ToggleSoldOutAsync(int menuItemId)
    {
        var menuItem = await _context.MenuItems.FindAsync(menuItemId);
        if (menuItem == null) return false;

        menuItem.IsSoldOut = !menuItem.IsSoldOut;
        await _context.SaveChangesAsync();

        // Broadcast to all clients
        await _hubContext.Clients.All.ReceiveSoldOutNotice(menuItem.Id, menuItem.IsSoldOut);
        return true;
    }

    public async Task CheckAndBroadcastSlaWarningsAsync()
    {
        var utcNow = DateTime.UtcNow;
        var delayedItems = await _context.OrderItems
            .Where(oi => oi.KitchenStatus != (int)KitchenStatus.Done)
            .ToListAsync();

        foreach (var item in delayedItems)
        {
            var startTime = item.SentToKitchenAt ?? item.CreatedAt;
            var waitingMinutes = (int)(utcNow - startTime).TotalMinutes;

            if (waitingMinutes >= SystemConstants.KitchenSlaWarningMinutes)
            {
                await _hubContext.Clients.Group(SystemConstants.SignalRGroups.Chefs)
                    .ReceiveKitchenSlaWarning(item.Id, waitingMinutes);
                await _hubContext.Clients.Group(SystemConstants.SignalRGroups.Cashiers)
                    .ReceiveKitchenSlaWarning(item.Id, waitingMinutes);
            }
        }
    }
}
