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
