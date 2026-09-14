namespace Restaurant.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest
{
    public bool IsActive { get; set; } = true;
}

public class ModifierOptionDto
{
    public int Id { get; set; }
    public int ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateModifierOptionRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateModifierOptionRequest : CreateModifierOptionRequest
{
    public bool IsActive { get; set; } = true;
}

public class ModifierGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int SortOrder { get; set; }
    public List<ModifierOptionDto> ModifierOptions { get; set; } = new();
}

public class CreateModifierGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public int SortOrder { get; set; }
    public List<CreateModifierOptionRequest> Options { get; set; } = new();
}

public class UpdateModifierGroupRequest : CreateModifierGroupRequest
{
}

public class MenuItemDto
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
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<ModifierGroupDto> ModifierGroups { get; set; } = new();
}

public class CreateMenuItemRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsCombo { get; set; }
    public int SortOrder { get; set; }
    public List<int> ModifierGroupIds { get; set; } = new();
}

public class UpdateMenuItemRequest : CreateMenuItemRequest
{
    public bool IsSoldOut { get; set; }
    public bool IsActive { get; set; } = true;
}
