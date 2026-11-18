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

public class DailySalesReportDto
{
    public DateTime Date { get; set; }
    public string FormattedDate { get; set; } = string.Empty;

    // F&B Standard Financial Metrics
    public decimal GrossSales { get; set; } // Tổng doanh thu gộp (trước giảm giá)
    public decimal PromotionDiscount { get; set; } // Giảm giá do khuyến mãi / voucher
    public decimal ComplimentaryDiscount { get; set; } // Giảm giá do tặng món / đổi điểm
    public decimal TotalDiscounts { get; set; } // Tổng giảm giá (KM + Tặng món)
    public decimal NetSales { get; set; } // Doanh thu thuần (= GrossSales - TotalDiscounts)
    public decimal TotalRevenue { get; set; } // Tổng thực thu (= NetSales)
    public decimal VatAmount { get; set; } // Thuế VAT bóc tách
    public decimal TotalRefunds { get; set; } // Hoàn tiền

    // Order Metrics
    public int TotalOrders { get; set; } // Số đơn hoàn thành & đã thanh toán
    public int DineInOrdersCount { get; set; } // Số đơn phục vụ tại bàn
    public int TakeawayOrdersCount { get; set; } // Số đơn mang về
    public int CancelledOrdersCount { get; set; } // Số đơn hủy
    public decimal AverageOrderValue { get; set; } // Giá trị trung bình/đơn (AOV)

    // Payment Methods Breakdown
    public decimal CashRevenue { get; set; } // Doanh thu tiền mặt
    public int CashOrdersCount { get; set; } // Số lượng đơn tiền mặt
    public decimal VietQrRevenue { get; set; } // Doanh thu chuyển khoản VietQR
    public int VietQrOrdersCount { get; set; } // Số lượng đơn chuyển khoản VietQR
    public List<PaymentMethodSummaryDto> PaymentMethods { get; set; } = new();

    // Shifts & Orders
    public List<DailyShiftSummaryDto> Shifts { get; set; } = new();
    public List<DailyOrderSummaryDto> Orders { get; set; } = new();
}

public class PaymentMethodSummaryDto
{
    public string MethodName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class DailyShiftSummaryDto
{
    public int ShiftId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string OpenedByName { get; set; } = string.Empty;
    public string? CashCustodianName { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCashAmount { get; set; }
    public decimal TotalTransferAmount { get; set; }
    public decimal? ClosingBalance { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class DailyOrderSummaryDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string OrderType { get; set; } = "Tại bàn";
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
}
