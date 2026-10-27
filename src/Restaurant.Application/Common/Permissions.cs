namespace Restaurant.Application.Common;

public static class Permissions
{
    // Shift Permissions
    public const string PosAccess = "POS.Access";
    public const string KdsAccess = "KDS.Access";

    public const string ShiftOpen = "Shift.Open";
    public const string ShiftClose = "Shift.Close";
    public const string ShiftViewOwn = "Shift.ViewOwn";
    public const string ShiftViewAll = "Shift.ViewAll";
    public const string ShiftApproveVariance = "Shift.ApproveVariance";
    public const string ShiftReopen = "Shift.Reopen";

    // Cash Permissions
    public const string CashTillIn = "Cash.TillIn";
    public const string CashTillOut = "Cash.TillOut";
    public const string CashTillOutLimit = "Cash.TillOutLimit"; // Threshold bypass permission
    public const string CashViewMovement = "Cash.ViewMovement";
    public const string CashAdjust = "Cash.Adjust";

    // Payment & Refund Permissions
    public const string PaymentProcess = "Payment.Process";
    public const string PaymentView = "Payment.View";
    public const string PaymentVoid = "Payment.Void";
    public const string RefundRequest = "Refund.Request";
    public const string RefundApprove = "Refund.Approve";

    // Bill & Sales Permissions
    public const string BillView = "Bill.View";
    public const string BillPrint = "Bill.Print";
    public const string SalesViewOwnShift = "Sales.ViewOwnShift";
    public const string SalesViewAll = "Sales.ViewAll";

    // Reconciliation & Approval Permissions
    public const string ReconciliationView = "Reconciliation.View";
    public const string ReconciliationApprove = "Reconciliation.Approve";
    public const string ApprovalViewCenter = "Approval.ViewCenter";
    public const string ApprovalProcess = "Approval.Process";

    // System Admin Permissions
    public const string SystemUserManage = "System.UserManage";
    public const string SystemRoleManage = "System.RoleManage";
    public const string SystemPermissionManage = "System.PermissionManage";
    public const string SystemRegisterManage = "System.RegisterManage";
    public const string SystemPolicyManage = "System.PolicyManage";

    private static readonly HashSet<string> AdminPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        PosAccess,
        KdsAccess,
        SystemUserManage,
        SystemRoleManage,
        SystemPermissionManage,
        SystemRegisterManage,
        SystemPolicyManage,
        ShiftViewAll,
        ShiftApproveVariance,
        ShiftReopen,
        SalesViewAll,
        ReconciliationView,
        ReconciliationApprove,
        ApprovalViewCenter,
        ApprovalProcess,
        RefundApprove,
        PaymentVoid,
        CashAdjust,
        CashTillOutLimit
    };

    private static readonly HashSet<string> CashierPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        PosAccess,
        ShiftOpen,
        ShiftClose,
        ShiftViewOwn,
        CashTillIn,
        CashTillOut,
        CashViewMovement,
        PaymentProcess,
        PaymentView,
        RefundRequest,
        BillView,
        BillPrint,
        SalesViewOwnShift
    };

    private static readonly HashSet<string> ChefPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        KdsAccess
    };

    private static readonly HashSet<string> ManagerPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        PosAccess,
        KdsAccess,
        ShiftOpen,
        ShiftClose,
        ShiftViewOwn,
        ShiftViewAll,
        ShiftApproveVariance,
        ShiftReopen,
        CashTillIn,
        CashTillOut,
        CashTillOutLimit,
        CashViewMovement,
        CashAdjust,
        PaymentProcess,
        PaymentView,
        PaymentVoid,
        RefundRequest,
        RefundApprove,
        BillView,
        BillPrint,
        SalesViewOwnShift,
        SalesViewAll,
        ReconciliationView,
        ReconciliationApprove,
        ApprovalViewCenter,
        ApprovalProcess
    };

    public static HashSet<string> GetPermissionsForRole(string roleName)
    {
        if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return AdminPermissions;
        }

        if (string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            return ManagerPermissions;
        }

        if (string.Equals(roleName, "Cashier", StringComparison.OrdinalIgnoreCase))
        {
            return CashierPermissions;
        }

        if (string.Equals(roleName, "Chef", StringComparison.OrdinalIgnoreCase))
        {
            return ChefPermissions;
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
