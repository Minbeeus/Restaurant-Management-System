using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IInventoryService
{
    Task<List<MaterialDto>> GetAllMaterialsAsync();
    Task<MaterialDto> CreateMaterialAsync(CreateMaterialRequest request);
    Task<bool> CreatePurchaseOrderAsync(int userId, CreatePurchaseOrderRequest request);
    Task<bool> AuditStockAsync(int userId, StockAuditRequest request);
    Task<bool> SetMenuItemRecipeAsync(SetMenuItemRecipeRequest request);
    Task<bool> SetModifierOptionRecipeAsync(SetModifierOptionRecipeRequest request);
    Task DeductInventoryForOrderItemAsync(int orderItemId);
    // Supplier Management
    Task<List<SupplierDto>> GetAllSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request);
    Task<bool> DeleteSupplierAsync(int id);

    // Unit Conversion Management
    Task<List<UnitConversionDto>> GetAllUnitConversionsAsync();
    Task<UnitConversionDto> CreateUnitConversionAsync(CreateUnitConversionRequest request);
}
