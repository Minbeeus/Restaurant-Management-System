# Đặc tả Chi tiết Chức năng — Phân hệ KDS (Kitchen Display System)

> **Module:** KDS (Kitchen Display System)  
> **Actor chính:** Nhân viên Bếp (Chef)  
> **Công nghệ:** Blazor Server/WebAssembly (Realtime client), SignalR Hub Client

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| KDS-001 | Xem Danh sách Thẻ Đơn Chế biến | Chef | Must Have |
| KDS-002 | Nhận Làm Món | Chef | Must Have |
| KDS-003 | Hoàn Thành Món | Chef | Must Have |
| KDS-004 | Xem Tổng hợp Gom Món | Chef | Should Have |
| KDS-005 | Báo Hết Món | Chef | Must Have |
| KDS-006 | Cảnh báo Chậm trễ Chế biến (Kitchen SLA & Delay Alert) | Chef, System | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### KDS-001: Xem Danh sách Thẻ Đơn Chế biến (View Kitchen Tickets)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Màn hình KDS hiển thị danh sách các đơn hàng cần chế biến dưới dạng các thẻ (Tickets) theo thứ tự thời gian đặt hàng tăng dần. Hỗ trợ giao diện tối (Dark Mode) để bảo vệ mắt.
- **Điều kiện tiên quyết:** Thiết bị bếp đã mở ứng dụng KDS.
- **Luồng chính:**
  1. Đầu bếp truy cập màn hình KDS.
  2. Giao diện hiển thị danh sách các Ticket đang hoạt động.
  3. Mỗi Ticket hiển thị: số bàn/mã đơn mang đi, thời gian chờ lũy tiến, danh sách món ăn kèm ghi chú, trạng thái từng món (`Pending`/`Processing`).
  4. Hệ thống đổi màu Ticket để cảnh báo: bình thường (xanh lá/trắng), cảnh báo 10-15 phút (vàng), khẩn cấp > 15 phút (đỏ nhấp nháy).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-001.1:** Given màn hình KDS đang hoạt động, When thu ngân duyệt một đơn Dine-in mới tại bàn 105, Then một thẻ đơn mới của bàn 105 phải xuất hiện lập tức ở cuối danh sách KDS trong vòng dưới 1 giây kèm theo âm thanh thông báo.
  - **AC-KDS-001.2:** Given thẻ đơn có thời gian chờ là 11 phút, When hiển thị trên màn hình KDS, Then viền thẻ đơn phải chuyển sang màu vàng để cảnh báo bếp.

---

### KDS-002: Nhận Làm Món (Start Processing Item)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Bếp xác nhận bắt đầu chế biến một món ăn để thu ngân và khách hàng biết tiến độ.
- **Luồng chính:**
  1. Đầu bếp chọn một món ăn trên Ticket (đang ở trạng thái `Pending`) và chạm/click vào món đó.
  2. Hệ thống chuyển trạng thái món ăn thành `Processing` (Đang làm).
  3. Hệ thống cập nhật thời gian bắt đầu chế biến `SentToKitchenAt` nếu chưa có.
  4. Phát tín hiệu SignalR cập nhật trạng thái món ăn lên POS và điện thoại khách hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-002.1:** Given món ăn đang ở trạng thái `Pending` trên KDS, When đầu bếp click vào món đó, Then trạng thái món ăn phải chuyển sang `Processing` trên DB và gửi tín hiệu SignalR cập nhật giao diện của khách hàng thành "Đang làm" trong vòng dưới 500ms.
  - **AC-KDS-002.2:** When nhận làm món thành công, Then hệ thống phải ghi nhận thời điểm bắt đầu chế biến `SentToKitchenAt` trong cơ sở dữ liệu.

---

### KDS-003: Hoàn Thành Món (Complete Item)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Đánh dấu hoàn thành chế biến món ăn để nhân viên phục vụ bê lên cho khách.
- **Luồng chính:**
  1. Đầu bếp click vào món ăn đang ở trạng thái `Processing` (hoặc `Pending`).
  2. Hệ thống chuyển trạng thái món thành `Done` (Hoàn thành), lưu thời điểm hoàn thành `CompletedAt` và kích hoạt tự động thuật toán trừ kho BOM cho món ăn vừa hoàn thành.
  3. Phát tín hiệu SignalR thông báo hoàn thành món ăn.
  4. Nếu toàn bộ món ăn trong Ticket đều đã chuyển sang `Done`:
     - Tự động xóa Ticket khỏi màn hình KDS.
     - Cập nhật trạng thái đơn hàng `Orders.Status = Completed`.
     - Thông báo cho POS biết đơn hàng đã làm xong để phục vụ.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-003.1:** Given ticket chỉ còn 1 món đang chế biến, When đầu bếp bấm hoàn thành món đó, Then ticket phải biến mất khỏi màn hình KDS và trạng thái đơn hàng trên POS phải chuyển sang màu xanh dương (Completed) qua SignalR.
  - **AC-KDS-003.2:** When hoàn thành món, Then hệ thống phải tự động cập nhật thời điểm hoàn thành `CompletedAt` của món ăn đó trong cơ sở dữ liệu và trừ lượng tồn kho thực tế của các nguyên liệu thô theo công thức BOM.

---

### KDS-004: Xem Tổng hợp Gom Món (Aggregate View)
- **Actor:** Chef
- **Priority:** Should Have
- **Mô tả:** Giao diện gom các món ăn giống nhau từ nhiều đơn hàng đang chờ để đầu bếp nấu chung một mẻ lớn, tối ưu hóa thời gian.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-004.1:** Given KDS đang có 3 đơn hàng khác nhau chứa món "Mì xào giòn" với số lượng lần lượt là 1, 2, 2, When đầu bếp bật màn hình gom món, Then màn hình phải hiển thị đúng "Mì xào giòn: 5 phần".
- **Bảng liên quan:** `OrderItems`

---

### KDS-005: Báo Hết Món (Mark Sold Out)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Đầu bếp báo hết nguyên liệu của một món ăn trực tiếp trên KDS, lập tức vô hiệu hóa món đó trên menu của POS và QR Ordering.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-005.1:** Given đầu bếp chọn món "Nước cam" và bấm "Báo hết món" trên KDS, When món ăn chuyển `IsSoldOut = true`, Then trên menu của khách hàng quét QR, nút thêm món "Nước cam" phải chuyển sang màu xám và hiển thị tag "Hết hàng" trong vòng dưới 1 giây.
- **Bảng liên quan:** `MenuItems`
- **Sự kiện SignalR:** Phát sự kiện `MenuItemSoldOut`.

---

### KDS-006: Cảnh báo Chậm trễ Chế biến (Kitchen SLA & Delay Alert)
- **Actor:** Chef, System
- **Priority:** Must Have
- **Mô tả:** Hệ thống tự động theo dõi thời gian chờ chế biến của từng món ăn và Ticket. Nếu vượt quá ngưỡng thời gian quy định (SLA, ví dụ 15 phút), viền thẻ ticket chuyển sang màu đỏ nhấp nháy và phát âm thanh cảnh báo trễ món.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-006.1:** Given ticket có thời gian chờ chế biến vượt quá 15 phút, When hiển thị trên màn KDS, Then viền thẻ đơn phải chuyển sang màu đỏ nhấp nháy và gửi tín hiệu cảnh báo trễ món `KitchenSlaWarning` lên màn hình thu ngân POS.
- **Bảng liên quan:** `OrderItems`, `Orders`
- **Sự kiện SignalR:** Phát sự kiện `KitchenSlaWarning`.
