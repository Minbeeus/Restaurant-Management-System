using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class IdentityServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IdentityService _identityService;

    public IdentityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var inMemorySettings = new Dictionary<string, string?> {
            {"JwtSettings:SecretKey", "RestaurantManagementSecretKeySuperProtection123!"},
            {"JwtSettings:ExpiryMinutes", "480"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _identityService = new IdentityService(_context, configuration);
    }

    [Fact]
    public void HashPassword_And_VerifyPassword_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "AdminPassword123!";

        // Act
        var hash = _identityService.HashPassword(password);
        var isValid = _identityService.VerifyPassword(password, hash);
        var isInvalid = _identityService.VerifyPassword("WrongPassword", hash);

        // Assert
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Admin" };
        var password = "AdminPassword123!";
        var user = new User
        {
            Id = 1,
            Username = "admin",
            FullName = "Admin User",
            PasswordHash = _identityService.HashPassword(password),
            RoleId = 1,
            Role = role,
            IsActive = true
        };

        _context.Roles.Add(role);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _identityService.LoginAsync(new LoginRequest { Username = "admin", Password = password });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.RoleName);
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithValidManagerPin_ReturnsTrue()
    {
        IdentityService.ResetManagerPinAttempts();
        var managerRole = new Role { Id = 10, Name = "Manager" };
        var manager = new User
        {
            Id = 10,
            Username = "mgr_test_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Test Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("888888"),
            IsActive = true
        };

        _context.Roles.Add(managerRole);
        _context.Users.Add(manager);
        await _context.SaveChangesAsync();

        var result = await _identityService.VerifyManagerPinAsync("888888");
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithValidAdminPin_ReturnsTrue()
    {
        IdentityService.ResetManagerPinAttempts();
        var adminRole = new Role { Id = 11, Name = "Admin" };
        var admin = new User
        {
            Id = 11,
            Username = "admin_pin_test_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Admin Pin Test",
            RoleId = 11,
            Role = adminRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("999999"),
            IsActive = true
        };

        _context.Roles.Add(adminRole);
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        var result = await _identityService.VerifyManagerPinAsync("999999");
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithCashierPin_ReturnsFalse()
    {
        IdentityService.ResetManagerPinAttempts();
        var cashierRole = new Role { Id = 12, Name = "Cashier" };
        var cashier = new User
        {
            Id = 12,
            Username = "cashier_pin_test_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Cashier Test",
            RoleId = 12,
            Role = cashierRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("111111"),
            IsActive = true
        };

        _context.Roles.Add(cashierRole);
        _context.Users.Add(cashier);
        await _context.SaveChangesAsync();

        var result = await _identityService.VerifyManagerPinAsync("111111");
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithInactiveManager_ReturnsFalse()
    {
        IdentityService.ResetManagerPinAttempts();
        var managerRole = new Role { Id = 13, Name = "Manager" };
        var manager = new User
        {
            Id = 13,
            Username = "inactive_mgr_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Inactive Manager",
            RoleId = 13,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("777777"),
            IsActive = false
        };

        _context.Roles.Add(managerRole);
        _context.Users.Add(manager);
        await _context.SaveChangesAsync();

        var result = await _identityService.VerifyManagerPinAsync("777777");
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithHardcodedBypassPins_ReturnsFalse()
    {
        IdentityService.ResetManagerPinAttempts();
        Assert.False(await _identityService.VerifyManagerPinAsync("000000"));
        Assert.False(await _identityService.VerifyManagerPinAsync("000003"));
    }

    [Fact]
    public async Task VerifyManagerPinAsync_WithEmptyOrNullPin_ReturnsFalse()
    {
        IdentityService.ResetManagerPinAttempts();
        Assert.False(await _identityService.VerifyManagerPinAsync(null!));
        Assert.False(await _identityService.VerifyManagerPinAsync(""));
        Assert.False(await _identityService.VerifyManagerPinAsync("   "));
    }

    [Fact]
    public async Task VerifyManagerPinAsync_FiveFailedAttempts_ThrowsLockoutException()
    {
        IdentityService.ResetManagerPinAttempts();
        var testPins = new[] { "654321", "654322", "654323", "654324" };

        foreach (var pin in testPins)
        {
            var res = await _identityService.VerifyManagerPinAsync(pin);
            Assert.False(res);
        }

        // 5th attempt with another different PIN triggers lockout
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.VerifyManagerPinAsync("654325"));

        Assert.Contains("bị khóa", ex.Message);

        // Subsequent attempt with yet another different PIN is blocked by lockout
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.VerifyManagerPinAsync("654326"));
    }

    [Fact]
    public async Task VerifyManagerPinAsync_ValidPin_ResetsConsecutiveFailedAttempts()
    {
        IdentityService.ResetManagerPinAttempts();
        var managerRole = new Role { Id = 14, Name = "Manager" };
        var validPin = "888999";
        var manager = new User
        {
            Id = 14,
            Username = "mgr_reset_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Manager Reset",
            RoleId = 14,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword(validPin),
            IsActive = true
        };

        _context.Roles.Add(managerRole);
        _context.Users.Add(manager);
        await _context.SaveChangesAsync();

        // 3 failed attempts with different PINs
        Assert.False(await _identityService.VerifyManagerPinAsync("100001"));
        Assert.False(await _identityService.VerifyManagerPinAsync("100002"));
        Assert.False(await _identityService.VerifyManagerPinAsync("100003"));

        // 1 valid attempt -> should succeed and reset failed counter
        Assert.True(await _identityService.VerifyManagerPinAsync(validPin));

        // 3 more failed attempts -> should not be locked out because counter was reset
        Assert.False(await _identityService.VerifyManagerPinAsync("200001"));
        Assert.False(await _identityService.VerifyManagerPinAsync("200002"));
        Assert.False(await _identityService.VerifyManagerPinAsync("200003"));
    }
}
