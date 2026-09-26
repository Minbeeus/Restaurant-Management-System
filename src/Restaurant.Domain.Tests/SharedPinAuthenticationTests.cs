using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Restaurant.Application.Common;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class SharedPinAuthenticationTests
{
    private ApplicationDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private (User cashier, User chef, User manager, User admin) SeedUsers(ApplicationDbContext context)
    {
        var cashierRole = new Role { Id = 1, Name = "Cashier" };
        var chefRole = new Role { Id = 2, Name = "Chef" };
        var managerRole = new Role { Id = 3, Name = "Manager" };
        var adminRole = new Role { Id = 4, Name = "Admin" };

        var cashier = new User
        {
            Id = 1,
            Username = "cashier01",
            FullName = "Thu Ngân 01",
            RoleId = 1,
            Role = cashierRole,
            EmployeeCode = "NV0001",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("111111"),
            IsActive = true
        };

        var chef = new User
        {
            Id = 2,
            Username = "chef01",
            FullName = "Bếp Trưởng 01",
            RoleId = 2,
            Role = chefRole,
            EmployeeCode = "NV0002",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("222222"),
            IsActive = true
        };

        var manager = new User
        {
            Id = 3,
            Username = "manager01",
            FullName = "Quản Lý 01",
            RoleId = 3,
            Role = managerRole,
            EmployeeCode = "NV0003",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("333333"),
            IsActive = true
        };

        var admin = new User
        {
            Id = 4,
            Username = "admin01",
            FullName = "Admin 01",
            RoleId = 4,
            Role = adminRole,
            EmployeeCode = "NV0004",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("444444"),
            IsActive = true
        };

        context.Roles.AddRange(cashierRole, chefRole, managerRole, adminRole);
        context.Users.AddRange(cashier, chef, manager, admin);
        context.SaveChanges();

        return (cashier, chef, manager, admin);
    }

    [Fact]
    public async Task ValidCashierPin_ReturnsPosAccess_And_PosDestination()
    {
        var db = GetDbContext(nameof(ValidCashierPin_ReturnsPosAccess_And_PosDestination));
        var (cashier, chef, manager, admin) = SeedUsers(db);

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "111111" });

        Assert.NotNull(res);
        Assert.Equal("cashier01", res.Username);
        Assert.Equal("POS", res.DestinationArea);
        Assert.Contains(Permissions.PosAccess, res.Permissions);
        Assert.DoesNotContain(Permissions.KdsAccess, res.Permissions);
    }

    [Fact]
    public async Task ValidChefPin_ReturnsKdsAccess_And_KdsDestination()
    {
        var db = GetDbContext(nameof(ValidChefPin_ReturnsKdsAccess_And_KdsDestination));
        var (cashier, chef, manager, admin) = SeedUsers(db);

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "222222" });

        Assert.NotNull(res);
        Assert.Equal("chef01", res.Username);
        Assert.Equal("KDS", res.DestinationArea);
        Assert.Contains(Permissions.KdsAccess, res.Permissions);
        Assert.DoesNotContain(Permissions.PosAccess, res.Permissions);
    }

    [Fact]
    public async Task ValidManagerPin_ReturnsBothAccess_And_BothDestination()
    {
        var db = GetDbContext(nameof(ValidManagerPin_ReturnsBothAccess_And_BothDestination));
        var (cashier, chef, manager, admin) = SeedUsers(db);

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "333333" });

        Assert.NotNull(res);
        Assert.Equal("manager01", res.Username);
        Assert.Equal("BOTH", res.DestinationArea);
        Assert.Contains(Permissions.PosAccess, res.Permissions);
        Assert.Contains(Permissions.KdsAccess, res.Permissions);
    }

    [Fact]
    public async Task InvalidPin_ReturnsNull()
    {
        var db = GetDbContext(nameof(InvalidPin_ReturnsNull));
        SeedUsers(db);

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "888888" });
        Assert.Null(res);
    }

    [Fact]
    public async Task FiveWrongPins_LocksAccount_For15Minutes()
    {
        var db = GetDbContext(nameof(FiveWrongPins_LocksAccount_For15Minutes));
        SeedUsers(db);

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        // Fail 4 times
        for (int i = 0; i < 4; i++)
        {
            var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "999999" });
            Assert.Null(res);
        }

        // 5th fail -> throws InvalidOperationException (Lockout)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "999999" });
        });
    }

    [Fact]
    public async Task DisabledUser_CannotLogin_WithPin()
    {
        var db = GetDbContext(nameof(DisabledUser_CannotLogin_WithPin));
        var (cashier, chef, manager, admin) = SeedUsers(db);

        cashier.IsActive = false;
        db.SaveChanges();

        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);

        var res = await identityService.PinLoginAsync(new PinLoginRequest { PinCode = "111111" });
        Assert.Null(res);
    }
}
