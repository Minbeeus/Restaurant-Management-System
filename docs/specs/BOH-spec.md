# Đặc tả Chi tiết Chức năng — Phân hệ BOH (Back of House / Admin)

> **Module:** BOH (Back of House & Administration)  
> **Actor chính:** Admin (Quản trị viên), Manager (Quản lý sảnh), Stockkeeper (Thủ kho), Accountant (Kế toán)  
> **Công nghệ:** Blazor Server, ASP.NET Core WebAPI, Entity Framework Core 9

---

## 1. Danh sách tính năng (Feature List Summary)

| Feature ID | Tên tính năng | Actor | Độ ưu tiên |
|------------|---------------|-------|------------|
| BOH-001 | Quản lý Danh mục Món ăn (Category Management) | Manager, Admin | Must Have |
| BOH-002 | Quản lý Thực đơn & Món thưởng (Menu Item Management) | Manager, Admin | Must Have |
| BOH-003 | Quản lý Combo & Nhóm Món Thành phần | Manager, Admin | Must Have |
| BOH-004 | Quản lý Nhóm Tuỳ chọn & Topping (Modifier Management) | Manager, Admin | Must Have |
| BOH-005 | Quản lý Khu vực & Sơ đồ Bàn Ăn (Area & Table Management) | Manager, Admin | Must Have |
| BOH-006 | Cấu hình Công thức Định lượng BOM (BOM Recipe Management) | Admin | Must Have |
| BOH-007 | Quản lý Đơn vị Tính & Quy đổi (Units & Conversions) | Admin | Must Have |
| BOH-008 | Quản lý Nhà cung cấp (Supplier Management) | Manager, Admin | Must Have |
| BOH-009 | Quản lý Nhập kho (Goods Receipt Management) | Stockkeeper, Admin | Must Have |
| BOH-010 | Kiểm kê Kho & Cân bằng Tồn (Stocktake Management) | Stockkeeper, Manager | Must Have |
| BOH-011 | Quản lý Chương trình Khuyến mãi / Voucher | Admin | Must Have |
| BOH-012 | Quản lý Khách hàng & Hạng Thành viên (CRM) | Admin, Manager | Must Have |
| BOH-013 | Trung tâm Phê duyệt & Yêu cầu Nhạy cảm (Approval Center) | Admin, Manager | Must Have |
| BOH-014 | Quản lý Ca & Đối soát Doanh thu / Thanh toán (Reconciliation) | Admin, Manager, Cashier | Must Have |
| BOH-015 | Tra cứu Lịch sử Đơn hàng (Order History) | Admin, Manager | Must Have |
| BOH-016 | Báo cáo Doanh thu & Dòng tiền Bán hàng (Revenue Report) | Admin, Manager | Must Have |
| BOH-017 | Báo cáo Món Bán chạy / Xu hướng (Top Movers Report) | Admin, Manager | Must Have |
| BOH-018 | Báo cáo Hao hụt & Lãng phí Kho (Inventory Loss Report) | Admin, Manager | Must Have |
| BOH-019 | Quản lý Nhân sự & Phân quyền (Staff & Roles Management) | Admin | Must Have |
| BOH-020 | Cấu hình Trạm Bếp KDS (Kitchen Stations Management) | Admin | Must Have |
| BOH-021 | Quản lý Máy Tính Tiền POS (Registers Management) | Admin | Must Have |
| BOH-022 | Cấu hình Ngưỡng & Chính sách Bảo mật POS (POS Policy Management) | Admin | Must Have |
| BOH-023 | Cấu hình Thông tin Hệ thống (System Settings) | Admin | Must Have |
| BOH-024 | Nhật ký Kiểm toán Hoạt động (Audit Logs) | Admin | Must Have |

---

## 2. Đặc tả chi tiết từng tính năng

### BOH-001: Quản lý Danh mục Món ăn (Category Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD danh mục món ăn hiển thị trên POS và QR Menu. Hỗ trợ kích hoạt/ngừng kích hoạt, sắp xếp thứ tự và quản lý danh mục đặc biệt (như danh mục "Đổi Điểm").
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-001.1:** Given danh mục có chứa các món ăn đang hoạt động, When xóa danh mục, Then hệ thống chặn lại và thông báo: "Không thể xóa danh mục đang có chứa món ăn hoạt động".
- **Bảng liên quan:** `Categories`

---

### BOH-002: Quản lý Thực đơn & Món thưởng (Menu Item Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD các món ăn trên thực đơn. Cấu hình hình ảnh, giá bán, mô tả, danh mục, liên kết nhóm tùy chọn (Size, Topping) và thiết lập món thưởng đổi điểm. Áp dụng Soft Delete.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-002.1:** When xóa món ăn, Then hệ thống thực hiện Soft Delete (`IsDeleted = true`) và ghi nhận thời gian `DeletedAt`.
- **Bảng liên quan:** `MenuItems`, `Categories`, `MenuItemModifierGroups`

---

### BOH-003: Quản lý Combo & Nhóm Món Thành phần (Combo Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Tạo và quản lý gói Combo. Thiết lập các nhóm lựa chọn món thành phần bắt buộc/tùy chọn (ví dụ: Nhóm Món Chính chọn 1 món, Nhóm Đồ Uống chọn 1 món kèm phụ thu nếu có).
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-003.1:** Given combo có 2 nhóm lựa chọn, When lưu cấu hình, Then hệ thống lưu chính xác vào `ComboGroups` và `ComboItems`.
- **Bảng liên quan:** `MenuItems`, `ComboGroups`, `ComboItems`

---

### BOH-004: Quản lý Nhóm Tuỳ chọn & Topping (Modifier Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Quản lý các nhóm tuỳ chọn (Size, Đường, Đá, Topping) và các giá trị tùy chọn chi tiết kèm giá phụ thu (`ModifierOptions`). Hỗ trợ ràng buộc số lượng chọn tối thiểu (`MinSelections`) và tối đa (`MaxSelections`).
- **Bảng liên quan:** `ModifierGroups`, `ModifierOptions`, `MenuItemModifierGroups`

---

### BOH-005: Quản lý Khu vực & Sơ đồ Bàn Ăn (Area & Table Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** Tạo và quản lý các khu vực sảnh (Tầng 1, Tầng 2, Sân vườn) và các bàn ăn tương ứng. Tự động sinh `QrToken` duy nhất khi tạo mới bàn để phục vụ in mã QR gọi món tại bàn.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-005.1:** When tạo mới một bàn ăn, Then hệ thống tự động sinh `QrToken` bảo mật (GUID/32 ký tự) và lưu vào bảng `Tables`.
- **Bảng liên quan:** `Areas`, `Tables`

---

### BOH-006: Cấu hình Công thức Định lượng BOM (BOM Recipe Management)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Thiết lập công thức định lượng nguyên vật liệu (Bill of Materials) cho cả món ăn chính (`Recipes`, `RecipeItems`) và từng tùy chọn modifier/topping (`ModifierRecipes`, `ModifierRecipeItems`) để tự động trừ kho khi món hoàn tất chế biến.
- **Bảng liên quan:** `Recipes`, `RecipeItems`, `ModifierRecipes`, `ModifierRecipeItems`, `Materials`

---

### BOH-007: Quản lý Đơn vị Tính & Quy đổi (Units & Conversions)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Định nghĩa đơn vị đo lường (kg, gram, lít, ml, lon, hộp) và tỷ lệ quy đổi giữa đơn vị nhập mua hàng với đơn vị định lượng sản xuất.
- **Bảng liên quan:** `UnitOfMeasures`, `UnitConversions`

---

### BOH-008: Quản lý Nhà cung cấp (Supplier Management)
- **Actor:** Manager, Admin
- **Priority:** Must Have
- **Mô tả:** CRUD thông tin nhà cung cấp nguyên vật liệu (Tên, Mã số thuế, SĐT, Email, Địa chỉ, Người liên hệ).
- **Bảng liên quan:** `Suppliers`

---

### BOH-009: Quản lý Nhập kho (Goods Receipt Management)
- **Actor:** Stockkeeper, Admin
- **Priority:** Must Have
- **Mô tả:** Tạo và quản lý các phiếu nhập kho nguyên vật liệu từ nhà cung cấp. Ghi nhận số lượng thực nhập, đơn giá nhập, tự động tính tổng tiền và cập nhật số lượng tồn kho tức thời.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-009.1:** When hoàn tất lưu phiếu nhập kho, Then số lượng tồn hiện tại của các nguyên liệu trong phiếu phải tăng thêm tương ứng và ghi log giao dịch kho `InventoryTransactions` (Type = In).
- **Bảng liên quan:** `GoodsReceipts`, `GoodsReceiptItems`, `Materials`, `InventoryTransactions`

---

### BOH-010: Kiểm kê Kho & Cân bằng Tồn (Stocktake Management)
- **Actor:** Stockkeeper, Manager
- **Priority:** Must Have
- **Mô tả:** Tạo đợt kiểm kê kho thực tế, đối chiếu số lượng tồn lý thuyết trên hệ thống với số lượng đếm thực tế. Ghi nhận chênh lệch và tự động điều chỉnh cân bằng kho.
- **Tiêu chí chấp nhận (Acceptance Criteria):**
  - **AC-BOH-010.1:** When xác nhận chốt kiểm kê, hệ thống tự động cập nhật số lượng tồn thực tế vào `Materials` và ghi log hao hụt/thừa kho vào `InventoryTransactions` (Type = StocktakeVariance).
- **Bảng liên quan:** `Stocktakes`, `StocktakeItems`, `Materials`, `InventoryTransactions`

---

### BOH-011: Quản lý Chương trình Khuyến mãi / Voucher
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Thiết lập các mã voucher giảm giá (theo số tiền cố định hoặc theo tỷ lệ phần trăm %), giá trị giảm tối đa, giá trị đơn hàng tối thiểu và hạn sử dụng.
- **Bảng liên quan:** `Vouchers`

---

### BOH-012: Quản lý Khách hàng & Hạng Thành viên (CRM)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Quản lý danh sách khách hàng thân thiết, xem điểm tích lũy, lịch sử giao dịch và cấu hình chính sách các hạng thành viên (`CustomerTiers`) bao gồm ngưỡng điểm thăng hạng và tỷ lệ giảm giá trực tiếp (`DiscountPercent`).
- **Bảng liên quan:** `Customers`, `CustomerTiers`, `LoyaltyTransactions`

---

### BOH-013: Trung tâm Phê duyệt & Yêu cầu Nhạy cảm (Approval Center)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Trung tâm xử lý và phê duyệt các yêu cầu nhạy cảm gửi từ POS:
  - Yêu cầu hủy món đã chế biến / đang chế biến.
  - Yêu cầu hủy đơn hàng.
  - Yêu cầu giảm giá đơn hàng vượt thẩm quyền thu ngân.
  - Yêu cầu rút tiền / nộp tiền két giữa ca.
- **Luồng chính:**
  1. Yêu cầu tạo từ POS được lưu vào bảng `ApprovalRequests` với trạng thái `Pending`.
  2. Quản lý/Admin truy cập Approval Center, xem chi tiết lý do và thông tin đơn hàng/nhân viên.
  3. Bấm "Duyệt" (Approved) hoặc "Từ chối" (Rejected) kèm ghi chú.
  4. Hệ thống thực hiện thao tác tương ứng (hủy món, điều chỉnh tiền, ghi log) và thông báo realtime về máy POS.
- **Bảng liên quan:** `ApprovalRequests`, `Orders`, `OrderItems`, `CashMovements`

---

### BOH-014: Quản lý Ca & Đối soát Doanh thu / Thanh toán (Reconciliation)
- **Actor:** Admin, Manager, Cashier
- **Priority:** Must Have
- **Mô tả:** Xem danh sách ca làm việc (`Shifts`), chi tiết số dư mở ca, doanh thu tiền mặt, chuyển khoản, số tiền chênh lệch lúc kết ca. Cho phép thực hiện đối soát chi tiết từng đơn hàng và phương thức thanh toán.
- **Bảng liên quan:** `Shifts`, `Payments`, `PaymentReconciliations`, `CashMovements`

---

### BOH-015: Tra cứu Lịch sử Đơn hàng (Order History)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Tra cứu danh sách toàn bộ đơn hàng trong hệ thống theo ngày, trạng thái (Draft, Confirmed, Completed, Cancelled), bàn hoặc mã đơn. Cho phép xem lại chi tiết hóa đơn (Bill Detail) và nhật ký chế biến.
- **Bảng liên quan:** `Orders`, `OrderItems`, `Payments`, `Tables`

---

### BOH-016: Báo cáo Doanh thu & Dòng tiền Bán hàng (Revenue Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Báo cáo tổng hợp doanh thu theo ngày/tháng/năm hoặc theo khoảng thời gian tùy chọn. Phân rã doanh thu theo tiền mặt, chuyển khoản VietQR, tổng số đơn hàng, chiết khấu và thuế VAT. Hỗ trợ biểu đồ trực quan.
- **Bảng liên quan:** `Orders`, `Payments`

---

### BOH-017: Báo cáo Món Bán chạy / Xu hướng (Top Movers Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Thống kê các món ăn có số lượng bán nhiều nhất và mang lại doanh số cao nhất trong kỳ để tối ưu thực đơn.
- **Bảng liên quan:** `Orders`, `OrderItems`, `MenuItems`

---

### BOH-018: Báo cáo Hao hụt & Lãng phí Kho (Inventory Loss Report)
- **Actor:** Admin, Manager
- **Priority:** Must Have
- **Mô tả:** Thống kê hao hụt nguyên vật liệu do lệch kiểm kê và chi phí nguyên liệu bị tiêu hao do hủy món/hủy đơn sau khi bếp đã chế biến (`CancellationWaste`).
- **Bảng liên quan:** `InventoryTransactions`, `Materials`, `Stocktakes`

---

### BOH-019: Quản lý Nhân sự & Phân quyền (Staff & Roles Management)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Quản lý danh sách nhân viên, tài khoản đăng nhập, gán vai trò (`Admin`, `Manager`, `Cashier`, `Chef`, `Staff`) và phân quyền chi tiết (Permissions: tạo đơn, duyệt yêu cầu, xem báo cáo, quản lý kho).
- **Bảng liên quan:** `Users`, `Roles`

---

### BOH-020: Cấu hình Trạm Bếp KDS (Kitchen Stations Management)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Định nghĩa và quản lý các trạm chế biến trong nhà hàng (Bếp Nóng, Bếp Lạnh, Quầy Pha Chế Bar, Bếp Pizza). Cho phép phân loại món ăn thuộc trạm nào để hiển thị lọc chính xác trên màn hình KDS.
- **Bảng liên quan:** `KitchenStations`, `Categories`

---

### BOH-021: Quản lý Máy Tính Tiền POS (Registers Management)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Quản lý danh sách máy thu ngân/quầy POS trong nhà hàng (Mã quầy, Tên quầy, Vị trí). Mỗi ca làm việc của thu ngân có thể gắn liền với một máy tính tiền xác định để theo dõi két tiền.
- **Bảng liên quan:** `Registers`, `Shifts`

---

### BOH-022: Cấu hình Ngưỡng & Chính sách Bảo mật POS (POS Policy Management)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Cấu hình các chính sách vận hành và hạn mức ủy quyền tại quầy POS:
  - Yêu cầu mã PIN Quản lý khi hủy món đã chế biến.
  - Hạn mức giảm giá tối đa thu ngân được phép tự quyết (ví dụ: tối đa 10% hoặc 50.000 VNĐ, vượt quá cần Quản lý duyệt).
  - Yêu cầu phê duyệt khi hủy toàn bộ đơn hàng.
- **Bảng liên quan:** `ApprovalRequests`, `Users`

---

### BOH-023: Cấu hình Thông tin Hệ thống (System Settings)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Cấu hình thông tin nhà hàng (Tên quán, Địa chỉ, Số điện thoại, Mã số thuế, Tỷ lệ thuế VAT mặc định, Lời chào chân trang bill K80, Tự động đẩy đơn sang KDS). Dữ liệu được lưu trữ bền vững trong cơ sở dữ liệu.
- **Bảng liên quan:** `SystemSettings` (hoặc cấu hình persistence)

---

### BOH-024: Nhật ký Kiểm toán Hoạt động (Audit Logs)
- **Actor:** Admin
- **Priority:** Must Have
- **Mô tả:** Tra cứu nhật ký hành động nhạy cảm trong hệ thống (Đăng nhập, Thay đổi cấu hình, Phê duyệt hủy đơn, Thao tác két tiền, Điều chỉnh kho).
- **Bảng liên quan:** `AuditLogEntries`
