using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/cashier")]
[AllowAnonymous]
public class CashierToolsController : ControllerBase
{
    private readonly ICashMovementService _cashMovementService;
    private readonly ICurrentUserService _currentUserService;

    public CashierToolsController(
        ICashMovementService cashMovementService,
        ICurrentUserService currentUserService)
    {
        _cashMovementService = cashMovementService;
        _currentUserService = currentUserService;
    }

    [HttpPost("till-in/{shiftId}")]
    public async Task<IActionResult> PerformTillIn(int shiftId, [FromBody] TillInRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? 1;
            var data = await _cashMovementService.PerformTillInAsync(userId, shiftId, request);
            return Ok(new { success = true, data, message = "Nộp tiền vào két (Till In) thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("till-out/{shiftId}")]
    public async Task<IActionResult> PerformTillOut(int shiftId, [FromBody] TillOutRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? 1;
            var data = await _cashMovementService.PerformTillOutAsync(userId, shiftId, request);
            return Ok(new { success = true, data, message = data.ApprovalRequestId.HasValue ? "Đã gửi Yêu cầu Phê duyệt rút tiền cho Quản lý." : "Rút tiền két (Till Out) thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("cash-adjustment/{shiftId}")]
    public async Task<IActionResult> PerformCashAdjustment(int shiftId, [FromBody] CashAdjustmentRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? 1;
            var data = await _cashMovementService.PerformCashAdjustmentAsync(userId, shiftId, request);
            return Ok(new { success = true, data, message = "Điều chỉnh số dư két thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("cash-movements/{shiftId}")]
    public async Task<IActionResult> GetShiftCashMovements(int shiftId)
    {
        try
        {
            var data = await _cashMovementService.GetShiftCashMovementsAsync(shiftId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("all-cash-movements")]
    public async Task<IActionResult> GetAllCashMovements([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var data = await _cashMovementService.GetAllStoreCashMovementsAsync(fromDate, toDate);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
