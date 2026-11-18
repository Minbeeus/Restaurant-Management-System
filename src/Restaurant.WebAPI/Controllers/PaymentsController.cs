using Restaurant.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpPost("generate-vietqr")]
    [HttpPost("vietqr")]
    [Authorize]
    public async Task<IActionResult> GenerateVietQr([FromBody] GenerateVietQrRequest request)
    {
        var result = await _paymentService.GenerateVietQrAsync(request);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("sepay-webhook")]
    [HttpGet("/api/v1/payments/sepay-webhook")]
    [HttpGet("/api/payments/sepay-webhook")]
    [HttpGet("/api/sepay-webhook")]
    [HttpGet("/sepay-webhook")]
    [AllowAnonymous]
    public IActionResult SepayWebhookHealthCheck()
    {
        return Ok(new
        {
            success = true,
            status = "ready",
            service = "SePay Webhook Listener",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("sepay-webhook")]
    [HttpPost("/api/v1/payments/sepay-webhook")]
    [HttpPost("/api/payments/sepay-webhook")]
    [HttpPost("/api/sepay-webhook")]
    [HttpPost("/sepay-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookPayload payload, [FromQuery] string? apiKey = null)
    {
        var signatureHeader = Request.Headers["Authorization"].FirstOrDefault()
                              ?? Request.Headers["X-Api-Key"].FirstOrDefault()
                              ?? Request.Headers["X-SePay-Signature"].FirstOrDefault();

        var configuredApiKey = _configuration["SePay:ApiKey"]?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey) && string.IsNullOrEmpty(signatureHeader) && string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "MISSING_WEBHOOK_AUTHORIZATION",
                    message = "Thiếu thông tin xác thực SePay Webhook."
                },
                timestamp = DateTime.UtcNow
            });
        }

        var success = await _paymentService.ProcessSepayWebhookAsync(payload, signatureHeader, apiKey);

        if (!success)
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_WEBHOOK_SIGNATURE",
                    message = "Chữ ký xác thực SePay Webhook không hợp lệ."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Webhook SePay đã được xử lý thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("status/{orderId}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckPaymentStatus(int orderId)
    {
        var isPaid = await _paymentService.CheckPaymentStatusAsync(orderId);
        return Ok(new
        {
            success = true,
            orderId,
            isPaid,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("cash")]
    [Authorize(Roles = SystemConstants.Roles.AdminManagerOrCashier)]
    public async Task<IActionResult> CashPayment([FromBody] ProcessCashPaymentRequest request)
    {
        try
        {
            int? currentUserId = null;
            var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(subClaim, out var uid))
            {
                currentUserId = uid;
            }

            var result = await _paymentService.ProcessCashPaymentAsync(request, currentUserId);
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
                    code = "PAYMENT_FAILED",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}
