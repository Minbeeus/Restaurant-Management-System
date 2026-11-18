using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IApprovalService _approvalService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IIdentityService _identityService;

    public ShiftService(
        ApplicationDbContext context,
        IStringLocalizer<SharedResources> localizer,
        IApprovalService approvalService,
        IPermissionService permissionService,
        IAuditService auditService,
        IIdentityService identityService)
    {
        _context = context;
        _localizer = localizer;
        _approvalService = approvalService;
        _permissionService = permissionService;
        _auditService = auditService;
        _identityService = identityService;
    }

    public async Task<bool> HasActiveShiftAsync(int userId)
    {
        return await _context.Shifts.AnyAsync(s => s.Status == (int)ShiftStatus.Open);
    }

    public async Task<ShiftReportDto?> GetCurrentShiftAsync(int userId)
    {
        var shift = await _context.Shifts
            .Include(s => s.User)
            .Include(s => s.OpenedByUser)
            .Include(s => s.CashCustodianUser)
            .Include(s => s.ClosedByUser)
            .Include(s => s.Register)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open);

        if (shift == null) return null;

        return await BuildShiftReportAsync(shift);
    }

    public async Task<ShiftReportDto> OpenShiftAsync(int userId, OpenShiftRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.ShiftOpen);

        var hasActive = await HasActiveShiftAsync(userId);
        if (hasActive)
        {
            throw new InvalidOperationException(_localizer["SHIFT_ALREADY_OPEN"] ?? "Cửa hàng đã có ca làm việc đang mở.");
        }

        if (string.IsNullOrWhiteSpace(request.CashCustodianPin))
        {
            throw new ArgumentException("Bắt buộc phải nhập mã PIN 6 số của nhân viên thu ngân giữ két.");
        }

        var custodian = await _identityService.GetUserByPinAsync(request.CashCustodianPin);
        if (custodian == null || !custodian.IsActive)
        {
            throw new InvalidOperationException("Mã PIN nhân viên giữ két không hợp lệ hoặc tài khoản đã bị khóa.");
        }

        if (request.RegisterId.HasValue)
        {
            var register = await _context.Registers.FindAsync(request.RegisterId.Value);
            if (register == null || !register.IsActive)
            {
                throw new InvalidOperationException("Máy thu ngân (Register) không tồn tại hoặc đã ngưng hoạt động.");
            }

            var registerHasActiveShift = await _context.Shifts.AnyAsync(s => s.RegisterId == request.RegisterId.Value && s.Status == (int)ShiftStatus.Open);
            if (registerHasActiveShift)
            {
                throw new InvalidOperationException("Máy thu ngân này đang có một Ca làm việc khác hoạt động.");
            }
        }

        var managerUser = await _context.Users.FindAsync(userId);
        if (managerUser == null)
        {
            throw new ArgumentException(_localizer["LOGIN_FAILED"]);
        }

        var shiftCode = $"SHF-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var shift = new Shift
        {
            ShiftCode = shiftCode,
            UserId = userId,
            OpenedByUserId = userId,
            CashCustodianUserId = custodian.Id,
            RegisterId = request.RegisterId,
            OpeningBalance = request.InitialCash,
            Status = (int)ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow,
            Note = request.Note
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        // Record OpeningCash movement
        _context.CashMovements.Add(new CashMovement
        {
            ShiftId = shift.Id,
            RegisterId = request.RegisterId,
            Type = (int)CashMovementType.OpeningCash,
            Amount = request.InitialCash,
            Reason = "Tiền đầu ca",
            Note = request.Note,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Shift.Open", "Shift", shift.Id.ToString(), $"Mở ca {shiftCode} thành công. Tiền đầu ca: {request.InitialCash:N0}đ. Người giữ két: {custodian.FullName} (#{custodian.EmployeeCode})", request.RegisterId, shift.Id);

        return await BuildShiftReportAsync(shift);
    }

    public async Task<ShiftReportDto> CloseShiftAsync(int userId, CloseShiftRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.ShiftClose);

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shift = await _context.Shifts
                .Include(s => s.User)
                .Include(s => s.OpenedByUser)
                .Include(s => s.CashCustodianUser)
                .Include(s => s.Payments)
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open);

            if (shift == null)
            {
                throw new InvalidOperationException(_localizer["NO_ACTIVE_SHIFT"] ?? "Không tìm thấy ca làm việc đang mở.");
            }

            // Condition 1: 100% orders must be completed and paid
            var activeOrders = await _context.Orders
                .Where(o => (o.ShiftId == shift.Id || o.ShiftId == 0) && o.Status != (int)OrderStatus.Cancelled)
                .Where(o => o.Status != (int)OrderStatus.Completed || o.PaymentStatus != (int)PaymentStatus.Paid)
                .ToListAsync();

            if (activeOrders.Any())
            {
                var sampleList = string.Join(", ", activeOrders.Take(5).Select(o => $"{o.OrderCode} ({(o.TableId.HasValue ? $"Bàn {o.TableId}" : "Mang đi")}, TT: {o.TotalAmount:N0}đ)"));
                throw new InvalidOperationException($"Điều kiện 1 không thỏa: Còn {activeOrders.Count} đơn hàng chưa thanh toán hoặc chưa hoàn thành ({sampleList}). Bắt buộc phải thanh toán và hoàn tất toàn bộ đơn trước khi đóng ca.");
            }

            var completedPayments = await _context.Payments
                .Where(p => p.ShiftId == shift.Id && p.Status == (int)PaymentStatus.Paid)
                .ToListAsync();

            var cashPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).ToList();
            var transferPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).ToList();

            shift.TotalCashOrders = cashPayments.Count;
            shift.TotalTransferOrders = transferPayments.Count;
            shift.TotalCashAmount = cashPayments.Sum(p => p.Amount);
            shift.TotalTransferAmount = transferPayments.Sum(p => p.Amount);

            // Fetch cash movements for exact cash balance calculation
            var movements = await _context.CashMovements
                .Where(m => m.ShiftId == shift.Id)
                .ToListAsync();

            var totalTillIn = movements.Where(m => m.Type == (int)CashMovementType.TillIn).Sum(m => m.Amount);
            var totalTillOut = movements.Where(m => m.Type == (int)CashMovementType.TillOut).Sum(m => m.Amount);
            var totalRefunds = movements.Where(m => m.Type == (int)CashMovementType.CashRefund).Sum(m => m.Amount);
            var totalAdjustments = movements.Where(m => m.Type == (int)CashMovementType.CashAdjustment).Sum(m => m.Amount);

            var expectedCash = shift.OpeningBalance + shift.TotalCashAmount + totalTillIn - totalRefunds + totalAdjustments;
            var remainingCashToTillOut = expectedCash - totalTillOut;
            var cashDifference = expectedCash - totalTillOut;

            // Condition 2: All cash in drawer must be tilled out
            if (expectedCash > 0 && totalTillOut <= 0)
            {
                throw new InvalidOperationException("Điều kiện 2 không thỏa: Chưa thực hiện rút tiền (Till Out) khỏi két chung của ca. Bắt buộc Quản lý phải rút toàn bộ tiền mặt khỏi két trước khi đóng ca.");
            }

            if (remainingCashToTillOut > 0)
            {
                throw new InvalidOperationException($"Điều kiện 2 không thỏa: Két chung còn {remainingCashToTillOut:N0}đ chưa rút (Remaining Cash to Till Out > 0). Phải rút toàn bộ tiền mặt khỏi két.");
            }

            // Condition 3: Cash Difference == 0
            if (cashDifference != 0)
            {
                throw new InvalidOperationException($"Điều kiện 3 không thỏa: Chênh lệch tiền mặt ({cashDifference:+N0;-N0;0}đ) khác 0. Yêu cầu khớp tiền tuyệt đối (Cash Difference == 0).");
            }

            // Condition 4: Manager/Admin role closes shift
            var utcNow = DateTime.UtcNow;
            shift.Status = (int)ShiftStatus.Closed;
            shift.ClosedAt = utcNow;
            shift.ClosedByUserId = userId;
            shift.ClosingBalance = expectedCash;
            shift.ActualCashCounted = totalTillOut;
            shift.Difference = 0;

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                shift.Note = string.IsNullOrEmpty(shift.Note) ? request.Note : $"{shift.Note} | {request.Note}";
            }

            // Release all tables when shift closes so no orphaned table statuses persist
            var nonAvailableTables = await _context.Tables.Where(t => t.Status != (int)TableStatus.Available).ToListAsync();
            foreach (var t in nonAvailableTables)
            {
                t.Status = (int)TableStatus.Available;
            }

            await _auditService.LogAsync(userId, "Shift.Close", "Shift", shift.Id.ToString(), $"Đóng ca {shift.ShiftCode ?? shift.Id.ToString()} thành công - Khớp tiền tuyệt đối 0đ", shift.RegisterId, shift.Id);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return await BuildShiftReportAsync(shift);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ShiftReportDto>> GetAllShiftsAsync(DateTime? fromDate, DateTime? toDate, int? status = null)
    {
        var query = _context.Shifts
            .Include(s => s.User)
            .Include(s => s.OpenedByUser)
            .Include(s => s.CashCustodianUser)
            .Include(s => s.ClosedByUser)
            .Include(s => s.Register)
            .AsQueryable();

        if (fromDate.HasValue) query = query.Where(s => s.OpenedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.OpenedAt <= toDate.Value);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var list = await query.OrderByDescending(s => s.OpenedAt).Take(100).ToListAsync();

        var reports = new List<ShiftReportDto>();
        foreach (var shift in list)
        {
            reports.Add(await BuildShiftReportAsync(shift));
        }
        return reports;
    }

    public async Task<ShiftReportDto> ReopenShiftAsync(int managerUserId, int shiftId)
    {
        await _permissionService.EnsurePermissionAsync(managerUserId, Application.Common.Permissions.ShiftReopen);

        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        if (shift.Status == (int)ShiftStatus.Open)
        {
            throw new InvalidOperationException("Ca làm việc này đã mở.");
        }

        var managerUser = await _context.Users.FindAsync(managerUserId);

        shift.Status = (int)ShiftStatus.Open;
        shift.ClosedAt = null;
        shift.Note = string.IsNullOrEmpty(shift.Note)
            ? $"Ca mở lại bởi Quản lý {managerUser?.FullName}"
            : $"{shift.Note} | Ca mở lại bởi Quản lý {managerUser?.FullName}";

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(managerUserId, "Shift.Reopen", "Shift", shift.Id.ToString(), $"Mở lại ca làm việc thành công bởi Quản lý {managerUser?.FullName}", shift.RegisterId, shift.Id);

        return await BuildShiftReportAsync(shift);
    }

    private async Task<ShiftReportDto> BuildShiftReportAsync(Shift shift)
    {
        var completedPayments = await _context.Payments
            .Include(p => p.ProcessedByUser)
            .Include(p => p.Order)
                .ThenInclude(o => o.CreatedByUser)
            .Where(p => p.ShiftId == shift.Id && p.Status == (int)PaymentStatus.Paid)
            .ToListAsync();

        var cashPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).ToList();
        var transferPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).ToList();

        var cashRevenue = cashPayments.Sum(p => p.Amount);
        var transferRevenue = transferPayments.Sum(p => p.Amount);

        var movements = await _context.CashMovements
            .Where(m => m.ShiftId == shift.Id)
            .ToListAsync();

        var totalTillIn = movements.Where(m => m.Type == (int)CashMovementType.TillIn).Sum(m => m.Amount);
        var totalTillOut = movements.Where(m => m.Type == (int)CashMovementType.TillOut).Sum(m => m.Amount);
        var totalRefunds = movements.Where(m => m.Type == (int)CashMovementType.CashRefund).Sum(m => m.Amount);
        var totalAdjustments = movements.Where(m => m.Type == (int)CashMovementType.CashAdjustment).Sum(m => m.Amount);

        var expectedCash = shift.OpeningBalance + cashRevenue + totalTillIn - totalRefunds + totalAdjustments;
        var remainingCashToTillOut = Math.Max(0, expectedCash - totalTillOut);
        var cashDifference = expectedCash - totalTillOut;

        var shiftOrders = await _context.Orders
            .Where(o => o.ShiftId == shift.Id)
            .ToListAsync();

        var dineInCount = shiftOrders.Count(o => o.OrderType == (int)OrderType.DineIn);
        var takeawayCount = shiftOrders.Count(o => o.OrderType == (int)OrderType.Pickup);
        var completedOrdersCount = shiftOrders.Count(o => o.Status == (int)OrderStatus.Completed && o.PaymentStatus == (int)PaymentStatus.Paid);
        var cancelledOrdersCount = shiftOrders.Count(o => o.Status == (int)OrderStatus.Cancelled);

        var outstandingOrders = await _context.Orders
            .Where(o => (o.ShiftId == shift.Id || o.ShiftId == 0) && o.Status != (int)OrderStatus.Cancelled)
            .Where(o => o.Status != (int)OrderStatus.Completed || o.PaymentStatus != (int)PaymentStatus.Paid)
            .Select(o => new OutstandingOrderDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                TableId = o.TableId,
                TableName = o.TableId.HasValue ? $"Bàn {o.TableId}" : "Mang đi",
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        var employeeSummaries = new List<ShiftEmployeeCashSummaryDto>();
        var userGroup = completedPayments
            .GroupBy(p => p.ProcessedByUserId ?? p.Order?.CreatedByUserId ?? 0)
            .ToList();

        foreach (var grp in userGroup)
        {
            var uid = grp.Key;
            var u = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == uid);
            var empCashPayments = grp.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).ToList();
            var empTransferPayments = grp.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).ToList();

            employeeSummaries.Add(new ShiftEmployeeCashSummaryDto
            {
                UserId = uid,
                EmployeeCode = u?.EmployeeCode ?? $"NV{uid:D6}",
                StaffName = u?.FullName ?? (uid == 0 ? "Hệ thống" : $"Nhân viên #{uid}"),
                RoleName = u?.Role?.Name ?? "Staff",
                CashOrdersCount = empCashPayments.Count,
                CashCollected = empCashPayments.Sum(p => p.Amount),
                NonCashOrdersCount = empTransferPayments.Count,
                NonCashCollected = empTransferPayments.Sum(p => p.Amount),
                TotalRevenue = grp.Sum(p => p.Amount)
            });
        }

        var statusName = ((ShiftStatus)shift.Status) switch
        {
            ShiftStatus.Open => "Mở (Open)",
            ShiftStatus.Closed => "Đã Đóng (Closed)",
            ShiftStatus.PendingClosingApproval => "Chờ Duyệt Chênh Lệch",
            _ => "Khác"
        };

        var openedByUser = shift.OpenedByUserId.HasValue ? await _context.Users.FindAsync(shift.OpenedByUserId.Value) : shift.User;
        var custodianUser = shift.CashCustodianUserId.HasValue ? await _context.Users.FindAsync(shift.CashCustodianUserId.Value) : null;
        var closedByUser = shift.ClosedByUserId.HasValue ? await _context.Users.FindAsync(shift.ClosedByUserId.Value) : null;

        var canClose = !outstandingOrders.Any() && remainingCashToTillOut == 0 && cashDifference == 0 && (expectedCash == 0 || totalTillOut > 0);

        return new ShiftReportDto
        {
            ShiftId = shift.Id,
            ShiftCode = shift.ShiftCode ?? $"SHF-{shift.Id:D6}",
            UserId = shift.UserId,
            StaffName = shift.User?.FullName ?? "Thu ngân",
            OpenedByStaffName = openedByUser?.FullName ?? "Quản lý",
            CashCustodianStaffName = custodianUser?.FullName ?? "Chưa gán",
            CashCustodianEmployeeCode = custodianUser?.EmployeeCode ?? "N/A",
            ClosedByStaffName = closedByUser?.FullName,
            StartTime = shift.OpenedAt,
            EndTime = shift.ClosedAt,
            Status = shift.Status,
            StatusName = statusName,
            InitialCash = shift.OpeningBalance,
            TotalRevenue = cashRevenue + transferRevenue,
            CashRevenue = cashRevenue,
            BankingRevenue = transferRevenue,
            TotalCashOrders = cashPayments.Count,
            TotalTransferOrders = transferPayments.Count,
            TotalTillIn = totalTillIn,
            TotalTillOut = totalTillOut,
            TotalCashRefunds = totalRefunds,
            TotalCashAdjustments = totalAdjustments,
            ExpectedCash = expectedCash,
            ActualCash = shift.ActualCashCounted ?? totalTillOut,
            RemainingCashToTillOut = remainingCashToTillOut,
            Difference = shift.Difference ?? cashDifference,
            CanCloseShift = canClose,
            Note = shift.Note,
            TotalOrders = shiftOrders.Count,
            CompletedOrdersCount = completedOrdersCount,
            CancelledOrdersCount = cancelledOrdersCount,
            OutstandingOrdersCount = outstandingOrders.Count,
            DineInOrders = dineInCount,
            TakeawayOrders = takeawayCount,
            OutstandingOrders = outstandingOrders,
            EmployeeSummaries = employeeSummaries
        };
    }
}
