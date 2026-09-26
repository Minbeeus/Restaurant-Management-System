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

public class CashierToolsAndApprovalWorkflowTests
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

        var cashier = new User { Id = 10, Username = "cashier01", FullName = "Nguyễn Văn Thu Ngân", RoleId = 1, Role = cashierRole, EmployeeCode = "NV0010" };
        var manager = new User { Id = 20, Username = "manager01", FullName = "Trần Văn Quản Lý", RoleId = 2, Role = managerRole, EmployeeCode = "NV0020" };
        var register = new Register { Id = 1, Code = "REG01", Name = "Máy POS 01", IsActive = true };

        context.Roles.AddRange(cashierRole, managerRole);
        context.Users.AddRange(cashier, manager);
        context.Registers.Add(register);
        context.SaveChanges();

        return (cashier, manager, register);
    }

    [Fact]
    public async Task OpenShift_And_CloseShift_WithoutVariance_ClosesShiftImmediately()
    {
        var db = GetDbContext(nameof(OpenShift_And_CloseShift_WithoutVariance_ClosesShiftImmediately));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var policyService = new PosPolicyService();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object);

        // Open shift
        var shiftDto = await shiftService.OpenShiftAsync(cashier.Id, new OpenShiftRequest { RegisterId = reg.Id, InitialCash = 2000000, Note = "Mở ca đầu ngày" });
        Assert.NotNull(shiftDto);
        Assert.Equal("Mở (Open)", shiftDto.StatusName);

        // Close shift with exact expected cash (2,000,000)
        var closeReport = await shiftService.CloseShiftAsync(cashier.Id, new CloseShiftRequest { ActualCashCounted = 2000000, Note = "Đóng ca đủ tiền" });
        Assert.Equal("Đã Đóng (Closed)", closeReport.StatusName);
        Assert.Equal(0, closeReport.Difference);
    }

    [Fact]
    public async Task OpenShift_OnRegisterWithActiveShift_ThrowsInvalidOperationException()
    {
        var db = GetDbContext(nameof(OpenShift_OnRegisterWithActiveShift_ThrowsInvalidOperationException));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var cashier2 = new User { Id = 11, Username = "cashier02", FullName = "Thu Ngân 2", RoleId = 1, EmployeeCode = "NV0011" };
        db.Users.Add(cashier2);
        db.SaveChanges();

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object);

        // Cashier 1 opens shift on Register 1
        await shiftService.OpenShiftAsync(cashier.Id, new OpenShiftRequest { RegisterId = reg.Id, InitialCash = 1000000 });

        // Cashier 2 tries to open another shift on same Register 1 -> Must throw!
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await shiftService.OpenShiftAsync(cashier2.Id, new OpenShiftRequest { RegisterId = reg.Id, InitialCash = 1000000 });
        });
    }

    [Fact]
    public async Task CloseShift_WithVariance_CreatesApprovalRequest_And_SetsPendingClosingApprovalStatus()
    {
        var db = GetDbContext(nameof(CloseShift_WithVariance_CreatesApprovalRequest_And_SetsPendingClosingApprovalStatus));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var shiftService = new ShiftService(db, localizer.Object, approvalService, permService, auditService.Object);

        await shiftService.OpenShiftAsync(cashier.Id, new OpenShiftRequest { RegisterId = reg.Id, InitialCash = 2000000 });

        // Close shift with 50,000 variance (1,950,000 actual vs 2,000,000 expected)
        var closeReport = await shiftService.CloseShiftAsync(cashier.Id, new CloseShiftRequest { ActualCashCounted = 1950000, Note = "Thiếu 50k" });

        Assert.Equal("Chờ Duyệt Chênh Lệch", closeReport.StatusName);
        Assert.Equal(-50000, closeReport.Difference);

        var pendingReqs = await approvalService.GetPendingRequestsAsync();
        Assert.Single(pendingReqs);
        Assert.Equal((int)ApprovalType.ShiftClosing, pendingReqs[0].Type);
        Assert.Equal(50000, pendingReqs[0].Amount);
    }

    [Fact]
    public async Task TillOut_UnderLimit_ExecutesImmediately()
    {
        var db = GetDbContext(nameof(TillOut_UnderLimit_ExecutesImmediately));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var policyService = new PosPolicyService();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var cashService = new CashMovementService(db, permService, approvalService, policyService, auditService.Object);

        var shift = new Shift { UserId = cashier.Id, RegisterId = reg.Id, OpeningBalance = 2000000, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        db.Shifts.Add(shift);
        db.SaveChanges();

        // Till Out 500,000 (under 1,000,000 limit)
        var result = await cashService.PerformTillOutAsync(cashier.Id, shift.Id, new TillOutRequest { Amount = 500000, Reason = "Mua văn phòng phẩm" });

        Assert.NotNull(result);
        Assert.Null(result.ApprovalRequestId);
        Assert.Equal((int)CashMovementType.TillOut, result.Type);

        var summary = await cashService.GetShiftCashMovementsAsync(shift.Id);
        Assert.Equal(500000, summary.TotalTillOut);
        Assert.Equal(1500000, summary.ExpectedCashBalance);
    }

    [Fact]
    public async Task TillOut_OverLimit_CreatesApprovalRequest()
    {
        var db = GetDbContext(nameof(TillOut_OverLimit_CreatesApprovalRequest));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var policyService = new PosPolicyService();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);
        var cashService = new CashMovementService(db, permService, approvalService, policyService, auditService.Object);

        var shift = new Shift { UserId = cashier.Id, RegisterId = reg.Id, OpeningBalance = 2000000, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        db.Shifts.Add(shift);
        db.SaveChanges();

        // Till Out 3,000,000 (over 1,000,000 limit) -> Creates Approval Request
        var result = await cashService.PerformTillOutAsync(cashier.Id, shift.Id, new TillOutRequest { Amount = 3000000, Reason = "Nộp ngân hàng" });

        Assert.NotNull(result);
        Assert.NotNull(result.ApprovalRequestId);

        var pendingReqs = await approvalService.GetPendingRequestsAsync();
        Assert.Single(pendingReqs);
        Assert.Equal(3000000, pendingReqs[0].Amount);
    }

    [Fact]
    public async Task ApproveRequest_SelfApproval_ThrowsInvalidOperationException()
    {
        var db = GetDbContext(nameof(ApproveRequest_SelfApproval_ThrowsInvalidOperationException));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);

        // Manager creates approval request
        var req = await approvalService.CreateRequestAsync(manager.Id, new CreateApprovalRequestDto
        {
            Type = (int)ApprovalType.CashAdjustment,
            Amount = 100000,
            Reason = "Tự tạo yêu cầu điều chỉnh"
        });

        // Manager tries to approve their own request -> Must be rejected!
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await approvalService.ApproveRequestAsync(manager.Id, new ProcessApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ApprovalNote = "Tự duyệt"
            });
        });
    }

    [Fact]
    public async Task PermissionService_CalculatesPermissionsCorrectly_ForCashier_And_Manager()
    {
        var db = GetDbContext(nameof(PermissionService_CalculatesPermissionsCorrectly_ForCashier_And_Manager));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);
        var permService = new PermissionService(db);

        var cashierPerms = await permService.GetUserPermissionsAsync(cashier.Id);
        var managerPerms = await permService.GetUserPermissionsAsync(manager.Id);

        Assert.Contains(Permissions.ShiftOpen, cashierPerms);
        Assert.Contains(Permissions.CashTillOut, cashierPerms);
        Assert.DoesNotContain(Permissions.ApprovalProcess, cashierPerms);
        Assert.DoesNotContain(Permissions.CashAdjust, cashierPerms);

        Assert.Contains(Permissions.ApprovalProcess, managerPerms);
        Assert.Contains(Permissions.CashAdjust, managerPerms);
        Assert.Contains(Permissions.ShiftReopen, managerPerms);
    }

    [Fact]
    public async Task CancelOrder_InPreparation_CreatesManagerApprovalRequest_And_ApproveExecutesCancellation()
    {
        var db = GetDbContext(nameof(CancelOrder_InPreparation_CreatesManagerApprovalRequest_And_ApproveExecutesCancellation));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var notificationService = new Mock<IRestaurantNotificationService>();
        var loyaltyService = new Mock<ILoyaltyService>();
        var cancelOrderService = new CancelOrderService(db, loyaltyService.Object, notificationService.Object, localizer.Object);

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);

        var shift = new Shift { UserId = cashier.Id, RegisterId = reg.Id, OpeningBalance = 1000000, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        db.Shifts.Add(shift);

        var order = new Order
        {
            OrderCode = "ORD001",
            Shift = shift,
            CreatedByUserId = cashier.Id,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 150000
        };
        db.Orders.Add(order);
        db.SaveChanges();

        var item = new OrderItem
        {
            OrderId = order.Id,
            MenuItemId = 1,
            MenuItemName = "Pizza",
            UnitPrice = 150000,
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Processing
        };
        db.OrderItems.Add(item);
        db.SaveChanges();

        // 1. Cashier attempts to cancel order in preparation -> Creates Pending Approval Request
        var result = await cancelOrderService.CancelOrderAsync(cashier.Id, order.Id, "Khách hủy giữa chừng");
        Assert.False(result); // Pending approval created

        var appReqs = await db.ApprovalRequests.Where(r => r.ReferenceId == order.Id).ToListAsync();
        Assert.Single(appReqs);
        Assert.Equal((int)ApprovalStatus.PendingApproval, appReqs[0].Status);
        Assert.Equal(cashier.Id, appReqs[0].RequestedByUserId);

        var dbOrderBeforeApprove = await db.Orders.FindAsync(order.Id);
        Assert.NotEqual((int)OrderStatus.Cancelled, dbOrderBeforeApprove!.Status); // Not cancelled yet

        // 2. Manager approves cancellation request -> Order Cancelled
        await approvalService.ApproveRequestAsync(manager.Id, new ProcessApprovalRequestDto
        {
            ApprovalRequestId = appReqs[0].Id,
            ApprovalNote = "Quản lý đồng ý hủy"
        });

        var dbOrderAfterApprove = await db.Orders.FindAsync(order.Id);
        Assert.Equal((int)OrderStatus.Cancelled, dbOrderAfterApprove!.Status); // Order cancelled
    }

    [Fact]
    public async Task Refund_ExceedingRemainingAmount_ThrowsInvalidOperationException()
    {
        var db = GetDbContext(nameof(Refund_ExceedingRemainingAmount_ThrowsInvalidOperationException));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);

        var shift = new Shift { UserId = cashier.Id, RegisterId = reg.Id, OpeningBalance = 1000000, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        db.Shifts.Add(shift);

        var order = new Order
        {
            OrderCode = "ORD003",
            Shift = shift,
            CreatedByUserId = cashier.Id,
            Status = (int)OrderStatus.Completed,
            PaymentStatus = (int)PaymentStatus.Paid,
            TotalAmount = 500000
        };
        db.Orders.Add(order);
        db.SaveChanges();

        var payment = new Payment
        {
            OrderId = order.Id,
            ShiftId = shift.Id,
            Amount = 500000,
            PaymentMethod = (int)PaymentMethod.Cash,
            Status = (int)PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);

        // Add an initial partial refund of 300,000
        var refund1 = new Refund
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            Amount = 300000,
            Reason = "Hoàn lại 1 món",
            RefundedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Refunds.Add(refund1);
        db.SaveChanges();

        // Cashier requests second refund of 300,000 (300k + 300k = 600k > 500k total)
        var req = await approvalService.CreateRequestAsync(cashier.Id, new CreateApprovalRequestDto
        {
            Type = (int)ApprovalType.Refund,
            ReferenceId = order.Id,
            Amount = 300000,
            Reason = "Hoàn thêm"
        });

        // Manager approval should throw InvalidOperationException because remaining max is 200,000
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await approvalService.ApproveRequestAsync(manager.Id, new ProcessApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ApprovalNote = "Đồng ý"
            });
        });
    }

    [Fact]
    public async Task OrderWithMultiplePayments_IsMarkedPaidOnlyWhenFullyPaid()
    {
        var db = GetDbContext(nameof(OrderWithMultiplePayments_IsMarkedPaidOnlyWhenFullyPaid));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var shift = new Shift { UserId = cashier.Id, RegisterId = reg.Id, OpeningBalance = 1000000, Status = (int)ShiftStatus.Open, OpenedAt = DateTime.UtcNow };
        db.Shifts.Add(shift);

        var order = new Order
        {
            OrderCode = "ORD-MULTI",
            Shift = shift,
            CreatedByUserId = cashier.Id,
            Status = (int)OrderStatus.InPreparation,
            PaymentStatus = (int)PaymentStatus.Unpaid,
            TotalAmount = 1000000
        };
        db.Orders.Add(order);
        db.SaveChanges();

        // 1. Partial Cash Payment 400,000
        var p1 = new Payment
        {
            OrderId = order.Id,
            ShiftId = shift.Id,
            Amount = 400000,
            PaymentMethod = (int)PaymentMethod.Cash,
            Status = (int)PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(p1);
        order.PaymentStatus = (int)PaymentStatus.PartiallyPaid;
        db.SaveChanges();

        var dbOrder1 = await db.Orders.FindAsync(order.Id);
        Assert.Equal((int)PaymentStatus.PartiallyPaid, dbOrder1!.PaymentStatus);

        // 2. Remaining VietQR Payment 600,000 -> Order fully paid
        var p2 = new Payment
        {
            OrderId = order.Id,
            ShiftId = shift.Id,
            Amount = 600000,
            PaymentMethod = (int)PaymentMethod.BankTransfer,
            Status = (int)PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(p2);
        db.SaveChanges();

        var totalPaid = await db.Payments.Where(p => p.OrderId == order.Id && p.Status == (int)PaymentStatus.Paid).SumAsync(p => p.Amount);
        if (totalPaid >= order.TotalAmount)
        {
            order.PaymentStatus = (int)PaymentStatus.Paid;
            order.Status = (int)OrderStatus.Completed;
        }
        db.SaveChanges();

        var dbOrder2 = await db.Orders.FindAsync(order.Id);
        Assert.Equal((int)PaymentStatus.Paid, dbOrder2!.PaymentStatus);
        Assert.Equal((int)OrderStatus.Completed, dbOrder2.Status);
    }

    [Fact]
    public async Task Admin_DoesNotInherit_ManagerPosPermissions_StrictRoleSeparation()
    {
        var db = GetDbContext(nameof(Admin_DoesNotInherit_ManagerPosPermissions_StrictRoleSeparation));
        
        var adminRole = new Role { Id = 3, Name = "Admin" };
        var managerRole = new Role { Id = 2, Name = "Manager" };
        var adminUser = new User { Id = 30, Username = "admin", FullName = "System Admin", RoleId = 3, Role = adminRole };
        var managerUser = new User { Id = 20, Username = "manager01", FullName = "Store Manager", RoleId = 2, Role = managerRole };

        db.Roles.AddRange(adminRole, managerRole);
        db.Users.AddRange(adminUser, managerUser);
        db.SaveChanges();

        var permService = new PermissionService(db);
        var adminPerms = await permService.GetUserPermissionsAsync(adminUser.Id);
        var managerPerms = await permService.GetUserPermissionsAsync(managerUser.Id);

        // Admin has User/System management, Manager has POS Approval management
        Assert.Contains(Permissions.SystemUserManage, adminPerms);
        Assert.Contains(Permissions.ApprovalProcess, managerPerms);
        Assert.DoesNotContain(Permissions.ApprovalProcess, adminPerms); // Strict Separation
    }

    [Fact]
    public async Task ConcurrentApprovals_OnlyOneExecutesSuccessfully()
    {
        var db = GetDbContext(nameof(ConcurrentApprovals_OnlyOneExecutesSuccessfully));
        var (cashier, manager, reg) = SeedUsersAndRegister(db);

        var manager2Role = new Role { Id = 4, Name = "Manager" };
        var manager2 = new User { Id = 21, Username = "manager02", FullName = "Quản lý 2", RoleId = 4, Role = manager2Role };
        db.Users.Add(manager2);
        db.SaveChanges();

        var permService = new PermissionService(db);
        var config = new Mock<IConfiguration>();
        var identityService = new IdentityService(db, config.Object);
        var auditService = new Mock<IAuditService>();
        var approvalService = new ApprovalService(db, identityService, permService, auditService.Object);

        var req = await approvalService.CreateRequestAsync(cashier.Id, new CreateApprovalRequestDto
        {
            Type = (int)ApprovalType.CashAdjustment,
            Amount = 50000,
            Reason = "Điều chỉnh tiền két"
        });

        // Manager 1 approves
        var res1 = await approvalService.ApproveRequestAsync(manager.Id, new ProcessApprovalRequestDto
        {
            ApprovalRequestId = req.Id,
            ApprovalNote = "Manager 1 duyệt"
        });
        Assert.Equal((int)ApprovalStatus.Executed, res1.Status);

        // Manager 2 attempts to approve the same request concurrently -> Must throw InvalidOperationException!
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await approvalService.ApproveRequestAsync(manager2.Id, new ProcessApprovalRequestDto
            {
                ApprovalRequestId = req.Id,
                ApprovalNote = "Manager 2 duyệt trùng"
            });
        });
    }
}
