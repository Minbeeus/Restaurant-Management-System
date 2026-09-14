using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class PosOrderService : IPosOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IRestaurantNotificationService _notificationService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public PosOrderService(
        ApplicationDbContext context,
        IRestaurantNotificationService notificationService,
        IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _notificationService = notificationService;
        _localizer = localizer;
    }

    public async Task<List<TableStatusOverviewDto>> GetTablesStatusAsync()
    {
        var tables = await _context.Tables
            .AsNoTracking()
            .Include(t => t.Area)
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        var activeOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.TableId != null && o.Status != (int)OrderStatus.Paid && o.Status != (int)OrderStatus.Cancelled)
            .ToListAsync();

        var result = new List<TableStatusOverviewDto>();
        foreach (var t in tables)
        {
            var activeOrd = activeOrders.FirstOrDefault(o => o.TableId == t.Id);
            result.Add(new TableStatusOverviewDto
            {
                TableId = t.Id,
                TableName = t.Name,
                AreaId = t.AreaId,
                AreaName = t.Area?.Name ?? string.Empty,
                Capacity = t.Capacity,
                Status = t.Status,
                StatusName = GetTableStatusName(t.Status),
                CurrentOrderId = activeOrd?.Id,
                CurrentOrderCode = activeOrd?.OrderCode,
                TotalAmount = activeOrd?.TotalAmount ?? 0,
                OrderCreatedAt = activeOrd?.CreatedAt
            });
        }

        return result;
    }

    public async Task<bool> ApproveQrOrderAsync(int orderId, string? note = null)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemModifiers)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == (int)OrderStatus.Cancelled) return false;

            order.Status = (int)OrderStatus.Confirmed;
            if (!string.IsNullOrWhiteSpace(note))
            {
                order.Note = string.IsNullOrEmpty(order.Note) ? note : $"{order.Note} | {note}";
            }

            if (order.Table != null)
            {
                order.Table.Status = (int)TableStatus.Occupied;
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Broadcast SignalR Table Status Sync
            if (order.TableId.HasValue)
            {
                await _notificationService.NotifyTableStatusSyncAsync(order.TableId.Value, "Occupied");
            }

            // Build Kitchen Ticket and push to ChefsGroup
            var ticket = new KitchenTicketDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                TableId = order.TableId,
                TableName = order.Table?.Name ?? "Dine-in",
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems.Select(oi => new KitchenItemDto
                {
                    OrderItemId = oi.Id,
                    OrderId = oi.OrderId,
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItemName,
                    Quantity = oi.Quantity,
                    Note = oi.Note,
                    KitchenStatus = oi.KitchenStatus,
                    KitchenStatusName = Enum.GetName(typeof(KitchenStatus), oi.KitchenStatus) ?? "Unknown",
                    SentToKitchenAt = oi.SentToKitchenAt,
                    CompletedAt = oi.CompletedAt,
                    WaitingMinutes = 0,
                    IsSlaWarning = false,
                    Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()
                }).ToList()
            };

            await _notificationService.NotifyKitchenTicketAsync(ticket);
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> MoveTableAsync(MoveTableRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sourceTable = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.SourceTableId);
            var targetTable = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.TargetTableId);

            if (sourceTable == null || targetTable == null) return false;
            if (targetTable.Status == (int)TableStatus.Occupied || targetTable.Status == (int)TableStatus.Billing)
            {
                throw new InvalidOperationException(_localizer["TARGET_TABLE_OCCUPIED"] ?? "Bàn đích đang có khách hoặc chờ thanh toán.");
            }

            var activeOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.TableId == request.SourceTableId && o.Status != (int)OrderStatus.Paid && o.Status != (int)OrderStatus.Cancelled);

            if (activeOrder != null)
            {
                activeOrder.TableId = targetTable.Id;
            }

            sourceTable.Status = (int)TableStatus.Available;
            targetTable.Status = activeOrder != null ? (int)TableStatus.Occupied : (int)TableStatus.Available;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Broadcast SignalR Events
            await _notificationService.NotifyTableStatusSyncAsync(sourceTable.Id, Enum.GetName(typeof(TableStatus), sourceTable.Status) ?? "Unknown");
            await _notificationService.NotifyTableStatusSyncAsync(targetTable.Id, Enum.GetName(typeof(TableStatus), targetTable.Status) ?? "Unknown");
            await _notificationService.NotifyTableSessionMovedAsync(sourceTable.Id, targetTable.Id, targetTable.Name);

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> MergeTablesAsync(MergeTableRequest request)
    {
        if (request.SourceTableIds == null || !request.SourceTableIds.Any()) return false;

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var targetTable = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.TargetTableId);
            if (targetTable == null) return false;

            var targetOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.TableId == request.TargetTableId && o.Status != (int)OrderStatus.Paid && o.Status != (int)OrderStatus.Cancelled);

            if (targetOrder == null)
            {
                // Create target order if target table was empty
                var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open)
                                 ?? await _context.Shifts.FirstOrDefaultAsync();

                targetOrder = new Order
                {
                    OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.TargetTableId}",
                    OrderType = 0,
                    TableId = request.TargetTableId,
                    ShiftId = activeShift?.Id ?? 1,
                    CreatedByUserId = activeShift?.UserId ?? 1,
                    Status = (int)OrderStatus.Confirmed,
                    SubTotal = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(targetOrder);
                await _context.SaveChangesAsync();
            }

            // Batch Query Source Orders & Tables in a single SQL query each
            var sourceOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => request.SourceTableIds.Contains(o.TableId ?? 0) && o.TableId != request.TargetTableId && o.Status != (int)OrderStatus.Paid && o.Status != (int)OrderStatus.Cancelled)
                .ToListAsync();

            var sourceTables = await _context.Tables
                .Where(t => request.SourceTableIds.Contains(t.Id) && t.Id != request.TargetTableId)
                .ToListAsync();

            foreach (var srcOrder in sourceOrders)
            {
                foreach (var item in srcOrder.OrderItems)
                {
                    item.OrderId = targetOrder.Id;
                }

                targetOrder.SubTotal += srcOrder.SubTotal;
                targetOrder.TotalAmount += srcOrder.TotalAmount;

                srcOrder.Status = (int)OrderStatus.Cancelled;
                srcOrder.Note = $"Merged into Order {targetOrder.OrderCode}";
            }

            foreach (var srcTable in sourceTables)
            {
                srcTable.Status = (int)TableStatus.Available;
            }

            targetTable.Status = (int)TableStatus.Occupied;

            // Batch Single SaveChanges
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Broadcast SignalR Notifications after Transaction commit
            foreach (var srcTableId in request.SourceTableIds)
            {
                if (srcTableId == request.TargetTableId) continue;
                await _notificationService.NotifyTableStatusSyncAsync(srcTableId, Enum.GetName(typeof(TableStatus), (int)TableStatus.Available) ?? "Unknown");
                await _notificationService.NotifyTableSessionMovedAsync(srcTableId, targetTable.Id, targetTable.Name);
            }
            await _notificationService.NotifyTableStatusSyncAsync(targetTable.Id, Enum.GetName(typeof(TableStatus), (int)TableStatus.Occupied) ?? "Unknown");

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private static string GetTableStatusName(int status) => status switch
    {
        (int)TableStatus.Available => "Available (Trống)",
        (int)TableStatus.Occupied => "Occupied (Đang có khách)",
        (int)TableStatus.NewOrder => "NewOrder (Đơn mới QR)",
        (int)TableStatus.Billing => "Billing (Chờ thanh toán)",
        _ => "Unknown"
    };

    public async Task<SubmitPosOrderResponse> SubmitPosOrderAsync(SubmitPosOrderRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.TableId);
            if (table == null)
            {
                throw new InvalidOperationException("Bàn không tồn tại.");
            }
            if (table.Status == (int)TableStatus.Billing)
            {
                throw new InvalidOperationException(_localizer["TABLE_IS_BILLING"]);
            }

            var activeOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.TableId == request.TableId && (o.Status == (int)OrderStatus.Draft || o.Status == (int)OrderStatus.Confirmed || o.Status == (int)OrderStatus.Completed));

            Order targetOrder;
            if (activeOrder != null)
            {
                targetOrder = activeOrder;
            }
            else
            {
                var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open)
                                 ?? await _context.Shifts.FirstOrDefaultAsync();
                var shiftId = activeShift?.Id ?? 1;
                var createdUserId = activeShift?.UserId ?? 1;

                targetOrder = new Order
                {
                    OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.TableId}",
                    OrderType = 0,
                    TableId = request.TableId,
                    ShiftId = shiftId,
                    CreatedByUserId = createdUserId,
                    Status = (int)OrderStatus.Confirmed,
                    SubTotal = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0,
                    Note = request.CustomerNote,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(targetOrder);
                await _context.SaveChangesAsync();
            }

            decimal addedAmount = 0;
            var newItemsForKitchen = new List<OrderItem>();
            
            foreach (var itemReq in request.Items)
            {
                var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == itemReq.MenuItemId);
                if (menuItem == null) continue;
                
                // For simplicity in POS submission, we assume frontend validated prices
                // In production, backend should re-validate prices. We will fetch price from DB
                decimal unitPrice = menuItem.Price;
                var selectedOptions = new List<ModifierOption>();
                
                if (itemReq.SelectedModifiers != null && itemReq.SelectedModifiers.Any())
                {
                    selectedOptions = await _context.ModifierOptions
                        .Where(o => itemReq.SelectedModifiers.Contains(o.Id))
                        .ToListAsync();
                        
                    unitPrice += selectedOptions.Sum(o => o.ExtraPrice);
                }

                var orderItem = new OrderItem
                {
                    OrderId = targetOrder.Id,
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    UnitPrice = unitPrice,
                    Quantity = itemReq.Quantity,
                    Note = itemReq.Note,
                    KitchenStatus = (int)KitchenStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();
                
                newItemsForKitchen.Add(orderItem);

                if (selectedOptions.Any())
                {
                    foreach (var opt in selectedOptions)
                    {
                        var orderItemModifier = new OrderItemModifier
                        {
                            OrderItemId = orderItem.Id,
                            ModifierOptionId = opt.Id,
                            ModifierOptionName = opt.Name,
                            ExtraPrice = opt.ExtraPrice
                        };
                        _context.OrderItemModifiers.Add(orderItemModifier);
                        orderItem.OrderItemModifiers.Add(orderItemModifier);
                    }
                    await _context.SaveChangesAsync();
                }

                addedAmount += (unitPrice * itemReq.Quantity);
            }

            targetOrder.SubTotal += addedAmount;
            targetOrder.TotalAmount += addedAmount;
            
            // Mark Order as Confirmed when submitting from POS
            targetOrder.Status = (int)OrderStatus.Confirmed;
            
            if (table.Status == (int)TableStatus.Available || table.Status == (int)TableStatus.NewOrder)
            {
                table.Status = (int)TableStatus.Occupied;
                await _notificationService.NotifyTableStatusSyncAsync(table.Id, "Occupied");
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Send to Kitchen
            if (newItemsForKitchen.Any())
            {
                var ticket = new KitchenTicketDto
                {
                    OrderId = targetOrder.Id,
                    OrderCode = targetOrder.OrderCode,
                    TableId = targetOrder.TableId,
                    TableName = table.Name,
                    CreatedAt = DateTime.UtcNow,
                    Items = newItemsForKitchen.Select(oi => new KitchenItemDto
                    {
                        OrderItemId = oi.Id,
                        OrderId = oi.OrderId,
                        MenuItemId = oi.MenuItemId,
                        MenuItemName = oi.MenuItemName,
                        Quantity = oi.Quantity,
                        Note = oi.Note,
                        KitchenStatus = oi.KitchenStatus,
                        KitchenStatusName = "Pending",
                        Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()
                    }).ToList()
                };

                await _notificationService.NotifyKitchenTicketAsync(ticket);
            }

            return new SubmitPosOrderResponse
            {
                OrderId = targetOrder.Id,
                OrderCode = targetOrder.OrderCode,
                OrderStatus = targetOrder.Status,
                OrderStatusName = Enum.GetName(typeof(OrderStatus), targetOrder.Status) ?? "Unknown",
                TotalAmount = targetOrder.TotalAmount,
                CreatedAt = targetOrder.CreatedAt
            };
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
