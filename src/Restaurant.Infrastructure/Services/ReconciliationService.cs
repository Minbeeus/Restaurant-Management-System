using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public ReconciliationService(ApplicationDbContext context, IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<List<PaymentReconciliationItemDto>> GetShiftReconciliationPreviewAsync(int shiftId)
    {
        var shift = await _context.Shifts.FindAsync(shiftId);
        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        var completedPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ShiftId == shiftId && p.Status == (int)PaymentStatus.Paid)
            .ToListAsync();

        var cashSystem = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
        var transferSystem = completedPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);

        var existingReconciliations = await _context.PaymentReconciliations
            .AsNoTracking()
            .Where(pr => pr.ShiftId == shiftId)
            .ToListAsync();

        var result = new List<PaymentReconciliationItemDto>
        {
            new PaymentReconciliationItemDto
            {
                PaymentMethod = (int)PaymentMethod.Cash,
                PaymentMethodName = "Tiền Mặt",
                SystemAmount = cashSystem,
                ActualAmount = existingReconciliations.FirstOrDefault(r => r.PaymentMethod == (int)PaymentMethod.Cash)?.ActualAmount ?? (shift.ActualCashCounted ?? cashSystem),
                Note = existingReconciliations.FirstOrDefault(r => r.PaymentMethod == (int)PaymentMethod.Cash)?.Note
            },
            new PaymentReconciliationItemDto
            {
                PaymentMethod = (int)PaymentMethod.BankTransfer,
                PaymentMethodName = "Chuyển Khoản VietQR",
                SystemAmount = transferSystem,
                ActualAmount = existingReconciliations.FirstOrDefault(r => r.PaymentMethod == (int)PaymentMethod.BankTransfer)?.ActualAmount ?? transferSystem,
                Note = existingReconciliations.FirstOrDefault(r => r.PaymentMethod == (int)PaymentMethod.BankTransfer)?.Note
            }
        };

        return result;
    }

    public async Task<bool> SaveShiftReconciliationAsync(int userId, SaveReconciliationRequest request)
    {
        await _permissionService.EnsurePermissionAsync(userId, Application.Common.Permissions.ReconciliationApprove);

        var shift = await _context.Shifts.FindAsync(request.ShiftId);
        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        var existing = await _context.PaymentReconciliations
            .Where(pr => pr.ShiftId == request.ShiftId)
            .ToListAsync();

        if (existing.Any())
        {
            _context.PaymentReconciliations.RemoveRange(existing);
        }

        foreach (var item in request.Items)
        {
            var pr = new PaymentReconciliation
            {
                ShiftId = request.ShiftId,
                PaymentMethod = item.PaymentMethod,
                SystemAmount = item.SystemAmount,
                ActualAmount = item.ActualAmount,
                Note = item.Note ?? request.Note,
                ReconciledByUserId = userId
            };
            _context.PaymentReconciliations.Add(pr);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StoreSalesSummaryDto> GetStoreSalesSummaryAsync(DateTime fromDate, DateTime toDate)
    {
        var completedOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status != (int)OrderStatus.Cancelled)
            .ToListAsync();

        var grossSales = completedOrders.Sum(o => o.SubTotal);
        var totalDiscounts = completedOrders.Sum(o => o.DiscountAmount);
        var totalOrders = completedOrders.Count;
        var totalItems = completedOrders.Sum(o => o.OrderItems.Sum(i => i.Quantity));

        var cashSales = completedOrders
            .SelectMany(o => o.Payments)
            .Where(p => p.PaymentMethod == (int)PaymentMethod.Cash && p.Status == (int)PaymentStatus.Paid)
            .Sum(p => p.Amount);

        var transferSales = completedOrders
            .SelectMany(o => o.Payments)
            .Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer && p.Status == (int)PaymentStatus.Paid)
            .Sum(p => p.Amount);

        return new StoreSalesSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            GrossSales = grossSales,
            TotalDiscounts = totalDiscounts,
            TotalOrders = totalOrders,
            TotalItemsSold = totalItems,
            CashSales = cashSales,
            TransferSales = transferSales,
            OtherSales = 0
        };
    }
}
