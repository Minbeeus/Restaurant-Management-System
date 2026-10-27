## 2026-10-14T17:20:09Z
[Message] sender=326badef-f4ff-4494-a54a-1a9ac14c000c priority=MESSAGE_PRIORITY_HIGH
content=You are Agent 4 (Giá vốn, báo cáo, transaction và test Auditor) in the multi-agent comprehensive audit for the POS/KDS workspace at d:\My Project.

Your working directory: d:\My Project\.agents\teamwork\explorer_agent4
Master request file: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md (MUST read the full file, especially section ## 2026-10-14T17:16:42Z).

CRITICAL CONSTRAINTS:
1. TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.
2. Read-only deep code investigation. Follow the full data flow from UI -> Controller -> Application/Domain -> DB and analyze tests.

YOUR AUDIT SCOPE:
- Phương pháp tính giá vốn (Costing methods):
  - Hệ thống tính giá vốn món ăn (Recipe Cost) và giá vốn hàng bán (COGS - Cost of Goods Sold) theo phương pháp nào? (FIFO, Bình quân gia quyền - Moving/Weighted Average, hay Giá vốn danh nghĩa / Giá mua gần nhất / Fixed Cost)?
  - Công thức tính giá vốn có phản ánh đúng biến động giá nhập kho thực tế không?
- Tính nhất quán giữa Doanh thu - Giá vốn - Tiêu hao:
  - Báo cáo doanh thu và báo cáo kho/giá vốn có liên kết với nhau không?
  - Có tính được lãi gộp (Gross Profit) theo món, theo ca làm việc (Shift), theo ngày không?
  - Các báo cáo kho hiện có (Báo cáo xuất nhập tồn, Báo cáo hao hụt, Báo cáo tồn thấp, Báo cáo kiểm kê lệch): thực tế đã implement hay chỉ là UI mock data?
- Transaction Boundaries & Lock Concurrency:
  - Các thao tác trừ kho, nhập kho, kiểm kê, thanh toán có được bọc trong Database Transaction (`BeginTransactionAsync` / `IExecutionStrategy`) không?
  - Có nguy cơ Race Condition khi nhiều bàn cùng gọi món, nhiều thu ngân cùng thanh toán, hoặc bếp bấm Done đồng thời trên nhiều màn hình KDS không?
  - Có sử dụng Concurrency Token (`IsRowVersion`, optimistic locking) hoặc pessimistic locking (`UPDLOCK`) trên tồn kho không?
- Đánh giá Test Suite hiện có:
  - Rà soát toàn bộ test cases hiện có trong `src/Restaurant.Domain.Tests` và các dự án test khác.
  - Test nào kiểm tra Inventory, BOM, Stocktake, Deduction, Cancellation?
  - Đánh giá độ bao phủ thực tế và chỉ rõ toàn bộ các kịch bản kiểm thử còn thiếu (Missing Tests): ví dụ thiếu test tồn âm, thiếu test race condition, thiếu test idempotency, thiếu test hủy đơn sau nấu...

EVALUATION & CLASSIFICATION CRITERIA:
- Classify every finding using: DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE.
- Prioritize: P0 (Critical), P1 (High), P2 (Medium), P3 (Low).
- Every finding MUST cite exact file path, line numbers, class/method name, and code snippet evidence.

DELIVERABLE:
Write a comprehensive, exhaustive audit report to:
`d:\My Project\.agents\teamwork\explorer_agent4\handoff.md`
Update your `progress.md` during investigation.
When complete, notify the orchestrator via send_message with a summary and path to your handoff.md.
