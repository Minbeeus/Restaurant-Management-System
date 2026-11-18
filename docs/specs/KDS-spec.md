# Đặc tả Chi tiết Chức năng — Phân hệ KDS (Kitchen Display System)

> **Module:** KDS (Kitchen Display System)  
> **Actor chính:** Nhân viên Bếp (Chef), Quầy pha chế (Bartender), Hệ thống tự động  
> **Công nghệ:** Blazor Server, SignalR Realtime Client, REST API

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| KDS-001 | Xem Thẻ Đơn Chế biến & Lọc theo Trạm (Station Tickets) | Chef, Bartender | Must Have |
| KDS-002 | Nhận Làm Món (Start Processing Item) | Chef | Must Have |
| KDS-003 | Hoàn Thành Món & Trừ Tồn kho Tự động (Complete & BOM) | Chef, System | Must Have |
| KDS-004 | Gom Món Tổng hợp (Batch / Aggregate View) | Chef | Should Have |
| KDS-005 | Báo Hết Món & Đồng bộ Menu Toàn hệ thống (Mark Sold Out) | Chef | Must Have |
| KDS-006 | Cảnh báo Vi phạm SLA & Trễ Chế biến (SLA Timer Alerts) | Chef, System | Must Have |
| KDS-007 | Lọc & Xem Lịch sử Món Đã Hoàn Thành (Completed Tickets) | Chef | Nice to Have |

---

## 2. Đặc tả chi tiết từng tính năng

### KDS-001: Xem Thẻ Đơn Chế biến & Lọc theo Trạm (Station Tickets)
- **Actor:** Chef, Bartender
- **Priority:** Must Have
- **Mô tả:** Màn hình KDS hiển thị danh sách các đơn hàng cần chế biến dưới dạng thẻ (Tickets) theo thứ tự thời gian. 
- **Phân trạm chế biến (Kitchen Station):** 
  - Cho phép chọn lọc theo từng trạm cụ thể (Bếp Pizza, Bếp Lạnh, Bếp Chiên, Quầy Pha Chế Bar) hoặc xem toàn bộ ("Tất cả trạm").
  - Mỗi thẻ hiển thị: Mã đơn, Bàn hoặc Đơn Mang đi (Takeaway), thời gian chờ tăng dần theo từng giây, ghi chú món ăn, trạng thái từng món (`Pending`/`Processing`).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-001.1:** Given đơn hàng mới được gửi từ POS hoặc QR, Then thẻ đơn xuất hiện trên màn hình KDS trong vòng dưới 1 giây qua SignalR `ReceiveKitchenTicket`.
  - **AC-KDS-001.2:** Given đầu bếp chọn trạm "Pha chế", Then KDS chỉ hiển thị các món ăn thuộc danh mục đồ uống gán cho trạm pha chế.
- **Bảng liên quan:** `Orders`, `OrderItems`, `KitchenStations`, `Categories`
- **Sự kiện SignalR:** Đăng ký nhận `ReceiveKitchenTicket`.

---

### KDS-002: Nhận Làm Món (Start Processing Item)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Đầu bếp bấm chọn một món đang ở trạng thái `Pending` để chuyển sang `Processing` (Đang nấu). 
- **Đồng bộ Realtime:** Hệ thống phát sự kiện SignalR `ReceiveOrderItemStatusChanged` cập nhật tiến độ lên POS và màn hình theo dõi đơn của khách hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-002.1:** When đầu bếp click vào món, Then trạng thái món chuyển sang `Processing` và ghi nhận thời điểm bắt đầu `SentToKitchenAt` nếu chưa có.
- **Bảng liên quan:** `OrderItems`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveOrderItemStatusChanged`.

---

### KDS-003: Hoàn Thành Món & Trừ Tồn kho Tự động (Complete & BOM)
- **Actor:** Chef, System
- **Priority:** Must Have
- **Mô tả:** Đầu bếp bấm hoàn thành món ăn (`KitchenStatus = Done`).
- **Quy tắc Trừ kho BOM:**
  - Hệ thống tự động kích hoạt tính toán trừ tồn kho nguyên vật liệu thô theo công thức định lượng (BOM) đã cấu hình cho món ăn (`Recipes`) và các tùy chọn Topping/Modifier (`ModifierRecipes`).
  - Ghi nhận biến động vào bảng `InventoryTransactions` (Type = Out, Reference = OrderItemId).
  - Duy trì tính Idempotent (chỉ trừ kho 1 lần duy nhất cho mỗi món).
- **Trạng thái Đơn hàng:**
  - Khi tất cả các món trong ticket hoàn tất, đơn hàng chuyển sang trạng thái sẵn sàng phục vụ (`Ready`), thông báo đến nhân viên phục vụ và thu ngân POS.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-003.1:** When đầu bếp bấm hoàn thành món, Then trạng thái món đổi thành `Done`, tồn kho các nguyên liệu tương ứng giảm đi đúng định lượng và POS nhận được thông báo món đã sẵn sàng.
- **Bảng liên quan:** `OrderItems`, `Orders`, `Recipes`, `ModifierRecipes`, `Materials`, `InventoryTransactions`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveOrderItemStatusChanged`.

---

### KDS-004: Gom Món Tổng hợp (Batch / Aggregate View)
- **Actor:** Chef
- **Priority:** Should Have
- **Mô tả:** Chế độ gom các món ăn giống nhau từ nhiều đơn hàng đang chờ chế biến thành một danh sách tổng hợp (ví dụ: "Khoai tây chiên: 4 phần", "Pizza Hải sản: 3 bánh") giúp bếp tối ưu thời gian nấu theo mẻ.
- **Bảng liên quan:** `OrderItems`, `MenuItems`

---

### KDS-005: Báo Hết Món & Đồng bộ Menu Toàn hệ thống (Mark Sold Out)
- **Actor:** Chef
- **Priority:** Must Have
- **Mô tả:** Đầu bếp bấm nút "Báo hết món" trực tiếp từ giao diện KDS khi phát hiện nguyên liệu trong bếp đã hết.
- **Đồng bộ tức thời:** Hệ thống phát sự kiện SignalR `ReceiveSoldOutNotice`. Món ăn trên POS và màn hình QR Ordering của khách hàng lập tức hiển thị nhãn "Hết hàng" và vô hiệu hóa nút đặt món.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-005.1:** When đầu bếp báo hết món A trên KDS, Then POS và QR menu nhận thông báo trong vòng dưới 1 giây và chặn không cho thêm món A vào giỏ hàng mới.
- **Bảng liên quan:** `MenuItems`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveSoldOutNotice`.

---

### KDS-006: Cảnh báo Vi phạm SLA & Trễ Chế biến (SLA Timer Alerts)
- **Actor:** Chef, System
- **Priority:** Must Have
- **Mô tả:** Hệ thống tự động tính toán thời gian chờ chế biến của từng món.
  - Bình thường (< 10 phút): Thẻ hiển thị màu tiêu chuẩn.
  - Cảnh báo (10 - 15 phút): Viền thẻ đổi sang màu Vàng cam.
  - Khẩn cấp / Trễ SLA (> 15 phút): Viền thẻ đổi sang màu Đỏ nhấp nháy, phát cảnh báo âm thanh và gửi sự kiện `ReceiveKitchenSlaWarning` lên màn hình thu ngân POS.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-KDS-006.1:** Given một món ăn chờ quá 15 phút chưa hoàn thành, Then viền thẻ chuyển màu đỏ nhấp nháy và phát tín hiệu cảnh báo trễ lên POS.
- **Bảng liên quan:** `OrderItems`, `Orders`
- **Sự kiện SignalR:** Phát sự kiện `ReceiveKitchenSlaWarning`.

---

### KDS-007: Lọc & Xem Lịch sử Món Đã Hoàn Thành (Completed Tickets)
- **Actor:** Chef
- **Priority:** Nice to Have
- **Mô tả:** Cho phép chuyển đổi chế độ xem để xem lại các ticket đã làm xong trong ca để kiểm tra lại thông tin nếu cần thiết.
- **Bảng liên quan:** `Orders`, `OrderItems`
