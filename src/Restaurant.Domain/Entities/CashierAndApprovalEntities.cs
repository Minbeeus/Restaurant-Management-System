using Restaurant.Domain.Common;

namespace Restaurant.Domain.Entities;

public class ApprovalRequest : AuditableEntity, IHasRowVersion
{
    public int Type { get; set; } // ApprovalType enum: 0=Refund, 1=Void, 2=TillOut, 3=ShiftClosing, 4=CashAdjustment
    public int? ReferenceId { get; set; } // OrderId, PaymentId, ShiftId, etc.
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public int? RejectedByUserId { get; set; }
    public User? RejectedByUser { get; set; }
    public DateTime? RejectedAt { get; set; }

    public int Status { get; set; } // ApprovalStatus enum: 0=PendingApproval, 1=Approved, 2=Rejected, 3=Cancelled, 4=Executed
    public string? ApprovalNote { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class CashMovement : AuditableEntity
{
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public int? RegisterId { get; set; }
    public Register? Register { get; set; }

    public int Type { get; set; } // CashMovementType enum: 0=OpeningCash, 1=CashSale, 2=TillIn, 3=TillOut, 4=CashRefund, 5=CashAdjustment
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
}

public class AuditLogEntry : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? RegisterId { get; set; }
    public int? ShiftId { get; set; }
    public string? CorrelationId { get; set; }

    public string Action { get; set; } = string.Empty; // e.g. "Shift.Open", "Till.Out", "Approval.Approve"
    public string EntityName { get; set; } = string.Empty; // e.g. "Shift", "ApprovalRequest", "Payment"
    public string? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class PaymentReconciliation : AuditableEntity
{
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public int PaymentMethod { get; set; } // 0=Cash, 1=BankTransfer, etc.
    public decimal SystemAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Difference => ActualAmount - SystemAmount;
    public string? Note { get; set; }

    public int ReconciledByUserId { get; set; }
    public User ReconciledByUser { get; set; } = null!;
}
