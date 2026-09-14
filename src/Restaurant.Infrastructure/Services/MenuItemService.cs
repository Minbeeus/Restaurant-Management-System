using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class MenuItemService : IMenuItemService
{
    private readonly ApplicationDbContext _context;

    public MenuItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItemDto>> GetAllAsync()
    {
        return await _context.MenuItems
            .Include(mi => mi.Category)
            .Include(mi => mi.MenuItemModifierGroups)
                .ThenInclude(mmg => mmg.ModifierGroup)
                    .ThenInclude(mg => mg.ModifierOptions)
            .OrderBy(mi => mi.SortOrder)
            .Select(mi => MapToDto(mi))
            .ToListAsync();
    }

    public async Task<List<MenuItemDto>> GetActiveMenuItemsAsync()
    {
        // Global Query Filter automatically filters out IsDeleted == true.
        // We explicitly check IsActive == true here for menu ordering.
        return await _context.MenuItems
            .Include(mi => mi.Category)
            .Include(mi => mi.MenuItemModifierGroups)
                .ThenInclude(mmg => mmg.ModifierGroup)
                    .ThenInclude(mg => mg.ModifierOptions)
            .Where(mi => mi.IsActive)
            .OrderBy(mi => mi.SortOrder)
            .Select(mi => MapToDto(mi))
            .ToListAsync();
    }

    public async Task<MenuItemDto?> GetByIdAsync(int id)
    {
        var mi = await _context.MenuItems
            .Include(mi => mi.Category)
            .Include(mi => mi.MenuItemModifierGroups)
                .ThenInclude(mmg => mmg.ModifierGroup)
                    .ThenInclude(mg => mg.ModifierOptions)
            .FirstOrDefaultAsync(mi => mi.Id == id);

        return mi == null ? null : MapToDto(mi);
    }

    public async Task<MenuItemDto> CreateAsync(CreateMenuItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên món ăn không được để trống.");
        if (request.Price < 0)
            throw new ArgumentException("Giá món ăn không thể âm.");

        var menuItem = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsCombo = request.IsCombo,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        if (request.ModifierGroupIds.Any())
        {
            menuItem.MenuItemModifierGroups = request.ModifierGroupIds.Select((groupId, index) => new MenuItemModifierGroup
            {
                ModifierGroupId = groupId,
                SortOrder = index
            }).ToList();
        }

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(menuItem.Id))!;
    }

    public async Task<MenuItemDto?> UpdateAsync(int id, UpdateMenuItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên món ăn không được để trống.");
        if (request.Price < 0)
            throw new ArgumentException("Giá món ăn không thể âm.");

        var menuItem = await _context.MenuItems
            .Include(mi => mi.MenuItemModifierGroups)
            .FirstOrDefaultAsync(mi => mi.Id == id);

        if (menuItem == null) return null;

        menuItem.CategoryId = request.CategoryId;
        menuItem.Name = request.Name.Trim();
        menuItem.Description = request.Description;
        menuItem.Price = request.Price;
        menuItem.ImageUrl = request.ImageUrl;
        menuItem.IsCombo = request.IsCombo;
        menuItem.IsSoldOut = request.IsSoldOut;
        menuItem.IsActive = request.IsActive;
        menuItem.SortOrder = request.SortOrder;

        // Update Modifier Groups association
        _context.MenuItemModifierGroups.RemoveRange(menuItem.MenuItemModifierGroups);
        if (request.ModifierGroupIds.Any())
        {
            menuItem.MenuItemModifierGroups = request.ModifierGroupIds.Select((groupId, index) => new MenuItemModifierGroup
            {
                MenuItemId = id,
                ModifierGroupId = groupId,
                SortOrder = index
            }).ToList();
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null) return false;

        _context.MenuItems.Remove(menuItem); // AuditableEntityInterceptor sets IsDeleted = true
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleSoldOutAsync(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null) return false;

        menuItem.IsSoldOut = !menuItem.IsSoldOut;
        await _context.SaveChangesAsync();
        return true;
    }

    private static MenuItemDto MapToDto(MenuItem mi)
    {
        return new MenuItemDto
        {
            Id = mi.Id,
            CategoryId = mi.CategoryId,
            CategoryName = mi.Category?.Name ?? string.Empty,
            Name = mi.Name,
            Description = mi.Description,
            Price = mi.Price,
            ImageUrl = mi.ImageUrl,
            IsSoldOut = mi.IsSoldOut,
            IsCombo = mi.IsCombo,
            IsActive = mi.IsActive,
            SortOrder = mi.SortOrder,
            ModifierGroups = mi.MenuItemModifierGroups.Select(mmg => new ModifierGroupDto
            {
                Id = mmg.ModifierGroup.Id,
                Name = mmg.ModifierGroup.Name,
                IsRequired = mmg.ModifierGroup.IsRequired,
                MinSelections = mmg.ModifierGroup.MinSelections,
                MaxSelections = mmg.ModifierGroup.MaxSelections,
                SortOrder = mmg.SortOrder,
                ModifierOptions = mmg.ModifierGroup.ModifierOptions.Select(o => new ModifierOptionDto
                {
                    Id = o.Id,
                    ModifierGroupId = o.ModifierGroupId,
                    Name = o.Name,
                    ExtraPrice = o.ExtraPrice,
                    IsDefault = o.IsDefault,
                    SortOrder = o.SortOrder,
                    IsActive = o.IsActive
                }).ToList()
            }).ToList()
        };
    }
}
