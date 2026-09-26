namespace Restaurant.Domain.Enums;

public enum TableStatus
{
    Available = 0,
    Occupied = 1,
    NewOrder = 2,
    Billing = 3
}

public enum OrderType
{
    DineIn = 0,
    Pickup = 1
}

public enum OrderStatus
{
    Draft = 0,
    Pending = 1,
    InPreparation = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5
}

public enum OrderItemStatus
{
    Active = 0,
    Voided = 1
}

public enum KitchenStatus
{
    Pending = 0,
    Processing = 1,
    Done = 2
}

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1
}

public enum PaymentStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    PartiallyRefunded = 3,
    Refunded = 4,
    Failed = 5
}

public enum ShiftStatus
{
    Open = 0,
    Closed = 1,
    PendingClosingApproval = 2
}

public enum ApprovalType
{
    Refund = 0,
    Void = 1,
    TillOut = 2,
    ShiftClosing = 3,
    CashAdjustment = 4,
    DiscountExceeded = 5,
    ReopenShift = 6,
    CancelOrder = 7
}

public enum ApprovalStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Executed = 4
}

public enum CashMovementType
{
    OpeningCash = 0,
    CashSale = 1,
    TillIn = 2,
    TillOut = 3,
    CashRefund = 4,
    CashAdjustment = 5
}

public enum VoucherDiscountType
{
    Percent = 0,
    FixedAmount = 1
}

public enum LoyaltyTransactionType
{
    Earn = 0,
    Redeem = 1,
    Reversal = 2
}

public enum StocktakeStatus
{
    InProgress = 0,
    Completed = 1
}

public enum InventoryTransactionType
{
    Import = 1,
    AuditAdjustment = 2,
    SaleDeduction = 3,
    CancellationReturn = 4,
    CancellationWaste = 5
}
