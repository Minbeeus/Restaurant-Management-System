using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/shifts")]
[AllowAnonymous]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    private readonly ICurrentUserService _currentUserService;

    public ShiftsController(IShiftService shiftService, ICurrentUserService currentUserService)
    {
        _shiftService = shiftService;
        _currentUserService = currentUserService;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentShift()
    {
        var userId = _currentUserService.UserId ?? 1;

        var shift = await _shiftService.GetCurrentShiftAsync(userId);
        if (shift == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "NO_ACTIVE_SHIFT",
                    message = "Hiện không có ca làm việc nào đang mở."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = shift,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("open")]
    public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request)
    {
        var userId = _currentUserService.UserId ?? 1;

        try
        {
            var result = await _shiftService.OpenShiftAsync(userId, request);
            return Ok(new
            {
                success = true,
                data = result,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "SHIFT_ERROR",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("close")]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request)
    {
        var userId = _currentUserService.UserId ?? 1;

        try
        {
            var report = await _shiftService.CloseShiftAsync(userId, request);
            return Ok(new
            {
                success = true,
                data = report,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "SHIFT_ERROR",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllShifts([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? status)
    {
        try
        {
            var data = await _shiftService.GetAllShiftsAsync(fromDate, toDate, status);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/reopen")]
    public async Task<IActionResult> ReopenShift(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? 1;
            var data = await _shiftService.ReopenShiftAsync(userId, id);
            return Ok(new { success = true, data, message = "Mở lại ca thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
