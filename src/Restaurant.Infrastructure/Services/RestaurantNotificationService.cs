using Microsoft.AspNetCore.SignalR;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Common;
using Restaurant.Infrastructure.Hubs;

namespace Restaurant.Infrastructure.Services;

public class RestaurantNotificationService : IRestaurantNotificationService
{
    private readonly IHubContext<RestaurantHub, IRestaurantHubClient> _hubContext;

    public RestaurantNotificationService(IHubContext<RestaurantHub, IRestaurantHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewQrOrderAsync(int orderId, int tableId, string tableName, string orderCode, decimal totalAmount)
    {
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceiveNewQrOrder(orderId, tableId, tableName, orderCode, totalAmount);

        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceiveTableStatusSync(tableId, "NewOrder");
    }

    public async Task NotifyOrderItemStatusChangedAsync(int tableId, int orderId, int orderItemId, int kitchenStatus, string kitchenStatusName)
    {
        // Broadcast to Cashiers Group
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceiveItemStatusUpdate(orderItemId, kitchenStatusName, orderId);

        // Broadcast to Chefs Group (so KDS receives Cancel/Void events in real-time)
        await _hubContext.Clients.Group(RestaurantHub.ChefsGroup)
            .ReceiveItemStatusUpdate(orderItemId, kitchenStatusName, orderId);

        // Broadcast to specific Table Group
        await _hubContext.Clients.Group($"TableGroup_{tableId}")
            .ReceiveOrderItemStatusChanged(tableId, orderId, orderItemId, kitchenStatus, kitchenStatusName);
    }

    public async Task NotifyTableStatusSyncAsync(int tableId, string status)
    {
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceiveTableStatusSync(tableId, status);
    }

    public async Task NotifyTableSessionMovedAsync(int oldTableId, int newTableId, string newTableName)
    {
        await _hubContext.Clients.Group($"TableGroup_{oldTableId}")
            .ReceiveTableSessionMoved(oldTableId, newTableId, newTableName);
    }

    public async Task NotifyKitchenTicketAsync(Application.DTOs.KitchenTicketDto ticket)
    {
        // Broadcast to general Chefs group
        await _hubContext.Clients.Group(RestaurantHub.ChefsGroup)
            .ReceiveKitchenTicket(ticket);

        // Also route sub-tickets or tickets to specific station groups if items specify KitchenStationId
        var stationIds = ticket.Items
            .Where(i => i.KitchenStationId.HasValue)
            .Select(i => i.KitchenStationId!.Value)
            .Distinct()
            .ToList();

        foreach (var sId in stationIds)
        {
            var stationItems = ticket.Items.Where(i => i.KitchenStationId == sId || (i.ParentItemId.HasValue && ticket.Items.Any(p => p.OrderItemId == i.ParentItemId && p.KitchenStationId == sId))).ToList();
            if (stationItems.Any())
            {
                var stationTicket = new Application.DTOs.KitchenTicketDto
                {
                    OrderId = ticket.OrderId,
                    OrderCode = ticket.OrderCode,
                    TableId = ticket.TableId,
                    TableName = ticket.TableName,
                    CreatedAt = ticket.CreatedAt,
                    Items = stationItems
                };
                await _hubContext.Clients.Group(SystemConstants.SignalRGroups.StationGroup(sId))
                    .ReceiveKitchenTicket(stationTicket);
            }
        }
    }

    public async Task NotifyLowStockWarningAsync(int materialId, string materialName, decimal currentStock, decimal minStockLevel)
    {
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceiveLowStockWarning(materialId, materialName, currentStock, minStockLevel);
    }

    public async Task NotifyPaymentCompletedAsync(int orderId, string orderCode, decimal amountPaid, string paymentMethod)
    {
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceivePaymentCompleted(orderId, orderCode, amountPaid, paymentMethod);
    }

    public async Task NotifyPaymentAlertAsync(int orderId, string orderCode, string message)
    {
        await _hubContext.Clients.Group(RestaurantHub.CashiersGroup)
            .ReceivePaymentAlert(orderId, orderCode, message);
    }
}
