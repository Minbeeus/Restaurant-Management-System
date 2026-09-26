using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                NameEn = c.NameEn,
                Description = c.Description,
                DescriptionEn = c.DescriptionEn,
                ImageUrl = c.ImageUrl,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var c = await _context.Categories.FindAsync(id);
        if (c == null) return null;

        return new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            NameEn = c.NameEn,
            Description = c.Description,
            DescriptionEn = c.DescriptionEn,
            ImageUrl = c.ImageUrl,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        // Shift existing categories with SortOrder >= request.SortOrder by +1
        var affectedCategories = await _context.Categories
            .Where(c => c.SortOrder >= request.SortOrder)
            .ToListAsync();

        foreach (var existing in affectedCategories)
        {
            existing.SortOrder += 1;
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            NameEn = request.NameEn?.Trim(),
            Description = request.Description,
            DescriptionEn = request.DescriptionEn,
            ImageUrl = request.ImageUrl,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        await NormalizeSortOrdersAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            NameEn = category.NameEn,
            Description = category.Description,
            DescriptionEn = category.DescriptionEn,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return null;

        if (category.SortOrder != request.SortOrder)
        {
            var affectedCategories = await _context.Categories
                .Where(c => c.Id != id && c.SortOrder >= request.SortOrder)
                .ToListAsync();

            foreach (var existing in affectedCategories)
            {
                existing.SortOrder += 1;
            }
        }

        category.Name = request.Name.Trim();
        category.NameEn = request.NameEn?.Trim();
        category.Description = request.Description;
        category.DescriptionEn = request.DescriptionEn;
        category.ImageUrl = request.ImageUrl;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        await NormalizeSortOrdersAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            NameEn = category.NameEn,
            Description = category.Description,
            DescriptionEn = category.DescriptionEn,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    private async Task NormalizeSortOrdersAsync()
    {
        var allCategories = await _context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync();

        int order = 1;
        bool hasChanges = false;
        foreach (var cat in allCategories)
        {
            if (cat.SortOrder != order)
            {
                cat.SortOrder = order;
                hasChanges = true;
            }
            order++;
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        var hasActiveItems = await _context.MenuItems.AnyAsync(m => m.CategoryId == id && !m.IsDeleted);
        if (hasActiveItems)
        {
            throw new InvalidOperationException("Không thể xóa danh mục đang chứa món ăn. Vui lòng chuyển hoặc xóa các món ăn trước.");
        }

        _context.Categories.Remove(category); // AuditableEntityInterceptor converts this to Soft Delete (IsDeleted = true)
        await _context.SaveChangesAsync();
        return true;
    }
}
