namespace Restaurant.Application.DTOs;

public class KitchenTicketDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int? TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<KitchenItemDto> Items { get; set; } = new();
}

public class KitchenItemDto
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public int KitchenStatus { get; set; } // 0: Pending, 1: Processing, 2: Done
    public string KitchenStatusName { get; set; } = string.Empty;
    public DateTime? SentToKitchenAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WaitingMinutes { get; set; }
    public bool IsSlaWarning { get; set; }
    public List<string> Modifiers { get; set; } = new();
}

public class UpdateKitchenStatusRequest
{
    public int KitchenStatus { get; set; } // 0: Pending, 1: Processing, 2: Done
}
