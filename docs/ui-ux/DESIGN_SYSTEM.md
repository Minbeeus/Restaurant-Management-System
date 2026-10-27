# QUY CHUẨN THIẾT KẾ ĐỒNG BỘ TOÀN HỆ THỐNG (DESIGN SYSTEM SPECIFICATION)

**Dự án:** Hệ thống Quản trị & Vận hành Nhà hàng POS / KDS / BOH / Admin / QR  
**Thương hiệu:** Mozzarella & Basil  
**Phiên bản:** 2.0 (Standardized Architecture)  
**Tài liệu:** `docs/ui-ux/DESIGN_SYSTEM.md`  

---

## 1. NGUYÊN TẮC THIẾT KẾ CỐT LÕI (DESIGN PRINCIPLES)

1. **Tối ưu cho Vận hành Thực tế (Ergonomics First):**
   - Môi trường nhà hàng đòi hỏi thao tác nhanh, chính xác dưới áp lực giờ cao điểm (Peak hours).
   - Mọi nút bấm tác vụ chính trên POS và KDS phải đạt kích thước tối thiểu **44px × 44px** để dễ dàng chạm bằng ngón tay trên màn hình cảm ứng hoặc click chuột nhanh.
2. **Phân cấp Thị giác Rõ ràng & Giảm Thiểu Tải Nhận Thức (Cognitive Load Reduction):**
   - Tránh trang trí rườm rà, hạn chế đổ bóng nặng hạt hoặc gradient gây mỏi mắt sau nhiều giờ làm việc.
   - Các hành động nguy hiểm (Hủy đơn, Xóa món, Trả hàng, Void item, Đóng ca chênh lệch) phải mang màu sắc nhận diện riêng biệt (`Tertiary / Danger`) và bắt buộc có bước xác nhận rõ ràng.
3. **Tính Nhất quán Tuyệt đối (Consistency across Workspaces):**
   - Cùng một trạng thái nghiệp vụ (ví dụ: Bàn đang có khách, Đơn hàng đang nấu, Nguyên liệu sắp hết) phải có cùng mã màu, cùng badge và icon tương ứng dù hiển thị ở POS, KDS, Admin hay Báo cáo.
4. **Hỗ trợ Số liệu & Tiền tệ Dễ Đọc (Tabular Figures & Currency Scannability):**
   - Giá tiền và số lượng luôn sử dụng font số đồng dạng (`tabular-nums` / `font-mono`) căn lề phải trong các bảng biểu và giỏ hàng, giúp thu ngân và quản lý quét mắt so sánh số liệu ngay lập tức.

---

## 2. HỆ THỐNG MÀU SẮC & TOKEN NGỮ NGHĨA (COLOR TOKENS)

### 2.1. Bảng màu Nhận diện Thương hiệu (Brand Colors)
| Token Name | Hex Code | Tên màu | Mục đích sử dụng |
|---|---|---|---|
| `primary` | `#486500` | Basil Green | Hành động chính, nút hoàn thành, nút lưu, nhận diện thương hiệu |
| `primary-hover` | `#3D5600` | Basil Green Dark | Trạng thái hover của các nút chính |
| `primary-container`| `#5C8001` | Basil Light | Thẻ active, thanh điều hướng phụ, badge nổi bật |
| `on-primary` | `#FFFFFF` | Pure White | Màu chữ trên nền Primary |
| `secondary` | `#446371` | Deep Sea Blue | Khung sườn, Sidebar, Topbar, tiêu đề nhóm quản trị |
| `secondary-container`| `#C4E5F5` | Sea Mist Blue | Nền khu vực phụ, trạng thái Đang tính tiền (Billing) |
| `tertiary` / `error` | `#AD2B19` | Roasted Tomato | Nút Danger, hành động hủy, xóa, cảnh báo rủi ro cao |
| `error-container` | `#FDE8E8` | Soft Pink/Red | Nền thông báo lỗi, nền badge trạng thái Sắp hết hàng/Trễ SLA |

### 2.2. Màu Nền & Mặt phẳng (Surfaces & Backgrounds)
| Token Name | Light Mode Hex | Dark Mode Hex (KDS) | Mục đích sử dụng |
|---|---|---|---|
| `background` | `#F8F9FF` (Mozzarella White) | `#0F172A` (Slate 900) | Nền tổng thể của toàn bộ ứng dụng |
| `surface` | `#FFFFFF` (Pure White) | `#1E293B` (Slate 800) | Mặt phẳng thẻ Card, Hộp thoại Modal, Khung bảng |
| `surface-low` | `#EFF3FF` | `#162032` | Nền khu vực phụ, nền danh mục món |
| `surface-container` | `#E6EEFF` | `#1E293B` | Nền input, nút bấm phụ, hover background |
| `surface-high` | `#DEE9FD` | `#334155` (Slate 700) | Nền toolbar, ô tìm kiếm, active tab |

### 2.3. Màu Chữ & Đường Viền (Text & Borders)
| Token Name | Light Mode | Dark Mode (KDS) | Mục đích |
|---|---|---|---|
| `text-primary` / `on-surface` | `#121C2A` (Charcoal) | `#F8FAFC` | Tiêu đề, số tiền chính, nội dung trọng tâm |
| `text-secondary` | `#374151` (Medium Gray) | `#CBD5E1` | Nhãn form, mô tả món ăn, nội dung bảng |
| `text-muted` | `#747967` / `#6B7280` | `#94A3B8` | Chú thích phụ, timestamp, mã ID |
| `border-main` | `#E2E8F0` / `#E5E7EB` | `#334155` | Đường viền thẻ card, phân chia hàng trong bảng |
| `border-variant` | `#C4C9B3` / `#CBD5E1` | `#475569` | Viền input, viền dropdown, vách ngăn toolbar |

### 2.4. Bảng Ánh xạ Trạng thái Ngữ nghĩa (Semantic Status Mapping)
Hệ thống sử dụng bảng màu trạng thái thống nhất cho toàn bộ các phân hệ POS, KDS, Kho và BOH:

| Trạng thái Nghiệp vụ | Loại Badge (`StatusBadge`) | Màu Nền (BG) | Màu Chữ (Text) | Màu Viền |
|---|---|---|---|---|
| **Bàn Trống / Hoàn tất / Còn hàng / Đã duyệt** | `Success` | `#DEF7EC` | `#03543F` | `#31C48D` |
| **Chờ phục vụ / Đang làm / Chờ duyệt / Mới gọi** | `Warning` | `#FEF3C7` | `#92400E` | `#F59E0B` |
| **Bàn có khách / Trễ SLA / Sắp hết hàng / Đã hủy** | `Danger` / `Alert` | `#FDE8E8` | `#9B1C1C` | `#F87171` |
| **Đang tính tiền / Thông tin / Tạm dừng** | `Info` | `#E0F2FE` | `#0369A1` | `#38BDF8` |
| **Đã ẩn / Không hoạt động / Mặc định** | `Neutral` | `#F1F5F9` | `#475569` | `#CBD5E1` |

---

## 3. QUY CHUẨN TYPOGRAPHY (TYPOGRAPHY SPECIFICATION)

### 3.1. Font Families
- **Heading Font:** `'Montserrat', sans-serif` — Áp dụng cho các tiêu đề chính, tên thương hiệu, tiêu đề trang và modal.
- **Body & Controls Font:** `'Inter', sans-serif` — Áp dụng cho toàn bộ văn bản đọc, form labels, nút bấm, bảng dữ liệu.
- **Numbers & Monetary Font:** `'Inter', monospace` với thuộc tính `font-variant-numeric: tabular-nums;` — Áp dụng cho số tiền, số lượng, mã đơn hàng.

### 3.2. Bảng Tỉ lệ Cỡ chữ (Type Scale)
| Tên Cấp bậc | Font Family | Size / Line-height | Font Weight | Phạm vi ứng dụng |
|---|---|---|---|---|
| `display-lg` | Montserrat | 32px / 40px | 700 (Bold) | Trang đăng nhập, banner chào đón lớn |
| `headline-md`| Montserrat | 24px / 32px | 700 (Bold) | Tiêu đề trang chính (Page Title) |
| `headline-sm`| Montserrat | 20px / 28px | 600 (SemiBold) | Tiêu đề card lớn, tiêu đề Modal, Section Header |
| `title-md` | Montserrat | 16px / 24px | 600 (SemiBold) | Tiêu đề widget, tên món trong thực đơn |
| `body-lg` | Inter | 16px / 24px | 400 (Regular) | Nội dung giới thiệu, thông báo lớn |
| `body-md` | Inter | 14px / 20px | 400 / 500 | Nội dung bảng, mô tả, văn bản biểu mẫu chuẩn |
| `body-sm` | Inter | 12px / 16px | 400 / 500 | Helper text, mô tả phụ, thông tin thời gian |
| `label-bold` | Inter | 12px / 16px | 600 (SemiBold) | Tiêu đề cột bảng (Uppercase), Tag, Badge |
| `label-caps` | Inter | 11px / 16px | 700 (Bold) | Nhãn danh mục nhỏ, chip trạng thái nén |

### 3.3. Quy chuẩn Hiển thị Số tiền và Số lượng
- Tiền tệ: Luôn dùng dấu phẩy `,` phân cách hàng nghìn và hậu tố `đ` (ví dụ: `250,000 đ`).
- Số lượng trong order: Ký hiệu tiền tố `x` hoặc hậu tố `x` in đậm (ví dụ: `2x` hoặc `x2`), màu tương phản cao.

---

## 4. HỆ THỐNG KHOẢNG CÁCH, BO GÓC & ĐỔ BÓNG (SPACING, RADIUS & ELEVATION)

### 4.1. Thang Spacing (Bội số của 4px)
- `gap-1` / `p-1`: 4px — Khoảng cách icon nhỏ với nhãn chữ.
- `gap-2` / `p-2`: 8px — Khoảng cách giữa các chip/badge, nút công cụ thu nhỏ.
- `gap-3` / `p-3`: 12px — Padding trong các hàng bảng, ô thẻ món thu gọn.
- `gap-4` / `p-4`: 16px — Padding tiêu chuẩn của Card, Toolbar, Dialog header.
- `gap-6` / `p-6`: 24px — Khoảng cách giữa các khối chính (Grid gap), padding trang.
- `gap-8` / `p-8`: 32px — Khoảng cách phân cách giữa các phân đoạn nội dung lớn.

### 4.2. Quy chuẩn Bo góc (Border Radius Scale)
- **`rounded-lg` (8px):** Nút hành động trong bảng (Edit/Delete icon buttons), ô checkbox, dropdown menu nhỏ.
- **`rounded-xl` (12px):** Nút bấm chính (Button), Ô nhập liệu (Input/Select/Textarea), Thẻ món ăn (Menu Card).
- **`rounded-2xl` (16px):** Thẻ Card tổng quan (AppCard), Bảng dữ liệu (DataTable wrapper), Thẻ bàn ăn (TableCard), Hộp thoại Modal (AppModal).
- **`rounded-full` (9999px):** Badge trạng thái, hình đại diện Avatar, nút tròn hành động nổi (FAB).

### 4.3. Đổ bóng (Elevation & Shadows)
- `shadow-2xs`: `0 1px 2px rgba(18, 28, 42, 0.04)` — Viền nhẹ cho các card phẳng trên nền surface-low.
- `shadow-sm`: `0 2px 4px rgba(18, 28, 42, 0.06)` — Thẻ tương tác khi rê chuột (hover).
- `shadow-lg`: `0 10px 15px -3px rgba(18, 28, 42, 0.08)` — Menu Dropdown thả xuống, Popover.
- `shadow-2xl`: `0 25px 50px -12px rgba(18, 28, 42, 0.25)` — Cửa sổ Modal trung tâm.

---

## 5. HỆ THỐNG BIỂU TƯỢNG (ICONOGRAPHY GUIDELINES)

- Thư viện duy nhất được phép sử dụng: **Google Material Symbols Outlined**.
- Cú pháp chuẩn: `<span class="material-symbols-outlined [classes]" data-icon="icon_name">icon_name</span>`.
- Kích thước chuẩn:
  - Inline với chữ: `text-[16px]` hoặc `text-base`
  - Nút bấm tiêu chuẩn: `text-[18px]` hoặc `text-[20px]`
  - Biểu tượng tiêu đề khối / Card: `text-2xl` (24px)
  - Empty state / Biểu tượng minh hoạ: `text-4xl` (36px - 40px)
- Tuyệt đối **không** dùng emoji để thay thế biểu tượng nghiệp vụ.

---

## 6. QUY TẮC COMPONENT DÙNG CHUNG (SHARED COMPONENTS CONTRACTS)

### 6.1. `AppButton`
- **Variants:** `Primary` (xanh lá basil), `Secondary` (xanh sea blue hoặc xám surface), `Outline` (viền mảnh), `Ghost` (trong suốt, hover sáng), `Danger` (đỏ tomato), `Success` (xanh ngọc).
- **Sizes:** `Sm` (chiều cao 32px), `Md` (chiều cao 40px), `Lg` (chiều cao 48px), `Touch` (chiều cao 54px cho POS/KDS).
- **Features:** Hỗ trợ `IsLoading` (spinner quay mượt, tự disabled), `Icon` (Material Symbol), `Disabled`.

### 6.2. `AppInput`, `AppSelect`, `AppTextarea`
- Đồng bộ góc bo `rounded-xl` (12px), viền mảnh `border-outline-variant/40`.
- Hiệu ứng focus: `focus:ring-2 focus:ring-primary/40 focus:border-primary focus:outline-none`.
- Hỗ trợ rõ ràng `Label`, `Placeholder`, `ErrorMessage` (tự động bật viền đỏ và text đỏ cảnh báo).

### 6.3. `AppModal`
- Cấu trúc: Khung mờ nền `fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4 animate-fade-in`.
- Khối nội dung: `bg-surface rounded-2xl shadow-2xl border border-outline-variant max-w-lg w-full overflow-hidden`.
- Header có tiêu đề rõ ràng và nút đóng ✕ bo tròn; Footer phân cấp rõ nút Hủy (`Secondary`/`Outline`) và nút Xác nhận (`Primary`/`Danger`).

### 6.4. `DataTable`
- Khung bọc `bg-surface rounded-2xl border border-outline-variant/30 shadow-sm overflow-hidden`.
- Header bảng: `bg-surface-container/50 border-b border-outline-variant/30 text-xs font-bold text-on-surface-variant uppercase tracking-wider py-4 px-5`.
- Hàng dữ liệu: Hover nhẹ `hover:bg-surface-container/20 transition-colors`.
- Cột số tiền/số lượng: Căn lề phải `text-right font-mono font-bold`.
- Empty state: Hiển thị icon trống và thông điệp lịch sự khi không có bản ghi.

### 6.5. `StatusBadge` & `SlaTimerBadge`
- Sử dụng hình dạng con nhộng `rounded-full px-3 py-1 text-xs font-bold inline-flex items-center gap-1.5`.
- Tự động map màu theo Semantic Status Mapping ở Mục 2.4.
- `SlaTimerBadge`: Đếm thời gian realtime, tự động kích hoạt animation `pulse-red` khi vượt ngưỡng cảnh báo SLA (15 phút).

---

## 7. QUY CHUẨN ĐẶC THÙ THEO PHÂN HỆ (MODULE-SPECIFIC GUIDELINES)

### 7.1. Phân hệ POS (Thu ngân / Bán hàng)
- Giữ vững cấu trúc **30% Giỏ hàng bên trái / 70% Thực đơn & Thao tác bên phải**.
- Giỏ hàng (`OrderCart`): Cố định không bị co kéo, danh sách món cuộn độc lập, tổng tiền hiển thị to rõ bằng màu Basil Green ở đáy màn hình.
- Thẻ bàn ăn (`TableCard`): Trạng thái hiển thị tức thì qua màu viền và badge (Xanh = Trống, Đỏ = Có khách, Vàng = Đơn mới, Xanh dương = Chờ thanh toán).

### 7.2. Phân hệ KDS (Màn hình Bếp / Pha chế)
- Chế độ **Dark Mode bắt buộc** (`bg-slate-900`) để giảm chói mắt cho đầu bếp trong điều kiện ánh sáng nhà bếp.
- Vé bếp (`KdsTicketCard`): Cột vé rộng tối thiểu 320px, cuộn ngang mượt mà.
- Món Combo: Hiển thị khối Combo cha màu hổ phách/vàng, thụt lề rõ ràng các món thành phần con bên trong.

### 7.3. Phân hệ BOH & Admin Quản trị
- Cấu trúc Layout cố định: Sidebar `260px` bên trái, Topbar `64px` ở trên, Content Canvas cuộn dọc độc lập.
- Tất cả các trang quản trị phải tuân thủ bố cục chuẩn:
  1. **Page Header:** Tiêu đề trang + Mô tả nghiệp vụ ngắn + Nút hành động chính (Thêm mới).
  2. **Filter Toolbar:** Ô tìm kiếm tức thì ở bên trái + Các bộ lọc danh mục/trạng thái ở bên phải.
  3. **Data Area:** Bảng dữ liệu hoặc lưới thẻ thống nhất.
  4. **Form View:** Ưu tiên chuyển đổi sang giao diện thêm/sửa full-page sạch sẽ hoặc modal chuẩn, tránh các form chắp vá.

### 7.4. Phân hệ QR Ordering (Khách hàng)
- Tối ưu 100% cho màn hình di động (Mobile First).
- Giỏ hàng nổi (FAB) bám góc dưới bên phải màn hình với bộ đếm số lượng nảy sinh động khi thêm món.
- Tùy chọn Topping hiển thị dạng Bottom Drawer trượt từ dưới lên, chạm dễ dàng bằng ngón tay cái.
