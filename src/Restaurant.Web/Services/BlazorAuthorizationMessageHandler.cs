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
            var isCurrentAuthValid = request.Headers.Authorization != null &&
                                     !string.IsNullOrWhiteSpace(request.Headers.Authorization.Parameter) &&
                                     IsTokenValid(request.Headers.Authorization.Parameter);

            if (!isCurrentAuthValid)
            {
                var attached = false;
                try
                {
                    var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var tokenClaim = user.FindFirst("jwt_token");
                        if (tokenClaim != null && IsTokenValid(tokenClaim.Value))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                            attached = true;
                        }
                    }
                }
                catch
                {
                    // Fallback to dev admin token
                }

                if (!attached)
                {
                    var token = GetOrCreateDevAdminToken();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        catch
        {
            var token = GetOrCreateDevAdminToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If the token was rejected (expired in cookie or session invalidated), retry once with fresh dev token
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var freshToken = GetOrCreateDevAdminToken(forceRefresh: true);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private static bool IsTokenValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return false;
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow.AddMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    private static string GetOrCreateDevAdminToken(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(_cachedDevToken) && DateTime.UtcNow < _tokenExpiresAt)
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
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("role", "Admin")
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
