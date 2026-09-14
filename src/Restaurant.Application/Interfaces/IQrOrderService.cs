using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IQrOrderService
{
    Task<(bool IsValid, string? ErrorCode, string? ErrorMessage)> ValidateTableSessionAsync(int tableId, string qrToken);
    Task<PublicMenuResponse> GetPublicMenuAsync(int tableId, string qrToken);
    Task<MenuItemDetailDto?> GetMenuItemDetailAsync(int menuItemId, int tableId, string qrToken);
    Task<CartValidateResponse> ValidateCartAsync(CartValidateRequest request);
    Task<SubmitQrOrderResponse> SubmitQrOrderAsync(SubmitQrOrderRequest request);
    Task<QrOrderStatusResponse?> GetOrderStatusForQrAsync(int tableId, string qrToken);
}
