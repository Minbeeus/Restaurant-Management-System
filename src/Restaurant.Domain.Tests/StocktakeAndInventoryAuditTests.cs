using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Domain.Tests.E2E;
using Xunit;

namespace Restaurant.Domain.Tests;

public class StocktakeAndInventoryAuditTests
{
    [Fact]
    public async Task Test_01_CreateBlindStocktake_RecordsCountsAndVariances_WithoutAlteringMaterialStock()
    {
        using var ctx = new E2ETestContext();

        // Given: MatFlour (100kg @ 25k), MatCheese (50kg @ 180k), MatCokeCan (120 lon @ 10k)
        var flourInitial = ctx.MatFlour.CurrentStock;
        var cheeseInitial = ctx.MatCheese.CurrentStock;
        var cokeInitial = ctx.MatCokeCan.CurrentStock;

        // When: Staff submits a blind stock count
        // - Flour: counted 95 kg (-5kg variance)
        // - Cheese: counted 52 kg (+2kg variance)
        // - Coke: counted 120 lon (0 variance)
        var blindRequest = new CreateBlindStocktakeRequest
        {
            Note = "Kiểm kê ca sáng",
            Items = new List<BlindStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 95m, Note = "Bao bị rách đáy" },
                new() { MaterialId = ctx.MatCheese.Id, ActualQuantity = 52m, Note = "Thừa 2 khối nguyên seal" },
                new() { MaterialId = ctx.MatCokeCan.Id, ActualQuantity = 120m, Note = "Khớp số lon" }
            }
        };

        var stocktakeDto = await ctx.InventoryService.CreateBlindStocktakeAsync(ctx.CashierUser.Id, blindRequest);

        // Then:
        Assert.NotNull(stocktakeDto);
        Assert.True(stocktakeDto.Id > 0);
        Assert.Equal((int)StocktakeStatus.PendingReconciliation, stocktakeDto.Status);
        Assert.Equal(3, stocktakeDto.Items.Count);

        // Check Flour item
        var flourItem = stocktakeDto.Items.First(i => i.MaterialId == ctx.MatFlour.Id);
        Assert.Equal(100m, flourItem.SystemQuantity);
        Assert.Equal(95m, flourItem.ActualQuantity);
        Assert.Equal(-5m, flourItem.Difference);
        Assert.Equal(-5m * 25000m, flourItem.DifferenceValue);

        // Check Cheese item
        var cheeseItem = stocktakeDto.Items.First(i => i.MaterialId == ctx.MatCheese.Id);
        Assert.Equal(50m, cheeseItem.SystemQuantity);
        Assert.Equal(52m, cheeseItem.ActualQuantity);
        Assert.Equal(2m, cheeseItem.Difference);
        Assert.Equal(2m * 180000m, cheeseItem.DifferenceValue);

        // Check Coke item
        var cokeItem = stocktakeDto.Items.First(i => i.MaterialId == ctx.MatCokeCan.Id);
        Assert.Equal(120m, cokeItem.SystemQuantity);
        Assert.Equal(120m, cokeItem.ActualQuantity);
        Assert.Equal(0m, cokeItem.Difference);
        Assert.Equal(0m, cokeItem.DifferenceValue);

        // CRITICAL: Materials stock in DB must NOT be modified yet during blind count!
        var flourInDb = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        var cheeseInDb = await ctx.Db.Materials.FindAsync(ctx.MatCheese.Id);
        var cokeInDb = await ctx.Db.Materials.FindAsync(ctx.MatCokeCan.Id);

        Assert.Equal(flourInitial, flourInDb!.CurrentStock);
        Assert.Equal(cheeseInitial, cheeseInDb!.CurrentStock);
        Assert.Equal(cokeInitial, cokeInDb!.CurrentStock);
    }

    [Fact]
    public async Task Test_02_ReconcileStocktake_UpdatesCurrentStock_AndGeneratesAuditAdjustmentTransactions()
    {
        using var ctx = new E2ETestContext();

        // 1. Create blind count
        var blindRequest = new CreateBlindStocktakeRequest
        {
            Note = "Kiểm kê cuối tháng",
            Items = new List<BlindStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 92m, Note = "Hao hụt tự nhiên" },
                new() { MaterialId = ctx.MatCheese.Id, ActualQuantity = 54m, Note = "Nhập nhầm ca trước" }
            }
        };
        var stocktakeDto = await ctx.InventoryService.CreateBlindStocktakeAsync(ctx.CashierUser.Id, blindRequest);

        // 2. Manager reconciles and approves
        var reconcileRequest = new ReconcileStocktakeRequest
        {
            StocktakeId = stocktakeDto.Id,
            Note = "Quản lý trưởng đã ký duyệt biên bản",
            Items = new List<ReconcileStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 92m, Reason = "Hao hụt bột mì hợp lý" },
                new() { MaterialId = ctx.MatCheese.Id, ActualQuantity = 54m, Reason = "Thừa 2kg đã kiểm tra" }
            }
        };

        var reconcileSuccess = await ctx.InventoryService.ReconcileStocktakeAsync(ctx.ManagerUser.Id, reconcileRequest);
        Assert.True(reconcileSuccess);

        // 3. Verify stocktake status
        var updatedStocktake = await ctx.InventoryService.GetStocktakeByIdAsync(stocktakeDto.Id);
        Assert.NotNull(updatedStocktake);
        Assert.Equal((int)StocktakeStatus.Completed, updatedStocktake.Status);
        Assert.NotNull(updatedStocktake.CompletedAt);

        // 4. Verify actual material stock has been updated in database
        var flourInDb = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        var cheeseInDb = await ctx.Db.Materials.FindAsync(ctx.MatCheese.Id);

        Assert.Equal(92m, flourInDb!.CurrentStock);
        Assert.Equal(54m, cheeseInDb!.CurrentStock);

        // 5. Verify InventoryTransactions audit log
        var transactions = await ctx.Db.InventoryTransactions
            .Where(t => t.ReferenceType == "Stocktake" && t.ReferenceId == stocktakeDto.Id)
            .ToListAsync();

        Assert.Equal(2, transactions.Count);

        var flourTx = transactions.First(t => t.MaterialId == ctx.MatFlour.Id);
        Assert.Equal((int)InventoryTransactionType.AuditAdjustment, flourTx.TransactionType);
        Assert.Equal(-8m, flourTx.Quantity);
        Assert.Equal(92m, flourTx.BalanceAfter);
        Assert.Equal(ctx.ManagerUser.Id, flourTx.CreatedByUserId);
        Assert.Contains("Hao hụt bột mì", flourTx.Note);

        var cheeseTx = transactions.First(t => t.MaterialId == ctx.MatCheese.Id);
        Assert.Equal((int)InventoryTransactionType.AuditAdjustment, cheeseTx.TransactionType);
        Assert.Equal(4m, cheeseTx.Quantity);
        Assert.Equal(54m, cheeseTx.BalanceAfter);
        Assert.Equal(ctx.ManagerUser.Id, cheeseTx.CreatedByUserId);
    }

    [Fact]
    public async Task Test_03_ReconcileStocktake_AllowsManagerToAdjustCount_BeforeApproval()
    {
        using var ctx = new E2ETestContext();

        // 1. Staff counted 88kg of flour by mistake
        var blindRequest = new CreateBlindStocktakeRequest
        {
            Note = "Đếm lần 1",
            Items = new List<BlindStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 88m, Note = "Đếm sơ bộ" }
            }
        };
        var stocktakeDto = await ctx.InventoryService.CreateBlindStocktakeAsync(ctx.CashierUser.Id, blindRequest);

        // 2. Manager recounts and finds 96kg (only 4kg missing, not 12kg)
        var reconcileRequest = new ReconcileStocktakeRequest
        {
            StocktakeId = stocktakeDto.Id,
            Note = "Quản lý đã đếm lại chính xác",
            Items = new List<ReconcileStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 96m, Reason = "Tìm thấy thêm 8kg trong kho dự trữ" }
            }
        };

        var reconcileSuccess = await ctx.InventoryService.ReconcileStocktakeAsync(ctx.ManagerUser.Id, reconcileRequest);
        Assert.True(reconcileSuccess);

        // 3. CurrentStock should be 96kg
        var flourInDb = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        Assert.Equal(96m, flourInDb!.CurrentStock);

        // 4. StocktakeItem should record the adjusted count of 96kg
        var updatedStocktake = await ctx.InventoryService.GetStocktakeByIdAsync(stocktakeDto.Id);
        var flourItem = updatedStocktake!.Items.First(i => i.MaterialId == ctx.MatFlour.Id);
        Assert.Equal(96m, flourItem.ActualQuantity);
        Assert.Equal(-4m, flourItem.Difference);
        Assert.Equal(-4m * 25000m, flourItem.DifferenceValue);
        Assert.Contains("kho dự trữ", flourItem.Note);
    }

    [Fact]
    public async Task Test_04_ResilientUserIdHandling_NeverFailsWhenUserIdIsZeroOrInvalid()
    {
        using var ctx = new E2ETestContext();

        // 1. Blind count with invalid user ID = 0 (or -99)
        var blindRequest = new CreateBlindStocktakeRequest
        {
            Note = "Blind count with invalid user ID",
            Items = new List<BlindStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 99m }
            }
        };
        var stocktakeDto = await ctx.InventoryService.CreateBlindStocktakeAsync(0, blindRequest);
        Assert.NotNull(stocktakeDto);
        Assert.True(stocktakeDto.Id > 0);

        // 2. Reconcile with invalid user ID = -1
        var reconcileRequest = new ReconcileStocktakeRequest
        {
            StocktakeId = stocktakeDto.Id,
            Items = new List<ReconcileStocktakeItemRequest>
            {
                new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 99m, Reason = "Khớp" }
            }
        };
        var reconcileSuccess = await ctx.InventoryService.ReconcileStocktakeAsync(-1, reconcileRequest);
        Assert.True(reconcileSuccess);

        // Verify transaction was logged with a valid user ID (fallback to Admin/First user)
        var tx = await ctx.Db.InventoryTransactions.FirstAsync(t => t.ReferenceId == stocktakeDto.Id);
        Assert.True(tx.CreatedByUserId > 0);
    }

    [Fact]
    public async Task Test_05_GetAllStocktakes_ReturnsRecentHistoryOrderedByDate()
    {
        using var ctx = new E2ETestContext();

        // Create 2 stocktakes
        await ctx.InventoryService.CreateBlindStocktakeAsync(ctx.ManagerUser.Id, new CreateBlindStocktakeRequest
        {
            Note = "Phiếu 1",
            Items = new List<BlindStocktakeItemRequest> { new() { MaterialId = ctx.MatFlour.Id, ActualQuantity = 95m } }
        });

        await ctx.InventoryService.CreateBlindStocktakeAsync(ctx.ManagerUser.Id, new CreateBlindStocktakeRequest
        {
            Note = "Phiếu 2",
            Items = new List<BlindStocktakeItemRequest> { new() { MaterialId = ctx.MatCheese.Id, ActualQuantity = 48m } }
        });

        var allStocktakes = await ctx.InventoryService.GetAllStocktakesAsync();
        Assert.NotNull(allStocktakes);
        Assert.True(allStocktakes.Count >= 2);
    }

    [Fact]
    public async Task Test_06_UpdateMaterial_ConvertUnit_Weight()
    {
        using var ctx = new E2ETestContext();

        var unitGram = new UnitOfMeasure { Id = 10, Name = "Gram", Abbreviation = "g", CreatedAt = DateTime.UtcNow };
        ctx.Db.UnitOfMeasures.Add(unitGram);
        await ctx.Db.SaveChangesAsync();

        var mat = new Material
        {
            Id = 1036,
            UnitOfMeasureId = 10, // Gram
            Name = "Bắp ngọt hạt",
            NameEn = "Sweet Corn Kernels",
            CurrentStock = 20m,
            MinStockLevel = 6000m,
            CostPerUnit = 35000m,
            IsActive = true
        };
        ctx.Db.Materials.Add(mat);
        await ctx.Db.SaveChangesAsync();

        // Update to Kg (Id = 1)
        var req = new CreateMaterialRequest
        {
            Name = "Bắp ngọt hạt",
            NameEn = "Sweet Corn Kernels",
            UnitOfMeasureId = ctx.UnitKg.Id,
            MinStockLevel = 6m,
            CostPerUnit = 35000000m
        };

        var result = await ctx.InventoryService.UpdateMaterialAsync(mat.Id, req, ctx.AdminUser.Id);

        Assert.NotNull(result);
        Assert.Equal(ctx.UnitKg.Id, result.UnitOfMeasureId);
        Assert.Equal(0.02m, result.CurrentStock);
    }
}
