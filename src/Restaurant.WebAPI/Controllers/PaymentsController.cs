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

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("generate-vietqr")]
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

    [HttpPost("sepay-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookPayload payload)
    {
        var signatureHeader = Request.Headers["X-SePay-Signature"].FirstOrDefault();
        var success = await _paymentService.ProcessSepayWebhookAsync(payload, signatureHeader);

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
}
