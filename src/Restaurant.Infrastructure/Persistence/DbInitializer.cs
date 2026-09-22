using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;

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

        // 3. Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Khai vị (Appetizers)", Description = "Món ăn nhẹ khởi đầu bữa tiệc", SortOrder = 1, IsActive = true },
                new Category { Name = "Pizza Ý Thượng hạng", Description = "Pizza nướng lò củi bột ủ 48 giờ", SortOrder = 2, IsActive = true },
                new Category { Name = "Mì Ý (Pasta)", Description = "Mì Ý tươi xào sốt truyền thống", SortOrder = 3, IsActive = true },
                new Category { Name = "Đồ uống & Tráng miệng", Description = "Trà tươi, Cà phê & Bánh ngọt Ý", SortOrder = 4, IsActive = true },
                new Category { Name = "Combo Ưu đãi", Description = "Set đồ ăn tiết kiệm cho gia đình & nhóm", SortOrder = 5, IsActive = true }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

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
                new MenuItem { CategoryId = catPizza?.Id ?? 1, Name = "Pizza Margherita Mozzarella", Description = "Phô mai Mozzarella tươi, sốt cà chua San Marzano, lá Basil", Price = 180000, SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500" },
                new MenuItem { CategoryId = catPizza?.Id ?? 1, Name = "Pizza Pepperoni Xúc xích Ý", Description = "Xúc xích Pepperoni cay nồng, lớp phô mai nung chảy", Price = 210000, SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500" },
                new MenuItem { CategoryId = catPasta?.Id ?? 1, Name = "Mì Ý Bolognese Thịt bò Băm", Description = "Mì Spagheti sốt bò băm đút lò kiểu Ý", Price = 145000, SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1551183053-bf91a1d81141?w=500" },
                new MenuItem { CategoryId = catKhaiVi?.Id ?? 1, Name = "Salad Caesar Gà Nướng", Description = "Rau xà lách Romaine, gà nướng tiêu đen, sốt Caesar", Price = 115000, SortOrder = 1, IsActive = true },
                new MenuItem { CategoryId = catDrink?.Id ?? 1, Name = "Trà Vải Khiếm Thị Đào Basil", Description = "Trà đen thanh mát kết hợp quả vải ngọt ngào & lá Basil tươi", Price = 45000, SortOrder = 1, IsActive = true },
                new MenuItem { CategoryId = catCombo?.Id ?? 1, Name = "Combo Gia Đình Pizza & Pasta", Description = "1 Pizza Margherita + 1 Mì Ý Bolognese + 2 Trà Vải", Price = 349000, IsCombo = true, SortOrder = 1, IsActive = true }
            };

            await context.MenuItems.AddRangeAsync(menuItems);
            await context.SaveChangesAsync();
        }

        // 5. ModifierGroups & ModifierOptions
        if (!await context.ModifierGroups.AnyAsync())
        {
            var sizeGroup = new ModifierGroup { Name = "Chọn Kích thước Pizza", IsRequired = true, MinSelections = 1, MaxSelections = 1, SortOrder = 1 };
            var toppingGroup = new ModifierGroup { Name = "Thêm Topping", IsRequired = false, MinSelections = 0, MaxSelections = 3, SortOrder = 2 };

            await context.ModifierGroups.AddRangeAsync(sizeGroup, toppingGroup);
            await context.SaveChangesAsync();

            var options = new List<ModifierOption>
            {
                new ModifierOption { ModifierGroupId = sizeGroup.Id, Name = "Size M (25cm)", ExtraPrice = 0, IsDefault = true, SortOrder = 1, IsActive = true },
                new ModifierOption { ModifierGroupId = sizeGroup.Id, Name = "Size L (30cm)", ExtraPrice = 45000, IsDefault = false, SortOrder = 2, IsActive = true },
                new ModifierOption { ModifierGroupId = toppingGroup.Id, Name = "Thêm Phô mai Mozzarella", ExtraPrice = 30000, SortOrder = 1, IsActive = true },
                new ModifierOption { ModifierGroupId = toppingGroup.Id, Name = "Thêm Xúc xích Ý", ExtraPrice = 35000, SortOrder = 2, IsActive = true }
            };

            await context.ModifierOptions.AddRangeAsync(options);
            await context.SaveChangesAsync();
        }

        // 6. Areas & Tables
        if (!await context.Areas.AnyAsync())
        {
            var area1 = new Area { Name = "Tầng 1 - Sảnh Chính", SortOrder = 1, IsActive = true };
            var area2 = new Area { Name = "Tầng 2 - Sân Thượng", SortOrder = 2, IsActive = true };
            var area3 = new Area { Name = "Sân Vườn VIP", SortOrder = 3, IsActive = true };

            await context.Areas.AddRangeAsync(area1, area2, area3);
            await context.SaveChangesAsync();

            var tables = new List<Table>
            {
                new Table { AreaId = area1.Id, Name = "Bàn 01", Capacity = 4, Status = 0, QrToken = "QR_TOKEN_TABLE_01_SECURE_TOKEN_32", PositionX = 50, PositionY = 50, Shape = 1, SortOrder = 1, IsActive = true },
                new Table { AreaId = area1.Id, Name = "Bàn 02", Capacity = 4, Status = 1, QrToken = "QR_TOKEN_TABLE_02_SECURE_TOKEN_32", PositionX = 180, PositionY = 50, Shape = 1, SortOrder = 2, IsActive = true },
                new Table { AreaId = area1.Id, Name = "Bàn 03 (VÍP)", Capacity = 6, Status = 2, QrToken = "QR_TOKEN_TABLE_03_SECURE_TOKEN_32", PositionX = 310, PositionY = 50, Shape = 2, Width = 120, SortOrder = 3, IsActive = true },
                new Table { AreaId = area2.Id, Name = "Bàn 11 (Sky View)", Capacity = 2, Status = 0, QrToken = "QR_TOKEN_TABLE_11_SECURE_TOKEN_32", PositionX = 50, PositionY = 150, Shape = 0, SortOrder = 1, IsActive = true },
                new Table { AreaId = area2.Id, Name = "Bàn 12 (Sky View)", Capacity = 4, Status = 3, QrToken = "QR_TOKEN_TABLE_12_SECURE_TOKEN_32", PositionX = 180, PositionY = 150, Shape = 1, SortOrder = 2, IsActive = true }
            };

            await context.Tables.AddRangeAsync(tables);
            await context.SaveChangesAsync();
        }

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
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Bột mì Tipo 00", CurrentStock = 120.5m, MinStockLevel = 30m, CostPerUnit = 25000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Phô mai Mozzarella Fresh", CurrentStock = 14.2m, MinStockLevel = 20m, CostPerUnit = 180000, IsActive = true },
                new Material { UnitOfMeasureId = unitLit.Id, Name = "Sốt cà chua San Marzano", CurrentStock = 45.0m, MinStockLevel = 15m, CostPerUnit = 45000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Lá húng tây Basil tươi", CurrentStock = 1.8m, MinStockLevel = 2.0m, CostPerUnit = 120000, IsActive = true },
                new Material { UnitOfMeasureId = unitKg.Id, Name = "Thịt bò Xay đút lò", CurrentStock = 28.5m, MinStockLevel = 10m, CostPerUnit = 220000, IsActive = true }
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
    }
}
