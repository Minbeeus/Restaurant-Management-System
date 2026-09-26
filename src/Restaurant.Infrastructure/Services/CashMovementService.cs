using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class CashMovementService : ICashMovementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IApprovalService _approvalService;
    private readonly IPosPolicyService _policyService;
    private readonly IAuditService _auditService;

    public CashMovementService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        IApprovalService approvalService,
        IPosPolicyService policyService,
        IAuditService auditService)
    {
        _context = context;
        _permissionService = permissionService;
        _approvalService = approvalService;
        _policyService = policyService;
        _auditService = auditService;
    }

    public async Task<CashMovementDto> PerformTillInAsync(int userId, int shiftId, TillInRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.CashTillIn);

        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null || shift.Status != (int)ShiftStatus.Open)
        {
            throw new InvalidOperationException("Ca làm việc không tồn tại hoặc đã đóng.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Số tiền nộp két (Till In) phải lớn hơn 0.");
        }

        var cm = new CashMovement
        {
            ShiftId = shiftId,
            RegisterId = shift.RegisterId,
            Type = (int)CashMovementType.TillIn,
            Amount = request.Amount,
            Reason = request.Reason,
            Note = request.Note,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(cm);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Till.In", "CashMovement", cm.Id.ToString(), $"Nộp tiền vào két: {request.Amount:N0}đ ({request.Reason})", shift.RegisterId, shiftId);

        var user = await _context.Users.FindAsync(userId);
        return MapToDto(cm, user?.FullName ?? "N/A");
    }

    public async Task<CashMovementDto> PerformTillOutAsync(int userId, int shiftId, TillOutRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.CashTillOut);

        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null || shift.Status != (int)ShiftStatus.Open)
        {
            throw new InvalidOperationException("Ca làm việc không tồn tại hoặc đã đóng.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Số tiền rút két (Till Out) phải lớn hơn 0.");
        }

        var policy = await _policyService.GetPolicyAsync();
        var hasLimitBypass = await _permissionService.HasPermissionAsync(userId, Application.Common.Permissions.CashTillOutLimit);
        var limit = policy.TillOutLimit;

        // If Amount > Limit and user cannot bypass -> Create Approval Request
        if (request.Amount > limit && !hasLimitBypass)
        {
            var appReq = await _approvalService.CreateRequestAsync(userId, new CreateApprovalRequestDto
            {
                Type = (int)ApprovalType.TillOut,
                ReferenceId = shiftId,
                Amount = request.Amount,
                Reason = request.Reason,
                Note = request.Note
            });

            await _auditService.LogAsync(userId, "Till.OutRequest", "ApprovalRequest", appReq.Id.ToString(), $"Tạo yêu cầu rút két vượt hạn mức {limit:N0}đ (Yêu cầu {request.Amount:N0}đ)", shift.RegisterId, shiftId);

            return new CashMovementDto
            {
                Id = 0,
                ShiftId = shiftId,
                Type = (int)CashMovementType.TillOut,
                TypeName = "Rút Tiền Két (Till Out - Chờ Duyệt)",
                Amount = request.Amount,
                Reason = request.Reason,
                Note = $"Số tiền vượt hạn mức {limit:N0}đ. Đã tạo Yêu cầu Phê duyệt #{appReq.Id} cho Quản lý.",
                CreatedByUserId = userId,
                CreatedByUserName = "Cashier",
                CreatedAt = DateTime.UtcNow,
                ApprovalRequestId = appReq.Id
            };
        }

        // Direct Execution within limit or by Manager
        var cm = new CashMovement
        {
            ShiftId = shiftId,
            RegisterId = shift.RegisterId,
            Type = (int)CashMovementType.TillOut,
            Amount = request.Amount,
            Reason = request.Reason,
            Note = request.Note,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(cm);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Till.Out", "CashMovement", cm.Id.ToString(), $"Rút tiền két trực tiếp: {request.Amount:N0}đ ({request.Reason})", shift.RegisterId, shiftId);

        var user = await _context.Users.FindAsync(userId);
        return MapToDto(cm, user?.FullName ?? "N/A");
    }

    public async Task<CashMovementDto> PerformCashAdjustmentAsync(int managerUserId, int shiftId, CashAdjustmentRequest request)
    {
        await _permissionService.EnsurePermissionAsync(managerUserId, Application.Common.Permissions.CashAdjust);

        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        if (request.Amount == 0) throw new ArgumentException("Số tiền điều chỉnh không được bằng 0.");

        var cm = new CashMovement
        {
            ShiftId = shiftId,
            RegisterId = shift.RegisterId,
            Type = (int)CashMovementType.CashAdjustment,
            Amount = request.Amount,
            Reason = request.Reason,
            Note = request.Note,
            CreatedByUserId = managerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(cm);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(managerUserId, "Cash.Adjust", "CashMovement", cm.Id.ToString(), $"Điều chỉnh số dư két: {request.Amount:N0}đ ({request.Reason})", shift.RegisterId, shiftId);

        var user = await _context.Users.FindAsync(managerUserId);
        return MapToDto(cm, user?.FullName ?? "Quản lý");
    }

    public async Task<CashMovementSummaryDto> GetShiftCashMovementsAsync(int shiftId)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        var movements = await _context.CashMovements
            .AsNoTracking()
            .Include(m => m.CreatedByUser)
            .Where(m => m.ShiftId == shiftId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var completedCashPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ShiftId == shiftId && p.PaymentMethod == (int)PaymentMethod.Cash && p.Status == (int)PaymentStatus.Paid)
            .SumAsync(p => p.Amount);

        var totalTillIn = movements.Where(m => m.Type == (int)CashMovementType.TillIn).Sum(m => m.Amount);
        var totalTillOut = movements.Where(m => m.Type == (int)CashMovementType.TillOut).Sum(m => m.Amount);
        var totalRefunds = movements.Where(m => m.Type == (int)CashMovementType.CashRefund).Sum(m => m.Amount);
        var totalAdjustments = movements.Where(m => m.Type == (int)CashMovementType.CashAdjustment).Sum(m => m.Amount);

        return new CashMovementSummaryDto
        {
            OpeningBalance = shift.OpeningBalance,
            TotalCashSales = completedCashPayments,
            TotalTillIn = totalTillIn,
            TotalTillOut = totalTillOut,
            TotalCashRefunds = totalRefunds,
            TotalAdjustments = totalAdjustments,
            Movements = movements.Select(m => MapToDto(m, m.CreatedByUser?.FullName ?? "N/A")).ToList()
        };
    }

    public async Task<List<CashMovementDto>> GetAllStoreCashMovementsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.CashMovements
            .AsNoTracking()
            .Include(m => m.CreatedByUser)
            .AsQueryable();

        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);

        var list = await query.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        return list.Select(m => MapToDto(m, m.CreatedByUser?.FullName ?? "N/A")).ToList();
    }

    private static CashMovementDto MapToDto(CashMovement cm, string createdByName)
    {
        var typeName = ((CashMovementType)cm.Type) switch
        {
            CashMovementType.OpeningCash => "Tiền Đầu Ca",
            CashMovementType.CashSale => "Bán Hàng Tiền Mặt",
            CashMovementType.TillIn => "Tiền Vào (Till In)",
            CashMovementType.TillOut => "Rút Tiền (Till Out)",
            CashMovementType.CashRefund => "Hoàn Tiền Mặt",
            CashMovementType.CashAdjustment => "Điều Chỉnh Két Tiền",
            _ => "Khác"
        };

        return new CashMovementDto
        {
            Id = cm.Id,
            ShiftId = cm.ShiftId,
            Type = cm.Type,
            TypeName = typeName,
            Amount = cm.Amount,
            Reason = cm.Reason,
            Note = cm.Note,
            CreatedByUserId = cm.CreatedByUserId,
            CreatedByUserName = createdByName,
            CreatedAt = cm.CreatedAt,
            ApprovalRequestId = cm.ApprovalRequestId
        };
    }
}
