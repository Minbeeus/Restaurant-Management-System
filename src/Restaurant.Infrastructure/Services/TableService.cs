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
            IsActive = true,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Width = request.Width > 0 ? request.Width : 80,
            Height = request.Height > 0 ? request.Height : 80,
            Shape = request.Shape,
            Rotation = request.Rotation
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
        table.PositionX = request.PositionX;
        table.PositionY = request.PositionY;
        table.Width = request.Width > 0 ? request.Width : 80;
        table.Height = request.Height > 0 ? request.Height : 80;
        table.Shape = request.Shape;
        table.Rotation = request.Rotation;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return false;

        if (table.Status == (int)TableStatus.Occupied || table.Status == (int)TableStatus.Billing || table.Status == (int)TableStatus.NewOrder)
        {
            throw new InvalidOperationException("Không thể xóa bàn đang có khách hoặc đang phục vụ. Vui lòng thanh toán đơn hàng trước.");
        }

        var hasActiveOrders = await _context.Orders.AnyAsync(o => o.TableId == id && o.Status != (int)OrderStatus.Paid && o.Status != (int)OrderStatus.Cancelled && o.Status != (int)OrderStatus.Completed);
        if (hasActiveOrders)
        {
            throw new InvalidOperationException("Không thể xóa bàn vì đang có đơn hàng chưa thanh toán.");
        }

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

    public async Task<bool> UpdateBatchLayoutAsync(int areaId, List<UpdateTableLayoutRequest> layout)
    {
        var tables = await _context.Tables.Where(t => t.AreaId == areaId).ToListAsync();
        
        foreach (var req in layout)
        {
            var table = tables.FirstOrDefault(t => t.Id == req.TableId);
            if (table != null)
            {
                table.PositionX = req.PositionX;
                table.PositionY = req.PositionY;
                table.Width = req.Width;
                table.Height = req.Height;
                table.Shape = req.Shape;
                table.Rotation = req.Rotation;
            }
        }

        await _context.SaveChangesAsync();
        return true;
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
            IsActive = t.IsActive,
            PositionX = t.PositionX,
            PositionY = t.PositionY,
            Width = t.Width,
            Height = t.Height,
            Shape = t.Shape,
            Rotation = t.Rotation
        };
    }
}
