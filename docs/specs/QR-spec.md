# Đặc tả Chi tiết Chức năng — Phân hệ QR Ordering (Khách hàng Tự phục vụ)

> **Module:** QR Ordering (Khách tự gọi món tại bàn)  
> **Actor chính:** Khách hàng (Customer / Guest), Thu ngân POS  
> **Công nghệ:** Mobile-First Web Responsive, SignalR Realtime Client, REST API

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| QR-001 | Quét Mã QR & Tự động Nhận diện Bàn / Session | Customer | Must Have |
| QR-002 | Xem Thực đơn Điện tử Tối ưu Di động | Customer | Must Have |
| QR-003 | Chọn Món, Tùy chọn Topping & Combo Gói | Customer | Must Have |
| QR-004 | Quản lý Giỏ hàng Tạm thời | Customer | Must Have |
| QR-005 | Nhập Thông tin Khách hàng & Gửi Đơn (Submit Order) | Customer | Must Have |
| QR-006 | Đồng bộ Phiên khi Chuyển / Gộp Bàn (Session Moved) | Customer, System | Must Have |
| QR-007 | Theo dõi Tiến độ Chế biến Realtime (Order Tracking) | Customer | Must Have |
| QR-008 | Gọi Thêm Món Vào Đơn Đang Phục vụ (Add More Items) | Customer | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### QR-001: Quét Mã QR & Tự động Nhận diện Bàn / Session
- **Actor:** Customer (Guest)
- **Priority:** Must Have
- **Mô tả:** Khách dùng camera điện thoại quét mã QR dán trên bàn (hoặc mã QR đặt tại quầy pickup). Trình duyệt mở đường dẫn chứa mã nhận diện bàn (`tableId`) và mã xác thực bảo mật (`token`).
- **Luồng chính:**
  1. Khách quét mã mở trang web: `https://[domain]/order?tableId=102&token=abc123xyz`.
  2. Hệ thống xác thực tính hợp lệ của `tableId` và `token`.
  3. Cấp session cho khách hàng, tự động tải dữ liệu thực đơn và khu vực bàn ăn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-001.1:** Given mã QR và token hợp lệ, When khách mở trang, Then hệ thống nhận diện đúng tên bàn ăn (ví dụ: "Bàn 102 - Tầng 1") và hiển thị thực đơn trong vòng dưới 1 giây.
- **Bảng liên quan:** `Tables`, `Areas`

---

### QR-002: Xem Thực đơn Điện tử Tối ưu Di động (Mobile Menu)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Giao diện xem món ăn thiết kế chuẩn Mobile-first, hiển thị danh mục theo thanh cuộn ngang, danh sách món kèm hình ảnh, mô tả, giá và nhãn trạng thái (Món mới, Bán chạy, Hết hàng).
- **Đồng bộ Món Hết hàng:** Nếu đầu bếp hoặc quản trị viên báo hết món, nút đặt món của sản phẩm đó lập tức chuyển sang màu xám và hiện nhãn "Hết hàng" (Sold Out) realtime qua SignalR.
- **Bảng liên quan:** `MenuItems`, `Categories`
- **Sự kiện SignalR:** Đăng ký nhận `ReceiveSoldOutNotice`.

---

### QR-003: Chọn Món, Tùy chọn Topping & Combo Gói
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Khi khách chạm vào một món ăn, ngăn kéo trượt từ dưới lên (Bottom Drawer `QrBottomDrawer.razor`) mở ra cho phép chọn:
  - **Món đơn có modifier:** Chọn Size, mức đường/đá, danh sách Topping thêm (kèm giá phụ thu).
  - **Gói Combo:** Bắt buộc khách chọn đủ số lượng món thành phần cho từng nhóm (ví dụ: Combo 2 người chọn 1 Pizza, 1 Món ăn kèm, 2 Lon nước). Nút "Thêm vào giỏ" bị khóa nếu chưa chọn đủ điều kiện bắt buộc.
- **Bảng liên quan:** `ModifierGroups`, `ModifierOptions`, `ComboGroups`, `ComboItems`

---

### QR-004: Quản lý Giỏ hàng Tạm thời (Cart Modal)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Modal xem giỏ hàng (`QrCartModal.razor`) hiển thị danh sách các món đã chọn, tùy chọn kèm theo, số lượng và tổng tiền tạm tính. Khách có thể tăng/giảm số lượng hoặc xóa món khỏi giỏ hàng.
- **Bảng liên quan:** Không (lưu trữ Client State).

---

### QR-005: Nhập Thông tin Khách hàng & Gửi Đơn (Submit Order)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Khách hàng bấm gửi đơn hàng. Hỗ trợ modal nhập thông tin khách hàng (`QrCustomerInfoModal.razor`): Họ tên, Số điện thoại (để tích điểm thành viên) và ghi chú đặc biệt cho nhà bếp.
- **Ràng buộc nghiệp vụ:**
  - **Chặn khi đang thanh toán:** Nếu bàn đang ở trạng thái `Billing` (chờ thanh toán), hệ thống chặn gửi đơn và thông báo: "Bàn đang trong quá trình thanh toán. Vui lòng liên hệ nhân viên phục vụ".
  - **Chống Spam (Rate Limit):** Giới hạn tối thiểu 30 giây giữa hai lần gửi đơn liên tiếp từ cùng một phiên khách.
- **Kết quả:** Đơn hàng tạo ở trạng thái `Draft`, trạng thái bàn chuyển sang `New Order` (Vàng), gửi sự kiện SignalR `ReceiveNewQrOrder` lên quầy POS thu ngân.
- **Bảng liên quan:** `Orders`, `OrderItems`, `OrderItemModifiers`, `Tables`, `Customers`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveNewQrOrder`, phát sự kiện `ReceiveTableStatusSync`.

---

### QR-006: Đồng bộ Phiên khi Chuyển / Gộp Bàn (Session Moved)
- **Actor:** Customer, System
- **Priority:** Must Have
- **Mô tả:** Trong lúc khách hàng đang mở trang QR của một bàn, nếu thu ngân tại POS thực hiện chuyển bàn hoặc gộp bàn:
  - Client khách nhận sự kiện SignalR `ReceiveTableSessionMoved`.
  - Tự động hiển thị thông báo: "Đơn hàng của bạn đã được chuyển sang Bàn [Tên Bàn Mới]".
  - Tự động cập nhật lại Session và địa chỉ URL của khách sang bàn mới mà không làm mất giỏ hàng hay ngắt kết nối.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-006.1:** Given khách đang mở bàn 105, When thu ngân chuyển sang bàn 201, Then màn hình điện thoại khách tự động cập nhật sang Bàn 201 trong vòng dưới 1 giây.
- **Bảng liên quan:** `Tables`, `Orders`
- **Sự kiện SignalR:** Đăng ký nhận `ReceiveTableSessionMoved`.

---

### QR-007: Theo dõi Tiến độ Chế biến Realtime (Order Tracking)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Màn hình trạng thái đơn hàng (`QrOrderStatus.razor`) hiển thị tiến độ thời gian thực của từng món ăn:
  - `Chờ duyệt` (Pending Approval): Đang chờ thu ngân xác nhận.
  - `Đang chế biến` (Cooking / Processing): Bếp đã nhận làm món.
  - `Đã hoàn thành` (Done / Ready): Món ăn đã làm xong, sẵn sàng được phục vụ lên bàn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-007.1:** When đầu bếp bấm hoàn thành món trên KDS, Then trạng thái món trên điện thoại khách lập tức cập nhật sang "Đã làm xong" qua sự kiện `ReceiveOrderItemStatusChanged`.
- **Bảng liên quan:** `Orders`, `OrderItems`
- **Sự kiện SignalR:** Đăng ký nhận `ReceiveOrderItemStatusChanged`.

---

### QR-008: Gọi Thêm Món Vào Đơn Đang Phục vụ (Add More Items)
- **Actor:** Customer (Dine-in only)
- **Priority:** Must Have
- **Mô tả:** Khách hàng đang ngồi tại bàn có thể quay lại trang Thực đơn để chọn thêm món mới. Khi gửi đơn, các món mới sẽ được tự động gộp vào đơn hàng đang chạy của bàn đó ở trạng thái chờ duyệt. Bàn ăn chuyển trạng thái `New Order` để thu ngân duyệt chuyển tiếp cho bếp.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveNewQrOrder`, phát sự kiện `ReceiveTableStatusSync`.
