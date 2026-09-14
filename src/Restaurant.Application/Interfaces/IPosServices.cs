using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IPosOrderService
{
    Task<List<TableStatusOverviewDto>> GetTablesStatusAsync();
    Task<bool> ApproveQrOrderAsync(int orderId, string? note = null);
    Task<bool> MoveTableAsync(MoveTableRequest request);
    Task<bool> MergeTablesAsync(MergeTableRequest request);
    Task<SubmitPosOrderResponse> SubmitPosOrderAsync(SubmitPosOrderRequest request);
}

public interface IShiftService
{
    Task<ShiftReportDto> OpenShiftAsync(int userId, OpenShiftRequest request);
    Task<ShiftReportDto> CloseShiftAsync(int userId, CloseShiftRequest request);
    Task<ShiftReportDto?> GetCurrentShiftAsync(int userId);
    Task<bool> HasActiveShiftAsync(int userId);
}
