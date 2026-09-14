using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace Restaurant.Domain.Tests;

public class AuditableEntityInterceptorTests
{
    private readonly ApplicationDbContext _context;

    public AuditableEntityInterceptorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_ShouldAutoFillCreatedAt_WhenNewEntityAdded()
    {
        // Arrange
        var role = new Role { Name = "TestRole", Description = "Test Description" };

        // Act
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, role.CreatedAt);
        Assert.True((DateTime.UtcNow - role.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task SaveChanges_ShouldAutoFillUpdatedAt_WhenEntityModified()
    {
        // Arrange
        var role = new Role { Name = "TestRole" };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Act
        role.Name = "UpdatedRoleName";
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(role.UpdatedAt);
        Assert.True((DateTime.UtcNow - role.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public async Task SaveChanges_ShouldSoftDelete_WhenFullAuditableEntityDeleted()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            RoleId = 1
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert (Kiểm tra xem IsDeleted chuyển thành true trong DB)
        var deletedUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(deletedUser);
        Assert.True(deletedUser.IsDeleted);
        Assert.NotNull(deletedUser.DeletedAt);
    }
}
