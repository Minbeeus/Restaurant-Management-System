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

    public ShiftService(
        ApplicationDbContext context,
        IStringLocalizer<SharedResources> localizer,
        IApprovalService approvalService,
        IPermissionService permissionService,
        IAuditService auditService)
    {
        _context = context;
        _localizer = localizer;
        _approvalService = approvalService;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    public async Task<bool> HasActiveShiftAsync(int userId)
    {
        return await _context.Shifts.AnyAsync(s => s.UserId == userId && s.Status == (int)ShiftStatus.Open);
    }

    public async Task<ShiftReportDto?> GetCurrentShiftAsync(int userId)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Register)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == (int)ShiftStatus.Open);

        if (shift == null) return null;

        return await BuildShiftReportAsync(shift);
    }

    public async Task<ShiftReportDto> OpenShiftAsync(int userId, OpenShiftRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.ShiftOpen);

        var hasActive = await HasActiveShiftAsync(userId);
        if (hasActive)
        {
            throw new InvalidOperationException(_localizer["SHIFT_ALREADY_OPEN"] ?? "Thu ngân đã có ca làm việc đang mở.");
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

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException(_localizer["LOGIN_FAILED"]);
        }

        var shift = new Shift
        {
            UserId = userId,
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

        await _auditService.LogAsync(userId, "Shift.Open", "Shift", shift.Id.ToString(), $"Mở ca làm việc thành công với tiền đầu ca {request.InitialCash:N0}đ", request.RegisterId, shift.Id);

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
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == (int)ShiftStatus.Open);

            if (shift == null)
            {
                throw new InvalidOperationException(_localizer["NO_ACTIVE_SHIFT"] ?? "Không tìm thấy ca làm việc đang mở.");
            }

            var utcNow = DateTime.UtcNow;

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

            var expectedCash = shift.OpeningBalance + shift.TotalCashAmount + totalTillIn - totalTillOut - totalRefunds + totalAdjustments;
            shift.ClosingBalance = expectedCash;
            shift.ActualCashCounted = request.ActualCashCounted;

            var variance = request.ActualCashCounted - expectedCash;
            shift.Difference = variance;

            if (variance != 0)
            {
                // Create approval request for variance
                shift.Status = (int)ShiftStatus.PendingClosingApproval;

                var appReq = await _approvalService.CreateRequestAsync(userId, new CreateApprovalRequestDto
                {
                    Type = (int)ApprovalType.ShiftClosing,
                    ReferenceId = shift.Id,
                    Amount = Math.Abs(variance),
                    Reason = $"Chênh lệch tiền đóng ca ({variance:+N0;-N0;0}đ): {request.Note ?? "Cần Quản lý duyệt"}",
                    Note = request.Note
                });

                shift.Note = string.IsNullOrEmpty(shift.Note)
                    ? $"Chờ Quản lý duyệt chênh lệch ({variance:+N0;-N0;0}đ) - Yêu cầu #{appReq.Id}"
                    : $"{shift.Note} | Chờ duyệt chênh lệch - Yêu cầu #{appReq.Id}";

                await _auditService.LogAsync(userId, "Shift.SubmitVariance", "Shift", shift.Id.ToString(), $"Gửi yêu cầu duyệt chênh lệch đóng ca ({variance:+N0;-N0;0}đ)", shift.RegisterId, shift.Id);
            }
            else
            {
                shift.Status = (int)ShiftStatus.Closed;
                shift.ClosedAt = utcNow;
                if (!string.IsNullOrWhiteSpace(request.Note))
                {
                    shift.Note = string.IsNullOrEmpty(shift.Note) ? request.Note : $"{shift.Note} | {request.Note}";
                }

                await _auditService.LogAsync(userId, "Shift.Close", "Shift", shift.Id.ToString(), "Đóng ca làm việc thành công - Khớp tiền", shift.RegisterId, shift.Id);
            }

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
            .AsNoTracking()
            .Include(s => s.User)
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
            .Where(p => p.ShiftId == shift.Id && p.Status == (int)PaymentStatus.Paid)
            .ToListAsync();

        var cashRevenue = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
        var transferRevenue = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);

        var movements = await _context.CashMovements
            .Where(m => m.ShiftId == shift.Id)
            .ToListAsync();

        var totalTillIn = movements.Where(m => m.Type == (int)CashMovementType.TillIn).Sum(m => m.Amount);
        var totalTillOut = movements.Where(m => m.Type == (int)CashMovementType.TillOut).Sum(m => m.Amount);
        var totalRefunds = movements.Where(m => m.Type == (int)CashMovementType.CashRefund).Sum(m => m.Amount);
        var totalAdjustments = movements.Where(m => m.Type == (int)CashMovementType.CashAdjustment).Sum(m => m.Amount);

        var expectedCash = shift.OpeningBalance + cashRevenue + totalTillIn - totalTillOut - totalRefunds + totalAdjustments;

        var statusName = ((ShiftStatus)shift.Status) switch
        {
            ShiftStatus.Open => "Mở (Open)",
            ShiftStatus.Closed => "Đã Đóng (Closed)",
            ShiftStatus.PendingClosingApproval => "Chờ Duyệt Chênh Lệch",
            _ => "Khác"
        };

        return new ShiftReportDto
        {
            ShiftId = shift.Id,
            UserId = shift.UserId,
            StaffName = shift.User?.FullName ?? "Thu ngân",
            StartTime = shift.OpenedAt,
            EndTime = shift.ClosedAt,
            Status = shift.Status,
            StatusName = statusName,
            InitialCash = shift.OpeningBalance,
            TotalRevenue = cashRevenue + transferRevenue,
            CashRevenue = cashRevenue,
            BankingRevenue = transferRevenue,
            TotalCashOrders = completedPayments.Count(p => p.PaymentMethod == (int)PaymentMethod.Cash),
            TotalTransferOrders = completedPayments.Count(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer),
            ExpectedCash = expectedCash,
            ActualCash = shift.ActualCashCounted ?? expectedCash,
            Difference = shift.Difference ?? (shift.ActualCashCounted.HasValue ? shift.ActualCashCounted.Value - expectedCash : 0),
            Note = shift.Note
        };
    }
}
