using Restaurant.Domain.Common;

namespace Restaurant.Domain.Entities;

public class CustomerTier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

public class Customer : FullAuditableEntity
{
    public int TierId { get; set; }
    public CustomerTier Tier { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }

    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class LoyaltyTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int Type { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnitOfMeasure
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<Material> Materials { get; set; } = new List<Material>();
}

public class Material : FullAuditableEntity, IHasRowVersion
{
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    public ICollection<ModifierRecipeItem> ModifierRecipeItems { get; set; } = new List<ModifierRecipeItem>();
    public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
    public ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}

public class UnitConversion
{
    public int Id { get; set; }
    public int FromUnitId { get; set; }
    public UnitOfMeasure FromUnit { get; set; } = null!;
    public int ToUnitId { get; set; }
    public UnitOfMeasure ToUnit { get; set; } = null!;
    public decimal ConversionFactor { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Recipe : AuditableEntity
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

public class RecipeItem : AuditableEntity
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class ModifierRecipe : AuditableEntity
{
    public int ModifierOptionId { get; set; }
    public ModifierOption ModifierOption { get; set; } = null!;
    public ICollection<ModifierRecipeItem> ModifierRecipeItems { get; set; } = new List<ModifierRecipeItem>();
}

public class ModifierRecipeItem : AuditableEntity
{
    public int ModifierRecipeId { get; set; }
    public ModifierRecipe ModifierRecipe { get; set; } = null!;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class Supplier : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}

public class GoodsReceipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
}

public class GoodsReceiptItem
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class Stocktake : AuditableEntity
{
    public int ConductedByUserId { get; set; }
    public User ConductedByUser { get; set; } = null!;
    public int Status { get; set; }
    public string? Note { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();
}

public class StocktakeItem
{
    public int Id { get; set; }
    public int StocktakeId { get; set; }
    public Stocktake Stocktake { get; set; } = null!;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public decimal SystemQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal Difference { get; set; }
    public decimal DifferenceValue { get; set; }
    public string? Note { get; set; }
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Note { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
