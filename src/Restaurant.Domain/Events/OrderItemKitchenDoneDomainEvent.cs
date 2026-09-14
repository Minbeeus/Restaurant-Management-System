using MediatR;

namespace Restaurant.Domain.Events;

public record OrderItemKitchenDoneDomainEvent(int OrderItemId, int OrderId) : INotification;
