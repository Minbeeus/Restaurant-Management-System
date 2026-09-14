using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class KdsServicesTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IHubContext<RestaurantHub, IRestaurantHubClient>> _hubContextMock;
    private readonly Mock<IRestaurantHubClient> _hubClientMock;
    private readonly Mock<IHubClients<IRestaurantHubClient>> _clientsMock;
    private readonly Mock<MediatR.IMediator> _mediatorMock;
    private readonly KitchenService _kitchenService;

    public KdsServicesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor())
            .Options;

        _context = new ApplicationDbContext(options);

        _hubContextMock = new Mock<IHubContext<RestaurantHub, IRestaurantHubClient>>();
        _hubClientMock = new Mock<IRestaurantHubClient>();
        _clientsMock = new Mock<IHubClients<IRestaurantHubClient>>();

        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_hubClientMock.Object);
        _clientsMock.Setup(c => c.All).Returns(_hubClientMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _mediatorMock = new Mock<MediatR.IMediator>();
        _kitchenService = new KitchenService(_context, _hubContextMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task GetActiveTickets_And_UpdateItemStatus_ShouldProcessKitchenFlow()
    {
        // Arrange
        var table = new Table { Name = "Bàn 05", AreaId = 1, QrToken = "token123" };
        var order = new Order
        {
            OrderCode = "ORD-001",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Confirmed,
            Table = table
        };
        var item1 = new OrderItem
        {
            Order = order,
            MenuItemId = 10,
            MenuItemName = "Phở Bò",
            Quantity = 2,
            KitchenStatus = (int)KitchenStatus.Pending
        };
        var item2 = new OrderItem
        {
            Order = order,
            MenuItemId = 11,
            MenuItemName = "Trà Đào",
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Pending
        };

        _context.Tables.Add(table);
        _context.Orders.Add(order);
        _context.OrderItems.AddRange(item1, item2);
        await _context.SaveChangesAsync();

        // Act 1: Get Active Tickets
        var tickets = await _kitchenService.GetActiveTicketsAsync();

        // Assert 1
        Assert.Single(tickets);
        Assert.Equal("ORD-001", tickets[0].OrderCode);
        Assert.Equal(2, tickets[0].Items.Count);

        // Act 2: Update item 1 status to Processing
        var updatedItem1 = await _kitchenService.UpdateItemStatusAsync(item1.Id, (int)KitchenStatus.Processing);

        // Assert 2
        Assert.NotNull(updatedItem1);
        Assert.Equal((int)KitchenStatus.Processing, updatedItem1.KitchenStatus);
        Assert.NotNull(updatedItem1.SentToKitchenAt);

        // Act 3: Complete item 1 and item 2
        await _kitchenService.UpdateItemStatusAsync(item1.Id, (int)KitchenStatus.Done);
        await _kitchenService.UpdateItemStatusAsync(item2.Id, (int)KitchenStatus.Done);

        // Assert 3: All items done -> Order auto-completed
        var dbOrder = await _context.Orders.FindAsync(order.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal((int)OrderStatus.Completed, dbOrder.Status);
    }

    [Fact]
    public async Task CheckAndBroadcastSlaWarnings_ShouldTriggerForDelayedItems()
    {
        // Arrange
        var order = new Order
        {
            OrderCode = "ORD-SLA",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20) // Created 20 mins ago
        };
        var item = new OrderItem
        {
            Order = order,
            MenuItemId = 12,
            MenuItemName = "Bún Chả",
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Pending,
            SentToKitchenAt = DateTime.UtcNow.AddMinutes(-18) // Sent 18 mins ago
        };

        _context.Orders.Add(order);
        _context.OrderItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _kitchenService.CheckAndBroadcastSlaWarningsAsync();

        // Assert: SignalR Warning event broadcasted
        _hubClientMock.Verify(c => c.ReceiveKitchenSlaWarning(item.Id, It.Is<int>(mins => mins >= 15)), Times.AtLeastOnce());
    }
}
