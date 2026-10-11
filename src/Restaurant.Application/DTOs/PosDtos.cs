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
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Shape { get; set; }
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
    public int? RegisterId { get; set; }
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
    public int? RegisterId { get; set; }
    public string? RegisterName { get; set; }
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

    public decimal OpeningBalance { get => InitialCash; set => InitialCash = value; }
    public decimal ClosingBalance { get => ExpectedCash; set => ExpectedCash = value; }
    public decimal ActualCashCounted { get => ActualCash; set => ActualCash = value; }
    public decimal Variance { get => Difference; set => Difference = value; }
}

public class SubmitPosOrderRequest
{
    public int? TableId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerNote { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
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

public class TakeawayOrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = "Khách mang về";
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderChannel { get; set; } = "QR";
    public bool IsQrOrder { get; set; }
    public int Status { get; set; } // 0: Chờ xác nhận, 1: Đang chuẩn bị, 2: Đã xong, 4: Đã huỷ
    public string MainStatusName { get; set; } = "Chờ xác nhận";
    public bool IsPaid { get; set; }
    public string DisplayStatus => IsPaid ? $"{MainStatusName} | $" : MainStatusName;
    public List<TakeawayOrderItemDto> Items { get; set; } = new();
}

public class TakeawayOrderItemDto
{
    public int OrderItemId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public string? Note { get; set; }
    public int? ParentItemId { get; set; }
    public int? ComboGroupId { get; set; }
    public int Status { get; set; }
    public List<string> Modifiers { get; set; } = new();
    public List<int> ModifierIds { get; set; } = new();
}

public class VoidOrderItemRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class VoucherValidationResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class K80ReceiptDto
{
    // Restaurant Metadata
    public string RestaurantName { get; set; } = "NHÀ HÀNG MINBEEUS";
    public string RestaurantAddress { get; set; } = "123 Nguyễn Văn Linh, Q.7, TP.HCM";
    public string RestaurantPhone { get; set; } = "0901.234.567";
    public string WifiInfo { get; set; } = "Wifi: Minbeeus_Guest / Pass: 88888888";
    public string ReceiptTitle { get; set; } = "HÓA ĐƠN THANH TOÁN";

    // Order Information
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime PrintedAt { get; set; } = DateTime.Now;
    public string TableName { get; set; } = string.Empty;
    public string OrderType { get; set; } = "DineIn";
    public string CashierName { get; set; } = "Thu Ngân POS";
    public string CustomerName { get; set; } = "Khách Lẻ";
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }

    // Financials (Authoritative from backend Order)
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Payment Lifecycle
    public bool IsPaid { get; set; }
    public string PaymentStatusName { get; set; } = "Chờ thanh toán";
    public string PaymentMethod { get; set; } = "Chưa thanh toán";
    public decimal? CashReceived { get; set; }
    public decimal? ChangeAmount { get; set; }

    // Customer / Footer message
    public string FooterMessage { get; set; } = "CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!\nVui lòng kiểm tra lại hóa đơn trước khi rời quầy";

    // Items list
    public List<K80ReceiptItemDto> Items { get; set; } = new();
}

public class K80ReceiptItemDto
{
    public int Id { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice => (UnitPrice - DiscountAmount) * Quantity;
    public bool IsComboChild { get; set; }
    public int? ParentItemId { get; set; }
    public List<string> Modifiers { get; set; } = new();
    public string? Note { get; set; }
}
