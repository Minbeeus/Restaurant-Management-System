using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs.Dashboard;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime? date = null)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);
        var prevDate = targetDate.AddDays(-1);

        // 1. Revenue
        var todayRevenue = await _context.Orders
            .Where(o => o.Status == 3 && o.CreatedAt >= targetDate && o.CreatedAt < nextDate) // 3 = Completed
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var yesterdayRevenue = await _context.Orders
            .Where(o => o.Status == 3 && o.CreatedAt >= prevDate && o.CreatedAt < targetDate)
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

        // 2. Active Orders
        var activeOrders = await _context.Orders
            .Where(o => o.Status == 0 || o.Status == 1 || o.Status == 2) // Pending, InProgress, Ready
            .CountAsync();

        // 3. Tables
        var totalTables = await _context.Tables.CountAsync();
        var occupiedTables = await _context.Tables.CountAsync(t => t.Status == 1); // 1 = Occupied

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
        var targetDate = date?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);

        var orders = await _context.Orders
            .Where(o => o.Status == 3 && o.CreatedAt >= targetDate && o.CreatedAt < nextDate)
            .Select(o => new { o.CreatedAt.Hour, o.TotalAmount })
            .ToListAsync();

        var hourlyData = orders
            .GroupBy(o => o.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Total = g.Sum(x => x.TotalAmount)
            })
            .ToDictionary(k => k.Hour, v => v.Total);

        var result = new List<SalesChartPointDto>();
        // Return hours from 06:00 to 23:00
        for (int i = 6; i <= 23; i++)
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
        var targetDate = date?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);

        var topMovers = await _context.OrderItems
            .Where(oi => oi.Order.Status == 3 && oi.Order.CreatedAt >= targetDate && oi.Order.CreatedAt < nextDate)
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
            .Where(o => o.Status == 3)
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
