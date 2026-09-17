using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Application.DTOs.Dashboard;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] DateTime? date)
    {
        var result = await _dashboardService.GetSummaryAsync(date);
        return Ok(result);
    }

    [HttpGet("sales-chart")]
    public async Task<ActionResult<List<SalesChartPointDto>>> GetSalesChart([FromQuery] DateTime? date)
    {
        var result = await _dashboardService.GetSalesChartAsync(date);
        return Ok(result);
    }

    [HttpGet("top-movers")]
    public async Task<ActionResult<List<TopMoverDto>>> GetTopMovers([FromQuery] DateTime? date, [FromQuery] int limit = 5)
    {
        var result = await _dashboardService.GetTopMoversAsync(date, limit);
        return Ok(result);
    }

    [HttpGet("activity-feed")]
    public async Task<ActionResult<List<ActivityFeedItemDto>>> GetActivityFeed([FromQuery] int limit = 10)
    {
        var result = await _dashboardService.GetActivityFeedAsync(limit);
        return Ok(result);
    }
}
