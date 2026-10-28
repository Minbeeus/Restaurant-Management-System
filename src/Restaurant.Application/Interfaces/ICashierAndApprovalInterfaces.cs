using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task<HashSet<string>> GetUserPermissionsAsync(int userId);
    Task EnsurePermissionAsync(int userId, string permission);
}

public interface IApprovalService
{
    Task<ApprovalRequestDto> CreateRequestAsync(int requestedByUserId, CreateApprovalRequestDto request);
    Task<List<ApprovalRequestDto>> GetPendingRequestsAsync();
    Task<List<ApprovalRequestDto>> GetRequestsHistoryAsync(int? status = null, int? type = null);
    Task<ApprovalRequestDto> ApproveRequestAsync(int approverUserId, ProcessApprovalRequestDto request);
    Task<ApprovalRequestDto> ApproveByManagerPinAsync(ManagerPinApprovalRequestDto request);
    Task<ApprovalRequestDto> RejectRequestAsync(int rejecterUserId, ProcessApprovalRequestDto request);
}

public interface ICashMovementService
{
    Task<CashMovementDto> PerformTillInAsync(int userId, int shiftId, TillInRequest request);
    Task<CashMovementDto> PerformTillOutAsync(int userId, int shiftId, TillOutRequest request);
    Task<CashMovementDto> PerformCashAdjustmentAsync(int managerUserId, int shiftId, CashAdjustmentRequest request);
    Task<CashMovementSummaryDto> GetShiftCashMovementsAsync(int shiftId);
    Task<List<CashMovementDto>> GetAllStoreCashMovementsAsync(DateTime? fromDate, DateTime? toDate);
}

public interface IBillViewerService
{
    Task<List<BillDetailDto>> QueryBillsAsync(BillQueryRequest request);
    Task<BillDetailDto?> GetBillDetailsAsync(int orderId);
    Task<K80ReceiptDto?> GetBillK80ReceiptAsync(int orderId);
}

public interface IReconciliationService
{
    Task<List<PaymentReconciliationItemDto>> GetShiftReconciliationPreviewAsync(int shiftId);
    Task<bool> SaveShiftReconciliationAsync(int userId, SaveReconciliationRequest request);
    Task<StoreSalesSummaryDto> GetStoreSalesSummaryAsync(DateTime fromDate, DateTime toDate);
    Task<StoreSalesSummaryDto> GetStoreSalesSummaryAsync(DateTime? fromDate, DateTime? toDate, int? shiftId = null);
    Task<ShiftReconciliationDetailDto> GetShiftDetailForReconciliationAsync(int shiftId);
}
