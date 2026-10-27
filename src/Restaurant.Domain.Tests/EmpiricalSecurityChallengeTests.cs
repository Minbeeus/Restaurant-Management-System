using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class EmpiricalSecurityChallengeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmpiricalSecurityChallengeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("UseInMemoryDatabase", "true");
        });
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private IdentityService CreateIdentityService(ApplicationDbContext context)
    {
        var settings = new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "RestaurantManagementSecretKeySuperProtection123!"},
            {"JwtSettings:ExpiryMinutes", "480"}
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new IdentityService(context, config);
    }

    // =========================================================================
    // 1. ADVERSARIAL PIN TESTING: BYPASS PINS AND INVALID PINS
    // =========================================================================

    [Theory]
    [InlineData("000000")] // Legacy bypass 1
    [InlineData("000003")] // Legacy bypass 2
    [InlineData("123456")] // Legacy default bypass
    [InlineData("999999")] // Unregistered PIN
    [InlineData("888889")] // Off-by-one
    [InlineData("00000")]  // 5 digits
    [InlineData("0000000")] // 7 digits
    [InlineData("ABCDEF")] // Alphanumeric
    [InlineData("!@#$%^")] // Special characters
    [InlineData("' OR '1'='1")] // SQL injection attempt
    public async Task VerifyManagerPinAsync_WithInvalidAndBypassPins_ReturnsFalse(string candidatePin)
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        // Seed manager with PIN 888888
        var managerRole = new Role { Id = 10, Name = "Manager" };
        var manager = new User
        {
            Id = 10,
            Username = "mgr_secure_" + Guid.NewGuid().ToString("N")[..6],
            FullName = "Legit Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("888888"),
            IsActive = true
        };

        db.Roles.Add(managerRole);
        db.Users.Add(manager);
        await db.SaveChangesAsync();

        // Act
        var result = await identityService.VerifyManagerPinAsync(candidatePin);

        // Assert
        Assert.False(result, $"PIN '{candidatePin}' should have been rejected but was accepted!");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task VerifyManagerPinAsync_WithNullOrWhitespace_ReturnsFalseSafely(string? whitespacePin)
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        var result = await identityService.VerifyManagerPinAsync(whitespacePin!);
        Assert.False(result);
    }

    // =========================================================================
    // 2. ADVERSARIAL ROLE TESTING: NON-MANAGER AND INACTIVE ACCOUNTS
    // =========================================================================

    [Fact]
    public async Task VerifyManagerPinAsync_RejectsNonManagerRoles_CashierChefStaff()
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        var cashierRole = new Role { Id = 1, Name = "Cashier" };
        var chefRole = new Role { Id = 2, Name = "Chef" };
        var staffRole = new Role { Id = 3, Name = "Staff" };

        var cashier = new User
        {
            Id = 1,
            Username = "cashier_adv",
            FullName = "Cashier User",
            RoleId = 1,
            Role = cashierRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("111111"),
            IsActive = true
        };

        var chef = new User
        {
            Id = 2,
            Username = "chef_adv",
            FullName = "Chef User",
            RoleId = 2,
            Role = chefRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("222222"),
            IsActive = true
        };

        var staff = new User
        {
            Id = 3,
            Username = "staff_adv",
            FullName = "Staff User",
            RoleId = 3,
            Role = staffRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("333333"),
            IsActive = true
        };

        db.Roles.AddRange(cashierRole, chefRole, staffRole);
        db.Users.AddRange(cashier, chef, staff);
        await db.SaveChangesAsync();

        // None of these non-manager PINs must ever authorize manager actions
        Assert.False(await identityService.VerifyManagerPinAsync("111111"));
        Assert.False(await identityService.VerifyManagerPinAsync("222222"));
        Assert.False(await identityService.VerifyManagerPinAsync("333333"));
    }

    [Fact]
    public async Task VerifyManagerPinAsync_RejectsInactiveAndDeletedManagers()
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        var managerRole = new Role { Id = 10, Name = "Manager" };
        var adminRole = new Role { Id = 11, Name = "Admin" };

        var inactiveManager = new User
        {
            Id = 10,
            Username = "inactive_mgr",
            FullName = "Inactive Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("444444"),
            IsActive = false // INACTIVE
        };

        var inactiveAdmin = new User
        {
            Id = 11,
            Username = "inactive_admin",
            FullName = "Inactive Admin",
            RoleId = 11,
            Role = adminRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("555555"),
            IsActive = false // INACTIVE
        };

        var deletedManager = new User
        {
            Id = 12,
            Username = "deleted_mgr",
            FullName = "Deleted Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("666666"),
            IsActive = true,
            IsDeleted = true // SOFT DELETED
        };

        db.Roles.AddRange(managerRole, adminRole);
        db.Users.AddRange(inactiveManager, inactiveAdmin, deletedManager);
        await db.SaveChangesAsync();

        Assert.False(await identityService.VerifyManagerPinAsync("444444"));
        Assert.False(await identityService.VerifyManagerPinAsync("555555"));
        Assert.False(await identityService.VerifyManagerPinAsync("666666"));
    }

    [Fact]
    public async Task VerifyManagerPinAsync_AcceptsActiveManagerAndAdminOnly()
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        var managerRole = new Role { Id = 10, Name = "Manager" };
        var adminRole = new Role { Id = 11, Name = "Admin" };

        var activeManager = new User
        {
            Id = 10,
            Username = "active_mgr",
            FullName = "Active Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("777777"),
            IsActive = true
        };

        var activeAdmin = new User
        {
            Id = 11,
            Username = "active_admin",
            FullName = "Active Admin",
            RoleId = 11,
            Role = adminRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("888888"),
            IsActive = true
        };

        db.Roles.AddRange(managerRole, adminRole);
        db.Users.AddRange(activeManager, activeAdmin);
        await db.SaveChangesAsync();

        Assert.True(await identityService.VerifyManagerPinAsync("777777"));
        Assert.True(await identityService.VerifyManagerPinAsync("888888"));
    }

    // =========================================================================
    // 3. LOCKOUT TESTING: 5 FAILURES LOCKOUT ENFORCEMENT
    // =========================================================================

    [Fact]
    public async Task VerifyManagerPinAsync_FiveConsecutiveFailures_EnforcesLockoutAndResetWorks()
    {
        IdentityService.ResetManagerPinAttempts();
        using var db = CreateInMemoryDbContext();
        var identityService = CreateIdentityService(db);

        var managerRole = new Role { Id = 10, Name = "Manager" };
        var activeManager = new User
        {
            Id = 10,
            Username = "lockout_mgr",
            FullName = "Lockout Manager",
            RoleId = 10,
            Role = managerRole,
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("777777"),
            IsActive = true
        };

        db.Roles.Add(managerRole);
        db.Users.Add(activeManager);
        await db.SaveChangesAsync();

        var failedPin = "112233";

        // Attempts 1 to 4 should return false without throwing
        for (int i = 1; i <= 4; i++)
        {
            var result = await identityService.VerifyManagerPinAsync(failedPin);
            Assert.False(result, $"Attempt {i} should return false");
        }

        // Attempt 5 MUST throw lockout InvalidOperationException
        var ex5 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            identityService.VerifyManagerPinAsync(failedPin));
        Assert.Contains("bị khóa", ex5.Message);

        // Attempt 6 MUST continue throwing lockout exception
        var ex6 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            identityService.VerifyManagerPinAsync(failedPin));
        Assert.Contains("bị khóa", ex6.Message);

        // After administrative reset, the service accepts valid manager PIN
        IdentityService.ResetManagerPinAttempts();
        var resultAfterReset = await identityService.VerifyManagerPinAsync("777777");
        Assert.True(resultAfterReset);
    }

    // =========================================================================
    // 4. WEBAPI HTTP ENDPOINT TESTS: VERIFY-MANAGER-PIN & 429 LOCKOUT
    // =========================================================================

    [Fact]
    public async Task Api_VerifyManagerPin_ReturnsHttp429_WhenLockedOutAfter5Failures()
    {
        IdentityService.ResetManagerPinAttempts();
        var client = _factory.CreateClient();

        var badPin = "987123";

        // Attempts 1 through 4 return 200 with isValid: false
        for (int i = 1; i <= 4; i++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/verify-manager-pin", new { PinCode = badPin });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var content = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(content.GetProperty("data").GetProperty("isValid").GetBoolean());
        }

        // Attempt 5 returns HTTP 429 Too Many Requests (PIN_LOCKED)
        var res5 = await client.PostAsJsonAsync("/api/v1/auth/verify-manager-pin", new { PinCode = badPin });
        Assert.Equal(HttpStatusCode.TooManyRequests, res5.StatusCode);
        var content5 = await res5.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(content5.GetProperty("success").GetBoolean());
        Assert.Equal("PIN_LOCKED", content5.GetProperty("error").GetProperty("code").GetString());
    }

    // =========================================================================
    // 5. WEBAPI ENDPOINT AUTHORIZATION LOCKDOWN (401 & 403 ENFORCEMENT)
    // =========================================================================

    [Theory]
    // PosOrdersController
    [InlineData("/api/v1/pos/orders/1/cancel", "POST")]
    [InlineData("/api/v1/pos/orders/1/items/1/void", "POST")]
    [InlineData("/api/v1/pos/tables/move", "POST")]
    [InlineData("/api/v1/pos/tables/merge", "POST")]
    [InlineData("/api/v1/pos/orders/1/approve", "POST")]
    // PaymentsController
    [InlineData("/api/v1/payments/cash", "POST")]
    // ShiftsController
    [InlineData("/api/v1/shifts/current", "GET")]
    [InlineData("/api/v1/shifts/open", "POST")]
    [InlineData("/api/v1/shifts/close", "POST")]
    // CashierToolsController
    [InlineData("/api/v1/cashier/till-in/1", "POST")]
    [InlineData("/api/v1/cashier/till-out/1", "POST")]
    [InlineData("/api/v1/cashier/cash-adjustment/1", "POST")]
    // ApprovalCenterController
    [InlineData("/api/v1/approvals/create", "POST")]
    [InlineData("/api/v1/approvals/pending", "GET")]
    [InlineData("/api/v1/approvals/1/approve", "POST")]
    [InlineData("/api/v1/approvals/1/reject", "POST")]
    [InlineData("/api/v1/approvals/manager-pin-approve", "POST")]
    // ReconciliationController
    [InlineData("/api/v1/reconciliation/save", "POST")]
    [InlineData("/api/v1/reconciliation/preview/1", "GET")]
    // VouchersController
    [InlineData("/api/v1/vouchers", "POST")]
    [InlineData("/api/v1/vouchers/1", "PUT")]
    [InlineData("/api/v1/vouchers/1", "DELETE")]
    // DashboardController
    [InlineData("/api/dashboard/summary", "GET")]
    [InlineData("/api/dashboard/sales-chart", "GET")]
    public async Task SensitiveEndpoints_StrictlyEnforce401_WhenAnonymous(string endpoint, string method)
    {
        var client = _factory.CreateClient();
        HttpResponseMessage response;

        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        if (method == "POST")
            response = await client.PostAsync(endpoint, content);
        else if (method == "PUT")
            response = await client.PutAsync(endpoint, content);
        else if (method == "DELETE")
            response = await client.DeleteAsync(endpoint);
        else
            response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string GenerateJwtToken(string role, int userId = 999)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("RestaurantManagementSecretKeySuperProtection123!");
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"test_{role.ToLower()}"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Theory]
    // Endpoints requiring Admin or Manager, called by Cashier -> Must return 403 Forbidden
    [InlineData("/api/v1/pos/orders/1/cancel", "POST")]
    [InlineData("/api/v1/pos/orders/1/items/1/void", "POST")]
    [InlineData("/api/v1/reconciliation/save", "POST")]
    [InlineData("/api/v1/reconciliation/preview/1", "GET")]
    [InlineData("/api/v1/vouchers", "POST")]
    [InlineData("/api/v1/vouchers/1", "PUT")]
    [InlineData("/api/v1/vouchers/1", "DELETE")]
    [InlineData("/api/dashboard/summary", "GET")]
    [InlineData("/api/dashboard/sales-chart", "GET")]
    [InlineData("/api/v1/approvals/1/approve", "POST")]
    [InlineData("/api/v1/approvals/1/reject", "POST")]
    public async Task SensitiveEndpoints_StrictlyEnforce403Forbidden_WhenCashierTriesManagerOrAdminAction(string endpoint, string method)
    {
        var cashierToken = GenerateJwtToken("Cashier");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        HttpResponseMessage response;

        if (method == "POST")
            response = await client.PostAsync(endpoint, content);
        else if (method == "PUT")
            response = await client.PutAsync(endpoint, content);
        else if (method == "DELETE")
            response = await client.DeleteAsync(endpoint);
        else
            response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
