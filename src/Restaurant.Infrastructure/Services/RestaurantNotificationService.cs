using Microsoft.AspNetCore.SignalR;
using Restaurant.Application.Interfaces;
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
        await _hubContext.Clients.Group(RestaurantHub.ChefsGroup)
            .ReceiveKitchenTicket(ticket);
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
