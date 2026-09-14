using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/shifts")]
[Authorize(Roles = "Admin,Manager,Cashier")]
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
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var shift = await _shiftService.GetCurrentShiftAsync(_currentUserService.UserId.Value);
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
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        try
        {
            var result = await _shiftService.OpenShiftAsync(_currentUserService.UserId.Value, request);
            return Ok(new
            {
                success = true,
                data = result,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "SHIFT_ALREADY_OPEN",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("close")]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        try
        {
            var report = await _shiftService.CloseShiftAsync(_currentUserService.UserId.Value, request);
            return Ok(new
            {
                success = true,
                data = report,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "NO_ACTIVE_SHIFT",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}
