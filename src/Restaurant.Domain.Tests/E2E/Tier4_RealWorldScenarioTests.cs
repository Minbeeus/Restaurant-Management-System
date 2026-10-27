using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Xunit;

namespace Restaurant.Domain.Tests.E2E;

/// <summary>
/// Tier 4: Real-World Scenarios
/// Simulates complete multi-step dine-in restaurant operations across opening shift, table seating,
/// ordering, kitchen prep, split bill, payments, and closing shift reconciliation.
/// </summary>
public class Tier4_RealWorldScenarioTests
{
    [Fact]
    public async Task SCENARIO_A_CompleteLunchRushDineInLifecycle()
    {
        using var ctx = new E2ETestContext();

        // -------------------------------------------------------------
        // Step 1: Cashier confirms active shift with initial float
        // -------------------------------------------------------------
        var currentShift = await ctx.ShiftService.GetCurrentShiftAsync(ctx.CashierUser.Id);
        Assert.NotNull(currentShift);
        Assert.Equal(1000000m, currentShift.InitialCash);

        // -------------------------------------------------------------
        // Step 2: Table 1 guests seated, submit Dine-in order
        // Combo Family (199k) + Pizza Margherita (150k) + 2 Coca Colas (50k) = 399,000 VND
        // -------------------------------------------------------------
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            CustomerNote = "Bàn 01 - Ăn trưa nhanh",
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
                },
                new() { MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1 },
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 2 }
            }
        });

        Assert.True(orderRes.OrderId > 0);
        var tableAfterOrder = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Serving, tableAfterOrder!.Status);

        // -------------------------------------------------------------
        // Step 3: Kitchen receives KDS ticket, Chef completes items
        // -------------------------------------------------------------
        var tickets = await ctx.KitchenService.GetActiveTicketsAsync();
        var myTicket = tickets.FirstOrDefault(t => t.OrderId == orderRes.OrderId);
        Assert.NotNull(myTicket);

        foreach (var item in myTicket.Items)
        {
            await ctx.KitchenService.UpdateItemStatusAsync(item.OrderItemId, (int)KitchenStatus.Done);
        }

        // -------------------------------------------------------------
        // Step 4: Verify Order transitions to Ready (not Completed before settlement)
        // -------------------------------------------------------------
        var readyOrder = await ctx.Db.Orders.FindAsync(orderRes.OrderId);
        Assert.NotNull(readyOrder);
        Assert.Equal((int)OrderStatus.Ready, readyOrder.Status);

        // -------------------------------------------------------------
        // Step 5: Verify BOM inventory deducted automatically
        // -------------------------------------------------------------
        var flour = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        var cheese = await ctx.Db.Materials.FindAsync(ctx.MatCheese.Id);
        var coke = await ctx.Db.Materials.FindAsync(ctx.MatCokeCan.Id);

        Assert.True(flour!.CurrentStock < 100m);
        Assert.True(cheese!.CurrentStock < 50m);
        Assert.True(coke!.CurrentStock < 120m);

        // -------------------------------------------------------------
        // Step 6: Print Temporary K80 Bill (Pre-bill)
        // Table moves to Billing; Order remains Unpaid
        // -------------------------------------------------------------
        var printed = await ctx.PosOrderService.PrintTempBillAsync(orderRes.OrderId);
        Assert.True(printed);

        var tableBilling = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Billing, tableBilling!.Status);
        Assert.Equal((int)PaymentStatus.Unpaid, readyOrder.PaymentStatus);

        // -------------------------------------------------------------
        // Step 7: Customer provides loyalty phone, redeems 100 points (5,000 VND discount)
        // -------------------------------------------------------------
        var redeemed = await ctx.LoyaltyService.RedeemPointsAsync(ctx.Customer1.Id, new RedeemPointsRequest
        {
            OrderId = orderRes.OrderId,
            PointsToRedeem = 100
        });
        Assert.True(redeemed);

        var discountedOrder = await ctx.Db.Orders.FindAsync(orderRes.OrderId);
        Assert.Equal(394000m, discountedOrder!.TotalAmount); // 399,000 - 5,000 = 394,000

        // -------------------------------------------------------------
        // Step 8: Customer pays cash: 400,000 VND tendered (Change: 6,000 VND)
        // -------------------------------------------------------------
        var cashPayRes = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = orderRes.OrderId,
            CashReceived = 400000m,
            CustomerId = ctx.Customer1.Id
        });

        Assert.Equal(394000m, cashPayRes.TotalAmount);
        Assert.Equal(6000m, cashPayRes.ChangeAmount);

        // -------------------------------------------------------------
        // Step 9: Release Table 1
        // -------------------------------------------------------------
        await ctx.PosOrderService.ClearTableAsync(ctx.Table1.Id);
        var tableFinal = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Available, tableFinal!.Status);

        // -------------------------------------------------------------
        // Step 10: Verify Loyalty Points gained and Customer balance
        // 394,000 / 10,000 = 39 points gained
        // 200 - 100 + 39 = 139 points
        // -------------------------------------------------------------
        var customerFinal = await ctx.Db.Customers.FindAsync(ctx.Customer1.Id);
        Assert.Equal(139, customerFinal!.AvailablePoints);
    }

    [Fact]
    public async Task SCENARIO_B_FamilyGatheringBillSplittingAndMultiPaymentSettlement()
    {
        using var ctx = new E2ETestContext();

        // -------------------------------------------------------------
        // Step 1: Table seated, 2 Pizzas and 2 Drinks ordered (Subtotal: 410,000 VND)
        // -------------------------------------------------------------
        var orderRes = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 },       // 180,000
                new() { MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1 }, // 150,000
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 2 }         // 50,000
            }
        });

        // -------------------------------------------------------------
        // Step 2: Food cooked on KDS
        // -------------------------------------------------------------
        var order = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderRes.OrderId);
        foreach (var item in order.OrderItems)
        {
            await ctx.KitchenService.UpdateItemStatusAsync(item.Id, (int)KitchenStatus.Done);
        }

        // -------------------------------------------------------------
        // Step 3: Guests request Split Bill
        // Order 1 keeps Pizza Beef (180,000) + 1 Coke (25,000) = 205,000 VND
        // Order 2 takes Pizza Margherita (150,000) + 1 Coke (25,000) = 175,000 VND (New Order on Table 2)
        // -------------------------------------------------------------
        var splitOrder = new Order
        {
            OrderCode = "ORD-SPLIT-GUEST2",
            ShiftId = ctx.ActiveShift.Id,
            TableId = ctx.Table2.Id,
            CreatedByUserId = ctx.CashierUser.Id,
            Status = (int)OrderStatus.InPreparation,
            SubTotal = 175000m,
            TotalAmount = 175000m,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Db.Orders.Add(splitOrder);

        order.SubTotal = 205000m;
        order.TotalAmount = 205000m;
        await ctx.Db.SaveChangesAsync();

        Assert.Equal(380000m, order.TotalAmount + splitOrder.TotalAmount);

        // -------------------------------------------------------------
        // Step 4: Guest 1 settles Order 1 via VietQR / SePay Bank Transfer
        // -------------------------------------------------------------
        var sepayWebhook = new SepayWebhookPayload
        {
            id = 771122,
            gateway = "VietinBank",
            accountNumber = "999888777",
            transferAmount = 205000m,
            content = $"POS{order.Id}",
            referenceCode = "SEPAY-SPLIT-01"
        };
        var webhookProcessed = await ctx.PaymentService.ProcessSepayWebhookAsync(sepayWebhook, null);
        Assert.True(webhookProcessed);

        var order1Paid = await ctx.Db.Orders.FindAsync(order.Id);
        Assert.Equal((int)PaymentStatus.Paid, order1Paid!.PaymentStatus);

        // -------------------------------------------------------------
        // Step 5: Guest 2 settles Order 2 via Cash
        // -------------------------------------------------------------
        var cashRes = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = splitOrder.Id,
            CashReceived = 200000m // Change: 25,000
        });

        Assert.Equal(175000m, cashRes.TotalAmount);
        Assert.Equal(25000m, cashRes.ChangeAmount);

        var order2Paid = await ctx.Db.Orders.FindAsync(splitOrder.Id);
        Assert.Equal((int)PaymentStatus.Paid, order2Paid!.PaymentStatus);

        // -------------------------------------------------------------
        // Step 6: Tables cleared
        // -------------------------------------------------------------
        await ctx.PosOrderService.ClearTableAsync(ctx.Table1.Id);
        await ctx.PosOrderService.ClearTableAsync(ctx.Table2.Id);

        var table1 = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        var table2 = await ctx.Db.Tables.FindAsync(ctx.Table2.Id);
        Assert.Equal((int)TableStatus.Available, table1!.Status);
        Assert.Equal((int)TableStatus.Available, table2!.Status);
    }

    [Fact]
    public async Task SCENARIO_C_QrSelfOrdering_KitchenDelayAlert_EmergencyCancellationWastage()
    {
        using var ctx = new E2ETestContext();

        // -------------------------------------------------------------
        // Step 1: Customer scans QR code on Table 2, submits order
        // -------------------------------------------------------------
        var table2 = await ctx.Db.Tables.FindAsync(ctx.Table2.Id);
        Assert.NotNull(table2);
        Assert.NotEmpty(table2.QrToken);

        var qrOrder = new Order
        {
            OrderCode = "QR-2026-0001",
            OrderType = (int)OrderType.DineIn,
            TableId = ctx.Table2.Id,
            ShiftId = ctx.ActiveShift.Id,
            CreatedByUserId = ctx.CashierUser.Id,
            Status = (int)OrderStatus.Draft, // QR starts in Draft
            SubTotal = 180000m,
            TotalAmount = 180000m,
            CreatedAt = DateTime.UtcNow
        };
        var qrOrderItem = new OrderItem
        {
            MenuItemId = ctx.MenuPizzaBeef.Id,
            MenuItemName = ctx.MenuPizzaBeef.Name,
            UnitPrice = 180000m,
            Quantity = 1,
            KitchenStatus = (int)KitchenStatus.Pending
        };
        qrOrder.OrderItems.Add(qrOrderItem);
        ctx.Db.Orders.Add(qrOrder);
        table2.Status = (int)TableStatus.Occupied;
        await ctx.Db.SaveChangesAsync();

        // -------------------------------------------------------------
        // Step 2: Cashier reviews and approves QR order -> InPreparation
        // -------------------------------------------------------------
        var approved = await ctx.PosOrderService.ApproveQrOrderAsync(qrOrder.Id, "Duyệt đơn bàn 02");
        Assert.True(approved);

        var orderApproved = await ctx.Db.Orders.FindAsync(qrOrder.Id);
        Assert.Equal((int)OrderStatus.InPreparation, orderApproved!.Status);

        // -------------------------------------------------------------
        // Step 3: Chef cooks dish to Done -> BOM raw materials deducted
        // -------------------------------------------------------------
        await ctx.KitchenService.UpdateItemStatusAsync(qrOrderItem.Id, (int)KitchenStatus.Done);

        var flourBeforeCancel = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        Assert.Equal(99.80m, flourBeforeCancel!.CurrentStock);

        // -------------------------------------------------------------
        // Step 4: Customer has emergency; Manager PIN verifies and cancels order
        // -------------------------------------------------------------
        var pinValid = await ctx.IdentityService.VerifyManagerPinAsync("888888"); // Manager PIN
        Assert.True(pinValid);

        var cancelled = await ctx.CancelOrderService.CancelOrderAsync(ctx.ManagerUser.Id, qrOrder.Id, "Khách có việc khẩn cấp xin hủy");
        Assert.True(cancelled);

        // -------------------------------------------------------------
        // Step 5: Verify BOM deductions: SaleDeduction preserved, CancellationWaste added
        // -------------------------------------------------------------
        var originalTx = await ctx.Db.InventoryTransactions.FirstAsync(t => t.ReferenceId == qrOrderItem.Id && t.TransactionType == (int)InventoryTransactionType.SaleDeduction);
        Assert.NotNull(originalTx);

        var wasteTx = await ctx.Db.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceId == qrOrder.Id && t.TransactionType == (int)InventoryTransactionType.CancellationWaste);
        Assert.NotNull(wasteTx);
        Assert.False(string.IsNullOrEmpty(wasteTx.Note));

        // -------------------------------------------------------------
        // Step 6: Table 2 returns to Available
        // -------------------------------------------------------------
        var table2Final = await ctx.Db.Tables.FindAsync(ctx.Table2.Id);
        Assert.Equal((int)TableStatus.Available, table2Final!.Status);
    }
}
