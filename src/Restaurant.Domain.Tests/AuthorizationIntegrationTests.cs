using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Xunit;

namespace Restaurant.Domain.Tests;

public class AuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("UseInMemoryDatabase", "true");
        });
    }

    [Fact]
    public async Task AdminOnlyEndpoint_ShouldReturn403Forbidden_WhenUserIsChef()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var chefRole = await db.Roles.FirstAsync(r => r.Name == "Chef");
        var chefUser = new User
        {
            Username = "chef_user_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Chef User",
            PasswordHash = identityService.HashPassword("Chef123!"),
            RoleId = chefRole.Id,
            IsActive = true
        };

        db.Users.Add(chefUser);
        await db.SaveChangesAsync();

        var loginResponse = await identityService.LoginAsync(new LoginRequest
        {
            Username = chefUser.Username,
            Password = "Chef123!"
        });

        Assert.NotNull(loginResponse);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

        // Act
        var response = await client.GetAsync("/api/v1/auth/admin-only-test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_ShouldReturn200OK_WhenUserIsAdmin()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
        var adminUser = new User
        {
            Username = "admin_user_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Admin User",
            PasswordHash = identityService.HashPassword("Admin123!"),
            RoleId = adminRole.Id,
            IsActive = true
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var loginResponse = await identityService.LoginAsync(new LoginRequest
        {
            Username = adminUser.Username,
            Password = "Admin123!"
        });

        Assert.NotNull(loginResponse);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

        // Act
        var response = await client.GetAsync("/api/v1/auth/admin-only-test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
