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

    private async Task ValidateComboStructureAsync(CreateComboRequest request, int? comboIdToExclude = null)
    {
        if (request.Price < 0)
        {
            throw new InvalidOperationException("Giá Combo không được âm.");
        }

        var totalItemsCount = request.FixedAndAddOnItems.Count + request.Groups.Sum(g => g.Items.Count);
        if (totalItemsCount == 0)
        {
            throw new InvalidOperationException("Combo không được rỗng. Cần có ít nhất một món ăn thành phần hoặc nhóm lựa chọn.");
        }

        // Validate Fixed and AddOn Items
        var fixedAndAddOnItemIds = new HashSet<int>();
        foreach (var itemReq in request.FixedAndAddOnItems)
        {
            if (itemReq.DefaultQuantity <= 0)
            {
                throw new InvalidOperationException("Số lượng món ăn thành phần phải lớn hơn 0.");
            }
            if (itemReq.AdditionalPrice < 0)
            {
                throw new InvalidOperationException("Phụ phí món không được âm.");
            }
            if (!fixedAndAddOnItemIds.Add(itemReq.MenuItemId))
            {
                throw new InvalidOperationException($"Món ăn (MenuItemId: {itemReq.MenuItemId}) bị trùng lặp trong danh sách món cố định/bổ sung.");
            }

            var targetItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == itemReq.MenuItemId && !m.IsDeleted);
            if (targetItem == null || !targetItem.IsActive)
            {
                throw new InvalidOperationException($"Món ăn (MenuItemId: {itemReq.MenuItemId}) không tồn tại hoặc đã ngừng phục vụ.");
            }
            if (targetItem.IsCombo)
            {
                throw new InvalidOperationException($"Không thể lồng Combo '{targetItem.Name}' bên trong Combo khác.");
            }
        }

        // Validate Groups
        foreach (var groupReq in request.Groups)
        {
            if (string.IsNullOrWhiteSpace(groupReq.Name))
            {
                throw new InvalidOperationException("Tên nhóm lựa chọn không được để trống.");
            }
            if (groupReq.MinSelections < 0)
            {
                throw new InvalidOperationException($"MinSelections của nhóm '{groupReq.Name}' không được âm.");
            }
            if (groupReq.MaxSelections < 1)
            {
                throw new InvalidOperationException($"MaxSelections của nhóm '{groupReq.Name}' phải từ 1 trở lên.");
            }
            if (groupReq.MinSelections > groupReq.MaxSelections)
            {
                throw new InvalidOperationException($"Nhóm '{groupReq.Name}' có MinSelections ({groupReq.MinSelections}) lớn hơn MaxSelections ({groupReq.MaxSelections}).");
            }
            if (groupReq.IsRequired && groupReq.MinSelections < 1)
            {
                throw new InvalidOperationException($"Nhóm '{groupReq.Name}' được đánh dấu bắt buộc nhưng MinSelections lại nhỏ hơn 1.");
            }
            if (!groupReq.Items.Any())
            {
                throw new InvalidOperationException($"Nhóm '{groupReq.Name}' phải có ít nhất một món lựa chọn.");
            }

            var groupItemIds = new HashSet<int>();
            foreach (var itemReq in groupReq.Items)
            {
                if (itemReq.DefaultQuantity <= 0)
                {
                    throw new InvalidOperationException($"Số lượng món trong nhóm '{groupReq.Name}' phải lớn hơn 0.");
                }
                if (itemReq.AdditionalPrice < 0)
                {
                    throw new InvalidOperationException($"Phụ phí món trong nhóm '{groupReq.Name}' không được âm.");
                }
                if (!groupItemIds.Add(itemReq.MenuItemId))
                {
                    throw new InvalidOperationException($"Món ăn (MenuItemId: {itemReq.MenuItemId}) bị trùng lặp trong nhóm '{groupReq.Name}'.");
                }

                var targetItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == itemReq.MenuItemId && !m.IsDeleted);
                if (targetItem == null || !targetItem.IsActive)
                {
                    throw new InvalidOperationException($"Món ăn (MenuItemId: {itemReq.MenuItemId}) trong nhóm '{groupReq.Name}' không tồn tại hoặc đã ngừng phục vụ.");
                }
                if (targetItem.IsCombo)
                {
                    throw new InvalidOperationException($"Không thể lồng Combo '{targetItem.Name}' bên trong nhóm '{groupReq.Name}'.");
                }
            }
        }
    }

    public async Task<ComboDetailDto> CreateComboAsync(CreateComboRequest request)
    {
        await ValidateComboStructureAsync(request);

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
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
                        SortOrder = itemReq.SortOrder,
                        IsActive = true
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
                    SortOrder = itemReq.SortOrder,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();

            var userId = (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value > 0) ? _currentUserService.UserId.Value : 1;
            try
            {
                await _auditService.LogAsync(userId, "CreateCombo", "MenuItem", combo.Id.ToString(), $"Created combo {combo.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit log warning: {ex.Message}");
            }

            await dbTransaction.CommitAsync();

            return await GetComboByIdAsync(combo.Id) ?? throw new InvalidOperationException("Không thể tìm lại Combo sau khi tạo.");
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ComboDetailDto?> UpdateComboAsync(int id, UpdateComboRequest request)
    {
        await ValidateComboStructureAsync(request, comboIdToExclude: id);

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
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

            // Strategy: Update Existing Groups/Items in-place, Add New ones, Remove/Deactivate missing ones.
            // This guarantees existing ComboItem.Id remains intact, ensuring historic or active references don't break.
            var existingGroups = combo.ComboGroups.ToList();
            var incomingGroupIds = request.Groups.Where(g => g.Id.HasValue && g.Id.Value > 0).Select(g => g.Id!.Value).ToHashSet();

            // 1. Remove groups not in request
            foreach (var oldGroup in existingGroups)
            {
                if (!incomingGroupIds.Contains(oldGroup.Id))
                {
                    _context.ComboItems.RemoveRange(oldGroup.ComboItems);
                    _context.ComboGroups.Remove(oldGroup);
                }
            }

            // 2. Process incoming groups (update existing or add new)
            foreach (var groupReq in request.Groups)
            {
                ComboGroup group;
                if (groupReq.Id.HasValue && groupReq.Id.Value > 0 && existingGroups.FirstOrDefault(g => g.Id == groupReq.Id.Value) is { } existingGroup)
                {
                    group = existingGroup;
                    group.Name = groupReq.Name;
                    group.MinSelections = groupReq.MinSelections;
                    group.MaxSelections = groupReq.MaxSelections;
                    group.IsRequired = groupReq.IsRequired;
                    group.SortOrder = groupReq.SortOrder;
                }
                else
                {
                    group = new ComboGroup
                    {
                        Combo = combo,
                        Name = groupReq.Name,
                        MinSelections = groupReq.MinSelections,
                        MaxSelections = groupReq.MaxSelections,
                        IsRequired = groupReq.IsRequired,
                        SortOrder = groupReq.SortOrder
                    };
                    combo.ComboGroups.Add(group);
                }

                var existingGroupItems = group.ComboItems.ToList();
                var incomingItemIds = groupReq.Items.Where(i => i.Id.HasValue && i.Id.Value > 0).Select(i => i.Id!.Value).ToHashSet();

                // Remove items in group not in request
                foreach (var oldItem in existingGroupItems)
                {
                    if (!incomingItemIds.Contains(oldItem.Id))
                    {
                        _context.ComboItems.Remove(oldItem);
                    }
                }

                // Update or add group items
                foreach (var itemReq in groupReq.Items)
                {
                    if (itemReq.Id.HasValue && itemReq.Id.Value > 0 && existingGroupItems.FirstOrDefault(i => i.Id == itemReq.Id.Value) is { } existingItem)
                    {
                        existingItem.MenuItemId = itemReq.MenuItemId;
                        existingItem.ComponentType = (int)itemReq.ComponentType;
                        existingItem.DefaultQuantity = itemReq.DefaultQuantity;
                        existingItem.AdditionalPrice = itemReq.AdditionalPrice;
                        existingItem.SortOrder = itemReq.SortOrder;
                        existingItem.IsActive = true;
                    }
                    else
                    {
                        group.ComboItems.Add(new ComboItem
                        {
                            Combo = combo,
                            ComboGroup = group,
                            MenuItemId = itemReq.MenuItemId,
                            ComponentType = (int)itemReq.ComponentType,
                            DefaultQuantity = itemReq.DefaultQuantity,
                            AdditionalPrice = itemReq.AdditionalPrice,
                            SortOrder = itemReq.SortOrder,
                            IsActive = true
                        });
                    }
                }
            }

            // 3. Process Fixed and AddOn Items without group
            var existingFixedItems = combo.ComboItems.Where(ci => ci.ComboGroupId == null).ToList();
            var incomingFixedItemIds = request.FixedAndAddOnItems.Where(i => i.Id.HasValue && i.Id.Value > 0).Select(i => i.Id!.Value).ToHashSet();

            foreach (var oldFixed in existingFixedItems)
            {
                if (!incomingFixedItemIds.Contains(oldFixed.Id))
                {
                    _context.ComboItems.Remove(oldFixed);
                }
            }

            foreach (var itemReq in request.FixedAndAddOnItems)
            {
                if (itemReq.Id.HasValue && itemReq.Id.Value > 0 && existingFixedItems.FirstOrDefault(i => i.Id == itemReq.Id.Value) is { } existingFixed)
                {
                    existingFixed.MenuItemId = itemReq.MenuItemId;
                    existingFixed.ComponentType = (int)itemReq.ComponentType;
                    existingFixed.DefaultQuantity = itemReq.DefaultQuantity;
                    existingFixed.AdditionalPrice = itemReq.AdditionalPrice;
                    existingFixed.SortOrder = itemReq.SortOrder;
                    existingFixed.IsActive = true;
                }
                else
                {
                    combo.ComboItems.Add(new ComboItem
                    {
                        Combo = combo,
                        MenuItemId = itemReq.MenuItemId,
                        ComponentType = (int)itemReq.ComponentType,
                        DefaultQuantity = itemReq.DefaultQuantity,
                        AdditionalPrice = itemReq.AdditionalPrice,
                        SortOrder = itemReq.SortOrder,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            var userId = (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value > 0) ? _currentUserService.UserId.Value : 1;
            try
            {
                await _auditService.LogAsync(userId, "UpdateCombo", "MenuItem", combo.Id.ToString(), $"Updated combo {combo.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit log warning: {ex.Message}");
            }

            await dbTransaction.CommitAsync();

            return await GetComboByIdAsync(combo.Id);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteComboAsync(int id)
    {
        var combo = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);
        if (combo == null) return false;

        // Soft delete
        combo.IsDeleted = true;
        await _context.SaveChangesAsync();
        var userId = (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value > 0) ? _currentUserService.UserId.Value : 1;
        try
        {
            await _auditService.LogAsync(userId, "DeleteCombo", "MenuItem", combo.Id.ToString(), $"Deleted combo {combo.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audit log warning: {ex.Message}");
        }
        
        return true;
    }

    public async Task<bool> ToggleComboStatusAsync(int id)
    {
        var combo = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.IsCombo);
        if (combo == null) return false;

        combo.IsActive = !combo.IsActive;
        await _context.SaveChangesAsync();
        var userId = (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value > 0) ? _currentUserService.UserId.Value : 1;
        try
        {
            await _auditService.LogAsync(userId, "ToggleComboStatus", "MenuItem", combo.Id.ToString(), $"Toggled status to {combo.IsActive}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audit log warning: {ex.Message}");
        }

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
            IsActive = item.IsActive,
            ImageUrl = item.MenuItem?.ImageUrl
        };
    }
}
