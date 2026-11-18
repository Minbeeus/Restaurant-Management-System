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

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date)
    {
        var tz = GetRestaurantTimeZone();
        var dateOnly = date.Date;
        var startLocal = DateTime.SpecifyKind(dateOnly, DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(dateOnly.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);

        // Query all orders on that day
        var allOrdersOnDay = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.Payments)
            .Include(o => o.CreatedByUser)
            .Include(o => o.OrderItems)
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var paidOrders = allOrdersOnDay
            .Where(o => (o.PaymentStatus == (int)PaymentStatus.Paid || o.Status == (int)OrderStatus.Completed)
                        && o.Status != (int)OrderStatus.Cancelled)
            .ToList();

        int cancelledOrdersCount = allOrdersOnDay.Count(o => o.Status == (int)OrderStatus.Cancelled);
        int dineInOrdersCount = paidOrders.Count(o => o.TableId != null);
        int takeawayOrdersCount = paidOrders.Count(o => o.TableId == null);
        int totalOrdersCount = paidOrders.Count;

        decimal totalGrossSales = 0;
        decimal totalPromotionDiscount = 0;
        decimal totalComplimentaryDiscount = 0;
        decimal totalNetSales = 0;

        decimal cashTotal = 0;
        int cashOrdersCount = 0;
        decimal vietQrTotal = 0;
        int vietQrOrdersCount = 0;

        var orderSummaries = new List<DailyOrderSummaryDto>();
        foreach (var o in paidOrders)
        {
            decimal orderComplimentary = o.LoyaltyDiscountAmount;
            decimal orderItemComplimentary = 0;
            decimal orderItemPromotion = 0;

            if (o.OrderItems != null && o.OrderItems.Any())
            {
                foreach (var item in o.OrderItems)
                {
                    bool isGiftItem = (!string.IsNullOrEmpty(item.Note) && 
                                       (item.Note.Contains("tặng", StringComparison.OrdinalIgnoreCase) || 
                                        item.Note.Contains("free", StringComparison.OrdinalIgnoreCase) ||
                                        item.Note.Contains("đổi điểm", StringComparison.OrdinalIgnoreCase)))
                                      || (item.DiscountAmount >= (item.UnitPrice * item.Quantity) && item.UnitPrice > 0);

                    if (isGiftItem)
                    {
                        orderItemComplimentary += item.DiscountAmount > 0 ? item.DiscountAmount : (item.UnitPrice * item.Quantity);
                    }
                    else if (item.DiscountAmount > 0)
                    {
                        orderItemPromotion += item.DiscountAmount;
                    }
                }
            }

            orderComplimentary += orderItemComplimentary;
            decimal orderPromotion = o.DiscountAmount + orderItemPromotion;
            decimal orderDiscountTotal = orderPromotion + orderComplimentary;

            decimal orderGross = o.SubTotal > 0 ? Math.Max(o.SubTotal, o.TotalAmount + orderDiscountTotal) : (o.TotalAmount + orderDiscountTotal);
            decimal orderNet = o.TotalAmount;

            totalGrossSales += orderGross;
            totalPromotionDiscount += orderPromotion;
            totalComplimentaryDiscount += orderComplimentary;
            totalNetSales += orderNet;

            var paidPayments = o.Payments.Where(p => p.Status == (int)PaymentStatus.Paid).ToList();
            string method = "Tiền mặt";
            if (paidPayments.Any())
            {
                var cashPart = paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.Cash).Sum(p => p.Amount);
                var qrPart = paidPayments.Where(p => p.PaymentMethod == (int)PaymentMethod.BankTransfer).Sum(p => p.Amount);

                if (cashPart > 0)
                {
                    cashTotal += cashPart;
                    cashOrdersCount++;
                }
                if (qrPart > 0)
                {
                    vietQrTotal += qrPart;
                    vietQrOrdersCount++;
                }

                if (qrPart > 0 && cashPart == 0) method = "VietQR";
                else if (qrPart > 0 && cashPart > 0) method = "Hỗn hợp";
            }
            else
            {
                cashTotal += o.TotalAmount;
                cashOrdersCount++;
            }

            var localCreated = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc), tz);
            orderSummaries.Add(new DailyOrderSummaryDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                TableName = o.Table?.Name ?? "Mang đi",
                OrderType = o.TableId != null ? "Tại bàn" : "Mang về",
                SubTotal = orderGross,
                DiscountAmount = orderDiscountTotal,
                TotalAmount = orderNet,
                PaymentMethodName = method,
                CreatedAt = localCreated,
                CashierName = o.CreatedByUser?.FullName ?? "Thu ngân",
                StatusName = o.Status == (int)OrderStatus.Completed ? "Hoàn thành" : "Đã thanh toán"
            });
        }

        var paymentMethods = new List<PaymentMethodSummaryDto>();
        if (cashTotal > 0 || totalNetSales == 0)
        {
            paymentMethods.Add(new PaymentMethodSummaryDto
            {
                MethodName = "Tiền mặt (Cash)",
                Icon = "payments",
                TotalAmount = cashTotal,
                TransactionCount = cashOrdersCount,
                Percentage = totalNetSales > 0 ? Math.Round((cashTotal / totalNetSales) * 100, 1) : 0
            });
        }
        if (vietQrTotal > 0)
        {
            paymentMethods.Add(new PaymentMethodSummaryDto
            {
                MethodName = "Chuyển khoản VietQR",
                Icon = "qr_code_2",
                TotalAmount = vietQrTotal,
                TransactionCount = vietQrOrdersCount,
                Percentage = totalNetSales > 0 ? Math.Round((vietQrTotal / totalNetSales) * 100, 1) : 0
            });
        }

        decimal totalDiscounts = totalPromotionDiscount + totalComplimentaryDiscount;
        decimal vatAmount = Math.Round(totalNetSales - (totalNetSales / 1.08m), 0);
        decimal aov = totalOrdersCount > 0 ? Math.Round(totalNetSales / totalOrdersCount, 0) : 0;

        // Query shifts on that day
        var shifts = await _context.Shifts
            .AsNoTracking()
            .Include(s => s.OpenedByUser)
            .Include(s => s.CashCustodianUser)
            .Include(s => s.ClosedByUser)
            .Where(s => s.OpenedAt >= startUtc && s.OpenedAt <= endUtc)
            .OrderBy(s => s.OpenedAt)
            .ToListAsync();

        var shiftSummaries = shifts.Select(s => new DailyShiftSummaryDto
        {
            ShiftId = s.Id,
            ShiftCode = s.ShiftCode ?? $"SHF-{s.Id}",
            OpenedByName = s.OpenedByUser?.FullName ?? "Quản lý",
            CashCustodianName = s.CashCustodianUser?.FullName ?? "Thu ngân giữ két",
            OpenedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(s.OpenedAt, DateTimeKind.Utc), tz),
            ClosedAt = s.ClosedAt.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(s.ClosedAt.Value, DateTimeKind.Utc), tz) : null,
            OpeningBalance = s.OpeningBalance,
            TotalCashAmount = s.TotalCashAmount,
            TotalTransferAmount = s.TotalTransferAmount,
            ClosingBalance = s.ClosingBalance,
            Status = s.Status,
            StatusName = s.Status == (int)ShiftStatus.Open ? "Đang mở" : "Đã đóng"
        }).ToList();

        return new DailySalesReportDto
        {
            Date = dateOnly,
            FormattedDate = dateOnly.ToString("dd/MM/yyyy"),
            GrossSales = totalGrossSales,
            PromotionDiscount = totalPromotionDiscount,
            ComplimentaryDiscount = totalComplimentaryDiscount,
            TotalDiscounts = totalDiscounts,
            NetSales = totalNetSales,
            TotalRevenue = totalNetSales,
            VatAmount = vatAmount,
            TotalRefunds = 0,
            TotalOrders = totalOrdersCount,
            DineInOrdersCount = dineInOrdersCount,
            TakeawayOrdersCount = takeawayOrdersCount,
            CancelledOrdersCount = cancelledOrdersCount,
            AverageOrderValue = aov,
            CashRevenue = cashTotal,
            CashOrdersCount = cashOrdersCount,
            VietQrRevenue = vietQrTotal,
            VietQrOrdersCount = vietQrOrdersCount,
            PaymentMethods = paymentMethods,
            Shifts = shiftSummaries,
            Orders = orderSummaries
        };
    }
}
