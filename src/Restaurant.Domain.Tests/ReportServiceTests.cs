using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class ReportServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetRevenueReportAsync_ShouldCalculateRevenueAndPaymentBreakdownCorrectly()
    {
        using var context = CreateInMemoryContext();

        var role = new Role { Id = 1, Name = "Admin" };
        var shift = new Shift { Id = 1, UserId = 1, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        var user = new User { Id = 1, Username = "admin", FullName = "Admin", RoleId = 1, Role = role };
        context.Roles.Add(role);
        context.Shifts.Add(shift);
        context.Users.Add(user);

        // Order 1: 180,000đ paid via VietQR (BankTransfer)
        var order1 = new Order
        {
            Id = 1,
            OrderCode = "ORD-001",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 180000m,
            CreatedAt = DateTime.UtcNow
        };
        var payment1 = new Payment
        {
            Id = 1,
            OrderId = 1,
            ShiftId = 1,
            PaymentMethod = (int)PaymentMethod.BankTransfer,
            Amount = 180000m,
            Status = (int)PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        // Order 2: 120,000đ paid in Cash
        var order2 = new Order
        {
            Id = 2,
            OrderCode = "ORD-002",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 120000m,
            CreatedAt = DateTime.UtcNow
        };
        var payment2 = new Payment
        {
            Id = 2,
            OrderId = 2,
            ShiftId = 1,
            PaymentMethod = (int)PaymentMethod.Cash,
            Amount = 120000m,
            Status = (int)PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        // Order 3: Cancelled order (should be excluded)
        var order3 = new Order
        {
            Id = 3,
            OrderCode = "ORD-003",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Cancelled,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 300000m,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.AddRange(order1, order2, order3);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var service = new ReportService(context);
        var result = await service.GetRevenueReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

        Assert.NotNull(result);
        Assert.Equal(300000m, result.TotalRevenue);
        Assert.Equal(2, result.TotalOrders);
        Assert.Equal(180000m, result.VietQrRevenue);
        Assert.Equal(120000m, result.CashRevenue);
        Assert.NotEmpty(result.DailyBreakdown);
    }

    [Fact]
    public async Task GetTopSellingReportAsync_ShouldRankItemsAndCalculateSalesCorrectly()
    {
        using var context = CreateInMemoryContext();

        var category = new Category { Id = 1, Name = "Pizza Ý" };
        var pizza = new MenuItem { Id = 1, Name = "Pizza Margherita Mozzarella", CategoryId = 1, Category = category, Price = 180000m };
        var pasta = new MenuItem { Id = 2, Name = "Mì Ý Bolognese Thịt bò Băm", CategoryId = 1, Category = category, Price = 130000m };
        var tea = new MenuItem { Id = 3, Name = "Trà Vải Hạt É Thanh Mát", CategoryId = 1, Category = category, Price = 39000m };

        context.Categories.Add(category);
        context.MenuItems.AddRange(pizza, pasta, tea);

        var order = new Order
        {
            Id = 1,
            OrderCode = "ORD-TOP-001",
            ShiftId = 1,
            CreatedByUserId = 1,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 500000m,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);

        context.OrderItems.AddRange(
            new OrderItem { Id = 1, OrderId = 1, MenuItemId = 1, MenuItemName = "Pizza Margherita Mozzarella", Quantity = 5, UnitPrice = 180000m, Status = (int)OrderItemStatus.Active },
            new OrderItem { Id = 2, OrderId = 1, MenuItemId = 2, MenuItemName = "Mì Ý Bolognese Thịt bò Băm", Quantity = 3, UnitPrice = 130000m, Status = (int)OrderItemStatus.Active },
            new OrderItem { Id = 3, OrderId = 1, MenuItemId = 3, MenuItemName = "Trà Vải Hạt É Thanh Mát", Quantity = 2, UnitPrice = 39000m, Status = (int)OrderItemStatus.Active }
        );

        await context.SaveChangesAsync();

        var service = new ReportService(context);
        var report = await service.GetTopSellingReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

        Assert.NotNull(report);
        Assert.Equal(10, report.TotalSoldQuantity);
        Assert.Equal(3, report.Items.Count);

        // Rank #1: Pizza (5 qty)
        Assert.Equal(1, report.Items[0].Rank);
        Assert.Equal("Pizza Margherita Mozzarella", report.Items[0].ItemName);
        Assert.Equal(5, report.Items[0].QuantitySold);
        Assert.Equal(900000m, report.Items[0].TotalRevenue);
        Assert.Equal("BestSeller", report.Items[0].EvaluationBadge);

        // Rank #2: Pasta (3 qty)
        Assert.Equal(2, report.Items[1].Rank);
        Assert.Equal("Mì Ý Bolognese Thịt bò Băm", report.Items[1].ItemName);
        Assert.Equal("HighSales", report.Items[1].EvaluationBadge);

        // Rank #3: Tea (2 qty)
        Assert.Equal(3, report.Items[2].Rank);
        Assert.Equal("Trà Vải Hạt É Thanh Mát", report.Items[2].ItemName);
    }
}
