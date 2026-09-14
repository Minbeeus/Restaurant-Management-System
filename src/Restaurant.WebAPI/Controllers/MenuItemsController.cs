using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemService _menuItemService;

    public MenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Cashier,Chef")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _menuItemService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveMenuItems()
    {
        var items = await _menuItemService.GetActiveMenuItemsAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _menuItemService.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy món ăn." });

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        try
        {
            var result = await _menuItemService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuItemRequest request)
    {
        try
        {
            var result = await _menuItemService.UpdateAsync(id, request);
            if (result == null) return NotFound(new { message = "Không tìm thấy món ăn." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _menuItemService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy món ăn." });

        return NoContent();
    }

    [HttpPatch("{id}/toggle-sold-out")]
    [Authorize(Roles = "Admin,Manager,Chef,Cashier")]
    public async Task<IActionResult> ToggleSoldOut(int id)
    {
        var success = await _menuItemService.ToggleSoldOutAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy món ăn." });

        return Ok(new { message = "Trạng thái báo hết món đã được thay đổi." });
    }
}
