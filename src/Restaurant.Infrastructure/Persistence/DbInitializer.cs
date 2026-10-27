using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;

namespace Restaurant.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Chủ nhà hàng - quyền cao nhất" },
                new Role { Name = "Manager", Description = "Quản lý sảnh - quản trị menu, bàn" },
                new Role { Name = "Cashier", Description = "Thu ngân - POS, thanh toán" },
                new Role { Name = "Chef", Description = "Bếp - màn hình KDS" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // 2. Users
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
        var cashierRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Cashier");
        var chefRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Chef");

        if (adminRole != null && !await context.Users.AnyAsync(u => u.EmployeeCode == "NV000000"))
        {
            await context.Users.AddAsync(new User
            {
                Username = "admin",
                FullName = "Quản trị viên Hệ thống",
                RoleId = adminRole.Id,
                EmployeeCode = "NV000000",
                PhoneNumber = "0912345678",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsActive = true
            });
        }

        if (managerRole != null && !await context.Users.AnyAsync(u => u.Username == "manager01"))
        {
            await context.Users.AddAsync(new User
            {
                Username = "manager01",
                FullName = "Nguyễn Hoàng Nam (Quản lý)",
                RoleId = managerRole.Id,
                EmployeeCode = "NV000003",
                PhoneNumber = "0934567890",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
                IsActive = true
            });
        }

        if (cashierRole != null && !await context.Users.AnyAsync(u => u.Username == "cashier_quick"))
        {
            await context.Users.AddAsync(new User
            {
                Username = "cashier_quick",
                FullName = "Lê Thị Thu (Thu ngân)",
                RoleId = cashierRole.Id,
                EmployeeCode = "NV000001",
                PhoneNumber = "0945678901",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cashier123!"),
                IsActive = true
            });
        }

        if (chefRole != null && !await context.Users.AnyAsync(u => u.Username == "chef_quick"))
        {
            await context.Users.AddAsync(new User
            {
                Username = "chef_quick",
                FullName = "Phạm Quốc Bảo (Bếp trưởng)",
                RoleId = chefRole.Id,
                EmployeeCode = "NV000002",
                PhoneNumber = "0956789012",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Chef123!"),
                IsActive = true
            });
        }

        await context.SaveChangesAsync();

        // 2.1 Registers
        if (!await context.Registers.AnyAsync())
        {
            var defaultRegister = new Register
            {
                Code = "REG01",
                Name = "Máy POS Thu Ngân 01",
                IsActive = true
            };
            await context.Registers.AddAsync(defaultRegister);
            await context.SaveChangesAsync();
        }

        // 2.2 Default Shift
        if (!await context.Shifts.AnyAsync())
        {
            var defaultUser = await context.Users.FirstOrDefaultAsync(u => u.EmployeeCode == "NV000001")
                              ?? await context.Users.FirstOrDefaultAsync();
            var register = await context.Registers.FirstOrDefaultAsync();
            if (defaultUser != null)
            {
                var defaultShift = new Shift
                {
                    UserId = defaultUser.Id,
                    RegisterId = register?.Id,
                    OpeningBalance = 1000000m,
                    Status = (int)ShiftStatus.Open,
                    OpenedAt = DateTime.UtcNow,
                    Note = "Ca đầu tiên tự động mở"
                };
                await context.Shifts.AddAsync(defaultShift);
                await context.SaveChangesAsync();
            }
        }

        // 3. Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Khai vị (Appetizers)", NameEn = "Appetizers", Description = "Món ăn nhẹ khởi đầu bữa tiệc", DescriptionEn = "Light appetizers to start your meal", SortOrder = 1, IsActive = true },
                new Category { Name = "Pizza Truyền Thống", NameEn = "Pizza", Description = "Pizza nướng lò củi bột ủ 48 giờ", DescriptionEn = "Wood-fired pizza with 48h fermented dough", SortOrder = 2, IsActive = true },
                new Category { Name = "Mì Ý (Pasta)", NameEn = "Italian Pasta", Description = "Mì Ý tươi xào sốt truyền thống", DescriptionEn = "Fresh pasta tossed in traditional sauces", SortOrder = 3, IsActive = true },
                new Category { Name = "Đồ uống & Tráng miệng", NameEn = "Drinks & Desserts", Description = "Trà tươi, Cà phê & Bánh ngọt Ý", DescriptionEn = "Fresh tea, coffee & Italian desserts", SortOrder = 4, IsActive = true },
                new Category { Name = "Combo Ưu đãi", NameEn = "Special Combos", Description = "Set đồ ăn tiết kiệm cho gia đình & nhóm", DescriptionEn = "Value meal sets for families & groups", SortOrder = 5, IsActive = true }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 3.1 KitchenStations
        if (!await context.KitchenStations.AnyAsync())
        {
            var defaultStation = new KitchenStation
            {
                Code = "MAIN",
                Name = "Main Kitchen",
                SortOrder = 1,
                IsActive = true
            };
            await context.KitchenStations.AddAsync(defaultStation);
            await context.SaveChangesAsync();
        }

        var mainStation = await context.KitchenStations.FirstOrDefaultAsync(s => s.Code == "MAIN");

        // 4. MenuItems & Modifiers
        if (!await context.MenuItems.AnyAsync())
        {
            var catKhaiVi = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Khai vị"));
            var catPizza = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Pizza"));
            var catPasta = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Mì Ý"));
            var catDrink = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Đồ uống"));
            var catCombo = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Combo"));

            var menuItems = new List<MenuItem>
            {
                new MenuItem { CategoryId = catPizza?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Pizza Margherita Mozzarella", NameEn = "Margherita Mozzarella Pizza", Description = "Phô mai Mozzarella tươi, sốt cà chua San Marzano, lá Basil", DescriptionEn = "Fresh Mozzarella, San Marzano tomato sauce, fresh Basil", Price = 180000, SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500" },
                new MenuItem { CategoryId = catPizza?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Pizza Pepperoni Xúc xích Ý", NameEn = "Italian Pepperoni Pizza", Description = "Xúc xích Pepperoni cay nồng, lớp phô mai nung chảy", DescriptionEn = "Spicy Pepperoni sausage with melted cheese", Price = 210000, SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500" },
                new MenuItem { CategoryId = catPasta?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Mì Ý Bolognese Thịt bò Băm", NameEn = "Spaghetti Bolognese", Description = "Mì Spagheti sốt bò băm đút lò kiểu Ý", DescriptionEn = "Classic Italian minced beef spaghetti", Price = 145000, SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1551183053-bf91a1d81141?w=500" },
                new MenuItem { CategoryId = catKhaiVi?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Salad Caesar Gà Nướng", NameEn = "Grilled Chicken Caesar Salad", Description = "Rau xà lách Romaine, gà nướng tiêu đen, sốt Caesar", DescriptionEn = "Romaine lettuce, black pepper grilled chicken, Caesar dressing", Price = 115000, SortOrder = 1, IsActive = true },
                new MenuItem { CategoryId = catDrink?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Trà Vải Khiếm Thị Đào Basil", NameEn = "Lychee Peach Basil Tea", Description = "Trà đen thanh mát kết hợp quả vải ngọt ngào & lá Basil tươi", DescriptionEn = "Refreshing black tea blended with sweet lychee & fresh basil", Price = 45000, SortOrder = 1, IsActive = true },
                new MenuItem { CategoryId = catCombo?.Id ?? 1, KitchenStationId = mainStation?.Id, Name = "Combo Gia Đình Pizza & Pasta", NameEn = "Family Pizza & Pasta Combo", Description = "1 Pizza Margherita + 1 Mì Ý Bolognese + 2 Trà Vải", DescriptionEn = "1 Margherita Pizza + 1 Spaghetti Bolognese + 2 Lychee Teas", Price = 349000, IsCombo = true, SortOrder = 1, IsActive = true }
            };

            await context.MenuItems.AddRangeAsync(menuItems);
            await context.SaveChangesAsync();
        }

        // 5. ModifierGroups & ModifierOptions
        if (!await context.ModifierGroups.AnyAsync())
        {
            var sizeGroup = new ModifierGroup { Name = "Chọn Kích thước Pizza", NameEn = "Select Pizza Size", IsRequired = true, MinSelections = 1, MaxSelections = 1, SortOrder = 1 };
            var toppingGroup = new ModifierGroup { Name = "Thêm Topping", NameEn = "Extra Toppings", IsRequired = false, MinSelections = 0, MaxSelections = 3, SortOrder = 2 };

            await context.ModifierGroups.AddRangeAsync(sizeGroup, toppingGroup);
            await context.SaveChangesAsync();

            var options = new List<ModifierOption>
            {
                new ModifierOption { ModifierGroupId = sizeGroup.Id, Name = "Size M (25cm)", NameEn = "Medium Size (25cm)", ExtraPrice = 0, IsDefault = true, SortOrder = 1, IsActive = true },
                new ModifierOption { ModifierGroupId = sizeGroup.Id, Name = "Size L (30cm)", NameEn = "Large Size (30cm)", ExtraPrice = 45000, IsDefault = false, SortOrder = 2, IsActive = true },
                new ModifierOption { ModifierGroupId = toppingGroup.Id, Name = "Thêm Phô mai Mozzarella", NameEn = "Extra Mozzarella Cheese", ExtraPrice = 30000, SortOrder = 1, IsActive = true },
                new ModifierOption { ModifierGroupId = toppingGroup.Id, Name = "Thêm Xúc xích Ý", NameEn = "Extra Italian Pepperoni", ExtraPrice = 35000, SortOrder = 2, IsActive = true }
            };

            await context.ModifierOptions.AddRangeAsync(options);
            await context.SaveChangesAsync();
        }

        // 6. Areas & Tables (Pizza Restaurant: 1 Floor, 4 Areas A-D, 34 Tables, 76 Total Capacity)
        await SeedPizzaAreasAndTablesAsync(context);


        // 7. UnitOfMeasures & Materials
        if (!await context.UnitOfMeasures.AnyAsync())
        {
            var unitKg = new UnitOfMeasure { Name = "Kilogram", Abbreviation = "Kg" };
            var unitLit = new UnitOfMeasure { Name = "Lít", Abbreviation = "L" };
            var unitHop = new UnitOfMeasure { Name = "Hộp / Lon", Abbreviation = "Hộp" };

            await context.UnitOfMeasures.AddRangeAsync(unitKg, unitLit, unitHop);
            await context.SaveChangesAsync();

            var materials = new List<Material>
            {
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Bột mì Tipo 00", NameEn = "Tipo 00 Flour", CurrentStock = 120.5m, MinStockLevel = 30m, CostPerUnit = 25000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Phô mai Mozzarella Fresh", NameEn = "Fresh Mozzarella Cheese", CurrentStock = 14.2m, MinStockLevel = 20m, CostPerUnit = 180000, IsActive = true },
                new Material { UnitOfMeasureId = unitLit.Id, Name = "Sốt cà chua San Marzano", NameEn = "San Marzano Tomato Sauce", CurrentStock = 45.0m, MinStockLevel = 15m, CostPerUnit = 45000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Lá húng tây Basil tươi", NameEn = "Fresh Basil Leaves", CurrentStock = 1.8m, MinStockLevel = 2.0m, CostPerUnit = 120000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Thịt bò Xay đút lò", NameEn = "Minced Beef", CurrentStock = 28.5m, MinStockLevel = 10m, CostPerUnit = 220000, IsActive = true }
            };

            await context.Materials.AddRangeAsync(materials);
            await context.SaveChangesAsync();
        }

        // 8. Suppliers
        if (!await context.Suppliers.AnyAsync())
        {
            var suppliers = new List<Supplier>
            {
                new Supplier { Name = "Công ty TNHH Thực phẩm Ý Việt", ContactPerson = "Nguyễn Văn Hùng", PhoneNumber = "0901234567", Address = "123 Quận 1, TP.HCM", IsActive = true },
                new Supplier { Name = "Nông sản Sạch Đà Lạt", ContactPerson = "Trần Thị Mai", PhoneNumber = "0987654321", Address = "45 Phường 3, Đà Lạt", IsActive = true }
            };
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }

        // 9. Customers & CustomerTiers
        if (!await context.CustomerTiers.AnyAsync())
        {
            var tierBronze = new CustomerTier { Name = "Thành viên Bạc", MinPoints = 0, DiscountPercent = 0 };
            var tierGold = new CustomerTier { Name = "Thành viên Vàng (Gold)", MinPoints = 500, DiscountPercent = 5 };
            var tierDiamond = new CustomerTier { Name = "Thành viên Kim Cương (Diamond)", MinPoints = 2000, DiscountPercent = 10 };

            await context.CustomerTiers.AddRangeAsync(tierBronze, tierGold, tierDiamond);
            await context.SaveChangesAsync();

            var customers = new List<Customer>
            {
                new Customer { FullName = "Nguyễn Văn An", PhoneNumber = "0909123456", TierId = tierGold.Id, TotalPoints = 1250, AvailablePoints = 450 },
                new Customer { FullName = "Trần Thị Bích", PhoneNumber = "0918234567", TierId = tierBronze.Id, TotalPoints = 320, AvailablePoints = 220 },
                new Customer { FullName = "Hoàng Minh Trí", PhoneNumber = "0933345678", TierId = tierDiamond.Id, TotalPoints = 3400, AvailablePoints = 1100 }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        // 10. Vouchers
        if (!await context.Vouchers.AnyAsync())
        {
            var vouchers = new List<Voucher>
            {
                new Voucher { Code = "MOZZARELLA10", DiscountType = 1, DiscountValue = 10, MinOrderAmount = 200000, MaxDiscountAmount = 50000, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(30), UsageLimit = 100, UsedCount = 24, IsActive = true },
                new Voucher { Code = "WELCOME50K", DiscountType = 2, DiscountValue = 50000, MinOrderAmount = 300000, MaxDiscountAmount = 50000, StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(15), UsageLimit = 50, UsedCount = 18, IsActive = true }
            };
            await context.Vouchers.AddRangeAsync(vouchers);
            await context.SaveChangesAsync();
        }

        // 11. Backfill English fields for pre-existing database records
        var existingCategories = await context.Categories.Where(c => c.NameEn == null).ToListAsync();
        foreach (var c in existingCategories)
        {
            if (c.Name.Contains("Khai vị")) { c.NameEn = "Appetizers"; c.DescriptionEn ??= "Light appetizers to start your meal"; }
            else if (c.Name.Contains("Pizza")) { c.NameEn = "Premium Italian Pizza"; c.DescriptionEn ??= "Wood-fired pizza with 48h fermented dough"; }
            else if (c.Name.Contains("Mì Ý")) { c.NameEn = "Italian Pasta"; c.DescriptionEn ??= "Fresh pasta tossed in traditional sauces"; }
            else if (c.Name.Contains("Đồ uống")) { c.NameEn = "Drinks & Desserts"; c.DescriptionEn ??= "Fresh tea, coffee & Italian desserts"; }
            else if (c.Name.Contains("Combo")) { c.NameEn = "Special Combos"; c.DescriptionEn ??= "Value meal sets for families & groups"; }
            else { c.NameEn = c.Name; }
        }

        var existingMenuItems = await context.MenuItems.Where(m => m.NameEn == null).ToListAsync();
        foreach (var m in existingMenuItems)
        {
            if (m.Name.Contains("Margherita")) { m.NameEn = "Margherita Mozzarella Pizza"; m.DescriptionEn ??= "Fresh Mozzarella, San Marzano tomato sauce, fresh Basil"; }
            else if (m.Name.Contains("Pepperoni")) { m.NameEn = "Italian Pepperoni Pizza"; m.DescriptionEn ??= "Spicy Pepperoni sausage with melted cheese"; }
            else if (m.Name.Contains("Bolognese")) { m.NameEn = "Spaghetti Bolognese"; m.DescriptionEn ??= "Classic Italian minced beef spaghetti"; }
            else if (m.Name.Contains("Salad")) { m.NameEn = "Grilled Chicken Caesar Salad"; m.DescriptionEn ??= "Romaine lettuce, black pepper grilled chicken, Caesar dressing"; }
            else if (m.Name.Contains("Trà Vải")) { m.NameEn = "Lychee Peach Basil Tea"; m.DescriptionEn ??= "Refreshing black tea blended with sweet lychee & fresh basil"; }
            else if (m.Name.Contains("Combo")) { m.NameEn = "Family Pizza & Pasta Combo"; m.DescriptionEn ??= "1 Margherita Pizza + 1 Spaghetti Bolognese + 2 Lychee Teas"; }
            else { m.NameEn = m.Name; }
        }

        var existingModGroups = await context.ModifierGroups.Where(mg => mg.NameEn == null).ToListAsync();
        foreach (var mg in existingModGroups)
        {
            if (mg.Name.Contains("Kích thước")) mg.NameEn = "Select Pizza Size";
            else if (mg.Name.Contains("Topping")) mg.NameEn = "Extra Toppings";
            else mg.NameEn = mg.Name;
        }

        var existingModOptions = await context.ModifierOptions.Where(o => o.NameEn == null).ToListAsync();
        foreach (var o in existingModOptions)
        {
            if (o.Name.Contains("Size M")) o.NameEn = "Medium Size (25cm)";
            else if (o.Name.Contains("Size L")) o.NameEn = "Large Size (30cm)";
            else if (o.Name.Contains("Phô mai")) o.NameEn = "Extra Mozzarella Cheese";
            else if (o.Name.Contains("Xúc xích")) o.NameEn = "Extra Italian Pepperoni";
            else o.NameEn = o.Name;
        }

        var existingAreas = await context.Areas.Where(a => a.NameEn == null).ToListAsync();
        foreach (var a in existingAreas)
        {
            if (a.Name.Contains("Tầng 1")) a.NameEn = "1st Floor - Main Hall";
            else if (a.Name.Contains("Tầng 2")) a.NameEn = "2nd Floor - Rooftop";
            else if (a.Name.Contains("Sân Vườn")) a.NameEn = "VIP Garden";
            else a.NameEn = a.Name;
        }

        var existingMaterials = await context.Materials.Where(m => m.NameEn == null).ToListAsync();
        foreach (var mat in existingMaterials)
        {
            if (mat.Name.Contains("Bột mì")) mat.NameEn = "Tipo 00 Flour";
            else if (mat.Name.Contains("Phô mai")) mat.NameEn = "Fresh Mozzarella Cheese";
            else if (mat.Name.Contains("Sốt cà chua")) mat.NameEn = "San Marzano Tomato Sauce";
            else if (mat.Name.Contains("Lá húng tây")) mat.NameEn = "Fresh Basil Leaves";
            else if (mat.Name.Contains("Thịt bò")) mat.NameEn = "Minced Beef";
            else mat.NameEn = mat.Name;
        }

        if (mainStation != null)
        {
            var unassignedItems = await context.MenuItems.Where(m => m.KitchenStationId == null).ToListAsync();
            foreach (var item in unassignedItems)
            {
                item.KitchenStationId = mainStation.Id;
            }
        }

        await context.SaveChangesAsync();

        // 13. Pizza Restaurant Complete Sample Data (POS + KDS + Combo + Inventory + BOM)
        await SampleDataSeeder.SeedSampleRestaurantDataAsync(context);
    }

    public static async Task SeedPizzaAreasAndTablesAsync(ApplicationDbContext context)
    {
        // Permanently remove legacy demo areas and tables if they exist
        var validAreaNames = new[] { "Khu A", "Khu B", "Khu C", "Khu D" };
        var legacyAreas = await context.Areas.IgnoreQueryFilters().Where(a => !validAreaNames.Contains(a.Name)).ToListAsync();
        if (legacyAreas.Count != 0)
        {
            var legacyAreaIds = legacyAreas.Select(a => a.Id).ToList();
            var legacyTables = await context.Tables.IgnoreQueryFilters().Where(t => legacyAreaIds.Contains(t.AreaId)).ToListAsync();
            var legacyTableIds = legacyTables.Select(t => t.Id).ToList();

            // Unlink any orders referencing legacy tables
            var ordersWithLegacyTables = await context.Orders.Where(o => o.TableId != null && legacyTableIds.Contains(o.TableId.Value)).ToListAsync();
            foreach (var o in ordersWithLegacyTables)
            {
                o.TableId = null;
            }

            context.Tables.RemoveRange(legacyTables);
            context.Areas.RemoveRange(legacyAreas);
            await context.SaveChangesAsync();
        }

        // Define the 4 standard pizza areas for 1 Floor
        var areaDefinitions = new[]
        {
            new { Name = "Khu A", NameEn = "Area A", SortOrder = 1 },
            new { Name = "Khu B", NameEn = "Area B", SortOrder = 2 },
            new { Name = "Khu C", NameEn = "Area C", SortOrder = 3 },
            new { Name = "Khu D", NameEn = "Area D", SortOrder = 4 }
        };

        var areaEntityMap = new Dictionary<string, Area>();
        foreach (var def in areaDefinitions)
        {
            var area = await context.Areas.FirstOrDefaultAsync(a => a.Name == def.Name);
            if (area == null)
            {
                area = new Area
                {
                    Name = def.Name,
                    NameEn = def.NameEn,
                    SortOrder = def.SortOrder,
                    IsActive = true
                };
                await context.Areas.AddAsync(area);
                await context.SaveChangesAsync();
            }
            else
            {
                bool modified = false;
                if (!area.IsActive) { area.IsActive = true; modified = true; }
                if (area.NameEn != def.NameEn) { area.NameEn = def.NameEn; modified = true; }
                if (area.SortOrder != def.SortOrder) { area.SortOrder = def.SortOrder; modified = true; }
                if (modified)
                {
                    await context.SaveChangesAsync();
                }
            }
            areaEntityMap[def.Name] = area;
        }

        // Define the 34 tables unified single-floor layout matching the architectural blueprint
        var tableDefinitions = new List<(string AreaName, string TableName, int Capacity, int Shape, int Width, int Height, double X, double Y, int SortOrder)>
        {
            // === KHU A: Sảnh Chính (8 bàn đôi, capacity 2 - 2 hàng x 4 bàn gọn trong block) ===
            // Hàng trên (y=135)
            ("Khu A", "A01", 2, 1, 65, 65, 390, 135, 1),
            ("Khu A", "A02", 2, 1, 65, 65, 495, 135, 2),
            ("Khu A", "A03", 2, 1, 65, 65, 600, 135, 3),
            ("Khu A", "A04", 2, 1, 65, 65, 705, 135, 4),
            // Hàng dưới (y=225)
            ("Khu A", "A05", 2, 1, 65, 65, 390, 225, 5),
            ("Khu A", "A06", 2, 1, 65, 65, 495, 225, 6),
            ("Khu A", "A07", 2, 1, 65, 65, 600, 225, 7),
            ("Khu A", "A08", 2, 1, 65, 65, 705, 225, 8),

            // === KHU B: Phòng Trung Tâm (8 bàn đôi, capacity 2 - 2 hàng x 4 bàn gọn trong block) ===
            // Hàng trên (y=375)
            ("Khu B", "B01", 2, 1, 65, 65, 410, 375, 1),
            ("Khu B", "B02", 2, 1, 65, 65, 495, 375, 2),
            ("Khu B", "B03", 2, 1, 65, 65, 580, 375, 3),
            ("Khu B", "B04", 2, 1, 65, 65, 665, 375, 4),
            // Hàng dưới (y=445)
            ("Khu B", "B05", 2, 1, 65, 65, 410, 445, 5),
            ("Khu B", "B06", 2, 1, 65, 65, 495, 445, 6),
            ("Khu B", "B07", 2, 1, 65, 65, 580, 445, 7),
            ("Khu B", "B08", 2, 1, 65, 65, 665, 445, 8),

            // === KHU C: View Cửa Sổ (7 bàn đôi, capacity 2 - 3 hàng gọn trong block) ===
            // Hàng 1 (y=320)
            ("Khu C", "C01", 2, 1, 60, 60, 930, 320, 1),
            ("Khu C", "C02", 2, 1, 60, 60, 1040, 320, 2),
            // Hàng 2 (y=390)
            ("Khu C", "C03", 2, 1, 60, 60, 900, 390, 3),
            ("Khu C", "C04", 2, 1, 60, 60, 985, 390, 4),
            ("Khu C", "C05", 2, 1, 60, 60, 1070, 390, 5),
            // Hàng 3 (y=455)
            ("Khu C", "C06", 2, 1, 60, 60, 930, 455, 6),
            ("Khu C", "C07", 2, 1, 60, 60, 1040, 455, 7),

            // === KHU D: Hiên Ngoài Trời / Sân Vườn (11 bàn - Lối vào ở giữa thông thoáng) ===
            // Cánh trái (5 bàn: 2 bàn 4 chỗ + 3 bàn 2 chỗ)
            ("Khu D", "D08", 4, 2, 80, 55, 135, 565, 8),
            ("Khu D", "D09", 4, 2, 80, 55, 225, 565, 9),
            ("Khu D", "D01", 2, 1, 55, 55, 315, 565, 1),
            ("Khu D", "D02", 2, 1, 55, 55, 380, 565, 2),
            ("Khu D", "D03", 2, 1, 55, 55, 445, 565, 3),
            // Cánh phải (6 bàn: 2 bàn 4 chỗ + 4 bàn 2 chỗ)
            ("Khu D", "D10", 4, 2, 75, 55, 745, 565, 10),
            ("Khu D", "D11", 4, 2, 75, 55, 830, 565, 11),
            ("Khu D", "D04", 2, 1, 50, 55, 915, 565, 4),
            ("Khu D", "D05", 2, 1, 50, 55, 975, 565, 5),
            ("Khu D", "D06", 2, 1, 50, 55, 1035, 565, 6),
            ("Khu D", "D07", 2, 1, 50, 55, 1095, 565, 7)
        };

        bool hasChanges = false;
        foreach (var def in tableDefinitions)
        {
            var area = areaEntityMap[def.AreaName];
            var existingTable = await context.Tables.FirstOrDefaultAsync(t => t.AreaId == area.Id && t.Name == def.TableName);
            if (existingTable == null)
            {
                var newTable = new Table
                {
                    AreaId = area.Id,
                    Name = def.TableName,
                    Capacity = def.Capacity,
                    Status = 0, // Available
                    QrToken = $"QR_PIZZA_{def.TableName}_{Guid.NewGuid():N}",
                    PositionX = def.X,
                    PositionY = def.Y,
                    Width = def.Width,
                    Height = def.Height,
                    Shape = def.Shape,
                    Rotation = 0,
                    SortOrder = def.SortOrder,
                    IsActive = true
                };
                await context.Tables.AddAsync(newTable);
                hasChanges = true;
            }
            else
            {
                // Synchronize coordinates and dimensions to architectural floorplan
                existingTable.PositionX = def.X;
                existingTable.PositionY = def.Y;
                existingTable.Width = def.Width;
                existingTable.Height = def.Height;
                existingTable.Shape = def.Shape;
                existingTable.Capacity = def.Capacity;
                existingTable.SortOrder = def.SortOrder;
                existingTable.IsActive = true;
                if (string.IsNullOrEmpty(existingTable.QrToken))
                {
                    existingTable.QrToken = $"QR_PIZZA_{def.TableName}_{Guid.NewGuid():N}";
                }
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
        }
    }
}

