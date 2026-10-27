# BÁO CÁO AUDIT TOÀN DIỆN: GIÁ VỐN, BÁO CÁO, TRANSACTION VÀ TEST SUITE
**Agent**: Agent 4 (Giá vốn, báo cáo, transaction và test Auditor)  
**Workspace**: `d:\My Project`  
**Solution**: `src/Restaurant.sln` (.NET 9 / EF Core 9 / SQL Server / Blazor Server)  
**Ngày thực hiện**: 2026-10-14  
**Chế độ**: Read-Only Deep Code Investigation (Tuyệt đối không sửa code / không migration)

---

## 1. OBSERVATION (QUAN SÁT THỰC CHỨNG & BẰNG CHỨNG CODE)

### 1.1. Phương pháp tính giá vốn món ăn (Recipe Cost) & Giá vốn hàng bán (COGS)

#### O-1.1.1. Recipe Cost (Giá vốn món ăn định mức) chỉ là công thức động tạm thời, không được lưu vết
- **Vị trí**: 
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:348-364` (`GetMenuItemRecipeAsync`)
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:380-394` (`GetModifierOptionRecipeAsync`)
  - `src/Restaurant.Web/Components/Pages/Admin/Menu/RecipeManagement.razor:100, 109`
- **Code thực chứng**:
  ```csharp
  // InventoryService.cs:360
  Items = recipe?.RecipeItems.Select(ri => new RecipeItemDto
  {
      MaterialId = ri.MaterialId,
      MaterialName = ri.Material?.Name ?? "Nguyên liệu",
      UnitOfMeasureId = ri.UnitOfMeasureId,
      Quantity = ri.Quantity,
      CostPerUnit = ri.Material?.CostPerUnit ?? 0 // Lấy trực tiếp từ giá hiện hành của Material
  }).ToList()
  ```
  ```razor
  <!-- RecipeManagement.razor:109 -->
  <span class="font-headline-sm text-xl font-bold text-primary">
      @_recipeItems.Sum(i => i.Quantity * i.CostPerUnit).ToString("N0") VNĐ
  </span>
  ```
- **Thực tế**: Entity `Recipe` (`CustomerAndInventoryEntities.cs:80-85`) và `MenuItem` (`AuthAndMenuEntities.cs:75-97`) hoàn toàn **không có thuộc tính lưu giá vốn** (`Cost`, `RecipeCost`, `HistoricalCost`). Giá vốn món ăn được tính nhân tức thời (`Quantity * Material.CostPerUnit`). Khi giá nguyên liệu thay đổi, toàn bộ giá vốn định mức trong quá khứ bị biến động theo mà không có snapshot lịch sử.
- **Phân loại**: `PARTIAL` | **Mức độ**: `P1`

#### O-1.1.2. Giá vốn hàng bán (COGS - Cost of Goods Sold) hoàn toàn CHƯA ĐƯỢC IMPLEMENT
- **Vị trí**:
  - `src/Restaurant.Domain/Entities/OrderAndPaymentEntities.cs:5-59` (`Order`, `OrderItem`)
  - `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs:179-193` (`InventoryTransaction`)
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:816-827` (`DeductInventoryForOrderItemAsync`)
- **Code thực chứng**:
  ```csharp
  // InventoryService.cs:816-827: Trừ kho khi món KDS hoàn thành
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
      // HOÀN TOÀN KHÔNG CÓ TRƯỜNG UnitCost HOẶC TotalCost
  });
  ```
- **Thực tế**:
  - Tìm kiếm toàn bộ solution với các từ khóa `COGS`, `CostOfGoodsSold`, `GrossProfit`, `Lãi gộp` -> **0 kết quả**.
  - `OrderItem` chỉ lưu `UnitPrice`, `DiscountAmount`, `Quantity`, không có `UnitCost`, `TotalCost`.
  - `InventoryTransaction` chỉ lưu số lượng biến động (`Quantity`, `BalanceAfter`), không hề ghi nhận đơn giá xuất kho tại thời điểm bán. Do đó, hệ thống không thể hạch toán được giá vốn hàng bán xuất kho thực tế.
- **Phân loại**: `NOT_STARTED` | **Mức độ**: `P0`

#### O-1.1.3. Phương pháp định giá tồn kho: Bình quân gia quyền di động (Moving Weighted Average) bị lỗi khi tồn âm và bị ghi đè tùy tiện
- **Vị trí**:
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:158-165` (`CreatePurchaseOrderAsync`)
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:83-95` (`UpdateMaterialAsync`)
- **Code thực chứng**:
  ```csharp
  // InventoryService.cs:158-165
  // Weighted Average Cost calculation:
  // NewCost = (OldStock * OldCost + NewQty * NewCost) / (OldStock + NewQty)
  var oldStock = material.CurrentStock > 0 ? material.CurrentStock : 0;
  var newStock = oldStock + item.Quantity;
  var totalCostVal = (oldStock * material.CostPerUnit) + (item.Quantity * item.UnitCost);

  material.CostPerUnit = newStock > 0 ? (totalCostVal / newStock) : item.UnitCost;
  material.CurrentStock += item.Quantity;
  ```
  ```csharp
  // InventoryService.cs:92 trong UpdateMaterialAsync
  material.CostPerUnit = request.CostPerUnit; // Cho phép sửa thẳng tay giá vốn
  ```
- **Thực tế**:
  1. **Lỗi tồn âm (Negative stock clamping)**: Khi nhà bếp xuất kho trước khiến `CurrentStock = -10kg`, công thức ép `oldStock = 0`, biến `newStock = item.Quantity` và gán thẳng `CostPerUnit = item.UnitCost`. Sai lệch nghiêm trọng về mặt kế toán tài chính vì giá trị âm trước đó bị xóa sổ khỏi công thức tính giá vốn bình quân.
  2. **Bỏ qua quy đổi đơn vị (Unit Conversion bypass)**: `CreatePurchaseOrderAsync` tại dòng 151 ép cứng `UnitOfMeasureId = material.UnitOfMeasureId`, bỏ qua bảng `UnitConversions` dù người dùng nhập theo thùng, két, bao.
  3. **Ghi đè thủ công vô điều kiện**: API `PUT api/v1/inventory/materials/{id}` cho phép người dùng sửa thẳng `CostPerUnit`, phá hủy hoàn toàn tính chính xác lịch sử của thuật toán bình quân gia quyền mà không có log kế toán.
  4. **Không có cơ chế FIFO**: Không có bảng lưu các lô nhập (Batches/Lots) hay hàng đợi giá nhập kho.
- **Phân loại**: `PARTIAL` / `NEEDS_BUSINESS_DECISION` | **Mức độ**: `P1`

---

### 1.2. Tính nhất quán Doanh thu - Giá vốn - Tiêu hao & Đánh giá Báo cáo

#### O-1.2.1. Báo cáo Doanh thu (Revenue Report) là 100% Mock Data tĩnh trên giao diện Razor
- **Vị trí**: `src/Restaurant.Web/Components/Pages/Admin/Reports/RevenueReport.razor:18-90`
- **Code thực chứng**:
  ```razor
  <!-- RevenueReport.razor:18-39 -->
  <div class="font-headline-sm text-2xl font-bold text-primary mt-2">128,450,000đ</div>
  <div class="font-headline-sm text-2xl font-bold text-on-background mt-2">482 đơn</div>
  <div class="font-headline-sm text-2xl font-bold text-secondary mt-2">84,300,000đ</div>
  <div class="font-headline-sm text-2xl font-bold text-on-background mt-2">44,150,000đ</div>
  <!-- Table rows hardcoded ngày 16/09/2026, 17/09/2026, 18/09/2026 -->
  @code {
      private void ExportReport()
      {
          // Excel Export action handler
      }
  }
  ```
- **Thực tế**: Trang `/admin/reports/revenue` hoàn toàn không gọi backend API, không inject service, không liên kết database. Toàn bộ số liệu là HTML hardcoded tĩnh.
- **Phân loại**: `UI_ONLY` | **Mức độ**: `P0`

#### O-1.2.2. Báo cáo Món bán chạy (Top Movers Report) là 100% Mock Data tĩnh
- **Vị trí**: `src/Restaurant.Web/Components/Pages/Admin/Reports/TopMoversReport.razor:24-57`
- **Code thực chứng**:
  ```razor
  <!-- TopMoversReport.razor:24-52 -->
  <td class="p-4 font-bold text-on-surface">Pizza Margherita Mozzarella</td>
  <td class="p-4 text-right font-mono font-bold text-base">214 phần</td>
  <td class="p-4 text-right font-mono font-bold text-primary">32,100,000đ</td>
  @code {
  }
  ```
- **Thực tế**: Dù backend `DashboardService.GetTopMoversAsync` đã có query LINQ nhóm theo `OrderItem`, trang `TopMoversReport.razor` lại để trống `@code {}` và hiển thị HTML cứng.
- **Phân loại**: `UI_ONLY` | **Mức độ**: `P1`

#### O-1.2.3. Báo cáo Hao hụt kho (Inventory Loss Report) tính giả định 35% và bỏ qua hủy đơn
- **Vị trí**: `src/Restaurant.Infrastructure/Services/InventoryService.cs:703-738` (`GetInventoryLossSummaryAsync`)
- **Code thực chứng**:
  ```csharp
  // InventoryService.cs:729-736
  var totalLoss = Math.Abs(items.Sum(si => si.DifferenceValue));

  return new InventoryLossSummaryDto
  {
      TotalVarianceAmount = totalLoss,
      TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0), // HARDCODE TỶ LỆ 35%!
      DiscrepancyCount = items.Count,
      DiscrepancyItems = discrepancyList
  };
  ```
- **Thực tế**:
  1. Trường `TotalWasteAmount` (Hao hụt tự nhiên & hủy chế biến) bị tính bừa bằng `totalLoss * 0.35m` (35%).
  2. Bỏ qua hoàn toàn các bản ghi hao hụt do hủy đơn (`CancellationWaste`) được tạo ra trong `CancelOrderService.cs:93`!
  3. Bỏ qua các khoản chênh lệch dương (thừa kho kiểm kê).
- **Phân loại**: `BROKEN` | **Mức độ**: `P1`

#### O-1.2.4. Báo cáo Xuất - Nhập - Tồn (Stock In-Out-Balance) và Lãi gộp (Gross Profit)
- **Thực tế**:
  - **Báo cáo Xuất - Nhập - Tồn**: Hoàn toàn **KHÔNG TỒN TẠI** cả ở UI, Controller lẫn Service. Không có bảng tổng hợp Tồn đầu kỳ, Nhập trong kỳ, Xuất bán, Xuất hủy, Tồn cuối kỳ.
  - **Báo cáo Lãi gộp (Gross Profit)** theo món, theo ca làm việc (Shift), theo ngày: Hoàn toàn **KHÔNG TỒN TẠI**.
  - `Shift` entity chỉ lưu dòng tiền mặt/chuyển khoản (`TotalCashAmount`, `TotalTransferAmount`), không lưu giá vốn hàng bán của ca.
  - `StoreSalesSummaryDto` trong `ReconciliationService.cs:120-131` chỉ tính `GrossSales = completedOrders.Sum(o => o.SubTotal)`, không có trường COGS hay Gross Profit.
- **Phân loại**: `NOT_STARTED` | **Mức độ**: `P0`

#### O-1.2.5. Báo cáo Tồn thấp (Low Stock Report)
- **Vị trí**: `src/Restaurant.Web/Components/Pages/Admin/Inventory/MaterialManagement.razor:34-40`
- **Thực tế**: Không có màn hình báo cáo chuyên biệt. Chỉ có nút toggle filter in-memory trên danh sách nguyên liệu và 1 thẻ StatCard trên Dashboard. Không hỗ trợ chọn ngày, xem lịch sử hay xuất báo cáo.
- **Phân loại**: `PARTIAL` | **Mức độ**: `P2`

---

### 1.3. Transaction Boundaries & Lock Concurrency

#### O-1.3.1. Trừ kho BOM tự động khi KDS Done KHÔNG CÓ TRANSACTION và KHÔNG ĐỒNG BỘ NGUYÊN TỬ
- **Vị trí**:
  - `src/Restaurant.Infrastructure/Services/KitchenService.cs:201, 226` (`UpdateItemStatusAsync`)
  - `src/Restaurant.Infrastructure/Handlers/DeductInventoryOnOrderItemKitchenDoneHandler.cs:16-19`
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs:740-841` (`DeductInventoryForOrderItemAsync`)
- **Code thực chứng**:
  ```csharp
  // KitchenService.cs:201 - Lưu trạng thái món ăn vào DB trước
  await _context.SaveChangesAsync();

  // KitchenService.cs:226 - Bắn Domain Event qua MediatR SAU KHI ĐÃ COMMIT
  if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
  {
      if (!isComboHeader)
      {
          await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
      }
  }
  ```
  ```csharp
  // InventoryService.cs:740-841: DeductInventoryForOrderItemAsync
  public async Task DeductInventoryForOrderItemAsync(int orderItemId)
  {
      // KHÔNG CÓ BeginTransactionAsync()
      ...
      material.CurrentStock -= deductQty;
      _context.InventoryTransactions.Add(...);
      ...
      await _context.SaveChangesAsync();
  }
  ```
- **Lỗ hổng thực chứng**:
  - Món ăn được đánh dấu `Done` ở dòng 201 của `KitchenService.cs`. Nếu việc trừ kho ở `DeductInventoryForOrderItemAsync` bị lỗi (Deadlock, DB exception, timeout), trạng thái món vẫn vĩnh viễn là `Done`, nhưng nguyên liệu trong kho **không bị trừ**!
  - Khi nhân viên bếp thử ấn lại: hàm `UpdateItemStatusAsync` kiểm tra `if (oldStatus == newStatus) return;` nên lập tức thoát, không bắn lại event trừ kho. Dữ liệu kho và bếp bị lệch vĩnh viễn!
- **Phân loại**: `BROKEN` | **Mức độ**: `P0`

#### O-1.3.2. Thiếu sót Transaction tại các phương thức nghiệp vụ kho khác
- **Vị trí**:
  - `InventoryService.CreateBlindStocktakeAsync` (`InventoryService.cs:540-578`): Lưu `Stocktake`, sau đó foreach lưu `StocktakeItem` không có `BeginTransactionAsync()`. Nếu lỗi giữa chừng, phiếu kiểm kê bị mồ côi không có items.
  - `InventoryService.SetMenuItemRecipeAsync` (`InventoryService.cs:258-295`): Xóa công thức cũ (`RemoveRange`), `SaveChangesAsync()`, sau đó thêm công thức mới. Nếu tiến trình bị đứt đoạn giữa 2 lệnh, công thức món bị xóa sạch.
  - `InventoryService.SetModifierOptionRecipeAsync` (`InventoryService.cs:297-334`): Tương tự, xóa công thức modifier cũ trước rồi mới insert mới mà không có transaction.
- **Phân loại**: `BROKEN` | **Mức độ**: `P1`

#### O-1.3.3. Thiếu `IExecutionStrategy` trên 15/16 Transaction Blocks
- **Vị trí**:
  - `PosOrderService.cs:75, 144, 189, 302`
  - `PaymentService.cs:130, 269`
  - `InventoryService.cs:125, 196, 583`
  - `CancelOrderService.cs:33`
  - `ShiftService.cs:119`
  - `QrOrderService.cs:343`
  - `ApprovalService.cs:100`
  - `ComboService.cs:157, 249`
- **Code thực chứng**:
  Chỉ duy nhất `PosOrderService.SplitBillAsync` (dòng 914) sử dụng:
  `var executionStrategy = _context.Database.CreateExecutionStrategy();`
  Tất cả 15 phương thức còn lại đều gọi trực tiếp:
  `using var dbTransaction = await _context.Database.BeginTransactionAsync();`
- **Nguy cơ**: Khi cấu hình SQL Server với tính năng kết nối linh hoạt / thử lại tự động (`EnableRetryOnFailure`), EF Core sẽ chặn và throw `InvalidOperationException`: *"The configured execution strategy does not support user-initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()'"*.
- **Phân loại**: `BROKEN` | **Mức độ**: `P1`

#### O-1.3.4. Phân tích Khóa và Nguy cơ Concurrency / Race Condition
- **Tình trạng Token Concurrency (`IHasRowVersion`)**:
  - Các Entity ĐÃ CÓ `IsRowVersion`: `Material`, `Order`, `Table`, `Shift`, `ApprovalRequest`.
  - Các Entity CHƯA CÓ `IsRowVersion`: `OrderItem`, `Payment`, `GoodsReceipt`, `Stocktake`, `InventoryTransaction`.
- **Lỗ hổng Concurrency nghiêm trọng trên `Material` khi KDS bấm Done đồng thời**:
  - `Material` có `RowVersion` nhưng trong `DeductInventoryForOrderItemAsync` (`InventoryService.cs:835`), `await _context.SaveChangesAsync()` **không hề bắt `DbUpdateConcurrencyException`**!
  - Khi 2 màn hình KDS bấm Done cho 2 món cùng dùng chung nguyên liệu (ví dụ: Phô mai Mozzarella hoặc Bột mì) tại cùng 1 thời điểm: EF Core phát hiện xung đột `RowVersion` và throw `DbUpdateConcurrencyException`. Món thứ hai sẽ bị crash lỗi 500, kho không bị trừ, trong khi món ăn trên KDS đã đổi màu Done từ bước trước!
- **Nguy cơ Race Condition tạo trùng đơn trên cùng một bàn**:
  - `PosOrderService.SubmitPosOrderAsync` (`PosOrderService.cs:431-450`): Tìm `activeOrder` bằng `FirstOrDefaultAsync` không có khóa bi quan (`UPDLOCK`) hay Transaction Isolation Level tương ứng.
  - Nếu nhân viên tại quầy POS và khách hàng quét QR gọi món cùng lúc cho Bàn 01 khi bàn đang Available, 2 order riêng biệt sẽ cùng được tạo ra cho 1 bàn vì database không có Unique Constraint trên `(TableId, ActiveOrderStatus)`.
- **Lỗ hổng va chạm mã đơn Takeaway (Takeaway OrderCode Collision)**:
  - `PosOrderService.cs:479`: `OrderCode = isTakeaway ? $"#TAK-{DateTime.UtcNow.Ticks % 100000}" : ...`
  - Modulo 100,000 chỉ có 100k giá trị. Khi nhiều đơn mang về tạo đồng thời, nguy cơ trùng `OrderCode` dẫn đến vi phạm Unique Index `IX_Orders_OrderCode` (`EntityConfigurations.cs:50`).
- **Khóa bi quan (Pessimistic Locking `UPDLOCK`/`HOLDLOCK`)**:
  - Tìm kiếm toàn bộ solution: `UPDLOCK`, `HOLDLOCK`, `XLOCK`, `FromSqlRaw` -> **0 kết quả**. Hoàn toàn không áp dụng khóa bi quan.
- **Phân loại**: `BROKEN` | **Mức độ**: `P0`

---

### 1.4. Đánh giá Test Suite hiện có & Danh mục Missing Tests

#### O-1.4.1. Thực trạng Test Suite
- **Dự án test**: Chỉ có **1 dự án duy nhất** là `src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`. Không có project test riêng cho WebAPI, Application hay Infrastructure.
- **Kết quả chạy test thực tế (dotnet test)**:
  - Lệnh: `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`
  - Kết quả: **295 Passed, 0 Failed, 0 Skipped** (Thời gian chạy: 2 phút 28 giây).
- **Phát hiện Test Rác / Test Ảo (Dummy Tests)**:
  1. `src/Restaurant.Domain.Tests/CancelOrderServiceTests.cs:8-11`:
     ```csharp
     [Fact]
     public void CancelOrder_DummyTest()
     {
         Assert.True(true); // FAKE TEST 100%!
     }
     ```
  2. `src/Restaurant.Domain.Tests/UnitTest1.cs:6-9`:
     ```csharp
     [Fact]
     public void Test1()
     {
         // METHOD HOÀN TOÀN RỖNG!
     }
     ```
- **Test hiện có về Inventory / BOM / Stock**:
  - `F25_R5_04_InventoryServiceTestSuite_GoodsReceiptAndStocktakeBalance`: Kiểm tra nhập kho bột mì (50kg) và kiểm kê hụt (2kg) trên In-Memory DB.
  - `ComboEndToEndQaTests.TC01_ComboHierarchyAndDeduction`: Kiểm tra gọi hàm `DeductInventoryForOrderItemAsync(pizzaChild.Id)` thủ công.
  - `F21_R5_01`: Kiểm tra sự kiện trừ kho khi nấu xong.
- **Phân loại**: `PARTIAL` | **Mức độ**: `P1`

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. **Từ O-1.1.1 & O-1.1.2 -> Không có cơ sở tính Lãi Gộp & Giá Vốn Hàng Bán**:
   - `OrderItem` không lưu giá vốn tại thời điểm bán; `InventoryTransaction` không lưu chi phí xuất kho; `Recipe` chỉ tính giá vốn tức thời theo giá hiện tại của `Material`.
   - $\Rightarrow$ Doanh thu ghi nhận trên giá bán thực tế, nhưng giá vốn xuất kho bằng 0 trong sổ sách giao dịch.
   - $\Rightarrow$ Bất kỳ nỗ lực nào nhằm báo cáo Lãi gộp (Gross Profit = Revenue - COGS) theo món, theo ca hoặc theo ngày trên hệ thống hiện tại đều không thể thực hiện được nếu không tái cấu trúc Schema.

2. **Từ O-1.1.3 -> Giá vốn bình quân bị méo mó khi có biến động giá hoặc tồn âm**:
   - Tại `InventoryService.cs:160`, công thức gán `oldStock = 0` khi `CurrentStock < 0`. Nếu nhà hàng bán âm 10kg thịt bò (giá vốn cũ 200k/kg), sau đó nhập 10kg thịt bò giá mới 250k/kg, hệ thống coi như tồn cũ bằng 0 và gán luôn giá vốn mới là 250k/kg, làm sai lệch giá trị tồn kho tổng thể.
   - Thêm vào đó, việc thiếu quy đổi đơn vị trong phiếu nhập và cho phép sửa trực tiếp `CostPerUnit` ở API Update khiến dữ liệu giá vốn không đáng tin cậy.

3. **Từ O-1.2.1, O-1.2.2, O-1.2.3 -> Phân hệ Báo cáo Doanh thu - Kho bị đứt gãy hoàn toàn**:
   - Giao diện `RevenueReport.razor` và `TopMoversReport.razor` hoàn toàn là HTML tĩnh, không hề liên kết với dữ liệu Order hay Payment trong Database.
   - Báo cáo hao hụt `InventoryLossReport.razor` sử dụng số nhân hardcode `0.35m` cho tiền hủy hàng và bỏ qua các giao dịch hủy món sau chế biến (`CancellationWaste`).
   - $\Rightarrow$ Hệ thống hiện tại chưa có phân hệ Báo cáo quản trị thực chất, các con số trên giao diện quản trị là dữ liệu giả lập.

4. **Từ O-1.3.1, O-1.3.2, O-1.3.4 -> Nguy cơ Thất thoát Kho và Lệch Trạng thái do Concurrency**:
   - `KitchenService.UpdateItemStatusAsync` lưu trạng thái `Done` trước khi gửi event trừ kho qua MediatR.
   - `DeductInventoryForOrderItemAsync` không có transaction và không xử lý bắt lỗi `DbUpdateConcurrencyException` của `Material.RowVersion`.
   - $\Rightarrow$ Khi nhiều đầu bếp cùng bấm Hoàn thành trên KDS cho các món có chung nguyên liệu, EF Core sẽ bắn ngoại lệ concurrency tại món thứ hai. Món ăn vẫn hiện đã nấu xong, nhưng kho không bị trừ và hệ thống không có cơ chế tự động thử lại (Retry).

5. **Từ O-1.3.3 -> Nguy cơ Sập toàn bộ Giao dịch khi chuyển sang SQL Server Production có Resilience**:
   - 15 phương thức cốt lõi sử dụng `BeginTransactionAsync()` mà không dùng `CreateExecutionStrategy()`.
   - $\Rightarrow$ Khi bật `EnableRetryOnFailure` trên Production SQL Server, mọi thao tác thanh toán, hủy đơn, nhập kho, đóng ca sẽ đồng loạt quăng lỗi `InvalidOperationException`.

6. **Từ O-1.4.1 -> Độ bao phủ Test Suite bị ảo hóa và thiếu các kịch bản biên quan trọng**:
   - 295 test cases vượt qua 100%, nhưng chứa test rác (`Assert.True(true)` trong `CancelOrderServiceTests`) và 100% chạy trên In-Memory Database.
   - In-Memory Database không hỗ trợ Transaction thực thụ, không kiểm tra khóa `RowVersion` chuẩn xác của SQL Server và không phát hiện race condition đa luồng.
   - $\Rightarrow$ Thiếu hoàn toàn các bài test về: Tồn kho âm, Race condition đa KDS, Thất bại trong transaction, Idempotency trừ kho và Thay đổi giá vốn bình quân.

---

## 3. CAVEATS (GIỚI HẠN PHẠM VI & GIẢ ĐỊNH)

1. **Giới hạn môi trường kiểm thử**:
   - Toàn bộ 295 tests hiện tại chạy trên `Microsoft.EntityFrameworkCore.InMemory`. Các hành vi liên quan đến Locking cấp bảng/dòng của SQL Server (`UPDLOCK`, `ROWLOCK`, Transaction Isolation Level) chưa được kiểm thử thực tế trên SQL Server vật lý.
2. **Quy định bảo toàn mã nguồn**:
   - Theo đúng Critical Constraint của Master Prompt, Agent 4 thực hiện điều tra Read-Only 100%, không sửa bất kỳ dòng code nào, không tạo migration và không reset database.
3. **Giả định nghiệp vụ về Giá vốn**:
   - Báo cáo giả định mô hình nhà hàng Pizza cần tính giá vốn theo phương pháp **Bình quân gia quyền di động** (Moving Weighted Average) hoặc **FIFO**. Cần quyết định nghiệp vụ chính thức từ Product Owner về việc có cho phép tồn âm và có chặn chế biến khi hết nguyên liệu hay không.

---

## 4. CONCLUSION (KẾT LUẬN & ĐÁNH GIÁ TỔNG THỂ)

### Bảng Ma trận Đánh giá Chức năng (Evaluation Matrix)

| Chức năng / Hạng mục | Trạng thái kỹ thuật | Mức độ ưu tiên | Tệp tin & Vị trí liên quan | Tóm tắt phát hiện |
| :--- | :--- | :--- | :--- | :--- |
| **Tính giá vốn định mức món (Recipe Cost)** | `PARTIAL` | **P1** | `InventoryService.cs:348-394`, `RecipeManagement.razor:100` | Tính động trên UI/Service, không lưu snapshot vào DB, giá biến động theo thời gian. |
| **Giá vốn hàng bán (COGS)** | `NOT_STARTED` | **P0** | `OrderAndPaymentEntities.cs`, `InventoryTransaction.cs` | Hoàn toàn chưa có trường lưu chi phí xuất kho khi bán, không hạch toán được COGS. |
| **Định giá tồn kho (Moving Average)** | `PARTIAL` / `NEEDS_BUSINESS_DECISION` | **P1** | `InventoryService.cs:158-165` | Ép tồn âm về 0 làm sai lệch giá bình quân; cho phép sửa đè giá vốn không có log; thiếu quy đổi đơn vị. |
| **Báo cáo Doanh thu (Revenue Report)** | `UI_ONLY` | **P0** | `RevenueReport.razor:18-90` | 100% Mock data HTML tĩnh, ngày cố định 16-18/09/2026, không có API kết nối. |
| **Báo cáo Món bán chạy (Top Movers)** | `UI_ONLY` | **P1** | `TopMoversReport.razor:24-57` | Mock data HTML tĩnh; backend đã có API nhưng frontend không gọi. |
| **Báo cáo Hao hụt (Inventory Loss)** | `BROKEN` | **P1** | `InventoryService.cs:729-736`, `InventoryLossReport.razor` | Hardcode tiền hủy món bằng 35% tiền lệch kiểm kê; bỏ qua hao hụt do hủy đơn (`CancellationWaste`). |
| **Báo cáo Xuất - Nhập - Tồn** | `NOT_STARTED` | **P1** | Chưa có Controller / Service / UI | Chưa implement sổ cái biến động kho mở kỳ - nhập - xuất - đóng kỳ. |
| **Báo cáo Lãi gộp (Gross Profit)** | `NOT_STARTED` | **P0** | Chưa có Entity / DTO / Service | Chưa có cấu trúc dữ liệu để tính lãi gộp theo món, theo ca hoặc theo ngày. |
| **Báo cáo Tồn thấp (Low Stock)** | `PARTIAL` | **P2** | `MaterialManagement.razor:34-40` | Chỉ là nút lọc in-memory trên UI danh mục nguyên liệu, chưa có trang báo cáo lịch sử. |
| **Transaction Trừ kho KDS** | `BROKEN` | **P0** | `KitchenService.cs:201-226`, `InventoryService.cs:740-841` | Đổi trạng thái món `Done` trước khi trừ kho; trừ kho không có transaction bọc ngoài; đứt gãy atomicity. |
| **Transaction Boundaries & Resilience**| `BROKEN` | **P1** | 15 Services across solution | 15/16 transaction thiếu `CreateExecutionStrategy()`, sẽ sập khi bật retry trên SQL Server. |
| **Xử lý Concurrency trên Material** | `BROKEN` | **P0** | `InventoryService.cs:835`, `EntityConfigurations.cs:160` | `Material` có `RowVersion` nhưng không catch `DbUpdateConcurrencyException`, KDS bấm Done đồng thời sẽ crash và mất dữ liệu trừ kho. |
| **Khóa bi quan (Pessimistic Locking)** | `NOT_STARTED` | **P2** | Toàn bộ solution | Không sử dụng `UPDLOCK`/`HOLDLOCK`, nguy cơ race condition khi tạo đơn cùng bàn từ POS và QR. |
| **Test Suite Coverage & Integrity** | `PARTIAL` | **P1** | `CancelOrderServiceTests.cs`, `UnitTest1.cs` | 295 tests pass nhưng chứa dummy test `Assert.True(true)`, thiếu 100% test về race condition, tồn âm, COGS và retry. |

---

### Danh mục Chi tiết Toàn bộ Kịch bản Kiểm thử Còn thiếu (Missing Tests Catalog)

1. **Nhóm kiểm thử Giá vốn & COGS (Costing & COGS)**:
   - `TEST-COST-01`: Biến động giá nhập kho bình quân gia quyền khi nhập nguyên liệu mới với đơn giá cao hơn/thấp hơn.
   - `TEST-COST-02`: Nhập kho khi nguyên liệu đang bị tồn âm (Negative stock) -> Xác minh công thức hạch toán giá trị chênh lệch.
   - `TEST-COST-03`: Snapshot giá vốn định mức món ăn tại thời điểm tạo đơn POS và thời điểm bếp hoàn thành.
   - `TEST-COST-04`: Tính toán Lãi gộp (Gross Profit) trên đơn hàng sau khi đã trừ giảm giá và voucher.

2. **Nhóm kiểm thử Concurrency & Race Condition**:
   - `TEST-CONC-01`: Giả lập 5 màn hình KDS bấm `Done` đồng thời cho 5 món ăn cùng sử dụng chung `Material` -> Xác minh xử lý retry khi gặp `DbUpdateConcurrencyException`, đảm bảo tồn kho bị trừ đủ 5 lần.
   - `TEST-CONC-02`: Giả lập khách quét QR gửi order và thu ngân POS bấm lưu order cho cùng 1 bàn tại cùng 1 tích tắc -> Xác minh chỉ có 1 Order duy nhất được kích hoạt hoặc gộp món an toàn.
   - `TEST-CONC-03`: Giả lập Webhook SePay và Thu ngân POS cùng xác nhận thanh toán đơn hàng tại cùng 1 mili-giây -> Xác minh cơ chế Idempotency chống nhân đôi tiền thanh toán.
   - `TEST-CONC-04`: Giả lập 2 đơn Takeaway sinh mã đơn cùng thời điểm -> Xác minh không bị va chạm trùng mã `OrderCode`.

3. **Nhóm kiểm thử Nguyên tử & Giao dịch (Transaction & Atomicity)**:
   - `TEST-TX-01`: Giả lập lỗi Exception ném ra trong quá trình trừ kho `DeductInventoryForOrderItemAsync` -> Xác minh trạng thái món ăn trên KDS bị rollback về `Processing`, không được giữ `Done`.
   - `TEST-TX-02`: Giả lập lỗi giữa chừng khi lập phiếu kiểm kê mù `CreateBlindStocktakeAsync` -> Xác minh không bị tạo ra phiếu rác không có items.
   - `TEST-TX-03`: Giả lập transient error kết nối SQL Server với `IExecutionStrategy` -> Xác minh giao dịch tự động retry thành công.

4. **Nhóm kiểm thử Nghiệp vụ Kho & Hủy đơn (Inventory & Waste)**:
   - `TEST-INV-01`: Trừ kho khi tồn kho không đủ (Insuffient / Zero stock) -> Xác minh chính sách cho phép tồn âm kèm cảnh báo hay chặn chế biến.
   - `TEST-INV-02`: Hủy đơn khi món ở trạng thái `Pending` (chưa nấu) -> Xác minh KHÔNG ghi nhận `CancellationWaste` và hoàn trả nguyên liệu.
   - `TEST-INV-03`: Hủy đơn khi món đã `Done` (đã nấu xong) -> Xác minh ghi nhận đúng chi phí hao hụt `CancellationWaste` vào báo cáo thất thoát.
   - `TEST-INV-04`: Thay thế test ảo `CancelOrder_DummyTest` bằng test tích hợp hủy đơn hoàn tiền và đối soát kho thực tế.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để kiểm chứng độc lập toàn bộ các kết luận trong báo cáo này, bất kỳ kỹ sư hoặc kiểm thử viên nào cũng có thể thực hiện theo các bước sau:

1. **Xác minh Test Suite hiện có và Dummy Test**:
   - Chạy lệnh test dự án:
     ```powershell
     dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj
     ```
     -> Kết quả sẽ hiển thị 295 Passed (0 Failed).
   - Mở file `src/Restaurant.Domain.Tests/CancelOrderServiceTests.cs` dòng 7-11 -> Trực tiếp quan sát thấy hàm `CancelOrder_DummyTest()` chỉ chứa `Assert.True(true);`.
   - Mở file `src/Restaurant.Domain.Tests/UnitTest1.cs` dòng 6-9 -> Trực tiếp quan sát thấy method `Test1()` rỗng.

2. **Xác minh Báo cáo Doanh thu & Món bán chạy là Mock UI**:
   - Mở file `src/Restaurant.Web/Components/Pages/Admin/Reports/RevenueReport.razor` dòng 18-80.
   - Quan sát thấy các chuỗi số tiền "128,450,000đ", "84,300,000đ" và các ngày 16/09/2026 - 18/09/2026 được viết cứng trong mã HTML. Block `@code` ở dòng 84-89 hoàn toàn không có bất kỳ lệnh gọi API backend nào.
   - Mở file `src/Restaurant.Web/Components/Pages/Admin/Reports/TopMoversReport.razor` dòng 24-56 -> Tương tự, tên món và số lượng bán được hardcode trong HTML, block `@code` rỗng.

3. **Xác minh Lỗ hổng Trừ kho KDS & Thiếu Transaction**:
   - Mở file `src/Restaurant.Infrastructure/Services/KitchenService.cs` dòng 201-227.
   - Quan sát thấy `await _context.SaveChangesAsync();` được gọi ở dòng 201 để lưu `item.KitchenStatus = Done`. Sau đó tại dòng 226 mới gọi `_mediator.Publish(...)`.
   - Mở file `src/Restaurant.Infrastructure/Services/InventoryService.cs` dòng 740-841 -> Quan sát hàm `DeductInventoryForOrderItemAsync`: hoàn toàn không có `using var dbTransaction = await _context.Database.BeginTransactionAsync();` và dòng 835 không có khối `catch (DbUpdateConcurrencyException)`.

4. **Xác minh Lỗi công thức Giá vốn và Báo cáo Hao hụt**:
   - Mở file `src/Restaurant.Infrastructure/Services/InventoryService.cs` dòng 158-165 -> Thấy rõ công thức `var oldStock = material.CurrentStock > 0 ? material.CurrentStock : 0;` triệt tiêu tồn âm.
   - Mở dòng 729-736 của `InventoryService.cs` -> Thấy rõ dòng code:
     `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0);` chứng minh số liệu hao hụt tự nhiên bị hardcode nhân với 0.35.

5. **Xác minh Sự vắng mặt của COGS và Sổ Cái Xuất Nhập Tồn**:
   - Thực hiện tìm kiếm chuỗi ký tự trên toàn bộ solution:
     ```powershell
     rg -i "CostOfGoodsSold|COGS|GrossProfit" "d:\My Project\src"
     ```
     -> Kết quả trả về rỗng (0 match), xác nhận hệ thống chưa có hạ tầng tính giá vốn hàng bán.
