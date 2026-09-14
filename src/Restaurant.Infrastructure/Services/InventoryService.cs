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
            CurrentStock = material.CurrentStock,
            MinStockLevel = material.MinStockLevel,
            CostPerUnit = material.CostPerUnit,
            IsActive = material.IsActive
        };
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
                CreatedByUserId = orderItem.Order?.CreatedByUserId ?? 1,
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
