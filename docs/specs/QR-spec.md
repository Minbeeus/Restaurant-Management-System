# Đặc tả Chi tiết Chức năng — Phân hệ QR Ordering (Khách hàng Tự phục vụ)

> **Module:** QR Ordering  
> **Actor chính:** Khách hàng (Customer / Guest)  
> **Công nghệ:** Mobile Web Responsive, SignalR Realtime Client

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| QR-001 | Quét Mã QR & Nhận diện Bàn/Pickup | Customer | Must Have |
| QR-002 | Xem Thực đơn | Customer | Must Have |
| QR-003 | Chọn Món & Combo | Customer | Must Have |
| QR-004 | Quản lý Giỏ hàng | Customer | Must Have |
| QR-005 | Gửi Đơn hàng | Customer | Must Have |
| QR-006 | Theo dõi Trạng thái Chế biến | Customer | Must Have |
| QR-007 | Gọi Thêm Món | Customer | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### QR-001: Quét Mã QR & Nhận diện Bàn/Pickup (Scan QR & Identify Table)
- **Actor:** Customer (Guest)
- **Priority:** Must Have
- **Mô tả:** Khách quét mã QR tại bàn (hoặc mã QR Pickup tại quầy) bằng camera điện thoại, hệ thống tự nhận diện thông tin bàn ăn và cấp Session ID để khách đặt món. Hỗ trợ cập nhật session tự động khi bị thu ngân chuyển/gộp bàn.
- **Luồng chính:**
  1. Khách hàng quét mã QR trên bàn. Trình duyệt di động mở đường dẫn có tham số: `https://nhahang.com/order?tableId=105&token=abc123xyz`.
  2. Backend xác thực `tableId` và `token`.
  3. Xác thực thành công:
     - Hệ thống cấp Session Token lưu trữ tại LocalStorage/Cookie.
     - Đồng bộ SignalR: Gửi sự kiện báo bàn 105 đang có khách bắt đầu thao tác.
     - Giao diện chuyển sang trang Thực đơn của bàn 105.
- **Cơ chế chuyển bàn tự động:**
  - Trong lúc khách đang mở trang, nếu thu ngân thực hiện chuyển bàn từ bàn 105 sang bàn 110, client nhận sự kiện SignalR `TableSessionMoved`:
    - Hệ thống tự động cập nhật lại `TableId = 110` trong cookie session của trình duyệt khách hàng.
    - Hiển thị thông báo trên màn hình khách: "Đơn hàng của bạn đã được chuyển sang Bàn 110. Hệ thống sẽ tự động cập nhật".
    - Trình duyệt tự động chuyển hướng và giữ nguyên giỏ hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-001.1:** Given khách hàng đang mở trang gọi món của Bàn 105, When thu ngân tại POS thực hiện chuyển đơn hàng sang Bàn 110, Then trình duyệt khách hàng phải nhận sự kiện `TableSessionMoved`, tự động đổi địa chỉ URL sang tham số `tableId=110` trong vòng dưới 1 giây và hiển thị thông báo chuyển bàn cho khách.
- **Bảng liên quan:** `Tables`, `Areas`
- **Sự kiện SignalR:** Đăng ký nhận sự kiện `TableSessionMoved`, phát sự kiện `TableStatusChanged`.

---

### QR-002: Xem Thực đơn (Browse Menu)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Giao diện xem thực đơn tối ưu hóa cho thiết bị di động (Mobile-First UI), hiển thị món theo từng danh mục. Món hết hàng hiển thị tag "Hết hàng" (Sold Out) realtime qua SignalR.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-002.1:** Given khách hàng đang xem thực đơn trên điện thoại, When đầu bếp bấm "Báo hết món" món "Sinh tố bơ" trên KDS, Then nút "Thêm vào giỏ" của món "Sinh tố bơ" trên điện thoại khách hàng phải lập tức bị vô hiệu hóa (Disabled) và đổi chữ thành "Hết hàng" trong vòng dưới 1 giây.
- **Bảng liên quan:** `MenuItems`, `Categories`
- **Sự kiện SignalR:** Đăng ký nhận sự kiện `MenuItemSoldOut`.

---

### QR-003: Chọn Món & Combo (Select Item with Modifiers / Combo options)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Khách chọn món ăn, lựa chọn các tùy chọn đi kèm (Size, Toppings...) hoặc chọn các món thành phần trong Combo thông qua giao diện drawer trượt.
- **Luồng chính:**
  1. Khách click chọn một món ăn hoặc combo.
  2. Một Drawer trượt từ dưới lên (Bottom Drawer) hiển thị:
     - **Nếu là Combo:** Hiển thị các nhóm lựa chọn bắt buộc (ví dụ: nhóm "Món chính" chọn 1, nhóm "Đồ uống" chọn 1). Khách hàng bắt buộc phải tích chọn đủ số lượng món thành phần ở mỗi nhóm.
     - **Nếu là món thường có modifier:** Hiển thị danh sách Size, Topping.
  3. Bấm "Thêm vào giỏ hàng".
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-003.1:** Given khách chọn Combo có nhóm món bắt buộc, When khách chưa tích đủ lựa chọn bắt buộc, Then nút "Thêm vào giỏ hàng" ở cuối drawer phải ở trạng thái vô hiệu hóa (Màu xám) và hiển thị thông báo nhắc chọn.
- **Bảng liên quan:** `ModifierGroups`, `ModifierOptions`, `MenuItems`

---

### QR-004: Quản lý Giỏ hàng (Manage Cart)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Khách hàng kiểm tra, tăng giảm số lượng hoặc xoá các món ăn trong giỏ hàng. Giỏ hàng được lưu trữ tạm thời trong browser session.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-004.1:** Given khách hàng đã thêm món vào giỏ hàng, When khách bấm nút giảm số lượng về `0` hoặc vuốt ngang dòng món ăn đó, Then món ăn phải được xóa khỏi giỏ hàng lập tức và tổng tiền giỏ hàng phải giảm đi chính xác.

---

### QR-005: Gửi Đơn hàng (Submit Order)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Bấm gửi đơn hàng đã chọn lên quầy thu ngân để duyệt. Áp dụng cơ chế giới hạn tần suất gửi đơn (Rate Limiting) và chặn đặt món nếu bàn đang chuẩn bị thanh toán.
- **Luồng chính:**
  1. Khách hàng bấm nút "Gửi đơn hàng" trong giỏ hàng.
  2. **Kiểm tra trạng thái Bàn:** Hệ thống kiểm tra trạng thái hiện tại của bàn ăn:
     - Nếu bàn đang ở trạng thái `Billing` (Chờ thanh toán): Hệ thống chặn gửi đơn, hiển thị thông báo "Bàn đang trong quá trình thanh toán. Vui lòng liên hệ nhân viên nếu muốn gọi thêm món".
  3. **Cơ chế chống Spam:** Hệ thống kiểm tra thời gian gửi đơn gần nhất của session này:
     - Nếu khoảng cách nhỏ hơn 30 giây: Hiển thị cảnh báo "Hệ thống đang xử lý đơn hàng của bạn. Vui lòng đợi 30 giây để gửi yêu cầu tiếp theo" và chặn gửi đơn.
     - Nếu hợp lệ: Cập nhật thời điểm gửi đơn gần nhất vào session, tiến hành gửi đơn.
  4. Tạo đơn hàng trạng thái `Draft` (Nháp) trong Database.
  5. Gửi thông báo SignalR `NewQrOrderReceived` lên quầy POS.
  6. Trạng thái bàn tự động chuyển sang `New Order` (Đơn mới) để thu ngân biết cần duyệt.
  7. Màn hình khách chuyển sang giao diện chờ duyệt.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-005.1:** Given khách hàng vừa gửi đơn hàng lúc 15:00:00, When khách bấm nút gửi thêm đơn mới lúc 15:00:15, Then hệ thống phải chặn lại và hiển thị thông báo yêu cầu chờ thêm 15 giây.
  - **AC-QR-005.2:** Given bàn 105 đang ở trạng thái `Billing` (Chờ thanh toán), When khách bấm "Gửi đơn hàng", Then hệ thống phải chặn lại và hiển thị thông báo "Bàn đang trong quá trình thanh toán. Vui lòng liên hệ nhân viên nếu muốn gọi thêm món".
  - **AC-QR-005.3:** When khách gửi đơn thành công, Then hệ thống phải tạo một đơn hàng có trạng thái `Draft` (giá trị `0`) trong database, gán trạng thái bàn thành `New Order` (giá trị `2`) và gửi thông báo SignalR đến quầy thu ngân.
- **Bảng liên quan:** `Orders`, `OrderItems`, `OrderItemModifiers`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `NewQrOrderReceived`, phát sự kiện `TableStatusChanged`.

---

### QR-006: Theo dõi Trạng thái Chế biến (Track Order Status)
- **Actor:** Customer
- **Priority:** Must Have
- **Mô tả:** Khách theo dõi tiến độ chế biến từng món ăn trong bếp thông qua giao diện cập nhật thời gian thực.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-006.1:** Given khách đang mở màn hình theo dõi đơn hàng, When đầu bếp bấm hoàn thành món "Bò kho" trên KDS, Then trạng thái món "Bò kho" trên điện thoại khách hàng phải lập tức đổi thành "Đã làm xong" (Done) màu xanh dương trong vòng dưới 1 giây.
- **Bảng liên quan:** `Orders`, `OrderItems`
- **Sự kiện SignalR:** Đăng ký nhận sự kiện `OrderItemStatusChanged`.

---

### QR-007: Gọi Thêm Món (Add More Items)
- **Actor:** Customer (Dine-in only)
- **Priority:** Must Have
- **Mô tả:** Cho phép khách hàng đang ngồi tại bàn quét mã gọi thêm món ăn mới, hệ thống tự động gộp vào đơn hàng đang chạy của bàn đó dưới dạng chờ duyệt. Áp dụng quy tắc chặn nếu bàn đang thanh toán.
- **Luồng chính:**
  1. Khách hàng chọn thêm các món ăn mới vào giỏ hàng và bấm "Gửi đơn".
  2. **Kiểm tra trạng thái Bàn:** Nếu bàn đang ở trạng thái `Billing` (Chờ thanh toán): Hệ thống chặn gửi đơn, hiển thị thông báo "Bàn đang trong quá trình thanh toán. Vui lòng liên hệ nhân viên nếu muốn gọi thêm món".
  3. Hệ thống tạo các dòng `OrderItems` mới gắn vào `OrderId` của đơn hiện tại với trạng thái bếp `Pending` và lưu snapshot giá.
  4. Trạng thái bàn tự động chuyển từ `Occupied` sang `New Order` (Đơn mới) để thu ngân duyệt.
  5. Phát SignalR thông báo thu ngân POS có món gọi thêm cần duyệt.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-QR-007.1:** Given bàn 105 đang có một đơn hàng trạng thái `Confirmed` (bàn đang màu đỏ Occupied), When khách gửi đơn gọi thêm 1 lon Coca, Then lon Coca phải được thêm vào đơn hàng hiện tại ở trạng thái chờ duyệt, trạng thái bàn 105 phải chuyển sang màu vàng `New Order` trên sơ đồ POS trong vòng dưới 1 giây.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `NewQrOrderReceived`, phát sự kiện `TableStatusChanged`.
