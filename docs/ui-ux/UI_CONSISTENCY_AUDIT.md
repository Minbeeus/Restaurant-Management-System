# BÁO CÁO TOÀN DIỆN AUDIT GIAO DIỆN & HỆ THỐNG THIẾT KẾ (UI/UX CONSISTENCY AUDIT)

**Dự án:** Hệ thống Quản lý Nhà hàng Tích hợp — Restaurant Management POS / KDS / BOH / Admin / QR  
**Vai trò:** Senior UI/UX Designer & Lead Frontend Architect  
**Tài liệu:** `docs/ui-ux/UI_CONSISTENCY_AUDIT.md`  
**Ngày thực hiện:** 17/10/2026  
**Trạng thái kiểm tra nền tảng:** Build: PASS (0 Errors, 0 Warnings) | Domain & E2E Tests: PASS (295/295 tests)

---

## 1. TỔNG QUAN KIẾN TRÚC FRONTEND & HIỆN TRẠNG KỸ THUẬT

### 1.1. Công nghệ nền tảng
- **Framework:** .NET 9.0 (ASP.NET Core Blazor InteractiveServer).
- **Styling Engines:** 
  1. **Tailwind CSS (CDN Plugin Forms & Container Queries):** Cấu hình trực tiếp trong `src/Restaurant.Web/Components/App.razor` với extended palette dựa trên triết lý Material Design 3 / Mozzarella & Basil.
  2. **Bootstrap 5.x CSS:** Nhúng file `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` trong `App.razor`.
  3. **Global CSS:** `wwwroot/app.css` định nghĩa các CSS Variables (`--color-primary`, `--color-secondary`, v.v.) và override một số class Bootstrap (`.btn-primary`, `.form-control`, v.v.).
  4. **Component-scoped CSS:** Các file `.razor.css` nằm rải rác (`MainLayout.razor.css`, `PosMain.razor.css`, `CategoryTabs.razor.css`, v.v.).
- **Hệ thống Biểu tượng (Iconography):**
  - **Chính thức:** Google Material Symbols Outlined (`<span class="material-symbols-outlined">icon_name</span>`).
  - **Tồn dư lỗi thời:** Bootstrap Icons (`<i class="bi bi-...">`) và FontAwesome syntax (`<i class="...">`) xuất hiện trong một số component dùng chung (`AppButton.razor`, `SlaTimerBadge.razor`, `AppToast.razor`), nhưng không được import CSS khiến icon bị rỗng hoặc lỗi hiển thị.
- **Typography:**
  - **Heading:** `Montserrat`, font-weight 600, 700.
  - **Body & Data:** `Inter`, font-weight 400, 500, 600.
  - **Số liệu & Tiền tệ:** Chưa được chuẩn hoá triệt để sang font số liệu đồng dạng (`tabular-nums` / `font-mono`).

---

## 2. DANH MỤC TOÀN BỘ ROUTE & CẤU TRÚC GIAO DIỆN (ROUTE INVENTORY)

| Route | Tên màn hình / Chức năng | Phân hệ | Layout sử dụng | Trạng thái tích hợp API | Đánh giá UI hiện tại |
|---|---|---|---|---|---|
| `/` | Trang chủ chuyển hướng | Chung | `MainLayout` | Static | Chờ chuẩn hóa giao diện đón tiếp |
| `/login` | Đăng nhập tập trung mã PIN | Auth | `EmptyLayout` | API Quick Login | Hardcode CSS & màu inline, lệch chuẩn token |
| `/admin/login` | Đăng nhập Quản trị viên (BOH) | Auth | `EmptyLayout` | API Password Login | Split-screen tương đối tốt, cần đồng bộ token |
| `/pos/login` | Điều hướng đăng nhập POS | Auth | `EmptyLayout` | Redirect | Trang chuyển hướng tạm |
| `/kds/login` | Điều hướng đăng nhập KDS | Auth | `EmptyLayout` | Redirect | Trang chuyển hướng tạm |
| `/admin/dashboard` | Dashboard Quản trị & KPI | Admin/BOH | `AdminLayout` | API Dashboard | Layout tốt, màu sắc stat cards còn phân mảnh |
| `/admin/categories` | Quản lý danh mục món | Admin/BOH | `AdminLayout` | API Menu | Chưa dùng chung Form/Table chuẩn |
| `/admin/menu-items` | Quản lý danh sách món ăn | Admin/BOH | `AdminLayout` | API Menu | Form thêm/sửa full-page tốt, bảng còn style riêng |
| `/admin/menu/combos` | Cấu hình Combo món ăn | Admin/BOH | `AdminLayout` | API Combo | Giao diện phong phú nhưng button/badge chưa đồng bộ |
| `/admin/modifiers` | Quản lý nhóm tùy chọn (Topping) | Admin/BOH | `AdminLayout` | API Menu | Cần chuẩn hóa Modal & Table |
| `/admin/recipes` | Định lượng công thức món (BOM) | Admin/BOH | `AdminLayout` | API Recipe/BOH | Tính toán số liệu cần font tabular/mono |
| `/admin/materials` | Danh mục nguyên vật liệu | Admin/BOH | `AdminLayout` | API Inventory | Bảng và form đẹp nhưng tự chế CSS riêng |
| `/admin/suppliers` | Quản lý nhà cung cấp | Admin/BOH | `AdminLayout` | API Inventory | Style chưa đồng bộ với MaterialManagement |
| `/admin/goods-receipts` | Nhập kho nguyên vật liệu | Admin/BOH | `AdminLayout` | API Inventory | Cần kiểm tra form nhập số lượng/giá |
| `/admin/stocktakes` | Kiểm kê & đối soát kho | Admin/BOH | `AdminLayout` | API Inventory | Chênh lệch kho cần màu semantic chuẩn |
| `/admin/orders` | Lịch sử đơn hàng toàn hệ thống | Admin/BOH | `AdminLayout` | API Orders | Bảng dữ liệu dài, cần chuẩn hóa filter |
| `/admin/areas` | Quản lý khu vực bàn | Admin/BOH | `AdminLayout` | API Tables | Cần đồng bộ với sơ đồ sàn POS |
| `/admin/tables` | Quản lý danh sách bàn ăn | Admin/BOH | `AdminLayout` | API Tables | Cần đồng bộ badge trạng thái với POS |
| `/admin/shifts` | Quản lý ca làm việc thu ngân | Admin/BOH | `AdminLayout` | API Shifts | Cần format số tiền kết ca rõ ràng |
| `/admin/staff` | Danh sách nhân viên & phân quyền | Admin/BOH | `AdminLayout` | API Identity | Cần chuẩn hóa modal cấp mã PIN |
| `/admin/approvals` | Trung tâm duyệt rủi ro tài chính | Admin/BOH | `AdminLayout` | API Approvals | Thẻ yêu cầu rõ ràng, cần chuẩn hóa nút Approve/Reject |
| `/admin/reconciliation` | Đối soát doanh thu & thanh toán | Admin/BOH | `AdminLayout` | API Reconcile | Cần chuẩn hóa badge đối soát SePay |
| `/admin/reports/revenue` | Báo cáo doanh thu & lợi nhuận | Admin/BOH | `AdminLayout` | API Reports | Cần biểu đồ và bảng dữ liệu nhất quán |
| `/admin/reports/top-movers` | Báo cáo món bán chạy | Admin/BOH | `AdminLayout` | API Reports | Cần rank badge và thanh tỉ lệ |
| `/admin/reports/inventory-loss` | Báo cáo thất thoát kho | Admin/BOH | `AdminLayout` | API Reports | Màu cảnh báo đỏ/vàng cần tuân thủ Design System |
| `/admin/customers` | Quản lý khách hàng thân thiết | Admin/BOH | `AdminLayout` | API CRM | Cần hiển thị thẻ hạng thành viên chuẩn |
| `/admin/vouchers` | Quản lý chương trình khuyến mãi | Admin/BOH | `AdminLayout` | API Marketing | Cần mã voucher monospaced rõ ràng |
| `/admin/system/users` | Quản lý tài khoản hệ thống | Admin/BOH | `AdminLayout` | API System | Cần đồng bộ với Staff |
| `/admin/system/roles` | Phân quyền vai trò | Admin/BOH | `AdminLayout` | API System | Checkbox ma trận quyền cần rõ ràng |
| `/admin/system/registers` | Quản lý máy POS / Két tiền | Admin/BOH | `AdminLayout` | API System | Cần đồng bộ layout |
| `/admin/system/kitchen-stations` | Quản lý phân luồng bếp | Admin/BOH | `AdminLayout` | API System | Cần đồng bộ màu trạm với KDS |
| `/admin/system/pos-policy` | Chính sách vận hành POS | Admin/BOH | `AdminLayout` | API System | Cần form switch toggle đồng nhất |
| `/admin/system/settings` | Cài đặt hệ thống chung | Admin/BOH | `AdminLayout` | API System | Cần tab chuyển nhóm cài đặt |
| `/admin/system/audit-logs` | Nhật ký hoạt động hệ thống | Admin/BOH | `AdminLayout` | API Audit | Log cần font code/mono và timestamp chuẩn |
| `/pos` | Dashboard POS Thu ngân | POS | `PosLayout` | API POS/SignalR | 70/30 split layout tốt, nút bấm to rõ |
| `/pos/tables` | Sơ đồ bàn & phòng | POS | `PosLayout` | API Tables/SignalR | Hỗ trợ cả Canvas 2D & Grid, cần chuẩn hóa thanh công cụ |
| `/pos/order` | Màn hình gọi món chính | POS | `PosLayout` | API Orders/SignalR | Cấu hình 30% giỏ hàng / 70% thực đơn rất tốt |
| `/pos/takeaway` | Màn hình tạo đơn mang về | POS | `PosLayout` | API Orders | Cần đồng bộ kiểu dáng với PosMain |
| `/pos/bills` | Xem và in lại hóa đơn | POS | `PosLayout` | API Orders | Khổ in nhiệt K80 cần chuẩn hóa typography |
| `/kds` | Màn hình Bếp thời gian thực | KDS | `KdsLayout` | API KDS/SignalR | Chế độ Dark mode, cần chuẩn hóa SLA badges & Combo items |
| `/qr` & `/order` | Gọi món QR tại bàn cho khách | QR | `EmptyLayout` | API QR Public | Tối ưu di động, cần đồng bộ nút bấm & giỏ hàng trượt |
| `/qr/status` | Trạng thái món khách đã đặt | QR | `EmptyLayout` | API QR Public | Cần hiển thị trạng thái bếp trực quan |

---

## 3. PHÂN TÍCH CHI TIẾT CÁC VẤN ĐỀ LỆCH CHUẨN (DISCREPANCY MATRIX)

### 3.1. Xung đột giữa Bootstrap và Tailwind CSS
- **Hiện tượng:** Cả `bootstrap.min.css` và Tailwind CDN cùng được nạp trong `App.razor`.
- **Hậu quả:** 
  - Bootstrap can thiệp vào các thẻ cơ bản (`h1-h6`, `table`, `a`, `button`, `input`), ghi đè font-size, margin và border-color của Tailwind.
  - Các component trong `Components/UI/` ban đầu được viết theo cú pháp Bootstrap (`class="btn btn-primary"`, `class="table table-hover"`, `class="modal fade"`, `class="form-control"`), dẫn đến giao diện mang phong cách Bootstrap cũ, góc bo 4px nhạt nhoà, không ăn khớp với phong cách thiết kế hiện đại (rounded-xl 12px/16px) của các màn hình POS/Admin mới.
  - Các nhà phát triển sau đó đã viết đè trực tiếp các class Tailwind dài vào từng trang để "tránh" các component dùng chung bị xấu, gây ra sự phân mảnh mã nguồn nghiêm trọng.

### 3.2. Sự thiếu nhất quán về Màu sắc & Token ngữ nghĩa
- **Màu thương hiệu (Brand Colors):**
  - Màu chủ đạo quy định: Basil Green `#486500` (Primary), On-Primary `#FFFFFF`.
  - Trên thực tế: Rải rác khắp các file là sự pha trộn giữa Tailwind utilities chuẩn (`bg-primary`), màu xanh lá khác của Tailwind (`bg-emerald-600`, `bg-emerald-700`, `text-green-600`), hoặc CSS var (`var(--color-primary)`).
- **Màu cảnh báo & Nguy hiểm (Semantic Danger/Alert):**
  - Màu tertiary quy định: Roasted Tomato `#AD2B19` / `#BA1A1A`.
  - Thực tế: Nhiều màn hình dùng `bg-red-500`, `bg-red-600`, `text-red-700`, `bg-rose-500`, `text-rose-600` một cách tùy tiện.
- **Màu trạng thái đơn hàng & Bàn ăn:**
  - Bàn trống: có nơi dùng `bg-emerald-100 text-emerald-800`, có nơi dùng `border-success-bg`.
  - Bàn có khách: có nơi dùng `bg-red-500/10 text-red-700`, có nơi dùng `border-error-container`.
  - Bàn đang tính tiền: dùng `border-secondary-container`.
  - Cần một bảng ánh xạ `StatusBadge` duy nhất, không để từng trang tự sinh class màu.

### 3.3. Typography & Kiểu số liệu
- **Headings:**
  - Montserrat được cấu hình nhưng một số trang dùng `text-2xl font-bold`, có trang dùng `font-headline-sm`, có trang dùng `text-xl font-black uppercase`.
- **Số tiền tệ (Currency Numbers):**
  - Trong POS: `TotalAmount.ToString("N0") đ` (font-mono font-black text-primary).
  - Trong Admin: `@mat.CostPerUnit.ToString("N0")đ` (font-mono text-sm).
  - Trong QR Menu: `@item.Price.ToString("N0")đ` (font-bold text-base).
  - Cần quy chuẩn thống nhất: Số tiền luôn đi kèm ký hiệu `đ` ở sau, có khoảng cách hoặc định dạng chuẩn `N0`, sử dụng font monospace/tabular-nums để các cột số căn phải thẳng hàng tuyệt đối.

### 3.4. Border Radius, Spacing và Shadows
- **Border Radius:**
  - Nút bấm: Có chỗ dùng `rounded` (4px), `rounded-lg` (8px), `rounded-xl` (12px), `rounded-full` (9999px).
  - Card/Panel: Có chỗ dùng `rounded-lg`, `rounded-xl`, `rounded-2xl` (16px).
  - Input: Có chỗ dùng `rounded` (4px từ Bootstrap), có chỗ dùng `rounded-xl`.
- **Kích thước nút & Touch Target:**
  - Trong POS: Nhiều nút có chiều cao chuẩn 44px - 48px rất tốt cho màn hình cảm ứng.
  - Tuy nhiên ở một số modal phụ (như `PosDiscountModal` hoặc `PosVoidItemModal`): Nút bấm nhỏ (32px), khó thao tác bằng ngón tay.
- **Shadows:**
  - Phân mảnh giữa `shadow-none`, `shadow-xs`, `shadow-sm`, `shadow-md`, `shadow-lg`, `shadow-2xl`. Cần chuẩn hóa: Card bình thường dùng `shadow-2xs` hoặc `shadow-xs`, Dropdown/Popover dùng `shadow-lg`, Modal dùng `shadow-2xl`.

### 3.5. Biểu tượng (Icons)
- **Vấn đề nghiêm trọng:**
  - `AppButton.razor`: `<i class="@Icon me-2"></i>` -> Gây lỗi hiển thị nếu truyền tên Material Symbol.
  - `SlaTimerBadge.razor`: `<i class="bi bi-clock me-1"></i>` -> Icon Bootstrap bị mất vì thiếu thư viện.
  - `AppToast.razor`: `<i class="bi @Icon me-2 fs-5"></i>` -> Icon Bootstrap bị mất.
  - Toàn bộ workspace đã đồng thuận sử dụng Material Symbols Outlined (`<span class="material-symbols-outlined">...</span>`). Do đó, tất cả component dùng chung phải được chuẩn hóa sang Material Symbols Outlined 100%.

### 3.6. Component dùng chung (`src/Restaurant.Web/Components/UI`)
1. **`AppButton.razor`:**
   - Dùng class Bootstrap `.btn .btn-primary`. Không đồng bộ với hệ thống nút bo góc 12px Tailwind đang dùng ở các trang nghiệp vụ.
   - Thiếu các biến thể: `Success`, `Warning`, `Outline`, v.v.
   - Thiếu hỗ trợ Material Symbols icon chuẩn.
2. **`DataTable.razor`:**
   - Sử dụng table HTML thuần với class Bootstrap `.table .table-hover` và style inline.
   - Thiếu thanh Toolbar tìm kiếm, thiếu phân trang tích hợp, header styling chưa đồng bộ với Admin.
3. **`AppModal.razor`:**
   - Sử dụng markup Bootstrap `.modal .fade .show` kết hợp style inline `rgba(0,0,0,0.5)`.
   - Trong khi đó, các modal mới ở POS (`PosPaymentModal`, `PosCancelOrderModal`) lại tự viết modal dạng Tailwind `fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4 animate-fade-in`.
4. **`AppInput.razor`, `AppSelect.razor`, `AppTextarea.razor`:**
   - Đều phụ thuộc vào class `.form-control` của Bootstrap với border-radius 4px, không đồng bộ với các input bo tròn 12px (`rounded-xl`) hiện đại của Tailwind.
5. **`StatusBadge.razor`:**
   - Đã dùng Tailwind nhưng mapping màu còn sơ sài, thiếu các trạng thái đặc thù của nhà hàng (Đang chế biến, Đã huỷ, Sắp hết hàng, Chờ duyệt, v.v.).

---

## 4. CHIẾN LƯỢC TÁI CẤU TRÚC & KẾ HOẠCH HÀNH ĐỘNG

### Giai đoạn 2 — Thiết lập Design System Token & Chuẩn hóa CSS Toàn diện
- Cập nhật `App.razor` và `wwwroot/app.css`:
  - Đồng bộ bảng biến CSS `:root` và Tailwind Configuration.
  - Loại bỏ các quy tắc Bootstrap xung đột trực tiếp với reset của Tailwind.
  - Bổ sung các semantic token rõ ràng cho Status (Pending, Processing, Done, Cancelled, LowStock, SafeStock).
  - Chuẩn hóa typography scale và font số liệu (`font-mono`, `tabular-nums`).

### Giai đoạn 3 — Hiện đại hóa Bộ Component dùng chung (`Components/UI`)
- Cải tạo toàn diện:
  - `AppButton.razor`: Chuyển sang Tailwind thuần, hỗ trợ Material Symbols, các biến thể `Primary`, `Secondary`, `Outline`, `Ghost`, `Danger`, `Success`, các size `Sm`, `Md`, `Lg`, `Touch`.
  - `StatusBadge.razor`: Hỗ trợ đầy đủ các trạng thái đơn hàng, bàn, kho, thanh toán.
  - `SlaTimerBadge.razor`: Thay thế `bi-clock` bằng Material Symbol `schedule`, hiệu ứng pulse đỏ khi trễ SLA.
  - `AppModal.razor`: Chuẩn hóa cấu trúc Tailwind modal với backdrop blur, góc bo 16px, header/body/footer và animation mượt mà.
  - `AppCard.razor`: Thống nhất border, radius 16px, header title và shadow.
  - `DataTable.razor`: Viết lại với Tailwind, responsive wrapper, header xám nhạt hiện đại, hover mượt, empty state và loading state thanh lịch.
  - `AppInput.razor`, `AppSelect.razor`, `AppCheckbox.razor`, `AppToggle.razor`, `AppTextarea.razor`: Chuẩn hóa label, focus ring, helper text và error state.
  - `AppToast.razor`: Sử dụng Material Symbols và Tailwind snackbar hiện đại.

### Giai đoạn 4 — Chuẩn hóa Giao diện theo Module
- **Module Auth / Đăng nhập:**
  - `SharedLogin.razor`: Loại bỏ style inline thô, sử dụng Design System token, bàn phím số numpad hiện đại, phản hồi chạm mượt mà.
  - `AdminLogin.razor`: Đồng bộ input, focus ring và button đăng nhập.
- **Module POS:**
  - `PosHeader.razor`: Giữ vững SignalR realtime, đồng bộ các nút công cụ thu ngân, ngôn ngữ, chuông thông báo.
  - `PosDashboard.razor`: Đồng bộ 3 thẻ hành động chính và danh sách 10 thông báo.
  - `PosTables.razor`: Chuẩn hóa màu sắc trạng thái bàn, thanh công cụ Canvas/Grid zoom, các nút chuyển bàn / gộp bàn / thanh toán.
  - `PosMain.razor` & `OrderCart.razor`: Chuẩn hóa typography tiền tệ, phân cấp thông tin món chính / combo / modifier.
- **Module KDS:**
  - `KdsDashboard.razor`: Chuẩn hóa giao diện bếp nền tối (`#0F172A`), thẻ ticket `KdsTicketCard`, phân cấp món combo và nút "Nhận làm" / "Hoàn thành".
- **Module Admin / BOH:**
  - `AdminHeader.razor` & `AdminSidebar.razor`: Giữ vững liên kết điều hướng, chuẩn hóa hover/active states.
  - Các trang nghiệp vụ trọng điểm: `MaterialManagement.razor`, `MenuItemManagement.razor`, `ApprovalCenter.razor`, `Dashboard.razor`.

### Giai đoạn 5 — Kiểm thử, Đo lường & Nghiệm thu
- Biên dịch lại toàn bộ solution (`dotnet build src/Restaurant.sln`).
- Chạy toàn bộ 295 unit & integration tests để đảm bảo **0 regression**.
- Đánh giá khả năng hiển thị, responsive layout, độ tương phản và tính khả dụng thực tế.
