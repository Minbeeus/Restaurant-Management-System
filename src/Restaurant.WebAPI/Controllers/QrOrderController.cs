using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/qr/orders")]
[AllowAnonymous]
public class QrOrderController : ControllerBase
{
    private readonly IQrOrderService _qrOrderService;

    public QrOrderController(IQrOrderService qrOrderService)
    {
        _qrOrderService = qrOrderService;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitQrOrderRequest request)
    {
        try
        {
            var response = await _qrOrderService.SubmitQrOrderAsync(request);
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
                    code = "TOO_MANY_REQUESTS",
                    message = ex.Message
                },
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
                    code = "BAD_REQUEST",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INTERNAL_SERVER_ERROR",
                    message = "Đã xảy ra lỗi khi gửi đơn hàng.",
                    details = new[] { ex.Message }
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetOrderStatus([FromQuery] int tableId, [FromQuery] string token)
    {
        try
        {
            var status = await _qrOrderService.GetOrderStatusForQrAsync(tableId, token);
            if (status == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "NOT_FOUND",
                        message = "Bàn hiện chưa có đơn hàng nào."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = status,
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
}
