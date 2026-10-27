# BÁO CÁO AUDIT CHUYÊN SÂU: LUỒNG POS/KDS, TRỪ KHO BOM, HỦY MÓN & GỌI THÊM

- **Người thực hiện:** Agent 3 (Luồng POS/KDS, Trừ kho, Hủy món và Gọi thêm Auditor)
- **Thư mục làm việc:** `d:\My Project\.agents\teamwork\explorer_agent3`
- **Mã kiểm toán:** `AUDIT-AGENT3-POS-KDS-INV-20261014`
- **Tình trạng:** Hoàn tất điều tra toàn diện (Read-Only Deep Audit)
- **Phiên bản mã nguồn:** .NET 9 / EF Core 9 / Blazor Server / SignalR

---

## I. TỔNG QUAN ĐÁNH GIÁ (EXECUTIVE SUMMARY)

Báo cáo này đánh giá tính toàn vẹn dữ liệu, an toàn giao dịch và mức độ hoàn thiện của luồng dữ liệu từ Giao diện (UI) -> SignalR Hub / WebAPI Controller -> Application / Domain Services -> Cơ sở dữ liệu (EF Core / SQL Server) liên quan đến:
1. **Thời điểm và cấp độ khấu trừ kho BOM thực tế trong chu trình POS/KDS**.
2. **Kiến trúc tích hợp SignalR, Domain Events, Outbox và rủi ro mất giao dịch hoặc trừ trùng khi retry/reconnect**.
3. **Ma trận hủy món và hủy đơn (Void / Cancel Matrix)**: xử lý trước nấu, trong nấu, sau nấu; cơ chế quản lý hao hụt (`CancellationWaste`) vs hoàn kho (`CancellationReturn`); thẩm quyền duyệt bằng PIN Quản lý (`Manager PIN`).
4. **Nghiệp vụ gọi thêm món (Add-on items / Multiple rounds)**: cách ly khấu trừ kho và tính toàn vẹn của đơn hàng.
5. **Đánh giá lỗ hổng quy đổi đơn vị tính BOM (Unit Conversion)** và **khoảng trống kiểm thử (Missing Tests)**.

### Bảng Tổng Hợp Phân Loại Tính Năng (Feature Audit Matrix)

| STT | Phân Hệ / Chức Năng | Phân Loại (Classification) | Ưu Tiên (Priority) | Tóm Tắt Phát Hiện Cốt Lõi |
|---|---|---|---|---|
| 1 | **Thời điểm trừ kho BOM** | `PARTIAL` | **P1** | Trừ duy nhất khi bếp bấm "Xong" (`KitchenStatus.Done`). Không trừ khi tạo đơn POS, không trừ khi gửi bếp, không trừ khi thanh toán. Món không qua bếp (đồ uống lon/đóng chai) hoặc bếp quên bấm Done sẽ không bao giờ bị trừ kho. |
| 2 | **Cấp độ trừ kho (Order vs Item)** | `DONE_VERIFIED` | **P2** | Trừ chính xác ở cấp món ăn (`OrderItemId`), không trừ ở cấp đơn (`Order`). Hỗ trợ định mức riêng cho Modifier/Topping. |
| 3 | **Kiến trúc Domain Event (Outbox vs Sync)** | `BROKEN` | **P0** | Xử lý In-process MediatR đồng bộ, **KHÔNG CÓ Outbox pattern**. Tách làm 2 lần `SaveChangesAsync` độc lập. Nếu pha 2 lỗi (concurrency / timeout), món đã thành `Done` nhưng kho không bị trừ, và cơ chế chặn retry làm mất trừ kho vĩnh viễn. |
| 4 | **Rủi ro trừ trùng khi Retry/Reconnect KDS** | `PARTIAL` | **P1** | Tuần tự được bảo vệ tốt qua 3 lớp (UI `ProcessingItems`, Status transition, `InventoryTransaction.AnyAsync`). Nhưng race condition đồng thời thiếu Unique Index DB và `OrderItem` thiếu `RowVersion`, gây lỗi 500 do xung đột `Material.RowVersion`. |
| 5 | **Hủy món lẻ sau nấu (`VoidOrderItemAsync`)** | `BROKEN` | **P0** | Món đã nấu xong (`Done`) bị hủy từ POS: giảm tiền đơn hàng nhưng **HOÀN TOÀN KHÔNG CHUYỂN THÀNH HAO HỤT** (`CancellationWaste`). Nguyên liệu bị trừ mất nhưng báo cáo tài chính/kho bị lệch nghiêm trọng. Test `F24_R5_03` che giấu lỗi này. |
| 6 | **Duyệt hủy đơn/món bằng PIN Quản lý trên POS** | `BROKEN` | **P0** | Thu ngân nhập đúng PIN Quản lý trên modal, nhưng request gửi lên backend bằng Token của Thu ngân (`Role = Cashier`), bị chặn bởi `[Authorize(Roles = "Admin,Manager")]` -> **HTTP 403 Forbidden**. POS UI nuốt lỗi và báo thành công giả tạo! |
| 7 | **Hủy đơn trong BOH Approval Center** | `BROKEN` | **P1** | Khi Quản lý duyệt hủy đơn từ Approval Center, backend cập nhật cờ DB trực tiếp và **BỎ QUA TOÀN BỘ** `CancelOrderService` (không chuyển waste, không hoàn tiền, không rollback điểm Loyalty, không SignalR). |
| 8 | **Báo cáo Hao hụt kho (`InventoryLossReport`)** | `BROKEN` | **P1** | Chỉ số `TotalWasteAmount` bị gán cứng bằng công thức FAKE: `Math.Round(totalLoss * 0.35m, 0)` (35% độ lệch kiểm kê). Toàn bộ dữ liệu `CancellationWaste` thực tế bị bỏ quên, không hiển thị. |
| 9 | **Cơ chế Hoàn kho (`CancellationReturn`)** | `NOT_STARTED` / `NEEDS_BUSINESS_DECISION` | **P2** | Giá trị enum `CancellationReturn = 4` là code chết (Dead Code). Hệ thống mặc định 100% món hủy sau nấu là vứt bỏ (`Waste`), không hỗ trợ nhập lại kho cho đồ nguyên lon/hộp. |
| 10 | **Hủy khi đang nấu (`Processing`)** | `NEEDS_BUSINESS_DECISION` | **P2** | Hủy đơn/món khi đang nấu: nguyên liệu thực tế đã tiêu hao trong bếp nhưng vì chưa `Done` nên chưa bị trừ trong DB, và `CancelOrderService` không ghi nhận gì -> Thất thoát ngầm không chứng từ. |
| 11 | **Gọi thêm món (Add-on items)** | `PARTIAL` | **P2** | Cập nhật đơn cũ, tạo `OrderItem` mới, trừ kho cách ly an toàn theo `OrderItemId`. Tuy nhiên nếu đơn hàng đã ở trạng thái `Ready`, gọi thêm món không đưa `Order.Status` quay lại `InPreparation`. |
| 12 | **Quy đổi đơn vị tính BOM khi trừ kho** | `BROKEN` | **P0** | `InventoryService.DeductInventoryForOrderItemAsync` nhân trực tiếp số lượng công thức với số lượng món mà **KHÔNG ÁP DỤNG BẢNG QUY ĐỔI ĐƠN VỊ TÍNH** (`UnitConversion`), gây lệch tồn kho nghiêm trọng nếu đơn vị công thức khác đơn vị kho (g vs kg). |
| 13 | **Độ bao phủ kiểm thử (Test Suite Coverage)** | `PARTIAL` | **P2** | `CancelOrderServiceTests.cs` chỉ chứa 1 dummy test `Assert.True(true)`. `UnitTest1.cs` rỗng. `KdsServicesTests` mock `IMediator` bỏ qua trừ kho. Không có test kiểm tra concurrency lock trừ kho. |

---

## II. 5-COMPONENT HANDOFF REPORT

### 1. OBSERVATIONS (QUAN SÁT THỰC CHỨNG & DẪN CHỨNG MÃ NGUỒN)

#### 1.1. Luồng Khấu Trừ Kho Thực Tế & Thời Điểm Kích Hoạt
- **Tạo đơn POS & Gửi bếp**:
  - Tệp: `src/Restaurant.Infrastructure/Services/PosOrderService.cs`
  - Dòng 551, 645:
    ```csharp
    KitchenStatus = (int)KitchenStatus.Pending, // Do not mark Done prematurely
    ```
  - Dòng 723:
    ```csharp
    await _notificationService.NotifyKitchenTicketAsync(ticket);
    ```
  - *Quan sát thực chứng:* Không hề có bất kỳ lệnh gọi nào tới `IInventoryService` hay trừ tồn kho nguyên liệu trong toàn bộ phương thức `SubmitPosOrderAsync`.
- **Bếp hoàn thành món ăn (Kitchen Done)**:
  - Tệp: `src/Restaurant.Infrastructure/Services/KitchenService.cs`
  - Dòng 220-228:
    ```csharp
    // 7. Domain Event publishing for BOM Inventory Deduction on Done (only for non-combo headers)
    if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
    {
        // Do not deduct for Combo header itself; deduct for child items and standalone items
        bool isComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue;
        if (!isComboHeader)
        {
            await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
        }
    }
    ```
  - Tệp: `src/Restaurant.Infrastructure/Handlers/DeductInventoryOnOrderItemKitchenDoneHandler.cs`
  - Dòng 16-19:
    ```csharp
    public async Task Handle(OrderItemKitchenDoneDomainEvent notification, CancellationToken cancellationToken)
    {
        await _inventoryService.DeductInventoryForOrderItemAsync(notification.OrderItemId);
    }
    ```
  - *Quan sát thực chứng:* Đây là **ĐIỂM KÍCH HOẠT DUY NHẤT** trong toàn bộ solution để thực hiện trừ kho BOM tự động.
- **Thanh toán đơn hàng (Payment Completed)**:
  - Tệp: `src/Restaurant.Infrastructure/Services/PaymentService.cs`
  - Dòng 214-215 (trong `ProcessSepayWebhookAsync`):
    ```csharp
    // OrderPaidDomainEvent removed (BOM Inventory Deduction is now handled by Kitchen Item Done event)
    ```
  - Dòng 236-370 (`ProcessCashPaymentAsync`): Chỉ thực hiện kiểm tra số tiền, cập nhật `Payment`, đổi `order.PaymentStatus = Paid`, `order.Status = Completed`, tích điểm Loyalty và phát thông báo SignalR.
  - *Quan sát thực chứng:* Nghiệp vụ thanh toán hoàn toàn không can thiệp vào tồn kho.
- **Cấp độ trừ kho**:
  - Tệp: `src/Restaurant.Infrastructure/Services/InventoryService.cs`
  - Dòng 740: `public async Task DeductInventoryForOrderItemAsync(int orderItemId)`
  - *Quan sát thực chứng:* Trừ kho được thực thi ở cấp độ từng món ăn (`OrderItem`), sử dụng `orderItem.MenuItemId` và danh sách `orderItem.OrderItemModifiers`. Combo Header bị bỏ qua ở `KitchenService.cs:223`, các món con trong combo (`ChildItems`) được trừ độc lập khi từng món con hoàn thành.

---

#### 1.2. Tích Hợp SignalR, Domain Events & Rủi Ro Giao Dịch
- **Cơ chế thực thi Domain Event**:
  - Tệp: `src/Restaurant.WebAPI/Program.cs`
  - Dòng 59:
    ```csharp
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Restaurant.Infrastructure.Services.InventoryService).Assembly));
    ```
  - *Quan sát thực chứng:* MediatR được đăng ký mặc định trong tiến trình (In-Process Synchronous). Toàn bộ solution không có bất kỳ cấu hình bảng Outbox (`OutboxMessages`), không có thư viện MassTransit/RabbitMQ/Kafka, và chỉ có duy nhất một `BackgroundService` là `KitchenSlaMonitoringWorker.cs` (kiểm tra SLA quá hạn 15 phút).
- **Lỗ hổng Phi Nguyên Tử (Two-Phase Split Save)**:
  - Tệp: `src/Restaurant.Infrastructure/Services/KitchenService.cs`:
    - Dòng 201: `await _context.SaveChangesAsync();` -> Commit trạng thái `OrderItem.KitchenStatus = Done` vào cơ sở dữ liệu.
    - Dòng 226: `await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));` -> Gọi MediatR.
  - Tệp: `src/Restaurant.Infrastructure/Services/InventoryService.cs`:
    - Dòng 835: `await _context.SaveChangesAsync();` -> Commit các dòng `InventoryTransaction` và trừ `Material.CurrentStock`.
  - *Quan sát thực chứng:* Hai lần `SaveChangesAsync` nằm ở hai transaction hoàn toàn tách biệt. Nếu lần lưu ở dòng 835 bị thất bại (do lỗi concurrency `Material.RowVersion` hoặc đứt kết nối), món ăn vẫn mang trạng thái `Done` trong DB nhưng tồn kho nguyên liệu không bị khấu trừ.
  - Tệp: `src/Restaurant.Infrastructure/Services/KitchenService.cs`:
    - Dòng 131:
      ```csharp
      if (oldStatus == newStatus)
      {
          var existingWaitingTime = (utcNow - (item.SentToKitchenAt ?? item.CreatedAt)).TotalMinutes;
          return new KitchenItemDto { ... };
      }
      ```
  - *Quan sát thực chứng:* Khi KDS thử gửi lại request cập nhật trạng thái `Done`, kiểm tra `oldStatus == newStatus` sẽ nhận thấy món đã là `Done` và return ngay lập tức. Event trừ kho bị **MẤT HOÀN TOÀN VÀ VĨNH VIỄN**.
- **Cơ chế chống trừ trùng (Idempotency Guards)**:
  - **Tầng UI**: `src/Restaurant.Web/Components/Pages/Kds/KdsMain/KdsDashboard.razor.cs:158`:
    `if (ProcessingItems.Contains(orderItemId)) return;` -> Chặn double click trên cùng 1 phiên trình duyệt.
  - **Tầng Kitchen Status**: `KitchenService.cs:131`:
    `if (oldStatus == newStatus) return;`
  - **Tầng Inventory Service**: `InventoryService.cs:743-746`:
    ```csharp
    var alreadyDeducted = await _context.InventoryTransactions
        .AnyAsync(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId == orderItemId && t.TransactionType == (int)InventoryTransactionType.SaleDeduction);
    if (alreadyDeducted) return;
    ```
- **Lỗ hổng Race Condition Đồng Thời (Concurrent Execution)**:
  - Tệp: `src/Restaurant.Domain/Entities/OrderAndPaymentEntities.cs:35-59` (`OrderItem`):
    *Quan sát thực chứng:* Entity `OrderItem` **KHÔNG CÓ** thuộc tính `RowVersion` (Concurrency Token).
  - Tệp: `src/Restaurant.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`:
    *Quan sát thực chứng:* Bảng `InventoryTransaction` **KHÔNG CÓ** Unique Index kết hợp `(ReferenceType, ReferenceId, TransactionType)`.
  - Tệp: `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs:60`:
    `Material` có `public byte[] RowVersion { get; set; } = Array.Empty<byte>();`
  - *Quan sát thực chứng:* Nếu 2 request đồng thời từ 2 màn hình bếp cùng hoàn thành 2 món ăn có chung nguyên liệu (hoặc cùng 1 món do race condition mạng), cả 2 request cùng đọc `material.CurrentStock`. Request commit trước sẽ thành công; request commit sau sẽ bị EF Core ném `DbUpdateConcurrencyException`. Vì `InventoryService.cs` không có khối try-catch cho lỗi concurrency, request này sẽ văng lỗi **HTTP 500 Internal Server Error**.

---

#### 1.3. Ma Trận Hủy Món & Hủy Đơn (Void / Cancel Matrix)
- **Hủy toàn bộ đơn hàng (`CancelOrderAsync`)**:
  - Tệp: `src/Restaurant.Infrastructure/Services/CancelOrderService.cs`
  - Dòng 86-96:
    ```csharp
    // 1. Rollback Inventory (find any BOM deducted via OrderItems for this Order)
    var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
    var saleDeductions = await _context.InventoryTransactions
        .Where(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId.HasValue && orderItemIds.Contains(t.ReferenceId.Value) && t.TransactionType == (int)InventoryTransactionType.SaleDeduction)
        .ToListAsync();

    foreach (var deduction in saleDeductions)
    {
        deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;
        var wasteText = string.Format(_localizer["CANCELLATION_WASTE_NOTE"] ?? "[WASTE] Cancelled Order {0} | Origin: {1}", order.OrderCode, deduction.Note);
        deduction.Note = wasteText;
    }
    ```
  - *Quan sát thực chứng:* Khi hủy cả đơn hàng, hệ thống tìm toàn bộ giao dịch `SaleDeduction` của các món trong đơn và chuyển đổi thành `CancellationWaste` (`TransactionType = 5`). Tồn kho vật lý `CurrentStock` không được hoàn lại (ghi nhận là hao hụt/lãng phí chế biến).
- **Hủy một món lẻ trong đơn (`VoidOrderItemAsync`) — LỖ HỔNG P0**:
  - Tệp: `src/Restaurant.Infrastructure/Services/PosOrderService.cs`
  - Dòng 924-972:
    ```csharp
    // 1. Mark target item as voided
    targetItem.Status = (int)OrderItemStatus.Voided;
    _context.VoidLogs.Add(new VoidLog { ... });

    // 2. If target item is a Combo Header, void all its child items
    ...
    // 3. Recalculate Order SubTotal and TotalAmount
    order.SubTotal = order.OrderItems
        .Where(oi => oi.Status != (int)OrderItemStatus.Voided)
        .Sum(oi => oi.UnitPrice * oi.Quantity);
    order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount);

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    ```
  - *Quan sát thực chứng:* Phương thức `VoidOrderItemAsync` **HOÀN TOÀN KHÔNG TRUY VẤN BẢNG `InventoryTransactions`**, không kiểm tra món đã nấu xong chưa, và không chuyển đổi `SaleDeduction` sang `CancellationWaste`.
  - Nếu món ăn đã được bếp làm xong (`KitchenStatus == Done`), nguyên liệu đã bị xuất kho dưới dạng `SaleDeduction`. Khi Thu ngân void món, khách không phải trả tiền (`TotalAmount` bị trừ), nhưng kho vẫn ghi nhận đây là xuất bán hàng hợp lệ (`SaleDeduction`).
  - Tệp: `src/Restaurant.Domain.Tests/E2E/Tier1_FeatureCoverageTests.cs:558-582`:
    ```csharp
    [Fact]
    public async Task F24_R5_03_VoidItemCancellationWaste_ConvertsVoidedItemDeductionsToWaste()
    {
        // ... tạo đơn có Pizza và CocaCola
        var voided = await ctx.PosOrderService.VoidOrderItemAsync(order.Id, cokeItem.Id, "Khách đổi nước suối", ctx.CashierUser.Id);
        Assert.True(voided);
        Assert.Equal(180000m, updatedOrder.SubTotal); // only Pizza Beef remaining
    }
    ```
  - *Quan sát thực chứng:* Test mang tên kiểm tra `ConvertsVoidedItemDeductionsToWaste` nhưng không hề gọi bếp làm xong món Coke, không hề kiểm tra bảng `InventoryTransactions`, và chỉ assert `SubTotal`. Đây là một test không đầy đủ làm che giấu lỗi P0 trong suốt thời gian qua.
- **Lỗ hổng 403 Forbidden khi Thu Ngân dùng PIN Quản Lý — LỖ HỔNG P0**:
  - Tệp: `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs`
  - Dòng 207:
    ```csharp
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(...)
    ```
  - Dòng 253:
    ```csharp
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("orders/{orderId}/items/{orderItemId}/void")]
    public async Task<IActionResult> VoidOrderItem(...)
    ```
  - Tệp: `src/Restaurant.Web/Components/Pages/Pos/Components/PosCancelOrderModal.razor`
  - Dòng 104-111: Thu ngân nhập mã PIN Quản lý -> Giao diện gọi `AuthApiClient.VerifyManagerPinAsync(ManagerPin)` -> Kết quả trả về `true` (PIN hợp lệ) -> Gọi `OnOrderCancelled.InvokeAsync(CancelReason)`.
  - Tệp: `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs`
  - Dòng 1111-1118:
    ```csharp
    var request = new CancelOrderRequest { OrderId = ActiveOrderId, Reason = reason };
    await Http.PostAsJsonAsync($"api/v1/pos/orders/{ActiveOrderId}/cancel", request);
    CartItems.Clear();
    _ = ShowToastAsync("Đã hủy đơn hàng thành công!");
    await Task.Delay(1000);
    HandleHeaderBackNav();
    ```
  - *Quan sát thực chứng:* `Http` gửi request với Bearer token của người dùng đang đăng nhập là **Thu ngân (`Role = Cashier`)**. WebAPI từ chối ngay lập tức với mã lỗi **HTTP 403 Forbidden**. Vì `PosMain.razor.cs` không kiểm tra `response.IsSuccessStatusCode`, nó tự động xóa giỏ hàng và báo cho nhân viên "Đã hủy đơn hàng thành công!", nhưng trong cơ sở dữ liệu đơn hàng **KHÔNG HỀ BỊ HỦY**!
  - Tương tự tại dòng 938-955 cho void món: Gọi API void với token Thu ngân, nhận HTTP 403, hiển thị thông báo lỗi "Không thể hủy món trên server" dù đã nhập đúng PIN Quản lý.
- **Hủy đơn qua BOH Approval Center bỏ qua toàn bộ nghiệp vụ**:
  - Tệp: `src/Restaurant.Infrastructure/Services/ApprovalService.cs`
  - Dòng 348-365:
    ```csharp
    case ApprovalType.CancelOrder:
        if (req.ReferenceId.HasValue)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == req.ReferenceId.Value);
            if (order != null && order.Status != (int)OrderStatus.Cancelled)
            {
                order.Status = (int)OrderStatus.Cancelled;
                if (order.Table != null) order.Table.Status = (int)TableStatus.Available;
            }
        }
        break;
    ```
  - *Quan sát thực chứng:* Bỏ qua hoàn toàn việc gọi `CancelOrderService`. Không chuyển `SaleDeduction` -> `CancellationWaste`, không tạo bản ghi `Refund`, không rollback điểm Loyalty và không phát SignalR cho bếp/bàn.
- **Báo cáo Hao hụt kho sử dụng công thức giả lập (35%)**:
  - Tệp: `src/Restaurant.Infrastructure/Services/InventoryService.cs`
  - Dòng 733-734:
    ```csharp
    TotalVarianceAmount = totalLoss,
    TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0),
    ```
  - Tệp: `src/Restaurant.Web/Components/Pages/Admin/Reports/InventoryLossReport.razor`
  - Dòng 28-30:
    ```razor
    <div class="text-xs font-bold text-on-surface-variant uppercase">@L["Report_InventoryLoss_WasteTitle"]</div>
    <div class="font-headline-sm text-2xl font-bold text-error mt-2">- @_summary.TotalWasteAmount.ToString("N0")đ</div>
    ```
  - *Quan sát thực chứng:* Giá trị Hao hụt hủy món trên báo cáo BOH Admin là một con số nhân bừa 35% từ chênh lệch kiểm kê (`StocktakeItems`). Bảng `InventoryTransactions` chứa các bản ghi `CancellationWaste` không hề được truy vấn trong báo cáo này.
- **Nghiệp vụ Hoàn kho (`CancellationReturn`)**:
  - Tệp: `src/Restaurant.Domain/Enums/Enums.cs:120`:
    `CancellationReturn = 4`
  - *Quan sát thực chứng:* Toàn bộ solution chỉ xuất hiện chữ `CancellationReturn` duy nhất 1 lần tại định nghĩa Enum. Không có logic nào hỗ trợ hoàn nguyên liệu về kho khi đơn bị hủy.

---

#### 1.4. Gọi Thêm Món (Add-On Items & Multiple Rounds)
- **Cập nhật đơn hàng vs Tạo mới**:
  - Tệp: `src/Restaurant.Infrastructure/Services/PosOrderService.cs`
  - Dòng 431-449:
    ```csharp
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
        ...
    }
    ```
  - *Quan sát thực chứng:* Hệ thống tái sử dụng đơn hàng hiện tại của bàn, không tạo đơn hàng mới.
- **Khấu trừ kho cho các món gọi thêm**:
  - Dòng 501-562: Các món mới trong request được tạo thành các bản ghi `OrderItem` mới với `KitchenStatus = (int)KitchenStatus.Pending`.
  - Dòng 695-724: Chỉ có danh sách `newItemsForKitchen` (các món mới) được đóng gói vào `KitchenTicketDto` để gửi xuống bếp.
  - Tồn kho của các món cũ đã được trừ trước đó khi bếp làm xong đợt 1 (với `ReferenceId = oldOrderItem.Id`). Các món mới có `Id` riêng biệt, chỉ bị trừ kho khi bếp bấm "Xong" cho từng món mới đó.
  - *Quan sát thực chứng:* **Hoàn toàn không có hiện tượng trừ lặp lại các món cũ trong đơn**.
- **Lỗi trạng thái đơn hàng khi gọi thêm**:
  - Tệp: `src/Restaurant.Infrastructure/Services/PosOrderService.cs:440-449 & 681-692`
  - *Quan sát thực chứng:* Nếu bàn đã hoàn thành đợt 1, `Order.Status` đang là `Ready` (Sẵn sàng phục vụ). Khi khách gọi thêm món, code không hề cập nhật `targetOrder.Status` quay trở lại `InPreparation`. Đơn hàng vẫn hiển thị `Ready` trên POS dù các món mới thêm đang chờ nấu trong bếp.
- **Bảo vệ giỏ hàng POS UI**:
  - Tệp: `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor`
  - Dòng 110-125: Nút tăng/giảm số lượng bị ẩn hoàn toàn đối với món đã gửi bếp (`@if (!item.IsSentToKitchen)`).
  - Tệp: `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs`
  - Dòng 558-583: Khi chọn lại món từ thực đơn, kiểm tra `!i.IsSentToKitchen`. Vì món cũ đã có `IsSentToKitchen = true`, hệ thống tạo một dòng mới độc lập với `Quantity = 1` và `IsSentToKitchen = false`.

---

#### 1.5. Lỗ Hổng Quy Đổi Đơn Vị Tính Trong Khấu Trừ BOM (Unit Mismatch)
- Tệp: `src/Restaurant.Infrastructure/Services/InventoryService.cs`
- Dòng 774-782 & 789-797:
  ```csharp
  foreach (var ri in recipe.RecipeItems)
  {
      var needed = ri.Quantity * orderItem.Quantity;
      if (materialDeductions.ContainsKey(ri.MaterialId))
          materialDeductions[ri.MaterialId] += needed;
      else
          materialDeductions[ri.MaterialId] = needed;
  }
  ...
  material.CurrentStock -= deductQty;
  ```
- *Quan sát thực chứng:* `RecipeItem` có trường `UnitOfMeasureId` (`src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs:93`), và hệ thống có bảng `UnitConversion`. Tuy nhiên, trong logic khấu trừ kho, hệ thống **hoàn toàn không tra cứu bảng quy đổi đơn vị**.
- Nếu nguyên liệu `Thịt bò` có đơn vị kho là `kg`, nhưng công thức định lượng món `Bò bít tết` khai báo `150` đơn vị là `g` (gram): hệ thống sẽ lấy `150 * 1 = 150` và trừ thẳng `material.CurrentStock -= 150`, làm âm kho 150kg thay vì chỉ trừ 0.150kg.

---

### 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. **Khấu trừ kho BOM phụ thuộc 100% vào sự kiện KDS Done**:
   - Từ Quan sát 1.1: Tạo đơn không trừ kho, gửi bếp không trừ kho, thanh toán không trừ kho.
   - Do đó: Bất kỳ sản phẩm nào không đi qua trạm KDS (món quầy pha chế không bật KDS, đồ uống đóng chai lấy tại tủ mát, đồ bán mang về đóng gói sẵn), hoặc các trường hợp nhân viên bếp quên thao tác bấm "Xong" trên màn hình KDS, nguyên liệu/hàng hóa sẽ **hoàn toàn không được ghi nhận xuất kho**. Sổ kho sẽ bị lệch dương lớn so với thực tế bán hàng.

2. **Rủi ro mất dữ liệu xuất kho do thiếu Outbox và tách Transaction**:
   - Từ Quan sát 1.2: `KitchenService` gọi `SaveChangesAsync` lưu `KitchenStatus = Done` trước, sau đó mới phát `OrderItemKitchenDoneDomainEvent` đồng bộ qua MediatR để `InventoryService` gọi `SaveChangesAsync` lần 2.
   - Khi có 2 trạm bếp cùng thao tác hoặc mạng chập chờn, lần lưu thứ 2 gặp `DbUpdateConcurrencyException` do xung đột `Material.RowVersion`.
   - Kết quả: Lần lưu 1 đã ghi nhận món xong, nhưng lần lưu 2 thất bại và rollback giao dịch trừ kho.
   - Vì `KitchenService.cs:131` chặn `oldStatus == newStatus`, lần gọi sau bị từ chối và không bao giờ bắn lại event. Xuất kho bị mất vĩnh viễn mà không để lại vết kiểm toán.

3. **Sai lệch nghiêm trọng giữa doanh thu và chi phí khi void món lẻ**:
   - Từ Quan sát 1.3: Khi hủy cả đơn hàng (`CancelOrderService`), giao dịch xuất kho được chuyển sang `CancellationWaste`. Nhưng khi hủy từng món lẻ (`VoidOrderItemAsync`), hệ thống chỉ trừ tiền hóa đơn và ghi `VoidLog`, hoàn toàn không đổi `TransactionType` của xuất kho.
   - Kết quả: Doanh thu của món bị xóa bỏ (khách không trả tiền), nhưng chi phí nguyên liệu vẫn nằm ở tài khoản xuất bán hàng thông thường (`SaleDeduction`), làm sai lệch toàn bộ báo cáo giá vốn hàng bán (COGS) và báo cáo hao hụt.

4. **Bế tắc nghiệp vụ tại quầy POS do cấu hình Authorization sai**:
   - Từ Quan sát 1.3: Endpoint `/api/v1/pos/orders/{id}/cancel` và `/void` yêu cầu quyền `Admin,Manager`.
   - Trên giao diện POS, Thu ngân mở modal nhập mã PIN Quản lý. Backend chỉ có endpoint kiểm tra PIN đúng/sai trả về `boolean`, không phát sinh Token/Ticket ủy quyền của Quản lý.
   - Sau đó, Web client dùng HttpClient có sẵn (gắn Token của Thu ngân) gọi API hủy -> Backend trả về HTTP 403 Forbidden.
   - Tệ hơn nữa: hàm `HandleOrderCancelledAsync` không kiểm tra `response.IsSuccessStatusCode`, vẫn xóa giỏ hàng và hiện thông báo thành công. Đơn hàng thực chất vẫn tồn tại trên server, bàn không được giải phóng thực sự trong database.

5. **Số liệu báo cáo BOH Admin bị bóp méo**:
   - Từ Quan sát 1.3: Dữ liệu `CancellationWaste` ghi nhận từ `CancelOrderService` không được sử dụng. `InventoryService.GetInventoryLossSummaryAsync` tự động nhân 35% trên số liệu kiểm kê. Báo cáo quản trị không phản ánh thực tế thất thoát.

---

### 3. CAVEATS (GIỚI HẠN & GIẢ ĐỊNH)

1. **Giới hạn môi trường điều tra**:
   - Đợt kiểm toán này tuân thủ tuyệt đối ràng buộc READ-ONLY. Không chạy migration, không sửa code, không reset cơ sở dữ liệu.
2. **Giả định về KDS**:
   - Giả định rằng toàn bộ các trạm bếp đều sử dụng giao diện web KDS tại `/kds`. Chưa đánh giá trường hợp nhà hàng in phiếu Order ra máy in nhiệt bếp (Kitchen ESC/POS printer) mà không có màn hình cảm ứng KDS.
3. **Món Combo**:
   - Các món Combo Header không được trừ kho (hợp lý). Từng món con trong Combo được trừ kho khi hoàn thành. Giả định rằng không có trường hợp món Combo lại lồng thêm một món Combo khác bên trong (đã được chặn ở `PosOrderService.cs:374`).

---

### 4. CONCLUSION (KẾT LUẬN & KIẾN NGHỊ KHẮC PHỤC)

Hệ thống POS/KDS hiện tại đã hình thành khung luồng nghiệp vụ tương đối đầy đủ về mặt giao diện và xử lý luồng thuận (Happy Path). Tuy nhiên, **tính toàn vẹn dữ liệu kho và an toàn giao dịch đang có các lỗ hổng nghiêm trọng ở mức độ P0 và P1**:
- **Lỗ hổng P0 - 403 Forbidden nuốt lỗi khi hủy đơn/món bằng PIN Quản lý**: Khiến thao tác của thu ngân tại quầy bị từ chối ngầm, UI báo thành công ảo.
- **Lỗ hổng P0 - Thiếu đồng bộ xuất kho khi Void món lẻ**: Gây mất cân đối nghiêm trọng giữa dòng tiền và sổ cái kho.
- **Lỗ hổng P0 - Không áp dụng bảng quy đổi đơn vị tính (Unit Conversion) khi trừ BOM**: Tiềm ẩn nguy cơ âm kho hàng trăm lần.
- **Lỗ hổng P0 - Kiến trúc trừ kho 2 pha không có Outbox**: Nguy cơ mất khấu trừ kho khi có lỗi đồng thời giữa các bếp.

#### Kế hoạch kiến nghị khắc phục:
1. **Khắc phục Authorization cho PIN Quản lý (P0)**:
   - Sửa endpoint `CancelOrder` và `VoidOrderItem` tại `PosOrdersController.cs`: Cho phép `Roles = "Admin,Manager,Cashier"`.
   - Nếu người gọi là `Cashier`, bắt buộc payload request phải chứa `ManagerPin` (hoặc `ApprovalRequestId`). Backend xác thực mã PIN qua `IdentityService.VerifyManagerPinAsync` ngay trong controller trước khi thực hiện hủy.
2. **Hoàn thiện hàm `VoidOrderItemAsync` (P0)**:
   - Trong `PosOrderService.VoidOrderItemAsync`, bổ sung truy vấn tìm các `InventoryTransaction` có `ReferenceType == OrderItem && ReferenceId == targetItem.Id && TransactionType == SaleDeduction`.
   - Chuyển đổi các bản ghi này sang `InventoryTransactionType.CancellationWaste` (hoặc `CancellationReturn` nếu có cờ hoàn kho từ UI).
3. **Đưa việc trừ kho BOM vào quy đổi đơn vị tính chuẩn (P0)**:
   - Trong `InventoryService.DeductInventoryForOrderItemAsync`, tích hợp bảng `UnitConversion` để chuẩn hóa số lượng từ `RecipeItem.UnitOfMeasureId` về `Material.UnitOfMeasureId` trước khi trừ tồn kho.
4. **Bảo đảm tính nguyên tử cho KDS Done (P0/P1)**:
   - Gộp việc cập nhật `KitchenStatus = Done` và việc trừ kho BOM vào chung một Database Transaction duy nhất, hoặc triển khai bảng `OutboxMessages` với background worker để đảm bảo giao dịch không bị mất.
5. **Chuẩn hóa báo cáo Hao hụt kho BOH (P1)**:
   - Xóa bỏ công thức nhân cứng 35% tại `InventoryService.cs:734`. Viết truy vấn SUM số tiền từ bảng `InventoryTransactions` với `TransactionType == InventoryTransactionType.CancellationWaste`.
6. **Bổ sung cơ chế Hoàn kho (`CancellationReturn`) & Trừ kho món quầy (P2)**:
   - Cho phép Quản lý chọn "Hao hụt (Bỏ thùng rác)" hay "Hoàn lại kho" khi hủy món.
   - Thêm cơ chế tự động trừ kho cho các món không qua bếp khi đơn hàng hoàn tất thanh toán.

---

### 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC THỰC ĐỘC LẬP)

Để kiểm chứng độc lập các phát hiện trên mà không làm thay đổi codebase:

1. **Xác minh lỗi 403 Forbidden khi Void món bằng PIN**:
   - Sử dụng Postman hoặc integration test: Đăng nhập tài khoản Cashier (`cashier / 123456`), lấy JWT token.
   - Gửi request `POST /api/v1/pos/orders/1/items/1/void` kèm Bearer token của Cashier.
   - *Kết quả mong đợi:* Nhận mã HTTP 403 Forbidden.
2. **Xác minh lỗi Void món không đổi CancellationWaste**:
   - Mở `src/Restaurant.Domain.Tests/E2E/Tier1_FeatureCoverageTests.cs`.
   - Xem bài test `F24_R5_03_VoidItemCancellationWaste_ConvertsVoidedItemDeductionsToWaste` (dòng 558). Thêm dòng code gọi `ctx.KitchenService.UpdateItemStatusAsync(cokeItem.Id, Done)` trước khi void và kiểm tra `tx.TransactionType == CancellationWaste`. Test sẽ FAIL vì `VoidOrderItemAsync` không hề đổi type.
3. **Xác minh công thức FAKE 35% của báo cáo hao hụt**:
   - Kiểm tra trực tiếp dòng 734 file `src/Restaurant.Infrastructure/Services/InventoryService.cs`:
     `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0),`
4. **Lệnh chạy kiểm thử hiện tại**:
   ```bash
   dotnet test "src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj"
   ```
   Xác minh các test hiện tại không kiểm thử sâu vào các nhánh ngoại lệ và race conditions này.
