using Microsoft.EntityFrameworkCore;
using Moq;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class ComboBackendIntegrityTests
{
    private readonly ApplicationDbContext _context;
    private readonly ComboService _comboService;
    private readonly KitchenService _kitchenService;
    private readonly PosOrderService _posOrderService;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IRestaurantNotificationService> _notifMock;
    private readonly Mock<Microsoft.Extensions.Localization.IStringLocalizer<Restaurant.Application.Resources.SharedResources>> _localizerMock;

    public ComboBackendIntegrityTests()
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

        var mediatorMock = new Mock<MediatR.IMediator>();
        _kitchenService = new KitchenService(_context, hubContextMock.Object, mediatorMock.Object);

        _notifMock = new Mock<IRestaurantNotificationService>();
        _localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Restaurant.Application.Resources.SharedResources>>();

        _posOrderService = new PosOrderService(_context, _notifMock.Object, _localizerMock.Object);
    }

    [Fact]
    public async Task CreateCombo_ValidRequest_PersistsSuccessfully()
    {
        // Arrange
        var cat = new Category { Name = "Combos" };
        var burger = new MenuItem { Name = "Burger", Price = 50000, IsActive = true };
        var coke = new MenuItem { Name = "Coke", Price = 20000, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(burger, coke);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Burger Combo",
            Price = 60000,
            Groups = new List<CreateComboGroupRequest>
            {
                new CreateComboGroupRequest
                {
                    Name = "Chọn nước",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest
                        {
                            MenuItemId = coke.Id,
                            ComponentType = ComboComponentType.Choice,
                            DefaultQuantity = 1,
                            AdditionalPrice = 0
                        }
                    }
                }
            },
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest
                {
                    MenuItemId = burger.Id,
                    ComponentType = ComboComponentType.Fixed,
                    DefaultQuantity = 1,
                    AdditionalPrice = 0
                }
            }
        };

        // Act
        var created = await _comboService.CreateComboAsync(request);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Burger Combo", created.Name);
        Assert.Single(created.Groups);
        Assert.Single(created.FixedAndAddOnItems);

        var dbCombo = await _context.MenuItems
            .Include(m => m.ComboGroups).ThenInclude(g => g.ComboItems)
            .Include(m => m.ComboItems)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        Assert.NotNull(dbCombo);
        Assert.True(dbCombo.IsCombo);
    }

    [Fact]
    public async Task CreateCombo_WhenUserIsAnonymousOrZero_DefaultsSafelyAndCreatesSuccessfully()
    {
        // Setup current user as null/anonymous
        _currentUserMock.Setup(c => c.UserId).Returns((int?)null);

        var cat = new Category { Name = "Combos" };
        var pizza = new MenuItem { Name = "Pizza Margherita", Price = 180000, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Family Combo",
            Price = 200000,
            Groups = new List<CreateComboGroupRequest>
            {
                new CreateComboGroupRequest
                {
                    Name = "Chọn 1 Pizza",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest
                        {
                            MenuItemId = pizza.Id,
                            ComponentType = ComboComponentType.Choice,
                            DefaultQuantity = 1,
                            AdditionalPrice = 0
                        }
                    }
                }
            },
            FixedAndAddOnItems = new List<CreateComboItemRequest>()
        };

        var created = await _comboService.CreateComboAsync(request);

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        _auditMock.Verify(a => a.LogAsync(1, "CreateCombo", "MenuItem", created.Id.ToString(), It.IsAny<string>(), null, null, null), Times.Once);
    }

    [Fact]
    public async Task CreateCombo_WithOnlyChoiceGroupsAndZeroFixedItems_CreatesSuccessfully()
    {
        var cat = new Category { Name = "Combo Ưu đãi" };
        var pizza1 = new MenuItem { Name = "Pizza Margherita", Price = 180000, IsActive = true };
        var pizza2 = new MenuItem { Name = "Pizza Pepperoni", Price = 210000, IsActive = true };
        var pasta = new MenuItem { Name = "Mì Ý Bolognese", Price = 145000, IsActive = true };
        var salad = new MenuItem { Name = "Salad Caesar", Price = 115000, IsActive = true };

        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza1, pizza2, pasta, salad);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Family Combo Meal",
            NameEn = "Family Combo",
            Description = "Gồm 1 pizza và 1 mỳ ý/1 salad",
            Price = 200000,
            Groups = new List<CreateComboGroupRequest>
            {
                new CreateComboGroupRequest
                {
                    Name = "Chọn 1 Pizza",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest { MenuItemId = pizza1.Id, ComponentType = ComboComponentType.Choice },
                        new CreateComboItemRequest { MenuItemId = pizza2.Id, ComponentType = ComboComponentType.Choice }
                    }
                },
                new CreateComboGroupRequest
                {
                    Name = "Chọn 1 mỳ ý",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest { MenuItemId = pasta.Id, ComponentType = ComboComponentType.Choice },
                        new CreateComboItemRequest { MenuItemId = salad.Id, ComponentType = ComboComponentType.Choice }
                    }
                }
            },
            FixedAndAddOnItems = new List<CreateComboItemRequest>()
        };

        var created = await _comboService.CreateComboAsync(request);

        Assert.NotNull(created);
        Assert.Equal(2, created.Groups.Count);
        Assert.Empty(created.FixedAndAddOnItems);
        Assert.Equal(200000, created.Price);
    }

    [Fact]
    public async Task CreateCombo_EmptyStructure_ThrowsValidationException()
    {
        var request = new CreateComboRequest
        {
            CategoryId = 1,
            Name = "Empty Combo",
            Price = 50000,
            Groups = new(),
            FixedAndAddOnItems = new()
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _comboService.CreateComboAsync(request));
        Assert.Contains("Combo không được rỗng", ex.Message);
    }

    [Fact]
    public async Task CreateCombo_NestedCombo_ThrowsValidationException()
    {
        var cat = new Category { Name = "Combos" };
        var innerCombo = new MenuItem { Name = "Inner Combo", Price = 50000, IsActive = true, IsCombo = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(innerCombo);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Outer Combo",
            Price = 90000,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest { MenuItemId = innerCombo.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _comboService.CreateComboAsync(request));
        Assert.Contains("Không thể lồng Combo", ex.Message);
    }

    [Fact]
    public async Task UpdateCombo_PreservesExistingComboItemIds_DoesNotCorruptHistory()
    {
        // Arrange
        var cat = new Category { Name = "Combos" };
        var pizza = new MenuItem { Name = "Pizza Slice", Price = 40000, IsActive = true };
        var tea = new MenuItem { Name = "Iced Tea", Price = 15000, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, tea);
        await _context.SaveChangesAsync();

        var created = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Lunch Combo",
            Price = 50000,
            Groups = new List<CreateComboGroupRequest>
            {
                new CreateComboGroupRequest
                {
                    Name = "Đồ uống",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest { MenuItemId = tea.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1 }
                    }
                }
            },
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        var originalGroupId = created.Groups[0].Id;
        var originalTeaItemId = created.Groups[0].Items[0].Id;
        var originalPizzaItemId = created.FixedAndAddOnItems[0].Id;

        // Act: Update price and group name, passing existing IDs
        var updateRequest = new UpdateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Lunch Combo Updated",
            Price = 55000,
            Groups = new List<CreateComboGroupRequest>
            {
                new CreateComboGroupRequest
                {
                    Id = originalGroupId,
                    Name = "Đồ uống đổi tên",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    Items = new List<CreateComboItemRequest>
                    {
                        new CreateComboItemRequest
                        {
                            Id = originalTeaItemId,
                            MenuItemId = tea.Id,
                            ComponentType = ComboComponentType.Choice,
                            DefaultQuantity = 1,
                            AdditionalPrice = 2000 // changed additional price
                        }
                    }
                }
            },
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest
                {
                    Id = originalPizzaItemId,
                    MenuItemId = pizza.Id,
                    ComponentType = ComboComponentType.Fixed,
                    DefaultQuantity = 1
                }
            }
        };

        var updated = await _comboService.UpdateComboAsync(created.Id, updateRequest);

        // Assert: IDs remain stable (In-place update, not dropped and recreated)
        Assert.NotNull(updated);
        Assert.Equal("Lunch Combo Updated", updated.Name);
        Assert.Equal(55000, updated.Price);
        Assert.Equal(originalGroupId, updated.Groups[0].Id);
        Assert.Equal(originalTeaItemId, updated.Groups[0].Items[0].Id);
        Assert.Equal(2000, updated.Groups[0].Items[0].AdditionalPrice);
        Assert.Equal(originalPizzaItemId, updated.FixedAndAddOnItems[0].Id);
    }

    [Fact]
    public async Task UpdateCombo_DoesNotAffectExistingSubmittedOrders()
    {
        // Arrange
        var user = new User { Id = 1, Username = "cashier1", FullName = "Cashier One", RoleId = 1 };
        var register = new Register { Id = 1, Code = "REG01", Name = "Register 1", IsActive = true };
        var shift = new Shift { Id = 1, UserId = 1, RegisterId = 1, Status = (int)ShiftStatus.Open, OpeningBalance = 500000m, OpenedAt = DateTime.UtcNow };
        var cat = new Category { Name = "Combos" };
        var chicken = new MenuItem { Name = "Gà Rán", Price = 35000, IsActive = true };
        _context.Users.Add(user);
        _context.Registers.Add(register);
        _context.Shifts.Add(shift);
        _context.Categories.Add(cat);
        _context.MenuItems.Add(chicken);
        await _context.SaveChangesAsync();

        var created = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Combo Đơn 1",
            Price = 40000,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest { MenuItemId = chicken.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1 }
            }
        });

        // Submit POS Order for this combo
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new CartItemValidateRequest
                {
                    MenuItemId = created.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new CartComboItemRequest
                        {
                            ComboItemId = created.FixedAndAddOnItems[0].Id,
                            MenuItemId = chicken.Id,
                            Quantity = 1
                        }
                    }
                }
            }
        });

        // Verify Order snapshot
        var initialOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == posResp.OrderId);
        Assert.NotNull(initialOrder);
        var initialHeader = initialOrder.OrderItems.First(oi => oi.ParentItemId == null);
        Assert.Equal(40000, initialHeader.UnitPrice);
        Assert.Equal("Combo Đơn 1", initialHeader.MenuItemName);

        // Act: Admin updates Combo Name and Price to 60000
        await _comboService.UpdateComboAsync(created.Id, new UpdateComboRequest
        {
            CategoryId = cat.Id,
            Name = "Combo Đơn 1 - Đã Tăng Giá",
            Price = 60000,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new CreateComboItemRequest
                {
                    Id = created.FixedAndAddOnItems[0].Id,
                    MenuItemId = chicken.Id,
                    ComponentType = ComboComponentType.Fixed,
                    DefaultQuantity = 1
                }
            }
        });

        // Assert: Past Order must remain UNTOUCHED with its original snapshot values
        var preservedOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == posResp.OrderId);
        Assert.NotNull(preservedOrder);
        var preservedHeader = preservedOrder.OrderItems.First(oi => oi.ParentItemId == null);
        Assert.Equal(40000, preservedHeader.UnitPrice);
        Assert.Equal("Combo Đơn 1", preservedHeader.MenuItemName);
        Assert.Equal(40000, preservedOrder.TotalAmount);
    }

    [Fact]
    public async Task KdsActiveTickets_MapsComboHierarchyAndStationDataCorrectly()
    {
        // Arrange
        var station = new KitchenStation { Code = "GRILL", Name = "Grill Station" };
        _context.KitchenStations.Add(station);

        var chicken = new MenuItem { Name = "Fried Chicken", Price = 45000, IsActive = true, KitchenStation = station };
        var combo = new MenuItem { Name = "Family Bucket", Price = 99000, IsActive = true, IsCombo = true };
        _context.MenuItems.AddRange(chicken, combo);

        var order = new Order { OrderCode = "ORD-KDS-COMBO", ShiftId = 1, CreatedByUserId = 1, Status = (int)OrderStatus.InPreparation };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var header = new OrderItem
        {
            OrderId = order.Id,
            MenuItemId = combo.Id,
            MenuItemName = combo.Name,
            UnitPrice = 99000,
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Pending
        };
        _context.OrderItems.Add(header);
        await _context.SaveChangesAsync();

        var child = new OrderItem
        {
            OrderId = order.Id,
            MenuItemId = chicken.Id,
            MenuItemName = chicken.Name,
            UnitPrice = 0,
            Quantity = 2,
            ParentItemId = header.Id,
            KitchenStatus = (int)KitchenStatus.Pending
        };
        _context.OrderItems.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var tickets = await _kitchenService.GetActiveTicketsAsync();

        // Assert
        Assert.Single(tickets);
        var childDto = tickets[0].Items.FirstOrDefault(i => i.OrderItemId == child.Id);
        Assert.NotNull(childDto);
        Assert.True(childDto.IsComboChild);
        Assert.Equal(header.Id, childDto.ParentItemId);
        Assert.Equal("Family Bucket", childDto.ParentItemName);
        Assert.Equal(station.Id, childDto.KitchenStationId);
    }

    private async Task SeedPosPrerequisitesAsync()
    {
        if (!await _context.Users.AnyAsync())
        {
            var user = new User { Id = 1, Username = "cashier1", FullName = "Cashier One", RoleId = 1 };
            var register = new Register { Id = 1, Code = "REG01", Name = "Register 1", IsActive = true };
            var shift = new Shift { Id = 1, UserId = 1, RegisterId = 1, Status = (int)ShiftStatus.Open, OpeningBalance = 500000m, OpenedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            _context.Registers.Add(register);
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task AdminComboScenarios_Combo1_AllFixedItems_CreatesAndOrdersCorrectly()
    {
        await SeedPosPrerequisitesAsync();

        // 1 Fixed Pizza, 1 Fixed Coke
        var category = new Category { Name = "Combos", IsActive = true };
        var pizza = new MenuItem { Name = "Pizza", Price = 120000, IsActive = true };
        var coke = new MenuItem { Name = "Coke", Price = 20000, IsActive = true };
        _context.Categories.Add(category);
        _context.MenuItems.AddRange(pizza, coke);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = category.Id,
            Name = "Combo 1 (Fixed Pizza + Coke)",
            Price = 130000,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 },
                new() { MenuItemId = coke.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 2 }
            }
        };

        var comboDetail = await _comboService.CreateComboAsync(request);
        Assert.NotNull(comboDetail);
        Assert.Equal(2, comboDetail.FixedAndAddOnItems.Count);
        Assert.Empty(comboDetail.Groups);

        // POS Submit Order
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = f.AdditionalPrice
                    }).ToList()
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(3, order.OrderItems.Count); // 1 header + 2 children
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        Assert.Equal(comboDetail.Id, header.MenuItemId);
        var children = order.OrderItems.Where(i => i.ParentItemId == header.Id).ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.MenuItemId == pizza.Id);
        Assert.Contains(children, c => c.MenuItemId == coke.Id);
    }

    [Fact]
    public async Task AdminComboScenarios_Combo2_FixedPizzaAndChoiceDrink_CreatesAndOrdersCorrectly()
    {
        await SeedPosPrerequisitesAsync();

        // 1 Fixed Pizza, Choose 1 Drink
        var category = new Category { Name = "Combos", IsActive = true };
        var pizza = new MenuItem { Name = "Pizza Margherita", Price = 120000, IsActive = true };
        var coke = new MenuItem { Name = "Coke Zero", Price = 20000, IsActive = true };
        var sprite = new MenuItem { Name = "Sprite", Price = 20000, IsActive = true };
        _context.Categories.Add(category);
        _context.MenuItems.AddRange(pizza, coke, sprite);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = category.Id,
            Name = "Combo 2 (Fixed Pizza + Choose Drink)",
            Price = 135000,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 }
            },
            Groups = new List<CreateComboGroupRequest>
            {
                new()
                {
                    Name = "Choose 1 Drink",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    SortOrder = 1,
                    Items = new List<CreateComboItemRequest>
                    {
                        new() { MenuItemId = coke.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 },
                        new() { MenuItemId = sprite.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 5000, SortOrder = 2 }
                    }
                }
            }
        };

        var comboDetail = await _comboService.CreateComboAsync(request);
        Assert.NotNull(comboDetail);
        Assert.Single(comboDetail.FixedAndAddOnItems);
        Assert.Single(comboDetail.Groups);
        Assert.Equal(2, comboDetail.Groups[0].Items.Count);

        // POS Submit Order with Fixed Pizza and Chosen Sprite
        var fixedPizza = comboDetail.FixedAndAddOnItems[0];
        var chosenSprite = comboDetail.Groups[0].Items.First(i => i.MenuItemId == sprite.Id);

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = fixedPizza.Id, MenuItemId = fixedPizza.MenuItemId, Quantity = 1, AdditionalPrice = fixedPizza.AdditionalPrice },
                        new() { ComboItemId = chosenSprite.Id, MenuItemId = chosenSprite.MenuItemId, Quantity = 1, AdditionalPrice = chosenSprite.AdditionalPrice, ComboGroupId = comboDetail.Groups[0].Id }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(3, order.OrderItems.Count); // 1 header + 2 children
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        var children = order.OrderItems.Where(i => i.ParentItemId == header.Id).ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.MenuItemId == pizza.Id);
        Assert.Contains(children, c => c.MenuItemId == sprite.Id && c.AdditionalPrice == 5000);
    }

    [Fact]
    public async Task AdminComboScenarios_Combo3_ChoicePizzaChoiceDrinkChoiceSide_CreatesAndOrdersCorrectly()
    {
        await SeedPosPrerequisitesAsync();

        // Choose 1 Pizza, Choose 1 Drink, Choose 1 Side
        var category = new Category { Name = "Combos", IsActive = true };
        var pizza1 = new MenuItem { Name = "Margherita", Price = 110000, IsActive = true };
        var pizza2 = new MenuItem { Name = "Pepperoni", Price = 130000, IsActive = true };
        var drink1 = new MenuItem { Name = "Coke", Price = 20000, IsActive = true };
        var drink2 = new MenuItem { Name = "Mineral Water", Price = 15000, IsActive = true };
        var side1 = new MenuItem { Name = "French Fries", Price = 35000, IsActive = true };
        var side2 = new MenuItem { Name = "Salad", Price = 40000, IsActive = true };
        _context.Categories.Add(category);
        _context.MenuItems.AddRange(pizza1, pizza2, drink1, drink2, side1, side2);
        await _context.SaveChangesAsync();

        var request = new CreateComboRequest
        {
            CategoryId = category.Id,
            Name = "Combo 3 (3 Choice Groups)",
            Price = 160000,
            Groups = new List<CreateComboGroupRequest>
            {
                new()
                {
                    Name = "Choose 1 Pizza",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    SortOrder = 1,
                    Items = new List<CreateComboItemRequest>
                    {
                        new() { MenuItemId = pizza1.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 },
                        new() { MenuItemId = pizza2.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 20000, SortOrder = 2 }
                    }
                },
                new()
                {
                    Name = "Choose 1 Drink",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    SortOrder = 2,
                    Items = new List<CreateComboItemRequest>
                    {
                        new() { MenuItemId = drink1.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 },
                        new() { MenuItemId = drink2.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 2 }
                    }
                },
                new()
                {
                    Name = "Choose 1 Side",
                    MinSelections = 1,
                    MaxSelections = 1,
                    IsRequired = true,
                    SortOrder = 3,
                    Items = new List<CreateComboItemRequest>
                    {
                        new() { MenuItemId = side1.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, SortOrder = 1 },
                        new() { MenuItemId = side2.Id, ComponentType = ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 5000, SortOrder = 2 }
                    }
                }
            }
        };

        var comboDetail = await _comboService.CreateComboAsync(request);
        Assert.NotNull(comboDetail);
        Assert.Empty(comboDetail.FixedAndAddOnItems);
        Assert.Equal(3, comboDetail.Groups.Count);

        // POS Submit Order selecting Pepperoni (+20k), Mineral Water (+0), Salad (+5k)
        var chosenPizza = comboDetail.Groups[0].Items.First(i => i.MenuItemId == pizza2.Id);
        var chosenDrink = comboDetail.Groups[1].Items.First(i => i.MenuItemId == drink2.Id);
        var chosenSide = comboDetail.Groups[2].Items.First(i => i.MenuItemId == side2.Id);

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = chosenPizza.Id, MenuItemId = chosenPizza.MenuItemId, Quantity = 1, AdditionalPrice = chosenPizza.AdditionalPrice, ComboGroupId = comboDetail.Groups[0].Id },
                        new() { ComboItemId = chosenDrink.Id, MenuItemId = chosenDrink.MenuItemId, Quantity = 1, AdditionalPrice = chosenDrink.AdditionalPrice, ComboGroupId = comboDetail.Groups[1].Id },
                        new() { ComboItemId = chosenSide.Id, MenuItemId = chosenSide.MenuItemId, Quantity = 1, AdditionalPrice = chosenSide.AdditionalPrice, ComboGroupId = comboDetail.Groups[2].Id }
                    }
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(4, order.OrderItems.Count); // 1 header + 3 children
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        var children = order.OrderItems.Where(i => i.ParentItemId == header.Id).ToList();
        Assert.Equal(3, children.Count);
        Assert.Contains(children, c => c.MenuItemId == pizza2.Id && c.AdditionalPrice == 20000);
        Assert.Contains(children, c => c.MenuItemId == drink2.Id && c.AdditionalPrice == 0);
        Assert.Contains(children, c => c.MenuItemId == side2.Id && c.AdditionalPrice == 5000);
    }

    [Fact]
    public async Task VoidComboHeader_CascadesVoidToAllChildren_AndRecalculatesTotals()
    {
        await SeedPosPrerequisitesAsync();

        var cat = new Category { Name = "Combo Cat" };
        var pizza = new MenuItem { Name = "Pizza V", Price = 100000, Category = cat, IsActive = true };
        var drink = new MenuItem { Name = "Drink V", Price = 20000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, drink);
        await _context.SaveChangesAsync();

        var comboDetail = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Void Test",
            Price = 110000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 },
                new() { MenuItemId = drink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 }
            }
        });

        // Create Order with Combo
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = 0
                    }).ToList()
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        Assert.Equal(110000, order.TotalAmount);
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        var children = order.OrderItems.Where(i => i.ParentItemId == header.Id).ToList();
        Assert.Equal(2, children.Count);

        // Void the Combo Header
        var voidSuccess = await _posOrderService.VoidOrderItemAsync(order.Id, header.Id, "Khách đổi ý hủy combo", 1);
        Assert.True(voidSuccess);

        // Refresh and verify
        await _context.Entry(order).ReloadAsync();
        var reloadedItems = await _context.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();

        var reloadedHeader = reloadedItems.First(i => i.Id == header.Id);
        Assert.Equal((int)OrderItemStatus.Voided, reloadedHeader.Status);

        var reloadedChildren = reloadedItems.Where(i => i.ParentItemId == header.Id).ToList();
        Assert.All(reloadedChildren, c => Assert.Equal((int)OrderItemStatus.Voided, c.Status));

        // SubTotal and TotalAmount should be 0
        Assert.Equal(0, order.SubTotal);
        Assert.Equal(0, order.TotalAmount);

        // VoidLog created
        var logs = await _context.VoidLogs.Where(l => l.OrderId == order.Id).ToListAsync();
        Assert.NotEmpty(logs);
    }

    [Fact]
    public async Task VoidSingleChildItem_OnlyVoidsChildItem_PreservesHeader()
    {
        await SeedPosPrerequisitesAsync();

        var cat = new Category { Name = "Child Void Cat" };
        var pizza = new MenuItem { Name = "Pizza CV", Price = 100000, Category = cat, IsActive = true };
        var drink = new MenuItem { Name = "Drink CV", Price = 20000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, drink);
        await _context.SaveChangesAsync();

        var comboDetail = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Child Void",
            Price = 110000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 },
                new() { MenuItemId = drink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = 0
                    }).ToList()
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        var drinkChild = order.OrderItems.First(i => i.ParentItemId == header.Id && i.MenuItemId == drink.Id);

        // Void drink child only
        var voidSuccess = await _posOrderService.VoidOrderItemAsync(order.Id, drinkChild.Id, "Hết nước ngọt", 1);
        Assert.True(voidSuccess);

        // Refresh items
        var reloadedItems = await _context.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        var reloadedHeader = reloadedItems.First(i => i.Id == header.Id);
        var reloadedDrink = reloadedItems.First(i => i.Id == drinkChild.Id);
        var reloadedPizza = reloadedItems.First(i => i.ParentItemId == header.Id && i.MenuItemId == pizza.Id);

        Assert.Equal((int)OrderItemStatus.Active, reloadedHeader.Status); // Header remains active
        Assert.Equal((int)OrderItemStatus.Active, reloadedPizza.Status); // Pizza child remains active
        Assert.Equal((int)OrderItemStatus.Voided, reloadedDrink.Status); // Only drink child is voided
    }

    [Fact]
    public async Task BillViewerService_GetBillK80ReceiptAsync_OrdersChildrenImmediatelyAfterHeader()
    {
        await SeedPosPrerequisitesAsync();

        var cat = new Category { Name = "Bill Cat" };
        var pizza = new MenuItem { Name = "Pizza Receipt", Price = 100000, Category = cat, IsActive = true };
        var drink = new MenuItem { Name = "Drink Receipt", Price = 20000, Category = cat, IsActive = true };
        var sideAlone = new MenuItem { Name = "French Fries Alone", Price = 35000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, drink, sideAlone);
        await _context.SaveChangesAsync();

        var comboDetail = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Family Receipt",
            Price = 120000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 },
                new() { MenuItemId = drink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 }
            }
        });

        // Submit order with Combo + Standalone side item
        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = 0
                    }).ToList()
                },
                new()
                {
                    MenuItemId = sideAlone.Id,
                    Quantity = 2
                }
            }
        });

        // Fetch K80 Receipt via BillViewerService
        var billService = new BillViewerService(_context);
        var receipt = await billService.GetBillK80ReceiptAsync(posResp.OrderId);

        Assert.NotNull(receipt);
        Assert.Equal(4, receipt.Items.Count); // Header + 2 children + 1 standalone

        // Header must come first, followed by its 2 children (IsComboChild = true)
        Assert.Equal("Combo Family Receipt", receipt.Items[0].MenuItemName);
        Assert.False(receipt.Items[0].IsComboChild);

        Assert.True(receipt.Items[1].IsComboChild);
        Assert.True(receipt.Items[2].IsComboChild);

        // Standalone comes last
        Assert.Equal("French Fries Alone", receipt.Items[3].MenuItemName);
        Assert.False(receipt.Items[3].IsComboChild);
    }

    [Fact]
    public async Task Kds_ComboChildProcessing_UpdatesHeaderToProcessing_AllDoneUpdatesHeaderToDone_AndOrderReady()
    {
        await SeedPosPrerequisitesAsync();

        var cat = new Category { Name = "KDS Combo Cat" };
        var pizza = new MenuItem { Name = "Pizza KDS", Price = 100000, Category = cat, IsActive = true };
        var drink = new MenuItem { Name = "Drink KDS", Price = 20000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, drink);
        await _context.SaveChangesAsync();

        var comboDetail = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo KDS Full",
            Price = 110000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 },
                new() { MenuItemId = drink.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = 0
                    }).ToList()
                }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var header = order.OrderItems.First(i => i.ParentItemId == null);
        var pizzaChild = order.OrderItems.First(i => i.ParentItemId == header.Id && i.MenuItemId == pizza.Id);
        var drinkChild = order.OrderItems.First(i => i.ParentItemId == header.Id && i.MenuItemId == drink.Id);

        // Initially both children and header are Pending (0)
        Assert.Equal((int)KitchenStatus.Pending, pizzaChild.KitchenStatus);
        Assert.Equal((int)KitchenStatus.Pending, drinkChild.KitchenStatus);
        Assert.Equal((int)KitchenStatus.Pending, header.KitchenStatus);

        // 1. Chef begins preparing Pizza (newStatus = 1 Processing)
        var updatedPizza = await _kitchenService.UpdateItemStatusAsync(pizzaChild.Id, (int)KitchenStatus.Processing);
        Assert.NotNull(updatedPizza);
        Assert.Equal((int)KitchenStatus.Processing, updatedPizza.KitchenStatus);

        // Refresh header -> Header must now be Processing (1)
        await _context.Entry(header).ReloadAsync();
        Assert.Equal((int)KitchenStatus.Processing, header.KitchenStatus);

        // 2. Chef finishes Pizza (newStatus = 2 Done)
        var donePizza = await _kitchenService.UpdateItemStatusAsync(pizzaChild.Id, (int)KitchenStatus.Done);
        Assert.NotNull(donePizza);
        Assert.Equal((int)KitchenStatus.Done, donePizza.KitchenStatus);

        // Header is still Processing because Drink is not yet done
        await _context.Entry(header).ReloadAsync();
        Assert.Equal((int)KitchenStatus.Processing, header.KitchenStatus);

        // 3. Chef finishes Drink (newStatus = 2 Done)
        var doneDrink = await _kitchenService.UpdateItemStatusAsync(drinkChild.Id, (int)KitchenStatus.Done);
        Assert.NotNull(doneDrink);
        Assert.Equal((int)KitchenStatus.Done, doneDrink.KitchenStatus);

        // Now all children are Done -> Header must automatically be Done (2)
        await _context.Entry(header).ReloadAsync();
        Assert.Equal((int)KitchenStatus.Done, header.KitchenStatus);

        // 4. Order.Status must be Ready (3), NOT Completed (4) before cashier payment!
        await _context.Entry(order).ReloadAsync();
        Assert.Equal((int)OrderStatus.Ready, order.Status);
    }

    [Fact]
    public async Task Kds_StationRouting_ReturnsOnlyItemsForSpecificStation()
    {
        await SeedPosPrerequisitesAsync();

        var stationKitchen = new KitchenStation { Code = "KITCHEN", Name = "Bếp Nóng" };
        var stationBar = new KitchenStation { Code = "BAR", Name = "Quầy Pha Chế" };
        _context.KitchenStations.AddRange(stationKitchen, stationBar);
        await _context.SaveChangesAsync();

        var cat = new Category { Name = "Station Cat" };
        var pizza = new MenuItem { Name = "Pizza Hot", Price = 100000, Category = cat, KitchenStationId = stationKitchen.Id, IsActive = true };
        var beer = new MenuItem { Name = "Draft Beer", Price = 30000, Category = cat, KitchenStationId = stationBar.Id, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.AddRange(pizza, beer);
        await _context.SaveChangesAsync();

        var comboDetail = await _comboService.CreateComboAsync(new CreateComboRequest
        {
            Name = "Combo Pizza & Beer",
            Price = 120000,
            CategoryId = cat.Id,
            FixedAndAddOnItems = new List<CreateComboItemRequest>
            {
                new() { MenuItemId = pizza.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 },
                new() { MenuItemId = beer.Id, ComponentType = ComboComponentType.Fixed, DefaultQuantity = 1, AdditionalPrice = 0 }
            }
        });

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = comboDetail.Id,
                    Quantity = 1,
                    SelectedComboItems = comboDetail.FixedAndAddOnItems.Select(f => new CartComboItemRequest
                    {
                        ComboItemId = f.Id,
                        MenuItemId = f.MenuItemId,
                        Quantity = f.DefaultQuantity,
                        AdditionalPrice = 0
                    }).ToList()
                }
            }
        });

        // 1. Fetch tickets for Kitchen Station
        var kitchenTickets = await _kitchenService.GetActiveTicketsAsync(includeCompleted: false, stationId: stationKitchen.Id);
        Assert.NotEmpty(kitchenTickets);
        var kitchenTicket = kitchenTickets.First(t => t.OrderId == posResp.OrderId);
        Assert.All(kitchenTicket.Items, i => Assert.True(i.KitchenStationId == stationKitchen.Id || i.KitchenStationId == null));
        Assert.Contains(kitchenTicket.Items, i => i.MenuItemName == "Pizza Hot");
        Assert.DoesNotContain(kitchenTicket.Items, i => i.MenuItemName == "Draft Beer");

        // 2. Fetch tickets for Bar Station
        var barTickets = await _kitchenService.GetActiveTicketsAsync(includeCompleted: false, stationId: stationBar.Id);
        Assert.NotEmpty(barTickets);
        var barTicket = barTickets.First(t => t.OrderId == posResp.OrderId);
        Assert.All(barTicket.Items, i => Assert.True(i.KitchenStationId == stationBar.Id || i.KitchenStationId == null));
        Assert.Contains(barTicket.Items, i => i.MenuItemName == "Draft Beer");
        Assert.DoesNotContain(barTicket.Items, i => i.MenuItemName == "Pizza Hot");
    }

    [Fact]
    public async Task Kds_TwoChefsProcessSameItemConcurrently_IdempotentAndSafe()
    {
        await SeedPosPrerequisitesAsync();

        var cat = new Category { Name = "Concurrent Cat" };
        var pizza = new MenuItem { Name = "Pizza Concur", Price = 100000, Category = cat, IsActive = true };
        _context.Categories.Add(cat);
        _context.MenuItems.Add(pizza);
        await _context.SaveChangesAsync();

        var posResp = await _posOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = pizza.Id, Quantity = 1 }
            }
        });

        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == posResp.OrderId);
        var pizzaItem = order.OrderItems.First();

        // Chef 1 updates to Processing
        var res1 = await _kitchenService.UpdateItemStatusAsync(pizzaItem.Id, (int)KitchenStatus.Processing);
        Assert.NotNull(res1);
        Assert.Equal((int)KitchenStatus.Processing, res1.KitchenStatus);

        // Chef 2 concurrently clicks to Processing (idempotent duplicate request)
        var res2 = await _kitchenService.UpdateItemStatusAsync(pizzaItem.Id, (int)KitchenStatus.Processing);
        Assert.NotNull(res2);
        Assert.Equal((int)KitchenStatus.Processing, res2.KitchenStatus);

        // Chef 1 finishes to Done
        var resDone1 = await _kitchenService.UpdateItemStatusAsync(pizzaItem.Id, (int)KitchenStatus.Done);
        Assert.NotNull(resDone1);
        Assert.Equal((int)KitchenStatus.Done, resDone1.KitchenStatus);

        // Chef 2 concurrently tries to revert or set to Processing after Done -> Must be rejected (null)
        var resInvalid = await _kitchenService.UpdateItemStatusAsync(pizzaItem.Id, (int)KitchenStatus.Processing);
        Assert.Null(resInvalid);
    }
}

