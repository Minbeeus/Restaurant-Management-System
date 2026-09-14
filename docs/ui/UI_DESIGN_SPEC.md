# HIỆN THỰC HOÁ TRẢI NGHIỆM NGƯỜI DÙNG: UI/UX DESIGN SPECIFICATION
**Project:** Hệ thống Restaurant POS & KDS
**Role:** Senior Lead UI/UX Designer
**Version:** 1.0

---

## 🎨 Part 1: Design System & Branding Guidelines (Mozzarella & Basil)

### 1.1. Color Palette (Bảng màu)
Hệ thống sử dụng bộ màu thương hiệu "Mozzarella & Basil", tạo cảm giác tự nhiên, sang trọng và ngon miệng.

#### 🟢 Brand & Core Colors
* **Primary (Basil Green):** `#486500` (Dùng cho các hành động chính, nút Complete, thương hiệu)
* **Primary Container:** `#5c8001` | **On Primary:** `#ffffff` | **On Primary Container:** `#fcffeb`
* **Secondary (Deep Sea Blue):** `#446371` (Dùng cho Sidebar, Header, khung cấu trúc)
* **Secondary Container:** `#c4e5f5` | **On Secondary:** `#ffffff` | **On Secondary Container:** `#486775`
* **Tertiary (Roasted Tomato):** `#ad2b19` (Dùng cho nút Danger, hủy đơn, xóa dữ liệu)
* **Tertiary Container:** `#cf442f` | **On Tertiary:** `#ffffff`

#### ⚪ Surface & Backgrounds ("Mozzarella White" Concept)
* **Background / Canvas:** `#f9f9ff` (Mozzarella White)
* **Surface Pure (Elevated Cards/Inputs):** `#ffffff` (Màu trắng tinh cho cards/inputs tương tác)
* **Surface Containers:**
  * `surface-container-low`: `#eff3ff`
  * `surface-container`: `#e6eeff`
  * `surface-container-high`: `#dee9fd`
* **Text / On Surface:** `#121c2a` (On-surface) | `#374151` (Charcoal Gray cho Body Data)
* **Borders / Outlines:** `#747967` (Outline) | `#c4c9b3` (Outline Variant) | `#e5e7eb` (Border Light)

#### 🚦 Functional & Status Badges
* **Success (Đã xong/Thành công):** BG `#DEF7EC` | Text `#03543F`
* **Alert / Danger (Lỗi/Trễ SLA/Đã hủy):** BG `#FDE8E8` | Text `#9B1C1C`
* **Warning / Pending (Chờ duyệt/Đang nấu):** BG `#FEF3C7` | Text `#92400E`

### 1.2. Typography (Kiểu chữ)
* **Headings (Tiêu đề - Montserrat, `#446371`):**
  * `display-lg`: Montserrat 32px, Bold (700), LineHeight 40px
  * `headline-md`: Montserrat 24px, SemiBold (600), LineHeight 32px
  * `headline-sm`: Montserrat 20px, SemiBold (600), LineHeight 28px
* **Body & Data (Nội dung - Inter):**
  * `body-lg`: Inter 16px, Regular (400), LineHeight 24px
  * `body-md`: Inter 14px, Regular (400), LineHeight 20px
  * `label-bold`: Inter 12px, SemiBold (600), LineHeight 16px, LetterSpacing 0.05em (Table Headers, Tags)

### 1.3. Components & Shapes
* **Border Radius:** Buttons/Inputs: `4px` | Containers/Cards: `8px` | Badges: `9999px` (Pill Shape).
* **Sidebar Layout:** Width `260px`, Background `#446371`, Nav items màu trắng 60% opacity. Item Active có thanh `4px` Basil Green (`#486500`) nằm dọc bên trái.
* **Feedback:** Tương tác chạm/click có hiệu ứng ripple hoặc opacity giảm mờ trong 0.15s.

---

## 📱 Part 2: Wireframe & Screen Specifications

### 2.1. Phân hệ BOH (Admin/Manager Workspace - Responsive Web)
**Cấu trúc Layout & Sidebar Navigation:**
Giao diện quản trị BOH sử dụng Layout cố định với Sidebar (Thanh điều hướng bên trái) chiếm `260px` chiều rộng, nền màu `Deep Sea Blue (#446371)`. Phần Content Area bên phải sẽ hiển thị nội dung chi tiết trên nền `Mozzarella White (#F9F9FF)`.

**Wireframe ASCII BOH Layout:**
```text
+-------------------------+-------------------------------------------------------+
| [Logo] Mozzarella &     | [Hambuger]  Content Area Title      [User: Admin v]   |
|        Basil            |                                                       |
|-------------------------| +---------------------------------------------------+ |
| BẢNG ĐIỀU KHIỂN         | | Data Table / Cards / Forms here...                | |
|  [ ] Tổng quan          | |                                                   | |
|                         | |                                                   | |
| QUẢN LÝ MÓN ĂN & MENU   | |                                                   | |
|  [ ] Danh mục món       | |                                                   | |
|  [ ] Danh sách món ăn   | |                                                   | |
|  [ ] Cấu hình Combo     | |                                                   | |
|  [ ] Tùy chọn & Topping | |                                                   | |
|                         | |                                                   | |
| QUẢN LÝ KHO & ĐỊNH LƯỢNG| |                                                   | |
|  [ ] Nguyên vật liệu    | |                                                   | |
|  [ ] C.thức định lượng  | |                                                   | |
|  [ ] Nhập kho           | |                                                   | |
|  [ ] Kiểm kê kho        | |                                                   | |
|                         | |                                                   | |
| QUẢN LÝ SẢNH & BÀN      | |                                                   | |
|  [ ] Sơ đồ sảnh & Bàn   | |                                                   | |
|  [ ] Quản lý mã QR      | |                                                   | |
|                         | |                                                   | |
| QUẢN LÝ KHÁCH HÀNG      | |                                                   | |
|  [ ] D.sách khách hàng  | |                                                   | |
|  [ ] Điểm & Hạng thẻ    | |                                                   | |
|                         | |                                                   | |
| BÁO CÁO & THỐNG KÊ      | |                                                   | |
|  [ ] Báo cáo doanh thu  | |                                                   | |
|  [ ] Món bán chạy       | |                                                   | |
|  [ ] Tiêu hao nguyên liệu||                                                   | |
|  [ ] Lịch sử kết ca     | |                                                   | |
|                         | |                                                   | |
| NHÂN VIÊN & HỆ THỐNG    | |                                                   | |
|  [ ] D.sách nhân viên   | |                                                   | |
|▌ [x] Phân quyền vai trò | | (Ví dụ trạng thái Active: Vạch màu Basil Green 4px) | |
|  [ ] Cấu hình hệ thống  | |                                                   | |
+-------------------------+-------------------------------------------------------+
```

* **Trạng thái Active (Đang chọn):** Mục con (Item) đang chọn sẽ sáng chữ màu trắng (`#FFFFFF`) và có 1 vạch dọc 4px màu `Basil Green (#486500)` ở mép trái.
* **Trạng thái Inactive (Mặc định):** Chữ màu xám mờ (`#C4E5F5` với 60% opacity).
* **Tiêu đề Khối (Blocks):** In hoa, sử dụng font `Inter label-bold` (12px, letter-spacing 0.05em). Cấu trúc các nhóm có thể thiết kế dưới dạng Accordion để thu gọn/mở rộng các mục con.

### 2.2. Phân hệ POS (Thu ngân / Bán hàng - Tablet/Desktop Touchscreen)
**Cấu trúc 2 cột (Layout 35/65)**
Màn hình POS được chia lưới tối ưu cho thao tác chạm nhanh:

```text
+------------------------------------+-----------------------------------------------------+
| Bàn: T1 - Cửa sổ (08:30) | N.Viên  | Danh mục (40%)        [Search Bar...]   [IMG: ON]   |
| #ORD-1025                          | +-------+ +-------+ +---------+ +-------+         |
| ---------------------------------- | | TẤT CẢ| | ĐỒ ĂN | | ĐỒ UỐNG | | COMBO |         |
| Món đã chọn:                       | +-------+ +-------+ +---------+ +-------+         |
|                                    | ------------------------------------------------- |
|  Pizza Hải Sản                250K | Món ăn (60% - Grid)                               |
|  [-] 1 [+]                         | +-------------+ +-------------+ +-------------+   |
|   ↳ Viền Phô Mai (50K)             | | Pizza Bò    | | Pizza HảiSản| | Combo G.Đình|   |
|   ↳ Đế Dày                         | | 200,000đ    | | 250,000đ    | | 499,000đ    |   |
|                                    | | (Image)     | | (Image)     | | (Image)     |   |
|  Coca Cola                     30K | +-------------+ +-------------+ +-------------+   |
|  [-] 2 [+]                         | +-------------+ +-------------+                   |
|   ↳ Đá riêng                       | | Trà Đào     | | Gà rán      |                   |
|                                    | | 50,000đ     | | (Xám mờ)    |                   |
|                                    | | (Image)     | | [HẾT MÓN]   |                   |
| ================================== | +-------------+ +-------------+                   |
| Tạm tính:                     310K |                                                   |
| Giảm giá (Loyalty):           -10K |                                                   |
| VAT (8%):                      24K |                                                   |
| TỔNG TIỀN: (Basil Green)      324K |                                                   |
|                                    |                                                   |
| [ Gửi Bếp ]        [Chuyển/Gộp Bàn]|                                                   |
| [ Tạm Tính]   [ THANH TOÁN (VietQR]|                                                   |
+------------------------------------+-----------------------------------------------------+
```

* **Cột Trái (35%):** Vùng Đơn hàng & Thanh toán. Hiển thị danh sách món với tùy chọn `[-] [+]` số lượng, Ghi chú và Topping thụt lề. TỔNG TIỀN in đậm bằng màu **Basil Green (`#486500`)**. Nút `[ THANH TOÁN ]` nổi bật nhất.
* **Cột Phải (65%):** Vùng Thực đơn.
  * *Phần trên (40%):* Thanh danh mục ngang dạng khối. Có công tắc bật/tắt hình ảnh món.
  * *Phần dưới (60%):* Lưới món ăn nền `Mozzarella White (#F9F9FF)`. Tên món `Charcoal Gray (#374151)`, Giá tiền `Basil Green (#486500)`. Món nào hết (`IsSoldOut`) sẽ bị xám mờ.

**Workflow: Đặt món Combo (Combo Builder)**
Khi thu ngân chạm vào 1 gói "Combo" (ví dụ: Combo Gia Đình), hệ thống không ném ngay vào Order List mà mở một Modal ở giữa màn hình:

```text
+---------------------------------------------------------+
| [Popup] Combo Builder: Combo Gia Đình                   |
|---------------------------------------------------------|
| Số lượng gói Combo:  [-] 1 [+]                          |
|                                                         |
| 1. Nhóm Món Chính (Chọn 1 - Bắt buộc)                   |
|   (o) Pizza Hải Sản Size L                              |
|   ( ) Pizza Bò Size L                                   |
|                                                         |
| 2. Nhóm Đồ Uống (Chọn 2 - Bắt buộc)                     |
|   [x] Coca Cola                                         |
|   [x] Sprite                                            |
|   [ ] Trà Đào                                           |
|                                                         |
| 3. Nhóm Ăn Kèm (Chọn 1 - Tùy chọn)                      |
|   ( ) Khoai tây chiên                                   |
|   ( ) Salad cá ngừ                                      |
|                                                         |
| [ HỦY ]                       [ XÁC NHẬN THÊM COMBO ]   |
|                       (Nút này sáng lên khi chọn đủ món)|
+---------------------------------------------------------+
```

### 2.3. Phân hệ KDS (Bếp/Bar - Tablet Dark Mode Display)
**Cấu trúc Theme:** Bắt buộc Dark Mode (Background: `#121C2A`, Card Surface: `#273140`). Tối ưu hiển thị ngang (16:9), khoảng cách nhìn xa.

**Topbar KDS & Công cụ:**
```text
+-----------------------------------------------------------------------------------------+
| [KDS] Ca Sáng | Đang chờ: 5 Đơn               [ Thẻ Đơn | Gom Món ]  [ BÁO HẾT MÓN 🔴 ] |
+-----------------------------------------------------------------------------------------+
```

**1. Màn hình Thẻ Đơn (Ticket View) - Lưới ngang cuộn mượt:**
```text
  +-----------------------+ +-----------------------+ +-----------------------+
  | T1 - Cửa sổ   [ 08m ] | | T2 - VIP      [ 12m ] | | T3 - Sảnh     [ 16m ] |
  | #ORD-101 (2 Khách)    | | #ORD-102 (4 Khách)    | | #ORD-103 (2 Khách)    |
  | --------------------- | | --------------------- | | --------------------- |
  |  x2 Burger bò         | |  x1 Pizza Hải Sản     | |  x1 Mì Ý Carbonara    |
  |     KHÔNG HÀNH        | |     THÊM PHÔ MAI      | |                       |
  |  x1 Khoai tây chiên   | |  x2 Trà Đào           | |  x1 Nước Suối         |
  |                       | |                       | |                       |
  | [ NHẬN MÓN ]  [ XONG ]| | [ NHẬN MÓN ]  [ XONG ]| | [ NHẬN MÓN ]  [ XONG ]|
  +-----------------------+ +-----------------------+ +-----------------------+
  (Viền Trắng/Xanh nhạt)    (Viền Vàng - Cảnh báo)    (Viền Đỏ nhấp nháy - Trễ!)
```
* **Thành phần Ticket:** Tên Bàn (`Montserrat`), Mã Đơn. Bộ đếm SLA (`SlaTimerBadge`).
* **Món ăn:** Số lượng (`x1, x2`) in to màu `Basil Green (#ABD558)` sáng. Tên món màu Trắng (`#FFFFFF`). Ghi chú in hoa màu Vàng (`#FEF3C7`).
* **SLA Alert:** `< 10m` (Bình thường), `10-15m` (Viền thẻ Vàng `#FEF3C7`), `> 15m` (Viền thẻ Đỏ `#AD2B19` nhấp nháy/pulse + Âm thanh ting).
* **Tương tác Nút:** 
  - `[ NHẬN MÓN ]` -> Đổi trạng thái `Processing`.
  - `[ XONG ]` -> Đổi trạng thái `Done` (Bắn notification cho Thu ngân), thẻ mờ dần và trượt khỏi màn hình.

**2. Màn hình Gom Món (Aggregate View):**
Dùng khi bếp bị dồn đơn (ví dụ: cần làm 10 cái Pizza cùng lúc).
```text
+-----------------------------------------------------------------------------------------+
| Gom Món Đang Chờ (Sắp xếp theo số lượng nhiều nhất)                                     |
|-----------------------------------------------------------------------------------------|
|  [ Pizza Hải Sản ] .............. TỔNG CỘNG: 8 Phần                                     |
|   ↳ Bàn T2 (1), Bàn T4 (3), Bàn T5 (2), Khách mang về (2)                               |
|-----------------------------------------------------------------------------------------|
|  [ Trà Đào Cam Sả] .............. TỔNG CỘNG: 5 Phần                                     |
|   ↳ Bàn T2 (2), Bàn T3 (3)                                                              |
+-----------------------------------------------------------------------------------------+
```

**Tương tác SignalR Realtime (KDS <-> POS):**
- **`ReceiveKitchenTicket`**: Khi POS bấm "Gửi Bếp", thẻ mới lập tức chèn vào lưới KDS kèm âm báo.
- **`ReceiveItemStatusUpdate`**: Khi KDS bấm "Nhận món" hoặc "Xong", Màn hình POS (đang mở chi tiết bàn đó) tự động cập nhật tick xanh cho từng món để thu ngân/phục vụ biết có thể đi bưng đồ.
- **`ReceiveKitchenSlaWarning`**: Event bắn từ Background Job nếu có đơn >15p, cả KDS và POS đều nhận cảnh báo đỏ.

### 2.4. Phân hệ QR Ordering (Khách hàng - Mobile First Web)
**Màn hình Thực đơn & Bottom Cart (Mobile View)**
```text
+--------------------+
| 12:45        [LTE] |
| T1 - Chào bạn!     |
| [Search Menu...]   |
|--------------------|
| [All] [Food] [Drink] 
|--------------------|
| Burger Bò          |
| 150,000đ           |
| (Image)  [+ THÊM]  |
|--------------------|
| Trà Đào Cam Sả     |
| 50,000đ            |
| (Image)  [+ THÊM]  |
|                    |
|                    |
|====================|
| 🛒 3 món | 200K    |
| [ GỬI ĐƠN NGAY ]   | (Thanh Sticky Bottom)
+--------------------+
```
**Mô tả Drawer chọn Topping:** Khi bấm [+ THÊM], một Bottom Drawer trượt lên từ đáy màn hình để chọn Size (M/L) và Topping, dễ dàng với tới bằng ngón cái.

---

## 🔄 Part 3: User Journey & Micro-interactions

### 3.1. Core User Flow (Luồng Đặt Món QR -> KDS -> Thanh Toán)
1. **Khách hàng (Mobile):** Quét QR -> Web load tức thì (Menu Cache) -> Khách lướt, thêm món, chọn Topping (Drawer trượt lên) -> Bấm Gửi Đơn -> Nhận thông báo "Vui lòng chờ nhân viên xác nhận".
2. **Thu ngân (POS):** Bàn T1 trên màn hình nhấp nháy Vàng (Audio ping "Ting ting" phát ra). Thu ngân chạm vào bàn -> Popup hiển thị chi tiết món -> Bấm "Duyệt Đơn".
3. **Bếp (KDS):** Ticket tự động nhảy ra màn hình KDS với hiệu ứng slide-in. Thời gian đếm ngược bắt đầu. Nếu quá 15p, viền ticket chuyển Đỏ kèm nhịp đập (pulse animation). Đầu bếp làm xong, chạm tay vào món để gạch ngang (Strikethrough).
4. **Thanh toán (POS):** Khách báo tính tiền. Thu ngân chọn bàn -> Sinh mã VietQR -> Khách quét chuyển khoản -> Webhook SePay ghi nhận -> Bàn chuyển sang 🟢 Xanh.

### 3.2. Micro-interactions (Hiệu ứng vi mô)
* **Visual SLA Alert (KDS):** CSS Animation `keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(230, 57, 70, 0.7); } 70% { box-shadow: 0 0 0 10px rgba(230, 57, 70, 0); } }`.
* **Sticky Cart (Mobile):** Khi khách thêm món, icon giỏ hàng nảy lên (Bounce) và rung nhẹ (Haptic feedback) thông qua Web Vibrate API `navigator.vibrate(50)`.
* **Toast Notification (BOH/POS):** Xóa bảng Toast cồng kềnh, dùng Snackbar nhỏ gọn bo góc hiện dưới cùng bên trái màn hình tự biến mất sau 3 giây.
* **Loading Skeletons:** Khi chuyển trang hoặc tải menu, thay vì dùng Spinner nhàm chán, hiển thị khung xương (Skeleton) nhấp nháy nhẹ để tạo cảm giác tốc độ tải nhanh hơn.

---

## 🔑 Part 4: Authentication & Login Interfaces

Thiết kế giao diện Đăng nhập được đồng bộ hoàn toàn với cấu trúc DTOs (`LoginRequest` và `QuickLoginRequest`) từ Backend API (`/api/v1/auth/login` và `/api/v1/auth/quick-login`).

### 4.1. Phân hệ BOH (Admin/Manager Workspace - Web Login)
Sử dụng luồng Đăng nhập tiêu chuẩn (`LoginRequest`) yêu cầu `Username` và `Password`. Giao diện được thiết kế dạng Split Screen, vừa đảm bảo bảo mật vừa mang tính thẩm mỹ cao.

**Cấu trúc Layout (Split Screen 50/50):**
* **Nửa trái (Branding):** Hiển thị hình ảnh chất lượng cao của nhà hàng. Có overlay mờ nhẹ để chèn slogan.
* **Nửa phải (Form):** Nền `Mozzarella White (#F9F9FF)`, form đăng nhập căn giữa, ánh xạ trực tiếp các trường dữ liệu.

**Wireframe ASCII BOH Login:**
```text
+------------------------------------+------------------------------------+
| [ Image / Branding Area ]          |                                    |
| (Hình ảnh món ăn đặc trưng,        |      [Logo Mozzarella & Basil]     |
|  màu sắc hấp dẫn)                  |                                    |
|                                    |  Đăng nhập hệ thống Quản trị       |
|                                    |                                    |
|  "Mang trải nghiệm ẩm thực         |  Tên đăng nhập (Username)          |
|   đỉnh cao đến khách hàng"         |  [______________________________]  |
|                                    |                                    |
|                                    |  Mật khẩu (Password)               |
|                                    |  [__________________________][👁] |
|                                    |                                    |
|                                    |  [ ] Ghi nhớ đăng nhập   Quên MK?  |
|                                    |                                    |
|                                    |  [       ĐĂNG NHẬP (Basil Green)]  |
|                                    |                                    |
+------------------------------------+------------------------------------+
```
* **Binding Model:** Input 1 bám vào `request.Username`, Input 2 bám vào `request.Password`.
* **API Gọi:** `POST /api/v1/auth/login`

### 4.2. Phân hệ POS / KDS (Thu ngân / Bếp - Tablet Touchscreen Login)
Do tính chất đổi ca thường xuyên, màn hình cảm ứng sẽ sử dụng tính năng **Quick Login** (`QuickLoginRequest`), yêu cầu nhân viên nhập mã truy cập nhanh (`AccessCode`).

**Cấu trúc Layout (Full Screen Access Code Pad):**
* Nền: `Mozzarella White (#F9F9FF)` cho POS hoặc `Dark Mode (#121C2A)` cho KDS.
* Không có thanh cuộn, giao diện nhập Numpad cỡ lớn ở trung tâm.

**Wireframe ASCII POS/KDS Quick Login:**
```text
+-----------------------------------------------------------------------+
|                                                                       |
|                          [Logo Mozzarella & Basil]                    |
|                                                                       |
|                     Nhập Mã Truy Cập Nhanh (Access Code)              |
|                                                                       |
|                             [ * ] [ * ] [ * ] [ _ ]                   |
|                                                                       |
|                            +---+ +---+ +---+                          |
|                            | 1 | | 2 | | 3 |                          |
|                            +---+ +---+ +---+                          |
|                            | 4 | | 5 | | 6 |                          |
|                            +---+ +---+ +---+                          |
|                            | 7 | | 8 | | 9 |                          |
|                            +---+ +---+ +---+                          |
|                            | C | | 0 | | < |                          |
|                            +---+ +---+ +---+                          |
|                                                                       |
+-----------------------------------------------------------------------+
```
* **Binding Model:** Chuỗi kí tự từ bàn phím số map trực tiếp với `request.AccessCode`.
* **API Gọi:** `POST /api/v1/auth/quick-login`
* **Micro-interactions:**
  - **Tự động gửi (Auto-submit):** Khi nhập đủ độ dài quy định của AccessCode, hệ thống sẽ tự trigger API mà không cần bấm Enter.
  - **Lỗi sai Code:** Các ô vuông chứa `[ * ]` rung nhẹ (Shake animation) và chuyển đỏ `Tertiary (#ad2b19)`, tự động clear giá trị để người dùng nhập lại nhanh chóng.
