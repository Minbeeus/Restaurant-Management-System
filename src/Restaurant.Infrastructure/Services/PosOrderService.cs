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
            .Where(o => o.TableId != null && o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled)
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
                OrderCreatedAt = activeOrd?.CreatedAt,
                PositionX = t.PositionX,
                PositionY = t.PositionY,
                Width = t.Width,
                Height = t.Height,
                Shape = t.Shape
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

            order.Status = (int)OrderStatus.InPreparation;
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
                .FirstOrDefaultAsync(o => o.TableId == request.SourceTableId && o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled);

            int oldStatus = sourceTable.Status;
            if (activeOrder != null)
            {
                activeOrder.TableId = targetTable.Id;
            }

            sourceTable.Status = (int)TableStatus.Available;
            targetTable.Status = activeOrder != null ? oldStatus : (int)TableStatus.Available;

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
                .FirstOrDefaultAsync(o => o.TableId == request.TargetTableId && o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled);

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
                    Status = (int)OrderStatus.InPreparation,
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
                .Where(o => request.SourceTableIds.Contains(o.TableId ?? 0) && o.TableId != request.TargetTableId && o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled)
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

            int maxStatus = targetTable.Status;
            foreach (var srcTable in sourceTables)
            {
                if (srcTable.Status > maxStatus) maxStatus = srcTable.Status;
                srcTable.Status = (int)TableStatus.Available;
            }

            targetTable.Status = maxStatus > (int)TableStatus.Available ? maxStatus : (int)TableStatus.Occupied;

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
            await _notificationService.NotifyTableStatusSyncAsync(targetTable.Id, Enum.GetName(typeof(TableStatus), targetTable.Status) ?? "Unknown");

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
        (int)TableStatus.Serving => "Serving (Đã gửi bếp)",
        (int)TableStatus.Billing => "Billing (Chờ thanh toán)",
        (int)TableStatus.Paid => "Paid (Đã thanh toán)",
        _ => "Unknown"
    };

    public async Task<SubmitPosOrderResponse> SubmitPosOrderAsync(SubmitPosOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any() || request.Items.All(i => i.Quantity <= 0))
        {
            throw new InvalidOperationException("Giỏ hàng đang trống hoặc số lượng món không hợp lệ.");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Table? table = null;
            if (request.TableId.HasValue && request.TableId.Value > 0)
            {
                table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.TableId.Value);
                if (table != null && table.Status == (int)TableStatus.Billing)
                {
                    throw new InvalidOperationException(_localizer["TABLE_IS_BILLING"]);
                }
            }

            // 0. Pre-validate all items and combo child items before mutating DB or creating Order
            foreach (var itemReq in request.Items)
            {
                var m = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == itemReq.MenuItemId);
                if (m == null || !m.IsActive)
                {
                    throw new InvalidOperationException($"Món ăn (ID: {itemReq.MenuItemId}) không tồn tại hoặc đã ngừng phục vụ.");
                }

                if (itemReq.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Số lượng món '{m.Name}' phải lớn hơn 0.");
                }

                if (m.IsCombo)
                {
                    if (itemReq.SelectedComboItems == null || !itemReq.SelectedComboItems.Any())
                    {
                        throw new InvalidOperationException($"Combo '{m.Name}' yêu cầu phải chọn các món thành phần.");
                    }

                    // 1. Validate all combo child items first (existence, association, active state, not nested)
                    var resolvedComboItems = new List<(CartComboItemRequest Req, ComboItem DbCi)>();
                    foreach (var comboReq in itemReq.SelectedComboItems)
                    {
                        if (comboReq.Quantity <= 0)
                        {
                            throw new InvalidOperationException($"Số lượng món thành phần trong Combo '{m.Name}' phải lớn hơn 0.");
                        }

                        ComboItem? dbCi = null;
                        if (comboReq.ComboItemId > 0)
                        {
                            dbCi = await _context.ComboItems
                                .Include(ci => ci.MenuItem)
                                .FirstOrDefaultAsync(ci => ci.Id == comboReq.ComboItemId);

                            if (dbCi == null || dbCi.ComboId != m.Id)
                            {
                                throw new InvalidOperationException($"Món thành phần (ComboItemId: {comboReq.ComboItemId}) không tồn tại hoặc không thuộc Combo '{m.Name}'.");
                            }
                        }
                        else
                        {
                            dbCi = await _context.ComboItems
                                .Include(ci => ci.MenuItem)
                                .FirstOrDefaultAsync(ci => ci.ComboId == m.Id && ci.MenuItemId == comboReq.MenuItemId);

                            if (dbCi == null)
                            {
                                throw new InvalidOperationException($"Món (MenuItemId: {comboReq.MenuItemId}) không thuộc cấu hình của Combo '{m.Name}'.");
                            }
                        }

                        if (dbCi.MenuItem == null || !dbCi.MenuItem.IsActive)
                        {
                            throw new InvalidOperationException($"Món thành phần '{dbCi.MenuItem?.Name ?? comboReq.MenuItemId.ToString()}' trong combo không tồn tại hoặc đã ngừng phục vụ.");
                        }

                        if (dbCi.MenuItem.IsCombo)
                        {
                            throw new InvalidOperationException($"Không được phép lồng Combo '{dbCi.MenuItem.Name}' bên trong Combo '{m.Name}'.");
                        }

                        // Auto-fill ComboGroupId and ComboItemId if omitted
                        if (!comboReq.ComboGroupId.HasValue && dbCi.ComboGroupId.HasValue)
                        {
                            comboReq.ComboGroupId = dbCi.ComboGroupId;
                        }
                        if (comboReq.ComboItemId <= 0)
                        {
                            comboReq.ComboItemId = dbCi.Id;
                        }

                        resolvedComboItems.Add((comboReq, dbCi));
                    }

                    // Check for duplicate combo child selections
                    var childItemKeys = new HashSet<(int? GroupId, int MenuItemId)>();
                    foreach (var (cReq, _) in resolvedComboItems)
                    {
                        if (!childItemKeys.Add((cReq.ComboGroupId, cReq.MenuItemId)))
                        {
                            throw new InvalidOperationException($"Món ăn (MenuItemId: {cReq.MenuItemId}) bị chọn trùng lặp trong cùng một nhóm của Combo '{m.Name}'.");
                        }
                    }

                    // 2. Group-level validation: MinSelections, MaxSelections, IsRequired
                    var comboEntity = await _context.MenuItems
                        .Include(c => c.ComboGroups)
                            .ThenInclude(g => g.ComboItems)
                        .Include(c => c.ComboItems)
                        .FirstOrDefaultAsync(c => c.Id == m.Id);

                    if (comboEntity != null && comboEntity.ComboGroups.Any())
                    {
                        foreach (var group in comboEntity.ComboGroups)
                        {
                            var selectedInGroup = resolvedComboItems
                                .Where(x => x.Req.ComboGroupId == group.Id || x.DbCi.ComboGroupId == group.Id)
                                .Sum(x => x.Req.Quantity);

                            if (group.IsRequired && selectedInGroup < group.MinSelections)
                            {
                                throw new InvalidOperationException($"Nhóm '{group.Name}' trong Combo '{m.Name}' yêu cầu chọn ít nhất {group.MinSelections} món (đã chọn: {selectedInGroup}).");
                            }

                            if (group.MaxSelections > 0 && selectedInGroup > group.MaxSelections)
                            {
                                throw new InvalidOperationException($"Nhóm '{group.Name}' trong Combo '{m.Name}' chỉ được chọn tối đa {group.MaxSelections} món (đã chọn: {selectedInGroup}).");
                            }
                        }
                    }
                }
            }

            Order? activeOrder = null;
            if (table != null)
            {
                activeOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.TableId == table.Id && (o.Status == (int)OrderStatus.Draft || o.Status == (int)OrderStatus.Pending || o.Status == (int)OrderStatus.InPreparation || o.Status == (int)OrderStatus.Ready));
            }

            Order targetOrder;
            if (activeOrder != null)
            {
                targetOrder = activeOrder;
                if (request.CustomerId.HasValue)
                {
                    targetOrder.CustomerId = request.CustomerId;
                }
                if (!string.IsNullOrEmpty(request.CustomerName)) targetOrder.CustomerName = request.CustomerName;
                if (!string.IsNullOrEmpty(request.CustomerPhone)) targetOrder.CustomerPhone = request.CustomerPhone;
            }
            else
            {
                var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == (int)ShiftStatus.Open)
                                 ?? await _context.Shifts.FirstOrDefaultAsync();

                if (activeShift == null)
                {
                    var firstUser = await _context.Users.FirstOrDefaultAsync()
                                    ?? throw new InvalidOperationException("Hệ thống chưa có tài khoản người dùng để gán đơn hàng.");
                    var firstRegister = await _context.Registers.FirstOrDefaultAsync();
                    activeShift = new Shift
                    {
                        UserId = firstUser.Id,
                        RegisterId = firstRegister?.Id,
                        OpeningBalance = 1000000m,
                        Status = (int)ShiftStatus.Open,
                        OpenedAt = DateTime.UtcNow,
                        Note = "Tự động mở ca cho đơn POS"
                    };
                    _context.Shifts.Add(activeShift);
                    await _context.SaveChangesAsync();
                }

                var shiftId = activeShift.Id;
                var createdUserId = activeShift.UserId;

                var isTakeaway = table == null;
                targetOrder = new Order
                {
                    OrderCode = isTakeaway ? $"#TAK-{DateTime.UtcNow.Ticks % 100000}" : $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.TableId}",
                    OrderType = isTakeaway ? (int)OrderType.Pickup : (int)OrderType.DineIn,
                    TableId = table?.Id,
                    ShiftId = shiftId,
                    CreatedByUserId = createdUserId,
                    CustomerId = request.CustomerId,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    Status = isTakeaway ? (int)OrderStatus.Pending : (int)OrderStatus.InPreparation,
                    SubTotal = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0,
                    Note = request.CustomerNote,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(targetOrder);
                await _context.SaveChangesAsync();
            }

            decimal addedSubTotal = 0;
            decimal addedDiscount = 0;
            var newItemsForKitchen = new List<OrderItem>();
            
            foreach (var itemReq in request.Items)
            {
                var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == itemReq.MenuItemId);
                if (menuItem == null || !menuItem.IsActive)
                {
                    throw new InvalidOperationException($"Món ăn (ID: {itemReq.MenuItemId}) không tồn tại hoặc đã ngừng phục vụ.");
                }

                if (itemReq.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Số lượng món '{menuItem.Name}' phải lớn hơn 0.");
                }
                
                decimal baseUnitPrice = menuItem.Price;
                var selectedOptions = new List<ModifierOption>();
                
                if (itemReq.SelectedModifiers != null && itemReq.SelectedModifiers.Any())
                {
                    selectedOptions = await _context.ModifierOptions
                        .Where(o => itemReq.SelectedModifiers.Contains(o.Id))
                        .ToListAsync();
                        
                    baseUnitPrice += selectedOptions.Sum(o => o.ExtraPrice);
                }

                // Backend pricing validation: Item discount calculation
                decimal itemDiscount = 0;
                if (itemReq.DiscountAmount > 0)
                {
                    itemDiscount = Math.Min(baseUnitPrice, Math.Max(0, itemReq.DiscountAmount));
                }
                else if (itemReq.CustomUnitPrice.HasValue && itemReq.CustomUnitPrice.Value < baseUnitPrice)
                {
                    itemDiscount = Math.Max(0, baseUnitPrice - itemReq.CustomUnitPrice.Value);
                }

                decimal finalUnitPrice = Math.Max(0, baseUnitPrice - itemDiscount);

                // Step 1: Create Combo Header (or standard item)
                var orderItem = new OrderItem
                {
                    OrderId = targetOrder.Id,
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    UnitPrice = finalUnitPrice,
                    DiscountAmount = itemDiscount * itemReq.Quantity,
                    Quantity = itemReq.Quantity,
                    Note = itemReq.Note,
                    KitchenStatus = (int)KitchenStatus.Pending, // Do not mark Done prematurely
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();
                
                if (!menuItem.IsCombo)
                {
                    newItemsForKitchen.Add(orderItem);
                }

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

                // Step 2: Handle Combo Items
                if (menuItem.IsCombo)
                {
                    if (itemReq.SelectedComboItems == null || !itemReq.SelectedComboItems.Any())
                    {
                        throw new InvalidOperationException($"Combo '{menuItem.Name}' yêu cầu phải chọn các món thành phần.");
                    }

                    foreach (var comboReq in itemReq.SelectedComboItems)
                    {
                        if (comboReq.Quantity <= 0)
                        {
                            throw new InvalidOperationException($"Số lượng món thành phần trong Combo '{menuItem.Name}' phải lớn hơn 0.");
                        }

                        ComboItem? dbCi = null;
                        if (comboReq.ComboItemId > 0)
                        {
                            dbCi = await _context.ComboItems
                                .Include(ci => ci.MenuItem)
                                .FirstOrDefaultAsync(ci => ci.Id == comboReq.ComboItemId);

                            if (dbCi == null || dbCi.ComboId != menuItem.Id)
                            {
                                throw new InvalidOperationException($"Món thành phần (ComboItemId: {comboReq.ComboItemId}) không tồn tại hoặc không thuộc Combo '{menuItem.Name}'.");
                            }
                        }
                        else
                        {
                            // Fallback by MenuItemId within this Combo
                            dbCi = await _context.ComboItems
                                .Include(ci => ci.MenuItem)
                                .FirstOrDefaultAsync(ci => ci.ComboId == menuItem.Id && ci.MenuItemId == comboReq.MenuItemId);

                            if (dbCi == null)
                            {
                                throw new InvalidOperationException($"Món (MenuItemId: {comboReq.MenuItemId}) không thuộc cấu hình của Combo '{menuItem.Name}'.");
                            }
                        }

                        if (dbCi.MenuItem == null || !dbCi.MenuItem.IsActive)
                        {
                            throw new InvalidOperationException($"Món thành phần '{dbCi.MenuItem?.Name ?? comboReq.MenuItemId.ToString()}' trong combo không tồn tại hoặc đã ngừng phục vụ.");
                        }

                        var childUnitPrice = dbCi.AdditionalPrice; 
                        var childSelectedOptions = new List<ModifierOption>();
                        if (comboReq.SelectedModifiers != null && comboReq.SelectedModifiers.Any())
                        {
                            childSelectedOptions = await _context.ModifierOptions
                                .Where(o => comboReq.SelectedModifiers.Contains(o.Id))
                                .ToListAsync();
                                
                            childUnitPrice += childSelectedOptions.Sum(o => o.ExtraPrice);
                        }

                        var childOrderItem = new OrderItem
                        {
                            OrderId = targetOrder.Id,
                            MenuItemId = dbCi.MenuItemId,
                            MenuItemName = dbCi.MenuItem.Name,
                            UnitPrice = childUnitPrice, // Additional Price + Modifiers
                            DiscountAmount = 0,
                            Quantity = comboReq.Quantity * itemReq.Quantity, // Multiply by parent quantity
                            Note = comboReq.Note,
                            KitchenStatus = (int)KitchenStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            ParentItemId = orderItem.Id,
                            ComboGroupId = dbCi.ComboGroupId,
                            AdditionalPrice = dbCi.AdditionalPrice
                        };

                        _context.OrderItems.Add(childOrderItem);
                        await _context.SaveChangesAsync();
                        newItemsForKitchen.Add(childOrderItem);

                        if (childSelectedOptions.Any())
                        {
                            foreach (var opt in childSelectedOptions)
                            {
                                var orderItemModifier = new OrderItemModifier
                                {
                                    OrderItemId = childOrderItem.Id,
                                    ModifierOptionId = opt.Id,
                                    ModifierOptionName = opt.Name,
                                    ExtraPrice = opt.ExtraPrice
                                };
                                _context.OrderItemModifiers.Add(orderItemModifier);
                                childOrderItem.OrderItemModifiers.Add(orderItemModifier);
                            }
                            await _context.SaveChangesAsync();
                        }

                        addedSubTotal += (childUnitPrice * comboReq.Quantity * itemReq.Quantity);
                    }
                }

                addedSubTotal += (baseUnitPrice * itemReq.Quantity);
                addedDiscount += (itemDiscount * itemReq.Quantity);
            }

            targetOrder.SubTotal += addedSubTotal;
            targetOrder.DiscountAmount += addedDiscount;
            targetOrder.TotalAmount = Math.Max(0, targetOrder.SubTotal - targetOrder.DiscountAmount);
            
            if (table != null && (table.Status == (int)TableStatus.Available || table.Status == (int)TableStatus.Occupied))
            {
                table.Status = (int)TableStatus.Serving;
                await _notificationService.NotifyTableStatusSyncAsync(table.Id, "Serving");
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
                    TableName = table?.Name ?? "Mang đi",
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
                        Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList(),
                        ParentItemId = oi.ParentItemId,
                        ParentItemName = oi.ParentItemId.HasValue ? targetOrder.OrderItems.FirstOrDefault(p => p.Id == oi.ParentItemId.Value)?.MenuItemName : null,
                        IsComboHeader = false,
                        ComboGroupId = oi.ComboGroupId,
                        KitchenStationId = oi.MenuItem?.KitchenStationId
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

    public async Task<bool> PrintTempBillAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return false;

        if (order.Table != null)
        {
            order.Table.Status = (int)TableStatus.Billing;
            await _context.SaveChangesAsync();

            await _notificationService.NotifyTableStatusSyncAsync(order.Table.Id, "Billing");
        }

        return true;
    }

    public async Task<bool> ClearTableAsync(int tableId)
    {
        var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId);
        if (table == null) return false;

        table.Status = (int)TableStatus.Available;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyTableStatusSyncAsync(table.Id, "Available");
        return true;
    }

    public async Task<List<TakeawayOrderDto>> GetTakeawayOrdersAsync()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .Where(o => o.OrderType == (int)OrderType.Pickup || o.TableId == null)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToTakeawayDto).ToList();
    }

    public async Task<TakeawayOrderDto?> GetTakeawayOrderByIdAsync(int orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToTakeawayDto(order);
    }

    public async Task<TakeawayOrderDto?> GetActiveOrderForTableAsync(int tableId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.CreatedByUser)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .FirstOrDefaultAsync(o => o.TableId == tableId && o.PaymentStatus != (int)PaymentStatus.Paid && o.Status != (int)OrderStatus.Completed && o.Status != (int)OrderStatus.Cancelled);

        return order == null ? null : MapToTakeawayDto(order);
    }

    private static TakeawayOrderDto MapToTakeawayDto(Order o)
    {
        var isPaid = o.PaymentStatus == (int)PaymentStatus.Paid || o.Payments.Any(p => p.Status == (int)PaymentStatus.Paid);
        var mainStatusName = o.Status switch
        {
            (int)OrderStatus.Draft => "Chờ xác nhận",
            (int)OrderStatus.Pending => "Chờ xử lý",
            (int)OrderStatus.InPreparation => "Đang chuẩn bị",
            (int)OrderStatus.Ready => "Sẵn sàng giao",
            (int)OrderStatus.Completed => "Hoàn thành",
            (int)OrderStatus.Cancelled => "Đã huỷ",
            _ => "Chờ xử lý"
        };

        var channel = "Thu ngân";
        if (o.Note != null && o.Note.Contains("QR"))
        {
            channel = "QR";
        }
        else if (o.CreatedByUser != null && !string.IsNullOrEmpty(o.CreatedByUser.FullName))
        {
            channel = o.CreatedByUser.FullName;
        }

        return new TakeawayOrderDto
        {
            OrderId = o.Id,
            OrderCode = string.IsNullOrEmpty(o.OrderCode) ? $"#TAK-{o.Id}" : o.OrderCode,
            CustomerName = o.Customer?.FullName ?? "Khách mang về",
            CustomerPhone = o.Customer?.PhoneNumber ?? string.Empty,
            CreatedAt = o.CreatedAt,
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            OrderChannel = channel,
            IsQrOrder = channel == "QR",
            Status = o.Status,
            MainStatusName = mainStatusName,
            IsPaid = isPaid,
            Items = o.OrderItems.Select(oi => new TakeawayOrderItemDto
            {
                OrderItemId = oi.Id,
                MenuItemId = oi.MenuItemId,
                MenuItemName = oi.MenuItemName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Note = oi.Note,
                ParentItemId = oi.ParentItemId,
                ComboGroupId = oi.ComboGroupId,
                Status = oi.Status,
                Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList(),
                ModifierIds = oi.OrderItemModifiers.Select(m => m.ModifierOptionId).ToList()
            }).ToList()
        };
    }

    public async Task<bool> VoidOrderItemAsync(int orderId, int orderItemId, string reason, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status == (int)OrderStatus.Completed || order.Status == (int)OrderStatus.Cancelled)
        {
            return false;
        }

        var targetItem = order.OrderItems.FirstOrDefault(oi => oi.Id == orderItemId);
        if (targetItem == null || targetItem.Status == (int)OrderItemStatus.Voided)
        {
            return false;
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var utcNow = DateTime.UtcNow;
                var voidReason = string.IsNullOrWhiteSpace(reason) ? "Hủy món từ POS" : reason;

                // 1. Mark target item as voided
                targetItem.Status = (int)OrderItemStatus.Voided;
                _context.VoidLogs.Add(new VoidLog
                {
                    OrderId = order.Id,
                    OrderItemId = targetItem.Id,
                    MenuItemId = targetItem.MenuItemId,
                    MenuItemName = targetItem.MenuItemName,
                    Quantity = targetItem.Quantity,
                    UnitPrice = targetItem.UnitPrice,
                    Reason = voidReason,
                    VoidByUserId = userId,
                    CreatedAt = utcNow
                });

                var voidedItems = new List<OrderItem> { targetItem };

                // 2. If target item is a Combo Header (ParentItemId == null), void all its child items
                if (targetItem.ParentItemId == null)
                {
                    var children = order.OrderItems.Where(oi => oi.ParentItemId == targetItem.Id && oi.Status != (int)OrderItemStatus.Voided).ToList();
                    foreach (var child in children)
                    {
                        child.Status = (int)OrderItemStatus.Voided;
                        _context.VoidLogs.Add(new VoidLog
                        {
                            OrderId = order.Id,
                            OrderItemId = child.Id,
                            MenuItemId = child.MenuItemId,
                            MenuItemName = child.MenuItemName,
                            Quantity = child.Quantity,
                            UnitPrice = child.UnitPrice,
                            Reason = $"Hủy theo Combo Header #{targetItem.Id} ({voidReason})",
                            VoidByUserId = userId,
                            CreatedAt = utcNow
                        });
                        voidedItems.Add(child);
                    }
                }

                // 3. Recalculate Order SubTotal and TotalAmount
                order.SubTotal = order.OrderItems
                    .Where(oi => oi.Status != (int)OrderItemStatus.Voided)
                    .Sum(oi => oi.UnitPrice * oi.Quantity);

                order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. Send SignalR Notifications
                int tableId = order.TableId ?? 0;
                foreach (var vItem in voidedItems)
                {
                    await _notificationService.NotifyOrderItemStatusChangedAsync(
                        tableId,
                        order.Id,
                        vItem.Id,
                        (int)vItem.KitchenStatus,
                        "Voided"
                    );
                }

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
