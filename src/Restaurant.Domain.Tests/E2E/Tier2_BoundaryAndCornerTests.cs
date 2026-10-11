using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Xunit;

namespace Restaurant.Domain.Tests.E2E;

/// <summary>
/// Tier 2: Boundary & Corner Cases
/// Tests extreme values, zero values, negative values, empty inputs, and illegal state transitions.
/// </summary>
public class Tier2_BoundaryAndCornerTests
{
    [Fact]
    public async Task TC_BND_01_CashTenderedLessThanTotal_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } // 180,000
            }
        });

        // Customer tenders 100,000 (short 80,000)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
            {
                OrderId = orderRes.OrderId,
                CashReceived = 100000m
            });
        });

        Assert.Contains("chưa đủ thanh toán", ex.Message);

        var order = await ctx.Db.Orders.FindAsync(orderRes.OrderId);
        Assert.NotNull(order);
        Assert.Equal((int)PaymentStatus.Unpaid, order.PaymentStatus);
    }

    [Fact]
    public async Task TC_BND_02_CashTenderedExactAmount_ChangeIsZeroAndStatusIsPaid()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } // 180,000
            }
        });

        var paymentRes = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 180000m // Exact
        });

        Assert.Equal(0m, paymentRes.ChangeAmount);
        Assert.Equal(180000m, paymentRes.TotalAmount);
        Assert.Equal(180000m, paymentRes.CashReceived);
    }

    [Fact]
    public async Task TC_BND_03_CashTenderedLargeDenomination_ChangeCalculatedAccurately()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } // 25,000
            }
        });

        // Customer hands a 500,000 VND note
        var paymentRes = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 500000m
        });

        Assert.Equal(475000m, paymentRes.ChangeAmount);
        Assert.Equal(25000m, paymentRes.TotalAmount);
    }

    [Fact]
    public async Task TC_BND_04_DoublePaymentPrevention_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 }
            }
        });

        // First payment succeeds
        await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 25000m
        });

        // Attempt second payment on same order
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
            {
                OrderId = orderRes.OrderId,
                CashReceived = 25000m
            });
        });

        Assert.Equal("Đơn hàng này đã được thanh toán.", ex.Message);
    }

    [Fact]
    public async Task TC_BND_05_DoubleWebhookIdempotency_SameTransactionRefReturnsTrueWithoutDoublePayment()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } // 25,000
            }
        });

        var payload = new SepayWebhookPayload
        {
            id = 991234,
            gateway = "Vietcombank",
            accountNumber = "123456789",
            transferAmount = 25000m,
            content = $"POS{orderRes.OrderId}",
            referenceCode = "REF-DUPLICATE-991234"
        };

        var firstCall = await ctx.PaymentService.ProcessSepayWebhookAsync(payload, null);
        var secondCall = await ctx.PaymentService.ProcessSepayWebhookAsync(payload, null);

        Assert.True(firstCall);
        Assert.True(secondCall);

        var paymentCount = await ctx.Db.Payments.CountAsync(p => p.OrderId == orderRes.OrderId);
        Assert.Equal(1, paymentCount);
    }

    [Fact]
    public async Task TC_BND_06_MoveTableToOccupiedTable_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        // Put Table 1 in Occupied
        await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } }
        });

        // Put Table 2 explicitly in Occupied status
        var table2 = await ctx.Db.Tables.FindAsync(ctx.Table2.Id);
        table2!.Status = (int)TableStatus.Occupied;
        await ctx.Db.SaveChangesAsync();

        // Attempt move Table 1 to Table 2 (which is occupied)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.PosOrderService.MoveTableAsync(new MoveTableRequest
            {
                SourceTableId = ctx.Table1.Id,
                TargetTableId = ctx.Table2.Id
            });
        });
    }

    [Fact]
    public async Task TC_BND_07_MergeTablesWithEmptySourceList_ReturnsFalse()
    {
        using var ctx = new E2ETestContext();

        var res = await ctx.PosOrderService.MergeTablesAsync(new MergeTableRequest
        {
            SourceTableIds = new List<int>(), // Empty
            TargetTableId = ctx.Table1.Id
        });

        Assert.False(res);
    }

    [Fact]
    public async Task TC_BND_08_VoidItemWithEmptyReason_AppliesDefaultReasonInVoidLog()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } }
        });

        var order = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderRes.OrderId);
        var item = order.OrderItems.First();

        var voided = await ctx.PosOrderService.VoidOrderItemAsync(order.Id, item.Id, "   ", ctx.CashierUser.Id);
        Assert.True(voided);

        var voidLog = await ctx.Db.VoidLogs.FirstOrDefaultAsync(v => v.OrderItemId == item.Id);
        Assert.NotNull(voidLog);
        Assert.NotEmpty(voidLog.Reason);
    }

    [Fact]
    public async Task TC_BND_09_VoidItemOnCompletedOrder_ReturnsFalse()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } }
        });

        // Pay to complete order
        await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 25000m
        });

        var order = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderRes.OrderId);
        var item = order.OrderItems.First();

        // Attempt to void item on already completed/paid order
        var voided = await ctx.PosOrderService.VoidOrderItemAsync(order.Id, item.Id, "Khách xin đổi", ctx.CashierUser.Id);
        Assert.False(voided);
    }

    [Fact]
    public async Task TC_BND_10_RevertKitchenItemFromDone_ReturnsNull()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } }
        });

        var item = await ctx.Db.OrderItems.FirstAsync(oi => oi.OrderId == orderRes.OrderId);

        // Transition to Done
        var doneRes = await ctx.KitchenService.UpdateItemStatusAsync(item.Id, (int)KitchenStatus.Done);
        Assert.NotNull(doneRes);
        Assert.Equal((int)KitchenStatus.Done, doneRes.KitchenStatus);

        // Attempt illegal transition: Done -> Processing
        var illegalRevert = await ctx.KitchenService.UpdateItemStatusAsync(item.Id, (int)KitchenStatus.Processing);
        Assert.Null(illegalRevert);

        // Status remains Done in DB
        var reQueriedItem = await ctx.Db.OrderItems.FindAsync(item.Id);
        Assert.NotNull(reQueriedItem);
        Assert.Equal((int)KitchenStatus.Done, reQueriedItem.KitchenStatus);
    }

    [Fact]
    public async Task TC_BND_11_RedeemLoyaltyPoints_ZeroOrNegativePoints_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 1 } }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.LoyaltyService.RedeemPointsAsync(ctx.Customer1.Id, new RedeemPointsRequest
            {
                OrderId = orderRes.OrderId,
                PointsToRedeem = 0 // Invalid 0 points
            });
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.LoyaltyService.RedeemPointsAsync(ctx.Customer1.Id, new RedeemPointsRequest
            {
                OrderId = orderRes.OrderId,
                PointsToRedeem = -50 // Invalid negative points
            });
        });
    }

    [Fact]
    public async Task TC_BND_12_RedeemLoyaltyPoints_ExceedingAvailableBalance_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest> { new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 } }
        });

        // Customer1 only has 200 available points, tries to redeem 500
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.LoyaltyService.RedeemPointsAsync(ctx.Customer1.Id, new RedeemPointsRequest
            {
                OrderId = orderRes.OrderId,
                PointsToRedeem = 500
            });
        });
    }

    [Fact]
    public async Task TC_BND_13_OpenShift_WhenShiftAlreadyOpen_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        // Cashier already has ActiveShift open from seed data
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.ShiftService.OpenShiftAsync(ctx.CashierUser.Id, new OpenShiftRequest
            {
                RegisterId = ctx.Register1.Id,
                InitialCash = 500000m
            });
        });

        Assert.Contains("SHIFT_ALREADY_OPEN", ex.Message);
    }

    [Fact]
    public async Task TC_BND_14_CloseShift_WithCashDiscrepancy_RecordsVarianceWithoutException()
    {
        using var ctx = new E2ETestContext();

        // Cashier counted 950,000 VND (expected 1,000,000 VND opening balance, shortage: -50,000)
        var report = await ctx.ShiftService.CloseShiftAsync(ctx.CashierUser.Id, new CloseShiftRequest
        {
            ActualCashCounted = 950000m,
            Note = "Thiếu 50k so với sổ sách"
        });

        Assert.NotNull(report);
        Assert.Equal(950000m, report.ActualCash);
        Assert.Equal(1000000m, report.ExpectedCash); // Expected balance
        Assert.Equal(-50000m, report.Difference);    // Variance recorded exactly
    }

    [Fact]
    public async Task TC_BND_15_SubmitOrder_EmptyItemsList_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
            {
                TableId = ctx.Table1.Id,
                Items = new List<CartItemValidateRequest>() // Empty
            });
        });
    }

    [Fact]
    public async Task TC_BND_16_SubmitOrder_ZeroQuantity_ThrowsInvalidOperationException()
    {
        using var ctx = new E2ETestContext();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
            {
                TableId = ctx.Table1.Id,
                Items = new List<CartItemValidateRequest>
                {
                    new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 0 } // Illegal 0 qty
                }
            });
        });
    }
}
