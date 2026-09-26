using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/reconciliation")]
[AllowAnonymous]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ICurrentUserService _currentUserService;

    public ReconciliationController(
        IReconciliationService reconciliationService,
        ICurrentUserService currentUserService)
    {
        _reconciliationService = reconciliationService;
        _currentUserService = currentUserService;
    }

    [HttpGet("preview/{shiftId}")]
    public async Task<IActionResult> GetShiftReconciliationPreview(int shiftId)
    {
        try
        {
            var data = await _reconciliationService.GetShiftReconciliationPreviewAsync(shiftId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("save")]
    public async Task<IActionResult> SaveShiftReconciliation([FromBody] SaveReconciliationRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? 1;
            var success = await _reconciliationService.SaveShiftReconciliationAsync(userId, request);
            return Ok(new { success, message = "Lưu đối soát thanh toán thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetStoreSalesSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var start = fromDate ?? DateTime.Today;
            var end = toDate ?? DateTime.Today.AddDays(1).AddTicks(-1);
            var data = await _reconciliationService.GetStoreSalesSummaryAsync(start, end);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
