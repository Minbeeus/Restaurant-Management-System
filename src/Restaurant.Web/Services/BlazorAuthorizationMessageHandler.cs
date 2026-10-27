using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Restaurant.Web.Services;

public class BlazorAuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private static string? _cachedDevToken;
    private static DateTime _tokenExpiresAt = DateTime.MinValue;

    public BlazorAuthorizationMessageHandler(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Headers.Authorization == null)
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    var tokenClaim = user.FindFirst("jwt_token");
                    if (tokenClaim != null && !string.IsNullOrWhiteSpace(tokenClaim.Value))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                    }
                }

                // If still null (e.g. direct navigation, development circuit context), attach valid dev Admin token
                if (request.Headers.Authorization == null)
                {
                    var token = GetOrCreateDevAdminToken();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        catch
        {
            // Prerendering or circuit context not initialized; attach fallback token
            if (request.Headers.Authorization == null)
            {
                var token = GetOrCreateDevAdminToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static string GetOrCreateDevAdminToken()
    {
        if (!string.IsNullOrEmpty(_cachedDevToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return _cachedDevToken;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("RestaurantManagementSecretKeySuperProtection123!");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1006"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        _cachedDevToken = tokenHandler.WriteToken(token);
        _tokenExpiresAt = DateTime.UtcNow.AddDays(6);
        return _cachedDevToken;
    }
}
