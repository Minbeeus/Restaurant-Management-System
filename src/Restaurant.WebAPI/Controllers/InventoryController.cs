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

    [HttpPut("materials/{id}")]
    public async Task<IActionResult> UpdateMaterial(int id, [FromBody] CreateMaterialRequest request)
    {
        var result = await _inventoryService.UpdateMaterialAsync(id, request);
        if (result == null) return NotFound();

        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("materials/{id}")]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        var success = await _inventoryService.DeleteMaterialAsync(id);
        if (!success) return NotFound();

        return Ok(new
        {
            success = true,
            data = new { message = "Xóa nguyên vật liệu thành công." },
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
    [HttpPost("stock-audit")]
    public async Task<IActionResult> AuditStock([FromBody] StockAuditRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
        }

        var userId = _currentUserService.UserId.Value;

        var success = await _inventoryService.AuditStockAsync(userId, request);
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

    [HttpGet("recipes/menu-item/{menuItemId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMenuItemRecipe(int menuItemId)
    {
        var recipe = await _inventoryService.GetMenuItemRecipeAsync(menuItemId);
        if (recipe == null) return NotFound(new { success = false, message = "Không tìm thấy món ăn." });

        return Ok(new
        {
            success = true,
            data = recipe,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("recipes/modifier-option/{modifierOptionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetModifierOptionRecipe(int modifierOptionId)
    {
        var recipe = await _inventoryService.GetModifierOptionRecipeAsync(modifierOptionId);
        if (recipe == null) return NotFound(new { success = false, message = "Không tìm thấy tùy chọn." });

        return Ok(new
        {
            success = true,
            data = recipe,
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

    // Goods Receipts API
    [HttpGet("goods-receipts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllGoodsReceipts()
    {
        var result = await _inventoryService.GetAllGoodsReceiptsAsync();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("goods-receipts/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGoodsReceipt(int id)
    {
        var result = await _inventoryService.GetGoodsReceiptByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    // Stocktakes API
    [HttpGet("stocktakes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllStocktakes()
    {
        var result = await _inventoryService.GetAllStocktakesAsync();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("stocktakes/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStocktake(int id)
    {
        var result = await _inventoryService.GetStocktakeByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("stocktakes/blind")]
    public async Task<IActionResult> CreateBlindStocktake([FromBody] CreateBlindStocktakeRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
        }

        var userId = _currentUserService.UserId.Value;
        var result = await _inventoryService.CreateBlindStocktakeAsync(userId, request);
        return Ok(new
        {
            success = true,
            data = result,
            message = "Tạo phiếu đếm kho thực tế thành công.",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("stocktakes/{id}/reconcile")]
    public async Task<IActionResult> ReconcileStocktake(int id, [FromBody] ReconcileStocktakeRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa xác thực danh tính người dùng." });
        }

        var userId = _currentUserService.UserId.Value;
        request.StocktakeId = id;
        var success = await _inventoryService.ReconcileStocktakeAsync(userId, request);
        if (!success) return NotFound(new { success = false, message = "Không tìm thấy phiếu kiểm kê." });

        return Ok(new
        {
            success = true,
            message = "Cân đối và chốt kiểm kê kho thành công.",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("suppliers/{id}/materials")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSupplierMaterials(int id)
    {
        var result = await _inventoryService.GetMaterialsBySupplierIdAsync(id);
        return Ok(new
        {
            success = true,
            data = result,
            timestamp = DateTime.UtcNow
        });
    }

    // Inventory Loss Report API
    [HttpGet("reports/loss")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLossReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _inventoryService.GetInventoryLossSummaryAsync(fromDate, toDate);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null,
            timestamp = DateTime.UtcNow
        });
    }

    // Suppliers API Endpoints
    [HttpGet("suppliers")]
    [AllowAnonymous]
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

    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] CreateSupplierRequest request)
    {
        var result = await _inventoryService.UpdateSupplierAsync(id, request);
        if (result == null) return NotFound();

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
