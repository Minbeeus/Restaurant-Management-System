using MediatR;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Events;

namespace Restaurant.Infrastructure.Handlers;

public class DeductInventoryOnOrderItemKitchenDoneHandler : INotificationHandler<OrderItemKitchenDoneDomainEvent>
{
    private readonly IInventoryService _inventoryService;

    public DeductInventoryOnOrderItemKitchenDoneHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Handle(OrderItemKitchenDoneDomainEvent notification, CancellationToken cancellationToken)
    {
        await _inventoryService.DeductInventoryForOrderItemAsync(notification.OrderItemId);
    }
}
