using Restaurant.Domain.Common;

namespace Restaurant.Domain.Entities;

public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : FullAuditableEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? PinCodeHash { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Area : FullAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Table> Tables { get; set; } = new List<Table>();
}

public class Table : FullAuditableEntity, IHasRowVersion
{
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public int Status { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public int Shape { get; set; } = 0; // 0: Round, 1: Square, 2: Rectangle
    public int Rotation { get; set; } = 0; // 0, 90, 180, 270
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Category : FullAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

public class MenuItem : FullAuditableEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsSoldOut { get; set; }
    public bool IsCombo { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public Recipe? Recipe { get; set; }
    public ICollection<MenuItemModifierGroup> MenuItemModifierGroups { get; set; } = new List<MenuItemModifierGroup>();
}

public class ModifierGroup : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public int SortOrder { get; set; }
    public ICollection<ModifierOption> ModifierOptions { get; set; } = new List<ModifierOption>();
    public ICollection<MenuItemModifierGroup> MenuItemModifierGroups { get; set; } = new List<MenuItemModifierGroup>();
}

public class ModifierOption : AuditableEntity
{
    public int ModifierGroupId { get; set; }
    public ModifierGroup ModifierGroup { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ModifierRecipe? ModifierRecipe { get; set; }
}

public class MenuItemModifierGroup
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int ModifierGroupId { get; set; }
    public ModifierGroup ModifierGroup { get; set; } = null!;
    public int SortOrder { get; set; }
}
