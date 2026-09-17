using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/areas")]
public class AreasController : ControllerBase
{
    private readonly IAreaService _areaService;
    private readonly ITableService _tableService;

    public AreasController(IAreaService areaService, ITableService tableService)
    {
        _areaService = areaService;
        _tableService = tableService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var areas = await _areaService.GetAllAsync();
        return Ok(areas);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var area = await _areaService.GetByIdAsync(id);
        if (area == null) return NotFound(new { message = "Không tìm thấy khu vực." });

        return Ok(area);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateAreaRequest request)
    {
        try
        {
            var result = await _areaService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAreaRequest request)
    {
        try
        {
            var result = await _areaService.UpdateAsync(id, request);
            if (result == null) return NotFound(new { message = "Không tìm thấy khu vực." });

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
        var success = await _areaService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy khu vực." });

        return NoContent();
    }

    [HttpPost("{areaId}/layout/batch-update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> BatchUpdateLayout(int areaId, [FromBody] List<UpdateTableLayoutRequest> layout)
    {
        try
        {
            var success = await _tableService.UpdateBatchLayoutAsync(areaId, layout);
            if (!success) return BadRequest(new { message = "Lỗi khi cập nhật sơ đồ bàn." });

            return Ok(new { message = "Cập nhật sơ đồ thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
