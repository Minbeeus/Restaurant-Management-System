using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [AllowAnonymous]
    public async Task<IActionResult> GetTablesOverview()
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                data = new List<TableStatusOverviewDto>(),
                error = new { code = "TABLE_OVERVIEW_ERROR", message = ex.Message, inner = ex.InnerException?.Message },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("takeaway/orders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTakeawayOrders()
    {
        try
        {
            var result = await _posOrderService.GetTakeawayOrdersAsync();
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
            return StatusCode(500, new
            {
                success = false,
                data = new List<TakeawayOrderDto>(),
                error = new { code = "TAKEAWAY_ORDERS_ERROR", message = ex.Message },
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("takeaway/orders/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTakeawayOrderById(int id)
    {
        var result = await _posOrderService.GetTakeawayOrderByIdAsync(id);
        if (result == null) return NotFound(new { success = false, message = "Không tìm thấy đơn mang về." });

        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("tables/{tableId}/active-order")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveOrderForTable(int tableId)
    {
        var result = await _posOrderService.GetActiveOrderForTableAsync(tableId);
        if (result == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng cho bàn này." });

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

    [Authorize(Roles = "Admin,Manager,Cashier")]
    [HttpPost("orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(
        int id, 
        [FromBody] CancelOrderRequest request, 
        [FromServices] ICancelOrderService cancelOrderService, 
        [FromServices] ICurrentUserService currentUserService, 
        [FromServices] IIdentityService identityService,
        [FromServices] Restaurant.Infrastructure.Persistence.ApplicationDbContext context)
    {
        if (!currentUserService.UserId.HasValue)
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

        int actingUserId = currentUserService.UserId.Value;

        // If Cashier calls, Manager PIN approval is strictly required
        if (User.IsInRole("Cashier"))
        {
            if (string.IsNullOrWhiteSpace(request.ManagerPin))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "FORBIDDEN_MANAGER_PIN_REQUIRED",
                        message = "Thu ngân yêu cầu mã PIN của Quản lý để thực hiện hủy đơn."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            var isPinValid = await identityService.VerifyManagerPinAsync(request.ManagerPin);
            if (!isPinValid)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "FORBIDDEN_INVALID_MANAGER_PIN",
                        message = "Mã PIN Quản lý không chính xác."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            var manager = await context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IsActive && (u.Role.Name == "Manager" || u.Role.Name == "Admin"));
            if (manager != null)
            {
                actingUserId = manager.Id;
            }
        }

        var success = await cancelOrderService.CancelOrderAsync(actingUserId, id, request.Reason);
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

    [Authorize(Roles = "Admin,Manager,Cashier")]
    [HttpPost("orders/{orderId}/items/{orderItemId}/void")]
    public async Task<IActionResult> VoidOrderItem(int orderId, int orderItemId, [FromBody] VoidOrderItemRequest request, [FromServices] ICurrentUserService currentUserService, [FromServices] IIdentityService identityService)
    {
        if (!currentUserService.UserId.HasValue)
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

        // If Cashier calls, Manager PIN approval is strictly required
        if (User.IsInRole("Cashier"))
        {
            if (string.IsNullOrWhiteSpace(request.ManagerPin))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "FORBIDDEN_MANAGER_PIN_REQUIRED",
                        message = "Thu ngân yêu cầu mã PIN của Quản lý để thực hiện hủy món."
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            var isPinValid = await identityService.VerifyManagerPinAsync(request.ManagerPin);
            if (!isPinValid)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "FORBIDDEN_INVALID_MANAGER_PIN",
                        message = "Mã PIN Quản lý không chính xác."
                    },
                    timestamp = DateTime.UtcNow
                });
            }
        }

        int actingUserId = currentUserService.UserId.Value;
        var success = await _posOrderService.VoidOrderItemAsync(orderId, orderItemId, request?.Reason ?? "Hủy món", actingUserId);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "VOID_ITEM_FAILED",
                    message = "Hủy món không thành công."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Hủy món thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("orders/submit")]
    [AllowAnonymous]
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

    [HttpPost("orders/{id}/print-temp-bill")]
    [AllowAnonymous]
    public async Task<IActionResult> PrintTempBill(int id)
    {
        var success = await _posOrderService.PrintTempBillAsync(id);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "ORDER_NOT_FOUND",
                    message = "Không tìm thấy đơn hàng để in tạm tính."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Đã in tạm tính và chuyển bàn sang trạng thái Chờ thanh toán (Billing)." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("tables/{id}/clear")]
    [AllowAnonymous]
    public async Task<IActionResult> ClearTable(int id)
    {
        var success = await _posOrderService.ClearTableAsync(id);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "TABLE_NOT_FOUND",
                    message = "Không tìm thấy bàn để dọn."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Đã dọn bàn thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("orders/{id}/complete")]
    public async Task<IActionResult> CompleteOrder(int id)
    {
        var success = await _posOrderService.CompleteOrderAsync(id);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "ORDER_NOT_FOUND", message = "Không tìm thấy đơn hàng để hoàn thành." },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Đơn hàng đã được đánh dấu hoàn thành." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }
}
