using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Common;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;

namespace Restaurant.Infrastructure.Services;

public class QrOrderService : IQrOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IRestaurantNotificationService _notificationService;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;

    public QrOrderService(
        ApplicationDbContext context,
        IRestaurantNotificationService notificationService,
        IStringLocalizer<SharedResources> localizer,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache)
    {
        _context = context;
        _notificationService = notificationService;
        _localizer = localizer;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<(bool IsValid, string? ErrorCode, string? ErrorMessage)> ValidateTableSessionAsync(int tableId, string qrToken)
    {
        if (tableId <= 0 || string.IsNullOrWhiteSpace(qrToken))
        {
            return (false, "INVALID_QR_TOKEN", _localizer["INVALID_QR_TOKEN"]);
        }

        var table = await _context.Tables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted);

        if (table == null || !table.QrToken.Equals(qrToken, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "INVALID_QR_TOKEN", _localizer["INVALID_QR_TOKEN"]);
        }

        // Check if Table is currently in Billing status
        if (table.Status == (int)TableStatus.Billing)
        {
            return (false, "TABLE_IS_BILLING", _localizer["TABLE_IS_BILLING"]);
        }

        return (true, null, null);
    }

    public async Task<PublicMenuResponse> GetPublicMenuAsync(int tableId, string qrToken)
    {
        var validation = await ValidateTableSessionAsync(tableId, qrToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage ?? _localizer["INVALID_QR_TOKEN"]);
        }

        var cacheKey = $"PublicMenu_{tableId}";
        if (_memoryCache.TryGetValue(cacheKey, out PublicMenuResponse? cachedResponse))
        {
            return cachedResponse!;
        }

        var table = await _context.Tables
            .AsNoTracking()
            .Include(t => t.Area)
            .FirstOrDefaultAsync(t => t.Id == tableId);

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .Include(c => c.MenuItems.Where(m => m.IsActive && !m.IsDeleted))
            .ToListAsync();

        var response = new PublicMenuResponse
        {
            TableId = table!.Id,
            TableName = table.Name,
            AreaName = table.Area?.Name ?? string.Empty,
            TableStatus = table.Status,
            Categories = categories.Select(c => new PublicCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                SortOrder = c.SortOrder,
                MenuItems = c.MenuItems
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new PublicMenuItemDto
                    {
                        Id = m.Id,
                        CategoryId = m.CategoryId,
                        CategoryName = c.Name,
                        Name = m.Name,
                        Description = m.Description,
                        Price = m.Price,
                        ImageUrl = m.ImageUrl,
                        IsSoldOut = m.IsSoldOut,
                        IsCombo = m.IsCombo,
                        SortOrder = m.SortOrder
                    }).ToList()
            }).ToList()
        };

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
        _memoryCache.Set(cacheKey, response, cacheOptions);

        return response;
    }

    public async Task<MenuItemDetailDto?> GetMenuItemDetailAsync(int menuItemId, int tableId, string qrToken)
    {
        var validation = await ValidateTableSessionAsync(tableId, qrToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage ?? _localizer["INVALID_QR_TOKEN"]);
        }

        var menuItem = await _context.MenuItems
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.MenuItemModifierGroups)
                .ThenInclude(mmg => mmg.ModifierGroup)
                    .ThenInclude(mg => mg.ModifierOptions.Where(o => o.IsActive))
            .FirstOrDefaultAsync(m => m.Id == menuItemId && m.IsActive && !m.IsDeleted);

        if (menuItem == null) return null;

        var modifierGroups = menuItem.MenuItemModifierGroups
            .OrderBy(mmg => mmg.SortOrder)
            .Select(mmg => new ModifierGroupDto
            {
                Id = mmg.ModifierGroup.Id,
                Name = mmg.ModifierGroup.Name,
                IsRequired = mmg.ModifierGroup.IsRequired,
                MinSelections = mmg.ModifierGroup.MinSelections,
                MaxSelections = mmg.ModifierGroup.MaxSelections,
                SortOrder = mmg.ModifierGroup.SortOrder,
                ModifierOptions = mmg.ModifierGroup.ModifierOptions
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new ModifierOptionDto
                    {
                        Id = o.Id,
                        ModifierGroupId = o.ModifierGroupId,
                        Name = o.Name,
                        ExtraPrice = o.ExtraPrice,
                        IsDefault = o.IsDefault,
                        SortOrder = o.SortOrder,
                        IsActive = o.IsActive
                    }).ToList()
            }).ToList();

        return new MenuItemDetailDto
        {
            Id = menuItem.Id,
            CategoryId = menuItem.CategoryId,
            CategoryName = menuItem.Category?.Name ?? string.Empty,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            ImageUrl = menuItem.ImageUrl,
            IsSoldOut = menuItem.IsSoldOut,
            IsCombo = menuItem.IsCombo,
            SortOrder = menuItem.SortOrder,
            ModifierGroups = modifierGroups
        };
    }

    public async Task<CartValidateResponse> ValidateCartAsync(CartValidateRequest request)
    {
        var response = new CartValidateResponse();

        var validation = await ValidateTableSessionAsync(request.TableId, request.QrToken);
        if (!validation.IsValid)
        {
            response.IsValid = false;
            response.ValidationErrors.Add(validation.ErrorMessage ?? _localizer["INVALID_QR_TOKEN"]);
            return response;
        }

        if (request.Items == null || !request.Items.Any())
        {
            response.IsValid = false;
            response.ValidationErrors.Add(_localizer["CART_EMPTY"]);
            return response;
        }

        decimal totalCartAmount = 0;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                response.ValidationErrors.Add(_localizer["INVALID_QUANTITY"]);
                continue;
            }

            var menuItem = await _context.MenuItems
                .AsNoTracking()
                .Include(m => m.MenuItemModifierGroups)
                    .ThenInclude(mmg => mmg.ModifierGroup)
                        .ThenInclude(mg => mg.ModifierOptions)
                .FirstOrDefaultAsync(m => m.Id == itemReq.MenuItemId && m.IsActive && !m.IsDeleted);

            if (menuItem == null)
            {
                response.ValidationErrors.Add(_localizer["ITEM_NOT_FOUND", itemReq.MenuItemId]);
                continue;
            }

            if (menuItem.IsSoldOut)
            {
                response.ValidationErrors.Add(_localizer["ITEM_SOLD_OUT", menuItem.Name]);
                continue;
            }

            var itemResult = new CartValidatedItemResult
            {
                MenuItemId = menuItem.Id,
                MenuItemName = menuItem.Name,
                Quantity = itemReq.Quantity,
                BasePrice = menuItem.Price,
                Note = itemReq.Note
            };

            // Validate Modifiers
            decimal modifiersPrice = 0;
            var availableGroups = menuItem.MenuItemModifierGroups.Select(mmg => mmg.ModifierGroup).ToList();

            if (itemReq.SelectedModifiers != null && itemReq.SelectedModifiers.Any())
            {
                var selectedOptions = await _context.ModifierOptions
                    .AsNoTracking()
                    .Where(o => itemReq.SelectedModifiers.Contains(o.Id) && o.IsActive)
                    .ToListAsync();

                foreach (var opt in selectedOptions)
                {
                    if (!availableGroups.Any(g => g.Id == opt.ModifierGroupId))
                    {
                        response.ValidationErrors.Add(_localizer["MODIFIER_INVALID", opt.Name, menuItem.Name]);
                    }
                    else
                    {
                        modifiersPrice += opt.ExtraPrice;
                        itemResult.SelectedModifierNames.Add($"{opt.Name} (+{opt.ExtraPrice:N0}đ)");
                    }
                }
            }

            foreach (var group in availableGroups)
            {
                var countSelectedInGroup = itemReq.SelectedModifiers?
                    .Count(modId => group.ModifierOptions.Any(o => o.Id == modId)) ?? 0;

                if (group.IsRequired && countSelectedInGroup < group.MinSelections)
                {
                    response.ValidationErrors.Add(_localizer["MODIFIER_MIN_LIMIT", group.MinSelections, group.Name, menuItem.Name]);
                }

                if (group.MaxSelections > 0 && countSelectedInGroup > group.MaxSelections)
                {
                    response.ValidationErrors.Add(_localizer["MODIFIER_MAX_LIMIT", group.MaxSelections, group.Name, menuItem.Name]);
                }
            }

            itemResult.ModifiersTotalPrice = modifiersPrice;
            itemResult.UnitPrice = menuItem.Price + modifiersPrice;
            itemResult.TotalPrice = itemResult.UnitPrice * itemReq.Quantity;

            totalCartAmount += itemResult.TotalPrice;
            response.Items.Add(itemResult);
        }

        response.SubTotal = totalCartAmount;
        response.IsValid = !response.ValidationErrors.Any();

        return response;
    }

    public async Task<SubmitQrOrderResponse> SubmitQrOrderAsync(SubmitQrOrderRequest request)
    {
        // 1. Rate Limiting Check per TableId using DistributedCache
        var cacheKey = $"QrRateLimit_{request.TableId}";
        var lastSubmitTimeStr = await _distributedCache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(lastSubmitTimeStr) && DateTime.TryParse(lastSubmitTimeStr, out var lastSubmitTime))
        {
            var secondsSinceLast = (DateTime.UtcNow - lastSubmitTime).TotalSeconds;
            if (secondsSinceLast < SystemConstants.QrSubmitRateLimitSeconds)
            {
                var remaining = Math.Ceiling(SystemConstants.QrSubmitRateLimitSeconds - secondsSinceLast);
                throw new InvalidOperationException(_localizer["RATE_LIMIT_WAIT", remaining]);
            }
        }

        // 2. Validate Session
        var validation = await ValidateTableSessionAsync(request.TableId, request.QrToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage ?? _localizer["INVALID_QR_TOKEN"]);
        }

        // 3. Validate Cart Payload
        var cartRequest = new CartValidateRequest
        {
            TableId = request.TableId,
            QrToken = request.QrToken,
            Items = request.Items
        };
        var cartValidation = await ValidateCartAsync(cartRequest);
        if (!cartValidation.IsValid)
        {
            throw new ArgumentException(string.Join(" | ", cartValidation.ValidationErrors));
        }

        // 4. DB Transaction with Concurrency & Isolation
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == request.TableId);
            if (table == null || table.Status == (int)TableStatus.Billing)
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
                    OrderType = 0, // DineIn
                    TableId = request.TableId,
                    ShiftId = shiftId,
                    CreatedByUserId = createdUserId,
                    Status = (int)OrderStatus.Draft,
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
            foreach (var itemRes in cartValidation.Items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = targetOrder.Id,
                    MenuItemId = itemRes.MenuItemId,
                    MenuItemName = itemRes.MenuItemName,
                    UnitPrice = itemRes.UnitPrice,
                    Quantity = itemRes.Quantity,
                    Note = itemRes.Note,
                    KitchenStatus = (int)KitchenStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                var itemReq = request.Items.FirstOrDefault(i => i.MenuItemId == itemRes.MenuItemId);
                if (itemReq?.SelectedModifiers != null && itemReq.SelectedModifiers.Any())
                {
                    var options = await _context.ModifierOptions
                        .Where(o => itemReq.SelectedModifiers.Contains(o.Id))
                        .ToListAsync();

                    foreach (var opt in options)
                    {
                        _context.OrderItemModifiers.Add(new OrderItemModifier
                        {
                            OrderItemId = orderItem.Id,
                            ModifierOptionId = opt.Id,
                            ModifierOptionName = opt.Name,
                            ExtraPrice = opt.ExtraPrice
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                addedAmount += itemRes.TotalPrice;
            }

            targetOrder.SubTotal += addedAmount;
            targetOrder.TotalAmount += addedAmount;

            if (table.Status == (int)TableStatus.Available)
            {
                table.Status = (int)TableStatus.NewOrder;
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(SystemConstants.QrSubmitRateLimitSeconds));
            await _distributedCache.SetStringAsync(cacheKey, DateTime.UtcNow.ToString("O"), cacheOptions);

            await _notificationService.NotifyNewQrOrderAsync(
                targetOrder.Id,
                table.Id,
                table.Name,
                targetOrder.OrderCode,
                targetOrder.TotalAmount
            );

            return new SubmitQrOrderResponse
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

    public async Task<QrOrderStatusResponse?> GetOrderStatusForQrAsync(int tableId, string qrToken)
    {
        var validation = await ValidateTableSessionAsync(tableId, qrToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage ?? _localizer["INVALID_QR_TOKEN"]);
        }

        var table = await _context.Tables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tableId);

        var activeOrder = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemModifiers)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(o => o.TableId == tableId && o.Status != (int)OrderStatus.Cancelled);

        if (activeOrder == null) return null;

        var itemsDto = activeOrder.OrderItems.Select(oi => new QrOrderItemStatusDto
        {
            OrderItemId = oi.Id,
            MenuItemId = oi.MenuItemId,
            MenuItemName = oi.MenuItemName,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            TotalPrice = oi.UnitPrice * oi.Quantity,
            Note = oi.Note,
            KitchenStatus = oi.KitchenStatus,
            KitchenStatusName = Enum.GetName(typeof(KitchenStatus), oi.KitchenStatus) ?? "Unknown",
            Modifiers = oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()
        }).ToList();

        return new QrOrderStatusResponse
        {
            OrderId = activeOrder.Id,
            OrderCode = activeOrder.OrderCode,
            TableId = table!.Id,
            TableName = table.Name,
            OrderStatus = activeOrder.Status,
            OrderStatusName = Enum.GetName(typeof(OrderStatus), activeOrder.Status) ?? "Unknown",
            SubTotal = activeOrder.SubTotal,
            TotalAmount = activeOrder.TotalAmount,
            CreatedAt = activeOrder.CreatedAt,
            Items = itemsDto
        };
    }
}
