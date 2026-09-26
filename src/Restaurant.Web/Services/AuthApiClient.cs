using System.Net.Http.Json;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public interface IAuthApiClient
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> QuickLoginAsync(QuickLoginRequest request);
    Task<PinLoginResponse?> PinLoginAsync(PinLoginRequest request);
    Task<bool> VerifyManagerPinAsync(string pinCode);
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

    public async Task<LoginResponse?> QuickLoginAsync(QuickLoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/quick-login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            return result?.Data;
        }

        return null;
    }

    public async Task<PinLoginResponse?> PinLoginAsync(PinLoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/pin-login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PinLoginResponse>>();
            return result?.Data;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var errorResult = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            var errorObj = errorResult?.Error as System.Text.Json.JsonElement?;
            var msg = errorObj?.GetProperty("message").GetString() ?? "Mã PIN bị khóa do nhập sai 5 lần.";
            throw new InvalidOperationException(msg);
        }

        return null;
    }

    public async Task<bool> VerifyManagerPinAsync(string pinCode)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/verify-manager-pin", new VerifyPinRequest { PinCode = pinCode });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<VerifyPinResult>>();
            return result?.Data?.IsValid ?? false;
        }
        return false;
    }
}

public class VerifyPinResult
{
    public bool IsValid { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public object? Error { get; set; }
}
