# Đặc tả Chi tiết Chức năng — Phân hệ BOH (Back of House)

> **Module:** BOH (Back of House)  
> **Actor chính:** Admin (Chủ nhà hàng), Manager (Quản lý sảnh), Accountant/Stockkeeper (Thủ kho/Kế toán)  
> **Công nghệ:** Blazor Server/WebAssembly

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| BOH-001 | Quản lý Danh mục Món ăn | Manager, Admin | Must Have |
| BOH-002 | Quản lý Thực đơn & Combo | Manager, Admin | Must Have |
| BOH-003 | Quản lý Nhóm Tuỳ chọn | Manager, Admin | Must Have |
| BOH-004 | Quản lý Khu vực Sảnh | Manager, Admin | Must Have |
| BOH-005 | Quản lý Bàn & Mã QR | Manager, Admin | Must Have |
| BOH-006 | Cấu hình Công thức BOM | Admin | Must Have |
| BOH-007 | Quản lý Đơn vị Tính | Admin | Must Have |
| BOH-008 | Quản lý Nhà cung cấp | Manager, Admin | Must Have |
| BOH-009 | Tạo Phiếu Nhập Kho | Manager, Admin, Stockkeeper | Must Have |
| BOH-010 | Kiểm Kê Kho | Manager, Admin, Stockkeeper | Must Have |
| BOH-011 | Quản lý Voucher | Admin | Must Have |
| BOH-012 | Báo cáo Doanh số Bán hàng | Admin, Manager | Must Have |
| BOH-013 | Báo cáo Hiệu suất Sản phẩm | Admin, Manager | Must Have |
| BOH-014 | Báo cáo Hao hụt Kho | Admin, Manager | Must Have |
| BOH-015 | Lịch sử Ca Làm việc | Admin, Manager | Must Have |
| BOH-016 | Cảnh báo Tồn kho Tối thiểu | Admin, Manager, Stockkeeper | Must Have |
| BOH-017 | Quản lý Tài khoản | Admin | Must Have |
| BOH-018 | Quản lý Khách hàng & Hạng | Admin, Manager | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### BOH-001: Quản lý Danh mục Món ăn (Manage Categories)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD danh mục món ăn hiển thị trên menu.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-001.1:** Given danh mục có chứa các món ăn đang hoạt động, When người dùng bấm xóa danh mục đó, Then hệ thống phải chặn lại và hiển thị cảnh báo "Không thể xóa danh mục đang có chứa món ăn hoạt động".
- **Bảng liên quan:** `Categories`

---

### BOH-002: Quản lý Thực đơn & Combo (Manage Menu Items)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD các món ăn trên thực đơn. Hỗ trợ thiết lập món ăn dạng Combo và liên kết các nhóm lựa chọn món thành phần.
- **Luồng chính:**
  1. Quản lý mở màn hình quản lý món ăn.
  2. Bấm "Thêm mới" món ăn. Điền các trường: tên, giá, mô tả.
  3. **Thiết lập Combo:** Tích chọn cờ `IsCombo = true`. Hệ thống mở rộng phần cấu hình liên kết nhóm lựa chọn. Quản lý liên kết các `ModifierGroups` tương ứng (ví dụ: nhóm "Chọn món chính", nhóm "Chọn nước").
  4. Bấm "Lưu". Món ăn được lưu và áp dụng cờ soft delete `IsDeleted = 0`.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-002.1:** Given thêm mới món ăn có tích chọn `IsCombo = true`, When lưu thành công, Then hệ thống phải lưu đúng giá trị `IsCombo = 1` trong bảng `MenuItems` trên cơ sở dữ liệu.
  - **AC-BOH-002.2:** When xóa một món ăn khỏi thực đơn, Then hệ thống phải thực hiện Soft Delete (cập nhật `IsDeleted = 1`, ghi nhận thời gian `DeletedAt` và không hiển thị trên menu bán hàng).
- **Bảng liên quan:** `MenuItems`, `Categories`

---

### BOH-003: Quản lý Nhóm Tuỳ chọn & Lựa chọn (Manage Modifiers)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Tạo nhóm tuỳ chọn (Size, Topping, các món thành phần trong Combo) và các tuỳ chọn chi tiết (Size M, Thêm trân châu, Hamburger...).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-003.1:** Given nhóm tuỳ chọn "Chọn nước combo", When thiết lập `MinSelections = 1` và `MaxSelections = 1`, Then hệ thống phải ép buộc người dùng chỉ được chọn đúng 1 đồ uống khi mua combo này tại POS hoặc QR Ordering.
- **Bảng liên quan:** `ModifierGroups`, `ModifierOptions`, `MenuItemModifierGroups`

---

### BOH-004: Quản lý Khu vực Sảnh (Manage Areas)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Tạo và quản lý sơ đồ khu vực của nhà hàng (Tầng 1, Ngoài trời...).
- **Bảng liên quan:** `Areas`

---

### BOH-005: Quản lý Bàn & Mã QR (Manage Tables & QR Codes)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD các bàn ăn thuộc khu vực. Tự động sinh `QrToken` unique khi tạo bàn mới. Thiết lập mã `QR Pickup` đặt tại quầy cho khách mua mang đi quét gọi món.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-005.1:** When thêm mới bàn thành công, Then hệ thống phải tự động tạo một chuỗi `QrToken` ngẫu nhiên có độ dài 32 ký tự và gán vào bàn đó để đảm bảo tính bảo mật.
- **Bảng liên quan:** `Tables`

---

### BOH-006: Cấu hình Công thức Định lượng BOM (Configure BOM Recipe)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Thiết lập công thức định lượng (BOM) chi tiết cho cả món ăn thường (`MenuItems`) và các tùy chọn đi kèm (`ModifierOptions` - bao gồm Topping, Size hoặc các món tùy chọn trong Combo) để phục vụ trừ kho tự động.
- **Luồng chính:**
  - **Cấu hình cho Món ăn:** Chọn món ăn, thêm các dòng nguyên liệu thô (`Materials`), đơn vị tính và số lượng. Lưu vào bảng `Recipes` và `RecipeItems`.
  - **Cấu hình cho Modifier / Option:** Chọn nhóm tùy chọn, chọn một Option cụ thể (ví dụ: "Trân châu đường đen" hoặc "Đùi gà chiên" trong combo). Thêm các dòng nguyên liệu thô tiêu hao khi khách chọn option này. Lưu vào bảng `ModifierRecipes` và `ModifierRecipeItems`.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-006.1:** Given cấu hình định lượng cho Modifier Option "Thêm trân châu", When thêm dòng nguyên liệu bột năng 20g và lưu, Then hệ thống phải lưu chính xác bản ghi liên kết trong bảng `ModifierRecipes` và `ModifierRecipeItems`.
  - **AC-BOH-006.2:** Given một món ăn hoặc option đã có công thức, When lưu công thức mới, Then hệ thống phải ghi đè công thức cũ và cập nhật thời điểm `UpdatedAt` của công thức.
- **Bảng liên quan:** `Recipes`, `RecipeItems`, `ModifierRecipes`, `ModifierRecipeItems`, `Materials`

---

### BOH-007: Quản lý Đơn vị Tính & Quy đổi (Manage Units)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Quản lý đơn vị đo lường (kg, gram, ml, ly) và định nghĩa hệ số quy đổi giữa đơn vị mua vào và đơn vị định lượng sản xuất.
- **Bảng liên quan:** `UnitOfMeasures`, `UnitConversions`

---

### BOH-008: Quản lý Nhà cung cấp (Manage Suppliers)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD danh sách đối tác cung cấp nguyên vật liệu phục vụ công tác nhập kho.
- **Bảng liên quan:** `Suppliers`

---

### BOH-009: Tạo Phiếu Nhập Kho (Create Goods Receipt)
- **Actor:** Stockkeeper, Admin
- **Priority:** Must Have
- **Mô tả:** Ghi nhận lô hàng nguyên liệu nhập vào kho, cập nhật giá vốn nhập và số lượng tồn hiện tại.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-009.1:** Given nhập kho 10 bao bột cà phê (quy đổi 1 bao = 50kg), When lưu phiếu nhập kho, Then số lượng tồn kho của nguyên liệu bột cà phê trong bảng `Materials` phải tăng thêm đúng 500kg.
- **Bảng liên quan:** `GoodsReceipts`, `GoodsReceiptItems`, `Materials`, `InventoryTransactions`

---

### BOH-010: Kiểm Kê Kho (Conduct Stocktake)
- **Actor:** Stockkeeper, Manager
- **Priority:** Must Have
- **Mô tả:** Thực hiện đếm thực tế tồn kho định kỳ để phát hiện và cân bằng số chênh lệch do hao hụt, thất thoát.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-010.1:** Given tồn hệ thống của Sữa đặc là 10 lon, When thủ kho đếm thực tế được 8 lon và hoàn tất kiểm kê, Then hệ thống phải cập nhật tồn kho thực tế về 8 lon, và tạo giao dịch điều chỉnh `-2 lon` (giá trị hao hụt) trong bảng `InventoryTransactions`.
- **Bảng liên quan:** `Stocktakes`, `StocktakeItems`, `Materials`, `InventoryTransactions`

---

### BOH-011: Quản lý Voucher / Mã Khuyến mãi (Manage Vouchers)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Thiết lập các mã giảm giá cho chương trình khuyến mãi.
- **Bảng liên quan:** `Vouchers`

---

### BOH-012: Báo cáo Doanh số Bán hàng (Sales Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Xem thống kê doanh số bán hàng theo ngày/tuần/tháng, lọc theo phương thức thanh toán, xuất file excel và vẽ biểu đồ trực quan.
- **Bảng liên quan:** `Orders`, `Payments`

---

### BOH-013: Báo cáo Hiệu suất Sản phẩm (Product Performance Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Biểu đồ & bảng thống kê các món ăn bán chạy nhất (Top-selling) và các món không hiệu quả.
- **Bảng liên quan:** `OrderItems`, `MenuItems`

---

### BOH-014: Báo cáo Hao hụt Kho (Inventory Variance Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Thống kê lượng chênh lệch và tổng giá trị tiền bị thất thoát sau các đợt kiểm kê kho, cũng như tự động tổng hợp chi phí nguyên liệu hao hụt do các giao dịch hủy món/hủy đơn (Cancellation Waste) để đánh giá hiệu quả vận hành.
- **Bảng liên quan:** `Stocktakes`, `StocktakeItems`, `InventoryTransactions`

---

### BOH-015: Lịch sử Ca Làm việc (Shift History)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Xem lại danh sách các ca làm việc của thu ngân, kiểm soát số tiền chênh lệch két tiền.
- **Bảng liên quan:** `Shifts`, `Users`

---

### BOH-016: Cảnh báo Tồn kho Tối thiểu (Low Stock Alert)
- **Actor:** Stockkeeper, Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Hiển thị cảnh báo màu đỏ trên dashboard khi số lượng tồn hiện tại của nguyên liệu thô xuống thấp hơn ngưỡng tối thiểu cấu hình.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-016.1:** Given nguyên liệu "Đá viên" có `MinStockLevel = 50kg`, When số lượng tồn kho hiện tại giảm xuống `49kg`, Then hệ thống phải lập tức hiển thị cảnh báo đỏ nổi bật trên màn hình Dashboard quản trị.
- **Bảng liên quan:** `Materials`

---

### BOH-017: Quản lý Tài khoản Nhân viên (Manage Staff Accounts)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** CRUD tài khoản đăng nhập của nhân viên, phân quyền vai trò. Không cho phép tự xóa tài khoản chính mình.
- **Bảng liên quan:** `Users`, `Roles`

---

### BOH-018: Quản lý Khách hàng & Hạng Thành viên (Manage Customers & Tiers)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Xem danh sách khách hàng thành viên, lịch sử giao dịch tích/tiêu điểm và thiết lập điều kiện thăng hạng.
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `LoyaltyTransactions`
