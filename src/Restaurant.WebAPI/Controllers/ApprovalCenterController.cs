using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/approvals")]
[Authorize(Roles = "Admin,Manager,Cashier")]
public class ApprovalCenterController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ICurrentUserService _currentUserService;

    public ApprovalCenterController(IApprovalService approvalService, ICurrentUserService currentUserService)
    {
        _approvalService = approvalService;
        _currentUserService = currentUserService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        try
        {
            var data = await _approvalService.GetPendingRequestsAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetRequestsHistory([FromQuery] int? status, [FromQuery] int? type)
    {
        try
        {
            var data = await _approvalService.GetRequestsHistoryAsync(status, type);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateApprovalRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
            }

            var userId = _currentUserService.UserId.Value;
            var data = await _approvalService.CreateRequestAsync(userId, request);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ProcessApprovalRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
            }

            request.ApprovalRequestId = id;
            var userId = _currentUserService.UserId.Value;
            var data = await _approvalService.ApproveRequestAsync(userId, request);
            return Ok(new { success = true, data, message = "Phê duyệt yêu cầu thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] ProcessApprovalRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
            }

            request.ApprovalRequestId = id;
            var userId = _currentUserService.UserId.Value;
            var data = await _approvalService.RejectRequestAsync(userId, request);
            return Ok(new { success = true, data, message = "Đã từ chối yêu cầu." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("manager-pin-approve")]
    public async Task<IActionResult> ApproveByManagerPin([FromBody] ManagerPinApprovalRequestDto request)
    {
        try
        {
            var data = await _approvalService.ApproveByManagerPinAsync(request);
            return Ok(new { success = true, data, message = "Phê duyệt nhanh bằng PIN Quản lý thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
