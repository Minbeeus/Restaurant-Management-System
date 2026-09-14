using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize(Roles = "Admin,Manager")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public InventoryController(IInventoryService inventoryService, ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
    }

    [HttpGet("materials")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllMaterials()
    {
        var materials = await _inventoryService.GetAllMaterialsAsync();
        return Ok(new
        {
            success = true,
            data = materials,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("materials")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        var result = await _inventoryService.CreateMaterialAsync(request);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("purchase-orders")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var success = await _inventoryService.CreatePurchaseOrderAsync(_currentUserService.UserId.Value, request);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_PURCHASE_ORDER",
                    message = "Lập phiếu nhập kho không thành công."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Lập phiếu nhập kho thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("audit")]
    public async Task<IActionResult> AuditStock([FromBody] StockAuditRequest request)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var success = await _inventoryService.AuditStockAsync(_currentUserService.UserId.Value, request);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "INVALID_AUDIT_REQUEST",
                    message = "Lập phiếu kiểm kê không thành công."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Cập nhật kết quả kiểm kê kho thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("recipes/menu-item")]
    public async Task<IActionResult> SetMenuItemRecipe([FromBody] SetMenuItemRecipeRequest request)
    {
        var success = await _inventoryService.SetMenuItemRecipeAsync(request);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "MENUITEM_NOT_FOUND",
                    message = "Không tìm thấy món ăn để thiết lập định lượng BOM."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Thiết lập công thức BOM cho món ăn thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("recipes/modifier-option")]
    public async Task<IActionResult> SetModifierOptionRecipe([FromBody] SetModifierOptionRecipeRequest request)
    {
        var success = await _inventoryService.SetModifierOptionRecipeAsync(request);
        if (!success)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new
                {
                    code = "MODIFIER_OPTION_NOT_FOUND",
                    message = "Không tìm thấy tùy chọn để thiết lập định lượng BOM."
                },
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Thiết lập công thức BOM cho tùy chọn thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    // Suppliers API Endpoints
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetAllSuppliers()
    {
        var result = await _inventoryService.GetAllSuppliersAsync();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("suppliers/{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _inventoryService.GetSupplierByIdAsync(id);
        if (supplier == null) return NotFound();

        return Ok(new
        {
            success = true,
            data = supplier,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var result = await _inventoryService.CreateSupplierAsync(request);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var success = await _inventoryService.DeleteSupplierAsync(id);
        if (!success) return NotFound();

        return Ok(new
        {
            success = true,
            data = new { message = "Xóa nhà cung cấp thành công." },
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    // Unit Conversions API Endpoints
    [HttpGet("unit-conversions")]
    public async Task<IActionResult> GetAllUnitConversions()
    {
        var result = await _inventoryService.GetAllUnitConversionsAsync();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("unit-conversions")]
    public async Task<IActionResult> CreateUnitConversion([FromBody] CreateUnitConversionRequest request)
    {
        var result = await _inventoryService.CreateUnitConversionAsync(request);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }
}
