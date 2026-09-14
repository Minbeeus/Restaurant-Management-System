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
}
