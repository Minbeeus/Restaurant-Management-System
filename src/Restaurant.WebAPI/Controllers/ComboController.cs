using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Common;

namespace Restaurant.WebAPI.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class ComboController : ControllerBase
{
    private readonly IComboService _comboService;

    public ComboController(IComboService comboService)
    {
        _comboService = comboService;
    }

    [HttpGet]
    [AllowAnonymous] // Pos có thể lấy menu ko cần token nếu Qr order, tuỳ cấu hình
    public async Task<IActionResult> GetAll()
    {
        var combos = await _comboService.GetAllCombosAsync();
        return Ok(combos);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var combo = await _comboService.GetComboByIdAsync(id);
        if (combo == null) return NotFound();
        return Ok(combo);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateComboRequest request)
    {
        var combo = await _comboService.CreateComboAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = combo.Id }, combo);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateComboRequest request)
    {
        var combo = await _comboService.UpdateComboAsync(id, request);
        if (combo == null) return NotFound();
        return Ok(combo);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _comboService.DeleteComboAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var success = await _comboService.ToggleComboStatusAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
