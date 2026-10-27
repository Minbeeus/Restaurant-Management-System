# BÁO CÁO BÀN GIAO KIỂM TOÁN TỔNG THỂ (HANDOFF REPORT)
**Tác nhân**: Lead Architect & Synthesizer Agent (`worker_lead`)  
**Mã bàn giao**: `HANDOFF-LEAD-INVENTORY-BOM-SYNTHESIS-20261015`  
**Thư mục làm việc**: `d:\My Project\.agents\teamwork\worker_lead`  
**Thời điểm hoàn thành**: 2026-10-15T00:42:00Z  
**Phạm vi**: Tổng hợp, đối chiếu chéo và chuẩn hóa kết quả kiểm toán từ 4 Agent chuyên sâu về BOM, Inventory, StockLedger, POS/KDS Integration, Costing, Concurrency và Tests.

---

## 1. OBSERVATION (QUAN SÁT THỰC CHỨNG & BẰNG CHỨNG MÃ NGUỒN)

Từ việc thẩm tra trực tiếp mã nguồn tại `d:\My Project` và đối chiếu chéo 4 báo cáo của Agent 1, Agent 2, Agent 3, Agent 4:

1. **Lỗ hổng ủy quyền PIN Quản lý trên POS (`BUG-01` - P0)**:
   - File: `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs:207, 253`
   - File: `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs:1114-1118`
   - Quan sát thực chứng: Endpoint `CancelOrder` và `VoidOrderItem` khai báo `[Authorize(Roles = "Admin,Manager")]`. Tại `PosMain.razor.cs`, khi thu ngân nhập mã PIN Quản lý hợp lệ qua dialog, request HTTP POST gửi lên backend lại dùng Bearer token của Thu ngân (`Role = Cashier`). ASP.NET Core trả về `HTTP 403 Forbidden`. Code Blazor không kiểm tra `response.IsSuccessStatusCode`, tự động xóa giỏ hàng và báo cho người dùng "Đã hủy đơn hàng thành công!", nhưng trong DB đơn hàng **hoàn toàn không bị hủy**.

2. **Đứt gãy tính nguyên tử (Atomicity) khi Bếp làm xong món (`BUG-02` - P0)**:
   - File: `src/Restaurant.Infrastructure/Services/KitchenService.cs:201, 226`
   - File: `src/Restaurant.Infrastructure/Services/InventoryService.cs:835`
   - Quan sát thực chứng: Cập nhật `item.KitchenStatus = Done` gọi `SaveChangesAsync()` commit vào database ở dòng 201. Sau đó dòng 226 mới phát Domain Event qua MediatR đồng bộ để `InventoryService` gọi `SaveChangesAsync()` lần 2. Do `Material` có `RowVersion` nhưng không có try-catch `DbUpdateConcurrencyException`, khi 2 trạm bếp cùng hoàn thành món ăn dùng chung vật tư, pha 2 bị ném ngoại lệ 500. Trạng thái món ăn đã là `Done` nhưng tồn kho **không bị trừ**. Khi KDS retry, kiểm tra `oldStatus == newStatus` sẽ chặn vĩnh viễn việc phát lại event, gây mất khấu trừ kho vĩnh viễn.

3. **Vi phạm tính bất biến của Sổ Cái Kho (`BUG-03` - P0)**:
   - File: `src/Restaurant.Infrastructure/Services/CancelOrderService.cs:103`
   - Quan sát thực chứng: `deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;`. Đoạn code này sửa đè trực tiếp thuộc tính `TransactionType` của dòng lịch sử `SaleDeduction` cũ trong quá khứ thay vì tạo một dòng giao dịch đảo hoặc giao dịch hao hụt mới. Sổ cái bị phá vỡ nguyên tắc Append-Only.

4. **Bảng quy đổi đơn vị tính `UnitConversions` bị bỏ rơi & Làm tròn thập phân (`BUG-04` - P0)**:
   - File: `src/Restaurant.Infrastructure/Services/InventoryService.cs:774-782`
   - File: `src/Restaurant.Infrastructure/Migrations/20260723205718_InitialCreate.cs:345`
   - Quan sát thực chứng: `needed = ri.Quantity * orderItem.Quantity;` được trừ thẳng vào `material.CurrentStock` mà không hề tra cứu bảng `UnitConversions`. Nếu món cấu hình 150g và vật tư lưu kho theo Kg, hệ thống sẽ trừ 150kg. Đồng thời `ConversionFactor` bị giới hạn `decimal(18,2)`, khiến $1\text{g} = 0.001\text{kg}$ bị cắt cụt về `0.00`, gây sai số làm tròn $100\%$.

5. **Lỗ hổng thất thoát kho khi Void món lẻ (`BUG-05` - P0)**:
   - File: `src/Restaurant.Infrastructure/Services/PosOrderService.cs:924-972`
   - File: `src/Restaurant.Domain.Tests/E2E/Tier1_FeatureCoverageTests.cs:558-582`
   - Quan sát thực chứng: `VoidOrderItemAsync` chỉ đổi trạng thái `OrderItem.Status = Voided` và giảm SubTotal đơn hàng, hoàn toàn **không xử lý tồn kho**. Nếu món đã nấu xong (`Done`), nguyên liệu đã bị xuất kho ở dạng `SaleDeduction` nhưng không được chuyển sang `CancellationWaste` hay `CancellationReturn`. Bài test `F24` chỉ assert SubTotal mà không kiểm tra kho, che giấu lỗi này suốt thời gian qua.

6. **Báo cáo Doanh thu & Top Movers là HTML tĩnh (`BUG-06`, `BUG-07` - P0/P1)**:
   - File: `src/Restaurant.Web/Components/Pages/Admin/Reports/RevenueReport.razor:18-90`
   - File: `src/Restaurant.Web/Components/Pages/Admin/Reports/TopMoversReport.razor:24-57`
   - Quan sát thực chứng: 100% dữ liệu hiển thị (128,450,000đ, ngày 16-18/09/2026; 214 phần Pizza Margherita) là HTML tĩnh cứng trong file `.razor`. Khối `@code {}` không hề gọi backend API hay database.

7. **Báo cáo Thất thoát kho dùng hệ số giả lập 35% (`BUG-08` - P1)**:
   - File: `src/Restaurant.Infrastructure/Services/InventoryService.cs:734`
   - Quan sát thực chứng: `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0);`. Tiền hao hụt tự nhiên bị tính bằng cách nhân bừa 35% từ chênh lệch kiểm kê, bỏ qua toàn bộ dữ liệu `CancellationWaste` trong database.

8. **Thiếu `IExecutionStrategy` trên 15/16 Transaction Blocks (`BUG-10` - P1)**:
   - 15/16 khối transaction trong toàn solution gọi trực tiếp `_context.Database.BeginTransactionAsync()` mà không dùng `CreateExecutionStrategy()`, sẽ sập với `InvalidOperationException` khi chạy trên SQL Server có bật `EnableRetryOnFailure`.

9. **Test ảo `Assert.True(true)` (`BUG-14` - P1)**:
   - File: `src/Restaurant.Domain.Tests/CancelOrderServiceTests.cs:8-11` chỉ có `Assert.True(true);`.
   - File: `src/Restaurant.Domain.Tests/UnitTest1.cs:6-9` là method rỗng.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. Từ **Observation 1**: Thu ngân không thể hủy đơn/void món bằng PIN Quản lý vì Web client gửi token Cashier lên endpoint cấm Cashier. Thao tác bị server từ chối nhưng client nuốt lỗi báo thành công ảo. $\Rightarrow$ **Đơn hàng không bị hủy trong database, gây sai lệch doanh thu ca và thất thoát dòng tiền nghiêm trọng**.
2. Từ **Observation 2**: Việc tách commit 2 pha không có transaction/outbox kết hợp với `Material.RowVersion` concurrency token khiến việc trừ kho thất bại khi có nhiều bếp cùng thao tác. Pha 1 đã đánh dấu `Done`, pha 2 lỗi rollback kho, cơ chế chặn retry chặn luôn các lần gọi tiếp theo. $\Rightarrow$ **Món ăn hoàn thành nhưng kho không bị trừ, gây thất thoát kho ngầm vĩnh viễn**.
3. Từ **Observation 3**: `CancelOrderService` sửa đè `TransactionType` của các dòng lịch sử cũ. $\Rightarrow$ **Sổ cái kế toán bị phá vỡ tính bất biến, không còn khả năng đối soát kiểm toán quá khứ**.
4. Từ **Observation 4**: Bảng `UnitConversions` bị bỏ rơi và schema `decimal(18,2)` cắt cụt số thập phân nhỏ. $\Rightarrow$ **Hệ thống không thể định lượng gia vị nhỏ và nguy cơ trừ sai lệch 1000 lần khi đơn vị công thức khác đơn vị kho**.
5. Từ **Observation 5**: Void món lẻ sau khi bếp nấu xong không có logic xử lý kho. $\Rightarrow$ **Doanh thu bị trừ tiền nhưng chi phí nguyên liệu vẫn nằm ở tài khoản bán hàng, làm lệch tỷ suất lợi nhuận và mất kiểm soát hao hụt**.
6. Từ **Observation 6, 7**: Báo cáo doanh thu là HTML mock và báo cáo hao hụt nhân cứng 35%. $\Rightarrow$ **Phân hệ báo cáo quản trị BOH hiện tại không phản ánh dữ liệu kinh doanh thực tế**.
7. Từ **Observation 8, 9**: 15 transaction thiếu ExecutionStrategy và test suite chứa test rác chạy 100% In-Memory. $\Rightarrow$ **Chưa an toàn để triển khai lên SQL Server Production**.

---

## 3. CAVEATS (GIỚI HẠN PHẠM VI & GIẢ ĐỊNH)

1. **Tuân thủ quy tắc Read-Only**: Cuộc kiểm toán được thực hiện ở chế độ đọc 100%, tuyệt đối không sửa code, không tạo migration và không can thiệp database.
2. **Giả định về Kho vật lý**: Toàn bộ hệ thống hiện tại đang ánh xạ vào 1 kho tập trung duy nhất (`Material.CurrentStock`). Chưa xem xét kịch bản đa chi nhánh hoặc tách nhiều kho vật lý độc lập (Kho Bar / Kho Bếp / Kho Tổng).
3. **Môi trường Test**: 295 tests hiện tại chỉ chạy trên In-Memory DB, chưa bao gồm các bài test concurrency và deadlock thực tế trên SQL Server vật lý.

---

## 4. CONCLUSION (KẾT LUẬN & ĐẦU RA BẮT BUỘC)

Hệ thống quản lý nhà hàng Pizza hiện tại đã xây dựng được khung giao diện và các luồng chức năng bề mặt tương đối hoàn chỉnh, nhưng **CHƯA ĐỦ ĐIỀU KIỆN VẬN HÀNH THỰC TẾ (NOT PRODUCTION-READY)** do các lỗ hổng nghiêm trọng về an toàn giao dịch, toàn vẹn dữ liệu kho và độ tin cậy của báo cáo tài chính.

Nhóm Lead Architect đã hoàn tất biên soạn đầy đủ **3 tài liệu kiểm toán chính thức** tại thư mục `d:\My Project\docs\audit\`:
1. `docs/audit/INVENTORY_BOM_AUDIT.md`: Báo cáo kiểm toán kỹ thuật toàn diện 11 phần với đầy đủ bằng chứng code thực chứng, ma trận phân loại tính năng, phân tích luồng dữ liệu, transaction boundaries, concurrency và test suite.
2. `docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md`: Báo cáo chi tiết 6 quyết định nghiệp vụ then chốt (Tồn kho âm, Trách nhiệm hủy món, Định mức size pizza, Bán thành phẩm, Giá vốn bình quân, Quy trình chốt kiểm kê) trình Ban Giám đốc và Product Owner phê duyệt.
3. `docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md`: Kế hoạch hành động chi tiết 4 Sprint (từ Sprint 1 P0 đến Sprint 4 P3) kèm danh sách file cần sửa, giải pháp kỹ thuật, rủi ro và tiêu chí nghiệm thu (Acceptance Criteria) cụ thể cho từng đầu việc.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP TÁI KIỂM CHỨNG ĐỘC LẬP)

Để kiểm chứng độc lập toàn bộ các kết luận trong báo cáo:

1. **Kiểm tra 3 tài liệu kiểm toán đã tạo**:
   - `docs/audit/INVENTORY_BOM_AUDIT.md` (Kích thước ~32KB, 11 sections đầy đủ).
   - `docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md` (Kích thước ~15KB, 6 quyết định nghiệp vụ).
   - `docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md` (Kích thước ~18KB, 4 Sprints chi tiết).
2. **Kiểm chứng lỗi 403 PIN Quản lý**:
   - Mở `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs` dòng 207 và 253: Thấy rõ `[Authorize(Roles = "Admin,Manager")]`.
   - Mở `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs` dòng 1114-1118: Thấy rõ lệnh gọi API không kiểm tra status code, tự động xóa giỏ hàng và hiện toast thành công.
3. **Kiểm chứng lỗi sửa đè Sổ cái**:
   - Mở `src/Restaurant.Infrastructure/Services/CancelOrderService.cs` dòng 103: Thấy rõ `deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;`.
4. **Kiểm chứng Mock Data Báo cáo**:
   - Mở `src/Restaurant.Web/Components/Pages/Admin/Reports/RevenueReport.razor` dòng 18-39: Thấy rõ chuỗi số tiền "128,450,000đ" và các ngày tháng viết cứng trong HTML.
   - Mở `src/Restaurant.Infrastructure/Services/InventoryService.cs` dòng 734: Thấy rõ công thức `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0);`.
5. **Kiểm chứng Test Ảo**:
   - Mở `src/Restaurant.Domain.Tests/CancelOrderServiceTests.cs` dòng 8-11: Thấy rõ `Assert.True(true);`.
