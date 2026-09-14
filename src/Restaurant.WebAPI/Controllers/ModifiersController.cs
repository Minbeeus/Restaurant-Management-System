using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/modifiers")]
public class ModifiersController : ControllerBase
{
    private readonly IModifierService _modifierService;

    public ModifiersController(IModifierService modifierService)
    {
        _modifierService = modifierService;
    }

    [HttpGet("groups")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await _modifierService.GetAllGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("groups/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGroupById(int id)
    {
        var group = await _modifierService.GetGroupByIdAsync(id);
        if (group == null) return NotFound(new { message = "Không tìm thấy nhóm tùy chọn." });

        return Ok(group);
    }

    [HttpPost("groups")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateModifierGroupRequest request)
    {
        try
        {
            var result = await _modifierService.CreateGroupAsync(request);
            return CreatedAtAction(nameof(GetGroupById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateModifierGroupRequest request)
    {
        try
        {
            var result = await _modifierService.UpdateGroupAsync(id, request);
            if (result == null) return NotFound(new { message = "Không tìm thấy nhóm tùy chọn." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var success = await _modifierService.DeleteGroupAsync(id);
        if (!success) return NotFound(new { message = "Không tìm thấy nhóm tùy chọn." });

        return NoContent();
    }

    [HttpPost("groups/{groupId}/options")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddOption(int groupId, [FromBody] CreateModifierOptionRequest request)
    {
        try
        {
            var result = await _modifierService.AddOptionAsync(groupId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("options/{optionId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateOption(int optionId, [FromBody] UpdateModifierOptionRequest request)
    {
        try
        {
            var result = await _modifierService.UpdateOptionAsync(optionId, request);
            if (result == null) return NotFound(new { message = "Không tìm thấy tùy chọn." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("options/{optionId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteOption(int optionId)
    {
        var success = await _modifierService.DeleteOptionAsync(optionId);
        if (!success) return NotFound(new { message = "Không tìm thấy tùy chọn." });

        return NoContent();
    }
}
