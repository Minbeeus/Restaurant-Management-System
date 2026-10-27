using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Xunit;

namespace Restaurant.Domain.Tests.E2E;

/// <summary>
/// Tier 3: Cross-Feature Combinations
/// Verifies safety, data consistency, and event propagation across multi-feature pairwise interactions.
/// </summary>
public class Tier3_CrossFeatureCombinationTests
{
    [Fact]
    public async Task TC_XF_01_TableMove_OrderItems_VoucherDiscount_CashPayment_TableRelease()
    {
        using var ctx = new E2ETestContext();

        // 1. Create order on Table 1
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 }, // 180,000
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 }   // 25,000 -> Total: 205,000
            }
        });

        // 2. Move Table 1 to Table 3
        var moved = await ctx.PosOrderService.MoveTableAsync(new MoveTableRequest
        {
            SourceTableId = ctx.Table1.Id,
            TargetTableId = ctx.Table3.Id
        });
        Assert.True(moved);

        var table1 = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        var table3 = await ctx.Db.Tables.FindAsync(ctx.Table3.Id);
        Assert.Equal((int)TableStatus.Available, table1!.Status);
        Assert.Equal((int)TableStatus.Serving, table3!.Status);

        // 3. Apply Voucher GIAM10 (10% capped at 20,000)
        var order = await ctx.Db.Orders.FindAsync(orderRes.OrderId);
        decimal discount = Math.Min(order!.SubTotal * 0.10m, ctx.Voucher10Pct.MaxDiscountAmount!.Value); // 20,000
        order.DiscountAmount = discount;
        order.TotalAmount = order.SubTotal - discount; // 205,000 - 20,000 = 185,000
        await ctx.Db.SaveChangesAsync();

        // 4. Pay via Cash
        var cashPay = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = order.Id,
            CashReceived = 200000m // Change: 15,000
        });

        Assert.Equal(185000m, cashPay.TotalAmount);
        Assert.Equal(15000m, cashPay.ChangeAmount);

        // 5. Release Table 3
        await ctx.PosOrderService.ClearTableAsync(ctx.Table3.Id);
        var table3After = await ctx.Db.Tables.FindAsync(ctx.Table3.Id);
        Assert.Equal((int)TableStatus.Available, table3After!.Status);
    }

    [Fact]
    public async Task TC_XF_02_KitchenDone_BOMDeduction_OrderCancellation_WasteConversion()
    {
        using var ctx = new E2ETestContext();

        // 1. Order 1 Pizza Margherita
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1 }
            }
        });

        var order = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderRes.OrderId);
        var item = order.OrderItems.First();

        // 2. Kitchen cooks to Done -> Deducts BOM
        await ctx.KitchenService.UpdateItemStatusAsync(item.Id, (int)KitchenStatus.Done);

        var cheeseMat = await ctx.Db.Materials.FindAsync(ctx.MatCheese.Id);
        Assert.Equal(49.85m, cheeseMat!.CurrentStock); // 50 - 0.15 = 49.85

        // Verify transaction is SaleDeduction
        var tx = await ctx.Db.InventoryTransactions.FirstAsync(t => t.ReferenceId == item.Id);
        Assert.Equal((int)InventoryTransactionType.SaleDeduction, tx.TransactionType);

        // 3. Manager cancels order
        var cancelled = await ctx.CancelOrderService.CancelOrderAsync(ctx.ManagerUser.Id, order.Id, "Khách bận việc gia đình");
        Assert.True(cancelled);

        // 4. BOM deduction maintains immutable ledger: SaleDeduction intact + CancellationWaste recorded
        await ctx.Db.Entry(tx).ReloadAsync();
        Assert.Equal((int)InventoryTransactionType.SaleDeduction, tx.TransactionType); // Preserves immutable audit trail

        var wasteTx = await ctx.Db.InventoryTransactions.FirstOrDefaultAsync(t => t.TransactionType == (int)InventoryTransactionType.CancellationWaste && t.MaterialId == tx.MaterialId);
        Assert.NotNull(wasteTx);
        Assert.False(string.IsNullOrEmpty(wasteTx.Note));

        // Table 1 released
        var table = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Available, table!.Status);
    }

    [Fact]
    public async Task TC_XF_03_ComboOrder_VoidComboHeader_CascadesVoidToAllChildItems()
    {
        using var ctx = new E2ETestContext();

        // Order 1 Combo Family with Pizza Margherita and Coca Cola
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new()
                {
                    MenuItemId = ctx.MenuComboFamily.Id,
                    Quantity = 1,
                    SelectedComboItems = new List<CartComboItemRequest>
                    {
                        new() { ComboItemId = ctx.ComboItemPizzaMargherita.Id, MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1, ComboGroupId = 1 },
                        new() { ComboItemId = ctx.ComboItemCocaCola.Id, MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1, ComboGroupId = 2 }
                    }
                }
            }
        });

        var order = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderRes.OrderId);
        var comboHeader = order.OrderItems.First(oi => oi.ParentItemId == null);

        // Void the Combo Header
        var voided = await ctx.PosOrderService.VoidOrderItemAsync(order.Id, comboHeader.Id, "Khách không lấy combo", ctx.CashierUser.Id);
        Assert.True(voided);

        // All 3 items (header + 2 children) must be voided
        var reloadedItems = await ctx.Db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
        Assert.Equal(3, reloadedItems.Count);
        Assert.All(reloadedItems, i => Assert.Equal((int)OrderItemStatus.Voided, i.Status));

        var updatedOrder = await ctx.Db.Orders.FindAsync(order.Id);
        Assert.Equal(0m, updatedOrder!.SubTotal);
        Assert.Equal(0m, updatedOrder.TotalAmount);
    }

    [Fact]
    public async Task TC_XF_04_CashPaidFirst_SePayWebhookArrivesLater_ConflictHandledSafely()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } } // 180,000
        });

        // 1. Cashier completes cash payment
        await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 180000m
        });

        // 2. Later, SePay webhook arrives for the same order
        var webhookPayload = new SepayWebhookPayload
        {
            id = 554433,
            gateway = "Techcombank",
            accountNumber = "987654321",
            transferAmount = 180000m,
            content = $"POS{orderRes.OrderId}",
            referenceCode = "REF-LATE-554433"
        };

        var handled = await ctx.PaymentService.ProcessSepayWebhookAsync(webhookPayload, null);
        Assert.True(handled); // Handled safely

        // Verifies SePay log records the conflict error
        var webhookLog = await ctx.Db.SepayWebhookLogs.FirstOrDefaultAsync(l => l.TransactionId == "554433");
        Assert.NotNull(webhookLog);
        Assert.True(webhookLog.IsProcessed);
        Assert.Contains("Conflict: Order ID", webhookLog.ErrorLog);

        // Only 1 payment record remains in database (cash payment)
        var payments = await ctx.Db.Payments.Where(p => p.OrderId == orderRes.OrderId).ToListAsync();
        Assert.Single(payments);
        Assert.Equal((int)PaymentMethod.Cash, payments[0].PaymentMethod);
    }

    [Fact]
    public async Task TC_XF_05_ShiftLifecycle_TillIn_Sales_TillOut_CloseShiftReconciled()
    {
        using var ctx = new E2ETestContext();

        // Shift 1 opened with 1,000,000 VND
        // 1. Till-In: 500,000 VND
        await ctx.CashMovementService.PerformTillInAsync(ctx.CashierUser.Id, ctx.ActiveShift.Id, new TillInRequest
        {
            Amount = 500000m,
            Reason = "Bổ sung tiền lẻ đầu ngày"
        });

        // 2. Cash Sale: Order 180,000 VND paid
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } }
        });
        await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 180000m
        });

        // 3. Till-Out: 300,000 VND
        await ctx.CashMovementService.PerformTillOutAsync(ctx.CashierUser.Id, ctx.ActiveShift.Id, new TillOutRequest
        {
            Amount = 300000m,
            Reason = "Nộp về két trung tâm"
        });

        // Expected cash: 1,000,000 (float) + 500,000 (till-in) + 180,000 (sale) - 300,000 (till-out) = 1,380,000 VND
        var shiftReport = await ctx.ShiftService.CloseShiftAsync(ctx.CashierUser.Id, new CloseShiftRequest
        {
            ActualCashCounted = 1380000m,
            Note = "Khớp tiền 100%"
        });

        Assert.Equal(1380000m, shiftReport.ExpectedCash);
        Assert.Equal(1380000m, shiftReport.ActualCash);
        Assert.Equal(0m, shiftReport.Difference); // Zero variance
    }

    [Fact]
    public async Task TC_XF_06_LoyaltyRedemption_VoucherDiscount_CashPayment_LoyaltyAccumulation()
    {
        using var ctx = new E2ETestContext();

        // Order: 2 Pizza Beef = 360,000 VND
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 2 }
            }
        });

        // 1. Redeem 100 loyalty points (5,000 VND discount)
        var redeemed = await ctx.LoyaltyService.RedeemPointsAsync(ctx.Customer1.Id, new RedeemPointsRequest
        {
            OrderId = orderRes.OrderId,
            PointsToRedeem = 100
        });
        Assert.True(redeemed);

        var order = await ctx.Db.Orders.FindAsync(orderRes.OrderId);
        Assert.Equal(355000m, order!.TotalAmount);

        // 2. Pay remaining 355,000 VND via Cash
        await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = order.Id,
            CashReceived = 355000m,
            CustomerId = ctx.Customer1.Id
        });

        // Customer gains: 355,000 / 10,000 = 35 points
        // Final available points: 200 (initial) - 100 (redeemed) + 35 (gained) = 135 points
        var customerAfter = await ctx.Db.Customers.FindAsync(ctx.Customer1.Id);
        Assert.NotNull(customerAfter);
        Assert.Equal(135, customerAfter.AvailablePoints);
    }

    [Fact]
    public async Task TC_XF_07_TableMerge_MultiTableOrdersConsolidated_SourceTablesFreed()
    {
        using var ctx = new E2ETestContext();

        // Table 1: 1 Pizza Beef (180,000)
        var ord1 = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } }
        });

        // Table 2: 1 Pizza Margherita (150,000)
        var ord2 = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table2.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1 } }
        });

        // Merge Table 2 into Table 1
        var merged = await ctx.PosOrderService.MergeTablesAsync(new MergeTableRequest
        {
            SourceTableIds = new List<int> { ctx.Table2.Id },
            TargetTableId = ctx.Table1.Id
        });
        Assert.True(merged);

        var targetOrder = await ctx.Db.Orders.FindAsync(ord1.OrderId);
        var sourceOrder = await ctx.Db.Orders.FindAsync(ord2.OrderId);

        Assert.Equal(330000m, targetOrder!.TotalAmount); // 180k + 150k
        Assert.Equal((int)OrderStatus.Cancelled, sourceOrder!.Status);

        var table1 = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        var table2 = await ctx.Db.Tables.FindAsync(ctx.Table2.Id);

        Assert.Equal((int)TableStatus.Serving, table1!.Status);
        Assert.Equal((int)TableStatus.Available, table2!.Status);
    }
}
