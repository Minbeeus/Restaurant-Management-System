using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs.Dashboard;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
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

    private static (DateTime startUtc, DateTime endUtc, DateTime prevStartUtc, DateTime prevEndUtc) GetUtcDateRanges(DateTime? date)
    {
        var tz = GetRestaurantTimeZone();
        var localDate = date?.Date ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

        var startLocal = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var endUtc = startUtc.AddDays(1);
        var prevStartUtc = startUtc.AddDays(-1);
        var prevEndUtc = startUtc;

        return (startUtc, endUtc, prevStartUtc, prevEndUtc);
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime? date = null)
    {
        var (startUtc, endUtc, prevStartUtc, prevEndUtc) = GetUtcDateRanges(date);

        // 1. Revenue (Paid orders)
        var todayRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == (int)PaymentStatus.Paid && o.CreatedAt >= startUtc && o.CreatedAt < endUtc)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var yesterdayRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == (int)PaymentStatus.Paid && o.CreatedAt >= prevStartUtc && o.CreatedAt < prevEndUtc)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        decimal trend = 0;
        if (yesterdayRevenue > 0)
        {
            trend = ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100;
        }
        else if (todayRevenue > 0)
        {
            trend = 100; // 100% increase if yesterday was 0 and today > 0
        }

        // 2. Active Orders (Not completed & Not cancelled)
        var activeOrders = await _context.Orders
            .Where(o => o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled)
            .CountAsync();

        // 3. Tables (Active tables currently being served in current open shift)
        var totalTables = await _context.Tables.CountAsync(t => t.IsActive && !t.IsDeleted);

        var currentShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open);
        int occupiedTables = 0;

        if (currentShift != null)
        {
            // Sync any desynced table statuses in DB
            var activeTableIds = await _context.Orders
                .Where(o => o.ShiftId == currentShift.Id &&
                            o.TableId.HasValue &&
                            o.Status != (int)OrderStatus.Completed &&
                            o.Status != (int)OrderStatus.Cancelled &&
                            o.PaymentStatus != (int)PaymentStatus.Paid)
                .Select(o => o.TableId!.Value)
                .Distinct()
                .ToListAsync();

            var orphanedTables = await _context.Tables
                .Where(t => (t.Status == (int)TableStatus.Serving || t.Status == (int)TableStatus.Billing) && !activeTableIds.Contains(t.Id))
                .ToListAsync();

            if (orphanedTables.Any())
            {
                foreach (var ot in orphanedTables) ot.Status = (int)TableStatus.Available;
                await _context.SaveChangesAsync();
            }

            occupiedTables = await _context.Tables.CountAsync(t => t.Status != (int)TableStatus.Available && !t.IsDeleted);
        }
        else
        {
            var hasShifts = await _context.Shifts.AnyAsync();
            if (hasShifts)
            {
                // In production where shifts exist but no shift is open, tables should be Available
                var orphaned = await _context.Tables
                    .Where(t => t.Status != (int)TableStatus.Available)
                    .ToListAsync();

                if (orphaned.Any())
                {
                    foreach (var ot in orphaned) ot.Status = (int)TableStatus.Available;
                    await _context.SaveChangesAsync();
                }

                occupiedTables = 0;
            }
            else
            {
                // In standalone test environments without shifts seeded
                occupiedTables = await _context.Tables.CountAsync(t => t.Status != (int)TableStatus.Available && !t.IsDeleted);
            }
        }

        // 4. Low Stock
        var lowStock = await _context.Materials
            .Where(m => m.IsActive && !m.IsDeleted && m.CurrentStock <= m.MinStockLevel)
            .CountAsync();

        return new DashboardSummaryDto
        {
            TodayRevenue = todayRevenue,
            RevenueTrendPercentage = Math.Round(trend, 1),
            IsRevenueTrendingUp = trend >= 0,
            ActiveOrders = activeOrders,
            TotalTables = totalTables,
            OccupiedTables = occupiedTables,
            LowStockMaterials = lowStock
        };
    }

    public async Task<List<SalesChartPointDto>> GetSalesChartAsync(DateTime? date = null)
    {
        var (startUtc, endUtc, _, _) = GetUtcDateRanges(date);
        var tz = GetRestaurantTimeZone();

        var orders = await _context.Orders
            .Where(o => o.PaymentStatus == (int)PaymentStatus.Paid && o.CreatedAt >= startUtc && o.CreatedAt < endUtc)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync();

        var hourlyData = orders
            .Select(o => new
            {
                Hour = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc), tz).Hour,
                o.TotalAmount
            })
            .GroupBy(o => o.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToDictionary(k => k.Hour, v => v.Total);

        var result = new List<SalesChartPointDto>();
        // Return hours from 00:00 to 23:00 for full 24-hour daily coverage
        for (int i = 0; i <= 23; i++)
        {
            result.Add(new SalesChartPointDto
            {
                TimeLabel = $"{i:D2}:00",
                Amount = hourlyData.GetValueOrDefault(i, 0)
            });
        }

        return result;
    }

    public async Task<List<TopMoverDto>> GetTopMoversAsync(DateTime? date = null, int limit = 5)
    {
        var (startUtc, endUtc, _, _) = GetUtcDateRanges(date);

        var topMovers = await _context.OrderItems
            .Where(oi => oi.Order.PaymentStatus == (int)PaymentStatus.Paid && oi.Order.CreatedAt >= startUtc && oi.Order.CreatedAt < endUtc)
            .GroupBy(oi => new { oi.MenuItemId, oi.MenuItemName })
            .Select(g => new TopMoverDto
            {
                MenuItemId = g.Key.MenuItemId,
                ItemName = g.Key.MenuItemName,
                QuantitySold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync();

        return topMovers;
    }

    public async Task<List<ActivityFeedItemDto>> GetActivityFeedAsync(int limit = 10)
    {
        var tz = GetRestaurantTimeZone();

        // Chỉ hiển thị các hoạt động liên quan đến đơn hàng (bỏ thông báo trừ định lượng kho)
        var recentOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var feed = recentOrders.Select(o =>
        {
            var tableText = o.Table != null ? $"Bàn {o.Table.Name}" : "Mang đi";
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.UpdatedAt ?? o.CreatedAt, DateTimeKind.Utc), tz);

            string title;
            string desc;
            string activityType = "Order";

            switch (o.Status)
            {
                case (int)OrderStatus.Completed:
                    title = $"Đơn hàng {o.OrderCode} - Hoàn thành";
                    desc = $"{tableText} • Tổng tiền: {o.TotalAmount:N0}đ";
                    break;
                case (int)OrderStatus.Cancelled:
                    title = $"Đơn hàng {o.OrderCode} - Đã hủy";
                    desc = $"{tableText} • {(!string.IsNullOrEmpty(o.Note) ? o.Note : "Đã hủy đơn")}";
                    activityType = "Cancelled";
                    break;
                case (int)OrderStatus.Ready:
                    title = $"Đơn hàng {o.OrderCode} - Sẵn sàng phục vụ";
                    desc = $"{tableText} • {o.OrderItems.Count} món • Tổng tiền: {o.TotalAmount:N0}đ";
                    break;
                case (int)OrderStatus.InPreparation:
                    title = $"Đơn hàng {o.OrderCode} - Bếp đang nấu";
                    desc = $"{tableText} • {o.OrderItems.Count} món • Tổng tiền: {o.TotalAmount:N0}đ";
                    break;
                case (int)OrderStatus.Pending:
                    title = $"Đơn hàng {o.OrderCode} - Chờ xác nhận";
                    desc = $"{tableText} • {o.OrderItems.Count} món • Tổng tiền: {o.TotalAmount:N0}đ";
                    break;
                default:
                    if (o.PaymentStatus == (int)PaymentStatus.Paid)
                    {
                        title = $"Đơn hàng {o.OrderCode} - Đã thanh toán";
                        desc = $"{tableText} • Tổng tiền: {o.TotalAmount:N0}đ";
                    }
                    else
                    {
                        title = $"Đơn hàng mới {o.OrderCode}";
                        desc = $"{tableText} • {o.OrderItems.Count} món • Tổng tiền: {o.TotalAmount:N0}đ";
                    }
                    break;
            }

            return new ActivityFeedItemDto
            {
                Title = title,
                Description = desc,
                Timestamp = localTime,
                ActivityType = activityType
            };
        }).ToList();

        return feed;
    }
}
