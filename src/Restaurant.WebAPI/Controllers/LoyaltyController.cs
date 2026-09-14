using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/loyalty")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    [HttpGet("customers/by-phone/{phoneNumber}")]
    public async Task<IActionResult> GetCustomerByPhone(string phoneNumber)
    {
        var customer = await _loyaltyService.GetCustomerByPhoneAsync(phoneNumber);
        if (customer == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "CUSTOMER_NOT_FOUND",
                    message = "Khách hàng không tồn tại."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = customer,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var result = await _loyaltyService.CreateCustomerAsync(request);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("redeem")]
    public IActionResult RedeemPoints([FromBody] RedeemPointsRequest request)
    {
        return BadRequest(new
        {
            success = false,
            data = (object?)null,
            error = new
            {
                code = "REDEEM_FAILED",
                message = "Vui lòng chỉ định CustomerId để đổi điểm."
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("redeem/{customerId}")]
    public async Task<IActionResult> RedeemPointsForCustomer(int customerId, [FromBody] RedeemPointsRequest request)
    {
        try
        {
            var success = await _loyaltyService.RedeemPointsAsync(customerId, request);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "REDEEM_FAILED",
                        message = "Đổi điểm không thành công."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = new { message = "Đổi điểm giảm giá thành công." },
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
                    code = "INSUFFICIENT_LOYALTY_POINTS",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}
