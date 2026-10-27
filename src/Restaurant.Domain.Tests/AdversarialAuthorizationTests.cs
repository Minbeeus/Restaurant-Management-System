using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class AdversarialAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string JwtSecret = "RestaurantManagementSecretKeySuperProtection123!";

    public AdversarialAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("UseInMemoryDatabase", "true");
        });
    }

    private string CreateToken(string role, int? userId = null, string username = "testuser")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Theory]
    // PosOrdersController
    [InlineData("/api/v1/pos/orders/1/cancel", "POST")]
    [InlineData("/api/v1/pos/orders/1/items/1/void", "POST")]
    [InlineData("/api/v1/pos/tables/move", "POST")]
    [InlineData("/api/v1/pos/tables/merge", "POST")]
    [InlineData("/api/v1/pos/orders/1/approve", "POST")]
    // PaymentsController
    [InlineData("/api/v1/payments/cash", "POST")]
    [InlineData("/api/v1/payments/generate-vietqr", "POST")]
    // ShiftsController
    [InlineData("/api/v1/shifts/current", "GET")]
    [InlineData("/api/v1/shifts/open", "POST")]
    [InlineData("/api/v1/shifts/close", "POST")]
    [InlineData("/api/v1/shifts/all", "GET")]
    [InlineData("/api/v1/shifts/1/reopen", "POST")]
    // CashierToolsController
    [InlineData("/api/v1/cashier/till-in/1", "POST")]
    [InlineData("/api/v1/cashier/till-out/1", "POST")]
    [InlineData("/api/v1/cashier/cash-adjustment/1", "POST")]
    [InlineData("/api/v1/cashier/cash-movements/1", "GET")]
    [InlineData("/api/v1/cashier/all-cash-movements", "GET")]
    // ApprovalCenterController
    [InlineData("/api/v1/approvals/pending", "GET")]
    [InlineData("/api/v1/approvals/history", "GET")]
    [InlineData("/api/v1/approvals/create", "POST")]
    [InlineData("/api/v1/approvals/1/approve", "POST")]
    [InlineData("/api/v1/approvals/1/reject", "POST")]
    [InlineData("/api/v1/approvals/manager-pin-approve", "POST")]
    // ReconciliationController
    [InlineData("/api/v1/reconciliation/preview/1", "GET")]
    [InlineData("/api/v1/reconciliation/save", "POST")]
    [InlineData("/api/v1/reconciliation/sales-summary", "GET")]
    // VouchersController
    [InlineData("/api/v1/vouchers", "GET")]
    [InlineData("/api/v1/vouchers", "POST")]
    [InlineData("/api/v1/vouchers/1", "PUT")]
    [InlineData("/api/v1/vouchers/1", "DELETE")]
    // DashboardController
    [InlineData("/api/dashboard/summary", "GET")]
    [InlineData("/api/dashboard/sales-chart", "GET")]
    [InlineData("/api/dashboard/top-movers", "GET")]
    [InlineData("/api/dashboard/activity-feed", "GET")]
    // InventoryController
    [InlineData("/api/v1/inventory/purchase-orders", "POST")]
    [InlineData("/api/v1/inventory/stock-audit", "POST")]
    public async Task AnonymousAccess_ToSensitiveEndpoints_MustReturn401Unauthorized(string endpoint, string method)
    {
        var client = _factory.CreateClient();

        HttpResponseMessage response = method switch
        {
            "POST" => await client.PostAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "PUT" => await client.PutAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "DELETE" => await client.DeleteAsync(endpoint),
            _ => await client.GetAsync(endpoint)
        };

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    // DashboardController (Roles = Admin,Manager)
    [InlineData("/api/dashboard/summary", "GET")]
    [InlineData("/api/dashboard/sales-chart", "GET")]
    [InlineData("/api/dashboard/top-movers", "GET")]
    [InlineData("/api/dashboard/activity-feed", "GET")]
    // ReconciliationController (Roles = Admin,Manager)
    [InlineData("/api/v1/reconciliation/preview/1", "GET")]
    [InlineData("/api/v1/reconciliation/save", "POST")]
    [InlineData("/api/v1/reconciliation/sales-summary", "GET")]
    // ApprovalCenterController (Roles = Admin,Manager)
    [InlineData("/api/v1/approvals/1/approve", "POST")]
    [InlineData("/api/v1/approvals/1/reject", "POST")]
    // VouchersController (Roles = Admin)
    [InlineData("/api/v1/vouchers", "GET")]
    [InlineData("/api/v1/vouchers", "POST")]
    [InlineData("/api/v1/vouchers/1", "PUT")]
    [InlineData("/api/v1/vouchers/1", "DELETE")]
    // InventoryController (Roles = Admin,Manager)
    [InlineData("/api/v1/inventory/purchase-orders", "POST")]
    [InlineData("/api/v1/inventory/stock-audit", "POST")]
    // SystemController (Roles = Admin)
    [InlineData("/api/v1/system/settings", "POST")]
    // AuthController (Roles = Admin, Roles = Admin,Manager)
    [InlineData("/api/v1/auth/admin-only-test", "GET")]
    [InlineData("/api/v1/auth/users", "GET")]
    // PosOrdersController (Roles = Admin,Manager for cancel and void)
    [InlineData("/api/v1/pos/orders/1/cancel", "POST")]
    [InlineData("/api/v1/pos/orders/1/items/1/void", "POST")]
    public async Task CashierRole_AccessingManagerOrAdminEndpoints_MustReturn403Forbidden(string endpoint, string method)
    {
        var client = _factory.CreateClient();
        var cashierToken = CreateToken("Cashier", userId: 99, username: "cashier_emp");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        HttpResponseMessage response = method switch
        {
            "POST" => await client.PostAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "PUT" => await client.PutAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "DELETE" => await client.DeleteAsync(endpoint),
            _ => await client.GetAsync(endpoint)
        };

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    // Endpoints that require acting UserId from Claims; must not fallback to UserId = 1
    [InlineData("/api/v1/pos/orders/1/cancel", "POST", "Manager")]
    [InlineData("/api/v1/pos/orders/1/items/1/void", "POST", "Manager")]
    [InlineData("/api/v1/shifts/current", "GET", "Cashier")]
    [InlineData("/api/v1/shifts/open", "POST", "Cashier")]
    [InlineData("/api/v1/shifts/close", "POST", "Cashier")]
    [InlineData("/api/v1/shifts/1/reopen", "POST", "Cashier")]
    [InlineData("/api/v1/cashier/till-in/1", "POST", "Cashier")]
    [InlineData("/api/v1/cashier/till-out/1", "POST", "Cashier")]
    [InlineData("/api/v1/cashier/cash-adjustment/1", "POST", "Cashier")]
    [InlineData("/api/v1/approvals/create", "POST", "Cashier")]
    [InlineData("/api/v1/approvals/1/approve", "POST", "Manager")]
    [InlineData("/api/v1/approvals/1/reject", "POST", "Manager")]
    [InlineData("/api/v1/reconciliation/save", "POST", "Manager")]
    [InlineData("/api/v1/inventory/purchase-orders", "POST", "Manager")]
    [InlineData("/api/v1/inventory/stock-audit", "POST", "Manager")]
    public async Task TokenWithoutUserIdClaim_MustReturn401Unauthorized_AndNeverFallbackToAdmin(string endpoint, string method, string role)
    {
        var client = _factory.CreateClient();
        // Generate valid token with role, but WITHOUT NameIdentifier / sub claim (null UserId)
        var tokenWithoutUserId = CreateToken(role, userId: null, username: "user_no_sub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenWithoutUserId);

        HttpResponseMessage response = method switch
        {
            "POST" => await client.PostAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "PUT" => await client.PutAsync(endpoint, new StringContent("{}", Encoding.UTF8, "application/json")),
            "DELETE" => await client.DeleteAsync(endpoint),
            _ => await client.GetAsync(endpoint)
        };

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyManagerPin_Endpoint_MustLockout_After5ConsecutiveFailures()
    {
        IdentityService.ResetManagerPinAttempts();
        var client = _factory.CreateClient();

        var wrongPinPayload = "{\"pinCode\": \"777777\"}";

        for (int i = 1; i <= 4; i++)
        {
            var res = await client.PostAsync("/api/v1/auth/verify-manager-pin",
                new StringContent(wrongPinPayload, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();
            Assert.Contains("\"isValid\":false", body);
        }

        // 5th attempt must trigger Lockout (HTTP 429 PIN_LOCKED)
        var fifthRes = await client.PostAsync("/api/v1/auth/verify-manager-pin",
            new StringContent(wrongPinPayload, Encoding.UTF8, "application/json"));
        Assert.Equal((HttpStatusCode)429, fifthRes.StatusCode);
        var fifthBody = await fifthRes.Content.ReadAsStringAsync();
        Assert.Contains("PIN_LOCKED", fifthBody);

        // Subsequent attempt while locked must still be 429
        var lockedRes = await client.PostAsync("/api/v1/auth/verify-manager-pin",
            new StringContent(wrongPinPayload, Encoding.UTF8, "application/json"));
        Assert.Equal((HttpStatusCode)429, lockedRes.StatusCode);

        IdentityService.ResetManagerPinAttempts();
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("000003")]
    [InlineData("999999")]
    public async Task VerifyManagerPin_WithBypassOrUnregisteredPins_MustReturnFalse(string pin)
    {
        IdentityService.ResetManagerPinAttempts();
        var client = _factory.CreateClient();

        var payload = $"{{\"pinCode\": \"{pin}\"}}";
        var res = await client.PostAsync("/api/v1/auth/verify-manager-pin",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"isValid\":false", body);

        IdentityService.ResetManagerPinAttempts();
    }

    [Fact]
    public async Task ApprovalService_ApproveByManagerPinAsync_RejectsBogusAndBypassPins_AcceptsValidManagerPin()
    {
        IdentityService.ResetManagerPinAttempts();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        var managerRole = new Role { Id = 1, Name = "Manager" };
        var cashierRole = new Role { Id = 2, Name = "Cashier" };
        var managerUser = new User
        {
            Id = 1,
            Username = "manager01",
            FullName = "Manager One",
            RoleId = 1,
            Role = managerRole,
            EmployeeCode = "NV000003",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("888888"),
            IsActive = true
        };
        var cashierUser = new User
        {
            Id = 2,
            Username = "cashier01",
            FullName = "Cashier One",
            RoleId = 2,
            Role = cashierRole,
            EmployeeCode = "NV000001",
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("111111"),
            IsActive = true
        };

        db.Roles.AddRange(managerRole, cashierRole);
        db.Users.AddRange(managerUser, cashierUser);
        await db.SaveChangesAsync();

        var permService = new PermissionService(db);
        var config = new Moq.Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Moq.Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);

        // Cashier creates approval request
        var req = await approvalService.CreateRequestAsync(cashierUser.Id, new CreateApprovalRequestDto
        {
            Type = (int)Domain.Enums.ApprovalType.DiscountExceeded,
            Amount = 100000,
            Reason = "Adversarial test request"
        });

        // 1. Hardcoded bypass PIN "000000" must be rejected with UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            approvalService.ApproveByManagerPinAsync(new ManagerPinApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ManagerPin = "000000",
                ApprovalNote = "Testing bypass"
            }));

        // 2. Hardcoded bypass PIN "000003" must be rejected with UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            approvalService.ApproveByManagerPinAsync(new ManagerPinApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ManagerPin = "000003",
                ApprovalNote = "Testing bypass"
            }));

        // 3. Hardcoded bypass PIN "123456" must be rejected with UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            approvalService.ApproveByManagerPinAsync(new ManagerPinApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ManagerPin = "123456",
                ApprovalNote = "Testing bypass"
            }));

        // 4. Arbitrary invalid PIN must be rejected with UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            approvalService.ApproveByManagerPinAsync(new ManagerPinApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ManagerPin = "999999",
                ApprovalNote = "Testing bypass"
            }));

        // 5. Genuine Manager PIN "888888" must succeed and be approved by manager01
        var approvedResult = await approvalService.ApproveByManagerPinAsync(new ManagerPinApprovalRequestDto
        {
            ApprovalRequestId = req.Id,
            ManagerPin = "888888",
            ApprovalNote = "Testing legitimate approval"
        });

        Assert.NotNull(approvedResult);
        Assert.Equal((int)Domain.Enums.ApprovalStatus.Executed, approvedResult.Status);
        Assert.Equal(managerUser.Id, approvedResult.ApprovedByUserId);
    }
}

