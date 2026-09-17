using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var newAdminRole = new Role { Name = "Admin", Description = "Chủ nhà hàng - quyền cao nhất" };
            var newManagerRole = new Role { Name = "Manager", Description = "Quản lý sảnh - quản trị menu, bàn" };
            var newCashierRole = new Role { Name = "Cashier", Description = "Thu ngân - POS, thanh toán" };
            var newChefRole = new Role { Name = "Chef", Description = "Bếp - màn hình KDS" };

            await context.Roles.AddRangeAsync(newAdminRole, newManagerRole, newCashierRole, newChefRole);
            await context.SaveChangesAsync();
        }

        // Tách kiểm tra User ra độc lập cho TỪNG tài khoản
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var cashierRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Cashier");
        var chefRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Chef");

        if (adminRole != null && !await context.Users.AnyAsync(u => u.EmployeeCode == "NV000000"))
        {
            var adminUser = new User
            {
                Username = "admin",
                FullName = "Quản trị viên",
                RoleId = adminRole.Id,
                EmployeeCode = "NV000000",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsActive = true
            };
            await context.Users.AddAsync(adminUser);
        }

        if (cashierRole != null && !await context.Users.AnyAsync(u => u.Username == "cashier_quick"))
        {
            var cashierUser = new User
            {
                Username = "cashier_quick",
                FullName = "Thu Ngân (QuickLogin)",
                RoleId = cashierRole.Id,
                EmployeeCode = "NV000001",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cashier123!"),
                IsActive = true
            };
            await context.Users.AddAsync(cashierUser);
        }

        if (chefRole != null && !await context.Users.AnyAsync(u => u.Username == "chef_quick"))
        {
            var chefUser = new User
            {
                Username = "chef_quick",
                FullName = "Bếp (QuickLogin)",
                RoleId = chefRole.Id,
                EmployeeCode = "NV000002",
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Chef123!"),
                IsActive = true
            };
            await context.Users.AddAsync(chefUser);
        }

        await context.SaveChangesAsync();
    }
}
