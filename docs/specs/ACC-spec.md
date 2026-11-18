# Đặc tả Chi tiết Chức năng — Phân hệ Quản lý Tài khoản & Loyalty

> **Module:** Account & Loyalty (Xác thực & Thành viên)  
> **Actor chính:** Nhân viên, Quản trị viên, Quản lý, Hệ thống tự động  
> **Công nghệ:** ASP.NET Core Identity / JWT, BCrypt, EF Core Interceptors

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| ACC-001 | Đăng nhập Hệ thống (Username / Password) | Staff, Admin, Chef, Manager | Must Have |
| ACC-002 | Đăng xuất Hệ thống | All Staff | Must Have |
| ACC-003 | Xác thực Mã PIN Quản lý (Manager PIN Authorization) | Admin, Manager | Must Have |
| ACC-004 | Đăng ký & Quản lý Khách hàng Thành viên | Cashier, Manager | Must Have |
| ACC-005 | Tích Điểm Loyalty Tự động | System (Automated) | Must Have |
| ACC-006 | Tiêu Điểm Loyalty Trừ Trực tiếp Hóa đơn | Cashier | Must Have |
| ACC-007 | Đổi Điểm Thành viên Lấy Món thưởng (Reward Items) | Cashier | Must Have |
| ACC-008 | Chiết khấu Tự động Theo Hạng Thành viên (Tier Discount) | System, Cashier | Must Have |
| ACC-009 | Xem Lịch sử Điểm & Tra cứu Thành viên | Admin, Manager, Cashier | Must Have |
| ACC-010 | Xử lý Hủy đơn & Phục hồi Tồn kho / Rollback Điểm thưởng | Admin, Manager, Cashier | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### ACC-001: Đăng nhập Hệ thống (User Login)
- **Actor:** Staff, Admin, Chef, Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Đăng nhập vào hệ thống bằng tài khoản (Username/Password). Trả về JWT token và đồng thời thiết lập Authentication Cookie (`RestaurantAuth`) cho giao diện Blazor Server.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-001.1:** Given tài khoản có `IsActive = true` và `IsDeleted = false`, When người dùng nhập đúng Username và Password, Then hệ thống trả về mã token JWT hợp lệ chứa các claims (`NameIdentifier`, `Name`, `Role`) trong vòng dưới 1 giây.
  - **AC-ACC-001.2:** Given tài khoản bị vô hiệu hóa (`IsActive = false`), When đăng nhập, Then hệ thống từ chối xác thực với mã HTTP 401 hoặc thông báo lỗi: "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin".
- **Bảng liên quan:** `Users`, `Roles`

---

### ACC-002: Đăng xuất Hệ thống (User Logout)
- **Actor:** All Staff
- **Priority:** Must Have
- **Mô tả:** Xoá session làm việc của nhân viên trên thiết bị, hủy Cookie `RestaurantAuth` và token lưu trữ.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-002.1:** When nhân viên bấm đăng xuất, Then hệ thống xóa cookie xác thực và chuyển hướng về màn hình đăng nhập `/login`.
- **Bảng liên quan:** Không.

---

### ACC-003: Xác thực Mã PIN Quản lý (Manager PIN Authorization)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Cho phép thu ngân yêu cầu Quản lý/Admin nhập mã PIN trực tiếp tại quầy POS để ủy quyền thực hiện các thao tác nhạy cảm (Hủy món đã nấu, Hủy đơn, Chiết khấu vượt thẩm quyền, Mở két tiền thủ công).
- **Luồng chính:**
  1. Thu ngân thao tác hành động cần duyệt (hoặc ấn "Xác thực PIN Quản lý").
  2. Modal `PosManagerPinModal` hiển thị bàn phím số yêu cầu nhập mã PIN.
  3. Gửi mã PIN đến `POST /api/v1/auth/verify-manager-pin`.
  4. Hệ thống kiểm tra mã PIN với các tài khoản có Role `Admin` hoặc `Manager` qua thuật toán băm bảo mật BCrypt:
     - Không cho phép bypass mã cứng (hardcode).
     - Khóa tạm thời 15 phút nếu nhập sai quá 5 lần liên tiếp.
  5. Nếu hợp lệ: Trả về trạng thái ủy quyền thành công, cho phép tiếp tục thao tác.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-003.1:** Given mã PIN nhập khớp với mã PIN của Quản lý đang hoạt động, Then API trả về `isValid: true` và ghi log kiểm toán.
  - **AC-ACC-003.2:** Given mã PIN nhập sai quá 5 lần, Then hệ thống từ chối và khóa xác thực trong 15 phút.
- **Bảng liên quan:** `Users`, `Roles`, `AuditLogEntries`

---

### ACC-004: Đăng ký & Quản lý Khách hàng Thành viên (Register Customer Member)
- **Actor:** Cashier, Manager
- **Priority:** Must Have
- **Mô tả:** Đăng ký nhanh khách hàng thành viên mới tại quầy POS hoặc quản lý thông tin khách hàng tại trang BOH Admin. Số điện thoại là định danh duy nhất.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-004.1:** Given số điện thoại đã tồn tại trong hệ thống, When thu ngân tạo khách hàng mới với số điện thoại này, Then hệ thống báo lỗi "Số điện thoại này đã được sử dụng bởi khách hàng khác" và không lưu.
  - **AC-ACC-004.2:** When tạo thành viên thành công, Then hệ thống tự động gán hạng mặc định ban đầu là `Đồng` (TierId tương ứng) và `TotalPoints = 0`, `AvailablePoints = 0`.
- **Bảng liên quan:** `Customers`, `CustomerTiers`

---

### ACC-005: Tích Điểm Loyalty Tự động (Earn Loyalty Points)
- **Actor:** System (Tự động)
- **Priority:** Must Have
- **Mô tả:** Tự động tính điểm tích lũy và cộng vào tài khoản khách hàng ngay sau khi đơn hàng được thanh toán thành công (`Payment.Status = Completed`).
- **Luồng chính:**
  1. Đơn hàng hoàn tất thanh toán và có gắn `CustomerId`.
  2. Lấy `TotalAmount` thực tế sau khi đã trừ giảm giá và voucher.
  3. Tính điểm tích lũy: `Points = Floor(TotalAmount / 10,000)` (hoặc theo cấu hình hệ thống).
  4. Cập nhật:
     - Tạo bản ghi `LoyaltyTransactions` (TransactionType = Earn, Points = số điểm tích).
     - `Customer.TotalPoints += Points`, `Customer.AvailablePoints += Points`.
     - Kiểm tra điều kiện thăng hạng: nếu `TotalPoints` vượt ngưỡng hạng kế tiếp, tự động cập nhật `Customer.TierId`.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-005.1:** Given đơn hàng có khách hàng A, thanh toán 250.000 VNĐ, Then điểm khả dụng và tổng điểm của khách A phải tăng thêm đúng 25 điểm.
  - **AC-ACC-005.2:** Given khách tích lũy đủ điểm ngưỡng hạng Bạc/Vàng, Then hệ thống tự động cập nhật `TierId` sang hạng mới.
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `LoyaltyTransactions`, `Orders`, `Payments`

---

### ACC-006: Tiêu Điểm Loyalty Trừ Trực tiếp Hóa đơn (Redeem Loyalty Points)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Sử dụng điểm tích lũy khả dụng của khách hàng để quy đổi thành tiền mặt giảm trừ trực tiếp vào hóa đơn tại POS.
- **Quy tắc nghiệp vụ:**
  - Điểm quy đổi không được vượt quá `Customer.AvailablePoints`.
  - Số tiền quy đổi không vượt quá tổng tiền của hóa đơn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-006.1:** Given khách có 200 điểm khả dụng, When thu ngân áp dụng tiêu 100 điểm (tương đương 5.000đ) và thanh toán thành công, Then hệ thống trừ 100 điểm (`AvailablePoints -= 100`) và tạo bản ghi `LoyaltyTransactions` với Type = Redeem.
- **Bảng liên quan:** `Orders`, `Customers`, `LoyaltyTransactions`, `Payments`

---

### ACC-007: Đổi Điểm Thành viên Lấy Món thưởng (Redeem Points for Reward Items)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Cho phép khách hàng thành viên sử dụng điểm khả dụng để đổi lấy các món ăn/đồ uống đặc biệt (thuộc nhóm Đổi Điểm hoặc món có cấu hình điểm đổi `PointsCost > 0`).
- **Luồng chính:**
  1. Thu ngân chọn khách hàng thành viên tại quầy POS.
  2. Thu ngân mở danh mục "Đổi Điểm" hoặc chọn món thưởng có cấu hình quy đổi điểm.
  3. Hệ thống kiểm tra số điểm yêu cầu (`PointsCost`) của món:
     - Nếu `Customer.AvailablePoints >= PointsCost`: Thêm món vào đơn với đơn giá 0đ và đánh dấu tiêu điểm thưởng tương ứng.
     - Nếu không đủ điểm: Cảnh báo "Khách hàng không đủ điểm để đổi món này".
  4. Khi đơn hàng hoàn tất: Hệ thống trừ số điểm đổi món khỏi `AvailablePoints` và ghi nhận giao dịch `LoyaltyTransactions`.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-007.1:** Given khách có 150 điểm, món thưởng yêu cầu 100 điểm, When thu ngân thêm món thưởng vào đơn và thanh toán, Then khách được nhận món với giá 0đ và điểm khả dụng còn lại là 50 điểm.
- **Bảng liên quan:** `MenuItems`, `Categories`, `Customers`, `LoyaltyTransactions`, `OrderItems`

---

### ACC-008: Chiết khấu Tự động Theo Hạng Thành viên (Tier Discount)
- **Actor:** System, Cashier
- **Priority:** Must Have
- **Mô tả:** Tự động áp dụng tỷ lệ chiết khấu (%) theo hạng thành viên (`CustomerTier.DiscountPercent`) khi gắn khách hàng vào đơn tại quầy POS.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-008.1:** Given khách hàng thuộc hạng Vàng được hưởng chiết khấu 10%, When thu ngân chọn khách hàng này vào đơn 500.000 VNĐ, Then hệ thống tự động tính giảm 50.000 VNĐ vào hóa đơn.
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `Orders`

---

### ACC-009: Xem Lịch sử Điểm & Tra cứu Thành viên (View Loyalty History)
- **Actor:** Admin, Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Tra cứu thông tin thành viên qua SĐT/Tên, xem chi tiết lịch sử tích/tiêu điểm và đối soát đơn hàng liên kết.
- **Bảng liên quan:** `Customers`, `LoyaltyTransactions`, `Orders`

---

### ACC-010: Xử lý Hủy đơn & Rollback Điểm thưởng (Order Cancellation & Reversal)
- **Actor:** Admin, Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Khi đơn hàng hoàn tất bị hủy hoặc hoàn tiền (`Refund`), hệ thống tự động hoàn lại điểm đã tiêu và thu hồi điểm đã tích của đơn hàng đó trong cùng 1 Database Transaction nguyên tử.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-010.1:** Given đơn hàng đã tích 30 điểm và tiêu 50 điểm của khách, When Quản lý duyệt hủy đơn/hoàn tiền, Then hệ thống phải hoàn lại 50 điểm đã tiêu và thu hồi 30 điểm đã tích cho khách hàng.
- **Bảng liên quan:** `Orders`, `LoyaltyTransactions`, `Customers`, `Refunds`
