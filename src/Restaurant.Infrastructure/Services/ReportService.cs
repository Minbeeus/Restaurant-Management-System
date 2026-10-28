using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static TimeZoneInfo GetRestaurantTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    private static (DateTime startUtc, DateTime endUtc) ResolveDateRangeUtc(DateTime? fromDate, DateTime? toDate)
    {
        var tz = GetRestaurantTimeZone();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var from = fromDate?.Date ?? nowLocal.Date.AddDays(-30);
        var to = toDate?.Date ?? nowLocal.Date;

        if (from > to)
        {
            (from, to) = (to, from);
        }

        var startLocal = DateTime.SpecifyKind(from, DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(to.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);

        return (startUtc, endUtc);
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(fromDate, toDate);
        var tz = GetRestaurantTimeZone();
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Payments)
            .Where(o => (o.PaymentStatus == (int)PaymentStatus.Paid || o.Status == (int)OrderStatus.Completed)
                        && o.Status != (int)OrderStatus.Cancelled
                        && o.CreatedAt >= startUtc && o.CreatedAt <= endUtc)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        decimal cashTotal = 0;
        decimal vietQrTotal = 0;

        foreach (var o in orders)
        {
            var paidPayments = o.Payments.Where(p => p.Status == (int)PaymentStatus.Paid).ToList();
            if (paidPayments.Any())
            {
                cashTotal += paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
                vietQrTotal += paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);
            }
            else
            {
                // Fallback: If completed without separate payment record, count as Cash
                cashTotal += o.TotalAmount;
            }
        }

        var totalRevenue = orders.Sum(o => o.TotalAmount);

        // Previous period for growth comparison
        var duration = endUtc - startUtc;
        var prevStartUtc = startUtc - duration;
        var prevEndUtc = startUtc;

        var prevRevenue = await _context.Orders
            .Where(o => (o.PaymentStatus == (int)PaymentStatus.Paid || o.Status == (int)OrderStatus.Completed)
                        && o.Status != (int)OrderStatus.Cancelled
                        && o.CreatedAt >= prevStartUtc && o.CreatedAt < prevEndUtc)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        decimal growth = 0;
        if (prevRevenue > 0)
        {
            growth = Math.Round(((totalRevenue - prevRevenue) / prevRevenue) * 100, 1);
        }
        else if (totalRevenue > 0)
        {
            growth = 100;
        }

        // Daily breakdown
        var dailyGroups = orders
            .GroupBy(o => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc), tz).Date)
            .OrderByDescending(g => g.Key);

        var dailyBreakdown = new List<DailyRevenueItemDto>();
        foreach (var g in dailyGroups)
        {
            var dayDate = g.Key;
            decimal dayCash = 0;
            decimal dayQr = 0;

            foreach (var o in g)
            {
                var paidPayments = o.Payments.Where(p => p.Status == (int)PaymentStatus.Paid).ToList();
                if (paidPayments.Any())
                {
                    dayCash += paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
                    dayQr += paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);
                }
                else
                {
                    dayCash += o.TotalAmount;
                }
            }

            var isToday = dayDate == todayLocal;
            dailyBreakdown.Add(new DailyRevenueItemDto
            {
                Date = dayDate,
                FormattedDate = $"{dayDate:dd/MM/yyyy}{(isToday ? " (Hôm nay)" : "")}",
                OrderCount = g.Count(),
                CashAmount = dayCash,
                VietQrAmount = dayQr,
                TotalAmount = g.Sum(x => x.TotalAmount)
            });
        }

        return new RevenueReportDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = orders.Count,
            VietQrRevenue = vietQrTotal,
            CashRevenue = cashTotal,
            GrowthPercentage = Math.Abs(growth),
            IsGrowthPositive = growth >= 0,
            DailyBreakdown = dailyBreakdown
        };
    }

    public async Task<TopSellingReportDto> GetTopSellingReportAsync(DateTime? fromDate, DateTime? toDate, int limit = 50)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(fromDate, toDate);

        var query = _context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.MenuItem)
                .ThenInclude(m => m.Category)
            .Where(oi => (oi.Order.PaymentStatus == (int)PaymentStatus.Paid || oi.Order.Status == (int)OrderStatus.Completed)
                        && oi.Order.Status != (int)OrderStatus.Cancelled
                        && oi.Status != (int)OrderItemStatus.Voided
                        && oi.Order.CreatedAt >= startUtc && oi.Order.CreatedAt <= endUtc);

        var rawList = await query
            .Select(oi => new
            {
                oi.MenuItemId,
                oi.MenuItemName,
                CategoryName = oi.MenuItem != null && oi.MenuItem.Category != null ? oi.MenuItem.Category.Name : "Món chính",
                oi.Quantity,
                oi.UnitPrice
            })
            .ToListAsync();

        var grouped = rawList
            .GroupBy(x => new { x.MenuItemId, x.MenuItemName, x.CategoryName })
            .Select(g => new
            {
                g.Key.MenuItemId,
                ItemName = g.Key.MenuItemName,
                g.Key.CategoryName,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToList();

        var items = new List<TopSellingReportItemDto>();
        for (int i = 0; i < grouped.Count; i++)
        {
            var g = grouped[i];
            var rank = i + 1;
            var badge = rank switch
            {
                1 => "BestSeller",
                2 or 3 => "HighSales",
                _ => "Good"
            };

            items.Add(new TopSellingReportItemDto
            {
                Rank = rank,
                MenuItemId = g.MenuItemId,
                ItemName = g.ItemName,
                CategoryName = g.CategoryName,
                QuantitySold = g.QuantitySold,
                TotalRevenue = g.TotalRevenue,
                EvaluationBadge = badge
            });
        }

        return new TopSellingReportDto
        {
            TotalSoldQuantity = items.Sum(x => x.QuantitySold),
            TotalRevenue = items.Sum(x => x.TotalRevenue),
            TopItemName = items.FirstOrDefault()?.ItemName ?? "Chưa có",
            Items = items
        };
    }
}
