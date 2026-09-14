# Kế hoạch Phân chia Công việc (Scrum Task Breakdown) — Lộ trình 3 tháng

> **Dự án:** Hệ thống Restaurant POS & KDS  
> **Lộ trình:** 2 tháng Code (8 Sprints × 1 tuần) + 1 tháng Test & Deploy (4 tuần)  
> **Phương pháp:** Scrum Model  

---

## GIAI ĐOẠN 1: PHÁT TRIỂN & LẬP TRÌNH (2 THÁNG - 8 SPRINTS)

---

### SPRINT 1 (Tuần 1): Thiết lập Foundation & Hệ thống Xác thực

#### Task 1.1: Khởi tạo Project & Cấu trúc Database
- **Mô tả:** Tạo project theo cấu trúc Clean Architecture và thiết lập Entity Framework Core 9.0 để kết nối SQL Server với 29 bảng.
- **Hướng dẫn thực hiện:**
  - *Domain:* Tạo Class Entities cho 29 bảng dữ liệu (User, Role, Table, Area, Order, Material...). Định nghĩa các Enums (`TableStatus`, `OrderStatus`, `KitchenStatus`).
  - *Infrastructure:* Tạo `ApplicationDbContext.cs`. Tách biệt file cấu hình Fluent API cho từng Entity (`IEntityTypeConfiguration<T>`) trong thư mục `Persistence/Configurations/`. Cấu hình Optimistic Concurrency Token (`.IsRowVersion()`) cho `Orders`, `Tables`, `Shifts`, `Materials`.
  - *Auditing & Soft Delete:* Viết `AuditableEntityInterceptor` kế thừa `SaveChangesInterceptor` để tự động điền `CreatedAt`/`UpdatedAt`. Cấu hình Global Query Filter cho `.IsDeleted` trong DbContext.
  - *Migrations:* Chạy lệnh `dotnet ef migrations add InitialCreate` và script data seed (tạo sẵn các Role cố định Admin, Manager, Cashier, Chef).
- **Điều kiện hoàn thành (DoD):**
  - DB được khởi tạo thành công trên SQL Server Local với đầy đủ 29 bảng, khóa ngoại và cột `RowVersion`.
  - Chạy `dotnet ef database update` không gặp lỗi.
  - Có Unit Test kiểm tra tính năng tự động điền `CreatedAt` khi thêm mới bản ghi.

#### Task 1.2: Hệ thống Xác thực JWT/Cookie & Phân quyền RBAC
- **Mô tả:** Xây dựng hệ thống đăng nhập/đăng xuất cho nhân viên và phân quyền vai trò.
- **Hướng dẫn thực hiện:**
  - *Infrastructure:* Triển khai `IdentityService` mã hóa mật khẩu bằng BCrypt. Cấu hình JWT Token Generator (Token, Expiry, SignKey) và Cookie Authentication.
  - *Application:* Định nghĩa `LoginRequest`, `LoginResponse`, `UserDto`. Viết `ICurrentUserService` để lấy UserId từ Context Token.
  - *WebAPI:* Tạo `AuthController.cs` cung cấp API `/api/v1/auth/login` và `/api/v1/auth/logout`. Cấu hình Policy-based Authorization cho các vai trò (Admin, Manager, Cashier, Chef).
- **Điều kiện hoàn thành (DoD):**
  - API trả về Token JWT hợp lệ khi nhập đúng tài khoản nhân viên.
  - Endpoint sử dụng `[Authorize(Roles = "Admin")]` chặn thành công các request từ tài khoản `Chef` (Chef nhận lỗi 403 Forbidden).

---

### SPRINT 2 (Tuần 2): Phân hệ BOH - Menu & Sơ đồ bàn

#### Task 2.1: CRUD Thực đơn, Danh mục & Modifiers
- **Mô tả:** Xây dựng các tính năng quản trị danh mục, món ăn (bao gồm món ăn thường và Combo) và các tùy chọn đi kèm (Toppings/Sizes).
- **Hướng dẫn thực hiện:**
  - *Application:* Tạo các Request DTOs, `CategoryService` và `MenuItemService` xử lý logic thêm/sửa/xoá (Soft Delete).
  - *BOH Client (Blazor):* Thiết kế giao diện CRUD Danh mục, Món ăn.
    - Đối với Món ăn: Thêm cờ Checkbox `IsCombo`. Nếu tích chọn, mở rộng tab liên kết các nhóm lựa chọn thành phần (`ModifierGroups`).
    - Thiết kế giao diện CRUD Modifiers.
- **Điều kiện hoàn thành (DoD):**
  - Tạo được món ăn thường và món combo trên giao diện BOH.
  - Khi bấm xóa món, bản ghi trong DB chuyển `IsDeleted = 1`, không hiển thị trên menu bán hàng.

#### Task 2.2: CRUD Sảnh, Bàn ăn & Sinh mã QR Token
- **Mô tả:** Quản lý khu vực sảnh, bàn ăn, tự động sinh mã QR Token bảo mật và hình ảnh QR Code.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết service sinh mã `QrToken` ngẫu nhiên 32 ký tự (sử dụng Cryptographic Random).
  - *WebAPI:* API tự động xuất ảnh QR Code từ link: `https://nhahang.com/order?tableId={Id}&token={QrToken}` sử dụng thư viện `QRCoder`.
  - *BOH Client:* Giao diện quản lý bàn ăn, nút "Tải mã QR" cho từng bàn và mã "QR Pickup" đặt tại quầy.
- **Điều kiện hoàn thành (DoD):**
  - Tạo bàn mới tự động sinh `QrToken` duy nhất.
  - Bấm nút "Tải mã QR" tải xuống đúng file ảnh QR dẫn đến link đặt món bảo mật của bàn đó.

---

### SPRINT 3 (Tuần 3): SignalR Hub & Phân hệ KDS (Bếp)

#### Task 3.1: Xây dựng SignalR Hub (`RestaurantHub`) & Strategy Reconnect/Resync
- **Mô tả:** Thiết lập hạ tầng truyền thông điệp thời gian thực (SignalR) giữa khách hàng (QR), thu ngân (POS) và bếp (KDS), bổ sung xử lý mất kết nối tự động lấy lại dữ liệu.
- **Hướng dẫn thực hiện:**
  - *WebAPI:* Tạo `RestaurantHub.cs`. Viết logic quản lý connection:
    - Khi client kết nối, đọc Token (JWT hoặc Guest Session Token) để xác định vai trò.
    - Tự động add client vào Group tương ứng: `CashiersGroup`, `ChefsGroup` hoặc `TableGroup_{TableId}`.
  - *Client Side Strategy:* Lập trình sự kiện `onreconnected` trên client Blazor/QR: Tự động gọi API sync delta state (`GET /api/kds/tickets/active`, `GET /api/pos/tables/status`) để đồng bộ lại dữ liệu khi khôi phục mạng.
- **Điều kiện hoàn thành (DoD):**
  - Client Blazor và Client Mobile Web kết nối thành công tới Hub.
  - Thử ngắt mạng 5s và bật lại, client tự động nối lại SignalR và tải lại dữ liệu mới nhất thành công.

#### Task 3.2: Nghiệp vụ Ticket bếp, Cảnh báo SLA trễ món (KDS-006) & Giao diện KDS
- **Mô tả:** Xây dựng màn hình hiển thị danh sách đơn chế biến realtime tại bếp, các nút tương tác chuyển trạng thái món và hệ thống cảnh báo trễ món KDS SLA (>15 phút).
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `KitchenService` xử lý:
    - Nhận món: Đổi `KitchenStatus = Processing`, cập nhật `SentToKitchenAt`.
    - Xong món: Đổi `KitchenStatus = Done`, cập nhật `CompletedAt`. Nếu tất cả món xong -> Đổi đơn hàng thành `Completed`.
  - *KDS Client (Blazor):* Thiết kế trang KDS tối giản (Dark Mode).
    - Hiển thị danh sách thẻ đơn (Ticket) cuộn ngang.
    - Cảnh báo trễ món SLA (KDS-006): Màu sắc thẻ đổi theo thời gian chờ (>10p màu vàng, >15p đỏ nhấp nháy). Khi vượt 15 phút, phát âm thanh cảnh báo và bắn SignalR `KitchenSlaWarning` lên POS thu ngân.
    - Màn hình gom món tổng hợp (Aggregate View) bằng cách gom nhóm `OrderItem` theo `MenuItemId`.
    - Nút "Báo hết món" (Sold Out) cập nhật `IsSoldOut = true` của món ăn và broadcast SignalR.
- **Điều kiện hoàn thành (DoD):**
  - Click vào món trên KDS đổi trạng thái lập tức đồng bộ lên POS.
  - Món chờ quá 15 phút đổi sang viền đỏ nhấp nháy và phát cảnh báo SignalR `KitchenSlaWarning` lên POS.
  - Nút "Báo hết món" làm xám món ăn trên menu khách ngay lập tức.

---

### SPRINT 4 (Tuần 4): QR Ordering - Menu & Chọn món di động

#### Task 4.1: Mobile Web UI Menu & Giỏ hàng LocalStorage
- **Mô tả:** Xây dựng trang thực đơn di động dành cho khách hàng tự gọi món tại bàn, hỗ trợ xem danh mục và giỏ hàng tạm thời.
- **Hướng dẫn thực hiện:**
  - *QR Client (Blazor Mobile Web):*
    - Thiết kế giao diện Mobile-First, menu cuộn dọc mượt mà.
    - Trượt danh mục ngang phía trên để cuộn nhanh đến nhóm đồ ăn tương ứng.
    - Giỏ hàng ghim cố định ở cuối trang (Sticky Bottom Bar). Lưu thông tin giỏ hàng (món, options, ghi chú) vào `LocalStorage` để tránh mất dữ liệu khi reload trình duyệt.
- **Điều kiện hoàn thành (DoD):**
  - Giao diện responsive chạy mượt mà trên trình duyệt Safari (iOS) và Chrome (Android).
  - Reload trình duyệt giỏ hàng vẫn được giữ nguyên.

#### Task 4.2: Drawer chọn Modifier & Chọn Combo
- **Actor:** Client (Khách hàng)
- **Mô tả:** Xây dựng giao diện trượt từ dưới lên (Bottom Drawer) để chọn size, topping hoặc chọn các món bắt buộc/tùy chọn trong Combo.
- **Hướng dẫn thực hiện:**
  - *QR Client:*
    - Khi click vào món ăn thường: Mở Bottom Drawer hiển thị nhóm Topping, Size.
    - Khi click vào món Combo: Hiển thị danh sách các món thành phần được chia theo nhóm (Món chính, Đồ ăn kèm, Nước uống).
    - Ép buộc khách phải chọn đủ số lượng tối thiểu (`MinSelections`) của từng nhóm combo mới cho phép bấm nút "Thêm vào giỏ".
- **Điều kiện hoàn thành (DoD):**
  - Drawer hiển thị đúng giá cộng thêm của từng Topping.
  - Combo không cho phép bấm "Thêm vào giỏ" nếu chưa chọn đủ món thành phần bắt buộc.

---

### SPRINT 5 (Tuần 5): QR Ordering - Gửi đơn & Theo dõi tiến độ

#### Task 5.1: Gửi đơn Draft (Rate Limiting 30s & Chặn Billing)
- **Mô tả:** Khách hàng bấm gửi đơn hàng lên POS thu ngân, áp dụng cơ chế chặn nếu bàn đang tính tiền hoặc spam gửi đơn liên tục.
- **Hướng dẫn thực hiện:**
  - *WebAPI:* Tạo API `/api/v1/qr-orders/submit`. Viết Middleware/ActionFilter kiểm tra Rate Limiting:
    - Lưu thời gian gửi đơn gần nhất của session khách. Nếu < 30 giây -> Trả về lỗi 429 Too Many Requests.
    - Kiểm tra trạng thái bàn trên DB: Nếu `Status = Billing` -> Chặn gửi đơn, trả về lỗi 400 Bad Request kèm thông báo "Bàn đang trong quá trình thanh toán".
  - *Application:* Tạo đơn hàng mới `OrderStatus = Draft` (Nháp) trên database. Bắn SignalR sự kiện `NewQrOrderReceived` lên quầy POS.
- **Điều kiện hoàn thành (DoD):**
  - Khách bấm gửi đơn, POS báo chuông và hiển thị thông báo duyệt.
  - Bấm gửi đơn 2 lần liên tục trong 10 giây báo lỗi Rate Limiting.
  - Bàn đang `Billing` (Chờ thanh toán) quét QR gọi thêm món báo lỗi chặn thanh toán.

#### Task 5.2: Theo dõi tiến độ chế biến realtime & Gọi thêm món
- **Mô tả:** Màn hình hiển thị tiến trình nấu nướng của bếp cho khách theo dõi thời gian thực. Hỗ trợ gọi thêm món gộp đơn.
- **Hướng dẫn thực hiện:**
  - *QR Client:*
    - Giao diện "Trạng thái đơn hàng" hiển thị timeline: Chờ duyệt -> Đang nấu -> Bê món.
    - Nhận sự kiện SignalR `OrderItemStatusChanged` từ bếp để cập nhật màu sắc/tiến trình của từng món realtime.
    - Cho phép khách bấm quay lại Menu để chọn thêm món và bấm gửi đơn lần 2 (tự động gắn vào đơn hàng cũ của bàn).
- **Điều kiện hoàn thành (DoD):**
  - Khách gọi thêm món lần 2, đơn hàng trên POS tự động cộng dồn món mới và báo màu vàng (New Order) chờ duyệt.

---

### SPRINT 6 (Tuần 6): Phân hệ POS - Vận hành Quầy thu ngân

#### Task 6.1: Giao diện Sơ đồ bàn realtime & Duyệt đơn QR
- **Mô tả:** Thiết kế màn hình sơ đồ bàn trực quan cho thu ngân cập nhật realtime và thực hiện duyệt đơn của khách.
- **Hướng dẫn thực hiện:**
  - *POS Client:*
    - Vẽ sơ đồ các bàn phân chia theo tabs khu vực. Trạng thái bàn (Available, Occupied, New Order, Billing) thay đổi realtime qua SignalR.
    - Click vào bàn màu Vàng (New Order) mở popup duyệt món. Thu ngân kiểm tra và bấm "Duyệt".
    - Hệ thống: Chuyển `OrderStatus = Confirmed`, chuyển bàn sang `Occupied`, bắn SignalR đẩy ticket xuống bếp KDS.
- **Điều kiện hoàn thành (DoD):**
  - Thu ngân bấm duyệt đơn, bàn đổi màu sang Đỏ (Occupied) trên tất cả các máy POS khác ngay lập tức.
  - Ticket của bàn lập tức hiển thị trên màn hình bếp KDS.

#### Task 6.2: Chuyển bàn, Gộp bàn & Tách hóa đơn
- **Mô tả:** Thực hiện các hành động chuyển bàn, hợp nhất bàn hoặc tách món thanh toán lẻ.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `OrderService`:
    - Chuyển bàn: Đổi `TableId` đơn hàng sang bàn đích.
    - Gộp bàn: Chuyển các `OrderItems` đơn nguồn sang đơn đích, hủy đơn nguồn.
  - *SignalR:* Phát sự kiện `TableSessionMoved` chứa Token phiên cũ và `TableId` bàn mới.
  - *QR Client:* Lắng nghe sự kiện `TableSessionMoved`, tự động đổi `tableId` trong cookie/URL để khách không bị mất kết nối hay mất giỏ hàng khi bị chuyển bàn.
- **Điều kiện hoàn thành (DoD):**
  - Thu ngân chuyển bàn A sang bàn B, điện thoại của khách ngồi bàn A lập tức tải lại và đổi URL hiển thị bàn B thành công.

#### Task 6.3: Quản lý Ca làm việc & Kết ca (Z-Report)
- **Mô tả:** Thu ngân mở ca đầu ngày, kiểm soát dòng tiền két và kết ca đối soát cuối ngày.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `ShiftService` xử lý mở ca (ghi nhận `OpeningBalance`) và kết ca (ghi nhận `ActualCashCounted`, tính `Difference`).
  - *POS Client:*
    - Giao diện mở ca yêu cầu nhập tiền két đầu ca.
    - Giao diện kết ca cho phép nhập tiền mặt thực tế, tự tính chênh lệch lệch két. Xuất báo cáo Z-Report tóm tắt (doanh thu cash/transfer, tổng đơn).
- **Điều kiện hoàn thành (DoD):**
  - Ca làm việc chuyển `Status = Closed` sau khi kết ca.
  - Thu ngân đã kết ca không thể tạo đơn hay thanh toán cho đến khi mở ca mới.

---

### SPRINT 7 (Tuần 7): Định lượng BOM & Job tự động trừ kho chạy ngầm

#### Task 7.1: Quản lý Kho, Nhập kho & Kiểm kê kho
- **Mô tả:** Quản lý nguyên vật liệu thô, lập phiếu nhập kho từ NCC và phiếu kiểm kê định kỳ.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `InventoryService` xử lý nhập kho (cộng `CurrentStock`, tính giá vốn trung bình `CostPerUnit`) và kiểm kê kho (điều chỉnh tồn kho thực tế, tạo chênh lệch giá trị hao hụt).
  - *BOH Client:* Giao diện phiếu nhập kho và phiếu kiểm kê kho.
- **Điều kiện hoàn thành (DoD):**
  - Phiếu nhập kho lưu thành công, số lượng tồn kho nguyên liệu trong database tăng tương ứng.
  - Phiếu kiểm kê hoàn tất tự tạo bản ghi chênh lệch trong `InventoryTransactions`.

#### Task 7.2: Thiết lập Công thức BOM & Xử lý Trừ kho Tự động qua DB Transaction
- **Mô tả:** Thiết lập công thức định lượng (BOM) món ăn/topping và thực hiện trừ kho tự động trong cùng Transaction DB khi đơn hàng hoàn tất.
- **Hướng dẫn thực hiện:**
  - *BOH Client:* Giao diện cấu hình định lượng nguyên liệu cho `Recipes` (1:1 MenuItem) và `ModifierRecipes` (1:1 ModifierOption).
  - *Application (MediatR Handler):* 
    - Đơn hàng chuyển `Done` phát Domain Event `OrderItemKitchenDoneDomainEvent(OrderItemId, OrderId)`.
    - Lắng nghe và gọi `InventoryService.DeductInventoryForOrderItemAsync`.
    - Thực hiện khấu trừ `Materials.CurrentStock` trực tiếp trong DB Transaction thanh toán, tạo `InventoryTransactions` (`Type = SaleDeduction`).
    - Kiểm tra nếu `CurrentStock < MinStockLevel`, phát SignalR cảnh báo tồn kho tối thiểu lên Dashboard. Cho phép tồn kho âm nhưng ghi log cảnh báo lệch kho.
- **Điều kiện hoàn thành (DoD):**
  - Thanh toán hóa đơn thành công, tồn kho nguyên vật liệu trong DB bị trừ đi chính xác theo công thức BOM lập tức trong 1 Transaction.
  - Có cảnh báo dashboard nếu nguyên liệu hết/xuống dưới mức tối thiểu.

---

### SPRINT 8 (Tuần 8): VietQR, SePay Webhook & Loyalty System

#### Task 8.1: Hệ thống tích/tiêu điểm Loyalty
- **Mô tả:** Đăng ký thành viên, tự động tích điểm theo hóa đơn và quy đổi điểm giảm tiền khi thanh toán.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `LoyaltyService`:
    - Đăng ký thành viên yêu cầu duy nhất `PhoneNumber`.
    - Tích điểm: `Points = TotalAmount / 10,000`. Cập nhật `TotalPoints` và `AvailablePoints` của khách hàng. Quét điều kiện tự nâng hạng thành viên (`TierId`).
    - Tiêu điểm: Quy đổi 1 điểm = 50đ. Áp dụng giảm trừ hóa đơn.
- **Điều kiện hoàn thành (DoD):**
  - Thanh toán đơn 200.000đ, khách hàng được cộng thêm 20 điểm vào tài khoản.
  - Điểm khả dụng bị trừ đi chính xác khi bấm tiêu điểm. Hạng thành viên tự thăng cấp khi điểm tích lũy đạt ngưỡng.

#### Task 8.2: Thanh toán VietQR & Webhook SePay
- **Mô tả:** Sinh mã VietQR động chứa nội dung đối soát unique và tích hợp webhook SePay để tự động hoàn thành thanh toán.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `PaymentService` sinh nội dung đối soát `SepayContent` và chuỗi ảnh VietQR.
  - *WebAPI:* API Endpoint `/api/v1/payments/sepay-webhook`.
    - Đọc payload thô, kiểm tra chữ ký SHA256 ở header `X-SePay-Signature` bằng API Key.
    - Kiểm tra Idempotency: Đọc mã `referenceCode` từ SePay Payload, ngắt trùng lặp nếu đã xử lý (`SepayWebhookLogs.IsProcessed = true`).
    - Đối soát: Tìm Payment trùng nội dung và số tiền.
    - **Xử lý xung đột:** Nếu đơn hàng đã được trả bằng Tiền mặt trước đó, ghi nhận lỗi vào `SepayWebhookLogs.ErrorLog` và bắn SignalR alert `PaymentAlert` lên POS.
    - Nếu chưa trả: Cập nhật Payment thành `Completed`, đơn hàng thành `Paid`, trừ kho BOM, tích điểm Loyalty, phát SignalR `PaymentCompleted` và `TableStatusChanged` để đóng modal thanh toán và đưa bàn về `Trống`.
- **Điều kiện hoàn thành (DoD):**
  - Sử dụng Postman giả lập gửi request webhook kèm signature hợp lệ, hóa đơn trên POS tự động đóng và hoàn tất thành công trong vòng dưới 1 giây.
  - Chuyển khoản sai nội dung hoặc trùng lặp, POS hiển thị danh sách đối soát thủ công cho thu ngân chọn khớp bằng tay.

#### Task 8.3: Xử lý Hủy đơn & Phục hồi Tồn kho / Rollback Điểm Loyalty (ACC-007)
- **Mô tả:** Xử lý hủy đơn hàng đã thanh toán hoặc đơn chưa chế biến, tự động hoàn trả tồn kho và rollback điểm tích lũy.
- **Hướng dẫn thực hiện:**
  - *Application:* Viết `CancelOrderCommandHandler` trong 1 DB Transaction:
    - Nếu đơn đã trừ kho: Hoàn trả lại tồn kho `Materials.CurrentStock` (nếu món chưa chế biến/hủy trước nấu).
    - Rollback Loyalty: Trả lại điểm khả dụng `AvailablePoints` nếu đơn có sử dụng tiêu điểm, đồng thời khấu trừ lại số điểm vừa được tích từ đơn bị hủy. Ghi log `LoyaltyTransactions` với `Type = Reversal`.
- **Điều kiện hoàn thành (DoD):**
  - Hủy đơn thành công, số điểm khả dụng và tồn kho nguyên liệu được phục hồi chính xác trong 1 Transaction DB.

---

## GIAI ĐOẠN 2: KIỂM THỬ HỆ THỐNG & DEPLOY THỬ NGHIỆM (1 THÁNG)

---

### TUẦN 9: Integration & Security Testing (Kiểm thử Liên thông & Bảo mật)
- **Mô tả:** Đánh giá tính toàn vẹn của dữ liệu truyền tải giữa các phân hệ và kiểm tra an ninh hệ thống.
- **Hướng dẫn thực hiện:**
  - Thiết lập môi trường Test Database độc lập.
  - Thực hiện kiểm thử tích hợp (Integration Tests) cho luồng: Đặt đơn QR (Draft) -> Duyệt POS (Confirmed) -> KDS làm xong (Done) -> Thanh toán (Paid) -> Trừ kho BOM.
  - Kiểm thử bảo mật (Security Tests): Giả mạo chữ ký webhook SePay (gửi chữ ký sai/thiếu), kiểm tra API có chặn và trả về 401 Unauthorized hay không. Kiểm thử phân quyền JWT (truy cập API Admin bằng token Staff).
- **Điều kiện hoàn thành (DoD):**
  - 100% các kịch bản kiểm thử liên thông (chuỗi nghiệp vụ) chạy thành công mà không gây lỗi khóa dữ liệu (deadlock).
  - Không có lỗ hổng bảo mật cho phép bypass xác thực API.

---

### TUẦN 10: Performance & Load Testing (Kiểm thử Hiệu năng & Tải)
- **Mô tả:** Đánh giá khả năng chịu tải của SignalR Hub khi có nhiều bàn gọi món cùng lúc và tối ưu hóa câu lệnh truy vấn database.
- **Hướng dẫn thực hiện:**
  - Sử dụng công cụ **k6** hoặc **JMeter** giả lập 100 client kết nối SignalR đồng thời, gửi liên tục yêu cầu gọi món.
  - Phân tích hiệu năng SQL Server: Kiểm tra các chỉ số (Indexes) trên các cột thường xuyên tìm kiếm hoặc làm khóa ngoại (`Orders.OrderCode`, `Customers.PhoneNumber`, `Payments.SepayContent`).
  - Viết lại các truy vấn EF Core chưa tối ưu (tránh lỗi N+1 query, sử dụng `.AsNoTracking()` cho các truy vấn chỉ đọc báo cáo).
- **Điều kiện hoàn thành (DoD):**
  - Hệ thống chịu tải thành công 100 kết nối SignalR đồng thời với thời gian phản hồi tin nhắn dưới 200ms.
  - Tất cả các API chính (lấy sơ đồ bàn, menu, lịch sử đơn) có thời gian phản hồi dưới 300ms.

---

### TUẦN 11: UAT & Bug Fixing (Kiểm thử Nghiệm thu Người dùng & Sửa lỗi)
- **Mô tả:** Giả lập vận hành một nhà hàng thực tế với các vai trò thu ngân, bếp, khách hàng để phát hiện các lỗi trải nghiệm (UX) và sửa lỗi.
- **Hướng dẫn thực hiện:**
  - Thiết lập mạng LAN nội bộ, cài đặt ứng dụng POS trên PC thu ngân, KDS trên máy tính bảng của bếp, và khách dùng điện thoại cá nhân quét QR.
  - Thực hiện chạy thử nghiệm quy trình từ lúc mở ca, đón khách, khách quét QR tự gọi món, gọi thêm, bếp làm món, gom món, thu ngân in hóa đơn tạm tính, khách chuyển khoản VietQR, SePay webhook tự động báo xong, thu ngân kết ca.
  - Thu thập nhật ký lỗi (logs), phân loại mức độ ưu tiên và sửa chữa toàn bộ lỗi phát sinh.
- **Điều kiện hoàn thành (DoD):**
  - Hệ thống vận hành trơn tru trong 3 ca làm việc giả lập liên tục mà không gặp lỗi treo giao diện hay sai lệch tồn kho.
  - Sửa sạch toàn bộ các lỗi nghiêm trọng (Critical/High) và trung bình (Medium).

---

### TUẦN 12: Production Deployment (Deploy thực tế & Báo cáo)
- **Mô tả:** Triển khai hệ thống lên môi trường production chạy thực tế và đóng gói đồ án.
- **Hướng dẫn thực hiện:**
  - *Backend:* Đóng gói ứng dụng backend thành Docker Container hoặc deploy trực tiếp lên dịch vụ Cloud (Azure/AWS) hoặc IIS Server chạy Windows Server.
  - *Database:* Deploy SQL Server lên Cloud (Azure SQL hoặc VPS SQL Server độc lập). Chạy script migration để tạo cấu trúc và seed data chính thức.
  - *Payment:* Cấu hình thông số tài khoản ngân hàng và API Key SePay thật trên môi trường Production để kết nối webhook ngân hàng thật.
  - *Frontend:* Publish các app Blazor (POS, KDS, BOH) và QR Ordering Web lên web server (Nginx/IIS).
- **Điều kiện hoàn thành (DoD):**
  - Đường dẫn web hoạt động chính thức trên HTTPS (ví dụ: `https://restaurant-pos.com`).
  - Khách hàng thực hiện quét QR và thanh toán chuyển khoản bằng tiền thật qua app ngân hàng thành công, hệ thống tự động hoàn tất đơn hàng và trừ kho trên server thật.
