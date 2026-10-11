using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace Restaurant.Web.Services;

public class BlazorAuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

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
            }
        }
        catch
        {
            // Prerendering or circuit context not initialized; proceed without attaching header
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
