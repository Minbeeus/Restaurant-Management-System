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

        // 3. Tables (Occupied includes Occupied, Serving, Billing, Paid)
        var totalTables = await _context.Tables.CountAsync();
        var occupiedTables = await _context.Tables.CountAsync(t => t.Status != (int)TableStatus.Available);

        // 4. Low Stock
        var lowStock = await _context.Materials
            .Where(m => m.IsActive && m.CurrentStock <= m.MinStockLevel)
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
        // For activity feed, we'll combine recent completed orders and recent inventory transactions
        var recentOrders = await _context.Orders
            .Where(o => o.Status == (int)OrderStatus.Completed || o.PaymentStatus == (int)PaymentStatus.Paid)
            .OrderByDescending(o => o.UpdatedAt)
            .Take(limit)
            .Select(o => new ActivityFeedItemDto
            {
                Title = $"Đơn hàng {o.OrderCode} hoàn thành",
                Description = $"Tổng tiền: {o.TotalAmount:N0}đ",
                Timestamp = o.UpdatedAt ?? o.CreatedAt,
                ActivityType = "Order"
            })
            .ToListAsync();

        var recentInventory = await _context.InventoryTransactions
            .Include(t => t.Material)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new ActivityFeedItemDto
            {
                Title = $"Biến động kho: {t.Material.Name}",
                Description = $"Số lượng: {(t.Quantity > 0 ? "+" : "")}{t.Quantity}",
                Timestamp = t.CreatedAt,
                ActivityType = "Inventory"
            })
            .ToListAsync();

        var feed = recentOrders.Concat(recentInventory)
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToList();

        return feed;
    }
}
