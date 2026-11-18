using Microsoft.Extensions.Configuration;

namespace Restaurant.Web.Services;

public interface IBackendEndpointProvider
{
    string BaseUrl { get; }
    string GetHubUrl(string? clientType = null, int? stationId = null);
    string GetApiUrl(string relativePath);
}

public class BackendEndpointProvider : IBackendEndpointProvider
{
    private readonly string _baseUrl;

    public BackendEndpointProvider(IConfiguration configuration)
    {
        var url = configuration["ApiBaseUrl"] ?? "http://localhost:5011";
        _baseUrl = url.TrimEnd('/');
    }

    public string BaseUrl => _baseUrl;

    public string GetHubUrl(string? clientType = null, int? stationId = null)
    {
        var url = $"{_baseUrl}/hubs/restaurant";
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(clientType))
        {
            queryParams.Add($"clientType={clientType}");
        }
        if (stationId.HasValue)
        {
            queryParams.Add($"stationId={stationId.Value}");
        }
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }
        return url;
    }

    public string GetApiUrl(string relativePath)
    {
        var cleanPath = relativePath.TrimStart('/');
        return $"{_baseUrl}/{cleanPath}";
    }
}
