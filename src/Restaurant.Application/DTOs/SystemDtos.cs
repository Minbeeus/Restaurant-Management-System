namespace Restaurant.Application.DTOs;

public class RegisterDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CreateRegisterRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class KitchenStationDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int MenuItemCount { get; set; }
}

public class CreateKitchenStationRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SystemSettingsDto
{
    public string RestaurantName { get; set; } = "Pizza Conner";
    public string Address { get; set; } = "123 Đường Pasteur, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";
    public string Phone { get; set; } = "028 3822 9999";
    public string TaxNumber { get; set; } = "0312345678";
    public decimal DefaultVatPercent { get; set; } = 8.0m;
    public decimal ServiceChargePercent { get; set; } = 5.0m;
    public string CurrencySymbol { get; set; } = "đ";
    public string ReceiptHeader { get; set; } = "HÂN HẠNH PHỤC VỤ QUÝ KHÁCH - BUON APPETITO!";
    public string ReceiptFooter { get; set; } = "Cảm ơn Quý khách và hẹn gặp lại!";
    public bool EnableAutoSendKitchen { get; set; } = true;
    public bool EnableTableTimer { get; set; } = true;
}
