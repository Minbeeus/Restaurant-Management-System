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

    public async Task<ShiftReconciliationDetailDto> GetShiftDetailForReconciliationAsync(int shiftId)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Register)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại.");

        // Reconciliation items
        var reconItems = await GetShiftReconciliationPreviewAsync(shiftId);

        // Orders in this shift
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.Payments)
            .Where(o => o.ShiftId == shiftId && o.Status != (int)OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var orderDtos = orders.Select(o => {
            var primaryPayment = o.Payments.FirstOrDefault(p => p.Status == (int)PaymentStatus.Paid);
            var payMethod = primaryPayment?.PaymentMethod == (int)PaymentMethod.BankTransfer 
                ? "Chuyển Khoản VietQR" 
                : (primaryPayment?.PaymentMethod == (int)PaymentMethod.Cash ? "Tiền Mặt" : "Chưa thanh toán");
            return new ReconciliationOrderDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                TableName = o.Table?.Name ?? (o.OrderType == 1 ? "Mang Về" : "Tại Bàn"),
                OrderType = o.OrderType == 1 ? "Mang Về" : "Tại Bàn",
                CreatedAt = o.CreatedAt,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                PaymentMethodName = payMethod,
                StatusName = o.PaymentStatus == (int)PaymentStatus.Paid ? "Đã thanh toán" : "Chờ thanh toán"
            };
        }).ToList();

        // Cash movements in this shift
        var cashMovements = await _context.CashMovements
            .AsNoTracking()
            .Where(cm => cm.ShiftId == shiftId)
            .ToListAsync();

        var tillIn = cashMovements.Where(cm => cm.Type == (int)CashMovementType.TillIn).Sum(cm => cm.Amount);
        var tillOut = cashMovements.Where(cm => cm.Type == (int)CashMovementType.TillOut).Sum(cm => cm.Amount);
        var refunds = cashMovements.Where(cm => cm.Type == (int)CashMovementType.CashRefund).Sum(cm => cm.Amount);

        var cashSales = reconItems.FirstOrDefault(i => i.PaymentMethod == (int)PaymentMethod.Cash)?.SystemAmount ?? 0m;
        var transferSales = reconItems.FirstOrDefault(i => i.PaymentMethod == (int)PaymentMethod.BankTransfer)?.SystemAmount ?? 0m;
        var totalRev = cashSales + transferSales;

        var expectedCash = shift.OpeningBalance + cashSales + tillIn - tillOut - refunds;
        var actualCash = reconItems.FirstOrDefault(i => i.PaymentMethod == (int)PaymentMethod.Cash)?.ActualAmount ?? (shift.ActualCashCounted ?? expectedCash);

        // Sales summary for this shift
        var grossSales = orders.Sum(o => o.SubTotal);
        var totalDiscounts = orders.Sum(o => o.DiscountAmount);
        var totalOrders = orders.Count;

        var salesSummary = new StoreSalesSummaryDto
        {
            FromDate = shift.OpenedAt,
            ToDate = shift.ClosedAt ?? DateTime.UtcNow,
            GrossSales = grossSales > 0 ? grossSales : totalRev,
            TotalDiscounts = totalDiscounts,
            TotalOrders = totalOrders,
            TotalItemsSold = 0,
            CashSales = cashSales,
            TransferSales = transferSales,
            OtherSales = 0
        };

        return new ShiftReconciliationDetailDto
        {
            ShiftId = shift.Id,
            StaffName = shift.User?.FullName ?? "Thu ngân",
            RegisterName = shift.Register?.Name ?? "Máy POS chính",
            StartTime = shift.OpenedAt,
            EndTime = shift.ClosedAt,
            Status = shift.Status,
            StatusName = shift.Status == 0 ? "Đang mở" : "Đã đóng ca",
            OpeningBalance = shift.OpeningBalance,
            ExpectedCash = expectedCash,
            ActualCash = actualCash,
            Difference = actualCash - expectedCash,
            Note = shift.Note,

            CashSales = cashSales,
            TransferSales = transferSales,
            TotalRevenue = totalRev,
            TotalCashOrders = shift.TotalCashOrders > 0 ? shift.TotalCashOrders : orders.Count(o => o.Payments.Any(p => p.PaymentMethod == (int)PaymentMethod.Cash)),
            TotalTransferOrders = shift.TotalTransferOrders > 0 ? shift.TotalTransferOrders : orders.Count(o => o.Payments.Any(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer)),

            TotalTillIn = tillIn,
            TotalTillOut = tillOut,
            TotalRefunds = refunds,

            Orders = orderDtos,
            ReconciliationItems = reconItems,
            SalesSummary = salesSummary
        };
    }

    public async Task<StoreSalesSummaryDto> GetStoreSalesSummaryAsync(DateTime fromDate, DateTime toDate)
    {
        return await GetStoreSalesSummaryInternalAsync(fromDate, toDate, null);
    }

    public async Task<StoreSalesSummaryDto> GetStoreSalesSummaryAsync(DateTime? fromDate, DateTime? toDate, int? shiftId = null)
    {
        return await GetStoreSalesSummaryInternalAsync(fromDate, toDate, shiftId);
    }

    private async Task<StoreSalesSummaryDto> GetStoreSalesSummaryInternalAsync(DateTime? fromDate, DateTime? toDate, int? shiftId)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
            .Where(o => o.Status != (int)OrderStatus.Cancelled);

        if (shiftId.HasValue)
        {
            query = query.Where(o => o.ShiftId == shiftId.Value);
        }
        else
        {
            if (fromDate.HasValue) query = query.Where(o => o.CreatedAt >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        var completedOrders = await query.ToListAsync();

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

        var totalDirect = cashSales + transferSales;

        return new StoreSalesSummaryDto
        {
            FromDate = fromDate ?? DateTime.Today,
            ToDate = toDate ?? DateTime.Today.AddDays(1).AddTicks(-1),
            GrossSales = grossSales > 0 ? grossSales : totalDirect,
            TotalDiscounts = totalDiscounts,
            TotalOrders = totalOrders,
            TotalItemsSold = totalItems,
            CashSales = cashSales,
            TransferSales = transferSales,
            OtherSales = 0
        };
    }
}
