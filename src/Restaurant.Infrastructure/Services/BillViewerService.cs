using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class BillViewerService : IBillViewerService
{
    private readonly ApplicationDbContext _context;

    public BillViewerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BillDetailDto>> QueryBillsAsync(BillQueryRequest request)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Table!)
                .ThenInclude(t => t.Area)
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.OrderItemModifiers)
                    .ThenInclude(m => m.ModifierOption)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
        {
            var kw = request.SearchKeyword.Trim().ToLower();
            query = query.Where(o =>
                o.OrderCode.ToLower().Contains(kw) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(kw)) ||
                (o.CustomerPhone != null && o.CustomerPhone.ToLower().Contains(kw)));
        }

        if (request.FromDate.HasValue) query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        if (request.OrderStatus.HasValue) query = query.Where(o => o.Status == request.OrderStatus.Value);
        if (request.ShiftId.HasValue) query = query.Where(o => o.ShiftId == request.ShiftId.Value);

        if (request.PaymentMethod.HasValue)
        {
            query = query.Where(o => o.Payments.Any(p => p.PaymentMethod == request.PaymentMethod.Value && p.Status == (int)PaymentStatus.Paid));
        }

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return orders.Select(MapToBillDetailDto).ToList();
    }

    public async Task<BillDetailDto?> GetBillDetailsAsync(int orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Table!)
                .ThenInclude(t => t.Area)
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.OrderItemModifiers)
                    .ThenInclude(m => m.ModifierOption)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;
        return MapToBillDetailDto(order);
    }

    public async Task<K80ReceiptDto?> GetBillK80ReceiptAsync(int orderId)
    {
        var bill = await GetBillDetailsAsync(orderId);
        if (bill == null) return null;

        var orderedBillItems = new List<BillItemDto>();
        var rootBillItems = bill.Items.Where(i => !i.ParentItemId.HasValue).ToList();
        var childBillItems = bill.Items.Where(i => i.ParentItemId.HasValue).GroupBy(i => i.ParentItemId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var root in rootBillItems)
        {
            orderedBillItems.Add(root);
            if (childBillItems.TryGetValue(root.Id, out var children))
            {
                orderedBillItems.AddRange(children);
            }
        }

        var parentIds = rootBillItems.Select(r => r.Id).ToHashSet();
        orderedBillItems.AddRange(bill.Items.Where(i => i.ParentItemId.HasValue && !parentIds.Contains(i.ParentItemId.Value)));

        return new K80ReceiptDto
        {
            OrderCode = bill.OrderCode,
            CreatedAt = bill.CreatedAt,
            TableName = bill.TableName,
            CashierName = bill.CashierName,
            CustomerName = bill.CustomerName,
            CustomerPhone = bill.CustomerPhone,
            SubTotal = bill.SubTotal,
            DiscountAmount = bill.DiscountAmount,
            TotalAmount = bill.TotalAmount,
            PaymentMethod = bill.PaymentMethodName,
            CashReceived = bill.CashReceived,
            Items = orderedBillItems.Select(i => new K80ReceiptItemDto
            {
                MenuItemName = i.MenuItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                IsComboChild = i.ParentItemId.HasValue,
                Modifiers = i.Modifiers
            }).ToList()
        };
    }

    private static BillDetailDto MapToBillDetailDto(Order o)
    {
        var lastPayment = o.Payments.FirstOrDefault(p => p.Status == (int)PaymentStatus.Paid) ?? o.Payments.LastOrDefault();
        var payMethodName = lastPayment?.PaymentMethod switch
        {
            (int)PaymentMethod.Cash => "Tiền Mặt",
            (int)PaymentMethod.BankTransfer => "Chuyển Khoản VietQR",
            _ => "Chưa thanh toán"
        };

        var statusName = ((OrderStatus)o.Status) switch
        {
            OrderStatus.Draft => "Nháp",
            OrderStatus.Pending => "Chờ xử lý",
            OrderStatus.InPreparation => "Đang chuẩn bị",
            OrderStatus.Ready => "Sẵn sàng",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };

        var orderTypeName = o.OrderType == 1 || o.TableId == null ? "Takeaway" : "DineIn";
        var tableAreaName = o.Table?.Area?.Name ?? (o.OrderType == 1 ? "Quầy POS" : "Khu vực chung");

        return new BillDetailDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            CreatedAt = o.CreatedAt,
            TableName = o.Table?.Name ?? (o.OrderType == 1 ? "Mang Về" : "Tại bàn"),
            TableAreaName = tableAreaName,
            OrderType = orderTypeName,
            CashierName = o.CreatedByUser?.FullName ?? "Thu ngân",
            CustomerName = o.CustomerName ?? o.Customer?.FullName ?? "Khách lẻ",
            CustomerPhone = o.CustomerPhone ?? o.Customer?.PhoneNumber ?? string.Empty,
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            PaymentMethodName = payMethodName,
            OrderStatusName = statusName,
            CashReceived = lastPayment?.CashReceived ?? o.TotalAmount,
            ChangeAmount = lastPayment?.ChangeAmount ?? 0,
            Items = o.OrderItems.Where(i => i.Status != (int)OrderItemStatus.Voided).Select(i => new BillItemDto
            {
                Id = i.Id,
                MenuItemName = i.MenuItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                ParentItemId = i.ParentItemId,
                Note = i.Note,
                Modifiers = i.OrderItemModifiers.Select(m => m.ModifierOption?.Name ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList()
            }).ToList()
        };
    }
}
