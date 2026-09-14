# Đặc tả Chi tiết Chức năng — Phân hệ Quản lý Tài khoản & Loyalty

> **Module:** Account & Loyalty (Xác thực & Thành viên)  
> **Actor chính:** Nhân viên, Quản trị viên, Hệ thống tự động  
> **Công nghệ:** ASP.NET Identity / JWT, EF Core Interceptors

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| ACC-001 | Đăng nhập Hệ thống | Staff, Admin, Chef | Must Have |
| ACC-002 | Đăng xuất | Staff, Admin, Chef | Must Have |
| ACC-003 | Đăng ký Khách hàng Thành viên | Cashier | Must Have |
| ACC-004 | Tích Điểm Loyalty | System (Automated) | Must Have |
| ACC-005 | Tiêu Điểm Loyalty | Cashier | Must Have |
| ACC-006 | Xem Lịch sử Điểm Loyalty | Admin, Manager | Must Have |
| ACC-007 | Xử lý Hủy đơn & Phục hồi Tồn kho / Điểm thưởng (Order Cancellation & Reversal) | Admin, Manager, Cashier | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### ACC-001: Đăng nhập Hệ thống (User Login)
- **Actor:** Staff, Admin, Chef, Manager
- **Priority:** Must Have
- **Mô tả:** Đăng nhập vào hệ thống quản lý bằng tài khoản được cấp để thực hiện quyền hạn tương ứng.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-001.1:** Given tài khoản có `IsActive = true` và `IsDeleted = false`, When người dùng nhập đúng Username và Password, Then hệ thống phải trả về mã token JWT hợp lệ chứa thông tin định danh và phân quyền trong vòng dưới 1 giây.
  - **AC-ACC-001.2:** Given tài khoản bị vô hiệu hóa (`IsActive = false`), When người dùng nhập đúng thông tin đăng nhập, Then hệ thống phải chặn lại và hiển thị thông báo lỗi "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin".
- **Bảng liên quan:** `Users`, `Roles`

---

### ACC-002: Đăng xuất (User Logout)
- **Actor:** Staff, Admin, Chef
- **Priority:** Must Have
- **Mô tả:** Xoá session làm việc của nhân viên trên thiết bị.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-002.1:** When nhân viên bấm đăng xuất, Then hệ thống phải xóa hoàn toàn token JWT khỏi Cookie/LocalStorage của trình duyệt và chuyển hướng về màn hình đăng nhập `/login` lập tức.
- **Bảng liên quan:** Không.

---

### ACC-003: Đăng ký Khách hàng Thành viên (Register Customer Member)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Đăng ký nhanh khách hàng thành viên mới tại quầy POS phục vụ chương trình tích điểm. Số điện thoại là trường duy nhất và bắt buộc.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-003.1:** Given số điện thoại "0901234567" đã tồn tại trên hệ thống, When thu ngân cố gắng tạo khách hàng mới với số điện thoại này, Then hệ thống phải báo lỗi "Số điện thoại này đã được sử dụng bởi khách hàng khác" và chặn không cho lưu.
  - **AC-ACC-003.2:** When tạo thành viên thành công, Then hệ thống phải tự động gán hạng thành viên ban đầu là `Đồng` (TierId = 1) và đặt số điểm ban đầu là `0`.
- **Bảng liên quan:** `Customers`, `CustomerTiers`

---

### ACC-004: Tích Điểm Loyalty (Earn Loyalty Points)
- **Actor:** System (Tự động)
- **Priority:** Must Have
- **Mô tả:** Tự động tính điểm tích lũy và cộng vào tài khoản khách hàng ngay sau khi đơn hàng được thanh toán thành công. Lượng tiền thanh toán tích điểm được tính trên tổng giá trị cuối cùng bao gồm cả món Combo và các Option đi kèm.
- **Điều kiện tiên quyết:** Đơn hàng được thanh toán thành công (`Payment.Status = Completed`) và đơn hàng có liên kết `CustomerId`.
- **Luồng chính:**
  1. Giao dịch thanh toán chuyển trạng thái thành công.
  2. Hệ thống lấy `TotalAmount` của hóa đơn (đã bao gồm giá món chính, giá combo và phụ thu modifier/topping sau khi trừ tất cả các khoản giảm giá).
  3. Áp dụng công thức tính điểm: `Points = Floor(TotalAmount / 10,000)` (Ví dụ: hóa đơn trị giá 355.000đ tích được 35 điểm).
  4. Hệ thống:
     - Tạo bản ghi `LoyaltyTransactions` (Type = Earn, Points = số điểm vừa tính).
     - Cập nhật tài khoản khách hàng: `TotalPoints += Points` và `AvailablePoints += Points`.
     - Kiểm tra thăng hạng thành viên: Nếu `Customer.TotalPoints` đạt ngưỡng tối thiểu của hạng cao hơn, cập nhật `Customer.TierId` sang hạng mới.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-004.1:** Given đơn hàng có liên kết khách hàng A, When hóa đơn được thanh toán thành công với số tiền thực tế khách trả là 250.000 VNĐ, Then số điểm khả dụng của khách hàng A trong database phải tăng thêm đúng 25 điểm.
  - **AC-ACC-004.2:** Given khách hàng A có tổng điểm tích lũy là 490 điểm (Hạng Đồng), When khách hàng hoàn tất hóa đơn 200.000 VNĐ (tích thêm 20 điểm), Then tổng điểm tích lũy của khách hàng tăng lên 510 điểm và hệ thống phải tự động cập nhật hạng thành viên sang `Bạc` (TierId = 2 - ngưỡng tối thiểu 500 điểm).
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `LoyaltyTransactions`, `Orders`, `Payments`

---

### ACC-005: Tiêu Điểm Loyalty (Redeem Loyalty Points)
- **Actor:** Cashier
- **Priority:** Must Have
- **Mô tả:** Sử dụng điểm tích lũy của khách hàng để quy đổi thành tiền mặt giảm trừ trực tiếp vào hóa đơn hiện tại.
- **Quy tắc nghiệp vụ:**
  - BR-ACC-005: Điểm quy đổi không được vượt quá số điểm khả dụng của khách (`AvailablePoints`) và số tiền giảm không được vượt quá tổng hóa đơn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-005.1:** Given khách hàng có 200 điểm khả dụng (tương đương 10.000đ), When thu ngân chọn tiêu 100 điểm (tương đương 5.000đ) và hoàn tất thanh toán hóa đơn, Then hệ thống phải trừ chính xác 100 điểm khả dụng của khách (`AvailablePoints -= 100`) và tạo bản ghi tiêu điểm trong bảng `LoyaltyTransactions` (Type = 1 - Redeem, Points = 100).
- **Bảng liên quan:** `Orders`, `Customers`, `LoyaltyTransactions`

---

### ACC-006: Xem Lịch sử Điểm Loyalty (View Loyalty History)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Tra cứu nhật ký biến động điểm tích lũy của khách hàng phục vụ kiểm tra, đối soát.
- **Bảng liên quan:** `LoyaltyTransactions`, `Customers`

---

### ACC-007: Xử lý Hủy đơn & Phục hồi Tồn kho / Điểm thưởng (Order Cancellation & Reversal)
- **Actor:** Admin, Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Xử lý việc hủy đơn hàng đã hoàn tất thanh toán hoặc hủy đơn khi chưa chế biến. Thực hiện phục hồi số lượng tồn kho nguyên liệu (nếu món chưa chế biến) và rollback điểm Loyalty (trừ lại điểm tích hoặc trả lại điểm đã tiêu).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-ACC-007.1:** Given đơn hàng đã hoàn tất thanh toán có dùng 50 điểm loyalty và đã được tích 20 điểm, When Manager thực hiện Hủy đơn/Hoàn tiền, Then hệ thống phải tự động hoàn lại 50 điểm khả dụng cho khách và trừ đi 20 điểm vừa tích trong 1 DB Transaction duy nhất.
- **Bảng liên quan:** `Orders`, `LoyaltyTransactions`, `Customers`, `InventoryTransactions`, `Materials`
