using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Common;

namespace Restaurant.WebAPI.Controllers;

[Route("api/v1/[controller]")]
[Route("api/v1/combos")]
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
        try
        {
            var combo = await _comboService.CreateComboAsync(request);
            return StatusCode(StatusCodes.Status201Created, combo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi xử lý hệ thống: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateComboRequest request)
    {
        try
        {
            var combo = await _comboService.UpdateComboAsync(id, request);
            if (combo == null) return NotFound(new { message = "Không tìm thấy Combo." });
            return Ok(combo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi xử lý hệ thống: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _comboService.DeleteComboAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy Combo." });
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi khi xóa Combo: {ex.Message}" });
        }
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
