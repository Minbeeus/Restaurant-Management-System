using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public class TableApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public TableApiClient(IHttpClientFactory httpClientFactory, AuthenticationStateProvider authenticationStateProvider)
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
        catch (InvalidOperationException) { }
    }

    // --- AREAS ---
    public async Task<List<AreaDto>> GetAreasAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<AreaDto>>("/api/v1/areas") ?? new List<AreaDto>();
    }

    public async Task<AreaDto?> GetAreaByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<AreaDto>($"/api/v1/areas/{id}");
    }

    public async Task<AreaDto?> CreateAreaAsync(CreateAreaRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/areas", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AreaDto>();
    }

    public async Task<AreaDto?> UpdateAreaAsync(int id, UpdateAreaRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/areas/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AreaDto>();
    }

    public async Task<bool> DeleteAreaAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/v1/areas/{id}");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Xóa thất bại (Mã lỗi: {response.StatusCode})");
        }
        return true;
    }

    // --- TABLES ---
    public async Task<List<TableDto>> GetTablesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<TableDto>>("/api/v1/tables") ?? new List<TableDto>();
    }

    public async Task<List<TableDto>> GetTablesByAreaIdAsync(int areaId)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<TableDto>>($"/api/v1/tables/area/{areaId}") ?? new List<TableDto>();
    }

    public async Task<TableDto?> GetTableByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<TableDto>($"/api/v1/tables/{id}");
    }

    public async Task<TableDto?> CreateTableAsync(CreateTableRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/tables", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableDto>();
    }

    public async Task<TableDto?> UpdateTableAsync(int id, UpdateTableRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/tables/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableDto>();
    }

    public async Task<bool> DeleteTableAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/v1/tables/{id}");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Xóa thất bại (Mã lỗi: {response.StatusCode})");
        }
        return true;
    }

    public async Task<bool> UpdateBatchLayoutAsync(int areaId, List<UpdateTableLayoutRequest> layout)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/areas/{areaId}/layout/batch-update", layout);
        return response.IsSuccessStatusCode;
    }
}
