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

    public ShiftService(ApplicationDbContext context, IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _localizer = localizer;
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
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == (int)ShiftStatus.Open);

        if (shift == null) return null;

        return await BuildShiftReportAsync(shift);
    }

    public async Task<ShiftReportDto> OpenShiftAsync(int userId, OpenShiftRequest request)
    {
        var hasActive = await HasActiveShiftAsync(userId);
        if (hasActive)
        {
            throw new InvalidOperationException(_localizer["SHIFT_ALREADY_OPEN"] ?? "Thu ngân đã có ca làm việc đang mở.");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException(_localizer["LOGIN_FAILED"]);
        }

        var shift = new Shift
        {
            UserId = userId,
            OpeningBalance = request.InitialCash,
            Status = (int)ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow,
            Note = request.Note
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        return await BuildShiftReportAsync(shift);
    }

    public async Task<ShiftReportDto> CloseShiftAsync(int userId, CloseShiftRequest request)
    {
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

            // Query completed payments for shift
            var completedPayments = await _context.Payments
                .Where(p => p.ShiftId == shift.Id && p.Status == (int)PaymentStatus.Completed)
                .ToListAsync();

            var cashPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).ToList();
            var transferPayments = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).ToList();

            shift.TotalCashOrders = cashPayments.Count;
            shift.TotalTransferOrders = transferPayments.Count;
            shift.TotalCashAmount = cashPayments.Sum(p => p.Amount);
            shift.TotalTransferAmount = transferPayments.Sum(p => p.Amount);

            var expectedCash = shift.OpeningBalance + shift.TotalCashAmount;
            shift.ClosingBalance = expectedCash;
            shift.ActualCashCounted = request.ActualCashCounted;
            shift.Difference = request.ActualCashCounted - expectedCash;
            shift.Status = (int)ShiftStatus.Closed;
            shift.ClosedAt = utcNow;

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                shift.Note = string.IsNullOrEmpty(shift.Note) ? request.Note : $"{shift.Note} | {request.Note}";
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

    private async Task<ShiftReportDto> BuildShiftReportAsync(Shift shift)
    {
        var completedPayments = await _context.Payments
            .Where(p => p.ShiftId == shift.Id && p.Status == (int)PaymentStatus.Completed)
            .ToListAsync();

        var cashRevenue = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
        var transferRevenue = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);
        var expectedCash = shift.OpeningBalance + cashRevenue;

        return new ShiftReportDto
        {
            ShiftId = shift.Id,
            UserId = shift.UserId,
            StaffName = shift.User?.FullName ?? "Thu ngân",
            StartTime = shift.OpenedAt,
            EndTime = shift.ClosedAt,
            Status = shift.Status,
            StatusName = shift.Status == (int)ShiftStatus.Open ? "Open" : "Closed",
            InitialCash = shift.OpeningBalance,
            TotalRevenue = cashRevenue + transferRevenue,
            CashRevenue = cashRevenue,
            BankingRevenue = transferRevenue,
            TotalCashOrders = completedPayments.Count(p => p.PaymentMethod == (int)PaymentMethod.Cash),
            TotalTransferOrders = completedPayments.Count(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer),
            ExpectedCash = expectedCash,
            ActualCash = shift.ActualCashCounted ?? expectedCash,
            Difference = shift.Difference ?? 0,
            Note = shift.Note
        };
    }
}
