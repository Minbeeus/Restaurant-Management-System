using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Domain.Events;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Domain.Tests.E2E;

/// <summary>
/// Autonomous, isolated test context for Opaque-Box E2E Tests.
/// Sets up in-memory ApplicationDbContext, seeds authoritative domain reference data,
/// and wires all real production Application and Infrastructure services.
/// </summary>
public class E2ETestContext : IDisposable
{
    public ApplicationDbContext Db { get; }
    public IIdentityService IdentityService { get; }
    public IPosOrderService PosOrderService { get; }
    public IPaymentService PaymentService { get; }
    public IKitchenService KitchenService { get; }
    public IInventoryService InventoryService { get; }
    public ICancelOrderService CancelOrderService { get; }
    public IShiftService ShiftService { get; }
    public ICashMovementService CashMovementService { get; }
    public IApprovalService ApprovalService { get; }
    public ILoyaltyService LoyaltyService { get; }
    public IBillViewerService BillViewerService { get; }
    public IReconciliationService ReconciliationService { get; }
    public IPermissionService PermissionService { get; }

    public Mock<IRestaurantNotificationService> NotificationMock { get; }
    public Mock<IHubContext<RestaurantHub, IRestaurantHubClient>> HubContextMock { get; }
    public Mock<IRestaurantHubClient> HubClientMock { get; }
    public Mock<IAuditService> AuditServiceMock { get; }
    public Mock<IConfiguration> ConfigurationMock { get; }

    // Seeded Reference Entities
    public User AdminUser { get; private set; } = null!;
    public User ManagerUser { get; private set; } = null!;
    public User CashierUser { get; private set; } = null!;
    public User ChefUser { get; private set; } = null!;

    public Register Register1 { get; private set; } = null!;
    public Shift ActiveShift { get; private set; } = null!;

    public Area Area1 { get; private set; } = null!;
    public Table Table1 { get; private set; } = null!;
    public Table Table2 { get; private set; } = null!;
    public Table Table3 { get; private set; } = null!;
    public Table Table4 { get; private set; } = null!;

    public Category CategoryMain { get; private set; } = null!;
    public Category CategoryDrink { get; private set; } = null!;

    public UnitOfMeasure UnitKg { get; private set; } = null!;
    public UnitOfMeasure UnitCan { get; private set; } = null!;

    public Material MatFlour { get; private set; } = null!;
    public Material MatCheese { get; private set; } = null!;
    public Material MatSauce { get; private set; } = null!;
    public Material MatBeef { get; private set; } = null!;
    public Material MatCokeCan { get; private set; } = null!;

    public MenuItem MenuPizzaBeef { get; private set; } = null!;
    public MenuItem MenuPizzaMargherita { get; private set; } = null!;
    public MenuItem MenuCocaCola { get; private set; } = null!;
    public MenuItem MenuFrenchFries { get; private set; } = null!;
    public MenuItem MenuComboFamily { get; private set; } = null!;

    public ComboGroup ComboGroupPizza { get; private set; } = null!;
    public ComboGroup ComboGroupDrink { get; private set; } = null!;
    public ComboItem ComboItemPizzaMargherita { get; private set; } = null!;
    public ComboItem ComboItemCocaCola { get; private set; } = null!;

    public Customer Customer1 { get; private set; } = null!;
    public CustomerTier TierBronze { get; private set; } = null!;
    public CustomerTier TierSilver { get; private set; } = null!;
    public CustomerTier TierGold { get; private set; } = null!;

    public Voucher Voucher10Pct { get; private set; } = null!;
    public Voucher VoucherFixed50K { get; private set; } = null!;

    public E2ETestContext(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new AuditableEntityInterceptor())
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();

        // Mocks setup
        NotificationMock = new Mock<IRestaurantNotificationService>();
        HubClientMock = new Mock<IRestaurantHubClient>();
        var hubClientsMock = new Mock<IHubClients<IRestaurantHubClient>>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(HubClientMock.Object);
        hubClientsMock.Setup(c => c.All).Returns(HubClientMock.Object);

        HubContextMock = new Mock<IHubContext<RestaurantHub, IRestaurantHubClient>>();
        HubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        AuditServiceMock = new Mock<IAuditService>();

        ConfigurationMock = new Mock<IConfiguration>();
        ConfigurationMock.Setup(c => c["JwtSettings:SecretKey"]).Returns("RestaurantManagementSecretKeySuperProtection123!");
        ConfigurationMock.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("480");
        ConfigurationMock.Setup(c => c["SePay:ApiKey"]).Returns("SePaySecretApiKey123!");

        var localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var qrCodeService = new QrCodeService();
        var policyService = new PosPolicyService();

        // Core Services
        IdentityService = new IdentityService(Db, ConfigurationMock.Object);
        PermissionService = new PermissionService(Db);
        ApprovalService = new ApprovalService(Db, IdentityService, PermissionService, AuditServiceMock.Object);
        LoyaltyService = new LoyaltyService(Db, localizerMock.Object);
        InventoryService = new InventoryService(Db, NotificationMock.Object, localizerMock.Object);
        PosOrderService = new PosOrderService(Db, NotificationMock.Object, localizerMock.Object);
        CancelOrderService = new CancelOrderService(Db, LoyaltyService, NotificationMock.Object, localizerMock.Object);
        ShiftService = new ShiftService(Db, localizerMock.Object, ApprovalService, PermissionService, AuditServiceMock.Object);
        CashMovementService = new CashMovementService(Db, PermissionService, ApprovalService, policyService, AuditServiceMock.Object);
        BillViewerService = new BillViewerService(Db);
        ReconciliationService = new ReconciliationService(Db, PermissionService);

        // MediatR pipeline wiring for BOM domain events
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<OrderItemKitchenDoneDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns<OrderItemKitchenDoneDomainEvent, CancellationToken>(async (ev, ct) =>
            {
                await InventoryService.DeductInventoryForOrderItemAsync(ev.OrderItemId);
            });

        KitchenService = new KitchenService(Db, HubContextMock.Object, mediatorMock.Object, AuditServiceMock.Object, null);
        PaymentService = new PaymentService(
            Db,
            qrCodeService,
            NotificationMock.Object,
            LoyaltyService,
            mediatorMock.Object,
            ConfigurationMock.Object,
            localizerMock.Object,
            AuditServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        // 1. Roles & Users
        var roleAdmin = new Role { Id = 1, Name = "Admin", Description = "System Administrator" };
        var roleManager = new Role { Id = 2, Name = "Manager", Description = "Floor Manager" };
        var roleCashier = new Role { Id = 3, Name = "Cashier", Description = "POS Cashier" };
        var roleChef = new Role { Id = 4, Name = "Chef", Description = "Kitchen Chef" };

        Db.Roles.AddRange(roleAdmin, roleManager, roleCashier, roleChef);

        AdminUser = new User
        {
            Id = 1,
            Username = "admin",
            FullName = "Admin Quản Trị",
            RoleId = 1,
            Role = roleAdmin,
            PasswordHash = IdentityService.HashPassword("Admin@123"),
            PinCodeHash = IdentityService.HashPassword("999999"),
            EmployeeCode = "NV0001",
            IsActive = true
        };

        ManagerUser = new User
        {
            Id = 2,
            Username = "manager01",
            FullName = "Trần Quản Lý",
            RoleId = 2,
            Role = roleManager,
            PasswordHash = IdentityService.HashPassword("Manager@123"),
            PinCodeHash = IdentityService.HashPassword("888888"),
            EmployeeCode = "NV0002",
            IsActive = true
        };

        CashierUser = new User
        {
            Id = 3,
            Username = "cashier01",
            FullName = "Lê Thu Ngân",
            RoleId = 3,
            Role = roleCashier,
            PasswordHash = IdentityService.HashPassword("Cashier@123"),
            PinCodeHash = IdentityService.HashPassword("777777"),
            EmployeeCode = "NV0003",
            IsActive = true
        };

        ChefUser = new User
        {
            Id = 4,
            Username = "chef01",
            FullName = "Nguyễn Bếp Trưởng",
            RoleId = 4,
            Role = roleChef,
            PasswordHash = IdentityService.HashPassword("Chef@123"),
            PinCodeHash = IdentityService.HashPassword("666666"),
            EmployeeCode = "NV0004",
            IsActive = true
        };

        Db.Users.AddRange(AdminUser, ManagerUser, CashierUser, ChefUser);

        // 2. Register & Active Shift
        Register1 = new Register { Id = 1, Code = "REG01", Name = "Máy POS Chính", IsActive = true };
        Db.Registers.Add(Register1);

        ActiveShift = new Shift
        {
            Id = 1,
            UserId = CashierUser.Id,
            RegisterId = Register1.Id,
            OpeningBalance = 1000000m,
            Status = (int)ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow.AddHours(-2),
            Note = "Ca sáng chính"
        };
        Db.Shifts.Add(ActiveShift);

        // 3. Area & Tables
        Area1 = new Area { Id = 1, Name = "Tầng 1 Sảnh Chính", IsActive = true };
        Db.Areas.Add(Area1);

        Table1 = new Table { Id = 1, Name = "Bàn 01", AreaId = 1, Capacity = 4, Status = (int)TableStatus.Available, IsActive = true, QrToken = "token_b01_secure_123456789012" };
        Table2 = new Table { Id = 2, Name = "Bàn 02", AreaId = 1, Capacity = 4, Status = (int)TableStatus.Available, IsActive = true, QrToken = "token_b02_secure_123456789012" };
        Table3 = new Table { Id = 3, Name = "Bàn 03", AreaId = 1, Capacity = 6, Status = (int)TableStatus.Available, IsActive = true, QrToken = "token_b03_secure_123456789012" };
        Table4 = new Table { Id = 4, Name = "Bàn 04", AreaId = 1, Capacity = 2, Status = (int)TableStatus.Available, IsActive = true, QrToken = "token_b04_secure_123456789012" };
        Db.Tables.AddRange(Table1, Table2, Table3, Table4);

        // 4. Units & Materials
        UnitKg = new UnitOfMeasure { Id = 1, Name = "Kilogram", Abbreviation = "kg", CreatedAt = DateTime.UtcNow };
        UnitCan = new UnitOfMeasure { Id = 2, Name = "Lon", Abbreviation = "lon", CreatedAt = DateTime.UtcNow };
        Db.UnitOfMeasures.AddRange(UnitKg, UnitCan);

        MatFlour = new Material { Id = 1, UnitOfMeasureId = 1, Name = "Bột Mì Làm Bánh", CurrentStock = 100m, MinStockLevel = 20m, CostPerUnit = 25000m, IsActive = true };
        MatCheese = new Material { Id = 2, UnitOfMeasureId = 1, Name = "Phô Mai Mozzarella", CurrentStock = 50m, MinStockLevel = 10m, CostPerUnit = 180000m, IsActive = true };
        MatSauce = new Material { Id = 3, UnitOfMeasureId = 1, Name = "Sốt Cà Chua Pizza", CurrentStock = 40m, MinStockLevel = 5m, CostPerUnit = 40000m, IsActive = true };
        MatBeef = new Material { Id = 4, UnitOfMeasureId = 1, Name = "Thịt Bò Bằm", CurrentStock = 30m, MinStockLevel = 5m, CostPerUnit = 220000m, IsActive = true };
        MatCokeCan = new Material { Id = 5, UnitOfMeasureId = 2, Name = "Coca Cola Lon 330ml", CurrentStock = 120m, MinStockLevel = 24m, CostPerUnit = 10000m, IsActive = true };
        Db.Materials.AddRange(MatFlour, MatCheese, MatSauce, MatBeef, MatCokeCan);

        // 5. Menu Categories & MenuItems
        CategoryMain = new Category { Id = 1, Name = "Pizza & Pasta", SortOrder = 1, IsActive = true };
        CategoryDrink = new Category { Id = 2, Name = "Thức Uống", SortOrder = 2, IsActive = true };
        Db.Categories.AddRange(CategoryMain, CategoryDrink);

        MenuPizzaBeef = new MenuItem { Id = 10, CategoryId = 1, Name = "Pizza Bò Bằm", Price = 180000m, IsActive = true, SortOrder = 1 };
        MenuPizzaMargherita = new MenuItem { Id = 11, CategoryId = 1, Name = "Pizza Margherita", Price = 150000m, IsActive = true, SortOrder = 2 };
        MenuCocaCola = new MenuItem { Id = 20, CategoryId = 2, Name = "Coca Cola", Price = 25000m, IsActive = true, SortOrder = 3 };
        MenuFrenchFries = new MenuItem { Id = 30, CategoryId = 1, Name = "Khoai Tây Chiên", Price = 45000m, IsActive = true, SortOrder = 4 };
        MenuComboFamily = new MenuItem { Id = 100, CategoryId = 1, Name = "Combo Gia Đình", Price = 199000m, IsCombo = true, IsActive = true, SortOrder = 5 };

        Db.MenuItems.AddRange(MenuPizzaBeef, MenuPizzaMargherita, MenuCocaCola, MenuFrenchFries, MenuComboFamily);

        // Combo Configuration
        ComboGroupPizza = new ComboGroup { Id = 1, ComboId = 100, Name = "Chọn Pizza Chính", MinSelections = 1, MaxSelections = 1, IsRequired = true };
        ComboGroupDrink = new ComboGroup { Id = 2, ComboId = 100, Name = "Chọn Đồ Uống", MinSelections = 1, MaxSelections = 1, IsRequired = true };
        Db.ComboGroups.AddRange(ComboGroupPizza, ComboGroupDrink);

        ComboItemPizzaMargherita = new ComboItem { Id = 101, ComboId = 100, ComboGroupId = 1, MenuItemId = 11, ComponentType = (int)ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, IsActive = true };
        ComboItemCocaCola = new ComboItem { Id = 102, ComboId = 100, ComboGroupId = 2, MenuItemId = 20, ComponentType = (int)ComboComponentType.Choice, DefaultQuantity = 1, AdditionalPrice = 0, IsActive = true };
        Db.ComboItems.AddRange(ComboItemPizzaMargherita, ComboItemCocaCola);

        // 6. BOM Recipes
        // Pizza Bò Bằm: 0.2kg Flour, 0.1kg Cheese, 0.05kg Sauce, 0.15kg Beef
        var recipePizzaBeef = new Recipe { Id = 1, MenuItemId = 10 };
        var rItemBeef1 = new RecipeItem { RecipeId = 1, MaterialId = 1, UnitOfMeasureId = 1, Quantity = 0.20m };
        var rItemBeef2 = new RecipeItem { RecipeId = 1, MaterialId = 2, UnitOfMeasureId = 1, Quantity = 0.10m };
        var rItemBeef3 = new RecipeItem { RecipeId = 1, MaterialId = 3, UnitOfMeasureId = 1, Quantity = 0.05m };
        var rItemBeef4 = new RecipeItem { RecipeId = 1, MaterialId = 4, UnitOfMeasureId = 1, Quantity = 0.15m };
        recipePizzaBeef.RecipeItems.Add(rItemBeef1);
        recipePizzaBeef.RecipeItems.Add(rItemBeef2);
        recipePizzaBeef.RecipeItems.Add(rItemBeef3);
        recipePizzaBeef.RecipeItems.Add(rItemBeef4);

        // Pizza Margherita: 0.2kg Flour, 0.15kg Cheese, 0.05kg Sauce
        var recipePizzaMarg = new Recipe { Id = 2, MenuItemId = 11 };
        var rItemMarg1 = new RecipeItem { RecipeId = 2, MaterialId = 1, UnitOfMeasureId = 1, Quantity = 0.20m };
        var rItemMarg2 = new RecipeItem { RecipeId = 2, MaterialId = 2, UnitOfMeasureId = 1, Quantity = 0.15m };
        var rItemMarg3 = new RecipeItem { RecipeId = 2, MaterialId = 3, UnitOfMeasureId = 1, Quantity = 0.05m };
        recipePizzaMarg.RecipeItems.Add(rItemMarg1);
        recipePizzaMarg.RecipeItems.Add(rItemMarg2);
        recipePizzaMarg.RecipeItems.Add(rItemMarg3);

        // Coca Cola: 1 lon
        var recipeCoke = new Recipe { Id = 3, MenuItemId = 20 };
        var rItemCoke1 = new RecipeItem { RecipeId = 3, MaterialId = 5, UnitOfMeasureId = 2, Quantity = 1.0m };
        recipeCoke.RecipeItems.Add(rItemCoke1);

        Db.Recipes.AddRange(recipePizzaBeef, recipePizzaMarg, recipeCoke);

        // 7. Customer Loyalty Tiers & Customers
        TierBronze = new CustomerTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0m, SortOrder = 1 };
        TierSilver = new CustomerTier { Id = 2, Name = "Bạc", MinPoints = 500, DiscountPercent = 5m, SortOrder = 2 };
        TierGold = new CustomerTier { Id = 3, Name = "Vàng", MinPoints = 1000, DiscountPercent = 10m, SortOrder = 3 };
        Db.CustomerTiers.AddRange(TierBronze, TierSilver, TierGold);

        Customer1 = new Customer
        {
            Id = 1,
            FullName = "Nguyễn Văn Thành",
            PhoneNumber = "0901234567",
            TierId = 1,
            TotalPoints = 200,
            AvailablePoints = 200,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
        Db.Customers.Add(Customer1);

        // 8. Vouchers
        Voucher10Pct = new Voucher
        {
            Id = 1,
            Code = "GIAM10",
            DiscountType = (int)VoucherDiscountType.Percent,
            DiscountValue = 10m,
            MinOrderAmount = 150000m,
            MaxDiscountAmount = 20000m,
            UsageLimit = 100,
            UsedCount = 0,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        VoucherFixed50K = new Voucher
        {
            Id = 2,
            Code = "GIAM50K",
            DiscountType = (int)VoucherDiscountType.FixedAmount,
            DiscountValue = 50000m,
            MinOrderAmount = 200000m,
            MaxDiscountAmount = 50000m,
            UsageLimit = 50,
            UsedCount = 0,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        Db.Vouchers.AddRange(Voucher10Pct, VoucherFixed50K);

        Db.SaveChanges();
    }

    public void Dispose()
    {
        Db.Database.EnsureDeleted();
        Db.Dispose();
        GC.SuppressFinalize(this);
    }
}
