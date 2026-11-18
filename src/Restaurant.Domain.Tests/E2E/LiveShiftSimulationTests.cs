using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Restaurant.Domain.Tests.E2E;

/// <summary>
/// Live End-to-End Shift Simulation Test.
/// Executes an authentic full restaurant shift against real production Domain & Infrastructure services.
/// </summary>
public class LiveShiftSimulationTests
{
    private readonly ITestOutputHelper _output;

    public LiveShiftSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunCompleteLiveShiftSimulation_FromOpeningToClosingZReport()
    {
        using var ctx = new E2ETestContext();

        _output.WriteLine("================================================================================");
        _output.WriteLine("   BẮT ĐẦU MÔ PHỎNG VẬN HÀNH CA SÁNG THỰC TẾ TRÊN HỆ THỐNG RESTAURANT POS/KDS    ");
        _output.WriteLine("================================================================================");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 1: MỞ CA LÀM VIỆC & BÀN GIAO KÉT TIỀN
        // ---------------------------------------------------------------------
        // Đóng ca mặc định trong TestContext để mở một ca hoàn toàn mới
        ctx.ActiveShift.Status = (int)ShiftStatus.Closed;
        await ctx.Db.SaveChangesAsync();

        _output.WriteLine("\n[08:00] 1. Quản lý mở ca với 1.000.000đ tiền đầu ca...");
        var openShiftReq = new OpenShiftRequest
        {
            RegisterId = ctx.Register1.Id,
            InitialCash = 1000000m,
            CashCustodianPin = "777777", // PIN thu ngân NV0003
            Note = "Ca sáng chính thức Tầng 1"
        };

        var shiftReport = await ctx.ShiftService.OpenShiftAsync(ctx.ManagerUser.Id, openShiftReq);
        Assert.NotNull(shiftReport);
        Assert.Equal(1000000m, shiftReport.InitialCash);
        _output.WriteLine($" -> Ca mở thành công! Mã Ca: {shiftReport.ShiftCode}, Tiền két: {shiftReport.ExpectedCash:N0}đ");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 2: BÀN 01 GỌI MÓN (DINE-IN ORDER)
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[09:30] 2. Khách vào Bàn 01 gọi Combo Family + Pizza Bò + Nước...");
        var order1Res = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table1.Id,
            CustomerNote = "Bàn 01 - Ăn tại chỗ",
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
                new() { MenuItemId = ctx.MenuPizzaBeef.Id, Quantity = 1 },
                new() { MenuItemId = ctx.MenuCocaCola.Id, Quantity = 2 }
            }
        });

        Assert.True(order1Res.OrderId > 0);
        var table1 = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Serving, table1!.Status);
        _output.WriteLine($" -> Đơn {order1Res.OrderCode} tạo thành công! Tổng tiền: {order1Res.TotalAmount:N0}đ, Bàn 01: Serving");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 3: BẾP CHẾ BIẾN & TỰ ĐỘNG TRỪ KHO ĐỊNH LƯỢNG (BOM)
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[09:45] 3. Bếp trưởng nhận ticket KDS và bấm hoàn thành các món...");
        var order1 = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == order1Res.OrderId);
        foreach (var item in order1.OrderItems)
        {
            await ctx.KitchenService.UpdateItemStatusAsync(item.Id, (int)KitchenStatus.Done);
        }

        var readyOrder1 = await ctx.Db.Orders.FindAsync(order1Res.OrderId);
        Assert.Equal((int)OrderStatus.Ready, readyOrder1!.Status);

        var flour = await ctx.Db.Materials.FindAsync(ctx.MatFlour.Id);
        var cheese = await ctx.Db.Materials.FindAsync(ctx.MatCheese.Id);
        _output.WriteLine($" -> Tất cả món đã nướng xong! Đơn {order1Res.OrderCode}: READY. Kho trừ BOM: Bột mì còn {flour!.CurrentStock}kg, Phô mai còn {cheese!.CurrentStock}kg");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 4: HỦY 1 MÓN SAU KHI NẤU XONG -> CANCELLATION WASTE
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[10:15] 4. Khách xin hủy 1 chai Coca do không uống hết...");
        var cokeItem = order1.OrderItems.First(oi => oi.MenuItemId == ctx.MenuCocaCola.Id && oi.ParentItemId == null);
        var voidSuccess = await ctx.PosOrderService.VoidOrderItemAsync(order1.Id, cokeItem.Id, "Khách không uống nước ngọt", ctx.ManagerUser.Id);
        Assert.True(voidSuccess);

        var wasteTx = await ctx.Db.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceId == cokeItem.Id && t.TransactionType == (int)InventoryTransactionType.CancellationWaste);
        Assert.NotNull(wasteTx);
        _output.WriteLine($" -> Hủy món thành công! Đã ghi nhận phế phẩm CancellationWaste mã #{wasteTx.Id}: {wasteTx.Note}");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 5: THANH TOÁN TIỀN MẶT BÀN 01
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[10:30] 5. Bàn 01 thanh toán Tiền mặt (Khách đưa 500.000đ)...");
        var reloadedOrder1 = await ctx.Db.Orders.FindAsync(order1.Id);
        var cashPayRes = await ctx.PaymentService.ProcessCashPaymentAsync(new ProcessCashPaymentRequest
        {
            OrderId = order1.Id,
            CashReceived = 500000m,
            CustomerId = ctx.Customer1.Id
        }, ctx.CashierUser.Id);

        Assert.NotNull(cashPayRes);
        _output.WriteLine($" -> Thanh toán thành công! Tiền bill: {cashPayRes.TotalAmount:N0}đ | Tiền thối: {cashPayRes.ChangeAmount:N0}đ");

        await ctx.PosOrderService.ClearTableAsync(ctx.Table1.Id);
        var table1AfterClean = await ctx.Db.Tables.FindAsync(ctx.Table1.Id);
        Assert.Equal((int)TableStatus.Available, table1AfterClean!.Status);
        _output.WriteLine(" -> Đã dọn bàn 01 -> Trạng thái: Available");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 6: BÀN 02 THANH TOÁN VIETQR QUA SEPAY WEBHOOK
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[11:45] 6. Bàn 02 gọi Pizza Margherita (150.000đ) và thanh toán VietQR...");
        var order2Res = await ctx.PosOrderService.SubmitPosOrderAsync(new SubmitPosOrderRequest
        {
            TableId = ctx.Table2.Id,
            Items = new List<CartItemValidateRequest>
            {
                new() { MenuItemId = ctx.MenuPizzaMargherita.Id, Quantity = 1 }
            }
        });

        // Bếp làm xong
        var order2 = await ctx.Db.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == order2Res.OrderId);
        foreach (var it in order2.OrderItems)
        {
            await ctx.KitchenService.UpdateItemStatusAsync(it.Id, (int)KitchenStatus.Done);
        }

        // Tạo VietQR
        var vietQr = await ctx.PaymentService.GenerateVietQrAsync(new GenerateVietQrRequest { OrderId = order2.Id });
        Assert.NotEmpty(vietQr.SepayContent);

        // Mô phỏng SePay Webhook bắn về
        var webhookProcessed = await ctx.PaymentService.ProcessSepayWebhookAsync(new SepayWebhookPayload
        {
            id = 899123,
            gateway = "TPBank",
            accountNumber = "11102042003",
            transferAmount = order2.TotalAmount,
            content = vietQr.SepayContent,
            transferType = "in",
            referenceCode = "FT261105001"
        }, null, "SePaySecretApiKey123!");

        Assert.True(webhookProcessed);
        var paidOrder2 = await ctx.Db.Orders.FindAsync(order2.Id);
        Assert.Equal((int)PaymentStatus.Paid, paidOrder2!.PaymentStatus);
        _output.WriteLine($" -> SePay Webhook xử lý thành công đơn {paidOrder2.OrderCode}: 150.000đ nhận qua VietQR!");

        // Dọn bàn 2
        await ctx.PosOrderService.ClearTableAsync(ctx.Table2.Id);

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 7: CHI TIỀN MẶT ĐỘT XUẤT (TILL-OUT EXPENSE)
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[14:00] 7. Quản lý chi 50.000đ tiền mặt từ két mua đá lạnh...");
        var activeShiftId = shiftReport.ShiftId;
        var tillOutRes = await ctx.CashMovementService.PerformTillOutAsync(ctx.ManagerUser.Id, activeShiftId, new TillOutRequest
        {
            Amount = 50000m,
            Reason = "Mua đá lạnh cấp bách"
        });
        Assert.NotNull(tillOutRes);
        _output.WriteLine($" -> Chi tiền thành công! Đã rút: {tillOutRes.Amount:N0}đ");

        // ---------------------------------------------------------------------
        // GIAI ĐOẠN 8: KIỂM ĐẾM TIỀN & ĐÓNG CA (SHIFT RECONCILIATION & Z-REPORT)
        // ---------------------------------------------------------------------
        _output.WriteLine("\n[16:00] 8. Hết ca: Đối soát tài chính và đóng ca...");

        var preCloseReport = await ctx.ShiftService.GetCurrentShiftAsync(ctx.CashierUser.Id);
        Assert.NotNull(preCloseReport);
        var remainingCashToTillOut = preCloseReport.RemainingCashToTillOut;
        _output.WriteLine($" -> Tiền mặt trong két cần rút (Till-Out): {remainingCashToTillOut:N0} VNĐ");

        // Rút toàn bộ tiền mặt bàn giao về két sắt trung tâm (Till-Out 100%)
        var finalTillOut = await ctx.CashMovementService.PerformTillOutAsync(ctx.ManagerUser.Id, activeShiftId, new TillOutRequest
        {
            Amount = remainingCashToTillOut,
            Reason = "Nộp toàn bộ tiền mặt cuối ca về két sắt an toàn"
        });
        Assert.NotNull(finalTillOut);

        // Nếu số tiền rút > 500.000đ (chính sách chống gian lận nội bộ), hệ thống tự sinh phiếu duyệt cho Admin
        if (finalTillOut.ApprovalRequestId.HasValue)
        {
            _output.WriteLine($" -> Số tiền {remainingCashToTillOut:N0}đ > hạn mức 500.000đ: Hệ thống tự động tạo Phiếu Phê Duyệt #{finalTillOut.ApprovalRequestId.Value}. Admin phê duyệt...");
            await ctx.ApprovalService.ApproveRequestAsync(ctx.AdminUser.Id, new ProcessApprovalRequestDto
            {
                ApprovalRequestId = finalTillOut.ApprovalRequestId.Value,
                ApprovalNote = "Admin duyệt nộp két cuối ca"
            });
        }

        // Đóng ca
        var closeReport = await ctx.ShiftService.CloseShiftAsync(ctx.ManagerUser.Id, new CloseShiftRequest
        {
            ActualCashCounted = remainingCashToTillOut,
            Note = "Đóng ca sáng khớp tiền tuyệt đối, bàn giao cho ca chiều"
        });

        Assert.Equal((int)ShiftStatus.Closed, closeReport.Status);
        Assert.Equal(0, closeReport.Difference);

        _output.WriteLine("\n================================================================================");
        _output.WriteLine("                         BÁO CÁO Z-REPORT ĐÓNG CA                               ");
        _output.WriteLine("================================================================================");
        _output.WriteLine($"Mã Ca:                 {closeReport.ShiftCode}");
        _output.WriteLine($"Máy Thu Ngân:          {closeReport.RegisterName}");
        _output.WriteLine($"Quản lý mở ca:         {closeReport.OpenedByStaffName}");
        _output.WriteLine($"Quản lý đóng ca:       {closeReport.ClosedByStaffName}");
        _output.WriteLine($"Thu ngân giữ két:      {closeReport.CashCustodianStaffName} ({closeReport.CashCustodianEmployeeCode})");
        _output.WriteLine("--------------------------------------------------------------------------------");
        _output.WriteLine($"Tiền mặt đầu ca:       {closeReport.InitialCash:N0} VNĐ");
        _output.WriteLine($"Doanh thu Tiền mặt:    {closeReport.CashRevenue:N0} VNĐ ({closeReport.TotalCashOrders} đơn)");
        _output.WriteLine($"Doanh thu Chuyển khoản:{closeReport.BankingRevenue:N0} VNĐ ({closeReport.TotalTransferOrders} đơn)");
        _output.WriteLine($"Chi tiền mặt trong ca: 50.000 VNĐ");
        _output.WriteLine($"Đã nộp két an toàn:    {remainingCashToTillOut:N0} VNĐ");
        _output.WriteLine("--------------------------------------------------------------------------------");
        _output.WriteLine($"TỔNG DOANH THU CA:     {closeReport.TotalRevenue:N0} VNĐ");
        _output.WriteLine($"CHÊNH LỆCH KÉT:        {closeReport.Difference:N0} VNĐ [KHỚP HOÀN TOÀN]");
        _output.WriteLine("TRẠNG THÁI CA:         ĐÃ ĐÓNG & KHÓA SỔ");
        _output.WriteLine("================================================================================\n");
    }
}
