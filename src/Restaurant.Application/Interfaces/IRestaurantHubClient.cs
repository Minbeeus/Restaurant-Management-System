using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IRestaurantHubClient
{
    Task ReceiveKitchenTicket(KitchenTicketDto ticket);
    Task ReceiveItemStatusUpdate(int orderItemId, string status, int orderId);
    Task ReceiveKitchenSlaWarning(int orderItemId, int pendingMinutes);
    Task ReceiveSoldOutNotice(int menuItemId, bool isSoldOut);
    Task ReceiveTableStatusSync(int tableId, string status);
    Task ReceiveNewQrOrder(int orderId, int tableId, string tableName, string orderCode, decimal totalAmount);
    Task ReceiveOrderItemStatusChanged(int tableId, int orderId, int orderItemId, int kitchenStatus, string kitchenStatusName);
    Task ReceiveTableSessionMoved(int oldTableId, int newTableId, string newTableName);
    Task ReceiveLowStockWarning(int materialId, string materialName, decimal currentStock, decimal minStockLevel);
    Task ReceivePaymentCompleted(int orderId, string orderCode, decimal amountPaid, string paymentMethod);
    Task ReceivePaymentAlert(int orderId, string orderCode, string message);
}
