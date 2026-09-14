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
}
