using Restaurant.Application.DTOs.Dashboard;

namespace Restaurant.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime? date = null);
    Task<List<SalesChartPointDto>> GetSalesChartAsync(DateTime? date = null);
    Task<List<TopMoverDto>> GetTopMoversAsync(DateTime? date = null, int limit = 5);
    Task<List<ActivityFeedItemDto>> GetActivityFeedAsync(int limit = 10);
}
