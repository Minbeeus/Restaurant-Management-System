using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Restaurant.Application.Common;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;
using Xunit;

namespace Restaurant.Domain.Tests;

public class ShiftReconciliationAndEmployeePinTests
{
    private ApplicationDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private (User cashier, User manager, Register register) SeedUsersAndRegister(ApplicationDbContext context)
    {
        var cashierRole = new Role { Id = 1, Name = "Cashier" };
        var managerRole = new Role { Id = 2, Name = "Manager" };

        var cashier = new User 
        { 
            Id = 2007, 
            Username = "vcm243", 
            FullName = "Văn Công Minh", 
            RoleId = 1, 
            Role = cashierRole, 
            EmployeeCode = "2007", 
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("002007"), 
            IsActive = true 
        };
        var manager = new User 
        { 
            Id = 1006, 
            Username = "admin", 
            FullName = "Quản trị viên", 
            RoleId = 2, 
            Role = managerRole, 
            EmployeeCode = "1006", 
            PinCodeHash = BCrypt.Net.BCrypt.HashPassword("001006"), 
            IsActive = true 
        };
        var register = new Register { Id = 1, Code = "REG01", Name = "Máy POS 01", IsActive = true };

        context.Roles.AddRange(cashierRole, managerRole);
        context.Users.AddRange(cashier, manager);
        context.Registers.Add(register);
        context.SaveChanges();

        return (cashier, manager, register);
    }

    [Theory]
    [InlineData("1", "000001")]
    [InlineData("12", "000012")]
    [InlineData("123", "000123")]
    [InlineData("1234", "001234")]
    [InlineData("12345", "012345")]
    [InlineData("123456", "123456")]
    [InlineData("2007", "002007")]
    [InlineData("NV2007", "002007")]
    [InlineData("  2007  ", "002007")]
    public void EmployeePinStandardizer_CorrectlyStandardizesPin_ToExactly6Digits(string employeeCode, string expectedPin)
    {
        var standardized = EmployeePinStandardizer.StandardizePinFromEmployeeCode(employeeCode);
        Assert.Equal(expectedPin, standardized);
        Assert.True(EmployeePinStandardizer.IsValidPin(standardized));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("1234567")] // > 6 digits
    [InlineData("ABC")]     // non-numeric
    [InlineData("12A45")]   // alphanumeric
    public void EmployeePinStandardizer_InvalidEmployeeCode_ThrowsArgumentException(string? invalidCode)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            EmployeePinStandardizer.StandardizePinFromEmployeeCode(invalidCode);
        });
    }

    [Fact]
    public async Task OpenShift_ManagerOpens_WithCustodianPin_SetsOpenedByAndCustodian_And_ShiftCode()
    {
        var db = GetDbContext(nameof(OpenShift_ManagerOpens_WithCustodianPin_SetsOpenedByAndCustodian_And_ShiftCode));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);

        var request = new OpenShiftRequest
        {
            RegisterId = reg.Id,
            InitialCash = 2000000,
            CashCustodianPin = "002007",
            Note = "Bàn giao ca đầu ngày"
        };

        var shiftDto = await shiftService.OpenShiftAsync(manager.Id, request);

        Assert.NotNull(shiftDto);
        Assert.StartsWith("SHF-", shiftDto.ShiftCode);
        Assert.Equal(manager.FullName, shiftDto.OpenedByStaffName);
        Assert.Equal(cashier.FullName, shiftDto.CashCustodianStaffName);
        Assert.Equal("2007", shiftDto.CashCustodianEmployeeCode);
        Assert.Equal(2000000, shiftDto.InitialCash);
        Assert.Equal(2000000, shiftDto.ExpectedCash);
    }

    [Fact]
    public async Task OpenShift_CashierAttemptsToOpen_ThrowsUnauthorizedAccessException()
    {
        var db = GetDbContext(nameof(OpenShift_CashierAttemptsToOpen_ThrowsUnauthorizedAccessException));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);

        // Cashier lacks Permissions.ShiftOpen
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await shiftService.OpenShiftAsync(cashier.Id, new OpenShiftRequest
            {
                RegisterId = reg.Id,
                InitialCash = 1000000,
                CashCustodianPin = "002007"
            });
        });
    }

    [Fact]
    public async Task Payment_Attribution_CorrectlyAttributesToProcessedByUserId_AndIncreasesSharedDrawerCash()
    {
        var db = GetDbContext(nameof(Payment_Attribution_CorrectlyAttributesToProcessedByUserId_AndIncreasesSharedDrawerCash));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);
        var notificationService = new Mock<IRestaurantNotificationService>();
        var loyaltyService = new LoyaltyService(db, localizer.Object);
        var qrCodeService = new Mock<IQrCodeService>();
        var mediator = new Mock<IMediator>();
        var paymentService = new PaymentService(db, qrCodeService.Object, notificationService.Object, loyaltyService, mediator.Object, config.Object, localizer.Object, auditService.Object);

        // Manager opens shift with 2,000,000 opening cash
        var shift = await shiftService.OpenShiftAsync(manager.Id, new OpenShiftRequest
        {
            RegisterId = reg.Id,
            InitialCash = 2000000,
            CashCustodianPin = "002007"
        });

        // Create order
        var order = new Order
        {
            OrderCode = "ORD-001",
            ShiftId = shift.ShiftId,
            CreatedByUserId = cashier.Id,
            TotalAmount = 500000,
            SubTotal = 500000,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Unpaid
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Process cash payment with processedByUserId = cashier.Id
        await paymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = order.Id,
            CashReceived = 500000,
            PaymentMethod = 0 // Cash
        }, processedByUserId: cashier.Id);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        Assert.NotNull(payment);
        Assert.Equal(cashier.Id, payment.ProcessedByUserId);
        Assert.Equal((int)PaymentStatus.Paid, payment.Status);

        // Expected cash in shared drawer must be 2,000,000 (initial) + 500,000 (cash payment) = 2,500,000
        var shiftReport = await shiftService.GetCurrentShiftAsync(manager.Id);
        Assert.NotNull(shiftReport);
        Assert.Equal(2500000, shiftReport.ExpectedCash);
        Assert.Equal(500000, shiftReport.CashRevenue);

        // Employee breakdown in Section C must show Cashier's attribution
        Assert.Single(shiftReport.EmployeeSummaries);
        var empSummary = shiftReport.EmployeeSummaries[0];
        Assert.Equal(cashier.Id, empSummary.UserId);
        Assert.Equal(500000, empSummary.CashCollected);
        Assert.Equal(1, empSummary.CashOrdersCount);
    }

    [Fact]
    public async Task CloseShift_FailsCondition1_WhenUncompletedOrUnpaidOrdersExist()
    {
        var db = GetDbContext(nameof(CloseShift_FailsCondition1_WhenUncompletedOrUnpaidOrdersExist));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);

        var shift = await shiftService.OpenShiftAsync(manager.Id, new OpenShiftRequest
        {
            RegisterId = reg.Id,
            InitialCash = 1000000,
            CashCustodianPin = "002007"
        });

        // Create an uncompleted order
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-UNPAID-01",
            ShiftId = shift.ShiftId,
            CreatedByUserId = cashier.Id,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 300000
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await shiftService.CloseShiftAsync(manager.Id, new CloseShiftRequest());
        });

        Assert.Contains("Điều kiện 1", ex.Message);
        Assert.Contains("ORD-UNPAID-01", ex.Message);
    }

    [Fact]
    public async Task CloseShift_FailsCondition2_WhenDrawerCashNotTilledOut()
    {
        var db = GetDbContext(nameof(CloseShift_FailsCondition2_WhenDrawerCashNotTilledOut));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);

        await shiftService.OpenShiftAsync(manager.Id, new OpenShiftRequest
        {
            RegisterId = reg.Id,
            InitialCash = 1500000,
            CashCustodianPin = "002007"
        });

        // No Till Out was executed -> Condition 2 fails
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await shiftService.CloseShiftAsync(manager.Id, new CloseShiftRequest());
        });

        Assert.Contains("Điều kiện 2", ex.Message);
    }

    [Fact]
    public async Task CloseShift_Succeeds_WhenAll4ConditionsMet()
    {
        var db = GetDbContext(nameof(CloseShift_Succeeds_WhenAll4ConditionsMet));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var policyService = new PosPolicyService();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object, identityService);
        var cashMovementService = new CashMovementService(db, permService, approvalService, policyService, auditService.Object);

        // 1. Manager opens shift
        var shift = await shiftService.OpenShiftAsync(manager.Id, new OpenShiftRequest
        {
            RegisterId = reg.Id,
            InitialCash = 1000000,
            CashCustodianPin = "002007"
        });

        // 2. Condition 1: All orders settled (none pending)
        // 3. Condition 2: Manager tills out entire 1,000,000 cash from shared drawer
        await cashMovementService.PerformTillOutAsync(manager.Id, shift.ShiftId, new TillOutRequest
        {
            Amount = 1000000,
            Reason = "Rút hết tiền két cuối ngày"
        });

        // 4. Condition 3 & 4: Manager closes shift with 0 difference
        var report = await shiftService.CloseShiftAsync(manager.Id, new CloseShiftRequest
        {
            Note = "Đóng ca thành công khớp tiền"
        });

        Assert.NotNull(report);
        Assert.Equal("Đã Đóng (Closed)", report.StatusName);
        Assert.Equal((int)ShiftStatus.Closed, report.Status);
        Assert.Equal(0, report.Difference);
        Assert.Equal(0, report.RemainingCashToTillOut);
        Assert.Equal(manager.FullName, report.ClosedByStaffName);
    }
}
