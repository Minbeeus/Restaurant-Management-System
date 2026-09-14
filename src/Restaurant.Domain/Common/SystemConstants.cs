namespace Restaurant.Domain.Common;

public static class SystemConstants
{
    public const int QrSubmitRateLimitSeconds = 30;
    public const int KitchenSlaWarningMinutes = 15;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static class ReferenceTypes
    {
        public const string OrderItem = "OrderItem";
        public const string Order = "Order";
        public const string GoodsReceipt = "GoodsReceipt";
        public const string Stocktake = "Stocktake";
    }

    public static class SignalRGroups
    {
        public const string Cashiers = "CashiersGroup";
        public const string Chefs = "ChefsGroup";
        public static string TableGroup(int tableId) => $"TableGroup_{tableId}";
    }
}
