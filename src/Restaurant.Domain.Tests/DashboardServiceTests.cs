using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class DashboardServiceTests
{
    private ApplicationDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetSummaryAsync_AccuratelyCalculates_Revenue_ActiveOrders_And_OccupiedTables()
    {
        using var context = GetDbContext(nameof(GetSummaryAsync_AccuratelyCalculates_Revenue_ActiveOrders_And_OccupiedTables));

        // Seed Tables: 1 Available, 1 Occupied, 1 Serving, 1 Billing, 1 Paid
        context.Tables.AddRange(
            new Table { Id = 1, Name = "Bàn 1", Status = (int)TableStatus.Available, AreaId = 1 },
            new Table { Id = 2, Name = "Bàn 2", Status = (int)TableStatus.Occupied, AreaId = 1 },
            new Table { Id = 3, Name = "Bàn 3", Status = (int)TableStatus.Serving, AreaId = 1 },
            new Table { Id = 4, Name = "Bàn 4", Status = (int)TableStatus.Billing, AreaId = 1 },
            new Table { Id = 5, Name = "Bàn 5", Status = (int)TableStatus.Paid, AreaId = 1 }
        );

        // Seed Orders for Today
        var nowUtc = DateTime.UtcNow;
        var paidOrderToday = new Order
        {
            Id = 101,
            OrderCode = "ORD-001",
            TableId = 5,
            Status = (int)OrderStatus.Ready,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 250000m,
            CreatedAt = nowUtc
        };

        var activeOrderInPrep = new Order
        {
            Id = 102,
            OrderCode = "ORD-002",
            TableId = 3,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 180000m,
            CreatedAt = nowUtc
        };

        var completedOrder = new Order
        {
            Id = 103,
            OrderCode = "ORD-003",
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 120000m,
            CreatedAt = nowUtc.AddHours(-1)
        };

        var cancelledOrder = new Order
        {
            Id = 104,
            OrderCode = "ORD-004",
            Status = (int)OrderStatus.Cancelled,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 50000m,
            CreatedAt = nowUtc
        };

        context.Orders.AddRange(paidOrderToday, activeOrderInPrep, completedOrder, cancelledOrder);
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var summary = await service.GetSummaryAsync();

        // Assert
        // Total tables: 5, Occupied (non-available): 4
        Assert.Equal(5, summary.TotalTables);
        Assert.Equal(4, summary.OccupiedTables);

        // Active orders: ORD-001 (Ready) + ORD-002 (InPreparation) = 2 (completed & cancelled excluded)
        Assert.Equal(2, summary.ActiveOrders);

        // Today Revenue: ORD-001 (250,000) + ORD-003 (120,000) = 370,000
        Assert.Equal(370000m, summary.TodayRevenue);
    }

    [Fact]
    public async Task GetSalesChartAsync_ReturnsHourlyData_ForPaidOrders()
    {
        using var context = GetDbContext(nameof(GetSalesChartAsync_ReturnsHourlyData_ForPaidOrders));

        var nowUtc = DateTime.UtcNow;
        context.Orders.Add(new Order
        {
            Id = 201,
            OrderCode = "ORD-201",
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 500000m,
            CreatedAt = nowUtc
        });
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var chart = await service.GetSalesChartAsync();

        // Assert
        Assert.NotEmpty(chart);
        var totalInChart = chart.Sum(c => c.Amount);
        Assert.Equal(500000m, totalInChart);
    }

    [Fact]
    public async Task GetTopMoversAsync_ReturnsTopSellingItems_ForToday()
    {
        using var context = GetDbContext(nameof(GetTopMoversAsync_ReturnsTopSellingItems_ForToday));

        var nowUtc = DateTime.UtcNow;
        var order = new Order
        {
            Id = 301,
            OrderCode = "ORD-301",
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 300000m,
            CreatedAt = nowUtc
        };

        var item1 = new OrderItem
        {
            Id = 1,
            OrderId = 301,
            Order = order,
            MenuItemId = 10,
            MenuItemName = "Pizza Hải Sản",
            Quantity = 3,
            UnitPrice = 80000m,
            Status = (int)OrderItemStatus.Active
        };

        var item2 = new OrderItem
        {
            Id = 2,
            OrderId = 301,
            Order = order,
            MenuItemId = 20,
            MenuItemName = "Coca Cola",
            Quantity = 5,
            UnitPrice = 12000m,
            Status = (int)OrderItemStatus.Active
        };

        context.Orders.Add(order);
        context.OrderItems.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        // Act
        var topMovers = await service.GetTopMoversAsync(null, 5);

        // Assert
        Assert.Equal(2, topMovers.Count);
        Assert.Equal("Coca Cola", topMovers[0].ItemName);
        Assert.Equal(5, topMovers[0].QuantitySold);
        Assert.Equal("Pizza Hải Sản", topMovers[1].ItemName);
        Assert.Equal(3, topMovers[1].QuantitySold);
    }
}
