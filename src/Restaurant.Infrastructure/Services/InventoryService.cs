using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Domain.Enums;
using Restaurant.Domain.Common;

namespace Restaurant.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IRestaurantNotificationService _notificationService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public InventoryService(
        ApplicationDbContext context,
        IRestaurantNotificationService notificationService,
        IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _notificationService = notificationService;
        _localizer = localizer;
    }

    public async Task<List<MaterialDto>> GetAllMaterialsAsync()
    {
        var materials = await _context.Materials
            .AsNoTracking()
            .Include(m => m.UnitOfMeasure)
            .Where(m => m.IsActive && !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return materials.Select(m => new MaterialDto
        {
            Id = m.Id,
            UnitOfMeasureId = m.UnitOfMeasureId,
            UnitName = m.UnitOfMeasure?.Name ?? string.Empty,
            Name = m.Name,
            NameEn = m.NameEn,
            CurrentStock = m.CurrentStock,
            MinStockLevel = m.MinStockLevel,
            CostPerUnit = m.CostPerUnit,
            IsActive = m.IsActive
        }).ToList();
    }

    public async Task<MaterialDto> CreateMaterialAsync(CreateMaterialRequest request)
    {
        var material = new Material
        {
            UnitOfMeasureId = request.UnitOfMeasureId,
            Name = request.Name,
            NameEn = request.NameEn?.Trim(),
            CurrentStock = 0,
            MinStockLevel = request.MinStockLevel,
            CostPerUnit = request.CostPerUnit,
            IsActive = true
        };

        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        var unit = await _context.UnitOfMeasures.FindAsync(request.UnitOfMeasureId);
        return new MaterialDto
        {
            Id = material.Id,
            UnitOfMeasureId = material.UnitOfMeasureId,
            UnitName = unit?.Name ?? string.Empty,
            Name = material.Name,
            NameEn = material.NameEn,
            CurrentStock = material.CurrentStock,
            MinStockLevel = material.MinStockLevel,
            CostPerUnit = material.CostPerUnit,
            IsActive = material.IsActive
        };
    }

    public async Task<MaterialDto?> UpdateMaterialAsync(int id, CreateMaterialRequest request)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return null;

        material.Name = request.Name;
        material.NameEn = request.NameEn?.Trim();
        material.UnitOfMeasureId = request.UnitOfMeasureId;
        material.MinStockLevel = request.MinStockLevel;
        material.CostPerUnit = request.CostPerUnit;

        await _context.SaveChangesAsync();

        var unit = await _context.UnitOfMeasures.FindAsync(request.UnitOfMeasureId);
        return new MaterialDto
        {
            Id = material.Id,
            UnitOfMeasureId = material.UnitOfMeasureId,
            UnitName = unit?.Name ?? string.Empty,
            Name = material.Name,
            NameEn = material.NameEn,
            CurrentStock = material.CurrentStock,
            MinStockLevel = material.MinStockLevel,
            CostPerUnit = material.CostPerUnit,
            IsActive = material.IsActive
        };
    }

    public async Task<bool> DeleteMaterialAsync(int id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return false;

        material.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CreatePurchaseOrderAsync(int userId, CreatePurchaseOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any()) return false;

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal totalAmount = request.Items.Sum(i => i.Quantity * i.UnitCost);

            var goodsReceipt = new GoodsReceipt
            {
                SupplierId = request.SupplierId,
                ReceivedByUserId = userId,
                TotalAmount = totalAmount,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            _context.GoodsReceipts.Add(goodsReceipt);
            await _context.SaveChangesAsync();

            foreach (var item in request.Items)
            {
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == item.MaterialId);
                if (material == null) continue;

                var receiptItem = new GoodsReceiptItem
                {
                    GoodsReceiptId = goodsReceipt.Id,
                    MaterialId = item.MaterialId,
                    UnitOfMeasureId = material.UnitOfMeasureId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.Quantity * item.UnitCost
                };
                _context.GoodsReceiptItems.Add(receiptItem);

                // Weighted Average Cost calculation:
                // NewCost = (OldStock * OldCost + NewQty * NewCost) / (OldStock + NewQty)
                var oldStock = material.CurrentStock > 0 ? material.CurrentStock : 0;
                var newStock = oldStock + item.Quantity;
                var totalCostVal = (oldStock * material.CostPerUnit) + (item.Quantity * item.UnitCost);

                material.CostPerUnit = newStock > 0 ? (totalCostVal / newStock) : item.UnitCost;
                material.CurrentStock += item.Quantity;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = material.Id,
                    TransactionType = (int)InventoryTransactionType.Import,
                    Quantity = item.Quantity,
                    BalanceAfter = material.CurrentStock,
                    ReferenceType = SystemConstants.ReferenceTypes.GoodsReceipt,
                    ReferenceId = goodsReceipt.Id,
                    Note = $"Imported from Supplier ID {request.SupplierId}",
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AuditStockAsync(int userId, StockAuditRequest request)
    {
        if (request.Items == null || !request.Items.Any()) return false;

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var stocktake = new Stocktake
            {
                ConductedByUserId = userId,
                Status = (int)StocktakeStatus.Completed,
                Note = request.Note,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _context.Stocktakes.Add(stocktake);
            await _context.SaveChangesAsync();

            foreach (var item in request.Items)
            {
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == item.MaterialId);
                if (material == null) continue;

                var systemQty = material.CurrentStock;
                var diff = item.ActualStock - systemQty;
                var diffVal = diff * material.CostPerUnit;

                _context.StocktakeItems.Add(new StocktakeItem
                {
                    StocktakeId = stocktake.Id,
                    MaterialId = material.Id,
                    SystemQuantity = systemQty,
                    ActualQuantity = item.ActualStock,
                    Difference = diff,
                    DifferenceValue = diffVal,
                    Note = item.Reason
                });

                material.CurrentStock = item.ActualStock;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = material.Id,
                    TransactionType = (int)InventoryTransactionType.AuditAdjustment,
                    Quantity = diff,
                    BalanceAfter = material.CurrentStock,
                    ReferenceType = SystemConstants.ReferenceTypes.Stocktake,
                    ReferenceId = stocktake.Id,
                    Note = item.Reason ?? "Stocktake adjustment",
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SetMenuItemRecipeAsync(SetMenuItemRecipeRequest request)
    {
        var menuItem = await _context.MenuItems.FindAsync(request.MenuItemId);
        if (menuItem == null) return false;

        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.MenuItemId == request.MenuItemId);

        if (existingRecipe == null)
        {
            existingRecipe = new Recipe
            {
                MenuItemId = request.MenuItemId
            };
            _context.Recipes.Add(existingRecipe);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.RecipeItems.RemoveRange(existingRecipe.RecipeItems);
            await _context.SaveChangesAsync();
        }

        foreach (var item in request.Items)
        {
            _context.RecipeItems.Add(new RecipeItem
            {
                RecipeId = existingRecipe.Id,
                MaterialId = item.MaterialId,
                UnitOfMeasureId = item.UnitOfMeasureId,
                Quantity = item.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetModifierOptionRecipeAsync(SetModifierOptionRecipeRequest request)
    {
        var option = await _context.ModifierOptions.FindAsync(request.ModifierOptionId);
        if (option == null) return false;

        var existingRecipe = await _context.ModifierRecipes
            .Include(r => r.ModifierRecipeItems)
            .FirstOrDefaultAsync(r => r.ModifierOptionId == request.ModifierOptionId);

        if (existingRecipe == null)
        {
            existingRecipe = new ModifierRecipe
            {
                ModifierOptionId = request.ModifierOptionId
            };
            _context.ModifierRecipes.Add(existingRecipe);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ModifierRecipeItems.RemoveRange(existingRecipe.ModifierRecipeItems);
            await _context.SaveChangesAsync();
        }

        foreach (var item in request.Items)
        {
            _context.ModifierRecipeItems.Add(new ModifierRecipeItem
            {
                ModifierRecipeId = existingRecipe.Id,
                MaterialId = item.MaterialId,
                UnitOfMeasureId = item.UnitOfMeasureId,
                Quantity = item.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MenuItemRecipeDto?> GetMenuItemRecipeAsync(int menuItemId)
    {
        var menuItem = await _context.MenuItems.FindAsync(menuItemId);
        if (menuItem == null) return null;

        var recipe = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeItems)
                .ThenInclude(ri => ri.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .FirstOrDefaultAsync(r => r.MenuItemId == menuItemId);

        var dto = new MenuItemRecipeDto
        {
            MenuItemId = menuItem.Id,
            MenuItemName = menuItem.Name,
            Items = recipe?.RecipeItems.Select(ri => new RecipeItemDto
            {
                MaterialId = ri.MaterialId,
                MaterialName = ri.Material?.Name ?? "Nguyên liệu",
                MaterialNameEn = ri.Material?.NameEn,
                UnitOfMeasureId = ri.UnitOfMeasureId,
                UnitName = ri.Material?.UnitOfMeasure?.Name ?? "Kg",
                Quantity = ri.Quantity,
                CostPerUnit = ri.Material?.CostPerUnit ?? 0
            }).ToList() ?? new List<RecipeItemDto>()
        };

        return dto;
    }

    public async Task<ModifierOptionRecipeDto?> GetModifierOptionRecipeAsync(int modifierOptionId)
    {
        var option = await _context.ModifierOptions.FindAsync(modifierOptionId);
        if (option == null) return null;

        var recipe = await _context.ModifierRecipes
            .AsNoTracking()
            .Include(r => r.ModifierRecipeItems)
                .ThenInclude(ri => ri.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .FirstOrDefaultAsync(r => r.ModifierOptionId == modifierOptionId);

        var dto = new ModifierOptionRecipeDto
        {
            ModifierOptionId = option.Id,
            ModifierOptionName = option.Name,
            Items = recipe?.ModifierRecipeItems.Select(ri => new RecipeItemDto
            {
                MaterialId = ri.MaterialId,
                MaterialName = ri.Material?.Name ?? "Nguyên liệu",
                MaterialNameEn = ri.Material?.NameEn,
                UnitOfMeasureId = ri.UnitOfMeasureId,
                UnitName = ri.Material?.UnitOfMeasure?.Name ?? "Kg",
                Quantity = ri.Quantity,
                CostPerUnit = ri.Material?.CostPerUnit ?? 0
            }).ToList() ?? new List<RecipeItemDto>()
        };

        return dto;
    }

    public async Task<List<GoodsReceiptDto>> GetAllGoodsReceiptsAsync()
    {
        var receipts = await _context.GoodsReceipts
            .AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.ReceivedByUser)
            .Include(g => g.GoodsReceiptItems)
                .ThenInclude(gi => gi.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return receipts.Select(g => new GoodsReceiptDto
        {
            Id = g.Id,
            SupplierId = g.SupplierId,
            SupplierName = g.Supplier?.Name ?? "Nhà cung cấp",
            ReceivedByUserId = g.ReceivedByUserId,
            ReceivedByUserName = g.ReceivedByUser?.FullName ?? "Thủ kho",
            TotalAmount = g.TotalAmount,
            Note = g.Note,
            CreatedAt = g.CreatedAt,
            Items = g.GoodsReceiptItems.Select(gi => new GoodsReceiptItemDto
            {
                Id = gi.Id,
                MaterialId = gi.MaterialId,
                MaterialName = gi.Material?.Name ?? "",
                UnitName = gi.Material?.UnitOfMeasure?.Name ?? "Kg",
                Quantity = gi.Quantity,
                UnitCost = gi.UnitCost,
                TotalCost = gi.TotalCost
            }).ToList()
        }).ToList();
    }

    public async Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(int id)
    {
        var g = await _context.GoodsReceipts
            .AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.ReceivedByUser)
            .Include(g => g.GoodsReceiptItems)
                .ThenInclude(gi => gi.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (g == null) return null;

        return new GoodsReceiptDto
        {
            Id = g.Id,
            SupplierId = g.SupplierId,
            SupplierName = g.Supplier?.Name ?? "Nhà cung cấp",
            ReceivedByUserId = g.ReceivedByUserId,
            ReceivedByUserName = g.ReceivedByUser?.FullName ?? "Thủ kho",
            TotalAmount = g.TotalAmount,
            Note = g.Note,
            CreatedAt = g.CreatedAt,
            Items = g.GoodsReceiptItems.Select(gi => new GoodsReceiptItemDto
            {
                Id = gi.Id,
                MaterialId = gi.MaterialId,
                MaterialName = gi.Material?.Name ?? "",
                UnitName = gi.Material?.UnitOfMeasure?.Name ?? "Kg",
                Quantity = gi.Quantity,
                UnitCost = gi.UnitCost,
                TotalCost = gi.TotalCost
            }).ToList()
        };
    }

    public async Task<List<StocktakeDto>> GetAllStocktakesAsync()
    {
        var stocktakes = await _context.Stocktakes
            .AsNoTracking()
            .Include(s => s.ConductedByUser)
            .Include(s => s.StocktakeItems)
                .ThenInclude(si => si.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        return stocktakes.Select(s => new StocktakeDto
        {
            Id = s.Id,
            ConductedByUserId = s.ConductedByUserId,
            ConductedByUserName = s.ConductedByUser?.FullName ?? "Thủ kho",
            Status = s.Status,
            Note = s.Note,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            Items = s.StocktakeItems.Select(si => new StocktakeItemDto
            {
                Id = si.Id,
                MaterialId = si.MaterialId,
                MaterialName = si.Material?.Name ?? "",
                UnitName = si.Material?.UnitOfMeasure?.Name ?? "Kg",
                SystemQuantity = si.SystemQuantity,
                ActualQuantity = si.ActualQuantity,
                Difference = si.Difference,
                DifferenceValue = si.DifferenceValue,
                Note = si.Note
            }).ToList()
        }).ToList();
    }

    public async Task<StocktakeDto?> GetStocktakeByIdAsync(int id)
    {
        var s = await _context.Stocktakes
            .AsNoTracking()
            .Include(s => s.ConductedByUser)
            .Include(s => s.StocktakeItems)
                .ThenInclude(si => si.Material)
                    .ThenInclude(m => m.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null) return null;

        return new StocktakeDto
        {
            Id = s.Id,
            ConductedByUserId = s.ConductedByUserId,
            ConductedByUserName = s.ConductedByUser?.FullName ?? "Thủ kho",
            Status = s.Status,
            Note = s.Note,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            Items = s.StocktakeItems.Select(si => new StocktakeItemDto
            {
                Id = si.Id,
                MaterialId = si.MaterialId,
                MaterialName = si.Material?.Name ?? "",
                UnitName = si.Material?.UnitOfMeasure?.Name ?? "Kg",
                SystemQuantity = si.SystemQuantity,
                ActualQuantity = si.ActualQuantity,
                Difference = si.Difference,
                DifferenceValue = si.DifferenceValue,
                Note = si.Note
            }).ToList()
        };
    }

    public async Task<StocktakeDto> CreateBlindStocktakeAsync(int userId, CreateBlindStocktakeRequest request)
    {
        var stocktake = new Stocktake
        {
            ConductedByUserId = userId,
            Status = (int)StocktakeStatus.PendingReconciliation,
            Note = request.Note,
            StartedAt = DateTime.UtcNow,
            CompletedAt = null
        };

        _context.Stocktakes.Add(stocktake);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var material = await _context.Materials
                .Include(m => m.UnitOfMeasure)
                .FirstOrDefaultAsync(m => m.Id == item.MaterialId);
            if (material == null) continue;

            var systemQty = material.CurrentStock;
            var diff = item.ActualQuantity - systemQty;
            var diffVal = diff * material.CostPerUnit;

            _context.StocktakeItems.Add(new StocktakeItem
            {
                StocktakeId = stocktake.Id,
                MaterialId = material.Id,
                SystemQuantity = systemQty,
                ActualQuantity = item.ActualQuantity,
                Difference = diff,
                DifferenceValue = diffVal,
                Note = item.Note
            });
        }

        await _context.SaveChangesAsync();
        return (await GetStocktakeByIdAsync(stocktake.Id))!;
    }

    public async Task<bool> ReconcileStocktakeAsync(int userId, ReconcileStocktakeRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var stocktake = await _context.Stocktakes
                .Include(s => s.StocktakeItems)
                .FirstOrDefaultAsync(s => s.Id == request.StocktakeId);

            if (stocktake == null) return false;

            stocktake.Status = (int)StocktakeStatus.Completed;
            stocktake.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                stocktake.Note = string.IsNullOrWhiteSpace(stocktake.Note) 
                    ? request.Note 
                    : $"{stocktake.Note} | Quản lý: {request.Note}";
            }

            foreach (var item in request.Items)
            {
                var stocktakeItem = stocktake.StocktakeItems.FirstOrDefault(si => si.MaterialId == item.MaterialId);
                var material = await _context.Materials.FindAsync(item.MaterialId);
                if (material == null) continue;

                var systemQty = stocktakeItem?.SystemQuantity ?? material.CurrentStock;
                var finalActual = item.ActualQuantity;
                var diff = finalActual - systemQty;
                var diffVal = diff * material.CostPerUnit;

                if (stocktakeItem != null)
                {
                    stocktakeItem.ActualQuantity = finalActual;
                    stocktakeItem.Difference = diff;
                    stocktakeItem.DifferenceValue = diffVal;
                    if (!string.IsNullOrWhiteSpace(item.Reason))
                    {
                        stocktakeItem.Note = item.Reason;
                    }
                }

                material.CurrentStock = finalActual;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = material.Id,
                    TransactionType = (int)InventoryTransactionType.AuditAdjustment,
                    Quantity = diff,
                    BalanceAfter = material.CurrentStock,
                    ReferenceType = SystemConstants.ReferenceTypes.Stocktake,
                    ReferenceId = stocktake.Id,
                    Note = string.IsNullOrWhiteSpace(item.Reason) ? "Cân đối kiểm kê kho" : item.Reason,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<MaterialDto>> GetMaterialsBySupplierIdAsync(int supplierId)
    {
        var supplier = await _context.Suppliers.FindAsync(supplierId);
        var allMaterials = await GetAllMaterialsAsync();
        if (supplier == null) return allMaterials;

        var name = supplier.Name.ToLowerInvariant();
        List<MaterialDto> filtered;

        if (name.Contains("ý việt") || name.Contains("thực phẩm ý") || name.Contains("bột mì"))
        {
            filtered = allMaterials.Where(m => 
                m.Name.Contains("Bột") || m.Name.Contains("Phô mai") || m.Name.Contains("Sốt") || 
                m.Name.Contains("Men") || m.Name.Contains("Olive") || m.Name.Contains("Mì") || 
                m.Name.Contains("Lasagna") || m.Name.Contains("Đế Pizza") || m.Name.Contains("Đường") || 
                m.Name.Contains("Muối")).ToList();
        }
        else if (name.Contains("đà lạt") || name.Contains("nông sản") || name.Contains("rau"))
        {
            filtered = allMaterials.Where(m => 
                m.Name.Contains("Rau") || m.Name.Contains("Nấm") || m.Name.Contains("Hành") || 
                m.Name.Contains("Ớt") || m.Name.Contains("Cà") || m.Name.Contains("Bắp") || 
                m.Name.Contains("Dứa") || m.Name.Contains("Oliu") || m.Name.Contains("Bông cải") || 
                m.Name.Contains("Xà lách") || m.Name.Contains("Dưa leo") || m.Name.Contains("Basil")).ToList();
        }
        else if (name.Contains("meat") || name.Contains("thịt"))
        {
            filtered = allMaterials.Where(m => 
                m.Name.Contains("Thịt") || m.Name.Contains("Xúc xích") || m.Name.Contains("Gà") || 
                m.Name.Contains("Bacon") || m.Name.Contains("Ham") || m.Name.Contains("Bò") || 
                m.Name.Contains("Heo")).ToList();
        }
        else if (name.Contains("biển đông") || name.Contains("hải sản"))
        {
            filtered = allMaterials.Where(m => 
                m.Name.Contains("Tôm") || m.Name.Contains("Mực") || m.Name.Contains("Nghêu") || 
                m.Name.Contains("Cua") || m.Name.Contains("Cá") || m.Name.Contains("Sò")).ToList();
        }
        else if (name.Contains("tân nam") || name.Contains("đồ uống") || name.Contains("pha chế"))
        {
            filtered = allMaterials.Where(m => 
                m.Name.Contains("Lon") || m.Name.Contains("Pepsi") || m.Name.Contains("7Up") || 
                m.Name.Contains("Mirinda") || m.Name.Contains("Trà") || m.Name.Contains("Cà phê") || 
                m.Name.Contains("Nước") || m.Name.Contains("Đào") || m.Name.Contains("Vải")).ToList();
        }
        else
        {
            filtered = allMaterials;
        }

        return filtered.Any() ? filtered : allMaterials;
    }

    public async Task<InventoryLossSummaryDto> GetInventoryLossSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var end = toDate ?? DateTime.UtcNow;

        var items = await _context.StocktakeItems
            .AsNoTracking()
            .Include(si => si.Material)
                .ThenInclude(m => m.UnitOfMeasure)
            .Include(si => si.Stocktake)
            .Where(si => si.Stocktake.StartedAt >= start && si.Stocktake.StartedAt <= end && si.Difference < 0)
            .ToListAsync();

        var discrepancyList = items.Select(si => new StocktakeItemDto
        {
            Id = si.Id,
            MaterialId = si.MaterialId,
            MaterialName = si.Material?.Name ?? "",
            UnitName = si.Material?.UnitOfMeasure?.Name ?? "Kg",
            SystemQuantity = si.SystemQuantity,
            ActualQuantity = si.ActualQuantity,
            Difference = si.Difference,
            DifferenceValue = si.DifferenceValue,
            Note = si.Note ?? si.Stocktake.Note
        }).ToList();

        var totalLoss = Math.Abs(items.Sum(si => si.DifferenceValue));

        return new InventoryLossSummaryDto
        {
            TotalVarianceAmount = totalLoss,
            TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0),
            DiscrepancyCount = items.Count,
            DiscrepancyItems = discrepancyList
        };
    }

    public async Task DeductInventoryForOrderItemAsync(int orderItemId)
    {
        // 1. Check if already deducted
        var alreadyDeducted = await _context.InventoryTransactions
            .AnyAsync(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId == orderItemId && t.TransactionType == (int)InventoryTransactionType.SaleDeduction);
            
        if (alreadyDeducted) return;

        var orderItem = await _context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.OrderItemModifiers)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

        if (orderItem == null) return;

        // 2. Load Recipes
        var recipe = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeItems)
            .FirstOrDefaultAsync(r => r.MenuItemId == orderItem.MenuItemId);

        var modifierOptionIds = orderItem.OrderItemModifiers.Select(m => m.ModifierOptionId).ToList();
        var modifierRecipes = await _context.ModifierRecipes
            .AsNoTracking()
            .Include(r => r.ModifierRecipeItems)
            .Where(r => modifierOptionIds.Contains(r.ModifierOptionId))
            .ToListAsync();

        // Map material -> total quantity to deduct
        var materialDeductions = new Dictionary<int, decimal>();

        if (recipe != null)
        {
            foreach (var ri in recipe.RecipeItems)
            {
                var needed = ri.Quantity * orderItem.Quantity;
                if (materialDeductions.ContainsKey(ri.MaterialId))
                    materialDeductions[ri.MaterialId] += needed;
                else
                    materialDeductions[ri.MaterialId] = needed;
            }
        }

        foreach (var mod in orderItem.OrderItemModifiers)
        {
            var modRecipe = modifierRecipes.FirstOrDefault(r => r.ModifierOptionId == mod.ModifierOptionId);
            if (modRecipe != null)
            {
                foreach (var mri in modRecipe.ModifierRecipeItems)
                {
                    var needed = mri.Quantity * orderItem.Quantity;
                    if (materialDeductions.ContainsKey(mri.MaterialId))
                        materialDeductions[mri.MaterialId] += needed;
                    else
                        materialDeductions[mri.MaterialId] = needed;
                }
            }
        }

        if (!materialDeductions.Any()) return;

        // 3. Batch Load all target Materials
        var targetMaterialIds = materialDeductions.Keys.ToList();
        var materials = await _context.Materials
            .Where(m => targetMaterialIds.Contains(m.Id))
            .ToListAsync();

        var lowStockWarnings = new List<(int MaterialId, string Name, decimal Stock, decimal Min)>();

        foreach (var material in materials)
        {
            if (!materialDeductions.TryGetValue(material.Id, out var deductQty)) continue;

            material.CurrentStock -= deductQty;

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = material.Id,
                TransactionType = (int)InventoryTransactionType.SaleDeduction,
                Quantity = -deductQty,
                BalanceAfter = material.CurrentStock,
                ReferenceType = SystemConstants.ReferenceTypes.OrderItem,
                ReferenceId = orderItem.Id,
                Note = $"BOM Deduction for OrderItem {orderItem.Id} (Order Code {orderItem.Order?.OrderCode})",
                CreatedByUserId = orderItem.Order != null ? orderItem.Order.CreatedByUserId : 0,
                CreatedAt = DateTime.UtcNow
            });

            if (material.CurrentStock <= material.MinStockLevel)
            {
                lowStockWarnings.Add((material.Id, material.Name, material.CurrentStock, material.MinStockLevel));
            }
        }

        await _context.SaveChangesAsync();

        foreach (var w in lowStockWarnings)
        {
            await _notificationService.NotifyLowStockWarningAsync(w.MaterialId, w.Name, w.Stock, w.Min);
        }
    }

    // Supplier Management
    public async Task<List<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return suppliers.Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            PhoneNumber = s.PhoneNumber,
            Address = s.Address,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return null;

        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            PhoneNumber = supplier.PhoneNumber,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            PhoneNumber = supplier.PhoneNumber,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int id, CreateSupplierRequest request)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return null;

        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.PhoneNumber = request.PhoneNumber;
        supplier.Address = request.Address;

        await _context.SaveChangesAsync();

        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            PhoneNumber = supplier.PhoneNumber,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;

        supplier.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    // Unit Conversion Management
    public async Task<List<UnitConversionDto>> GetAllUnitConversionsAsync()
    {
        var conversions = await _context.UnitConversions
            .AsNoTracking()
            .Include(uc => uc.FromUnit)
            .Include(uc => uc.ToUnit)
            .ToListAsync();

        return conversions.Select(uc => new UnitConversionDto
        {
            Id = uc.Id,
            FromUnitId = uc.FromUnitId,
            FromUnitName = uc.FromUnit?.Name ?? string.Empty,
            ToUnitId = uc.ToUnitId,
            ToUnitName = uc.ToUnit?.Name ?? string.Empty,
            ConversionFactor = uc.ConversionFactor
        }).ToList();
    }

    public async Task<UnitConversionDto> CreateUnitConversionAsync(CreateUnitConversionRequest request)
    {
        var conversion = new UnitConversion
        {
            FromUnitId = request.FromUnitId,
            ToUnitId = request.ToUnitId,
            ConversionFactor = request.ConversionFactor,
            CreatedAt = DateTime.UtcNow
        };

        _context.UnitConversions.Add(conversion);
        await _context.SaveChangesAsync();

        var fromUnit = await _context.UnitOfMeasures.FindAsync(request.FromUnitId);
        var toUnit = await _context.UnitOfMeasures.FindAsync(request.ToUnitId);

        return new UnitConversionDto
        {
            Id = conversion.Id,
            FromUnitId = conversion.FromUnitId,
            FromUnitName = fromUnit?.Name ?? string.Empty,
            ToUnitId = conversion.ToUnitId,
            ToUnitName = toUnit?.Name ?? string.Empty,
            ConversionFactor = conversion.ConversionFactor
        };
    }
}
