namespace Restaurant.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public decimal RevenueTrendPercentage { get; set; }
    public bool IsRevenueTrendingUp { get; set; }

    public int ActiveOrders { get; set; }
    
    public int OccupiedTables { get; set; }
    public int TotalTables { get; set; }
    
    public int LowStockMaterials { get; set; }
}

public class SalesChartPointDto
{
    public string TimeLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TopMoverDto
{
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
}

public class ActivityFeedItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty; // e.g., "Order", "Inventory", "System"
}
