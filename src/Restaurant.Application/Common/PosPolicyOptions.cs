namespace Restaurant.Application.Common;

public class PosPolicyOptions
{
    public decimal TillOutLimit { get; set; } = 1000000m; // 1,000,000 VNĐ
    public decimal DiscountPercentLimit { get; set; } = 15.0m; // 15% limit for cashiers
    public decimal DiscountAmountLimit { get; set; } = 200000m; // 200,000 VNĐ limit for cashiers
    public decimal RefundLimit { get; set; } = 500000m; // 500,000 VNĐ
    public decimal CashAdjustmentLimit { get; set; } = 500000m; // 500,000 VNĐ
    public bool RequireManagerApprovalForRefund { get; set; } = true;
    public bool RequireApprovalForShiftVariance { get; set; } = true;
    public int MaxPinAttemptsBeforeCooldown { get; set; } = 5;
    public int PinCooldownMinutes { get; set; } = 15;
}
