using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/pos/policy")]
[Route("api/v1/pos/policy")]
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

    [HttpPut]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> UpdatePolicy([FromBody] PosPolicyOptions options)
    {
        await _policyService.UpdatePolicyAsync(options);
        return Ok(new { success = true, data = options, message = "Cập nhật chính sách POS thành công" });
    }
}
