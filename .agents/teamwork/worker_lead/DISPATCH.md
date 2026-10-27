## 2026-10-14T17:34:48Z
You are the Lead Architect & Synthesizer Agent in the comprehensive POS/KDS restaurant workspace audit at d:\My Project.

Your working directory: d:\My Project\.agents\teamwork\worker_lead
Master request file: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md (Read especially section ## 2026-10-14T17:16:42Z).

CRITICAL CONSTRAINTS:
1. TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.
2. Bạn CHỈ tạo và ghi 3 tài liệu báo cáo kiểm toán Markdown vào thư mục `docs/audit/` và tài liệu handoff.md trong working directory của bạn.

INPUT SOURCES:
4 báo cáo kiểm toán chuyên sâu độc lập từ 4 Agent đã hoàn thành:
1. Agent 1 (BOM, định mức và quy đổi đơn vị): `d:\My Project\.agents\teamwork\explorer_agent1\handoff.md`
2. Agent 2 (Nhập kho, xuất kho, kiểm kê và StockLedger): `d:\My Project\.agents\teamwork\explorer_agent2\handoff.md`
3. Agent 3 (Luồng POS/KDS, trừ kho, hủy món và gọi thêm): `d:\My Project\.agents\teamwork\explorer_agent3\handoff.md`
4. Agent 4 (Giá vốn, báo cáo, transaction và test): `d:\My Project\.agents\teamwork\explorer_agent4\handoff.md`

NHIỆM VỤ CỦA BẠN:
Đọc toàn bộ 4 báo cáo trên, đối chiếu chéo, giải quyết các phát hiện trùng lặp, thống nhất các mã phân loại (DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE) và mức độ ưu tiên (P0-P3), giữ nguyên đầy đủ bằng chứng code thực chứng (file path, line numbers, code snippets).

TẠO ĐỦ 3 TÀI LIỆU CHÍNH THỨC VÀO THƯ MỤC `d:\My Project\docs\audit\`:
1. `d:\My Project\docs\audit\INVENTORY_BOM_AUDIT.md` (11 sections)
2. `d:\My Project\docs\audit\INVENTORY_BOM_BUSINESS_DECISIONS.md` (6 business decisions)
3. `d:\My Project\docs\audit\INVENTORY_BOM_REMEDIATION_PLAN.md` (Sprint 1 to 4 with task details)
