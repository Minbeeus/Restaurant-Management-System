using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/tables")]
public class TablesController : ControllerBase
{
    private readonly ITableService _tableService;

    public TablesController(ITableService tableService)
    {
        _tableService = tableService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int? areaId = null, [FromQuery] int? status = null)
    {
        var tables = await _tableService.GetAllAsync();
        
        if (areaId.HasValue)
        {
            tables = tables.Where(t => t.AreaId == areaId.Value).ToList();
        }
        
        if (status.HasValue)
        {
            tables = tables.Where(t => t.Status == status.Value).ToList();
        }

        return Ok(tables);
    }

    [HttpGet("area/{areaId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByAreaId(int areaId)
    {
        var tables = await _tableService.GetByAreaIdAsync(areaId);
        return Ok(tables);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var table = await _tableService.GetByIdAsync(id);
        if (table == null) return NotFound(new { message = "Không tìm thấy bàn ăn." });

        return Ok(table);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateTableRequest request)
    {
        try
        {
            var result = await _tableService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTableRequest request)
    {
        try
        {
            var result = await _tableService.UpdateAsync(id, request);
            if (result == null) return NotFound(new { message = "Không tìm thấy bàn ăn." });

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
        var success = await _tableService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy bàn ăn." });

        return NoContent();
    }

    [HttpGet("{id}/qr-code")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQrCode(int id)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var qrImageBytes = await _tableService.GetTableQrCodeImageAsync(id, baseUrl);

        if (qrImageBytes == null)
            return NotFound(new { message = "Không tìm thấy bàn ăn để sinh mã QR." });

        return File(qrImageBytes, "image/png", $"table_{id}_qr.png");
    }
}
