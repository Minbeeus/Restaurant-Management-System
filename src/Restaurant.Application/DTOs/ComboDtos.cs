using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

public class ComboItemDto
{
    public int Id { get; set; }
    public int ComboId { get; set; }
    public int? ComboGroupId { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? MenuItemNameEn { get; set; }
    public string DisplayName => (System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("en", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(MenuItemNameEn)) ? MenuItemNameEn : MenuItemName;
    public decimal OriginalPrice { get; set; }
    public ComboComponentType ComponentType { get; set; }
    public int DefaultQuantity { get; set; } = 1;
    public decimal AdditionalPrice { get; set; } = 0;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
}

public class ComboGroupDto
{
    public int Id { get; set; }
    public int ComboId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public List<ComboItemDto> Items { get; set; } = new();
}

public class ComboDetailDto : MenuItemDto
{
    public List<ComboItemDto> FixedAndAddOnItems { get; set; } = new();
    public List<ComboGroupDto> Groups { get; set; } = new();
}

public class CreateComboItemRequest
{
    public int? Id { get; set; } // Null if new, or existing ComboItem.Id if update
    public int MenuItemId { get; set; }
    public ComboComponentType ComponentType { get; set; }
    public int DefaultQuantity { get; set; } = 1;
    public decimal AdditionalPrice { get; set; } = 0;
    public int SortOrder { get; set; }
}

public class CreateComboGroupRequest
{
    public int? Id { get; set; } // Null if new, or existing ComboGroup.Id if update
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public List<CreateComboItemRequest> Items { get; set; } = new();
}

public class CreateComboRequest : CreateMenuItemRequest
{
    public List<CreateComboItemRequest> FixedAndAddOnItems { get; set; } = new();
    public List<CreateComboGroupRequest> Groups { get; set; } = new();
}

public class UpdateComboRequest : CreateComboRequest
{
    public bool IsSoldOut { get; set; }
    public bool IsActive { get; set; } = true;
}
