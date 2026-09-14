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

    public KitchenService(
        ApplicationDbContext context, 
        IHubContext<RestaurantHub, IRestaurantHubClient> hubContext,
        IMediator mediator)
    {
        _context = context;
        _hubContext = hubContext;
        _mediator = mediator;
    }

    public async Task<List<KitchenTicketDto>> GetActiveTicketsAsync()
    {
        var utcNow = DateTime.UtcNow;

        var orders = await _context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .Where(o => o.OrderItems.Any(oi => oi.KitchenStatus != (int)KitchenStatus.Done))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new KitchenTicketDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            TableId = o.TableId,
            TableName = o.Table?.Name ?? "Mang về",
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems.Select(oi =>
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
                    Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()
                };
            }).ToList()
        }).ToList();
    }

    public async Task<KitchenItemDto?> UpdateItemStatusAsync(int orderItemId, int newStatus)
    {
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Table)
            .Include(oi => oi.OrderItemModifiers)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

        if (item == null) return null;

        var utcNow = DateTime.UtcNow;
        item.KitchenStatus = newStatus;

        if (newStatus == (int)KitchenStatus.Processing && item.SentToKitchenAt == null)
        {
            item.SentToKitchenAt = utcNow;
        }
        else if (newStatus == (int)KitchenStatus.Done)
        {
            item.CompletedAt = utcNow;
        }

        await _context.SaveChangesAsync();

        if (newStatus == (int)KitchenStatus.Done)
        {
            await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
        }

        // Check if all items in this order are completed
        var allItemsInOrder = await _context.OrderItems
            .Where(oi => oi.OrderId == item.OrderId)
            .ToListAsync();

        var isAllDone = allItemsInOrder.All(oi => oi.KitchenStatus == (int)KitchenStatus.Done);
        if (isAllDone && item.Order.Status != (int)OrderStatus.Paid && item.Order.Status != (int)OrderStatus.Cancelled)
        {
            item.Order.Status = (int)OrderStatus.Completed;
            await _context.SaveChangesAsync();
        }

        var statusName = Enum.GetName(typeof(KitchenStatus), newStatus) ?? "Unknown";

        // SignalR Broadcast to Cashiers & Table
        await _hubContext.Clients.Group(SystemConstants.SignalRGroups.Cashiers)
            .ReceiveItemStatusUpdate(item.Id, statusName, item.OrderId);

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
            Modifiers = item.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()
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
