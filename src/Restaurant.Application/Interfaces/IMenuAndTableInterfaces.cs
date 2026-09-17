using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IMenuItemService
{
    Task<List<MenuItemDto>> GetAllAsync();
    Task<List<MenuItemDto>> GetActiveMenuItemsAsync();
    Task<MenuItemDto?> GetByIdAsync(int id);
    Task<MenuItemDto> CreateAsync(CreateMenuItemRequest request);
    Task<MenuItemDto?> UpdateAsync(int id, UpdateMenuItemRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleSoldOutAsync(int id);
}

public interface IModifierService
{
    Task<List<ModifierGroupDto>> GetAllGroupsAsync();
    Task<ModifierGroupDto?> GetGroupByIdAsync(int id);
    Task<ModifierGroupDto> CreateGroupAsync(CreateModifierGroupRequest request);
    Task<ModifierGroupDto?> UpdateGroupAsync(int id, UpdateModifierGroupRequest request);
    Task<bool> DeleteGroupAsync(int id);
    Task<ModifierOptionDto> AddOptionAsync(int groupId, CreateModifierOptionRequest request);
    Task<ModifierOptionDto?> UpdateOptionAsync(int optionId, UpdateModifierOptionRequest request);
    Task<bool> DeleteOptionAsync(int optionId);
}

public interface IAreaService
{
    Task<List<AreaDto>> GetAllAsync();
    Task<AreaDto?> GetByIdAsync(int id);
    Task<AreaDto> CreateAsync(CreateAreaRequest request);
    Task<AreaDto?> UpdateAsync(int id, UpdateAreaRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface ITableService
{
    Task<List<TableDto>> GetAllAsync();
    Task<List<TableDto>> GetByAreaIdAsync(int areaId);
    Task<TableDto?> GetByIdAsync(int id);
    Task<TableDto> CreateAsync(CreateTableRequest request);
    Task<TableDto?> UpdateAsync(int id, UpdateTableRequest request);
    Task<bool> UpdateBatchLayoutAsync(int areaId, List<UpdateTableLayoutRequest> layout);
    Task<bool> DeleteAsync(int id);
    Task<byte[]?> GetTableQrCodeImageAsync(int tableId, string baseUrl);
}

public interface IQrCodeService
{
    string GenerateSecureToken();
    byte[] GenerateQrCodeImage(string url);
}
