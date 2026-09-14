using Restaurant.Domain.Common;

namespace Restaurant.Domain.Entities;

public class Order : AuditableEntity, IHasRowVersion
{
    public string OrderCode { get; set; } = string.Empty;
    public int OrderType { get; set; }
    public int? TableId { get; set; }
    public Table? Table { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public int Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public decimal LoyaltyDiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? VoucherCode { get; set; }
    public string? Note { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<VoidLog> VoidLogs { get; set; } = new List<VoidLog>();
}

public class OrderItem : AuditableEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public string MenuItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
    public int KitchenStatus { get; set; }
    public DateTime? SentToKitchenAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<OrderItemModifier> OrderItemModifiers { get; set; } = new List<OrderItemModifier>();
}

public class OrderItemModifier
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int ModifierOptionId { get; set; }
    public ModifierOption ModifierOption { get; set; } = null!;
    public string ModifierOptionName { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

public class VoidLog
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int VoidByUserId { get; set; }
    public User VoidByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal? CashReceived { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? TransactionRef { get; set; }
    public string? SepayContent { get; set; }
    public int Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public SepayWebhookLog? SepayWebhookLog { get; set; }
}

public class SepayWebhookLog
{
    public int Id { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Content { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public string? ErrorLog { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public class Shift : IHasRowVersion
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ActualCashCounted { get; set; }
    public decimal? Difference { get; set; }
    public int TotalCashOrders { get; set; }
    public int TotalTransferOrders { get; set; }
    public decimal TotalCashAmount { get; set; }
    public decimal TotalTransferAmount { get; set; }
    public int Status { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Voucher : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public int DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}
