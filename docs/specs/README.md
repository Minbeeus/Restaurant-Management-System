# Spec Kit — Hệ thống Quản lý Nhà hàng Full Web (POS, KDS, BOH & QR Ordering)

> **Dự án:** Hệ thống Quản lý Nhà hàng Đa phân hệ  
> **Công nghệ:** .NET 9 · ASP.NET Core WebAPI · Blazor Server · Entity Framework Core 9 · SignalR · SQL Server · SePay  
> **Phiên bản:** 2.0 (Cập nhật đồng bộ theo mã nguồn thực tế)  
> **Ngày cập nhật:** 2026-10-30  

---

## 1. Tổng quan Spec Kit

Spec Kit là bộ tài liệu đặc tả chức năng chi tiết theo từng phân hệ nghiệp vụ của hệ thống, được chuẩn hóa và đồng bộ 100% với cấu trúc mã nguồn thực tế trong solution (`Restaurant.sln`).

Mỗi tính năng (Feature) được chuẩn hóa theo cấu trúc:
- **Feature ID**: Mã định danh (POS-xxx, KDS-xxx, BOH-xxx, QR-xxx, ACC-xxx)
- **Actor**: Vai trò thao tác (Cashier, Chef, Admin, Manager, Stockkeeper, Customer)
- **Độ ưu tiên**: Must Have / Should Have / Nice to Have
- **Mô tả & Luồng thao tác**: Quy trình nghiệp vụ chi tiết
- **Tiêu chí chấp nhận (Acceptance Criteria)**: Tiêu chuẩn kiểm thử Given - When - Then
- **Bảng dữ liệu & Sự kiện SignalR**: Các thực thể EF Core và Hub events liên quan

---

## 2. Danh mục Phân hệ & Thống kê Feature

| # | Phân hệ | File Spec | Số Feature | Actor chính | Mô tả phạm vi |
|---|---------|-----------|:----------:|-------------|---------------|
| 1 | **POS** (Point of Sale) | [POS-spec.md](./POS-spec.md) | **21** | Cashier, Manager | Sơ đồ bàn, POS Mang đi, Bán hàng, Đổi điểm, Hủy món/đơn, In K80, Thanh toán tiền mặt & VietQR/SePay, Quản lý két |
| 2 | **KDS** (Kitchen Display System) | [KDS-spec.md](./KDS-spec.md) | **7** | Chef, Bartender | Màn hình bếp realtime, phân lọc theo trạm bếp (Kitchen Stations), nhận/hoàn thành món, trừ kho BOM, cảnh báo trễ SLA |
| 3 | **BOH** (Back of House / Admin) | [BOH-spec.md](./BOH-spec.md) | **24** | Admin, Manager, Stockkeeper | Quản lý Thực đơn/Combo/BOM, Kho hàng & Kiểm kê, Approval Center, Đối soát thanh toán, Báo cáo doanh thu/hao hụt, Cấu hình hệ thống & Máy POS |
| 4 | **QR Ordering** | [QR-spec.md](./QR-spec.md) | **8** | Customer (Guest) | Khách tự quét mã tại bàn, gọi món/combo/topping, gửi đơn lên POS, theo dõi tiến độ nấu món realtime, chống spam |
| 5 | **Account & Loyalty** | [ACC-spec.md](./ACC-spec.md) | **10** | All Staff, Admin, Cashier | Đăng nhập JWT/Cookie, PIN Quản lý BCrypt, Tích điểm tự động, Chiết khấu theo hạng thẻ, Đổi điểm lấy món thưởng, Rollback giao dịch |

👉 **Tổng cộng: 70 Feature Specifications**

---

## 3. Ma trận Phân quyền Vai trò (RBAC Matrix)

| Nhóm Nghiệp Vụ / Chức năng | Admin | Manager | Cashier | Chef | Stockkeeper | Customer |
|-----------------------------|:-----:|:-------:|:-------:|:----:|:-----------:|:--------:|
| **POS — Sơ đồ bàn & Gọi món tại bàn** | ✅ | ✅ | ✅ | | | |
| **POS — Bán hàng Mang đi (Takeaway)** | ✅ | ✅ | ✅ | | | |
| **POS — Duyệt đơn QR & Báo chế biến** | ✅ | ✅ | ✅ | | | |
| **POS — Thu tiền mặt & VietQR tự động** | ✅ | ✅ | ✅ | | | |
| **POS — Khai báo Mở ca, Két tiền, Kết ca K80** | ✅ | ✅ | ✅ | | | |
| **POS — Nhập mã PIN Quản lý ủy quyền tại quầy** | ✅ | ✅ | ⚙️ | | | |
| **KDS — Nhận làm món, Hoàn thành món** | | | | ✅ | | |
| **KDS — Báo hết món (Sold Out) realtime** | ✅ | ⚙️ | | ✅ | | |
| **KDS — Lọc món theo Trạm bếp (Stations)** | | | | ✅ | | |
| **BOH — Quản lý Menu, Combo, Modifiers, Bàn** | ✅ | ✅ | | | | |
| **BOH — Cấu hình Định lượng BOM & Đơn vị tính** | ✅ | ⚙️ | | | | |
| **BOH — Nhập kho nguyên liệu, Đơn vị cung cấp** | ✅ | ✅ | | | ✅ | |
| **BOH — Kiểm kê kho & Cân bằng tồn kho** | ✅ | ✅ | | | ✅ | |
| **BOH — Trung tâm phê duyệt (Approval Center)** | ✅ | ✅ | ⚙️ | | | |
| **BOH — Đối soát ca làm việc & Doanh thu** | ✅ | ✅ | ⚙️ | | | |
| **BOH — Báo cáo Doanh thu, Món bán chạy, Hao hụt** | ✅ | ✅ | | | | |
| **BOH — Quản lý Tài khoản, Vai trò & Phân quyền** | ✅ | | | | | |
| **BOH — Cấu hình Trạm bếp, Máy POS, POS Policy** | ✅ | | | | | |
| **BOH — Cấu hình Hệ thống & Xem Audit Logs** | ✅ | | | | | |
| **QR — Quét mã QR, Chọn món, Gửi đơn** | | | | | | ✅ |
| **QR — Theo dõi tiến độ chế biến đơn hàng** | | | | | | ✅ |
| **Loyalty — Tích/Tiêu điểm & Đổi món thưởng** | ⚙️ | ⚙️ | ✅ | | | ⚙️ |

> **Ký hiệu:**  
> - `✅` : Có quyền truy cập và thao tác đầy đủ  
> - `⚙️` : Người yêu cầu hoặc chỉ xem dữ liệu liên quan  
> - Trống : Không có quyền truy cập  

---

## 4. Ma trận Sự kiện SignalR Hub (`IRestaurantHubClient`)

Toàn bộ hệ thống giao tiếp realtime thông qua hub SignalR tập trung (`/hubs/restaurant`), kết nối đa nền tảng giữa POS, KDS, BOH Admin và QR Client:

| Phương thức SignalR Client | Nguồn phát | Đối tượng nhận | Mục đích nghiệp vụ |
|---------------------------|------------|----------------|---------------------|
| `ReceiveTableStatusSync(tableId, status)` | POS, QR, Payment | Màn hình POS (Tất cả thu ngân) | Cập nhật màu sắc trạng thái bàn (Trống, Đang ăn, Đơn mới, Chờ thanh toán) |
| `ReceiveNewQrOrder(orderId, tableId, ...)` | QR Ordering | POS (Màn hình thu ngân) | Thông báo có đơn gọi món mới từ khách tại bàn cần duyệt |
| `ReceiveKitchenTicket(ticket)` | POS (Sau khi duyệt đơn) | KDS (Màn hình bếp) | Đẩy thẻ đơn món mới vào hàng đợi chế biến của bếp |
| `ReceiveOrderItemStatusChanged(...)` | KDS (Đầu bếp thao tác) | POS + Điện thoại khách (QR) | Đồng bộ trạng thái món (Pending ➔ Processing ➔ Done) |
| `ReceiveSoldOutNotice(menuItemId, isSoldOut)` | KDS hoặc BOH Admin | POS + QR Client (Tất cả khách) | Vô hiệu hóa hoặc kích hoạt lại món ăn trên thực đơn khi hết/có hàng |
| `ReceiveKitchenSlaWarning(orderItemId, ...)` | KDS Background Service | POS Thu ngân | Cảnh báo khi có món ăn chờ chế biến quá thời gian SLA quy định (> 15 phút) |
| `ReceivePaymentCompleted(orderId, ...)` | Webhook SePay / Payments API | POS Thu ngân | Tự động thông báo thanh toán thành công, đổi trạng thái đơn và in bill K80 |
| `ReceivePaymentAlert(orderId, message)` | Payments API | POS Thu ngân | Cảnh báo sai lệch số tiền hoặc lỗi giao dịch thanh toán |
| `ReceiveTableSessionMoved(oldTableId, ...)` | POS Thu ngân | Điện thoại khách tại bàn (QR) | Tự động đổi session và bàn ăn trên trình duyệt của khách khi đổi/chuyển bàn |
| `ReceiveLowStockWarning(materialId, ...)` | Inventory Service | BOH Admin / Dashboard | Cảnh báo nguyên liệu thô chạm ngưỡng tồn kho tối thiểu |

---

## 5. Tài liệu Kiến trúc & Thiết kế Liên quan

- **Kiến trúc Tổng thể:** [ARCHITECTURE.md](../ARCHITECTURE.md)
- **Thiết kế Cơ sở Dữ liệu (42+ DbSets):** [database_design.md](../data/database_design.md)
- **Thiết kế RESTful WebAPI:** [API_DESIGN.md](../api/API_DESIGN.md)
- **Hệ thống Thiết kế Giao diện (Design System):** [DESIGN_SYSTEM.md](../ui-ux/DESIGN_SYSTEM.md)
