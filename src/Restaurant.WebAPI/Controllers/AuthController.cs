using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public AuthController(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResources> localizer)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _localizer = localizer;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _identityService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "LOGIN_FAILED",
                    message = _localizer["LOGIN_FAILED"].Value
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = response,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("quick-login")]
    [AllowAnonymous]
    public async Task<IActionResult> QuickLogin([FromBody] QuickLoginRequest request)
    {
        var response = await _identityService.QuickLoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "LOGIN_FAILED",
                    message = _localizer["LOGIN_FAILED"].Value
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = response,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("pin-login")]
    [AllowAnonymous]
    public async Task<IActionResult> PinLogin([FromBody] PinLoginRequest request)
    {
        try
        {
            var response = await _identityService.PinLoginAsync(request);
            if (response == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "INVALID_PIN",
                        message = "Mã PIN không đúng, vui lòng thử lại."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = response,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(429, new
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

    [HttpPost("verify-manager-pin")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyManagerPin([FromBody] VerifyPinRequest request)
    {
        var isValid = await _identityService.VerifyManagerPinAsync(request.PinCode);
        return Ok(new
        {
            success = isValid,
            data = new { isValid },
            error = isValid ? null : new { code = "INVALID_PIN", message = "Mã PIN Quản lý không đúng." },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new
        {
            success = true,
            data = new { message = _localizer["LOGOUT_SUCCESS"].Value },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var user = await _identityService.GetUserByIdAsync(_currentUserService.UserId.Value);
        if (user == null) return NotFound();

        return Ok(new
        {
            success = true,
            data = user,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("admin-only-test")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnlyTest()
    {
        return Ok("Admin accessed");
    }
}
