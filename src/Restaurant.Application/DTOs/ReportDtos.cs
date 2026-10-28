namespace Restaurant.Application.DTOs;

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal VietQrRevenue { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal GrowthPercentage { get; set; }
    public bool IsGrowthPositive { get; set; }
    public List<DailyRevenueItemDto> DailyBreakdown { get; set; } = new();
}

public class DailyRevenueItemDto
{
    public DateTime Date { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal VietQrAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TopSellingReportItemDto
{
    public int Rank { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public string EvaluationBadge { get; set; } = string.Empty; // "BestSeller", "HighSales", "Good"
}

public class TopSellingReportDto
{
    public int TotalSoldQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public string TopItemName { get; set; } = string.Empty;
    public List<TopSellingReportItemDto> Items { get; set; } = new();
}
