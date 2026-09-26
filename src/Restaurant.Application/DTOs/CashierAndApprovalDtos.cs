namespace Restaurant.Application.DTOs;

public class TillInRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class TillOutRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CashAdjustmentRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CreateApprovalRequestDto
{
    public int Type { get; set; } // ApprovalType enum
    public int? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class ProcessApprovalRequestDto
{
    public int ApprovalRequestId { get; set; }
    public string? ApprovalNote { get; set; }
}

public class ManagerPinApprovalRequestDto
{
    public int ApprovalRequestId { get; set; }
    public string ManagerPin { get; set; } = string.Empty;
    public string? ApprovalNote { get; set; }
}

public class ApprovalRequestDto
{
    public int Id { get; set; }
    public int Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }

    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public int? RejectedByUserId { get; set; }
    public string? RejectedByUserName { get; set; }
    public DateTime? RejectedAt { get; set; }

    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ApprovalNote { get; set; }
}

public class CashMovementDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ApprovalRequestId { get; set; }
}

public class CashMovementSummaryDto
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalTillIn { get; set; }
    public decimal TotalTillOut { get; set; }
    public decimal TotalCashRefunds { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal ExpectedCashBalance => OpeningBalance + TotalCashSales + TotalTillIn - TotalTillOut - TotalCashRefunds + TotalAdjustments;
    public List<CashMovementDto> Movements { get; set; } = new();
}

public class BillQueryRequest
{
    public string? SearchKeyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PaymentMethod { get; set; }
    public int? OrderStatus { get; set; }
    public int? ShiftId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BillDetailDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethodName { get; set; } = "Tiền Mặt";
    public string OrderStatusName { get; set; } = "Hoàn thành";
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public List<BillItemDto> Items { get; set; } = new();
}

public class BillItemDto
{
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice => (UnitPrice - DiscountAmount) * Quantity;
    public List<string> Modifiers { get; set; } = new();
}

public class SaveReconciliationRequest
{
    public int ShiftId { get; set; }
    public List<PaymentReconciliationItemDto> Items { get; set; } = new();
    public string? Note { get; set; }
}

public class PaymentReconciliationItemDto
{
    public int PaymentMethod { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal SystemAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Difference => ActualAmount - SystemAmount;
    public string? Note { get; set; }
}

public class StoreSalesSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetSales => GrossSales - TotalDiscounts;
    public int TotalOrders { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal AverageOrderValue => TotalOrders > 0 ? NetSales / TotalOrders : 0;
    public decimal CashSales { get; set; }
    public decimal TransferSales { get; set; }
    public decimal OtherSales { get; set; }
}
