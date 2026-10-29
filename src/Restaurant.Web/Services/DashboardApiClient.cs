using System.Net.Http.Json;
using Restaurant.Application.DTOs.Dashboard;

namespace Restaurant.Web.Services;

public class DashboardApiClient
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
    }

    public async Task<DashboardSummaryDto?> GetSummaryAsync(DateTime? date = null)
    {
        var url = date.HasValue ? $"/api/dashboard/summary?date={date.Value:yyyy-MM-dd}" : "/api/dashboard/summary";
        return await _httpClient.GetFromJsonAsync<DashboardSummaryDto>(url);
    }

    public async Task<List<SalesChartPointDto>?> GetSalesChartAsync(DateTime? date = null)
    {
        var url = date.HasValue ? $"/api/dashboard/sales-chart?date={date.Value:yyyy-MM-dd}" : "/api/dashboard/sales-chart";
        return await _httpClient.GetFromJsonAsync<List<SalesChartPointDto>>(url);
    }

    public async Task<List<TopMoverDto>?> GetTopMoversAsync(DateTime? date = null, int limit = 5)
    {
        var url = date.HasValue ? $"/api/dashboard/top-movers?date={date.Value:yyyy-MM-dd}&limit={limit}" : $"/api/dashboard/top-movers?limit={limit}";
        return await _httpClient.GetFromJsonAsync<List<TopMoverDto>>(url);
    }

    public async Task<List<ActivityFeedItemDto>?> GetActivityFeedAsync(int limit = 10)
    {
        return await _httpClient.GetFromJsonAsync<List<ActivityFeedItemDto>>($"/api/dashboard/activity-feed?limit={limit}");
    }
}
