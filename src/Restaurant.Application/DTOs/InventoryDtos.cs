namespace Restaurant.Application.DTOs;

public class MaterialDto
{
    public int Id { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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

