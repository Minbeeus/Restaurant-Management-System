namespace Restaurant.Application.DTOs;

public class MaterialDto
{
    public int Id { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string DisplayName => (System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("en", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(NameEn)) ? NameEn : Name;
    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool IsActive { get; set; }
    public bool IsLowStock => CurrentStock <= MinStockLevel;
}

public class CreateMaterialRequest
{
    public int UnitOfMeasureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal CostPerUnit { get; set; }
}

public class PurchaseOrderItemRequest
{
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public int SupplierId { get; set; }
    public string? Note { get; set; }
    public List<PurchaseOrderItemRequest> Items { get; set; } = new();
}

public class StockAuditItemRequest
{
    public int MaterialId { get; set; }
    public decimal ActualStock { get; set; }
    public string? Reason { get; set; }
}

public class StockAuditRequest
{
    public string? Note { get; set; }
    public List<StockAuditItemRequest> Items { get; set; } = new();
}

public class RecipeItemRequest
{
    public int MaterialId { get; set; }
    public int UnitOfMeasureId { get; set; }
    public decimal Quantity { get; set; }
}

public class SetMenuItemRecipeRequest
{
    public int MenuItemId { get; set; }
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public class SetModifierOptionRecipeRequest
{
    public int ModifierOptionId { get; set; }
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}

public class UnitConversionDto
{
    public int Id { get; set; }
    public int FromUnitId { get; set; }
    public string FromUnitName { get; set; } = string.Empty;
    public int ToUnitId { get; set; }
    public string ToUnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
}

public class CreateUnitConversionRequest
{
    public int FromUnitId { get; set; }
    public int ToUnitId { get; set; }
    public decimal ConversionFactor { get; set; }
}

public class RecipeItemDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string? MaterialNameEn { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal LineCost => Quantity * CostPerUnit;
}

public class MenuItemRecipeDto
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public List<RecipeItemDto> Items { get; set; } = new();
    public decimal TotalCost => Items.Sum(i => i.LineCost);
}

public class ModifierOptionRecipeDto
{
    public int ModifierOptionId { get; set; }
    public string ModifierOptionName { get; set; } = string.Empty;
    public List<RecipeItemDto> Items { get; set; } = new();
    public decimal TotalCost => Items.Sum(i => i.LineCost);
}

public class GoodsReceiptItemDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class GoodsReceiptDto
{
    public int Id { get; set; }
    public string ReceiptCode => $"GR-{CreatedAt:yyyyMMdd}-{Id:D2}";
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int ReceivedByUserId { get; set; }
    public string ReceivedByUserName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
}

public class StocktakeItemDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal Difference { get; set; }
    public decimal DifferenceValue { get; set; }
    public string? Note { get; set; }
}

public class StocktakeDto
{
    public int Id { get; set; }
    public string AuditCode => $"AUDIT-{StartedAt:yyyyMMdd}-{Id:D2}";
    public int ConductedByUserId { get; set; }
    public string ConductedByUserName { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        1 => "Đã chốt & cân đối",
        2 => "Chờ Quản lý cân đối",
        _ => "Đang kiểm"
    };
    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<StocktakeItemDto> Items { get; set; } = new();
    public decimal TotalDifferenceValue => Items.Sum(i => i.DifferenceValue);
}

public class CreateBlindStocktakeRequest
{
    public string? Note { get; set; }
    public List<BlindStocktakeItemRequest> Items { get; set; } = new();
}

public class BlindStocktakeItemRequest
{
    public int MaterialId { get; set; }
    public decimal ActualQuantity { get; set; }
    public string? Note { get; set; }
}

public class ReconcileStocktakeRequest
{
    public int StocktakeId { get; set; }
    public string? Note { get; set; }
    public List<ReconcileStocktakeItemRequest> Items { get; set; } = new();
}

public class ReconcileStocktakeItemRequest
{
    public int MaterialId { get; set; }
    public decimal ActualQuantity { get; set; }
    public string? Reason { get; set; }
}

public class InventoryLossSummaryDto
{
    public decimal TotalVarianceAmount { get; set; }
    public decimal TotalWasteAmount { get; set; }
    public int DiscrepancyCount { get; set; }
    public List<StocktakeItemDto> DiscrepancyItems { get; set; } = new();
}

