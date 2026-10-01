using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ComboService : IComboService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ComboService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ComboDetailDto>> GetAllCombosAsync()
    {
        var combos = await _context.MenuItems
            .Where(m => m.IsCombo)
            .Include(m => m.Category)
            .Include(m => m.ComboGroups)
                .ThenInclude(g => g.ComboItems)
                    .ThenInclude(ci => ci.MenuItem)
            .Include(m => m.ComboItems)
                .ThenInclude(ci => ci.MenuItem)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        return combos.Select(MapToDetailDto).ToList();
    }

    public async Task<ComboDetailDto?> GetComboByIdAsync(int id)
    {
        var combo = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.ComboGroups)
                .ThenInclude(g => g.ComboItems)
                    .ThenInclude(ci => ci.MenuItem)
            .Include(m => m.ComboItems)
                .ThenInclude(ci => ci.MenuItem)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);

        if (combo == null) return null;

        return MapToDetailDto(combo);
    }

    public async Task<ComboDetailDto> CreateComboAsync(CreateComboRequest request)
    {
        var combo = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            NameEn = request.NameEn,
            Description = request.Description,
            DescriptionEn = request.DescriptionEn,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsCombo = true,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.MenuItems.Add(combo);

        // Add Groups
        foreach (var groupReq in request.Groups)
        {
            var group = new ComboGroup
            {
                Combo = combo,
                Name = groupReq.Name,
                MinSelections = groupReq.MinSelections,
                MaxSelections = groupReq.MaxSelections,
                IsRequired = groupReq.IsRequired,
                SortOrder = groupReq.SortOrder
            };

            foreach (var itemReq in groupReq.Items)
            {
                group.ComboItems.Add(new ComboItem
                {
                    Combo = combo,
                    ComboGroup = group,
                    MenuItemId = itemReq.MenuItemId,
                    ComponentType = (int)itemReq.ComponentType,
                    DefaultQuantity = itemReq.DefaultQuantity,
                    AdditionalPrice = itemReq.AdditionalPrice,
                    SortOrder = itemReq.SortOrder
                });
            }

            combo.ComboGroups.Add(group);
        }

        // Add Fixed and AddOn Items without group
        foreach (var itemReq in request.FixedAndAddOnItems)
        {
            combo.ComboItems.Add(new ComboItem
            {
                Combo = combo,
                MenuItemId = itemReq.MenuItemId,
                ComponentType = (int)itemReq.ComponentType,
                DefaultQuantity = itemReq.DefaultQuantity,
                AdditionalPrice = itemReq.AdditionalPrice,
                SortOrder = itemReq.SortOrder
            });
        }

        await _context.SaveChangesAsync();
        var userId = _currentUserService.UserId ?? 0;
        await _auditService.LogAsync(userId, "CreateCombo", "MenuItem", combo.Id.ToString(), $"Created combo {combo.Name}");

        return await GetComboByIdAsync(combo.Id) ?? throw new InvalidOperationException();
    }

    public async Task<ComboDetailDto?> UpdateComboAsync(int id, UpdateComboRequest request)
    {
        var combo = await _context.MenuItems
            .Include(m => m.ComboGroups)
                .ThenInclude(g => g.ComboItems)
            .Include(m => m.ComboItems)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);

        if (combo == null) return null;

        combo.CategoryId = request.CategoryId;
        combo.Name = request.Name;
        combo.NameEn = request.NameEn;
        combo.Description = request.Description;
        combo.DescriptionEn = request.DescriptionEn;
        combo.Price = request.Price;
        combo.ImageUrl = request.ImageUrl;
        combo.SortOrder = request.SortOrder;
        combo.IsSoldOut = request.IsSoldOut;
        combo.IsActive = request.IsActive;

        // Xóa toàn bộ Groups và Items cũ rồi tạo lại cho đơn giản
        // Vì OrderItem snapshot cấu trúc vào ParentItemId và ComboGroupId null/cũ cũng không ảnh hưởng OrderHistory
        // (Do EF Core DeleteBehavior là Restrict nên nếu nhóm đang được dùng trong OrderItem sẽ lỗi FK.
        // Tuy nhiên OrderItem của Combo không reference FK trực tiếp tới ComboGroup mà chỉ lưu ID dạng snapshot nếu cần, 
        // Trong OrderAndPaymentEntities.cs, OrderItem.ComboGroupId không có relationship (navigation property). Nên xóa thoải mái.)
        
        _context.ComboItems.RemoveRange(combo.ComboItems);
        _context.ComboGroups.RemoveRange(combo.ComboGroups);
        
        // Thêm lại
        foreach (var groupReq in request.Groups)
        {
            var group = new ComboGroup
            {
                Combo = combo,
                Name = groupReq.Name,
                MinSelections = groupReq.MinSelections,
                MaxSelections = groupReq.MaxSelections,
                IsRequired = groupReq.IsRequired,
                SortOrder = groupReq.SortOrder
            };

            foreach (var itemReq in groupReq.Items)
            {
                group.ComboItems.Add(new ComboItem
                {
                    Combo = combo,
                    ComboGroup = group,
                    MenuItemId = itemReq.MenuItemId,
                    ComponentType = (int)itemReq.ComponentType,
                    DefaultQuantity = itemReq.DefaultQuantity,
                    AdditionalPrice = itemReq.AdditionalPrice,
                    SortOrder = itemReq.SortOrder
                });
            }

            combo.ComboGroups.Add(group);
        }

        foreach (var itemReq in request.FixedAndAddOnItems)
        {
            combo.ComboItems.Add(new ComboItem
            {
                Combo = combo,
                MenuItemId = itemReq.MenuItemId,
                ComponentType = (int)itemReq.ComponentType,
                DefaultQuantity = itemReq.DefaultQuantity,
                AdditionalPrice = itemReq.AdditionalPrice,
                SortOrder = itemReq.SortOrder
            });
        }

        await _context.SaveChangesAsync();
        var userId = _currentUserService.UserId ?? 0;
        await _auditService.LogAsync(userId, "UpdateCombo", "MenuItem", combo.Id.ToString(), $"Updated combo {combo.Name}");

        return await GetComboByIdAsync(combo.Id);
    }

    public async Task<bool> DeleteComboAsync(int id)
    {
        var combo = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);
        if (combo == null) return false;

        // Soft delete
        combo.IsDeleted = true;
        await _context.SaveChangesAsync();
        var userId = _currentUserService.UserId ?? 0;
        await _auditService.LogAsync(userId, "DeleteCombo", "MenuItem", combo.Id.ToString(), $"Deleted combo {combo.Name}");
        
        return true;
    }

    public async Task<bool> ToggleComboStatusAsync(int id)
    {
        var combo = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);
        if (combo == null) return false;

        combo.IsActive = !combo.IsActive;
        await _context.SaveChangesAsync();
        var userId = _currentUserService.UserId ?? 0;
        await _auditService.LogAsync(userId, "ToggleComboStatus", "MenuItem", combo.Id.ToString(), $"Toggled status to {combo.IsActive}");

        return true;
    }

    private ComboDetailDto MapToDetailDto(MenuItem combo)
    {
        return new ComboDetailDto
        {
            Id = combo.Id,
            CategoryId = combo.CategoryId,
            CategoryName = combo.Category?.Name ?? string.Empty,
            Name = combo.Name,
            NameEn = combo.NameEn,
            Description = combo.Description,
            DescriptionEn = combo.DescriptionEn,
            Price = combo.Price,
            ImageUrl = combo.ImageUrl,
            IsSoldOut = combo.IsSoldOut,
            IsCombo = combo.IsCombo,
            IsActive = combo.IsActive,
            SortOrder = combo.SortOrder,
            Groups = combo.ComboGroups.Select(g => new ComboGroupDto
            {
                Id = g.Id,
                ComboId = g.ComboId,
                Name = g.Name,
                MinSelections = g.MinSelections,
                MaxSelections = g.MaxSelections,
                IsRequired = g.IsRequired,
                SortOrder = g.SortOrder,
                Items = g.ComboItems.Select(ci => MapToItemDto(ci)).OrderBy(ci => ci.SortOrder).ToList()
            }).OrderBy(g => g.SortOrder).ToList(),
            FixedAndAddOnItems = combo.ComboItems.Where(ci => ci.ComboGroupId == null).Select(ci => MapToItemDto(ci)).OrderBy(ci => ci.SortOrder).ToList()
        };
    }

    private ComboItemDto MapToItemDto(ComboItem item)
    {
        return new ComboItemDto
        {
            Id = item.Id,
            ComboId = item.ComboId,
            ComboGroupId = item.ComboGroupId,
            MenuItemId = item.MenuItemId,
            MenuItemName = item.MenuItem?.Name ?? string.Empty,
            MenuItemNameEn = item.MenuItem?.NameEn,
            OriginalPrice = item.MenuItem?.Price ?? 0,
            ComponentType = (ComboComponentType)item.ComponentType,
            DefaultQuantity = item.DefaultQuantity,
            AdditionalPrice = item.AdditionalPrice,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
    }
}
