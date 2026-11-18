using Restaurant.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/shifts")]
[Authorize(Roles = SystemConstants.Roles.AdminManagerOrCashier)]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public ShiftsController(IShiftService shiftService, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _shiftService = shiftService;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    [HttpGet("has-active")]
    [Authorize]
    public async Task<IActionResult> HasActiveShift()
    {
        var hasActive = await _shiftService.HasActiveShiftAsync(_currentUserService.UserId ?? 1);
        return Ok(new { success = true, hasActive });
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<IActionResult> GetCurrentShift()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "Chưa xác thực danh tính người dùng."
                },
                timestamp = DateTime.UtcNow
            });
        }

        var userId = _currentUserService.UserId.Value;

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
    [Authorize(Roles = SystemConstants.Roles.AdminOrManager)]
    public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_REQUEST",
                    message = "Dữ liệu yêu cầu mở ca không hợp lệ."
                },
                timestamp = DateTime.UtcNow
            });
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "Chưa xác thực danh tính người dùng."
                },
                timestamp = DateTime.UtcNow
            });
        }

        var userId = _currentUserService.UserId.Value;

        if (string.IsNullOrWhiteSpace(request.CashCustodianPin))
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "CASH_CUSTODIAN_PIN_REQUIRED",
                    message = "Bắt buộc nhập mã PIN 6 số của nhân viên thu ngân giữ két."
                },
                timestamp = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ManagerPin))
        {
            try
            {
                var isPinValid = await _identityService.VerifyManagerPinAsync(request.ManagerPin);
                if (!isPinValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        data = (object?)null,
                        error = new
                        {
                            code = "INVALID_MANAGER_PIN",
                            message = "Mã PIN Quản lý không chính xác."
                        },
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "PIN_LOCKED",
                        message = ex.Message
                    },
                    timestamp = DateTime.UtcNow
                });
            }
        }

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
    [Authorize(Roles = SystemConstants.Roles.AdminOrManager)]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "Chưa xác thực danh tính người dùng."
                },
                timestamp = DateTime.UtcNow
            });
        }

        var userId = _currentUserService.UserId.Value;

        try
        {
            var result = await _shiftService.CloseShiftAsync(userId, request);
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
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "UNAUTHORIZED",
                        message = "Chưa xác thực danh tính người dùng."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            var userId = _currentUserService.UserId.Value;
            var data = await _shiftService.ReopenShiftAsync(userId, id);
            return Ok(new { success = true, data, message = "Mở lại ca thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
