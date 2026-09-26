using Microsoft.Extensions.Options;
using Restaurant.Application.Common;
using Restaurant.Application.Interfaces;

namespace Restaurant.Infrastructure.Services;

public class PosPolicyService : IPosPolicyService
{
    private readonly PosPolicyOptions _options;

    public PosPolicyService(IOptions<PosPolicyOptions>? options = null)
    {
        _options = options?.Value ?? new PosPolicyOptions();
    }

    public Task<PosPolicyOptions> GetPolicyAsync()
    {
        return Task.FromResult(_options);
    }
}
