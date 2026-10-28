using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Xunit;

namespace Restaurant.Domain.Tests;

public class TableFloorplanSeederTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public TableFloorplanSeederTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

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
    public async Task SeedPizzaAreasAndTables_ShouldCreateExact4AreasAnd34TablesWith76Capacity()
    {
        // Arrange
        using var context = GetDbContext(nameof(SeedPizzaAreasAndTables_ShouldCreateExact4AreasAnd34TablesWith76Capacity));

        // Act
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);

        // Assert - Areas
        var activeAreas = await context.Areas.Where(a => a.IsActive).OrderBy(a => a.SortOrder).ToListAsync();
        Assert.Equal(4, activeAreas.Count);
        Assert.Equal("Khu A", activeAreas[0].Name);
        Assert.Equal("Area A", activeAreas[0].NameEn);
        Assert.Equal("Khu B", activeAreas[1].Name);
        Assert.Equal("Area B", activeAreas[1].NameEn);
        Assert.Equal("Khu C", activeAreas[2].Name);
        Assert.Equal("Area C", activeAreas[2].NameEn);
        Assert.Equal("Khu D", activeAreas[3].Name);
        Assert.Equal("Area D", activeAreas[3].NameEn);

        // Assert - Tables
        var activeTables = await context.Tables.Where(t => t.IsActive).ToListAsync();
        Assert.Equal(34, activeTables.Count);

        // Assert - Double tables & 4-seat tables
        var doubleTables = activeTables.Where(t => t.Capacity == 2).ToList();
        var fourSeatTables = activeTables.Where(t => t.Capacity == 4).ToList();
        Assert.Equal(30, doubleTables.Count);
        Assert.Equal(4, fourSeatTables.Count);

        // Assert - Total Capacity
        var totalCapacity = activeTables.Sum(t => t.Capacity);
        Assert.Equal(76, totalCapacity);

        // Assert - Area Distribution
        var areaA = activeAreas[0];
        var areaB = activeAreas[1];
        var areaC = activeAreas[2];
        var areaD = activeAreas[3];

        var tablesA = activeTables.Where(t => t.AreaId == areaA.Id).OrderBy(t => t.Name).ToList();
        var tablesB = activeTables.Where(t => t.AreaId == areaB.Id).OrderBy(t => t.Name).ToList();
        var tablesC = activeTables.Where(t => t.AreaId == areaC.Id).OrderBy(t => t.Name).ToList();
        var tablesD = activeTables.Where(t => t.AreaId == areaD.Id).OrderBy(t => t.Name).ToList();

        // Area A: 8 double tables (A01 - A08), Capacity 16
        Assert.Equal(8, tablesA.Count);
        Assert.All(tablesA, t => Assert.Equal(2, t.Capacity));
        Assert.Equal(16, tablesA.Sum(t => t.Capacity));
        Assert.Equal(new[] { "A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08" }, tablesA.Select(t => t.Name).ToArray());

        // Area B: 8 double tables (B01 - B08), Capacity 16
        Assert.Equal(8, tablesB.Count);
        Assert.All(tablesB, t => Assert.Equal(2, t.Capacity));
        Assert.Equal(16, tablesB.Sum(t => t.Capacity));
        Assert.Equal(new[] { "B01", "B02", "B03", "B04", "B05", "B06", "B07", "B08" }, tablesB.Select(t => t.Name).ToArray());

        // Area C: 7 double tables (C01 - C07), Capacity 14
        Assert.Equal(7, tablesC.Count);
        Assert.All(tablesC, t => Assert.Equal(2, t.Capacity));
        Assert.Equal(14, tablesC.Sum(t => t.Capacity));
        Assert.Equal(new[] { "C01", "C02", "C03", "C04", "C05", "C06", "C07" }, tablesC.Select(t => t.Name).ToArray());

        // Area D: 7 double (D01 - D07) + 4 four-seat (D08 - D11), Capacity 30
        Assert.Equal(11, tablesD.Count);
        Assert.Equal(30, tablesD.Sum(t => t.Capacity));
        var dDouble = tablesD.Where(t => t.Capacity == 2).ToList();
        var dFour = tablesD.Where(t => t.Capacity == 4).ToList();
        Assert.Equal(7, dDouble.Count);
        Assert.Equal(4, dFour.Count);
        Assert.Equal(new[] { "D01", "D02", "D03", "D04", "D05", "D06", "D07" }, dDouble.Select(t => t.Name).ToArray());
        Assert.Equal(new[] { "D08", "D09", "D10", "D11" }, dFour.Select(t => t.Name).ToArray());

        // Assert - Shapes & QrTokens
        Assert.All(doubleTables, t => Assert.Equal(1, t.Shape)); // Square 80x80
        Assert.All(fourSeatTables, t => Assert.Equal(2, t.Shape)); // Rectangle 120x80
        Assert.All(activeTables, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.QrToken));
            Assert.StartsWith("QR_PIZZA_", t.QrToken);
        });
        Assert.Equal(34, activeTables.Select(t => t.QrToken).Distinct().Count()); // Unique QR tokens
    }

    [Fact]
    public async Task SeedPizzaAreasAndTables_ShouldBeIdempotent_WhenExecutedMultipleTimes()
    {
        // Arrange
        using var context = GetDbContext(nameof(SeedPizzaAreasAndTables_ShouldBeIdempotent_WhenExecutedMultipleTimes));

        // Act - Run 3 times
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);

        // Assert
        var activeAreas = await context.Areas.Where(a => a.IsActive).ToListAsync();
        var activeTables = await context.Tables.Where(t => t.IsActive).ToListAsync();

        Assert.Equal(4, activeAreas.Count);
        Assert.Equal(34, activeTables.Count);
        Assert.Equal(76, activeTables.Sum(t => t.Capacity));
    }

    [Fact]
    public async Task SeedPizzaAreasAndTables_ShouldRemoveLegacyAreasAndTables_AndUnlinkOrders()
    {
        // Arrange
        using var context = GetDbContext(nameof(SeedPizzaAreasAndTables_ShouldRemoveLegacyAreasAndTables_AndUnlinkOrders));
        var legacyArea = new Area { Name = "Tầng 1 - Sảnh Chính", SortOrder = 1, IsActive = true };
        await context.Areas.AddAsync(legacyArea);
        await context.SaveChangesAsync();

        var legacyTable = new Table { AreaId = legacyArea.Id, Name = "Bàn 01", Capacity = 4, Status = 0, QrToken = "LEGACY_QR_1", IsActive = true };
        await context.Tables.AddAsync(legacyTable);
        await context.SaveChangesAsync();

        var order = new Order
        {
            TableId = legacyTable.Id,
            OrderCode = "ORD-LEGACY-001",
            TotalAmount = 250000,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        // Act
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);

        // Assert - Legacy areas and tables must be removed
        var deletedLegacyArea = await context.Areas.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == legacyArea.Id);
        var deletedLegacyTable = await context.Tables.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == legacyTable.Id);
        var existingOrder = await context.Orders.FindAsync(order.Id);

        Assert.Null(deletedLegacyArea);
        Assert.Null(deletedLegacyTable);

        // Order FK must be unlinked safely
        Assert.NotNull(existingOrder);
        Assert.Null(existingOrder.TableId);

        // Active pizza elements are exactly 4 areas and 34 tables
        var activeAreas = await context.Areas.Where(a => a.IsActive).ToListAsync();
        var activeTables = await context.Tables.Where(t => t.IsActive).ToListAsync();
        Assert.Equal(4, activeAreas.Count);
        Assert.Equal(34, activeTables.Count);
    }

    [Fact]
    public async Task SeedPizzaAreasAndTables_ShouldHaveNoCollisionsAcrossAllTablesOnSingleFloor()
    {
        // Arrange
        using var context = GetDbContext(nameof(SeedPizzaAreasAndTables_ShouldHaveNoCollisionsAcrossAllTablesOnSingleFloor));
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);

        var allTables = await context.Tables.Where(t => t.IsActive).ToListAsync();
        Assert.Equal(34, allTables.Count);

        // Act & Assert collision across ALL 34 tables on the unified single-floor canvas
        for (int i = 0; i < allTables.Count; i++)
        {
            var t1 = allTables[i];
            for (int j = i + 1; j < allTables.Count; j++)
            {
                var t2 = allTables[j];

                bool collision = !(t2.PositionX >= t1.PositionX + t1.Width ||
                                   t2.PositionX + t2.Width <= t1.PositionX ||
                                   t2.PositionY >= t1.PositionY + t1.Height ||
                                   t2.PositionY + t2.Height <= t1.PositionY);

                Assert.False(collision, $"Table {t1.Name} ({t1.PositionX}, {t1.PositionY}, {t1.Width}x{t1.Height}) and Table {t2.Name} ({t2.PositionX}, {t2.PositionY}, {t2.Width}x{t2.Height}) collide on the unified floorplan canvas!");
            }
        }
    }

    [Fact]
    public async Task LiveDatabase_SeedPizzaAreasAndTables()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RestaurantDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        using var context = new ApplicationDbContext(options);
        await DbInitializer.SeedPizzaAreasAndTablesAsync(context);

        var activeAreas = await context.Areas.Where(a => a.IsActive).OrderBy(a => a.SortOrder).ToListAsync();
        Assert.Equal(4, activeAreas.Count);

        var activeTables = await context.Tables.Where(t => t.IsActive).ToListAsync();
        foreach (var t in activeTables.OrderBy(x => x.Name))
        {
            _output.WriteLine($"DB TABLE: {t.Name} | AreaId={t.AreaId} | X={t.PositionX}, Y={t.PositionY}, W={t.Width}, H={t.Height}");
        }
        Assert.Equal(34, activeTables.Count);
        Assert.Equal(76, activeTables.Sum(t => t.Capacity));

        // Verify zero collision across all 34 tables in live database
        for (int i = 0; i < activeTables.Count; i++)
        {
            var t1 = activeTables[i];
            for (int j = i + 1; j < activeTables.Count; j++)
            {
                var t2 = activeTables[j];

                bool collision = !(t2.PositionX >= t1.PositionX + t1.Width ||
                                   t2.PositionX + t2.Width <= t1.PositionX ||
                                   t2.PositionY >= t1.PositionY + t1.Height ||
                                   t2.PositionY + t2.Height <= t1.PositionY);

                Assert.False(collision, $"Live Database: Table {t1.Name} and Table {t2.Name} collide on floorplan canvas!");
            }
        }
    }

    [Fact]
    public async Task LiveDatabase_CheckAndRestoreSuppliers()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RestaurantDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        using var context = new ApplicationDbContext(options);
        var allSuppliers = await context.Suppliers.IgnoreQueryFilters().ToListAsync();
        
        // Log all suppliers found
        foreach (var sup in allSuppliers)
        {
            // Reactivate any inactive suppliers
            if (!sup.IsActive)
            {
                sup.IsActive = true;
            }
        }

        // Ensure default suppliers exist
        var defaultSuppliers = new List<(string Name, string Contact, string Phone, string Address)>
        {
            ("Công ty TNHH Thực phẩm Ý Việt", "Nguyễn Văn Hùng", "0901234567", "123 Quận 1, TP.HCM"),
            ("Nông sản Sạch Đà Lạt", "Trần Thị Mai", "0987654321", "45 Phường 3, Đà Lạt"),
            ("Công ty CP Sữa & Phô Mai Anchor Việt Nam", "Lê Hoàng Long", "0918882233", "Khu chế xuất Tân Thuận, Quận 7, TP.HCM"),
            ("Nhà Phân Phối Hải Sản & Thịt Tươi Biển Đông", "Phạm Văn Tuấn", "0934567890", "Cảng cá Hạ Long, Quảng Ninh")
        };

        foreach (var def in defaultSuppliers)
        {
            var exists = allSuppliers.FirstOrDefault(s => s.Name == def.Name);
            if (exists == null)
            {
                var newSup = new Supplier
                {
                    Name = def.Name,
                    ContactPerson = def.Contact,
                    PhoneNumber = def.Phone,
                    Address = def.Address,
                    IsActive = true
                };
                await context.Suppliers.AddAsync(newSup);
            }
            else
            {
                exists.IsActive = true;
            }
        }

        await context.SaveChangesAsync();

        var activeSuppliers = await context.Suppliers.Where(s => s.IsActive).ToListAsync();
        Assert.True(activeSuppliers.Count >= 2);
    }
}

