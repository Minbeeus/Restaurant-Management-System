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


## 2026-10-12T18:42:02Z

This is a single self-contained fix; keep it small and focused.
Requested team: Small, focused team.

Tối ưu hóa toàn diện giao diện và nghiệp vụ POS Order: cố định sốt pizza theo công thức (không chọn sốt và không hiện tên sốt trong tên món), bỏ nhóm tùy chọn Mức độ cay (nhân viên tự ghi chú vào note), thêm ô ghi chú tự do cho CSR trên tất cả các món, loại bỏ nút "Tất cả nhóm", cập nhật thẻ món trong giỏ hàng (hiển thị số lượng ở đầu, đơn giá 1 phần ở góc phải), chuyển giao diện chọn món Combo sang dạng thẻ text không ảnh đồng bộ với POS, áp dụng giá món niêm yết đã bao gồm VAT, và bỏ nút "Sơ đồ bàn" đồng thời đổi nút "Xác Nhận & Gửi Bếp" thành "Hoàn Thành" với popup xác nhận hủy order khi giỏ hàng trống.

Working directory: D:\My Project
Integrity mode: development

## Requirements

### R1. Chuẩn hóa Sốt Pizza và Loại bỏ Mức Độ Cay
- Mỗi loại bánh pizza được gán một loại sốt nền cố định theo công thức chuẩn. Khi nhân viên chọn bánh, không hiển thị nhóm tùy chọn "Chọn Sốt Nền Pizza" và không hiển thị tên sốt trong tên món hay danh sách thành phần tùy chọn.
- Loại bỏ hoàn toàn nhóm tùy chọn "Mức Độ Cay" trên giao diện cấu hình món. Độ cay mặc định đi theo sốt của bánh; nhân viên tự trao đổi với khách và ghi chú vào ô ghi chú tự do của món nếu khách có yêu cầu riêng.

### R2. Bổ sung Ghi chú Tự do (CSR Custom Note) cho Tất cả Món ăn
- Bổ sung trường nhập ghi chú dạng text tự do trên giao diện cấu hình tùy chọn của mọi món ăn (áp dụng cho toàn bộ menu: Pizza, món lẻ, đồ uống, combo).
- Nội dung ghi chú được gắn trực tiếp vào món khi thêm vào giỏ hàng, hiển thị rõ ràng trên thẻ món trong giỏ hàng và đồng bộ khi gửi xuống bếp (KDS) và in bill.

### R3. Loại bỏ Nút "Tất Cả Nhóm" trong Bộ lọc Tùy chọn
- Loại bỏ nút "TẤT CẢ NHÓM" trên thanh tab phân loại nhóm tùy chọn tại modal/giao diện tùy chọn món (PosOptionBuilder) và giao diện chọn combo (PosComboBuilder).
- Giao diện tự động kích hoạt mặc định nhóm tùy chọn đầu tiên thay vì hiển thị tất cả nhóm.

### R4. Chuẩn hóa Hiển thị Thẻ Món trong Giỏ Hàng (Order Cart)
- Tại thẻ món trong giỏ hàng:
  - Ở phần đầu của tên món: hiển thị rõ số lượng của món đó (ví dụ: 1x Pizza Margherita hoặc huy hiệu số lượng ở đầu dòng).
  - Ở góc trên bên phải của thẻ món: hiển thị đơn giá của một phần (giá 1 suất ăn đã kèm tùy chọn phụ thu, thay vì tổng tiền dòng hoặc nhân số lượng).

### R5. Đồng bộ Giao diện Chọn Món Combo theo Chuẩn POS (Bỏ ảnh)
- Cải tiến khu vực chọn món thành phần trong Combo (PosComboBuilder) đồng bộ theo định dạng thẻ món POS (MenuItemCard): loại bỏ hoàn toàn ảnh minh họa lớn, sử dụng thiết kế thẻ gọn gàng hiển thị tên món, giá phụ thu (hoặc miễn phí) và các nút điều khiển trực quan.

### R6. Cơ chế Tính Giá và Thuế VAT (Giá Đã Bao Gồm VAT)
- Tất cả giá món hiển thị và niêm yết trên hệ thống là giá ĐÃ BAO GỒM VAT.
- Tổng số tiền cần thu của đơn hàng bằng tổng giá trị các món ăn (đã có VAT) trừ đi giảm giá (nếu có).
- Thuế VAT được bóc tách từ tổng tiền theo tỷ lệ thuế suất (ví dụ: VAT 8% được tính theo công thức VAT = Subtotal - Subtotal / (1 + 0.08)), tuyệt đối không cộng thêm VAT lên trên giá món.

### R7. Bỏ Nút "Sơ Đồ Bàn" & Cải tiến Luồng Thoát/Hủy Đơn Rỗng
- Loại bỏ nút quay lại "<- Sơ đồ bàn" ở góc trên bên trái header của giao diện POS Order.
- Đổi tên nút "Xác Nhận & Gửi Bếp" trên thanh công cụ thành "Hoàn Thành".
- Xử lý tương tác khi bấm "Hoàn Thành":
  - Nếu đơn hàng KHÔNG có món nào (giỏ hàng rỗng): hiển thị popup modal hỏi xác nhận hủy lượt order này. Khi xác nhận, hệ thống thực hiện hủy lượt order, giải phóng trạng thái bàn về "Trống" (Available) và điều hướng về màn hình sơ đồ bàn (/pos/tables).
  - Nếu đơn hàng ĐÃ CÓ món: thực hiện lưu/gửi bếp bình thường và điều hướng về sơ đồ bàn.

## Acceptance Criteria

### Pizza & Modifier Options
- [ ] Mở tùy chọn bất kỳ loại pizza nào không còn xuất hiện nhóm "Chọn Sốt Nền Pizza" và nhóm "Mức Độ Cay".
- [ ] Tên món pizza và danh sách tùy chọn trên giỏ hàng không chứa tên sốt nền.
- [ ] Giao diện tùy chọn của mọi món đều có trường nhập ghi chú text tự do cho CSR, và nội dung nhập được lưu vào đơn hàng, hiển thị trên giỏ hàng và KDS.
- [ ] Thanh tab nhóm tùy chọn không còn nút "TẤT CẢ NHÓM"; nhóm đầu tiên được chọn mặc định khi mở màn hình.

### Giỏ Hàng (Cart) & Combo Interface
- [ ] Thẻ món trong giỏ hàng hiển thị số lượng ở đầu tên món và đơn giá của một phần ở góc trên bên phải.
- [ ] Giao diện chọn món Combo không còn thẻ hình ảnh lớn, chuyển sang dạng thẻ text gọn gàng tương thích với thẻ món POS.

### VAT Calculation & Order Exit Flow
- [ ] Tổng tiền cần thu bằng đúng tổng giá niêm yết các món (đã bao gồm VAT) trừ giảm giá; tiền thuế VAT hiển thị là phần bóc tách từ doanh thu, không làm tăng tổng tiền phải trả.
- [ ] Header không còn nút "<- Sơ đồ bàn"; nút gửi bếp được hiển thị với nhãn "Hoàn Thành".
- [ ] Khi giỏ hàng trống và bấm "Hoàn Thành", xuất hiện modal xác nhận hủy lượt order. Khi xác nhận, bàn được trả về trạng thái Available và chuyển về sơ đồ bàn.
- [ ] Toàn bộ mã nguồn biên dịch thành công (dotnet build src/Restaurant.sln) không phát sinh lỗi.


## 2026-10-14T17:16:42Z

Toàn diện audit workspace nhà hàng POS/KDS tại d:\My Project, tập trung đánh giá tính đúng đắn và mức độ hoàn thiện của hệ thống BOM (Recipe), Quản lý kho, Sổ cái biến động tồn kho (StockLedger), Tích hợp trừ kho tự động POS/KDS, Giá vốn và Báo cáo, nhằm phát hiện sai lệch, race conditions, rủi ro hao hụt thất thoát và các khoảng trống nghiệp vụ.

Working directory: d:\My Project
Integrity mode: development

Requested team structure:
- Lead Agent (Architect & Synthesizer): Đối chiếu kết quả, giải quyết các phát hiện trùng lặp và tổng hợp báo cáo.
- Agent 1: BOM, định mức và quy đổi đơn vị.
- Agent 2: Nhập kho, xuất kho, kiểm kê và StockLedger.
- Agent 3: Luồng POS/KDS, trừ kho, hủy món và gọi thêm.
- Agent 4: Giá vốn, báo cáo, transaction và test.

# MASTER AUDIT PROMPT — BOM, INVENTORY, STOCK MOVEMENT & RECIPE COSTING

## 1. VAI TRÒ VÀ MỤC TIÊU
Bạn là Senior Software Architect, Inventory Management Specialist và QA Engineer, có kinh nghiệm xây dựng hệ thống quản lý nhà hàng POS/KDS, BOM/Recipe, Inventory, Stock Ledger và Costing.

Hãy audit toàn bộ workspace hiện tại để đánh giá chính xác mức độ hoàn thiện và tính đúng đắn của các nghiệp vụ:
1. Quản lý nguyên liệu và hàng hóa.
2. Quản lý đơn vị tính và quy đổi đơn vị.
3. Quản lý BOM/Recipe — định mức nguyên liệu cho từng món ăn.
4. Nhập kho, xuất kho, điều chỉnh kho và kiểm kê.
5. Trừ kho tự động khi chế biến món ăn.
6. Xử lý món gọi thêm, hủy món, hủy đơn, hoàn tiền và hoàn kho.
7. Tính giá vốn món ăn và giá vốn bán hàng.
8. Theo dõi lịch sử biến động kho, đối soát tồn kho và cảnh báo tồn thấp.
9. Tích hợp giữa POS, KDS, đơn hàng, thanh toán và hệ thống kho.
10. Các chức năng quản trị, API, database, phân quyền và báo cáo liên quan.

Mục tiêu: xác định chức năng nào đã hoàn thiện thực sự, chức năng nào mới có giao diện, chức năng nào chưa có backend, chức năng nào hoạt động sai nghiệp vụ và chức năng nào chưa đủ bằng chứng để kết luận.
Không được chỉ kiểm tra tên file, giao diện hoặc sự tồn tại của API rồi kết luận chức năng đã hoàn thành. Phải lần theo luồng dữ liệu từ UI đến API, Application/Domain, database, transaction và các sự kiện realtime.

## 2. BỐI CẢNH KỸ THUẬT
- Workspace: d:\My Project (src/Restaurant.sln, ASP.NET Core 9, Blazor Server, EF Core 9, SQL Server, SignalR).
- Đọc cấu trúc solution, tài liệu kiến trúc, các báo cáo audit trước đây (docs/audit/FULL_WORKSPACE_AUDIT.md) và quy tắc nghiệp vụ.
- Tìm toàn bộ entity, DbContext, service, controller/API, Razor component, domain event, SignalR handler và test liên quan đến kho/BOM.
- TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.

## 3. PHẠM VI AUDIT CHI TIẾT
- Phân công Agent 1: BOM, định mức và quy đổi đơn vị (Đơn vị mua/kho/BOM, hệ số quy đổi thập phân, định mức món đơn, combo, topping/modifier, bán thành phẩm).
- Phân công Agent 2: Vòng đời phiếu nhập kho, xuất kho, kiểm kê, điều chỉnh, tính bất biến, chống cộng/trừ trùng kho (Idempotency), cấu trúc và độ tin cậy của StockLedger, chính sách tồn âm.
- Phân công Agent 3: Luồng POS/KDS, xác định chính xác thời điểm khấu trừ kho thực tế, rủi ro trừ trùng khi retry/reconnect, ma trận hủy món trước/trong/sau nấu (hoàn kho vs hao hụt waste) và gọi thêm món.
- Phân công Agent 4: Phương pháp tính giá vốn (FIFO, bình quân gia quyền), tính nhất quán doanh thu - giá vốn - tiêu hao, phân tích transaction boundaries, lock concurrency, và đánh giá độ bao phủ test suite hiện có.
- Lead Agent: Tổng hợp, đối chiếu chéo, khử trùng lặp và biên soạn 3 báo cáo bắt buộc.

## 4. QUY TẮC PHÂN LOẠI
Mỗi chức năng phải được phân loại theo:
DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE.
Mức độ ưu tiên: P0 - Critical, P1 - High, P2 - Medium, P3 - Low.
Mỗi phát hiện phải có đường dẫn file cụ thể, tên class/method/API và bằng chứng code.

## 5. ĐẦU RA BẮT BUỘC
Tạo đủ 3 tài liệu tại:
1. `docs/audit/INVENTORY_BOM_AUDIT.md` (Gồm 11 phần: Executive Summary, Feature Matrix, Architecture & Data Flow, BOM & Unit Conversion, Inventory Transactions & StockLedger, POS/KDS Integration & Hủy món, Costing & Reports, Bugs & Evidence thực chứng, Missing Tests).
2. `docs/audit/INVENTORY_BOM_BUSINESS_DECISIONS.md` (Các câu hỏi nghiệp vụ cần chủ nhà hàng/Product Owner ra quyết định).
3. `docs/audit/INVENTORY_BOM_REMEDIATION_PLAN.md` (Kế hoạch khắc phục theo lộ trình P0 -> P3 kèm rủi ro và tiêu chí nghiệm thu).
