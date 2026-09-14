using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var adminRole = new Role { Name = "Admin", Description = "Chủ nhà hàng - quyền cao nhất" };
            var managerRole = new Role { Name = "Manager", Description = "Quản lý sảnh - quản trị menu, bàn" };
            var cashierRole = new Role { Name = "Cashier", Description = "Thu ngân - POS, thanh toán" };
            var chefRole = new Role { Name = "Chef", Description = "Bếp - màn hình KDS" };

            await context.Roles.AddRangeAsync(adminRole, managerRole, cashierRole, chefRole);
            await context.SaveChangesAsync();

            if (!await context.Users.AnyAsync(u => u.EmployeeCode == "NV000000"))
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
                await context.Users.AddRangeAsync(adminUser, cashierUser, chefUser);
                await context.SaveChangesAsync();
            }
        }
    }
}
