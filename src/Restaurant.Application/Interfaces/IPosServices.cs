using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IPosOrderService
{
    Task<List<TableStatusOverviewDto>> GetTablesStatusAsync();
    Task<bool> ApproveQrOrderAsync(int orderId, string? note = null);
    Task<bool> MoveTableAsync(MoveTableRequest request);
    Task<bool> MergeTablesAsync(MergeTableRequest request);
    Task<SubmitPosOrderResponse> SubmitPosOrderAsync(SubmitPosOrderRequest request);
    Task<bool> PrintTempBillAsync(int orderId);
    Task<bool> ClearTableAsync(int tableId);
    Task<List<TakeawayOrderDto>> GetTakeawayOrdersAsync();
    Task<TakeawayOrderDto?> GetTakeawayOrderByIdAsync(int orderId);
    Task<TakeawayOrderDto?> GetActiveOrderForTableAsync(int tableId);
}

public interface IShiftService
{
    Task<ShiftReportDto> OpenShiftAsync(int userId, OpenShiftRequest request);
    Task<ShiftReportDto> CloseShiftAsync(int userId, CloseShiftRequest request);
    Task<ShiftReportDto?> GetCurrentShiftAsync(int userId);
    Task<bool> HasActiveShiftAsync(int userId);
    Task<List<ShiftReportDto>> GetAllShiftsAsync(DateTime? fromDate, DateTime? toDate, int? status = null);
    Task<ShiftReportDto> ReopenShiftAsync(int managerUserId, int shiftId);
}
