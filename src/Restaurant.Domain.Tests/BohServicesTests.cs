using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class BohServicesTests
{
    private readonly ApplicationDbContext _context;
    private readonly QrCodeService _qrCodeService;
    private readonly CategoryService _categoryService;
    private readonly MenuItemService _menuItemService;
    private readonly AreaService _areaService;
    private readonly TableService _tableService;

    public BohServicesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor())
            .Options;

        _context = new ApplicationDbContext(options);
        _qrCodeService = new QrCodeService();
        _categoryService = new CategoryService(_context);
        _menuItemService = new MenuItemService(_context);
        _areaService = new AreaService(_context);
        _tableService = new TableService(_context, _qrCodeService);
    }

    [Fact]
    public async Task CategoryService_Create_And_SoftDelete_ShouldWork()
    {
        // Act - Create
        var category = await _categoryService.CreateAsync(new CreateCategoryRequest
        {
            Name = "Khai vị",
            Description = "Món ăn nhẹ",
            SortOrder = 1
        });

        // Assert - Create
        Assert.NotNull(category);
        Assert.True(category.Id > 0);
        Assert.Equal("Khai vị", category.Name);

        // Act - Delete (Soft Delete)
        var deleteResult = await _categoryService.DeleteAsync(category.Id);

        // Assert - Delete
        Assert.True(deleteResult);
        var categories = await _categoryService.GetAllAsync();
        Assert.Empty(categories);

        // Check DB directly with IgnoreQueryFilters
        var dbCategory = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == category.Id);
        Assert.NotNull(dbCategory);
        Assert.True(dbCategory.IsDeleted);
    }

    [Fact]
    public async Task MenuItemService_Create_And_SoftDelete_ShouldWork()
    {
        // Arrange
        var category = await _categoryService.CreateAsync(new CreateCategoryRequest { Name = "Món chính" });

        // Act - Create
        var item = await _menuItemService.CreateAsync(new CreateMenuItemRequest
        {
            CategoryId = category.Id,
            Name = "Phở Bò Gia Truyền",
            Price = 65000,
            SortOrder = 1
        });

        // Assert - Create
        Assert.NotNull(item);
        Assert.Equal("Phở Bò Gia Truyền", item.Name);
        Assert.Equal(65000, item.Price);

        // Act - Delete (Soft Delete)
        var deleteResult = await _menuItemService.DeleteAsync(item.Id);

        // Assert - Delete
        Assert.True(deleteResult);
        var activeItems = await _menuItemService.GetActiveMenuItemsAsync();
        Assert.Empty(activeItems);

        // Check DB directly
        var dbItem = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == item.Id);
        Assert.NotNull(dbItem);
        Assert.True(dbItem.IsDeleted);
    }

    [Fact]
    public async Task TableService_CreateTable_ShouldAutoGenerate32CharQrToken_And_ImagePng()
    {
        // Arrange
        var area = await _areaService.CreateAsync(new CreateAreaRequest { Name = "Tầng 1" });

        // Act - Create Table
        var table = await _tableService.CreateAsync(new CreateTableRequest
        {
            AreaId = area.Id,
            Name = "Bàn 01",
            Capacity = 4,
            SortOrder = 1
        });

        // Assert - QrToken
        Assert.NotNull(table);
        Assert.NotEmpty(table.QrToken);
        Assert.True(table.QrToken.Length >= 32);

        // Act - Get QR Image
        var qrImage = await _tableService.GetTableQrCodeImageAsync(table.Id, "https://nhahang.com");

        // Assert - Image Bytes
        Assert.NotNull(qrImage);
        Assert.NotEmpty(qrImage);
    }

    [Fact]
    public async Task InventoryService_GetMaterialsBySupplier_ShouldFilterBySupplierCategory()
    {
        // Arrange
        var uom = new UnitOfMeasure { Name = "Kilogram", Abbreviation = "Kg" };
        await _context.UnitOfMeasures.AddAsync(uom);
        await _context.SaveChangesAsync();

        var materials = new List<Material>
        {
            new Material { UnitOfMeasureId = uom.Id, Name = "Phô mai Mozzarella khối", CostPerUnit = 220000, IsActive = true },
            new Material { UnitOfMeasureId = uom.Id, Name = "Sữa tươi tiệt trùng", CostPerUnit = 32000, IsActive = true },
            new Material { UnitOfMeasureId = uom.Id, Name = "Bắp ngọt hạt", CostPerUnit = 35000, IsActive = true },
            new Material { UnitOfMeasureId = uom.Id, Name = "Bột Matcha Nhật Bản", CostPerUnit = 450000, IsActive = true },
            new Material { UnitOfMeasureId = uom.Id, Name = "Cánh gà tươi", CostPerUnit = 110000, IsActive = true },
            new Material { UnitOfMeasureId = uom.Id, Name = "Bột mì Pizza Tipo 00", CostPerUnit = 28000, IsActive = true }
        };
        await _context.Materials.AddRangeAsync(materials);

        var anchorSupplier = new Supplier { Name = "Công ty CP Sữa & Phô Mai Anchor Việt Nam", IsActive = true };
        var dalatSupplier = new Supplier { Name = "Nông sản Sạch Đà Lạt", IsActive = true };
        await _context.Suppliers.AddRangeAsync(anchorSupplier, dalatSupplier);
        await _context.SaveChangesAsync();

        var inventoryService = new InventoryService(_context, null!);

        // Act
        var anchorItems = await inventoryService.GetMaterialsBySupplierIdAsync(anchorSupplier.Id);
        var dalatItems = await inventoryService.GetMaterialsBySupplierIdAsync(dalatSupplier.Id);

        // Assert - Anchor should ONLY return cheese & dairy items
        Assert.Contains(anchorItems, m => m.Name.Contains("Phô mai Mozzarella"));
        Assert.Contains(anchorItems, m => m.Name.Contains("Sữa tươi"));
        Assert.DoesNotContain(anchorItems, m => m.Name.Contains("Bắp ngọt"));
        Assert.DoesNotContain(anchorItems, m => m.Name.Contains("Bột Matcha"));
        Assert.DoesNotContain(anchorItems, m => m.Name.Contains("Cánh gà"));

        // Assert - Da Lat should return vegetables/fruits
        Assert.Contains(dalatItems, m => m.Name.Contains("Bắp ngọt"));
        Assert.DoesNotContain(dalatItems, m => m.Name.Contains("Phô mai"));
        Assert.DoesNotContain(dalatItems, m => m.Name.Contains("Cánh gà"));
    }
}
