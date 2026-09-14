namespace Restaurant.Application.Interfaces;

public interface IRestaurantNotificationService
{
    Task NotifyNewQrOrderAsync(int orderId, int tableId, string tableName, string orderCode, decimal totalAmount);
    Task NotifyOrderItemStatusChangedAsync(int tableId, int orderId, int orderItemId, int kitchenStatus, string kitchenStatusName);
    Task NotifyTableStatusSyncAsync(int tableId, string status);
    Task NotifyTableSessionMovedAsync(int oldTableId, int newTableId, string newTableName);
    Task NotifyKitchenTicketAsync(DTOs.KitchenTicketDto ticket);
    Task NotifyLowStockWarningAsync(int materialId, string materialName, decimal currentStock, decimal minStockLevel);
    Task NotifyPaymentCompletedAsync(int orderId, string orderCode, decimal amountPaid, string paymentMethod);
    Task NotifyPaymentAlertAsync(int orderId, string orderCode, string message);
}
