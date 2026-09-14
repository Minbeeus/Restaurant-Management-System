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
    Confirmed = 1,
    Completed = 2,
    Paid = 3,
    Cancelled = 4
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
    Pending = 0,
    Completed = 1,
    Failed = 2
}

public enum ShiftStatus
{
    Open = 0,
    Closed = 1
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
