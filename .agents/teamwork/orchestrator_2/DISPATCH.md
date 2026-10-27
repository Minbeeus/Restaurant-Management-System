## 2026-10-14T17:18:18Z
You are the Project Orchestrator (orchestrator_2) for the comprehensive POS/KDS restaurant workspace audit at d:\My Project.

Your working directory: d:\My Project\.agents\teamwork\orchestrator_2
Project workspace: d:\My Project
Original user request file: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md (see section ## 2026-10-14T17:16:42Z)

CRITICAL INSTRUCTIONS & CONSTRAINTS:
1. TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.
2. Read the master audit request in d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md carefully.
3. Coordinate the requested multi-agent audit structure:
   - Lead Agent (Architect & Synthesizer): Đối chiếu kết quả, giải quyết các phát hiện trùng lặp và tổng hợp báo cáo.
   - Agent 1: BOM, định mức và quy đổi đơn vị (Đơn vị mua/kho/BOM, hệ số quy đổi thập phân, định mức món đơn, combo, topping/modifier, bán thành phẩm).
   - Agent 2: Nhập kho, xuất kho, kiểm kê và StockLedger (Vòng đời phiếu, tính bất biến, chống cộng/trừ trùng kho Idempotency, cấu trúc StockLedger, chính sách tồn âm).
   - Agent 3: Luồng POS/KDS, trừ kho, hủy món và gọi thêm (Thời điểm khấu trừ kho thực tế, rủi ro trừ trùng khi retry/reconnect, ma trận hủy món trước/trong/sau nấu, hoàn kho vs hao hụt waste).
   - Agent 4: Giá vốn, báo cáo, transaction và test (Phương pháp tính giá vốn FIFO/bình quân gia quyền, tính nhất quán doanh thu - giá vốn - tiêu hao, transaction boundaries, lock concurrency, test suite coverage).
4. Classify all findings using: DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE. Prioritize P0 to P3. Provide exact file paths, class/method/API names, and code evidence.
5. Mandatory deliverables:
   - docs/audit/INVENTORY_BOM_AUDIT.md (11 sections)
   - docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md
   - docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md
6. Maintain progress.md and BRIEFING.md in your working directory at every key milestone.
7. When complete, write handoff.md in your working directory and notify the Sentinel.
