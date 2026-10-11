using Microsoft.Extensions.Options;
using Restaurant.Application.Common;
using Restaurant.Application.Interfaces;

namespace Restaurant.Infrastructure.Services;

public class PosPolicyService : IPosPolicyService
{
    private static PosPolicyOptions _currentOptions = new PosPolicyOptions();

    public PosPolicyService(IOptions<PosPolicyOptions>? options = null)
    {
        if (options?.Value != null)
        {
            _currentOptions = options.Value;
        }
    }

    public Task<PosPolicyOptions> GetPolicyAsync()
    {
        return Task.FromResult(_currentOptions);
    }

    public Task UpdatePolicyAsync(PosPolicyOptions options)
    {
        _currentOptions = options ?? new PosPolicyOptions();
        return Task.CompletedTask;
    }
}
