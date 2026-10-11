using Restaurant.Application.Common;

namespace Restaurant.Application.Interfaces;

public interface IPosPolicyService
{
    Task<PosPolicyOptions> GetPolicyAsync();
    Task UpdatePolicyAsync(PosPolicyOptions options);
}
