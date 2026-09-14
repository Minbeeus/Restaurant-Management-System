namespace Restaurant.Application.DTOs;

public class TableStatusOverviewDto
{
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? CurrentOrderId { get; set; }
    public string? CurrentOrderCode { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? OrderCreatedAt { get; set; }
}

public class ApproveOrderRequest
{
    public string? Note { get; set; }
}

public class MoveTableRequest
{
    public int SourceTableId { get; set; }
    public int TargetTableId { get; set; }
}

public class MergeTableRequest
{
    public List<int> SourceTableIds { get; set; } = new();
    public int TargetTableId { get; set; }
}

public class OpenShiftRequest
{
    public decimal InitialCash { get; set; }
    public string? Note { get; set; }
}

public class CloseShiftRequest
{
    public decimal ActualCashCounted { get; set; }
    public string? Note { get; set; }
}

public class ShiftReportDto
{
    public int ShiftId { get; set; }
    public int UserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal InitialCash { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal BankingRevenue { get; set; }
    public int TotalCashOrders { get; set; }
    public int TotalTransferOrders { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Difference { get; set; }
    public string? Note { get; set; }
}

public class SubmitPosOrderRequest
{
    public int TableId { get; set; }
    public string? CustomerNote { get; set; }
    public List<CartItemValidateRequest> Items { get; set; } = new();
}

public class SubmitPosOrderResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
