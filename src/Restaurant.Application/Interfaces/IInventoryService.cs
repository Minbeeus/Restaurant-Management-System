using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IInventoryService
{
    Task<List<MaterialDto>> GetAllMaterialsAsync();
    Task<MaterialDto> CreateMaterialAsync(CreateMaterialRequest request);
    Task<MaterialDto?> UpdateMaterialAsync(int id, CreateMaterialRequest request);
    Task<bool> DeleteMaterialAsync(int id);
    Task<bool> CreatePurchaseOrderAsync(int userId, CreatePurchaseOrderRequest request);
    Task<bool> AuditStockAsync(int userId, StockAuditRequest request);
    Task<bool> SetMenuItemRecipeAsync(SetMenuItemRecipeRequest request);
    Task<bool> SetModifierOptionRecipeAsync(SetModifierOptionRecipeRequest request);
    Task<MenuItemRecipeDto?> GetMenuItemRecipeAsync(int menuItemId);
    Task<ModifierOptionRecipeDto?> GetModifierOptionRecipeAsync(int modifierOptionId);
    Task DeductInventoryForOrderItemAsync(int orderItemId);

    // Goods Receipts & Stocktakes
    Task<List<GoodsReceiptDto>> GetAllGoodsReceiptsAsync();
    Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(int id);
    Task<List<StocktakeDto>> GetAllStocktakesAsync();
    Task<StocktakeDto?> GetStocktakeByIdAsync(int id);
    Task<StocktakeDto> CreateBlindStocktakeAsync(int userId, CreateBlindStocktakeRequest request);
    Task<bool> ReconcileStocktakeAsync(int userId, ReconcileStocktakeRequest request);
    Task<InventoryLossSummaryDto> GetInventoryLossSummaryAsync(DateTime? fromDate, DateTime? toDate);
    Task<List<MaterialDto>> GetMaterialsBySupplierIdAsync(int supplierId);

    // Supplier Management
    Task<List<SupplierDto>> GetAllSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request);
    Task<SupplierDto?> UpdateSupplierAsync(int id, CreateSupplierRequest request);
    Task<bool> DeleteSupplierAsync(int id);

    // Unit Conversion Management
    Task<List<UnitConversionDto>> GetAllUnitConversionsAsync();
    Task<UnitConversionDto> CreateUnitConversionAsync(CreateUnitConversionRequest request);
}
