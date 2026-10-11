using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class PosReceiptPrintingTests
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

    private (User cashier, Register register, Shift shift, Table table, Category category) SeedBaseData(ApplicationDbContext db)
    {
        var role = new Role { Id = 1, Name = "Cashier", Description = "Thu Ngân" };
        var cashier = new User { Id = 1, Username = "cashier1", FullName = "Nguyễn Thu Ngân", RoleId = 1, Role = role, IsActive = true };
        var register = new Register { Id = 1, Code = "REG01", Name = "Quầy 01", IsActive = true };
        var shift = new Shift { Id = 1, UserId = 1, RegisterId = 1, Status = (int)ShiftStatus.Open, OpeningBalance = 1000000m, OpenedAt = DateTime.UtcNow };
        var table = new Table { Id = 1, Name = "Bàn 05", Status = (int)TableStatus.Occupied };
        var category = new Category { Id = 1, Name = "Pizza & Mì Ý", SortOrder = 1, IsActive = true };

        db.Roles.Add(role);
        db.Users.Add(cashier);
        db.Registers.Add(register);
        db.Shifts.Add(shift);
        db.Tables.Add(table);
        db.Categories.Add(category);
        db.SaveChanges();

        return (cashier, register, shift, table, category);
    }

    [Fact]
    public async Task Test1_GetBillK80Receipt_CorrectDataMapping_AndAuthoritativeTotals()
    {
        var db = GetDbContext(nameof(Test1_GetBillK80Receipt_CorrectDataMapping_AndAuthoritativeTotals));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var pizza = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Pizza Hải Sản", Price = 250000, IsActive = true };
        db.MenuItems.Add(pizza);

        var order = new Order
        {
            Id = 100,
            OrderCode = "HD-20261011-0100",
            OrderType = (int)OrderType.DineIn,
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            CustomerName = "Trần Thị Mai",
            CustomerPhone = "0912.333.444",
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            SubTotal = 250000,
            DiscountAmount = 25000,
            TotalAmount = 225000,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = order.Id,
            MenuItemId = pizza.Id,
            MenuItemName = pizza.Name,
            UnitPrice = 250000,
            DiscountAmount = 25000,
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Pending
        };

        order.OrderItems.Add(orderItem);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.Equal("HD-20261011-0100", receipt.OrderCode);
        Assert.Equal("Bàn 05", receipt.TableName);
        Assert.Equal("Nguyễn Thu Ngân", receipt.CashierName);
        Assert.Equal("Trần Thị Mai", receipt.CustomerName);
        Assert.Equal("0912.333.444", receipt.CustomerPhone);
        Assert.Equal(250000, receipt.SubTotal);
        Assert.Equal(25000, receipt.DiscountAmount);
        Assert.Equal(225000, receipt.TotalAmount);
        Assert.Single(receipt.Items);
        Assert.Equal("Pizza Hải Sản", receipt.Items[0].MenuItemName);
        Assert.Equal(225000, receipt.Items[0].TotalPrice);
    }

    [Fact]
    public async Task Test2_ComboParentChild_HierarchyAndDisplayData()
    {
        var db = GetDbContext(nameof(Test2_ComboParentChild_HierarchyAndDisplayData));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var comboItem = new MenuItem { Id = 50, CategoryId = category.Id, Name = "Combo Gia Đình", Price = 350000, IsCombo = true, IsActive = true };
        var pizza = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Pizza Bò Bằm", Price = 180000, IsActive = true };
        var drink = new MenuItem { Id = 20, CategoryId = category.Id, Name = "Coca Cola", Price = 25000, IsActive = true };
        db.MenuItems.AddRange(comboItem, pizza, drink);

        var order = new Order
        {
            Id = 101,
            OrderCode = "HD-COMBO-001",
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            SubTotal = 350000,
            DiscountAmount = 0,
            TotalAmount = 350000,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };

        var parent = new OrderItem
        {
            Id = 10,
            OrderId = order.Id,
            MenuItemId = comboItem.Id,
            MenuItemName = comboItem.Name,
            UnitPrice = 350000,
            Quantity = 1
        };

        var child1 = new OrderItem
        {
            Id = 11,
            OrderId = order.Id,
            MenuItemId = pizza.Id,
            MenuItemName = pizza.Name,
            UnitPrice = 0,
            Quantity = 1,
            ParentItemId = 10
        };

        var child2 = new OrderItem
        {
            Id = 12,
            OrderId = order.Id,
            MenuItemId = drink.Id,
            MenuItemName = drink.Name,
            UnitPrice = 0,
            Quantity = 2,
            ParentItemId = 10
        };

        order.OrderItems.Add(parent);
        order.OrderItems.Add(child1);
        order.OrderItems.Add(child2);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.Equal(3, receipt.Items.Count);

        // First item should be parent combo
        Assert.Equal("Combo Gia Đình", receipt.Items[0].MenuItemName);
        Assert.False(receipt.Items[0].IsComboChild);

        // Following items should be children
        Assert.Equal("Pizza Bò Bằm", receipt.Items[1].MenuItemName);
        Assert.True(receipt.Items[1].IsComboChild);
        Assert.Equal(10, receipt.Items[1].ParentItemId);

        Assert.Equal("Coca Cola", receipt.Items[2].MenuItemName);
        Assert.True(receipt.Items[2].IsComboChild);
        Assert.Equal(10, receipt.Items[2].ParentItemId);
    }

    [Fact]
    public async Task Test3_ModifiersAndToppings_IncludedInReceipt()
    {
        var db = GetDbContext(nameof(Test3_ModifiersAndToppings_IncludedInReceipt));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var pizza = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Pizza Margherita", Price = 150000, IsActive = true };
        db.MenuItems.Add(pizza);

        var order = new Order
        {
            Id = 102,
            OrderCode = "HD-MOD-001",
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            SubTotal = 210000,
            TotalAmount = 210000,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };

        var item = new OrderItem
        {
            Id = 20,
            OrderId = order.Id,
            MenuItemId = pizza.Id,
            MenuItemName = pizza.Name,
            UnitPrice = 210000,
            Quantity = 1,
            Note = "Nướng vàng giòn"
        };

        var opt1 = new ModifierOption { Id = 1, Name = "Cỡ: 30cm - Lớn", ExtraPrice = 40000 };
        var opt2 = new ModifierOption { Id = 2, Name = "Đế: Viền Phô Mai", ExtraPrice = 20000 };
        db.ModifierOptions.AddRange(opt1, opt2);

        var mod1 = new OrderItemModifier { Id = 1, OrderItemId = item.Id, ModifierOptionId = 1, ModifierOption = opt1, ModifierOptionName = opt1.Name, ExtraPrice = 40000 };
        var mod2 = new OrderItemModifier { Id = 2, OrderItemId = item.Id, ModifierOptionId = 2, ModifierOption = opt2, ModifierOptionName = opt2.Name, ExtraPrice = 20000 };
        item.OrderItemModifiers.Add(mod1);
        item.OrderItemModifiers.Add(mod2);

        order.OrderItems.Add(item);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.Single(receipt.Items);
        var receiptItem = receipt.Items[0];
        Assert.Equal("Nướng vàng giòn", receiptItem.Note);
        Assert.Equal(2, receiptItem.Modifiers.Count);
        Assert.Contains("Cỡ: 30cm - Lớn", receiptItem.Modifiers);
        Assert.Contains("Đế: Viền Phô Mai", receiptItem.Modifiers);
    }

    [Fact]
    public async Task Test4_UnpaidOrder_DoesNotInventPaymentOrMarkPaid()
    {
        var db = GetDbContext(nameof(Test4_UnpaidOrder_DoesNotInventPaymentOrMarkPaid));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var item = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Mì Ý Carbonara", Price = 120000, IsActive = true };
        db.MenuItems.Add(item);

        var order = new Order
        {
            Id = 103,
            OrderCode = "HD-PREBILL-001",
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            SubTotal = 120000,
            TotalAmount = 120000,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };

        order.OrderItems.Add(new OrderItem { Id = 31, OrderId = order.Id, MenuItemId = item.Id, MenuItemName = item.Name, UnitPrice = 120000, Quantity = 1 });
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.False(receipt.IsPaid);
        Assert.Equal("PHIẾU TẠM TÍNH (PRE-BILL)", receipt.ReceiptTitle);
        Assert.Equal("Chưa thanh toán", receipt.PaymentStatusName);
        Assert.Equal("Chưa thanh toán", receipt.PaymentMethod);
        Assert.Null(receipt.CashReceived);
        Assert.Null(receipt.ChangeAmount);
    }

    [Fact]
    public async Task Test5_PaidOrder_DisplaysPaymentDetailsAccurately()
    {
        var db = GetDbContext(nameof(Test5_PaidOrder_DisplaysPaymentDetailsAccurately));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var item = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Pizza Pepperoni", Price = 200000, IsActive = true };
        db.MenuItems.Add(item);

        var order = new Order
        {
            Id = 104,
            OrderCode = "HD-PAID-001",
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            SubTotal = 200000,
            TotalAmount = 200000,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid
        };

        order.OrderItems.Add(new OrderItem { Id = 41, OrderId = order.Id, MenuItemId = item.Id, MenuItemName = item.Name, UnitPrice = 200000, Quantity = 1 });

        var payment = new Payment
        {
            Id = 1,
            OrderId = order.Id,
            ShiftId = shift.Id,
            PaymentMethod = (int)PaymentMethod.Cash,
            Amount = 200000,
            CashReceived = 300000,
            ChangeAmount = 100000,
            Status = (int)PaymentStatus.Paid,
            CompletedAt = DateTime.UtcNow
        };
        order.Payments.Add(payment);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.True(receipt.IsPaid);
        Assert.Equal("HÓA ĐƠN THANH TOÁN", receipt.ReceiptTitle);
        Assert.Equal("Đã thanh toán", receipt.PaymentStatusName);
        Assert.Equal("Tiền Mặt", receipt.PaymentMethod);
        Assert.Equal(300000, receipt.CashReceived);
        Assert.Equal(100000, receipt.ChangeAmount);
    }

    [Fact]
    public async Task Test6_PrintTempBill_TransitionsTableToBilling_WithoutChangingOrderPaymentStatus()
    {
        var db = GetDbContext(nameof(Test6_PrintTempBill_TransitionsTableToBilling_WithoutChangingOrderPaymentStatus));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var order = new Order
        {
            Id = 105,
            OrderCode = "HD-TEMP-001",
            TableId = table.Id,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            SubTotal = 100000,
            TotalAmount = 100000,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var posOrderService = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var result = await posOrderService.PrintTempBillAsync(order.Id);
        Assert.True(result);

        var reloadedTable = await db.Tables.FindAsync(table.Id);
        var reloadedOrder = await db.Orders.FindAsync(order.Id);

        Assert.NotNull(reloadedTable);
        Assert.Equal((int)TableStatus.Billing, reloadedTable.Status);

        Assert.NotNull(reloadedOrder);
        // CRITICAL: Payment status MUST remain Unpaid!
        Assert.Equal((int)PaymentStatus.Unpaid, reloadedOrder.PaymentStatus);
    }

    [Fact]
    public async Task Test7_MissingOptionalFields_HandledGracefully()
    {
        var db = GetDbContext(nameof(Test7_MissingOptionalFields_HandledGracefully));
        // Order with null Customer, null Table (Takeaway), null CustomerPhone
        var (cashier, _, shift, _, category) = SeedBaseData(db);
        cashier.FullName = string.Empty; // Test fallback to "Thu ngân"
        await db.SaveChangesAsync();

        var item = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Trà Đào", Price = 35000, IsActive = true };
        db.MenuItems.Add(item);

        var order = new Order
        {
            Id = 106,
            OrderCode = "HD-MINIMAL-001",
            TableId = null,
            ShiftId = shift.Id,
            CreatedByUserId = cashier.Id,
            CustomerName = null,
            CustomerPhone = null,
            SubTotal = 35000,
            TotalAmount = 35000,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };

        order.OrderItems.Add(new OrderItem { Id = 51, OrderId = order.Id, MenuItemId = item.Id, MenuItemName = item.Name, UnitPrice = 35000, Quantity = 1 });
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var billViewerService = new BillViewerService(db);
        var receipt = await billViewerService.GetBillK80ReceiptAsync(order.Id);

        Assert.NotNull(receipt);
        Assert.Equal("HD-MINIMAL-001", receipt.OrderCode);
        Assert.Equal("Khách lẻ", receipt.CustomerName);
        Assert.Equal(string.Empty, receipt.CustomerPhone);
        Assert.Equal("Thu ngân", receipt.CashierName);
        Assert.Equal("Takeaway", receipt.OrderType);
    }

    [Fact]
    public async Task Test8_PrintRetry_DoesNotDuplicateKitchenItems()
    {
        var db = GetDbContext(nameof(Test8_PrintRetry_DoesNotDuplicateKitchenItems));
        var (cashier, _, shift, table, category) = SeedBaseData(db);

        var pizza = new MenuItem { Id = 10, CategoryId = category.Id, Name = "Pizza Rau Củ", Price = 160000, IsActive = true };
        db.MenuItems.Add(pizza);
        await db.SaveChangesAsync();

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var posOrderService = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        // First Submission
        var submitReq = new SubmitPosOrderRequest
        {
            TableId = table.Id,
            CustomerNote = "Lần gửi 1",
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = pizza.Id, Quantity = 2 }
            }
        };

        var res1 = await posOrderService.SubmitPosOrderAsync(submitReq);
        Assert.True(res1.OrderId > 0);

        var orderAfterFirstSubmit = await db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == res1.OrderId);
        Assert.Single(orderAfterFirstSubmit.OrderItems);
        Assert.Equal(2, orderAfterFirstSubmit.OrderItems.First().Quantity);

        // Simulate Retry / Print again without adding new cart items
        // The order service print temp bill should only update table billing status without modifying items
        var printSuccess = await posOrderService.PrintTempBillAsync(res1.OrderId);
        Assert.True(printSuccess);

        var orderAfterPrint = await db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == res1.OrderId);
        Assert.Single(orderAfterPrint.OrderItems);
        Assert.Equal(2, orderAfterPrint.OrderItems.First().Quantity);
    }
}
