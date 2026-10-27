# BÁO CÁO AUDIT TOÀN DIỆN: NHẬP KHO, XUẤT KHO, KIỂM KÊ VÀ STOCKLEDGER (AGENT 2)

> **Dự án:** Hệ thống Quản lý Nhà hàng Pizza (POS, KDS, BOH Admin & QR Ordering)  
> **Workspace:** `d:\My Project` (`src/Restaurant.sln`)  
> **Kiểm toán viên:** Agent 2 — Senior Software Architect, Inventory Management Specialist & StockLedger Auditor  
> **Thời điểm:** 2026-10-14T17:30:00Z  
> **Trạng thái tuân thủ:** Read-only investigation. Tuyệt đối không can thiệp code/migration/database.

---

## TỔNG QUAN ĐÁNH GIÁ (EXECUTIVE SUMMARY)

Module Quản lý kho, Phiếu biến động kho và Sổ cái tồn kho (StockLedger) hiện tại trong codebase đạt mức độ phát triển ban đầu nhưng **chưa đạt tiêu chuẩn vận hành kế toán kho và an toàn giao dịch thực tế**. 
Mặc dù hệ thống đã có giao diện quản trị Blazor khá trau chuốt cho Phiếu Nhập Kho (`GoodsReceiptManagement.razor`), Kiểm kê kho 2 bước (`StocktakeManagement.razor`), Danh mục nguyên vật liệu (`MaterialManagement.razor`), và Báo cáo thất thoát (`InventoryLossReport.razor`), tuy nhiên phía Backend và Cơ sở dữ liệu tồn tại **nhiều khoảng trống kiến trúc và lỗi nghiệp vụ nghiêm trọng (Critical P0 & High P1)**:

1. **Phân hệ Xuất kho (Goods Issue / Transfer / Scrap) hoàn toàn chưa phát triển (NOT_STARTED - P1):** Không có Entity, không có API và không có UI để ghi nhận xuất hủy, xuất đổi trả nhà cung cấp, hoặc xuất luân chuyển kho nội bộ.
2. **Sổ cái biến động kho (`InventoryTransactions`) bị phá vỡ tính bất biến (BROKEN - P0):** Trong `CancelOrderService.cs:93`, khi đơn bị hủy, hệ thống thực hiện **UPDATE trực tiếp** trường `TransactionType` của dòng lịch sử cũ từ `SaleDeduction` sang `CancellationWaste` thay vì ghi nhận một bút toán đảo (reversal) hoặc bút toán hao hụt mới. Ngoài ra bảng này không lưu đơn giá vốn (`UnitPrice`), không lưu thành tiền (`TotalCost`), không lưu số dư trước (`BalanceBefore`), và hoàn toàn không có API/UI nào để người quản trị hoặc kế toán tra cứu sổ cái.
3. **Lỗ hổng mất dấu trừ kho vĩnh viễn trên KDS khi có Concurrency Conflict (BROKEN - P0):** Việc trừ kho nguyên liệu BOM được kích hoạt bất đồng bộ qua Domain Event `OrderItemKitchenDoneDomainEvent`. Sau khi cập nhật trạng thái món thành `Done` và gọi `SaveChangesAsync()`, event mới được phát. Nếu quá trình trừ kho gặp lỗi `DbUpdateConcurrencyException` do tranh chấp `Material.RowVersion`, giao dịch trừ kho bị văng lỗi. Nếu KDS bấm retry gửi lại trạng thái `Done`, logic tại `KitchenService.cs:220` kiểm tra `oldStatus != Done` sẽ trả về `false`, khiến hệ thống **vĩnh viễn bỏ qua việc trừ kho cho món ăn đó**.
4. **Báo cáo thất thoát sử dụng công thức giả định (BROKEN - P1):** Tại `InventoryService.cs:734`, trường `TotalWasteAmount` trong báo cáo hao hụt được tính bằng công thức hardcoded: `Math.Round(totalLoss * 0.35m, 0)` (35% tổng chênh lệch kiểm kê) thay vì tổng hợp dữ liệu thực từ các bút toán `CancellationWaste` trong sổ cái.
5. **Cơ chế chốt kiểm kê đè số dư và triệt tiêu biến động bán hàng trong ngày (BROKEN - P1):** `ReconcileStocktakeAsync` không kiểm tra trạng thái phiếu đã chốt hay chưa (cho phép gọi nhiều lần), đồng thời gán đè trực tiếp `material.CurrentStock = finalActual`. Nếu kiểm đếm mù thực hiện đầu ngày và chốt cuối ngày, toàn bộ lượng nguyên liệu tiêu hao do bán hàng trong khoảng thời gian này sẽ bị xóa sạch khỏi số dư tồn kho.
6. **Không áp dụng quy đổi đơn vị (UnitConversion) trong Nhập kho và Trừ kho (BROKEN - P1):** Bảng `UnitConversions` chỉ phục vụ CRUD tĩnh; khi nhập hàng 10 thùng/bao hoặc trừ kho theo gram, hệ thống cộng/trừ trực tiếp số lượng thô vào `CurrentStock` mà không nhân hệ số quy đổi, vi phạm trực tiếp tiêu chí đặc tả `AC-BOH-009.1`.
7. **Thiếu cơ chế Idempotency & Khóa chống cộng/trừ trùng kho (BROKEN - P1):** Toàn bộ API nhập kho `POST /api/v1/inventory/purchase-orders` và chốt kiểm kê `POST /api/v1/inventory/stocktakes/{id}/reconcile` không có Idempotency Key, không có Unique Constraint ở database, dễ dàng bị nhân đôi số lượng khi double-click hoặc mạng retry.

---

## MA TRẬN ĐÁNH GIÁ CHỨC NĂNG (FEATURE AUDIT MATRIX)

| Mã | Chức năng nghiệp vụ | Phân loại | Ưu tiên | Bằng chứng Code (File & Line) | Tóm tắt tình trạng |
|:---:|---|:---:|:---:|---|---|
| **F01** | Tạo Phiếu Nhập Kho (Goods Receipt / PO) | **PARTIAL** | **P1** | `InventoryService.cs:121-190`, `InventoryController.cs:79-107`, `GoodsReceiptManagement.razor:571-617` | Tạo phiếu ghi nhận tức thì vào kho; thiếu hoàn toàn quy trình phê duyệt (Draft/Approved), không thể hủy/sửa phiếu, không có Idempotency. |
| **F02** | Xem Danh sách & Chi tiết Phiếu Nhập | **DONE_VERIFIED** | **P2** | `InventoryService.cs:398-467`, `InventoryController.cs:234-261`, `GoodsReceiptManagement.razor:100-132` | Hiển thị danh sách, xem chi tiết, giao diện in ấn phiếu nhập. Tuy nhiên API mở `[AllowAnonymous]`. |
| **F03** | Quy trình Xuất kho (Goods Issue / Transfer / Scrap) | **NOT_STARTED** | **P1** | Toàn bộ codebase không có `GoodsIssue` | Chưa hề được thiết kế hay cài đặt. Không có xuất kho hủy hàng, không có xuất trả NCC, không có luân chuyển kho. |
| **F04** | Kiểm kê kho 2 bước (Blind Stocktake -> Reconcile) | **PARTIAL** | **P1** | `InventoryService.cs:540-648`, `InventoryController.cs:293-331`, `StocktakeManagement.razor` | Có UI đếm mù và màn hình QL đối soát; nhưng thiếu kiểm tra khóa phiếu đã chốt, đè bẹp số dư bán hàng trong ngày. |
| **F05** | Điều chỉnh tồn kho tức thời (Stock Audit direct) | **PARTIAL** | **P2** | `InventoryService.cs:192-256`, `InventoryController.cs:109-143` | Endpoint cũ ghi đè trực tiếp `CurrentStock`, chạy song song với quy trình kiểm kê 2 bước gây phân mảnh logic. |
| **F06** | Sổ cái biến động kho (`InventoryTransactions`) | **BROKEN** | **P0** | `CustomerAndInventoryEntities.cs:179-193`, `CancelOrderService.cs:87-96` | Bị vi phạm tính bất biến (sửa trực tiếp bản ghi cũ); thiếu trường `UnitPrice`, `TotalCost`, `BalanceBefore`; không có API/UI tra cứu. |
| **F07** | Chống cộng/trừ trùng kho (Idempotency Key & Lock) | **BROKEN** | **P1** | `InventoryController.cs:79`, `InventoryService.cs:743-747` | Không có Idempotency Key trên API; không có Unique Constraint DB chống trừ trùng giữa các thread đồng thời. |
| **F08** | Chính sách tồn kho âm (Negative Stock Policy) | **NEEDS_BUSINESS_DECISION** | **P1** | `InventoryService.cs:814`, `InventoryService.cs:160` | Cho phép âm vô hạn độ mà không có cấu hình chặn/cảnh báo; tính giá vốn bình quân khi âm coi tồn cũ = 0. |
| **F09** | Trừ kho tự động BOM khi nấu xong (KDS Kitchen Done) | **BROKEN** | **P0** | `KitchenService.cs:201-228`, `DeductInventoryOnOrderItemKitchenDoneHandler.cs:16-19`, `InventoryService.cs:740-841` | Trừ kho ngoài transaction chính; nếu concurrency conflict thì mất hẳn lượt trừ kho khi KDS retry. |
| **F10** | Xử lý hoàn kho / hao hụt khi Hủy món (Void Item) | **BROKEN** | **P1** | `PosOrderService.cs:896-994` (`VoidOrderItemAsync`) | Chỉ đổi trạng thái `Voided` và thêm `VoidLogs`; hoàn toàn BỎ QUA việc xử lý hoàn kho hoặc ghi nhận hao hụt nguyên liệu đã nấu. |
| **F11** | Báo cáo hao hụt & thất thoát kho (Loss & Waste Report) | **BROKEN** | **P1** | `InventoryService.cs:703-738`, `InventoryLossReport.razor:21-32` | Dữ liệu `TotalWasteAmount` bị gán cứng bằng công thức ảo `totalLoss * 0.35m`; không đối soát dữ liệu hủy đơn thực tế. |
| **F12** | Quy đổi đơn vị tính trong luồng kho (Unit Conversion) | **BROKEN** | **P1** | `InventoryService.cs:151, 814`, `EntityConfigurations.cs:208-216`, Migration `InitialCreate.cs:345` | Chỉ có CRUD tĩnh; hoàn toàn không áp dụng khi Nhập/Trừ kho; độ chính xác DB `decimal(18,2)` làm tròn cụt các tỷ lệ thập phân nhỏ. |
| **F13** | Liên kết Nhà cung cấp & Nguyên vật liệu | **BROKEN** | **P2** | `InventoryService.cs:659-698` (`GetMaterialsBySupplierIdAsync`) | Dùng chuỗi if-else so khớp tên nhà cung cấp bằng từ khóa tiếng Việt hardcoded thay vì quan hệ khóa ngoại trên Database. |
| **F14** | Độ bao phủ kiểm thử (Test Suite Coverage) | **PARTIAL** | **P2** | `Tier1_FeatureCoverageTests.cs:585-618` | Chỉ có duy nhất 1 test case cơ bản (33 dòng) cho PO và Audit; 0 test cho Blind Stocktake, 0 test cho Concurrency, 0 test cho Loss Report. |

---

## 1. COMPONENT 1: OBSERVATIONS (BẰNG CHỨNG THỰC CHỨNG TỪNG DÒNG MÃ)

### 1.1. Vòng đời Phiếu Nhập Kho (Goods Receipt): Thiếu hoàn toàn State Machine và Quy trình duyệt

Tại [`CustomerAndInventoryEntities.cs#L126-L137`](file:///d:/My%20Project/src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs#L126-L137):
```csharp
public class GoodsReceipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
}
```
*Quan sát thực tế:*
- `GoodsReceipt` **không có cột `Status`**. Không có các trạng thái nghiệp vụ chuẩn như: `Draft` (Lưu nháp), `Submitted` (Chờ duyệt), `Approved` (Đã duyệt), `Completed` (Đã nhập kho), `Cancelled` (Đã hủy).
- Không kế thừa `AuditableEntity` hay `FullAuditableEntity`: không có `CreatedByUserId`, không có `UpdatedAt`, `UpdatedByUserId`, không có Soft Delete (`IsDeleted`).
- Không có cột mã phiếu (`ReceiptCode`) trong CSDL. Mã phiếu `GR-yyyyMMdd-Id` chỉ là getter tính toán sinh ra trong DTO (`InventoryDtos.cs:150`).
- Trong giao diện [`GoodsReceiptManagement.razor#L108-L113`](file:///d:/My%20Project/src/Restaurant.Web/Components/Pages/Admin/Inventory/GoodsReceiptManagement.razor#L108-L113), cột trạng thái được hardcode bằng HTML tĩnh:
```html
<td class="py-4 px-6 text-center">
    <span class="inline-flex items-center gap-1 px-3 py-1 bg-emerald-100 text-emerald-800 rounded-full text-xs font-bold">
        <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
        Đã nhập kho
    </span>
</td>
```
- Tại [`InventoryService.cs#L121-L184`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L121-L184), phương thức `CreatePurchaseOrderAsync` khi được gọi lập tức cộng kho `material.CurrentStock += item.Quantity` và chốt sổ cái ngay lập tức. Người dùng không thể tạo đơn đặt hàng nháp, không thể gửi quản lý duyệt giá, và nếu nhập nhầm số lượng/đơn giá thì **không có cách nào để sửa, hủy hoặc rollback phiếu nhập**.

### 1.2. Phân hệ Xuất kho (Goods Issue): Hoàn toàn vắng bóng trong hệ thống

- Tìm kiếm toàn bộ codebase với từ khóa `GoodsIssue`, `StockIssue`, `StockTransfer`, `StockScrap`: kết quả **0 file, 0 entity, 0 controller**.
- Các nghiệp vụ nhà hàng bắt buộc phải có gồm:
  - Xuất hủy do nguyên liệu hết hạn, ôi thiu, hỏng hóc trong kho.
  - Xuất chuyển kho nội bộ (từ Kho tổng sang Bếp / Bar).
  - Xuất trả hàng lỗi cho Nhà cung cấp.
  - Xuất sử dụng nội bộ (Staff Meal / Huấn luyện nghiệp vụ).
- Hiện tại, bất kỳ sự tiêu hao nào ngoài việc bán món qua KDS đều phải trông chờ vào việc "Kiểm kê kho định kỳ để ghi nhận hao hụt". Điều này làm mất tính kiểm soát theo thời gian thực và không thể phân định trách nhiệm thất thoát.

### 1.3. Lỗ hổng Sổ Cái (StockLedger): Vi phạm nghiêm trọng nguyên tắc Immutability (Bất biến)

Tại [`CancelOrderService.cs#L85-L96`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/CancelOrderService.cs#L85-L96):
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
*Quan sát thực tế:*
- Thay vì chèn thêm một bản ghi bù trừ mới (`CancellationReturn` nếu trả lại kho, hoặc `CancellationWaste` để ghi nhận tổn thất) với timestamp tại thời điểm hủy đơn, phương thức này **sửa trực tiếp (IN-PLACE MUTATION)** thuộc tính `TransactionType` và `Note` của bản ghi giao dịch đã phát sinh trong quá khứ!
- Điều này vi phạm nguyên tắc cơ bản nhất của Sổ Cái Kế Toán (Accounting Ledger): **Sổ cái phải là Append-Only (chỉ ghi thêm, không bao giờ sửa đổi lịch sử)**. Việc sửa đè này làm sai lệch báo cáo kiểm toán lịch sử và làm mất dấu vết thời điểm tiêu hao ban đầu.

### 1.4. Khiếm khuyết cấu trúc bảng `InventoryTransaction`

Tại [`CustomerAndInventoryEntities.cs#L179-L193`](file:///d:/My%20Project/src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs#L179-L193):
```csharp
public class InventoryTransaction
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Note { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
```
*Các khiếm khuyết cấu trúc:*
1. **Thiếu trường giá trị tài chính:** Hoàn toàn không có `UnitPrice` (đơn giá vốn tại thời điểm xuất/nhập) và không có `TotalCost` (thành tiền). Khi giá vốn nguyên liệu trong bảng `Materials.CostPerUnit` thay đổi theo thời gian, không thể định giá được giá trị các biến động trong quá khứ.
2. **Thiếu số dư đầu kỳ (`BalanceBefore`):** Chỉ có `BalanceAfter`. Khi kiểm toán đối chiếu phải suy ngược bằng phép trừ `BalanceAfter - Quantity`, dễ dẫn đến sai lệch nếu có ghi đè tồn kho do kiểm kê.
3. **Không phân tách `InQuantity` và `OutQuantity`:** Dùng một cột số `Quantity` có dấu âm/dương. Khi tổng hợp theo báo cáo kế toán (Nhập - Xuất - Tồn) phải viết các câu lệnh điều kiện phức tạp.
4. **Không có API và không có UI để xem Sổ Cái:** Toàn bộ bảng `InventoryTransactions` không có endpoint API lấy danh sách/lọc theo nguyên liệu, và menu Admin (`AdminSidebar.razor:106-111`) hoàn toàn không có mục "Sổ cái kho / Lịch sử biến động". Dữ liệu tồn kho trở thành một "hộp đen" đối với người vận hành.

### 1.5. Lỗ hổng Concurrency và Mất dấu trừ kho khi KDS gặp lỗi tranh chấp

Tại [`KitchenService.cs#L201-L228`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/KitchenService.cs#L201-L228):
```csharp
await _context.SaveChangesAsync(); // <-- Dòng 201: Đã commit trạng thái OrderItem.KitchenStatus = Done

// 7. Domain Event publishing for BOM Inventory Deduction on Done (only for non-combo headers)
if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
{
    bool isComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue;
    if (!isComboHeader)
    {
        await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId)); // <-- Dòng 226
    }
}
```
Và tại [`InventoryService.cs#L804-L835`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L804-L835):
```csharp
var materials = await _context.Materials
    .Where(m => targetMaterialIds.Contains(m.Id))
    .ToListAsync();

foreach (var material in materials)
{
    if (!materialDeductions.TryGetValue(material.Id, out var deductQty)) continue;
    material.CurrentStock -= deductQty;
    _context.InventoryTransactions.Add(new InventoryTransaction { ... });
}

await _context.SaveChangesAsync(); // <-- Dòng 835: Lưu trừ kho
```
*Cơ chế lỗi:*
1. Entity `Material` được cấu hình Optimistic Concurrency Token qua `RowVersion` (`EntityConfigurations.cs:160`).
2. Khi nhiều món ăn nấu xong cùng lúc ở các trạm bếp khác nhau (hoặc cùng lúc với giao dịch nhập kho), việc cập nhật `Material.CurrentStock` sẽ gây ra ngoại lệ `DbUpdateConcurrencyException`.
3. Tại `DeductInventoryForOrderItemAsync`, **không có khối try/catch và không có cơ chế retry**. Ngoại lệ bị ném ngược về `KitchenService.UpdateOrderItemKitchenStatusAsync`.
4. Tuy nhiên, trước khi event được phát ra, `OrderItem.KitchenStatus = Done` **đã được lưu thành công vào CSDL ở Dòng 201**.
5. Khi KDS nhận lỗi 500 hoặc người dùng bấm nút Done lần nữa:
   - Hệ thống đọc trạng thái hiện tại từ DB: `oldStatus = KitchenStatus.Done`.
   - Điều kiện kiểm tra tại dòng 220: `newStatus == Done && oldStatus != Done` **bị sai (false)**!
   - Event `OrderItemKitchenDoneDomainEvent` **vĩnh viễn không bao giờ được phát lại**!
   - Kết quả: Món ăn hiển thị hoàn thành trên hệ thống, nhưng nguyên liệu BOM **không bao giờ bị trừ trong kho**.

### 1.6. Kiểm kê kho đè bẹp số dư bán hàng và cho phép Reconcile nhiều lần

Tại [`InventoryService.cs#L581-L642`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L581-L642):
```csharp
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
        ...
        foreach (var item in request.Items)
        {
            var stocktakeItem = stocktake.StocktakeItems.FirstOrDefault(si => si.MaterialId == item.MaterialId);
            var material = await _context.Materials.FindAsync(item.MaterialId);
            if (material == null) continue;

            var systemQty = stocktakeItem?.SystemQuantity ?? material.CurrentStock;
            var finalActual = item.ActualQuantity;
            var diff = finalActual - systemQty;
            ...
            material.CurrentStock = finalActual; // <-- Dòng 623: Gán đè số đếm thực tế

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = material.Id,
                TransactionType = (int)InventoryTransactionType.AuditAdjustment,
                Quantity = diff, // <-- Dòng 629: Chênh lệch so với số hệ thống lúc đếm mù
                BalanceAfter = material.CurrentStock,
                ...
            });
        }
        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        return true;
    }
...
```
*Hai lỗi logic nghiêm trọng:*
1. **Thiếu kiểm tra khóa phiếu (`stocktake.Status == Completed`):** Phương thức không hề kiểm tra xem phiếu kiểm kê này đã được chốt trước đó chưa. Nếu quản lý bấm nút "Cân đối kho" 2 lần, hoặc gọi lại API, hệ thống sẽ chèn thêm một loạt bản ghi `AuditAdjustment` trùng lặp vào sổ cái và làm sai lệch nghiêm trọng lịch sử tồn kho.
2. **Triệt tiêu biến động bán hàng trong thời gian chờ duyệt (Snapshot Desynchronization):**
   - Giả sử lúc 09:00 sáng, nhân viên đếm mù: Hệ thống ghi nhận `SystemQuantity = 100 kg`.
   - Trong ngày, nhà hàng bán 30 chiếc bánh pizza, hệ thống đã trừ kho `30 kg` -> `CurrentStock` hiện thực tế là `70 kg`.
   - Đến 17:00 chiều, quản lý mở phiếu đếm lúc sáng ra để chốt với số thực tế nhân viên đếm là `98 kg` (hao hụt 2 kg so với lúc sáng).
   - Tại dòng 609: `diff = 98 - 100 = -2 kg`.
   - Tại dòng 623: `material.CurrentStock = finalActual = 98 kg`!
   - Toàn bộ `30 kg` nguyên liệu tiêu hao do bán hàng từ 09:00 đến 17:00 **bị xóa sạch khỏi tồn kho hiện tại** (nhảy vọt từ 70 lên 98 thay vì phải là $70 - 2 = 68$ kg).
   - Đồng thời trong `InventoryTransaction`: `Quantity = -2`, nhưng `BalanceAfter = 98` (trong khi số dư trước đó là 70). Phép toán sổ cái $70 + (-2) = 68 \neq 98$ bị gãy hoàn toàn!

### 1.7. Báo cáo Hao hụt kho sử dụng số liệu giả lập (Fake Calculation)

Tại [`InventoryService.cs#L731-L737`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L731-L737):
```csharp
var totalLoss = Math.Abs(items.Sum(si => si.DifferenceValue));

return new InventoryLossSummaryDto
{
    TotalVarianceAmount = totalLoss,
    TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0), // <-- Dòng 734: Hardcode 35%
    DiscrepancyCount = items.Count,
    DiscrepancyItems = discrepancyList
};
```
*Quan sát thực tế:*
- Chỉ số `TotalWasteAmount` (Hao hụt tự nhiên & hủy chế biến) hiển thị trên giao diện [`InventoryLossReport.razor#L29`](file:///d:/My%20Project/src/Restaurant.Web/Components/Pages/Admin/Reports/InventoryLossReport.razor#L29) được tính toán bằng cách **nhân bừa 35% vào tổng giá trị thất thoát kiểm kê**.
- Hệ thống hoàn toàn không truy vấn bảng `InventoryTransactions` với `TransactionType == InventoryTransactionType.CancellationWaste` để lấy chi phí hao hụt thực tế từ các đơn hàng bị hủy món/hủy đơn. Đây là một con số giả tạo che giấu dữ liệu tài chính thực tế của nhà hàng.

### 1.8. Bỏ qua hoàn toàn xử lý kho khi Thu ngân hủy món lẻ (`VoidOrderItemAsync`)

Tại [`PosOrderService.cs#L896-L972`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/PosOrderService.cs#L896-L972):
```csharp
public async Task<bool> VoidOrderItemAsync(int orderId, int orderItemId, string reason, int userId)
{
    ...
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
    ...
}
```
*Quan sát thực tế:*
- Khi thu ngân thực hiện hủy món trên POS (`VoidOrderItemAsync`), phương thức chỉ cập nhật `OrderItem.Status = Voided`, ghi log vào `VoidLogs` và tính lại tiền đơn hàng.
- **Không có bất kỳ dòng code nào kiểm tra xem món này đã nấu xong trên KDS (`KitchenStatus == Done`) hay chưa!**
- Nếu món đã nấu xong (nguyên liệu đã bị trừ kho): Số nguyên liệu này không được đánh dấu là `CancellationWaste`, cũng không được nhập hoàn kho (`CancellationReturn`). Không có giao dịch kho nào được tạo ra. Dữ liệu kế toán kho bị thất thoát không dấu vết!

### 1.9. Không áp dụng UnitConversion và Giới hạn độ chính xác Database `decimal(18,2)`

1. Tại [`InventoryService.cs#L165, L814`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L165):
```csharp
material.CurrentStock += item.Quantity; // Nhập kho
...
material.CurrentStock -= deductQty;    // Trừ kho BOM
```
- Khi nhập hàng: Nếu nhập 10 thùng bia (mỗi thùng 24 lon) hoặc 10 bao bột (mỗi bao 50kg), hệ thống chỉ cộng thêm đúng `10` vào `CurrentStock`.
- Điều này vi phạm trực tiếp tiêu chí nghiệm thu của Spec Kit [`docs/specs/BOH-spec.md#L125`](file:///d:/My%20Project/docs/specs/BOH-spec.md#L125): *"AC-BOH-009.1: Given nhập kho 10 bao bột cà phê (quy đổi 1 bao = 50kg), When lưu phiếu nhập kho, Then số lượng tồn kho của nguyên liệu bột cà phê trong bảng Materials phải tăng thêm đúng 500kg"*.
2. Tại Migration [`20260723205718_InitialCreate.cs#L316, L345`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Migrations/20260723205718_InitialCreate.cs#L316):
```csharp
CurrentStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
...
ConversionFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
```
- Cấu hình kiểu dữ liệu trong SQL Server là `decimal(18,2)`. Các nguyên liệu định lượng nhỏ theo gram (ví dụ 0.005 kg men nở, 0.002 kg lá oregano) hoặc tỷ lệ quy đổi (1 gram = 0.001 kg) đều bị làm tròn thành `0.01` hoặc bị cắt cụt thành `0.00`.

### 1.10. Hardcode quan hệ Nhà cung cấp - Nguyên vật liệu bằng chuỗi ký tự tiếng Việt

Tại [`InventoryService.cs#L659-L698`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs#L659-L698):
```csharp
if (name.Contains("ý việt") || name.Contains("thực phẩm ý") || name.Contains("bột mì"))
{
    filtered = allMaterials.Where(m => 
        m.Name.Contains("Bột") || m.Name.Contains("Phô mai") || m.Name.Contains("Sốt") || ...).ToList();
}
else if (name.Contains("đà lạt") || name.Contains("nông sản") || name.Contains("rau"))
{ ... }
else if (name.Contains("meat") || name.Contains("thịt"))
{ ... }
```
*Quan sát thực tế:*
- Màn hình Lập phiếu nhập kho yêu cầu chọn NCC để lọc ra các mặt hàng NCC đó cung cấp. Nhưng trong CSDL **hoàn toàn không có bảng liên kết `SupplierMaterials`**.
- Thay vào đó, lập trình viên đã hardcode kiểm tra tên NCC chứa chữ "ý việt", "đà lạt", "meat" để lọc nguyên liệu bằng `string.Contains`!
- Khi nhà hàng thêm NCC mới (ví dụ "Công ty TNHH Minh Phát"), hệ thống sẽ rơi vào nhánh `else { filtered = allMaterials; }`.

---

## 2. COMPONENT 2: LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

```
[Observation 1.1: GoodsReceipt không có cột Status, không thể sửa/hủy/duyệt]
    + [Observation 1.2: Hoàn toàn không có Entity/API GoodsIssue xuất kho hủy/chuyển/trả]
    ---> SUY LUẬN 1: Vòng đời chứng từ kho bị què cụt. Luồng nhập là "1 chiều không thể đảo ngược",
                     luồng xuất nội bộ/hủy hàng bằng 0, không đáp ứng chuẩn kiểm soát thất thoát.

[Observation 1.3: CancelOrderService sửa đè TransactionType của InventoryTransactions]
    + [Observation 1.4: InventoryTransactions thiếu UnitPrice, TotalCost, BalanceBefore, không có UI xem sổ]
    ---> SUY LUẬN 2: Tính toàn vẹn và bất biến của Sổ Cái Kế Toán bị phá hủy. Dữ liệu biến động kho không
                     thể kiểm toán độc lập, không xác định được giá trị tồn kho theo thời gian.

[Observation 1.5: Trừ kho BOM diễn ra ngoài transaction chính sau khi OrderItem đã Done]
    + [EntityConfigurations: Material có RowVersion kiểm tra Concurrency]
    + [KitchenService.cs:220: Chặn retry bằng if (newStatus == Done && oldStatus != Done)]
    ---> SUY LUẬN 3: Khi có xung đột đồng thời, việc trừ kho bị lỗi nhưng trạng thái món vẫn lưu là Done.
                     KDS retry sẽ bị chặn bởi điều kiện oldStatus != Done, gây THẤT THOÁT TRỪ KHO VĨNH VIỄN.

[Observation 1.6: ReconcileStocktake gán đè material.CurrentStock = finalActual]
    + [ReconcileStocktake không kiểm tra phiếu đã Completed]
    ---> SUY LUẬN 4: Quản lý chốt kiểm kê đè bẹp mọi biến động bán hàng phát sinh trong ngày,
                     và có thể kích hoạt chốt trùng nhiều lần làm sai lệch số dư lũy kế.

[Observation 1.7: InventoryService.cs:734 tính TotalWasteAmount = totalLoss * 0.35m]
    ---> SUY LUẬN 5: Báo cáo tài chính hao hụt đưa ra số liệu giả mạo, che giấu chi phí thực tế.

[Observation 1.8: VoidOrderItemAsync không kiểm tra KitchenStatus và không phát sinh giao dịch kho]
    ---> SUY LUẬN 6: Khách gọi món, bếp nấu xong trừ kho, sau đó thu ngân bấm Void món thì nguyên liệu
                     coi như bốc hơi khỏi sổ sách mà không ghi nhận bất kỳ chi phí hao hụt nào.

[Observation 1.9: Không gọi UnitConversion khi nhập/xuất; precision DB là decimal(18,2)]
    ---> SUY LUẬN 7: Tỷ lệ quy đổi đơn vị không hoạt động trên luồng thực tế, sai lệch quy đổi số lượng lớn.
```

---

## 3. COMPONENT 3: CAVEATS (GIỚI HẠN PHẠM VI & GIẢ ĐỊNH)

1. **Phạm vi kiểm tra:** Báo cáo này tập trung chuyên sâu vào phân hệ Nhập kho, Xuất kho, Kiểm kê kho, Sổ cái `InventoryTransaction`, và sự tích hợp với POS/KDS từ góc độ kho nguyên vật liệu. Các khía cạnh công thức định mức chi tiết (BOM/Modifier recipes) được phân công chuyên sâu cho Agent 1.
2. **Giả định về kiến trúc Database:** Hiện tại giải pháp chỉ có 1 kho vật lý duy nhất đại diện bởi `Material.CurrentStock`. Toàn bộ phân tích dựa trên tiền đề nhà hàng hiện tại vận hành mô hình 1 kho tập trung, chưa có nhu cầu chia đa chi nhánh hoặc tách riêng Kho Lạnh/Kho Pha chế/Kho Bếp trên nhiều điểm lưu trữ vật lý.
3. **Môi trường Runtime:** Toàn bộ quan sát được kiểm chứng trực tiếp trên mã nguồn C# (.NET 9) và file Migration EF Core hiện có trong repo tại `d:\My Project`.

---

## 4. COMPONENT 4: CONCLUSION (KẾT LUẬN & KIẾN NGHỊ KHẮC PHỤC)

### 4.1. Đánh giá chung
Hệ thống hiện tại **CHƯA ĐỦ ĐIỀU KIỆN SẴN SÀNG CHO UAT HOẶC PRODUCTION** về mặt quản lý kho và sổ cái. Việc đưa vào vận hành thực tế ở trạng thái này sẽ gây ra:
- Thất thoát tài chính do số dư kho bị lệch nghiêm trọng giữa thực tế và sổ sách.
- Mất dấu nguyên liệu khi KDS gặp tải cao / xung đột concurrency.
- Không thể làm việc với kế toán hoặc cơ quan thuế do sổ cái không bất biến và không có đơn giá biến động.

### 4.2. Danh sách kiến nghị khắc phục chi tiết theo độ ưu tiên

#### Cấp độ P0 — Khắc phục Ngay lập tức (Critical Architecture & Data Integrity)
1. **[P0] Đảm bảo tính nguyên khối (Atomicity) khi hoàn thành món trên KDS và trừ kho:**
   - Đưa việc cập nhật `KitchenStatus = Done` và trừ kho BOM vào chung một Database Transaction duy nhất, hoặc sử dụng Outbox Pattern / Transactional Inbox.
   - Thêm cơ chế tự động thử lại khi gặp `DbUpdateConcurrencyException` trên `Material.RowVersion` (tải lại `CurrentStock` mới nhất và trừ lại số lượng).
   - Tháo gỡ cơ chế chặn cứng `oldStatus != Done` nếu trước đó việc trừ kho chưa hoàn thành (kiểm tra `InventoryTransactions.AnyAsync` thay vì chỉ dựa vào `oldStatus`).
2. **[P0] Khôi phục tính Bất Biến (Immutability) cho Sổ Cái `InventoryTransactions`:**
   - Tuyệt đối cấm câu lệnh UPDATE trên `InventoryTransactions`.
   - Khi hủy đơn trong `CancelOrderService`, giữ nguyên bản ghi `SaleDeduction` ban đầu và tạo thêm một bản ghi mới mang loại `InventoryTransactionType.CancellationWaste` (nếu hủy bỏ đồ ăn) hoặc `InventoryTransactionType.CancellationReturn` (nếu hoàn nguyên liệu về kho) với dấu và số lượng tương ứng.
3. **[P0] Bổ sung cấu trúc tài chính cho `InventoryTransaction`:**
   - Thêm các cột: `UnitPrice` (đơn vị giá vốn), `TotalCost` (tổng giá trị biến động), `BalanceBefore` (tồn trước khi đổi), `InQuantity`, `OutQuantity`.
   - Tạo Migration cập nhật database schema.

#### Cấp độ P1 — Cần thiết cho vận hành chuẩn (High Priority)
4. **[P1] Chuẩn hóa Vòng đời Phiếu Nhập Kho (`GoodsReceipt`):**
   - Bổ sung trường `Status` (Draft = 0, Submitted = 1, Approved = 2, Cancelled = 3).
   - Chỉ khi Quản lý duyệt phiếu (`Approved`), hệ thống mới thực hiện cộng `Material.CurrentStock` và ghi sổ cái `InventoryTransaction`.
   - Bổ sung trường `ReceiptCode` duy nhất trong DB và lưu vết `CreatedByUserId`, `ApprovedByUserId`, `ApprovedAt`.
5. **[P1] Xây dựng Phân hệ Xuất kho (Goods Issue):**
   - Thiết kế Entity `GoodsIssue` và `GoodsIssueItem` hỗ trợ các loại: Xuất hủy (Scrap/Spoilage), Xuất trả NCC (Return to Supplier), Xuất nội bộ (Internal Use).
   - Xây dựng API và giao diện quản trị BOH để nhân viên kho thực hiện xuất hủy khi nguyên liệu hết hạn.
6. **[P1] Sửa lỗi đồng bộ số dư trong Chốt kiểm kê (`ReconcileStocktakeAsync`):**
   - Khóa chặt không cho phép duyệt lại phiếu kiểm kê đã ở trạng thái `Completed`.
   - Thay vì gán đè `material.CurrentStock = finalActual`, hệ thống phải tính chênh lệch dựa trên số tồn thực tế tại thời điểm đếm, hoặc khóa sổ tạm thời (Freeze Stock) trong lúc đếm, hoặc ghi nhận giao dịch điều chỉnh là $\Delta = \text{Actual} - \text{CurrentStockAtReconcile}$.
7. **[P1] Loại bỏ công thức giả lập trong Báo cáo Thất thoát:**
   - Sửa `GetInventoryLossSummaryAsync` để tính `TotalWasteAmount` bằng tổng tiền thực tế của các bản ghi `InventoryTransactionType.CancellationWaste` trong kỳ.
8. **[P1] Xử lý trừ kho và hao hụt trong Hủy món lẻ (`VoidOrderItemAsync`):**
   - Kiểm tra: Nếu món đã nấu xong (`KitchenStatus == Done`), tự động ghi nhận một bản ghi `CancellationWaste` vào `InventoryTransactions` tương ứng với định mức BOM của món bị hủy.
9. **[P1] Kích hoạt Quy đổi đơn vị (UnitConversion) trong Nhập và Trừ kho:**
   - Khi nhập kho hoặc trừ kho, bắt buộc tra cứu `UnitConversions` để nhân hệ số quy đổi về đơn vị đo cơ sở của nguyên liệu (`Material.UnitOfMeasureId`).
   - Cập nhật kiểu dữ liệu cột `ConversionFactor` và `Quantity` lên `decimal(18,4)` để bảo toàn độ chính xác cho các đơn vị đo nhỏ.

#### Cấp độ P2 — Cải tiến giao diện & Trải nghiệm (Medium Priority)
10. **[P2] Xây dựng Màn hình Tra cứu Sổ Cái Tồn Kho (StockLedger View):**
    - Bổ sung trang `/admin/inventory/ledger` cho phép lọc biến động theo nguyên liệu, theo loại giao dịch, theo ngày, xuất Excel đối soát.
11. **[P2] Thay thế logic so khớp tên NCC bằng Bảng liên kết Database:**
    - Tạo bảng `SupplierMaterials` (`SupplierId`, `MaterialId`, `SupplierProductCode`, `ContractPrice`) để thay thế chuỗi if-else substring tiếng Việt trong `GetMaterialsBySupplierIdAsync`.
12. **[P2] Bổ sung Unit Tests & Integration Tests toàn diện:**
    - Viết test suite cho `GoodsReceipt`, `BlindStocktake`, `ReconcileStocktake`, `VoidItemInventory` và `ConcurrencyRetry`.

---

## 5. COMPONENT 5: VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Kiểm toán viên độc lập hoặc Lead Architect có thể xác minh toàn bộ các quan sát trên bằng các bước sau:

### 5.1. Xác minh bằng lệnh kiểm tra mã nguồn (Grep / Search)
1. Kiểm tra sự vắng mặt của Entity GoodsIssue:
   ```powershell
   Get-ChildItem -Path "d:\My Project\src" -Recurse -Filter "*GoodsIssue*"
   # Kết quả: Không tìm thấy file nào
   ```
2. Kiểm tra lỗi sửa đè Sổ cái (In-place Mutation):
   Mở file [`d:\My Project\src\Restaurant.Infrastructure\Services\CancelOrderService.cs`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/CancelOrderService.cs), xem dòng 93:
   `deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;`
3. Kiểm tra công thức giả lập báo cáo hao hụt:
   Mở file [`d:\My Project\src\Restaurant.Infrastructure\Services\InventoryService.cs`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs), xem dòng 734:
   `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0);`
4. Kiểm tra hardcode so khớp tên Nhà cung cấp:
   Mở file [`d:\My Project\src\Restaurant.Infrastructure\Services\InventoryService.cs`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/InventoryService.cs), xem dòng 659-698.
5. Kiểm tra bỏ qua kho khi Void món lẻ:
   Mở file [`d:\My Project\src\Restaurant.Infrastructure\Services\PosOrderService.cs`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/PosOrderService.cs), xem dòng 896-994 (không có tham chiếu nào tới `InventoryService` hoặc `InventoryTransactions`).

### 5.2. Chạy Test Suite hiện tại để xác minh độ bao phủ
Chạy lệnh kiểm thử tiêu chuẩn của solution:
```powershell
dotnet test "d:\My Project\src\Restaurant.Domain.Tests\Restaurant.Domain.Tests.csproj"
```
*Kết quả quan sát:* Toàn bộ các bài test hiện tại đều PASS, nhưng khi kiểm tra `Tier1_FeatureCoverageTests.cs:585-618`, chỉ có duy nhất 1 bài test kiểm tra luồng đơn giản của `CreatePurchaseOrderAsync` và `AuditStockAsync`. Hoàn toàn không có test nào cho các trường hợp biên, hủy đơn, lỗi concurrency, hoặc kiểm kê 2 bước.

### 5.3. Điều kiện bác bỏ báo cáo (Invalidation Conditions)
Báo cáo này sẽ bị coi là mất hiệu lực nếu:
1. Tồn tại một phân hệ Xuất kho (`GoodsIssue`) độc lập ở một project khác ngoài solution `Restaurant.sln`.
2. Có một Service nền hoặc trigger cơ sở dữ liệu tự động ghi đè hành vi của `CancelOrderService.cs` để bảo vệ sổ cái.
3. Đã có một cấu hình riêng biệt trong EF Core can thiệp vào quy trình phát event MediatR của `KitchenService.cs` để biến nó thành Atomic Transaction.

*(Xác nhận: Không có điều kiện nào trong 3 điều kiện trên tồn tại trong workspace hiện tại).*
