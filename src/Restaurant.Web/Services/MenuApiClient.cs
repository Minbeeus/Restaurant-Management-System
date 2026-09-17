using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public class MenuApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public MenuApiClient(IHttpClientFactory httpClientFactory, AuthenticationStateProvider authenticationStateProvider)
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

    // --- CATEGORIES ---
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories") ?? new List<CategoryDto>();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<CategoryDto>($"/api/v1/categories/{id}");
    }

    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/categories", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>();
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/categories/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>();
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/v1/categories/{id}");
        return response.IsSuccessStatusCode;
    }

    // --- MENU ITEMS ---
    public async Task<List<MenuItemDto>> GetMenuItemsAsync(int? categoryId = null)
    {
        await SetAuthorizationHeaderAsync();
        var url = categoryId.HasValue ? $"/api/v1/menu-items?categoryId={categoryId}" : "/api/v1/menu-items";
        return await _httpClient.GetFromJsonAsync<List<MenuItemDto>>(url) ?? new List<MenuItemDto>();
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<MenuItemDto>($"/api/v1/menu-items/{id}");
    }

    public async Task<MenuItemDto?> CreateMenuItemAsync(CreateMenuItemRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/menu-items", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuItemDto>();
    }

    public async Task<MenuItemDto?> UpdateMenuItemAsync(int id, UpdateMenuItemRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/menu-items/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuItemDto>();
    }

    public async Task<bool> DeleteMenuItemAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/v1/menu-items/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleSoldOutAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PatchAsync($"/api/v1/menu-items/{id}/toggle-sold-out", null);
        return response.IsSuccessStatusCode;
    }

    // --- MODIFIER GROUPS ---
    public async Task<List<ModifierGroupDto>> GetModifierGroupsAsync()
    {
        await SetAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<List<ModifierGroupDto>>("/api/v1/modifiers") ?? new List<ModifierGroupDto>();
    }

    public async Task<ModifierGroupDto?> CreateModifierGroupAsync(CreateModifierGroupRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/modifiers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModifierGroupDto>();
    }

    public async Task<ModifierGroupDto?> UpdateModifierGroupAsync(int id, UpdateModifierGroupRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/modifiers/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ModifierGroupDto>();
    }

    public async Task<bool> DeleteModifierGroupAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/v1/modifiers/{id}");
        return response.IsSuccessStatusCode;
    }
}
