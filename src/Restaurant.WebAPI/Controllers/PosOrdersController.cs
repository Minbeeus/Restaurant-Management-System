using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/pos")]
[Authorize(Roles = "Admin,Manager,Cashier")]
public class PosOrdersController : ControllerBase
{
    private readonly IPosOrderService _posOrderService;

    public PosOrdersController(IPosOrderService posOrderService)
    {
        _posOrderService = posOrderService;
    }

    [HttpGet("tables/overview")]
    public async Task<IActionResult> GetTablesOverview()
    {
        var result = await _posOrderService.GetTablesStatusAsync();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("orders/{id}/approve")]
    public async Task<IActionResult> ApproveOrder(int id, [FromBody] ApproveOrderRequest? request)
    {
        var success = await _posOrderService.ApproveQrOrderAsync(id, request?.Note);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "ORDER_NOT_FOUND",
                    message = "Không tìm thấy đơn hàng cần duyệt."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Duyệt đơn hàng thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("tables/move")]
    public async Task<IActionResult> MoveTable([FromBody] MoveTableRequest request)
    {
        try
        {
            var success = await _posOrderService.MoveTableAsync(request);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "INVALID_MOVE_REQUEST",
                        message = "Không thể thực hiện chuyển bàn."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = new { message = "Chuyển bàn thành công." },
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
                    code = "TARGET_TABLE_OCCUPIED",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("tables/merge")]
    public async Task<IActionResult> MergeTables([FromBody] MergeTableRequest request)
    {
        var success = await _posOrderService.MergeTablesAsync(request);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_MERGE_REQUEST",
                    message = "Không thể thực hiện gộp bàn."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Gộp bàn thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("orders/{id}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request, [FromServices] ICancelOrderService cancelOrderService, [FromServices] ICurrentUserService currentUserService)
    {
        if (!currentUserService.UserId.HasValue) return Unauthorized();

        var success = await cancelOrderService.CancelOrderAsync(currentUserService.UserId.Value, id, request.Reason);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "CANCEL_ORDER_FAILED",
                    message = "Hủy đơn hàng không thành công."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Hủy đơn hàng và rollback thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("orders/submit")]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitPosOrderRequest request)
    {
        try
        {
            var result = await _posOrderService.SubmitPosOrderAsync(request);
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
                    code = "INVALID_ORDER",
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
                    code = "INTERNAL_ERROR",
                    message = ex.Message
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}
