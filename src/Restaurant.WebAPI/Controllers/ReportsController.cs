using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/reports")]
[AllowAnonymous]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IInventoryService _inventoryService;

    public ReportsController(IReportService reportService, IInventoryService inventoryService)
    {
        _reportService = reportService;
        _inventoryService = inventoryService;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _reportService.GetRevenueReportAsync(fromDate, toDate);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("top-movers")]
    public async Task<IActionResult> GetTopMoversReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int limit = 50)
    {
        var result = await _reportService.GetTopSellingReportAsync(fromDate, toDate, limit);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("loss")]
    public async Task<IActionResult> GetLossReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _inventoryService.GetInventoryLossSummaryAsync(fromDate, toDate);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }
}
