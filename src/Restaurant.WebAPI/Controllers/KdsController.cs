using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/kds")]
public class KdsController : ControllerBase
{
    private readonly IKitchenService _kitchenService;

    public KdsController(IKitchenService kitchenService)
    {
        _kitchenService = kitchenService;
    }

    [HttpGet("tickets/active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveTickets()
    {
        var tickets = await _kitchenService.GetActiveTicketsAsync();
        return Ok(tickets);
    }

    [HttpPatch("items/{id}/status")]
    [AllowAnonymous] // Changed for Demo purposes
    public async Task<IActionResult> UpdateItemStatus(int id, [FromBody] UpdateKitchenStatusRequest request)
    {
        var result = await _kitchenService.UpdateItemStatusAsync(id, request.KitchenStatus);
        if (result == null) return NotFound(new { message = "Không tìm thấy món ăn trong hàng chờ bếp." });

        return Ok(result);
    }

    [HttpPost("items/{menuItemId}/sold-out")]
    [Authorize(Roles = "Chef,Manager,Admin")]
    public async Task<IActionResult> ToggleSoldOut(int menuItemId)
    {
        var success = await _kitchenService.ToggleSoldOutAsync(menuItemId);
        if (!success) return NotFound(new { message = "Không tìm thấy món ăn để đổi trạng thái." });

        return Ok(new { message = "Cập nhật trạng thái báo hết món thành công." });
    }
}
