using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/pos/policy")]
[Authorize]
public class PosPolicyController : ControllerBase
{
    private readonly IPosPolicyService _policyService;

    public PosPolicyController(IPosPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    public async Task<ActionResult<PosPolicyOptions>> GetPolicy()
    {
        var policy = await _policyService.GetPolicyAsync();
        return Ok(policy);
    }
}
