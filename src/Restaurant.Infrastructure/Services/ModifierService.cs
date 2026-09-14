using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ModifierService : IModifierService
{
    private readonly ApplicationDbContext _context;

    public ModifierService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ModifierGroupDto>> GetAllGroupsAsync()
    {
        return await _context.ModifierGroups
            .Include(mg => mg.ModifierOptions.OrderBy(o => o.SortOrder))
            .OrderBy(mg => mg.SortOrder)
            .Select(mg => new ModifierGroupDto
            {
                Id = mg.Id,
                Name = mg.Name,
                IsRequired = mg.IsRequired,
                MinSelections = mg.MinSelections,
                MaxSelections = mg.MaxSelections,
                SortOrder = mg.SortOrder,
                ModifierOptions = mg.ModifierOptions.Select(o => new ModifierOptionDto
                {
                    Id = o.Id,
                    ModifierGroupId = o.ModifierGroupId,
                    Name = o.Name,
                    ExtraPrice = o.ExtraPrice,
                    IsDefault = o.IsDefault,
                    SortOrder = o.SortOrder,
                    IsActive = o.IsActive
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<ModifierGroupDto?> GetGroupByIdAsync(int id)
    {
        var mg = await _context.ModifierGroups
            .Include(mg => mg.ModifierOptions.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(mg => mg.Id == id);

        if (mg == null) return null;

        return new ModifierGroupDto
        {
            Id = mg.Id,
            Name = mg.Name,
            IsRequired = mg.IsRequired,
            MinSelections = mg.MinSelections,
            MaxSelections = mg.MaxSelections,
            SortOrder = mg.SortOrder,
            ModifierOptions = mg.ModifierOptions.Select(o => new ModifierOptionDto
            {
                Id = o.Id,
                ModifierGroupId = o.ModifierGroupId,
                Name = o.Name,
                ExtraPrice = o.ExtraPrice,
                IsDefault = o.IsDefault,
                SortOrder = o.SortOrder,
                IsActive = o.IsActive
            }).ToList()
        };
    }

    public async Task<ModifierGroupDto> CreateGroupAsync(CreateModifierGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên nhóm tùy chọn không được để trống.");

        var group = new ModifierGroup
        {
            Name = request.Name.Trim(),
            IsRequired = request.IsRequired,
            MinSelections = request.MinSelections,
            MaxSelections = request.MaxSelections,
            SortOrder = request.SortOrder,
            ModifierOptions = request.Options.Select(o => new ModifierOption
            {
                Name = o.Name.Trim(),
                ExtraPrice = o.ExtraPrice,
                IsDefault = o.IsDefault,
                SortOrder = o.SortOrder,
                IsActive = true
            }).ToList()
        };

        _context.ModifierGroups.Add(group);
        await _context.SaveChangesAsync();

        return (await GetGroupByIdAsync(group.Id))!;
    }

    public async Task<ModifierGroupDto?> UpdateGroupAsync(int id, UpdateModifierGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên nhóm tùy chọn không được để trống.");

        var group = await _context.ModifierGroups.FindAsync(id);
        if (group == null) return null;

        group.Name = request.Name.Trim();
        group.IsRequired = request.IsRequired;
        group.MinSelections = request.MinSelections;
        group.MaxSelections = request.MaxSelections;
        group.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();
        return await GetGroupByIdAsync(id);
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        var group = await _context.ModifierGroups.FindAsync(id);
        if (group == null) return false;

        _context.ModifierGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ModifierOptionDto> AddOptionAsync(int groupId, CreateModifierOptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên tùy chọn không được để trống.");

        var option = new ModifierOption
        {
            ModifierGroupId = groupId,
            Name = request.Name.Trim(),
            ExtraPrice = request.ExtraPrice,
            IsDefault = request.IsDefault,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.ModifierOptions.Add(option);
        await _context.SaveChangesAsync();

        return new ModifierOptionDto
        {
            Id = option.Id,
            ModifierGroupId = option.ModifierGroupId,
            Name = option.Name,
            ExtraPrice = option.ExtraPrice,
            IsDefault = option.IsDefault,
            SortOrder = option.SortOrder,
            IsActive = option.IsActive
        };
    }

    public async Task<ModifierOptionDto?> UpdateOptionAsync(int optionId, UpdateModifierOptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên tùy chọn không được để trống.");

        var option = await _context.ModifierOptions.FindAsync(optionId);
        if (option == null) return null;

        option.Name = request.Name.Trim();
        option.ExtraPrice = request.ExtraPrice;
        option.IsDefault = request.IsDefault;
        option.SortOrder = request.SortOrder;
        option.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return new ModifierOptionDto
        {
            Id = option.Id,
            ModifierGroupId = option.ModifierGroupId,
            Name = option.Name,
            ExtraPrice = option.ExtraPrice,
            IsDefault = option.IsDefault,
            SortOrder = option.SortOrder,
            IsActive = option.IsActive
        };
    }

    public async Task<bool> DeleteOptionAsync(int optionId)
    {
        var option = await _context.ModifierOptions.FindAsync(optionId);
        if (option == null) return false;

        _context.ModifierOptions.Remove(option);
        await _context.SaveChangesAsync();
        return true;
    }
}
