using System.Net.Http.Json;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public interface IAuthApiClient
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            return result?.Data;
        }

        return null;
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public object? Error { get; set; }
}
