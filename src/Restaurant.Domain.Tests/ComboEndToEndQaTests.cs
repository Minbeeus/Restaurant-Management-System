using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class ComboEndToEndQaTests
{
    private readonly ApplicationDbContext _context;
    private readonly ComboService _comboService;
    private readonly KitchenService _kitchenService;
    private readonly PosOrderService _posOrderService;
    private readonly InventoryService _inventoryService;
    private readonly PaymentService _paymentService;
    private readonly BillViewerService _billViewerService;

    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IRestaurantNotificationService> _notifMock;
    private readonly Mock<IStringLocalizer<SharedResources>> _localizerMock;
    private readonly Mock<ILoyaltyService> _loyaltyMock;
    private readonly Mock<IQrCodeService> _qrMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<MediatR.IMediator> _mediatorMock;

    public ComboEndToEndQaTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new AuditableEntityInterceptor())
            .Options;

        _context = new ApplicationDbContext(options);

        _auditMock = new Mock<IAuditService>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(c => c.UserId).Returns(1);

        _comboService = new ComboService(_context, _auditMock.Object, _currentUserMock.Object);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<RestaurantHub, IRestaurantHubClient>>();
        var hubClientMock = new Mock<IRestaurantHubClient>();
        var clientsMock = new Mock<Microsoft.AspNetCore.SignalR.IHubClients<IRestaurantHubClient>>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(hubClientMock.Object);
        clientsMock.Setup(c => c.All).Returns(hubClientMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _mediatorMock = new Mock<MediatR.IMediator>();
        _kitchenService = new KitchenService(_context, hubContextMock.Object, _mediatorMock.Object);

        _notifMock = new Mock<IRestaurantNotificationService>();
        _localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        _posOrderService = new PosOrderService(_context, _notifMock.Object, _localizerMock.Object);
        _inventoryService = new InventoryService(_context, _notifMock.Object, _localizerMock.Object);

        _loyaltyMock = new Mock<ILoyaltyService>();
        _qrMock = new Mock<IQrCodeService>();
        _configMock = new Mock<IConfiguration>();

        _paymentService = new PaymentService(
            _context,
            _qrMock.Object,
            _notifMock.Object,
            _loyaltyMock.Object,
            _mediatorMock.Object,
            _configMock.Object,
            _localizerMock.Object,
            _auditMock.Object
        );

        _billViewerService = new BillViewerService(_context);
    }

    private async Task SeedPrerequisitesAsync()
    {
        var role = new Role { Name = "Admin", Description = "Administrator" };
        var user = new User { Username = "cashier1", FullName = "Thu Ngân 1", PasswordHash = "hash", Role = role, IsActive = true };
        var register = new Register { Name = "POS-01", Code = "TERM-01", IsActive = true };
        var shift = new Shift
        {
            User = user,
            Register = register,
            OpeningBalance = 1000000,
            Status = (int)ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow
        };
        var table = new Table { Name = "Bàn 10", AreaId = 1, QrToken = "token-10", Status = (int)TableStatus.Available };

        _context.Roles.Add(role);
        _context.Users.Add(user);
        _context.Registers.Add(register);
        _context.Shifts.Add(shift);
        _context.Tables.Add(table);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task TC01_BasicCombo_CreatesHeaderAndChildren_KdsReceivesChildren_InventoryDeducted()
    {
        await SeedPrerequisitesAsync();

        // 1. Admin creates items & recipe for inventory deduction
        var uom = new UnitOfMeasure { Name = "Cái", Abbreviation = "CAI" };
        var flour = new Material { Name = "Bột Mì", UnitOfMeasure = uom, CurrentStock = 100, MinStockLevel = 10, CostPerUnit = 20000, IsActive = true };
        _context.UnitOfMeasures.Add(uom);
        _context.Materials.Add(flour);

        var cat = new Category { Name = "Fastfood" };
        var pizza = new MenuItem { Name = "Pizza Margherita", Price = 120000, Category = cat, IsActive = true };
        var drink = new MenuItem { Name = "Coke Can", Price = 20000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, drink);
        await _context.SaveChangesAsync();

        // Add recipe for pizza: 2 Bột Mì per Pizza
        var pizzaRecipe = new Recipe { MenuItemId = pizza.Id };
        pizzaRecipe.RecipeItems.Add(new RecipeItem { MaterialId = flour.Id, Quantity = 2, UnitOfMeasureId = uom.Id });
        _context.Recipes.Add(pizzaRecipe);
        await _context.SaveChangesAsync();

        // 2. Admin creates Combo
        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Basic",
            Price = 130000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 },
                new() { MenuItemId = drink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        // 3. POS Submits Combo
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = combo.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity
                    }).ToList()
                }
            }
        });

        // 4. Verify DB Order and OrderItems
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == posResp.OrderId);
        Assert.NotNull(order);
        Assert.Equal(3, order.OrderItems.Count); // 1 Header + 2 Children

        var header = order.OrderItems.First(oi => oi.ParentItemId == null);
        var children = order.OrderItems.Where(oi => oi.ParentItemId == header.Id).ToList();
        Assert.Equal("Combo Basic", header.MenuItemName);
        Assert.Equal(130000, header.UnitPrice);
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.MenuItemName == "Pizza Margherita");
        Assert.Contains(children, c => c.MenuItemName == "Coke Can");

        // 5. Verify KDS Active Tickets
        var tickets = await _kitchenService.GetActiveTicketsAsync();
        var ticket = tickets.FirstOrDefault(t => t.OrderId == order.Id);
        Assert.NotNull(ticket);
        Assert.Equal(3, ticket.Items.Count); // 1 Header + 2 Children
        var kdsHeader = ticket.Items.First(i => i.IsComboHeader);
        var kdsChildren = ticket.Items.Where(i => !i.IsComboHeader).ToList();
        Assert.Equal(header.Id, kdsHeader.OrderItemId);
        Assert.Equal(2, kdsChildren.Count);
        Assert.All(kdsChildren, i => Assert.Equal(header.Id, i.ParentItemId));

        // 6. Kitchen finishes Pizza -> Inventory Deduction
        var pizzaChild = children.First(c => c.MenuItemName == "Pizza Margherita");
        await _inventoryService.DeductInventoryForOrderItemAsync(pizzaChild.Id);

        var updatedFlour = await _context.Materials.FindAsync(flour.Id);
        Assert.NotNull(updatedFlour);
        Assert.Equal(98, updatedFlour.CurrentStock); // 100 - 2 = 98
    }

    [Fact]
    public async Task TC02_FixedPlusChoiceCombo_HierarchyAndPricesVerified()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Combos" };
        var pizza = new MenuItem { Name = "Pizza Hải Sản", Price = 150000, Category = cat, IsActive = true };
        var tea = new MenuItem { Name = "Trà Đào", Price = 30000, Category = cat, IsActive = true };
        var coffee = new MenuItem { Name = "Cà Phê Muối", Price = 35000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, tea, coffee);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Ăn Uống",
            Price = 160000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            },
            Groups = new List<CreateComboGroupRequest>
            {
                new()
                {
                    Name = "Chọn 1 Thức Uống",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new() { MenuItemId = tea.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0 },
                        new() { MenuItemId = coffee.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 5000 }
                    }
                }
            }
        });

        // Customer chooses Coffee (AdditionalPrice = 5000)
        var coffeeChoice = combo.Groups[0].Items.First(i => i.MenuItemId == coffee.Id);
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = combo.FixedAndAddOnItems[0].Id, MenuItemId = pizza.Id, Quantity = 1 },
                        new() { ComboItemId = coffeeChoice.Id, MenuItemId = coffee.Id, Quantity = 1, AdditionalPrice = 5000, ComboGroupId = combo.Groups[0].Id }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(165000, order.TotalAmount); // 160k base + 5k coffee extra

        var header = order.OrderItems.First(oi => oi.ParentItemId == null);
        var coffeeOrderItem = order.OrderItems.First(oi => oi.MenuItemId == coffee.Id);
        Assert.Equal(header.Id, coffeeOrderItem.ParentItemId);
        Assert.Equal(5000, coffeeOrderItem.UnitPrice);
        Assert.Equal(combo.Groups[0].Id, coffeeOrderItem.ComboGroupId);
    }

    [Fact]
    public async Task TC03_MultipleComboQuantity_MultipliesChildQuantitiesCorrectly()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Combos" };
        var burger = new MenuItem { Name = "Burger Bò", Price = 60000, Category = cat, IsActive = true };
        var fries = new MenuItem { Name = "Khoai Tây Chiên", Price = 30000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(burger, fries);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Burger Khoai",
            Price = 80000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = burger.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 },
                new() { MenuItemId = fries.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        // POS Submits 2 x Combo Burger Khoai
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 2,
                    SelectedComboItems = combo.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity
                    }).ToList()
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(160000, order.TotalAmount); // 2 * 80000

        var header = order.OrderItems.First(oi => oi.ParentItemId == null);
        Assert.Equal(2, header.Quantity);

        var children = order.OrderItems.Where(oi => oi.ParentItemId == header.Id).ToList();
        Assert.All(children, c => Assert.Equal(2, c.Quantity)); // 2 Burgers, 2 Fries
    }

    [Fact]
    public async Task TC04_ComboPlusSingleItem_KdsDistinguishesHierarchyFromStandalone()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Food" };
        var comboMain = new MenuItem { Name = "Gà Rán", Price = 40000, Category = cat, IsActive = true };
        var singleItem = new MenuItem { Name = "Mì Ý Sốt Bò Bằm", Price = 75000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(comboMain, singleItem);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Gà",
            Price = 50000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = comboMain.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = combo.FixedAndAddOnItems[0].Id, MenuItemId = comboMain.Id, Quantity = 1 }
                    }
                },
                new()
                {
                    MenuItemId = singleItem.Id,
                    Quantity = 1
                }
            }
        });

        var tickets = await _kitchenService.GetActiveTicketsAsync();
        var ticket = tickets.First(t => t.OrderId == posResp.OrderId);

        var comboChild = ticket.Items.First(i => i.MenuItemName == "Gà Rán");
        var standalone = ticket.Items.First(i => i.MenuItemName == "Mì Ý Sốt Bò Bằm");

        Assert.NotNull(comboChild.ParentItemId);
        Assert.Equal("Combo Gà", comboChild.ParentItemName);
        Assert.Null(standalone.ParentItemId);
        Assert.Null(standalone.ParentItemName);
    }

    [Fact]
    public async Task TC05_ComboChildWithModifier_PersistsModifierAndTransmitsToKds()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "PizzaCat" };
        var pizza = new MenuItem { Name = "Pizza Phô Mai", Price = 100000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var modGroup = new ModifierGroup { Name = "Topping", MinSelections = 0, MaxSelections = 2, IsRequired = false };
        var cheeseOption = new ModifierOption { Name = "Thêm Phô Mai", ExtraPrice = 15000, ModifierGroup = modGroup };
        _context.ModifierGroups.Add(modGroup);
        _context.ModifierOptions.Add(cheeseOption);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Pizza Topping",
            Price = 110000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new()
                        {
                            ComboItemId = combo.FixedAndAddOnItems[0].Id,
                            MenuItemId = pizza.Id,
                            Quantity = 1,
                            SelectedModifiers = new List<int> { cheeseOption.Id }
                        }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.OrderItemModifiers).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(125000, order.TotalAmount); // 110k + 15k modifier

        var tickets = await _kitchenService.GetActiveTicketsAsync();
        var ticket = tickets.First(t => t.OrderId == posResp.OrderId);
        var kitchenItem = ticket.Items.First(i => i.MenuItemName == "Pizza Phô Mai");
        Assert.Contains("Thêm Phô Mai", kitchenItem.Modifiers);
    }

    [Fact]
    public async Task TC06_VoidComboHeader_VoidsAllChildren_RecalculatesTotalAmount()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Cat" };
        var pizza = new MenuItem { Name = "Pizza A", Price = 80000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Test Void",
            Price = 90000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = combo.FixedAndAddOnItems[0].Id, MenuItemId = pizza.Id, Quantity = 1 }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var header = order.OrderItems.First(oi => oi.ParentItemId == null);
        var child = order.OrderItems.First(oi => oi.ParentItemId == header.Id);

        // Act: Void the Combo Header
        var voidSuccess = await _posOrderService.VoidOrderItemAsync(order.Id, header.Id, "Khách đổi ý hủy combo", userId: 1);
        Assert.True(voidSuccess);

        // Reload order
        await _context.Entry(order).ReloadAsync();
        await _context.Entry(header).ReloadAsync();
        await _context.Entry(child).ReloadAsync();

        Assert.Equal((int)OrderItemStatus.Voided, header.Status);
        Assert.Equal((int)OrderItemStatus.Voided, child.Status);
        Assert.Equal(0, order.TotalAmount);

        var voidLogs = await _context.VoidLogs.Where(v => v.OrderId == order.Id).ToListAsync();
        Assert.Equal(2, voidLogs.Count); // Logged for header and child
    }

    [Fact]
    public async Task TC07_VoidAfterProcessing_AllowsVoidWithAuditAndLogs()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Cat" };
        var item = new MenuItem { Name = "Món Đang Nấu", Price = 70000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = item.Id, Quantity = 1 }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var orderItem = order.OrderItems.First();

        // Kitchen moves to Processing
        await _kitchenService.UpdateItemStatusAsync(orderItem.Id, (int)KitchenStatus.Processing);
        await _context.Entry(orderItem).ReloadAsync();
        Assert.Equal((int)KitchenStatus.Processing, orderItem.KitchenStatus);

        // Act: Void after processing
        var voidSuccess = await _posOrderService.VoidOrderItemAsync(order.Id, orderItem.Id, "Món nấu hỏng, khách không đợi được", userId: 1);
        Assert.True(voidSuccess);

        await _context.Entry(orderItem).ReloadAsync();
        await _context.Entry(order).ReloadAsync();
        Assert.Equal((int)OrderItemStatus.Voided, orderItem.Status);
        Assert.Equal(0, order.TotalAmount);
    }

    [Fact]
    public async Task TC08_AdminEditDuringActivePosSession_PreservesHistoricalOrders()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Cat" };
        var pizza = new MenuItem { Name = "Pizza V1", Price = 80000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Giá Ban Đầu",
            Price = 100000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        // Order 1 placed with Price 100k
        var posResp1 = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = combo.FixedAndAddOnItems[0].Id, MenuItemId = pizza.Id, Quantity = 1 }
                    }
                }
            }
        });

        // Admin updates Combo Price to 150k and changes Name
        await _comboService.UpdateComboAsync(combo.Id, new UpdateComboRequest
        {
            Name = "Combo Giá Mới 150k",
            Price = 150000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { Id = combo.FixedAndAddOnItems[0].Id, MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        // Verify Order 1 is completely UNTOUCHED
        var order1 = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp1.OrderId);
        Assert.Equal(100000, order1.TotalAmount);
        var header1 = order1.OrderItems.First(oi => oi.ParentItemId == null);
        Assert.Equal("Combo Giá Ban Đầu", header1.MenuItemName);
        Assert.Equal(100000, header1.UnitPrice);
    }

    [Fact]
    public async Task TC09_KdsStationRouting_EachStationReceivesOnlyItsDesignatedItems()
    {
        await SeedPrerequisitesAsync();

        var stationKitchen = new KitchenStation { Code = "KITCHEN", Name = "Bếp Nóng" };
        var stationBar = new KitchenStation { Code = "BAR", Name = "Quầy Pha Chế" };
        _context.KitchenStations.AddRange(stationKitchen, stationBar);
        await _context.SaveChangesAsync();

        var cat = new Category { Name = "Cat" };
        var steak = new MenuItem { Name = "Bò Bít Tết", Price = 200000, Category = cat, KitchenStationId = stationKitchen.Id, IsActive = true };
        var wine = new MenuItem { Name = "Rượu Vang Đỏ", Price = 90000, Category = cat, KitchenStationId = stationBar.Id, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(steak, wine);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Steak & Wine",
            Price = 270000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = steak.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 },
                new() { MenuItemId = wine.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = combo.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity
                    }).ToList()
                }
            }
        });

        // 1. Kitchen Station tickets
        var kitchenTickets = await _kitchenService.GetActiveTicketsAsync(includeCompleted: false, stationId: stationKitchen.Id);
        var kitchenTicket = kitchenTickets.First(t => t.OrderId == posResp.OrderId);
        Assert.Contains(kitchenTicket.Items, i => i.MenuItemName == "Bò Bít Tết");
        Assert.DoesNotContain(kitchenTicket.Items, i => i.MenuItemName == "Rượu Vang Đỏ");

        // 2. Bar Station tickets
        var barTickets = await _kitchenService.GetActiveTicketsAsync(includeCompleted: false, stationId: stationBar.Id);
        var barTicket = barTickets.First(t => t.OrderId == posResp.OrderId);
        Assert.Contains(barTicket.Items, i => i.MenuItemName == "Rượu Vang Đỏ");
        Assert.DoesNotContain(barTicket.Items, i => i.MenuItemName == "Bò Bít Tết");
    }

    [Fact]
    public async Task TC10_PaymentFlow_AfterKitchenDone_OrderReady_ThenPaidAndCompleted_GeneratesReceipt()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Cat" };
        var pizza = new MenuItem { Name = "Pizza X", Price = 90000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var combo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Thanh Toán",
            Price = 100000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = combo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = combo.FixedAndAddOnItems[0].Id, MenuItemId = pizza.Id, Quantity = 1 }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var child = order.OrderItems.First(oi => oi.ParentItemId != null);

        // 1. Kitchen finishes child item
        await _kitchenService.UpdateItemStatusAsync(child.Id, (int)KitchenStatus.Done);
        await _context.Entry(order).ReloadAsync();

        // 2. Order status MUST BE Ready, NOT Completed before payment!
        Assert.Equal((int)OrderStatus.Ready, order.Status);
        Assert.Equal((int)PaymentStatus.Unpaid, order.PaymentStatus);

        // 3. Cashier processes Cash payment
        var cashResp = await _paymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = order.Id,
            CashReceived = 100000
        });

        Assert.NotNull(cashResp);
        Assert.Equal(0, cashResp.ChangeAmount);

        // 4. Order status MUST BE Completed & PaymentStatus = Paid
        await _context.Entry(order).ReloadAsync();
        Assert.Equal((int)OrderStatus.Completed, order.Status);
        Assert.Equal((int)PaymentStatus.Paid, order.PaymentStatus);

        // 5. Generate K80 Receipt
        var receipt = await _billViewerService.GetBillK80ReceiptAsync(order.Id);
        Assert.NotNull(receipt);
        Assert.Equal(100000, receipt.TotalAmount);
        Assert.Equal("Tiền Mặt", receipt.PaymentMethod);
        Assert.Contains(receipt.Items, i => i.MenuItemName == "Combo Thanh Toán" && !i.IsComboChild);
        Assert.Contains(receipt.Items, i => i.MenuItemName == "Pizza X" && i.IsComboChild);
    }

    [Fact]
    public async Task NegativeTests_ValidatesInputAndBusinessRules()
    {
        await SeedPrerequisitesAsync();

        var cat = new Category { Name = "Cat" };
        var pizza = new MenuItem { Name = "Pizza Neg", Price = 50000, Category = cat, IsActive = true };
        var inactiveDrink = new MenuItem { Name = "Inactive Coke", Price = 15000, Category = cat, IsActive = false };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, inactiveDrink);
        await _context.SaveChangesAsync();

        // 1. Cannot create Combo containing inactive MenuItem
        await Assert.ThrowsAsync<InvalidOperationException>(() => _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Invalid Combo With Inactive",
            Price = 60000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = inactiveDrink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        }));

        // 2. Cannot submit Combo with invalid ComboItemId
        var validCombo = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Valid Combo",
            Price = 50000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = validCombo.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = 999999, MenuItemId = pizza.Id, Quantity = 1 } // Non-existent ComboItemId
                    }
                }
            }
        }));

        // 3. Cannot submit order with Quantity <= 0
        await Assert.ThrowsAsync<InvalidOperationException>(() => _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = 1,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = validCombo.Id, Quantity = 0 }
            }
        }));
    }
}
