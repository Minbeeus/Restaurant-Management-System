# Original User Request

## Initial Request — 2026-10-11T19:02:22Z

Hoàn thiện toàn bộ hệ thống quản lý nhà hàng Pizza (POS, KDS, BOH Admin & QR Ordering) trên codebase hiện có tại d:\My Project, khắc phục triệt để các lỗ hổng P0, hoàn thiện các khiếm khuyết P1/P2/P3 được xác định trong docs/audit/FULL_WORKSPACE_AUDIT.md, đảm bảo an toàn giao dịch, bảo toàn dữ liệu và sẵn sàng cho UAT.

Working directory: d:\My Project
Integrity mode: development

## Context & Baseline Resources

- Solution: src/Restaurant.sln (ASP.NET Core 9 / .NET 9, Blazor Server, SQL Server, EF Core 9, SignalR, xUnit)
- Audit Report: docs/audit/FULL_WORKSPACE_AUDIT.md (Đã rà soát toàn bộ 6 project, 42 DbSets, 23 Controllers, 80 tests)
- Spec Kit: docs/specs/ (POS-spec.md, KDS-spec.md, BOH-spec.md, ACC-spec.md, QR-spec.md)
- Trạng thái hiện tại: Build thành công (0 Warning, 0 Error), 80/80 tests passed, sơ đồ bàn 34 bàn 1 sàn duy nhất (zero collision) đã chuẩn hóa và không được sửa lại.

## Phân công đội ngũ đa tác nhân:
- Agent 0 (Lead Architect & Integrator): Điều phối tổng thể, tích hợp code, kiểm soát schema, migration và nghiệm thu.
- Agent 1 (Security & Auth - P0/P1): Sửa lỗi VerifyManagerPinAsync, gỡ bỏ [AllowAnonymous] trên các endpoint nhạy cảm, khôi phục chuỗi xác thực Blazor -> WebAPI.
- Agent 2 (POS & Cashier - P0/P2): Bổ sung nút thanh toán trực tiếp tại quầy, bảo vệ thanh toán tiền mặt và chuyển khoản SePay trong DB transaction.
- Agent 3 (Admin & Reliability - P1): Xóa bỏ dữ liệu mock tĩnh trong AdminApiClient, lưu trữ SystemSettings bền vững vào database.
- Agent 4 (KDS & Realtime - P1): Xác thực SignalR RestaurantHub, bảo vệ nhóm kết nối, bảo toàn trạng thái đơn Ready khi nấu xong.
- Agent 5 (Inventory & BOM - P2): Toàn vẹn dữ liệu trừ kho tự động, xử lý tồn kho âm và hao hụt khi hủy đơn.
- Agent 6 (Split Bill & Printing - P3): Triển khai tính năng tách hóa đơn (POS-008) và lớp trừu tượng in nhiệt K80 ESC/POS.
- Agent 7 (QA & Testing): Viết integration test cho 401/403, kiểm thử toàn bộ luồng nghiệp vụ, đảm bảo 100% tests pass và không có lỗi hồi quy.

---

## Requirements

### R1. Khắc phục triệt để lỗ hổng Bảo mật & Xác thực (P0 & P1)
- Sửa hàm IdentityService.VerifyManagerPinAsync:
  - Loại bỏ điều kiện u.Username == "admin" khỏi truy vấn LINQ.
  - Loại bỏ các mã PIN hardcoded (000000, 000003, 123456).
  - Xác thực mã PIN bằng hàm băm BCrypt.Verify(pinCode, u.PinCodeHash) của tài khoản có vai trò Manager hoặc Admin.
  - Bổ sung giới hạn số lần thử sai (Rate limiting / Lockout chống brute-force) tương tự PIN Login.
- Khôi phục chuỗi xác thực Blazor -> WebAPI:
  - Hoàn thiện luồng đăng nhập PIN tại SharedLogin.razor.cs: sau khi xác thực PIN thành công, thiết lập Cookie xác thực RestaurantAuth và lưu trữ JWT an toàn cho circuit.
  - Đảm bảo các HTTP client (AdminApiClient, HttpClient) tự động đính kèm Authorization: Bearer <token> hợp lệ khi gọi API backend.
- Đóng toàn bộ endpoint nhạy cảm bị mở [AllowAnonymous]:
  - Gỡ bỏ [AllowAnonymous] và áp dụng [Authorize(Roles = ...)] chuẩn mực tại:
    - PosOrdersController: Hủy đơn (CancelOrder), Hủy món (VoidOrderItem), Chuyển bàn (MoveTable), Gộp bàn (MergeTables), Duyệt đơn QR (ApproveOrder).
    - PaymentsController: Thanh toán tiền mặt (CashPayment).
    - ShiftsController: Mở ca (OpenShift), Đóng ca (CloseShift), Lấy ca hiện tại (GetCurrentShift).
    - CashierToolsController: Nộp tiền két (TillIn), Rút tiền két (TillOut), Điều chỉnh tiền (CashAdjustment).
    - ApprovalCenterController: Duyệt yêu cầu (ApproveRequest), Từ chối (RejectRequest), Duyệt bằng PIN (ApproveByManagerPin).
    - ReconciliationController: Đối soát ca (SaveShiftReconciliation, GetShiftReconciliationPreview).
    - VouchersController: CRUD Voucher (CreateVoucher, UpdateVoucher, DeleteVoucher).
    - DashboardController: Bổ sung [Authorize(Roles = "Admin,Manager")] cho các API thống kê doanh thu.
- Xóa fallback định danh Admin mặc định:
  - Loại bỏ mọi câu lệnh var userId = currentUserService.UserId ?? 1;. Bắt buộc đọc danh tính từ claims đã xác thực; nếu thiếu token, trả về 401 Unauthorized.

### R2. Hoàn thiện luồng Thanh toán POS & Nghiệp vụ Quầy Thu ngân (P0 & P2)
- Bổ sung nút "Thanh Toán" trực tiếp trên thanh công cụ của PosMain.razor (và/hoặc chân giỏ hàng OrderCart.razor) khi đơn hàng đã có món và hợp lệ.
- Đảm bảo luồng mở modal thanh toán (PosPaymentModal) hỗ trợ cả 2 phương thức: Tiền mặt (CashPayment) và Chuyển khoản VietQR (GenerateVietQr + SePay Webhook).
- Bảo vệ giao dịch thanh toán trong Database Transaction, chống xử lý trùng (Idempotent), tự động cập nhật trạng thái đơn, giải phóng bàn và khấu trừ kho đúng quy định.
- Giữ nguyên tính toàn vẹn của in hóa đơn K80 tạm tính (K80ReceiptModal), không tự ý đánh dấu đơn là đã thanh toán khi mới in phiếu tạm tính.

### R3. Độ tin cậy API Quản trị & Lưu trữ Cấu hình Bền vững (P1)
- Loại bỏ dữ liệu Mock tĩnh trong AdminApiClient.cs:
  - Xóa toàn bộ các hàm fallback ngầm GetMockMaterials(), GetMockSuppliers(), GetMockOrders(), GetMockCustomers(), GetMockVouchers().
  - Khi API lỗi hoặc mất mạng, hiển thị thông báo lỗi tường minh (Toast / Alert) trên giao diện thay vì hiển thị dữ liệu giả lập.
- Lưu trữ Cấu hình Hệ thống (SystemSettings) vào Database:
  - Thay thế biến RAM static SystemSettingsDto _currentSettings trong SystemController.cs bằng Entity và bảng cơ sở dữ liệu SystemSettings trong EF Core.
  - Tạo Migration an toàn và bảo đảm cấu hình nhà hàng (Tên, địa chỉ, hotline, VAT) vẫn tồn tại sau khi restart server.

### R4. Bảo mật SignalR & Toàn vẹn Trạng thái KDS Realtime (P1)
- Bảo vệ RestaurantHub: Yêu cầu xác thực token JWT khi kết nối Hub; loại bỏ việc tin cậy query string clientType=pos hay clientType=kds để tự ý gán nhóm quyền.
- Bảo đảm phân quyền nhóm SignalR (CashiersGroup, ChefsGroup, StationGroup) dựa trên Claims danh tính thực tế.
- Giữ nguyên quy tắc nghiệp vụ: Khi tất cả món ăn trên KDS hoàn thành (Done), đơn hàng chuyển sang trạng thái Ready (Sẵn sàng phục vụ), tuyệt đối không tự ý đánh dấu thành Paid hoặc Completed trước khi thu ngân thanh toán.
- Đảm bảo việc phát Domain Event khấu trừ kho nguyên liệu BOM (OrderItemKitchenDoneDomainEvent) chỉ thực hiện đúng 1 lần cho mỗi món ăn.

### R5. Toàn vẹn Dữ liệu Kho, BOM & Báo cáo Doanh thu (P2)
- Kiểm tra tính toàn vẹn của việc trừ kho tự động khi món ăn hoàn thành, nhập kho tăng tồn, kiểm kê điều chỉnh tồn và ghi nhận thất thoát.
- Xử lý hoàn trả tồn kho hoặc ghi nhận hao hụt hủy đơn (CancellationWaste) khi đơn bị hủy theo đúng quy trình CancelOrderService.
- Bổ sung kiểm thử unit test và integration test cho toàn bộ các phương thức của InventoryService (BOM deduction, Goods receipt, Stocktake balance).

### R6. Triển khai Tính năng Tách Hóa Đơn (Split Bill POS-008) & Cấu hình In Nhiệt (P3)
- Tách hóa đơn (Split Bill):
  - Xây dựng giao diện modal chọn món/số lượng cần tách từ đơn gốc sang đơn mới.
  - Backend API xử lý việc tạo đơn mới, giảm số lượng món ở đơn cũ, tính lại Subtotal, VAT 8%, Voucher/Discount cho cả 2 đơn trong 1 Database Transaction.
  - Kiểm tra ràng buộc: không cho tách đơn đã thanh toán hoặc đơn đã hủy.
- In hóa đơn K80:
  - Duy trì hỗ trợ in qua trình duyệt và xuất PDF hiện có.
  - Xây dựng lớp trừu tượng (Service/Adapter) cấu hình cổng in mạng LAN ESC/POS (IP:Port 9100) để sẵn sàng cấu hình thiết bị phần cứng thực tế.

### R7. Kiểm thử Toàn diện & Xác thực An ninh (QA & Testing)
- Bổ sung bộ test kiểm tra bảo mật (Security Integration Tests):
  - Gửi request ẩn danh tới các endpoint nhạy cảm -> Xác minh nhận mã 401 Unauthorized.
  - Cashier cố gọi API của Manager/Admin -> Xác minh nhận mã 403 Forbidden.
  - Nhập sai PIN hoặc thử vượt quyền -> Bị từ chối.
- Chạy toàn bộ test suite cũ (80 tests) và test mới -> Đảm bảo 100% tests Passed, không có lỗi hồi quy.
- Cập nhật đầy đủ các tài liệu:
  - docs/audit/FULL_WORKSPACE_AUDIT.md
  - docs/audit/IMPLEMENTATION_COMPLETION_REPORT.md
  - docs/audit/BUSINESS_DECISIONS_REQUIRED.md

---

## Acceptance Criteria

### 1. An ninh & Xác thực
- [ ] Endpoint /api/v1/auth/verify-manager-pin chỉ trả về true khi mã PIN khớp với mã băm của User có Role Manager/Admin trong DB. Nhập mã PIN sai hoặc các mã 000000, 123456, 999999 đều trả về false.
- [ ] Toàn bộ các endpoint nhạy cảm (Hủy đơn, Void món, Đổi/Gộp bàn, Thanh toán tiền mặt, Mở/Đóng ca, Két tiền, Duyệt yêu cầu, Voucher, Thống kê) trả về HTTP 401 Unauthorized nếu không có Bearer token.
- [ ] Đăng nhập bằng mã PIN trên giao diện Web (SharedLogin) thiết lập phiên làm việc đầy đủ, các lời gọi API từ Blazor tự động gửi Token hợp lệ mà không bị lỗi 401.
- [ ] Không còn bất kỳ mã PIN hardcoded hoặc fallback UserId = 1 nào trong code logic nghiệp vụ.

### 2. POS & Thanh toán
- [ ] Giao diện PosMain.razor có nút "Thanh Toán" trực tiếp, mở PosPaymentModal để thu tiền mặt hoặc sinh mã VietQR.
- [ ] Thanh toán tiền mặt và chuyển khoản SePay hoàn thành thành công, cập nhật trạng thái đơn thành Paid, đổi trạng thái bàn thành Available và kích hoạt in hóa đơn K80.
- [ ] Tính năng Tách hóa đơn (Split Bill) cho phép tách một phần món sang đơn mới, tổng tiền của 2 đơn sau khi tách khớp chính xác với đơn gốc.

### 3. KDS & BOH
- [ ] Khi bếp bấm hoàn thành tất cả món, đơn hàng chuyển sang trạng thái Ready, không bị tự đổi sang Paid hay Completed.
- [ ] Tồn kho nguyên vật liệu BOM bị khấu trừ chính xác trong DB khi món hoàn thành trên KDS.
- [ ] AdminApiClient.cs không còn bất kỳ hàm fallback mock nào; giao diện hiển thị thông báo lỗi khi API bị ngắt kết nối.
- [ ] Cài đặt hệ thống (SystemSettings) được lưu trong database và giữ nguyên sau khi khởi động lại ứng dụng.

### 4. Build, Test & Tài liệu
- [ ] dotnet build src/Restaurant.sln thành công 100% với 0 Error, 0 Warning.
- [ ] dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj vượt qua 100% (gồm 80 tests cũ + các tests mới).
- [ ] Báo cáo docs/audit/IMPLEMENTATION_COMPLETION_REPORT.md được tạo mới với đầy đủ bằng chứng kiểm thử và kết quả xác minh.
