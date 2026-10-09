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

public class PosOrderServiceTests
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

    private void SeedData(ApplicationDbContext db)
    {
        var user = new User { Id = 1, Username = "admin", FullName = "Admin User", RoleId = 1 };
        var register = new Register { Id = 1, Code = "REG01", Name = "Main Register", IsActive = true };
        var shift = new Shift { Id = 1, UserId = 1, RegisterId = 1, Status = (int)ShiftStatus.Open, OpeningBalance = 1000000m, OpenedAt = DateTime.UtcNow };
        var table = new Table { Id = 1, Name = "Bàn 01", Status = (int)TableStatus.Available };
        var category = new Category { Id = 1, Name = "Món chính", SortOrder = 1, IsActive = true };

        // Single Items
        var pizza = new MenuItem { Id = 10, CategoryId = 1, Name = "Pizza Margherita", Price = 150000, IsActive = true, SortOrder = 1 };
        var coke = new MenuItem { Id = 20, CategoryId = 1, Name = "Coca Cola", Price = 25000, IsActive = true, SortOrder = 2 };
        var fries = new MenuItem { Id = 30, CategoryId = 1, Name = "French Fries", Price = 40000, IsActive = true, SortOrder = 3 };

        // Combo Family (#100)
        var comboFamily = new MenuItem { Id = 100, CategoryId = 1, Name = "Combo Family", Price = 199000, IsCombo = true, IsActive = true, SortOrder = 4 };
        var groupPizza = new ComboGroup { Id = 1, ComboId = 100, Name = "Chọn Pizza", MinSelections = 1, MaxSelections = 1, IsRequired = true };
        var groupDrink = new ComboGroup { Id = 2, ComboId = 100, Name = "Chọn Thức Uống", MinSelections = 1, MaxSelections = 1, IsRequired = true };
        var ciPizza = new ComboItem { Id = 101, ComboId = 100, ComboGroupId = 1, MenuItemId = 10, ComponentType = (int)ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, IsActive = true };
        var ciCoke = new ComboItem { Id = 102, ComboId = 100, ComboGroupId = 2, MenuItemId = 20, ComponentType = (int)ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, IsActive = true };

        // Combo Solo (#200)
        var comboSolo = new MenuItem { Id = 200, CategoryId = 1, Name = "Combo Solo", Price = 89000, IsCombo = true, IsActive = true, SortOrder = 5 };
        var ciSoloFries = new ComboItem { Id = 201, ComboId = 200, ComboGroupId = null, MenuItemId = 30, ComponentType = (int)ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 5000, IsActive = true };
        var ciSoloCoke = new ComboItem { Id = 202, ComboId = 200, ComboGroupId = null, MenuItemId = 20, ComponentType = (int)ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0, IsActive = true };

        db.Users.Add(user);
        db.Registers.Add(register);
        db.Shifts.Add(shift);
        db.Tables.Add(table);
        db.Categories.Add(category);
        db.MenuItems.AddRange(pizza, coke, fries, comboFamily, comboSolo);
        db.ComboGroups.AddRange(groupPizza, groupDrink);
        db.ComboItems.AddRange(ciPizza, ciCoke, ciSoloFries, ciSoloCoke);
        db.SaveChanges();
    }

    [Fact]
    public async Task Test1_SubmitPosOrder_SingleCombo_CreatesParentAndChildOrderItemsWithCorrectHierarchy()
    {
        var db = GetDbContext(nameof(Test1_SubmitPosOrder_SingleCombo_CreatesParentAndChildOrderItemsWithCorrectHierarchy));
        SeedData(db);

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var service = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var request = new SubmitPosOrderRequest
        {
            TableId = 1,
            CustomerNote = "Đơn 1 combo",
            Items = new List<CartItemValidateRequest>
            {
                new CartItemValidateRequest
                {
                    MenuItemId = 100, // Combo Family
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest { ComboItemId = 101, MenuItemId = 10, Quantity = 1, AdditionalPrice = 0, ComboGroupId = 1 },
                        new CartComboItemRequest { ComboItemId = 102, MenuItemId = 20, Quantity = 1, AdditionalPrice = 0, ComboGroupId = 2 }
                    }
                }
            }
        };

        var response = await service.SubmitPosOrderAsync(request);

        Assert.True(response.OrderId > 0);
        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == response.OrderId);
        Assert.NotNull(order);

        // Expect exactly 3 OrderItems: 1 Combo Header + 2 Child items
        Assert.Equal(3, order.OrderItems.Count);

        var comboHeader = order.OrderItems.FirstOrDefault(oi => oi.MenuItemId == 100);
        Assert.NotNull(comboHeader);
        Assert.Null(comboHeader.ParentItemId);
        Assert.Equal(199000, comboHeader.UnitPrice);
        Assert.Equal(1, comboHeader.Quantity);
        Assert.Equal((int)KitchenStatus.Pending, comboHeader.KitchenStatus);

        var children = order.OrderItems.Where(oi => oi.ParentItemId == comboHeader.Id).ToList();
        Assert.Equal(2, children.Count);

        var pizzaChild = children.FirstOrDefault(c => c.MenuItemId == 10);
        Assert.NotNull(pizzaChild);
        Assert.Equal("Pizza Margherita", pizzaChild.MenuItemName);
        Assert.Equal(1, pizzaChild.Quantity);
        Assert.Equal(comboHeader.Id, pizzaChild.ParentItemId);
        Assert.Equal(1, pizzaChild.ComboGroupId);
        Assert.Equal((int)KitchenStatus.Pending, pizzaChild.KitchenStatus);

        var cokeChild = children.FirstOrDefault(c => c.MenuItemId == 20);
        Assert.NotNull(cokeChild);
        Assert.Equal("Coca Cola", cokeChild.MenuItemName);
        Assert.Equal(1, cokeChild.Quantity);
        Assert.Equal(comboHeader.Id, cokeChild.ParentItemId);
        Assert.Equal(2, cokeChild.ComboGroupId);
        Assert.Equal((int)KitchenStatus.Pending, cokeChild.KitchenStatus);

        // Verify kitchen notification received the 2 children, NOT the combo header
        notificationMock.Verify(n => n.NotifyKitchenTicketAsync(It.Is<KitchenTicketDto>(t => 
            t.Items.Count == 2 && t.Items.All(i => i.MenuItemId != 100))), Times.Once);
    }

    [Fact]
    public async Task Test2_SubmitPosOrder_ComboWithQuantityTwo_MultipliesChildQuantity()
    {
        var db = GetDbContext(nameof(Test2_SubmitPosOrder_ComboWithQuantityTwo_MultipliesChildQuantity));
        SeedData(db);

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var service = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var request = new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new CartItemValidateRequest
                {
                    MenuItemId = 100, // Combo Family
                    Quantity = 2,     // Quantity = 2
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest { ComboItemId = 101, MenuItemId = 10, Quantity = 1, AdditionalPrice = 0 },
                        new CartComboItemRequest { ComboItemId = 102, MenuItemId = 20, Quantity = 1, AdditionalPrice = 0 }
                    }
                }
            }
        };

        var response = await service.SubmitPosOrderAsync(request);

        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == response.OrderId);
        Assert.NotNull(order);

        var comboHeader = order.OrderItems.First(oi => oi.MenuItemId == 100);
        Assert.Equal(2, comboHeader.Quantity);

        var children = order.OrderItems.Where(oi => oi.ParentItemId == comboHeader.Id).ToList();
        Assert.Equal(2, children.Count);

        // Each child must have quantity = 1 * 2 = 2
        Assert.All(children, c => Assert.Equal(2, c.Quantity));
    }

    [Fact]
    public async Task Test3_SubmitPosOrder_ComboAndSingleItem_MaintainsCorrectHierarchies()
    {
        var db = GetDbContext(nameof(Test3_SubmitPosOrder_ComboAndSingleItem_MaintainsCorrectHierarchies));
        SeedData(db);

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var service = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var request = new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                // Combo item
                new CartItemValidateRequest
                {
                    MenuItemId = 100,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest { ComboItemId = 101, MenuItemId = 10, Quantity = 1 },
                        new CartComboItemRequest { ComboItemId = 102, MenuItemId = 20, Quantity = 1 }
                    }
                },
                // Separate single item
                new CartItemValidateRequest
                {
                    MenuItemId = 30, // French Fries lẻ
                    Quantity = 1
                }
            }
        };

        var response = await service.SubmitPosOrderAsync(request);

        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(4, order.OrderItems.Count);

        var comboHeader = order.OrderItems.First(oi => oi.MenuItemId == 100);
        var comboChildren = order.OrderItems.Where(oi => oi.ParentItemId == comboHeader.Id).ToList();
        var singleItem = order.OrderItems.First(oi => oi.MenuItemId == 30);

        Assert.Null(comboHeader.ParentItemId);
        Assert.Equal(2, comboChildren.Count);
        Assert.Null(singleItem.ParentItemId); // Single item has no parent

        // Both children and single item must be sent to kitchen (3 cookable items)
        notificationMock.Verify(n => n.NotifyKitchenTicketAsync(It.Is<KitchenTicketDto>(t => 
            t.Items.Count == 3 && t.Items.Any(i => i.MenuItemId == 10) && t.Items.Any(i => i.MenuItemId == 20) && t.Items.Any(i => i.MenuItemId == 30))), Times.Once);
    }

    [Fact]
    public async Task Test4_SubmitPosOrder_TwoDifferentCombos_CreatesDistinctParentAndChildren()
    {
        var db = GetDbContext(nameof(Test4_SubmitPosOrder_TwoDifferentCombos_CreatesDistinctParentAndChildren));
        SeedData(db);

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var service = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var request = new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                // Combo #1: Combo Family
                new CartItemValidateRequest
                {
                    MenuItemId = 100,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest { ComboItemId = 101, MenuItemId = 10, Quantity = 1 },
                        new CartComboItemRequest { ComboItemId = 102, MenuItemId = 20, Quantity = 1 }
                    }
                },
                // Combo #2: Combo Solo
                new CartItemValidateRequest
                {
                    MenuItemId = 200,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest { ComboItemId = 201, MenuItemId = 30, Quantity = 1 }
                    }
                }
            }
        };

        var response = await service.SubmitPosOrderAsync(request);

        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(5, order.OrderItems.Count);

        var familyHeader = order.OrderItems.First(oi => oi.MenuItemId == 100);
        var soloHeader = order.OrderItems.First(oi => oi.MenuItemId == 200);

        Assert.Null(familyHeader.ParentItemId);
        Assert.Null(soloHeader.ParentItemId);

        var familyChildren = order.OrderItems.Where(oi => oi.ParentItemId == familyHeader.Id).ToList();
        var soloChild = order.OrderItems.First(oi => oi.ParentItemId == soloHeader.Id);

        Assert.Equal(2, familyChildren.Count);
        Assert.Contains(familyChildren, c => c.MenuItemId == 10);
        Assert.Contains(familyChildren, c => c.MenuItemId == 20);
        Assert.Equal(30, soloChild.MenuItemId);
    }

    [Fact]
    public async Task Test5_SubmitPosOrder_InvalidComboItemId_ThrowsAndRollsBack()
    {
        var db = GetDbContext(nameof(Test5_SubmitPosOrder_InvalidComboItemId_ThrowsAndRollsBack));
        SeedData(db);

        var notificationMock = new Mock<IRestaurantNotificationService>();
        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        var service = new PosOrderService(db, notificationMock.Object, localizerMock.Object);

        var request = new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new CartItemValidateRequest
                {
                    MenuItemId = 100, // Combo Family
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        // 201 belongs to Combo Solo (200), NOT Combo Family (100)!
                        new CartComboItemRequest { ComboItemId = 201, MenuItemId = 30, Quantity = 1 }
                    }
                }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitPosOrderAsync(request));
        Assert.Contains("không tồn tại hoặc không thuộc Combo", ex.Message);

        // Verify rollback: no order created
        var ordersCount = await db.Orders.CountAsync();
        Assert.Equal(0, ordersCount);
    }
}

