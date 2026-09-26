using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/bills")]
[AllowAnonymous]
public class BillViewerController : ControllerBase
{
    private readonly IBillViewerService _billViewerService;

    public BillViewerController(IBillViewerService billViewerService)
    {
        _billViewerService = billViewerService;
    }

    [HttpPost("query")]
    public async Task<IActionResult> QueryBills([FromBody] BillQueryRequest request)
    {
        try
        {
            var data = await _billViewerService.QueryBillsAsync(request);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBillDetails(int id)
    {
        try
        {
            var data = await _billViewerService.GetBillDetailsAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn." });
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}/k80")]
    public async Task<IActionResult> GetBillK80Receipt(int id)
    {
        try
        {
            var data = await _billViewerService.GetBillK80ReceiptAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Không tìm thấy mẫu in cho hóa đơn." });
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
