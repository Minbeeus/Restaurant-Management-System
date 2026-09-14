# Spec Kit — Hệ thống Quản lý Nhà hàng Full Web

> **Dự án:** Đồ án Tốt nghiệp — Restaurant POS, KDS, BOH & QR Ordering  
> **Công nghệ:** ASP.NET Core · SQL Server · Blazor · SignalR  
> **Phiên bản:** 1.0  
> **Ngày tạo:** 2026-07-14

---

## Tổng quan Spec Kit

Spec Kit là bộ tài liệu đặc tả chức năng chi tiết theo từng phân hệ. Mỗi chức năng (Feature) được mô tả đầy đủ với cấu trúc chuẩn bao gồm:

| Thành phần | Mô tả |
|------------|-------|
| **Feature ID** | Mã định danh duy nhất (POS-001, KDS-001, BOH-001, QR-001, ACC-001) |
| **Actor** | Ai thực hiện chức năng |
| **Priority** | Must Have / Should Have / Nice to Have |
| **Mô tả** | Tóm tắt nghiệp vụ |
| **Điều kiện tiên quyết** | Trạng thái hệ thống trước khi thực hiện |
| **Luồng chính** | Các bước thao tác chính theo thứ tự |
| **Luồng thay thế / Ngoại lệ** | Các trường hợp ngoại lệ, lỗi, edge case |
| **Quy tắc nghiệp vụ** | Ràng buộc và logic nghiệp vụ |
| **Tiêu chí chấp nhận** | Điều kiện kiểm thử đầu ra |
| **Bảng liên quan** | Bảng dữ liệu tương tác |
| **Sự kiện SignalR** | Sự kiện realtime liên quan (nếu có) |

---

## Danh mục Phân hệ & Tổng số Feature

| # | Phân hệ | File Spec | Số Feature | Actor chính |
|---|---------|-----------|------------|-------------|
| # | Phân hệ | File Spec | Số Feature | Actor chính |
|---|---------|-----------|------------|-------------|
| 1 | **POS** (Point of Sale) | [POS-spec.md](./POS-spec.md) | 18 | Cashier |
| 2 | **KDS** (Kitchen Display System) | [KDS-spec.md](./KDS-spec.md) | 6 | Chef |
| 3 | **BOH** (Back of House) | [BOH-spec.md](./BOH-spec.md) | 18 | Admin, Manager |
| 4 | **QR Ordering** | [QR-spec.md](./QR-spec.md) | 7 | Customer (Guest) |
| 5 | **Account & Loyalty** | [ACC-spec.md](./ACC-spec.md) | 7 | All Staff, System |

**Tổng cộng: 56 Feature Specifications**

---

## Ma trận Feature theo Actor (RBAC)

| Feature | Admin | Manager | Cashier | Chef | Customer |
|---------|:-----:|:-------:|:-------:|:----:|:--------:|
| **POS** — Sơ đồ bàn, Lập đơn, Thanh toán | | | ✅ | | |
| **POS** — Duyệt đơn QR | | | ✅ | | |
| **POS** — Mở/Kết ca | | | ✅ | | |
| **POS** — Xử lý Đổi/Chuyển/Gộp bàn | | | ✅ | | |
| **KDS** — Xem ticket, Cập nhật trạng thái | | | | ✅ | |
| **KDS** — Báo hết món, Cảnh báo SLA trễ | | | | ✅ | |
| **BOH** — Quản lý Menu, Sơ đồ bàn | ✅ | ✅ | | | |
| **BOH** — Cấu hình BOM, Quản lý kho | ✅ | ✅ | | | |
| **BOH** — Nhập kho, Kiểm kê | ✅ | ✅ | | | |
| **BOH** — Báo cáo Doanh thu, Hao hụt | ✅ | ✅ | | | |
| **BOH** — Quản lý Tài khoản, Voucher | ✅ | | | | |
| **BOH** — Quản lý Khách hàng & Tier | ✅ | ✅ | | | |
| **QR** — Quét mã, Xem menu, Gọi món | | | | | ✅ |
| **QR** — Gửi đơn, Theo dõi trạng thái | | | | | ✅ |
| **ACC** — Đăng nhập/Đăng xuất | ✅ | ✅ | ✅ | ✅ | |
| **ACC** — Đăng ký Thành viên | | | ✅ | | |
| **ACC** — Tích/Tiêu điểm Loyalty & Rollback | ⚙️ | ⚙️ | ✅ | | |

> ✅ = Truy cập trực tiếp | ⚙️ = Xem/quản trị dữ liệu

---

## Ma trận Feature — Sự kiện SignalR

| Sự kiện SignalR | Nguồn phát | Nơi nhận |
|-----------------|-----------|----------|
| `TableStatusChanged` | POS, QR, Payment | POS (tất cả thu ngân) |
| `TableSessionMoved` | POS (khi gộp/chuyển bàn) | QR Client (khách hàng tại bàn) |
| `NewQrOrderReceived` | QR Ordering | POS (thu ngân đang mở ca) |
| `OrderItemStatusChanged` | KDS (bếp) | POS + QR Client (khách hàng tại bàn) |
| `OrderCompleted` | KDS (khi tất cả món Done) | POS |
| `MenuItemSoldOut` | KDS (bếp) | POS (tất cả) + QR (tất cả session) |
| `PaymentCompleted` | SePay Webhook Handler | POS (thu ngân xử lý đơn) |
| `NewOrderSentToKitchen` | POS (sau khi confirm) | KDS (tất cả màn hình bếp) |
| `KitchenSlaWarning` | KDS (khi món trễ > 15m) | POS (thu ngân) |

---

## Tài liệu Liên quan

| Tài liệu | Đường dẫn | Mô tả |
|-----------|-----------|-------|
| Feature Overview | [feature.md](../feature.md) | Tổng quan chức năng hệ thống |
| Database Design | [database_design.md](../database_design.md) | Thiết kế cơ sở dữ liệu 27 bảng |
| Rule System | [rules/README.md](../rules/README.md) | Bộ quy tắc phát triển |
