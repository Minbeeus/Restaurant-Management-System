# Đặc tả Chi tiết Chức năng — Phân hệ POS (Point of Sale)

> **Module:** POS (Point of Sale)  
> **Actor chính:** Thu ngân (Cashier)  
> **Công nghệ:** Blazor Server/WebAssembly, SignalR Realtime Client

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| POS-001 | Xem Sơ đồ Bàn | Cashier | Must Have |
| POS-002 | Tạo Đơn Dine-in | Cashier | Must Have |
| POS-003 | Tạo Đơn Pickup | Cashier | Must Have |
| POS-004 | Thêm Món & Combo | Cashier | Must Have |
| POS-005 | Hủy Món / Giảm số lượng | Cashier | Must Have |
| POS-006 | Chuyển Bàn | Cashier | Must Have |
| POS-007 | Gộp Bàn | Cashier | Must Have |
| POS-008 | Tách Hóa Đơn | Cashier | Should Have |
| POS-009 | Tra cứu Khách hàng | Cashier | Must Have |
| POS-010 | Áp dụng Tiêu Điểm | Cashier | Must Have |
| POS-011 | Áp dụng Voucher | Cashier | Must Have |
| POS-012 | In Hóa đơn Tạm tính | Cashier | Must Have |
| POS-013 | Thanh toán Tiền mặt | Cashier | Must Have |
| POS-014 | Thanh toán VietQR | Cashier | Must Have |
| POS-015 | Duyệt Đơn từ QR | Cashier | Must Have |
| POS-016 | Mở Ca | Cashier | Must Have |
| POS-017 | Kết Ca (Z-Report) | Cashier | Must Have |
| POS-018 | Xử lý Đổi/Chuyển/Gộp Bàn Realtime & Table Session | Cashier | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### POS-001: Xem Sơ đồ Bàn (View Table Map)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hiển thị trực quan danh sách các bàn ăn theo từng khu vực (Tầng 1, Tầng 2...). Trạng thái bàn được đồng bộ realtime từ SignalR.
- **Điều kiện tiên quyết:** Thu ngân đã đăng nhập.
- **Luồng chính:**
  1. Thu ngân truy cập màn hình Sơ đồ bàn.
  2. Hệ thống tải danh sách các khu vực (`Areas`) và các bàn (`Tables`).
  3. Hiển thị trực quan từng bàn với màu sắc tương ứng theo trạng thái:
     - Xám: `Trống (Available)`
     - Đỏ: `Đang bị chiếm (Occupied)`
     - Vàng: `Đơn mới (New Order)`
     - Xanh lá: `Chờ thanh toán (Billing)`
  4. Thu ngân click vào bàn để mở giao diện chi tiết đơn hàng hoặc tạo đơn mới.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-001.1:** Given thu ngân đang mở màn hình Sơ đồ bàn, When có khách gửi đơn gọi món qua QR tại bàn 102, Then bàn 102 trên màn hình POS lập tức chuyển sang màu Vàng (New Order) trong vòng dưới 1 giây mà không cần tải lại trang.
  - **AC-POS-001.2:** Given bàn 105 đang hiển thị màu xanh lá (Billing), When thu ngân bấm chọn bàn 105, Then hệ thống phải điều hướng chính xác vào chi tiết đơn hàng đang hoạt động của bàn 105.
- **Bảng liên quan:** `Areas`, `Tables`, `Orders`
- **Sự kiện SignalR:** Đăng ký nhận sự kiện `TableStatusChanged`.

---

### POS-002: Tạo Đơn Dine-in (Create Dine-in Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Khởi tạo một đơn ăn tại bàn mới liên kết với bàn cụ thể.
- **Điều kiện tiên quyết:** Thu ngân đang mở ca làm việc (`Shifts.Status = Open`) và bàn được chọn đang ở trạng thái `Trống (Available)`.
- **Luồng chính:**
  1. Thu ngân click vào bàn trống trên sơ đồ bàn.
  2. Hệ thống mở màn hình bán hàng, tự động khởi tạo một đơn hàng mới ở trạng thái `Confirmed` (vì thu ngân trực tiếp lập đơn), gán `TableId` tương ứng.
  3. Thu ngân thực hiện thêm món và bấm "Gửi bếp".
  4. Hệ thống chuyển trạng thái bàn sang `Đang bị chiếm (Occupied)` và gửi thông báo SignalR.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-002.1:** Given thu ngân chưa thực hiện mở ca, When thu ngân bấm vào một bàn trống bất kỳ, Then hệ thống phải chặn lại và hiển thị cảnh báo "Vui lòng mở ca làm việc trước khi thực hiện bán hàng".
  - **AC-POS-002.2:** Given bàn 101 đang Trống, When thu ngân click vào bàn 101 và chọn món rồi lưu đơn, Then trạng thái bàn 101 lập tức đổi thành Đỏ (Occupied) trên toàn hệ thống.
- **Bảng liên quan:** `Tables`, `Orders`, `Shifts`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged`.

---

### POS-003: Tạo Đơn Pickup (Create Pickup Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Khởi tạo một đơn hàng mang đi hoặc mua trực tiếp tại quầy không liên kết với bàn ăn.
- **Điều kiện tiên quyết:** Thu ngân đang mở ca làm việc.
- **Luồng chính:**
  1. Thu ngân bấm nút "Đơn mang đi" (Pickup Order).
  2. Hệ thống mở màn hình bán hàng, tự động đặt `OrderType = Pickup` và sinh mã đơn hàng tạm thời (VD: `PK-001`).
  3. Thu ngân thêm món, nhập tên khách (nếu có) và lưu đơn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-003.1:** Given màn hình bán hàng của đơn mang đi mới, When thu ngân bấm "Lưu đơn", Then hệ thống phải tạo một đơn hàng mới có `TableId = NULL` và `OrderType = 1` (Pickup).
- **Bảng liên quan:** `Orders`, `Shifts`
- **Sự kiện SignalR:** Không.

---

### POS-004: Thêm Món & Combo (Add Items to Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Tìm kiếm món ăn và thêm vào đơn hàng hiện tại, lựa chọn các option modifier đi kèm hoặc chọn các món thành phần trong Combo.
- **Điều kiện tiên quyết:** Giao diện chi tiết đơn hàng đang mở.
- **Luồng chính:**
  1. Thu ngân tìm món bằng cách gõ tên, mã món hoặc click chọn theo danh mục.
  2. Click vào món ăn:
     - **Nếu là Món thường có Modifier:** Hệ thống mở pop-up chọn modifier (Size, Topping...). Thu ngân chọn option và số lượng.
     - **Nếu là Món Combo (`IsCombo = true`):** Hệ thống mở pop-up hiển thị các nhóm món bắt buộc chọn trong combo (ModifierGroups của combo). Thu ngân phải chọn đủ số lượng món thành phần (ModifierOptions) trong từng nhóm mới được bấm "Xác nhận".
  3. Bấm "Xác nhận" để thêm món/combo vào danh sách đơn.
  4. Hệ thống lưu snapshot tên món và giá bán tại thời điểm chọn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-004.1:** Given pop-up Combo "Combo Trưa Vui Vẻ" có nhóm "Món chính" (bắt buộc chọn 1), When thu ngân chưa chọn món chính nào mà bấm "Xác nhận", Then hệ thống phải chặn lại và cảnh báo "Vui lòng chọn 1 món chính trong Combo".
  - **AC-POS-004.2:** Given một món ăn trong đơn, When thu ngân tăng số lượng món hoặc thêm topping, Then tổng tiền tạm tính của đơn hàng phải tự động cập nhật ngay trên giao diện trong vòng dưới 100ms.
- **Bảng liên quan:** `Orders`, `OrderItems`, `OrderItemModifiers`, `MenuItems`, `ModifierOptions`
- **Sự kiện SignalR:** Phát sự kiện `NewOrderSentToKitchen`.

---

### POS-005: Hủy Món / Giảm số lượng (Void Item)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hủy một hoặc nhiều món ăn đã lưu trong đơn hàng.
- **Điều kiện tiên quyết:** Đơn hàng chưa thanh toán.
- **Luồng chính:**
  1. Thu ngân bấm nút hủy món/giảm số lượng của một món đã gửi bếp.
  2. Hệ thống hiển thị modal yêu cầu nhập lý do hủy món (bắt buộc).
  3. Thu ngân nhập lý do và bấm xác nhận.
  4. Hệ thống xóa món (hoặc giảm số lượng), ghi nhận vào bảng `VoidLogs` và tính lại tổng tiền đơn hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-005.1:** Given món ăn đã được lưu vào database, When thu ngân thực hiện xóa món mà để trống ô lý do hủy, Then hệ thống phải hiển thị cảnh báo đỏ "Lý do hủy món là bắt buộc" và không cho phép hoàn tất.
  - **AC-POS-005.2:** When hủy món thành công, Then hệ thống phải tự động cập nhật lại tổng tiền đơn hàng và tạo một bản ghi chi tiết trong bảng `VoidLogs`.
- **Bảng liên quan:** `Orders`, `OrderItems`, `VoidLogs`
- **Sự kiện SignalR:** Phát sự kiện `OrderItemStatusChanged` gửi xuống KDS để bếp biết ngưng chế biến (nếu món đang ở trạng thái Pending/Processing).

---

### POS-006: Chuyển Bàn (Move Table)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Di chuyển toàn bộ đơn hàng từ bàn hiện tại sang một bàn trống khác và cập nhật phiên cho khách tại bàn.
- **Điều kiện tiên quyết:** Bàn nguồn đang Occupied/Billing, bàn đích đang ở trạng thái Available.
- **Luồng chính:**
  1. Thu ngân chọn chức năng "Chuyển bàn".
  2. Chọn bàn đích trống. Bấm xác nhận.
  3. Hệ thống cập nhật `TableId` của đơn hàng sang bàn đích.
  4. Cập nhật trạng thái bàn nguồn thành `Available` và bàn đích thành trạng thái của bàn nguồn cũ.
  5. Phát sự kiện SignalR thông báo chuyển bàn sang POS và QR Client.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-006.1:** When chuyển bàn thành công, Then hệ thống phải phát sự kiện `TableSessionMoved` chứa Token phiên cũ và `TableId` bàn mới để điện thoại khách tại bàn tự động chuyển giao diện theo bàn mới mà không mất giỏ hàng.
- **Bảng liên quan:** `Orders`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged` (cho cả 2 bàn) và `TableSessionMoved`.

---

### POS-007: Gộp Bàn (Merge Tables)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hợp nhất hóa đơn của nhiều bàn vào một bàn chính.
- **Điều kiện tiên quyết:** Cả bàn nguồn và bàn đích đều đang có đơn hàng chưa thanh toán.
- **Luồng chính:**
  1. Thu ngân chọn chức năng "Gộp bàn".
  2. Chọn bàn nguồn và bàn đích. Bấm xác nhận.
  3. Hệ thống chuyển toàn bộ `OrderItems` của đơn hàng nguồn sang đơn hàng đích.
  4. Hủy đơn hàng nguồn (chuyển `OrderStatus.Cancelled`), chuyển trạng thái bàn nguồn về `Available`.
  5. Tính toán lại tổng tiền đơn hàng đích.
  6. Phát sự kiện SignalR thông báo chuyển giao session cho các khách hàng tại bàn nguồn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-007.1:** When gộp bàn thành công, Then tất cả các thiết bị khách hàng đang kết nối tại bàn nguồn phải nhận được SignalR `TableSessionMoved` điều hướng họ về trang theo dõi đơn hàng của bàn đích.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged`, `TableSessionMoved` và `NewOrderSentToKitchen`.

---

### POS-008: Tách Hóa Đơn (Split Bill)
- **Actor:** Cashier
- **Priority:** Should Have
- **Mô tả:** Tách một số món ăn từ đơn hàng hiện tại sang một đơn hàng mới.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-008.1:** When thực hiện tách món, Then hệ thống phải tạo một bản ghi `Orders` mới chứa các món được chọn, cập nhật lại số lượng và tổng tiền của cả đơn cũ và đơn mới chính xác.
- **Bảng liên quan:** `Orders`, `OrderItems`

---

### POS-009: Tra cứu Khách hàng Thành viên (Lookup Customer)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Tra cứu khách hàng bằng số điện thoại để tích điểm hoặc áp dụng ưu đãi giảm giá theo hạng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-009.1:** When thu ngân nhập số điện thoại hợp lệ (10 chữ số) đã đăng ký, Then hệ thống phải trả về đúng tên khách, hạng thành viên và số điểm khả dụng hiện tại trong két dữ liệu dưới 500ms.
- **Bảng liên quan:** `Customers`, `CustomerTiers`

---

### POS-010: Áp dụng Tiêu Điểm Loyalty (Redeem Loyalty Points)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Sử dụng điểm tích lũy của khách hàng để giảm trừ tiền trực tiếp trên hóa đơn.
- **Quy tắc nghiệp vụ:**
  - BR-POS-010: Số tiền giảm trừ từ điểm loyalty không được vượt quá tổng số tiền của đơn hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-010.1:** Given khách hàng có 100 điểm khả dụng (tương đương 5.000đ), When thu ngân nhập tiêu 100 điểm, Then đơn hàng phải hiển thị số tiền giảm trừ loyalty là 5.000 VNĐ và tổng tiền cần thanh toán giảm đi đúng 5.000 VNĐ.
- **Bảng liên quan:** `Orders`, `Customers`

---

### POS-011: Áp dụng Voucher (Apply Voucher)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Nhập mã voucher khuyến mãi để áp dụng giảm giá cho đơn hàng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-011.1:** Given voucher "GIAM10" giảm 10% tối đa 20.000đ cho đơn từ 150.000đ, When đơn hàng có SubTotal là 300.000đ và thu ngân áp mã, Then số tiền giảm giá phải hiển thị đúng là 20.000 VNĐ (đạt trần tối đa).
- **Bảng liên quan:** `Orders`, `Vouchers`

---

### POS-012: In Hóa đơn Tạm tính (Print Temporary Bill)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Xuất và in hóa đơn tạm tính để nhân viên gửi cho khách kiểm tra trước khi tính tiền.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-012.1:** When thu ngân bấm "In tạm tính", Then trạng thái bàn trong database phải chuyển thành `Billing` và đồng bộ lên tất cả các client POS khác qua SignalR.
- **Bảng liên quan:** `Orders`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged`.

---

### POS-013: Thanh toán Tiền mặt (Cash Payment)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Hoàn tất thanh toán hóa đơn bằng hình thức tiền mặt.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-013.1:** When thanh toán tiền mặt thành công, Then hệ thống phải tự động tạo bản ghi `Payments` (trạng thái `Completed`), cập nhật đơn hàng sang `Paid`, và đưa bàn về `Trống (Available)`. (Ghi chú: Việc trừ kho BOM đã được thực hiện tự động khi bếp hoàn thành chế biến món ăn).
- **Bảng liên quan:** `Orders`, `Payments`, `Shifts`, `Materials`, `Customers`, `Tables`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged`.

---

### POS-014: Thanh toán Chuyển khoản VietQR & Webhook SePay (Bank Transfer Payment)
- **Actor:** Cashier, System
- **Priority:** Must Have
- **Mô tả:** Tạo mã VietQR động để khách chuyển khoản và tự động đối soát qua SePay Webhook. Xử lý trường hợp ngoại lệ chuyển khoản chậm hoặc thu ngân thu tiền mặt trước.
- **Luồng chính:**
  1. Thu ngân chọn hình thức thanh toán "Chuyển khoản".
  2. Hệ thống tạo bản ghi `Payments` (Status = Pending), sinh nội dung chuyển khoản độc nhất (`SepayContent` ví dụ: `BILL105X`).
  3. Hiển thị mã VietQR động chứa tài khoản ngân hàng, số tiền và nội dung chuyển khoản là `SepayContent`.
  4. Khách hàng thực hiện chuyển khoản.
  5. Webhook từ SePay gọi về API backend. Hệ thống đối soát tự động:
     - **Trường hợp 1 (Bình thường):** Khớp số tiền và nội dung chuyển khoản. Cập nhật Payment sang `Completed`, đơn hàng sang `Paid`, tích điểm, phát SignalR đóng modal thanh toán và đưa bàn về `Trống`. (Lưu ý: Kho BOM đã tự động trừ khi bếp làm xong món).
     - **Trường hợp 2 (Xung đột tiền mặt trước):** Nếu đơn hàng đã được thu ngân bấm hoàn tất bằng tiền mặt trước đó (đơn đã ở trạng thái `Paid`), hệ thống ghi nhận log webhook vào `SepayWebhookLogs` với `IsProcessed = false`, ghi nhận lỗi `ErrorLog = "Đơn hàng đã được thanh toán bằng tiền mặt trước đó"`, đồng thời gửi thông báo SignalR Alert cảnh báo lên màn hình POS của thu ngân để thu ngân làm việc trực tiếp với khách (trả lại tiền hoặc ghi nhận thừa).
     - **Trường hợp 3 (Sai nội dung/số tiền):** Webhook ghi nhận lỗi, không cập nhật hóa đơn. Thu ngân bấm nút "Đối soát thủ công" trên POS để xem danh sách lịch sử biến động số dư chưa khớp, chọn đúng giao dịch và bấm "Cưỡng bức thanh toán" để hoàn tất hóa đơn bằng tay.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-014.1:** Given đơn hàng đã được thanh toán bằng tiền mặt, When webhook SePay gửi thông tin chuyển khoản khớp nội dung đến sau đó, Then hệ thống không được thay đổi trạng thái đơn hàng, ghi lỗi vào `SepayWebhookLogs.ErrorLog` và hiển thị thông báo đỏ cảnh báo thu ngân trên POS.
  - **AC-POS-014.2:** Given đơn hàng chưa thanh toán, When nhận được webhook SePay khớp đúng số tiền và nội dung, Then hệ thống phải tự động đóng modal QR trên POS và cập nhật trạng thái bàn thành `Trống (Available)` trong vòng dưới 1 giây.
- **Bảng liên quan:** `Orders`, `Payments`, `SepayWebhookLogs`, `Materials`, `Customers`, `Tables`
- **Sự kiện SignalR:** Đăng ký nhận sự kiện `PaymentCompleted`, phát sự kiện `TableStatusChanged` và `PaymentAlert`.

---

### POS-015: Duyệt Đơn từ QR Ordering (Approve QR Order)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thu ngân duyệt các đơn hàng do khách hàng tự gọi món qua QR tại bàn trước khi chuyển xuống bếp.
- **Luồng chính:**
  1. Khách gửi đơn QR (Draft), hệ thống đẩy thông báo realtime lên màn hình POS qua SignalR `NewQrOrderReceived`.
  2. Bàn liên quan trên sơ đồ bàn chuyển sang trạng thái `New Order` (Vàng).
  3. Thu ngân click vào bàn để xem danh sách các món khách đã đặt.
  4. Thu ngân bấm "Duyệt đơn".
  5. Hệ thống thực hiện:
     - Kiểm tra trạng thái tồn kho thực tế của các món (Xử lý Edge Case nếu món vừa báo `Sold Out` tại KDS).
     - Áp dụng `Optimistic Locking` (sử dụng `RowVersion`) tránh xung đột nếu khách bấm hủy/sửa đơn từ điện thoại đúng lúc thu ngân bấm duyệt.
     - Chuyển đơn hàng sang trạng thái `Confirmed` / `InService`.
     - Tạo các `KitchenTicketItem` và đẩy thông báo SignalR `NewOrderSentToKitchen` xuống KDS.
  6. Trạng thái bàn tự động chuyển từ `New Order` về `Occupied` (Đỏ).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-015.1:** Given đơn hàng Draft của bàn 102 đang ở trạng thái chờ duyệt, When thu ngân bấm "Duyệt đơn", Then trạng thái đơn hàng phải chuyển sang `Confirmed` và trạng thái bàn 102 phải chuyển sang `Occupied` trên toàn hệ thống trong vòng dưới 500ms.
  - **AC-POS-015.2:** Given món trong đơn QR vừa bị bếp bấm báo hết (`Sold Out`), When thu ngân bấm Duyệt đơn, Then hệ thống phải chặn duyệt, thông báo món hết và cho phép thu ngân xóa món hết để duyệt phần còn lại.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Tables`, `MenuItems`
- **Sự kiện SignalR:** Đăng ký nhận `NewQrOrderReceived`, phát sự kiện `NewOrderSentToKitchen`, phát sự kiện `TableStatusChanged`.

---

### POS-016: Mở Ca (Open Shift)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Khai báo số tiền két ban đầu để bắt đầu ca làm việc của thu ngân.
- **Quy tắc nghiệp vụ:**
  - BR-POS-016: Một tài khoản thu ngân chỉ được phép có tối đa một ca làm việc ở trạng thái Open tại một thời điểm.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-016.1:** Given thu ngân đang có một ca làm việc đang mở, When thu ngân bấm mở ca mới, Then hệ thống phải chặn lại và thông báo "Bạn có ca làm việc chưa đóng. Vui lòng kết ca cũ trước".
- **Bảng liên quan:** `Shifts`

---

### POS-017: Kết Ca (Close Shift / Z-Report)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Thực hiện đối soát số dư tiền mặt thực tế tại két vào cuối ca làm việc và xuất báo cáo Z-Report.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-017.1:** Given ca làm việc đang mở, When thu ngân nhập số tiền đếm két thực tế và bấm xác nhận kết ca, Then trạng thái ca làm việc trong cơ sở dữ liệu phải chuyển thành `Closed` và thời gian đóng ca `ClosedAt` phải được ghi nhận chính xác theo giờ hệ thống.
- **Bảng liên quan:** `Shifts`, `Payments`, `Orders`

---

### POS-018: Xử lý Đổi/Chuyển/Gộp Bàn Realtime & Table Session (Table Merging & Splitting Management)
- **Actor:** Cashier, System
- **Priority:** Must Have
- **Mô tả:** Xử lý việc đổi bàn, chuyển bàn hoặc gộp bàn realtime. Đồng bộ Session ID của bàn cũ sang bàn mới để các thiết bị điện thoại của khách quét mã QR tự động nhận diện và cập nhật trạng thái bàn mà không bị ngắt quãng giao dịch.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-POS-018.1:** When thu ngân bấm gộp/chuyển bàn, hệ thống phải phát tín hiệu SignalR `TableSessionMoved` chứa Token phiên cũ và `TableId` bàn mới để điện thoại khách tại bàn tự động chuyển giao diện theo bàn mới mà không mất giỏ hàng hay lệch đơn.
- **Bảng liên quan:** `Tables`, `Orders`, `OrderItems`
- **Sự kiện SignalR:** Phát sự kiện `TableStatusChanged`, `TableSessionMoved`.
