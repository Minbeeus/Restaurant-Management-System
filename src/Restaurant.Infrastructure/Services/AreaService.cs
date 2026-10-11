using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class AreaService : IAreaService
{
    private readonly ApplicationDbContext _context;

    public AreaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AreaDto>> GetAllAsync()
    {
        return await _context.Areas
            .Include(a => a.Tables)
            .OrderBy(a => a.SortOrder)
            .Select(a => new AreaDto
            {
                Id = a.Id,
                Name = a.Name,
                NameEn = a.NameEn,
                SortOrder = a.SortOrder,
                IsActive = a.IsActive,
                TableCount = a.Tables.Count(t => t.IsActive && !t.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<AreaDto?> GetByIdAsync(int id)
    {
        var area = await _context.Areas
            .Include(a => a.Tables)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null) return null;

        return new AreaDto
        {
            Id = area.Id,
            Name = area.Name,
            NameEn = area.NameEn,
            SortOrder = area.SortOrder,
            IsActive = area.IsActive,
            TableCount = area.Tables.Count(t => t.IsActive && !t.IsDeleted)
        };
    }

    public async Task<AreaDto> CreateAsync(CreateAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên khu vực không được để trống.");

        var area = new Area
        {
            Name = request.Name.Trim(),
            NameEn = request.NameEn?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.Areas.Add(area);
        await _context.SaveChangesAsync();

        return new AreaDto
        {
            Id = area.Id,
            Name = area.Name,
            NameEn = area.NameEn,
            SortOrder = area.SortOrder,
            IsActive = area.IsActive,
            TableCount = 0
        };
    }

    public async Task<AreaDto?> UpdateAsync(int id, UpdateAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên khu vực không được để trống.");

        var area = await _context.Areas
            .Include(a => a.Tables)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null) return null;

        area.Name = request.Name.Trim();
        area.NameEn = request.NameEn?.Trim();
        area.SortOrder = request.SortOrder;
        area.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return new AreaDto
        {
            Id = area.Id,
            Name = area.Name,
            NameEn = area.NameEn,
            SortOrder = area.SortOrder,
            IsActive = area.IsActive,
            TableCount = area.Tables.Count
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area == null) return false;

        var hasTables = await _context.Tables.AnyAsync(t => t.AreaId == id && !t.IsDeleted);
        if (hasTables)
        {
            throw new InvalidOperationException("Không thể xóa khu vực đang chứa bàn. Vui lòng chuyển hoặc xóa bàn trước.");
        }

        _context.Areas.Remove(area);
        await _context.SaveChangesAsync();
        return true;
    }
}
