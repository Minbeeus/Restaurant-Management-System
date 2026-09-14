using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/qr")]
[AllowAnonymous]
public class QrMenuController : ControllerBase
{
    private readonly IQrOrderService _qrOrderService;

    public QrMenuController(IQrOrderService qrOrderService)
    {
        _qrOrderService = qrOrderService;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetPublicMenu([FromQuery] int tableId, [FromQuery] string token)
    {
        try
        {
            var menu = await _qrOrderService.GetPublicMenuAsync(tableId, token);
            return Ok(new
            {
                success = true,
                data = menu,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_QR_SESSION",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("menu-items/{id}")]
    public async Task<IActionResult> GetMenuItemDetail(int id, [FromQuery] int tableId, [FromQuery] string token)
    {
        try
        {
            var item = await _qrOrderService.GetMenuItemDetailAsync(id, tableId, token);
            if (item == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "NOT_FOUND",
                        message = "Không tìm thấy món ăn hoặc món ăn đã ngưng kinh doanh."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = item,
                error = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_QR_SESSION",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("cart/validate")]
    public async Task<IActionResult> ValidateCart([FromBody] CartValidateRequest request)
    {
        var result = await _qrOrderService.ValidateCartAsync(request);
        if (!result.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                data = result,
                error = new
                {
                    code = "INVALID_CART_SELECTION",
                    message = "Giỏ hàng không hợp lệ.",
                    details = result.ValidationErrors
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }
}
