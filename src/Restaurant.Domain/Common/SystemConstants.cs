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
        public static string StationGroup(int stationId) => $"Station_{stationId}";
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Cashier = "Cashier";
        public const string Chef = "Chef";
        public const string Customer = "Customer";

        public const string AdminOrManager = "Admin,Manager";
        public const string AdminManagerOrCashier = "Admin,Manager,Cashier";
        public const string AdminManagerOrChef = "Admin,Manager,Chef";
        public const string Staff = "Admin,Manager,Cashier,Chef";
    }

    public static class PaymentMethods
    {
        public const string Cash = "Cash";
        public const string VietQR = "VietQR";
        public const string BankTransfer = "BankTransfer";
    }

    public static class ActivityTypes
    {
        public const string Order = "Order";
        public const string Cancelled = "Cancelled";
        public const string Payment = "Payment";
        public const string Inventory = "Inventory";
    }

    public static class HubEndpoints
    {
        public const string RestaurantHub = "hubs/restaurant";
    }
}
