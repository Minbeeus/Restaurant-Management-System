using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class TableService : ITableService
{
    private readonly ApplicationDbContext _context;
    private readonly IQrCodeService _qrCodeService;

    public TableService(ApplicationDbContext context, IQrCodeService qrCodeService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
    }

    public async Task<List<TableDto>> GetAllAsync()
    {
        return await _context.Tables
            .Include(t => t.Area)
            .OrderBy(t => t.SortOrder)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<List<TableDto>> GetByAreaIdAsync(int areaId)
    {
        return await _context.Tables
            .Include(t => t.Area)
            .Where(t => t.AreaId == areaId)
            .OrderBy(t => t.SortOrder)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<TableDto?> GetByIdAsync(int id)
    {
        var table = await _context.Tables
            .Include(t => t.Area)
            .FirstOrDefaultAsync(t => t.Id == id);

        return table == null ? null : MapToDto(table);
    }

    public async Task<TableDto> CreateAsync(CreateTableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên bàn ăn không được để trống.");
        if (request.Capacity <= 0)
            throw new ArgumentException("Sức chứa bàn phải lớn hơn 0.");

        var table = new Table
        {
            AreaId = request.AreaId,
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            Status = (int)TableStatus.Available,
            QrToken = _qrCodeService.GenerateSecureToken(),
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(table.Id))!;
    }

    public async Task<TableDto?> UpdateAsync(int id, UpdateTableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên bàn ăn không được để trống.");
        if (request.Capacity <= 0)
            throw new ArgumentException("Sức chứa bàn phải lớn hơn 0.");

        var table = await _context.Tables.FindAsync(id);
        if (table == null) return null;

        table.AreaId = request.AreaId;
        table.Name = request.Name.Trim();
        table.Capacity = request.Capacity;
        table.Status = request.Status;
        table.SortOrder = request.SortOrder;
        table.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return false;

        _context.Tables.Remove(table);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<byte[]?> GetTableQrCodeImageAsync(int tableId, string baseUrl)
    {
        var table = await _context.Tables.FindAsync(tableId);
        if (table == null) return null;

        var orderUrl = $"{baseUrl.TrimEnd('/')}/order?tableId={table.Id}&token={table.QrToken}";
        return _qrCodeService.GenerateQrCodeImage(orderUrl);
    }

    private static TableDto MapToDto(Table t)
    {
        return new TableDto
        {
            Id = t.Id,
            AreaId = t.AreaId,
            AreaName = t.Area?.Name ?? string.Empty,
            Name = t.Name,
            Capacity = t.Capacity,
            Status = t.Status,
            QrToken = t.QrToken,
            SortOrder = t.SortOrder,
            IsActive = t.IsActive
        };
    }
}
