# Đặc tả Chi tiết Chức năng — Phân hệ POS (Point of Sale)

> **Module:** POS (Point of Sale)  
> **Actor chính:** Thu ngân (Cashier), Quản lý sảnh (Manager)  
> **Công nghệ:** Blazor Server, SignalR Realtime Client, REST API

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| POS-001 | Xem Sơ đồ Bàn & Đồng bộ Realtime | Cashier | Must Have |
| POS-002 | Tạo & Xử lý Đơn Ăn tại Bàn (Dine-in Order) | Cashier | Must Have |
| POS-003 | Giao diện Chuyên dụng & Đơn Mang đi (Takeaway Order) | Cashier | Must Have |
| POS-004 | Thêm Món, Combo & Tùy chọn Modifier/Topping | Cashier | Must Have |
| POS-005 | Đổi Điểm Thưởng Khách hàng Lấy Món ăn (Reward Items) | Cashier | Must Have |
| POS-006 | Hủy Món & Giảm Số Lượng (Void Item Workflow) | Cashier, Manager | Must Have |
| POS-007 | Hủy Đơn Hàng (Cancel Order Workflow) | Cashier, Manager | Must Have |
| POS-008 | Chuyển Bàn & Đổi Bàn Ăn Realtime | Cashier | Must Have |
| POS-009 | Gộp Bàn & Gộp Đơn Hàng (Table Merge) | Cashier | Must Have |
| POS-010 | Tách Hóa Đơn (Split Bill) | Cashier | Should Have |
| POS-011 | Tra cứu & Gắn Khách hàng Thành viên (Loyalty Lookup) | Cashier | Must Have |
| POS-012 | Áp dụng Tiêu Điểm Khách hàng & Voucher Khuyến mãi | Cashier | Must Have |
| POS-013 | Chiết khấu & Giảm giá Đơn hàng (Discount Modal) | Cashier, Manager | Must Have |
| POS-014 | Xác thực Mã PIN Quản lý Tại quầy (Manager PIN Verification) | Manager, Cashier | Must Have |
| POS-015 | Xem Trước & In Tạm tính Phiếu K80 (Pre-bill / Billing) | Cashier | Must Have |
| POS-016 | Thanh toán Tiền mặt & Thối tiền (Cash Payment) | Cashier | Must Have |
| POS-017 | Thanh toán Tự động VietQR & Tích hợp SePay Webhook | Cashier, System | Must Have |
| POS-018 | Tiếp nhận & Duyệt Đơn Gọi món từ QR Ordering | Cashier | Must Have |
| POS-019 | Mở Ca & Khai báo Số dư Két Đầu ca | Cashier | Must Have |
| POS-020 | Công cụ Thu ngân: Nạp/Rút Két (Till In/Out) & Kiểm Kê | Cashier, Manager | Must Have |
| POS-021 | Kết Ca & In Báo cáo Z-Report Máy K80 (K80 End Of Day) | Cashier | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### POS-001: Xem Sơ đồ Bàn & Đồng bộ Realtime (View Table Map)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hiển thị trực quan danh sách các bàn ăn theo từng khu vực (Tầng 1, Tầng 2, Ngoài trời...). Trạng thái bàn được đồng bộ tức thời qua SignalR.
- **Màu sắc trạng thái:**
  - Xám: `Trống (Available)`
  - Đỏ: `Đang có khách (Occupied)`
  - Vàng: `Đơn mới từ QR cần duyệt (New Order)`
  - Xanh lá: `Chờ thanh toán / Đã in tạm tính (Billing)`
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-001.1:** Given khách gửi đơn QR tại bàn 102, When POS nhận sự kiện `ReceiveNewQrOrder` hoặc `ReceiveTableStatusSync`, Then bàn 102 lập tức đổi sang màu Vàng (New Order) mà không cần tải lại trang.
- **Bảng liên quan:** `Areas`, `Tables`, `Orders`

---

### POS-002: Tạo & Xử lý Đơn Ăn tại Bàn (Dine-in Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thu ngân chọn một bàn trống trên sơ đồ bàn để bắt đầu tạo đơn hàng ăn tại chỗ. Hệ thống tự động chuyển trạng thái bàn sang `Occupied` và tạo phiên phục vụ.
- **Bảng liên quan:** `Orders`, `Tables`

---

### POS-003: Giao diện Chuyên dụng & Đơn Mang đi (Takeaway Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Phân hệ POS Takeaway chuyên biệt (`PosTakeawayMain.razor`) phục vụ khách mua mang về, giao hàng hoặc đặt nhanh tại quầy mà không cần gắn bàn ăn. Hỗ trợ modal thanh toán nhanh riêng (`PosTakeawayPaymentModal.razor`).
- **Luồng chính:**
  1. Thu ngân vào màn hình Mang đi (Takeaway).
  2. Chọn món nhanh từ Menu Grid hoặc gõ tìm kiếm.
  3. Bấm "Thanh toán ngay".
  4. Mở modal `PosTakeawayPaymentModal`, chọn Tiền mặt hoặc VietQR, hoàn tất đơn và in hóa đơn K80 ngay lập tức.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-003.1:** When thanh toán đơn Takeaway hoàn tất, đơn chuyển trạng thái `Completed`/`Paid`, món được gửi xuống bếp và không làm thay đổi trạng thái bất kỳ bàn nào trên sơ đồ bàn.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Payments`

---

### POS-004: Thêm Món, Combo & Tùy chọn Modifier/Topping
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thêm món ăn vào giỏ hàng (`OrderCart`). Hỗ trợ cấu hình tùy chọn kích cỡ (Size), Topping, Đường, Đá qua `PosOptionBuilder` hoặc chọn các món thành phần của gói Combo qua `PosComboBuilder`.
- **Bảng liên quan:** `MenuItems`, `ModifierGroups`, `ModifierOptions`, `ComboGroups`, `ComboItems`, `OrderItems`

---

### POS-005: Đổi Điểm Thưởng Khách hàng Lấy Món ăn (Reward Items)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hỗ trợ thêm các món thuộc nhóm "Đổi Điểm" vào giỏ hàng với giá 0đ khi khách hàng có đủ điểm khả dụng. Hệ thống tự động trừ điểm thưởng của khách khi đơn hàng thanh toán thành công.
- **Bảng liên quan:** `MenuItems`, `Customers`, `LoyaltyTransactions`, `OrderItems`

---

### POS-006: Hủy Món & Giảm Số Lượng (Void Item Workflow)
- **Actor:** Cashier, Manager
- **Priority:** Must Have
- **Mô tả:** Xóa món hoặc giảm số lượng món đã gọi trong đơn hàng.
  - **Món chưa chế biến (`Pending`):** Thu ngân được phép hủy trực tiếp kèm lý do hủy.
  - **Món đang làm hoặc đã nấu (`Processing`/`Done`):** Mở modal `PosVoidItemModal`, yêu cầu xác thực PIN Quản lý hoặc gửi yêu cầu phê duyệt sang Approval Center. Khi được duyệt, hệ thống ghi nhận hao hụt nguyên vật liệu (`CancellationWaste`) vào bảng `InventoryTransactions`.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-006.1:** When hủy món đã làm xong, Then hệ thống ghi nhận bản ghi vào `VoidLogs` và ghi log hao hụt kho tương ứng với định lượng BOM của món bị hủy.
- **Bảng liên quan:** `OrderItems`, `VoidLogs`, `ApprovalRequests`, `InventoryTransactions`

---

### POS-007: Hủy Đơn Hàng (Cancel Order Workflow)
- **Actor:** Cashier, Manager
- **Priority:** Must Have
- **Mô tả:** Hủy toàn bộ đơn hàng đang phục vụ qua `PosCancelOrderModal`. Yêu cầu nhập lý do hủy và yêu cầu mã PIN xác thực của Quản lý. Nếu đơn đã thanh toán, hệ thống thực hiện hoàn tiền (`Refund`), giải phóng bàn và hoàn trả điểm thưởng/tồn kho nếu hợp lệ.
- **Bảng liên quan:** `Orders`, `Refunds`, `Tables`, `ApprovalRequests`

---

### POS-008: Chuyển Bàn & Đổi Bàn Ăn Realtime (Table Transfer)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Chuyển toàn bộ đơn hàng của bàn hiện tại sang một bàn trống khác.
- **Cơ chế Realtime:** Phát sự kiện SignalR `TableSessionMoved` kèm thông tin `oldTableId` và `newTableId`. Thiết bị khách hàng đang mở trang QR của bàn cũ sẽ tự động nhận diện và cập nhật sang bàn mới mà không mất dữ liệu giỏ hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-008.1:** Given bàn A chuyển sang bàn B, Then bàn A trở về màu xám (Available), bàn B chuyển sang màu đỏ (Occupied) và điện thoại khách tự động cập nhật URL bàn B.
- **Bảng liên quan:** `Tables`, `Orders`

---

### POS-009: Gộp Bàn & Gộp Đơn Hàng (Table Merge)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Gộp hai hoặc nhiều bàn đang có khách vào chung một đơn hàng chính. Các món ăn từ đơn phụ được chuyển sang đơn chính, bàn phụ được giải phóng về trạng thái trống hoặc liên kết với bàn chính.
- **Bảng liên quan:** `Tables`, `Orders`, `OrderItems`

---

### POS-010: Tách Hóa Đơn (Split Bill)
- **Actor:** Cashier
- **Priority:** Should Have
- **Mô tả:** Tách một phần các món ăn hoặc số lượng từ đơn hiện tại sang một hóa đơn/bàn mới để thanh toán riêng biệt theo yêu cầu của khách.
- **Bảng liên quan:** `Orders`, `OrderItems`

---

### POS-011: Tra cứu & Gắn Khách hàng Thành viên (Loyalty Lookup)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Mở modal `PosMemberModal` tra cứu thành viên bằng Số điện thoại hoặc Họ tên. Hiển thị hạng thẻ, điểm khả dụng, và tỷ lệ chiết khấu theo hạng (`CustomerTier.DiscountPercent`). Gắn khách hàng vào đơn hàng để hưởng ưu đãi.
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `Orders`

---

### POS-012: Áp dụng Tiêu Điểm Khách hàng & Voucher Khuyến mãi
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** 
  - **Tiêu điểm:** Nhập số điểm muốn tiêu (quy đổi 1 điểm = 50đ hoặc 100đ tùy cấu hình), trừ trực tiếp vào số tiền phải thanh toán.
  - **Voucher:** Nhập hoặc quét mã Voucher khuyến mãi, hệ thống kiểm tra điều kiện (hạn dùng, đơn tối thiểu) và tính số tiền giảm.
- **Bảng liên quan:** `Orders`, `Vouchers`, `Customers`, `LoyaltyTransactions`

---

### POS-013: Chiết khấu & Giảm giá Đơn hàng (Discount Modal)
- **Actor:** Cashier, Manager
- **Priority:** Must Have
- **Mô tả:** Mở `PosDiscountModal` để giảm giá theo tỷ lệ phần trăm (%) hoặc số tiền cố định (VNĐ). Nếu tỷ lệ giảm giá vượt hạn mức cho phép của thu ngân theo chính sách POS, hệ thống kích hoạt yêu cầu nhập mã PIN Quản lý để ủy quyền.
- **Bảng liên quan:** `Orders`, `ApprovalRequests`

---

### POS-014: Xác thực Mã PIN Quản lý Tại quầy (Manager PIN Verification)
- **Actor:** Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Modal `PosManagerPinModal` cho phép Quản lý nhập mã PIN 4-6 chữ số ngay trên màn hình cảm ứng để phê duyệt nhanh các thao tác hủy món, giảm giá, mở két mà không cần đăng nhập lại tài khoản.
- **Bảng liên quan:** `Users`, `Roles`

---

### POS-015: Xem Trước & In Tạm tính Phiếu K80 (Pre-bill / Billing)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thu ngân bấm in phiếu tạm tính để đưa khách kiểm tra trước khi thanh toán. Bàn ăn chuyển trạng thái sang `Billing` (màu xanh lá). Đơn hàng không bị đổi trạng thái sang Paid. Hỗ trợ xem trước và in khổ giấy K80 qua máy in nhiệt LAN/USB.
- **Bảng liên quan:** `Orders`, `Tables`

---

### POS-016: Thanh toán Tiền mặt & Thối tiền (Cash Payment)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Chọn hình thức Tiền mặt trên `PosPaymentModal`. Nhập số tiền khách đưa (có gợi ý các mệnh giá tiền chẵn), hệ thống tự tính số tiền thối lại cho khách. Sau khi xác nhận:
  - Tạo bản ghi `Payments` (PaymentMethod = Cash, Status = Completed).
  - Cập nhật `Orders.Status = Paid`.
  - Tự động cộng điểm Loyalty cho khách nếu có.
  - Giải phóng bàn về trạng thái `Available`.
  - Tự động mở modal in hóa đơn hoàn tất.
- **Bảng liên quan:** `Orders`, `Payments`, `Tables`, `Customers`, `LoyaltyTransactions`

---

### POS-017: Thanh toán Tự động VietQR & Tích hợp SePay Webhook
- **Actor:** Cashier, System
- **Priority:** Must Have
- **Mô tả:** Tạo mã VietQR động chứa số tiền chính xác và mã đơn hàng duy nhất (`DHxxxxx`). Màn hình POS hiển thị mã QR và tự động lắng nghe Webhook SePay qua SignalR:
  - Khi khách quét mã chuyển khoản thành công, SePay gửi Webhook đến WebAPI.
  - WebAPI xác thực HMAC, cập nhật đơn hàng sang Paid và phát sự kiện `ReceivePaymentCompleted` qua SignalR.
  - Màn hình POS tự động nhận diện thanh toán thành công, đóng modal và in hóa đơn mà thu ngân không cần thao tác bấm nút.
- **Bảng liên quan:** `Payments`, `Orders`, `SepayWebhookLogs`, `Tables`

---

### POS-018: Tiếp nhận & Duyệt Đơn Gọi món từ QR Ordering
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Khi khách đặt món qua QR, thông báo xuất hiện trên POS và bàn đổi sang màu Vàng (`New Order`). Thu ngân kiểm tra danh sách món khách gọi, bấm "Duyệt đơn" để chuyển đơn sang bếp chế biến (`NewOrderSentToKitchen`).
- **Bảng liên quan:** `Orders`, `OrderItems`, `Tables`

---

### POS-019: Mở Ca & Khai báo Số dư Két Đầu ca (Open Shift)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thu ngân bắt đầu ca làm việc bằng việc chọn máy thu ngân (`Register`) và nhập số tiền mặt ban đầu có trong két (`OpeningBalance`). Mỗi thu ngân chỉ có tối đa 1 ca mở cùng thời điểm.
- **Bảng liên quan:** `Shifts`, `Registers`

---

### POS-020: Công cụ Thu ngân: Nạp/Rút Két (Till In/Out) & Kiểm Kê
- **Actor:** Cashier, Manager
- **Priority:** Must Have
- **Mô tả:** Cung cấp modal `PosCashierToolsModal`:
  - **Till In (Nạp tiền):** Ghi nhận nạp thêm tiền lẻ vào két trong ca kèm lý do.
  - **Till Out (Rút tiền):** Rút bớt tiền mặt nộp về két an toàn của nhà hàng (yêu cầu PIN quản lý).
  - **Kiểm kê tiền mặt:** Đếm tiền mặt thực tế giữa ca để kiểm soát chênh lệch.
- **Bảng liên quan:** `CashMovements`, `Shifts`

---

### POS-021: Kết Ca & In Báo cáo Z-Report Máy K80 (K80 End Of Day)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Cuối ca làm việc, thu ngân đếm tiền thực tế trong két, nhập số tiền cuối ca. Hệ thống tính số tiền lý thuyết theo doanh thu tiền mặt, chênh lệch (thừa/thiếu), ghi nhận đóng ca và mở modal `K80EndOfDayModal` in trực tiếp báo cáo tổng kết ca ra máy in nhiệt khổ K80.
- **Bảng liên quan:** `Shifts`, `Payments`, `CashMovements`
