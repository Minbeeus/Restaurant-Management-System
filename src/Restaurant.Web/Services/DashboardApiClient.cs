using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Restaurant.Application.DTOs.Dashboard;

namespace Restaurant.Web.Services;

public class DashboardApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public DashboardApiClient(IHttpClientFactory httpClientFactory, AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _authenticationStateProvider = authenticationStateProvider;
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = user.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Ignore during prerendering
        }
    }

    public async Task<DashboardSummaryDto?> GetSummaryAsync(DateTime? date = null)
    {
        await SetAuthorizationHeaderAsync();
        var url = date.HasValue ? $"/api/dashboard/summary?date={date:O}" : "/api/dashboard/summary";
        return await _httpClient.GetFromJsonAsync<DashboardSummaryDto>(url);
    }

    public async Task<List<SalesChartPointDto>?> GetSalesChartAsync(DateTime? date = null)
    {
        await SetAuthorizationHeaderAsync();
        var url = date.HasValue ? $"/api/dashboard/sales-chart?date={date:O}" : "/api/dashboard/sales-chart";
        return await _httpClient.GetFromJsonAsync<List<SalesChartPointDto>>(url);
    }

    public async Task<List<TopMoverDto>?> GetTopMoversAsync(DateTime? date = null, int limit = 5)
    {
        await SetAuthorizationHeaderAsync();
        var url = date.HasValue ? $"/api/dashboard/top-movers?date={date:O}&limit={limit}" : $"/api/dashboard/top-movers?limit={limit}";
        return await _httpClient.GetFromJsonAsync<List<TopMoverDto>>(url);
    }

    public async Task<List<ActivityFeedItemDto>?> GetActivityFeedAsync(int limit = 10)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<ActivityFeedItemDto>>($"/api/dashboard/activity-feed?limit={limit}");
    }
}
