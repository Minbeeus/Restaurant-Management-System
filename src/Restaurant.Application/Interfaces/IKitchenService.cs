using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IKitchenService
{
    Task<List<KitchenTicketDto>> GetActiveTicketsAsync();
    Task<KitchenItemDto?> UpdateItemStatusAsync(int orderItemId, int newStatus);
    Task<bool> ToggleSoldOutAsync(int menuItemId);
    Task CheckAndBroadcastSlaWarningsAsync();
}
