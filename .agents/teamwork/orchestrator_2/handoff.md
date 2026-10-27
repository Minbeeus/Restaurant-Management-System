# BÁO CÁO BÀN GIAO ĐIỀU PHỐI KIỂM TOÁN (ORCHESTRATOR HANDOFF REPORT)
**Tác nhân**: Project Orchestrator (`orchestrator_2`)  
**Mã bàn giao**: `HANDOFF-ORCHESTRATOR-INVENTORY-BOM-AUDIT-20261015`  
**Thư mục làm việc**: `d:\My Project\.agents\teamwork\orchestrator_2`  
**Parent Conversation ID (Sentinel)**: `f4310805-cbf8-4224-b935-f312a7431f6c`  
**Thời điểm hoàn thành**: 2026-10-15T00:43:00Z  

---

## 1. MILESTONE STATE & EXECUTION SUMMARY

| Milestone / Subtask | Phân công | Agent Conv ID | Trạng thái | Báo cáo đầu ra |
|---|---|---|:---:|---|
| **Agent 1**: BOM, Định mức & Quy đổi đơn vị | `explorer_agent1` | `d99b62f4-1dda-4235-96dd-4f35f5de1f3e` | **DONE** | `.agents/teamwork/explorer_agent1/handoff.md` |
| **Agent 2**: Nhập/Xuất kho, Kiểm kê & StockLedger | `explorer_agent2` | `ae7e4876-e643-477a-96f0-077d2d5c63d9` | **DONE** | `.agents/teamwork/explorer_agent2/handoff.md` |
| **Agent 3**: Luồng POS/KDS, Trừ kho, Hủy món/đơn | `explorer_agent3` | `e8310e0c-f7bd-402b-8db1-1f5754034d7f` | **DONE** | `.agents/teamwork/explorer_agent3/handoff.md` |
| **Agent 4**: Giá vốn, Báo cáo, Transactions & Tests | `explorer_agent4` | `d0c64b03-ecac-4441-a22e-13c9bb9973f1` | **DONE** | `.agents/teamwork/explorer_agent4/handoff.md` |
| **Lead Agent**: Đối chiếu chéo & Biên soạn tài liệu | `worker_lead` | `c837ffd2-646c-450a-b5cf-21c205948afc` | **DONE** | 3 tài liệu chính thức tại `docs/audit/` |

---

## 2. OBSERVATION & KEY AUDIT FINDINGS

Cuộc kiểm toán đa tác nhân độc lập đã thẩm tra toàn diện mã nguồn tại `d:\My Project` ở chế độ **READ-ONLY 100%** (không sửa code, không tạo migration, không can thiệp database) và phát hiện 14 lỗi thực tế nghiêm trọng:

1. **Lỗ hổng ủy quyền PIN Quản lý tại POS (`BUG-01` - P0 - BROKEN)**:
   - `PosOrdersController.cs:207, 253` yêu cầu `[Authorize(Roles = "Admin,Manager")]`.
   - `PosMain.razor.cs:1114-1118` gửi token của Thu ngân (`Role = Cashier`), bị trả về `HTTP 403 Forbidden`. Code Blazor không kiểm tra status code, tự xóa giỏ hàng và báo "Hủy thành công" trong khi đơn hàng **hoàn toàn không bị hủy trong database**.
2. **Đứt gãy tính nguyên tử khi Bếp hoàn thành món (`BUG-02` - P0 - BROKEN)**:
   - `KitchenService.cs:201` lưu `KitchenStatus = Done` vào DB. Đến dòng 226 mới phát Domain Event trừ kho. Khi 2 trạm bếp cùng hoàn thành món ăn dùng chung vật tư có `Material.RowVersion`, lần 2 bị ném `DbUpdateConcurrencyException`. Trạng thái món là `Done` nhưng tồn kho **vĩnh viễn không bị trừ**, và hệ thống chặn mọi lượt retry.
3. **Phá vỡ tính bất biến của Sổ Cái Kho (`BUG-03` - P0 - BROKEN)**:
   - `CancelOrderService.cs:103` sửa đè trực tiếp: `deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;`. Sổ cái bị phá vỡ nguyên tắc Append-Only, thiếu `UnitPrice`, `TotalCost`, `BalanceBefore`.
4. **Bảng quy đổi `UnitConversions` bị bỏ rơi & Lỗi làm tròn số học (`BUG-04` - P0 - BACKEND_ONLY/BROKEN)**:
   - `InventoryService.cs:774-782` trừ kho thô mà không tra cứu `UnitConversions`. Cột `ConversionFactor` bị khóa cứng `decimal(18,2)`, khiến $1\text{g} = 0.001\text{kg}$ bị cắt cụt về `0.00` ($100\%$ sai số).
5. **Thất thoát kho khi Void món lẻ (`BUG-05` - P0 - BROKEN)**:
   - `PosOrderService.cs:924-972` chỉ đổi `OrderItem.Status = Voided` và giảm SubTotal, hoàn toàn không chuyển nguyên liệu đã nấu thành `CancellationWaste` hay hoàn kho.
6. **Báo cáo Doanh thu & Món bán chạy là Mock HTML tĩnh (`BUG-06`, `BUG-07` - P0/P1 - UI_ONLY)**:
   - `RevenueReport.razor` và `TopMoversReport.razor` hiển thị 100% dữ liệu cứng trong HTML (doanh thu 128,450,000đ, ngày 16-18/09/2026), không kết nối backend API. Giá vốn hàng bán (COGS) và Lãi gộp hoàn toàn chưa phát triển (0%).
7. **Báo cáo thất thoát nhân bừa tỷ lệ 35% (`BUG-08` - P1 - BROKEN)**:
   - `InventoryService.cs:734` tính `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0);` từ chênh lệch kiểm kê, bỏ qua dữ liệu thực trong sổ cái.
8. **Kiểm kê ghi đè triệt tiêu hàng bán trong ngày (`BUG-09` - P1 - BROKEN)**:
   - `ReconcileStocktakeAsync` gán đè `material.CurrentStock = finalActual`, xóa sạch lượng hàng đã bán trong khoảng thời gian kiểm kê; cho phép reconcile nhiều lần gây sai lệch lũy kế.
9. **Thiếu `IExecutionStrategy` trên 15/16 Transaction Blocks (`BUG-10` - P1 - BROKEN)**:
   - Sẽ sập với `InvalidOperationException` khi kích hoạt `EnableRetryOnFailure` trên SQL Server.
10. **Lệch ID đơn vị tính trên UI (`BUG-11` - P1 - BROKEN)**:
    - `MaterialManagement.razor` hardcode giá trị 6 là "Lon", nhưng trong DB ID 6 là "Gói/Bịch".
11. **Bao bì Combo Header không bao giờ bị trừ kho (`BUG-12` - P1 - BROKEN)**:
    - `KitchenService.cs` chặn `isComboHeader`.
12. **Lọc NCC bằng chuỗi substring tiếng Việt (`BUG-13` - P2 - PARTIAL)**:
    - `InventoryService.cs:337-338` hardcode chuỗi "ý việt", "đà lạt".
13. **Test suite chứa test ảo (`BUG-14` - P1 - BROKEN)**:
    - `CancelOrderServiceTests.CancelOrder_DummyTest` (`Assert.True(true)`) và `UnitTest1.Test1()` (rỗng).

---

## 3. MANDATORY DELIVERABLES CREATED & VERIFIED

Lead Synthesizer Agent đã biên soạn đầy đủ 3 tài liệu chính thức tại `docs/audit/`:

1. **`docs/audit/INVENTORY_BOM_AUDIT.md`** (740 dòng, ~74KB):
   - Đầy đủ 11 sections chi tiết: Executive Summary, Comprehensive Feature Matrix (19 chức năng phân loại), Architecture & Data Flow, BOM & Unit Conversion, Inventory Lifecycle & StockLedger, POS/KDS Integration & Cancellation Matrix, Costing & Financial Reports, Concurrency & Transactions, Bugs & Evidence thực chứng (14 bugs), Test Suite Coverage & 15 Missing Tests, và Cross-Cutting Recommendations.
2. **`docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md`** (231 dòng, ~21KB):
   - 6 quyết định nghiệp vụ chiến lược kèm phân tích ưu/nhược điểm, phân tích rủi ro và khuyến nghị chuyên gia cho Product Owner:
     * QĐ 1: Chính sách tồn kho âm (Đề xuất 1B: Cho phép bán âm có kiểm soát kèm đối soát sau).
     * QĐ 2: Ma trận trách nhiệm hủy món (Đề xuất 2A: ReturnToStock vs RecordAsWaste theo PIN Quản lý).
     * QĐ 3: Định mức Pizza theo Size (Đề xuất 3B: Co giãn tỷ lệ diện tích kết hợp vật tư thay thế).
     * QĐ 4: Bán thành phẩm / Multi-level BOM (Đề xuất 4B: Giữ BOM phẳng cho UAT, nâng cấp Lệnh sơ chế ở Phase 2).
     * QĐ 5: Phương pháp tính giá vốn chính thức (Đề xuất 5A: Bình quân gia quyền di động chuẩn mực).
     * QĐ 6: Quy trình kiểm kê định kỳ (Đề xuất 6B: Kiểm kê động theo độ lệch Delta).
3. **`docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md`** (405 dòng, ~29KB):
   - Lộ trình kỹ thuật 4 Sprint từ Sprint 1 (P0) đến Sprint 4 (P3), đầy đủ tệp tin cần sửa, giải pháp kỹ thuật, rủi ro và Tiêu chí nghiệm thu (Acceptance Criteria) cụ thể cho từng đầu việc.

---

## 4. VERIFICATION & INTEGRITY COMPLIANCE

- **Zero-Modification Rule**: Tuyệt đối không có dòng code nào trong `src/` bị sửa đổi; không có migration mới nào được tạo; cơ sở dữ liệu không bị can thiệp.
- **Evidence-Based**: Mọi phát hiện và phân loại đều đi kèm đường dẫn tệp tin, số dòng và đoạn mã nguồn đối chứng chính xác.
- **Background Processes Clean**: Recurring cron `task-10` đã được hủy triệt để, không còn tác vụ chạy ngầm nào tồn đọng.

## 5. NEXT STEPS FOR SENTINEL / IMPLEMENTATION TEAM

1. Trình tài liệu `docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md` lên Product Owner / Ban Giám Đốc để chốt phương án cho 6 quyết định nghiệp vụ.
2. Chuyển giao `docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md` cho Nhóm Kỹ thuật để triển khai Sprint 1 (P0 - Toàn vẹn dữ liệu & An toàn giao dịch cốt lõi).
