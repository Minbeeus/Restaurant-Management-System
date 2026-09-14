namespace Restaurant.Application.DTOs;

public class PublicCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public List<PublicMenuItemDto> MenuItems { get; set; } = new();
}

public class PublicMenuItemDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsSoldOut { get; set; }
    public bool IsCombo { get; set; }
    public int SortOrder { get; set; }
}

public class PublicMenuResponse
{
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public int TableStatus { get; set; }
    public List<PublicCategoryDto> Categories { get; set; } = new();
}

public class MenuItemDetailDto : PublicMenuItemDto
{
    public List<ModifierGroupDto> ModifierGroups { get; set; } = new();
}

public class CartItemValidateRequest
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public List<int> SelectedModifiers { get; set; } = new();
    public List<int> SelectedComboItems { get; set; } = new();
    public string? Note { get; set; }
}

public class CartValidateRequest
{
    public int TableId { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public List<CartItemValidateRequest> Items { get; set; } = new();
}

public class CartValidatedItemResult
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal ModifiersTotalPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public List<string> SelectedModifierNames { get; set; } = new();
    public string? Note { get; set; }
}

public class CartValidateResponse
{
    public bool IsValid { get; set; }
    public decimal SubTotal { get; set; }
    public List<CartValidatedItemResult> Items { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

public class SubmitQrOrderRequest
{
    public int TableId { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }
    public List<CartItemValidateRequest> Items { get; set; } = new();
}

public class SubmitQrOrderResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QrOrderItemStatusDto
{
    public int OrderItemId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Note { get; set; }
    public int KitchenStatus { get; set; } // 0: Pending, 1: Processing, 2: Done
    public string KitchenStatusName { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = new();
}

public class QrOrderStatusResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QrOrderItemStatusDto> Items { get; set; } = new();
}

