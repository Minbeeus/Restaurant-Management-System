import os
import urllib.request
import zlib
import base64
import json
import time

diagrams = {
    "spec_usecase_overall": '''flowchart LR
    Customer(("Khách hàng<br/>(Customer)"))
    Cashier(("Thu ngân<br/>(Cashier)"))
    Chef(("Đầu bếp<br/>(Kitchen Chef)"))
    Admin(("Quản lý / Thủ kho<br/>(Manager / Admin)"))
    SePay[("Cổng thanh toán<br/>VietQR SePay")]

    subgraph SystemBoundary ["HỆ THỐNG QUẢN LÝ NHÀ HÀNG & ĐẶT MÓN TẠI BÀN"]
        UC1(["Quét QR & Tự gọi món tại bàn"])
        UC2(["Quản lý Sơ đồ Bàn ăn Realtime"])
        UC3(["Tiếp nhận & Duyệt đơn hàng POS"])
        UC4(["Bán hàng tại quầy & Đơn mang đi"])
        UC5(["Điều phối Chế biến Nhà bếp KDS"])
        UC6(["Thanh toán Đa phương thức VietQR & Tiền mặt"])
        UC7(["Tự động trừ tồn kho định lượng BOM"])
        UC8(["Quản lý Ca làm việc & Tiền két thu ngân"])
        UC9(["Thiết lập Thực đơn, Topping & Định mức BOM"])
        UC10(["Kiểm kê Kho & Đối soát Cân bằng tồn"])
        UC11(["Phê duyệt yêu cầu hủy món & Giảm giá đặc biệt"])
        UC12(["Báo cáo Doanh thu & Hao hụt thất thoát"])
    end

    Customer --> UC1
    Customer --> UC6
    
    Cashier --> UC2
    Cashier --> UC3
    Cashier --> UC4
    Cashier --> UC6
    Cashier --> UC8
    
    Chef --> UC5
    Chef --> UC7
    
    Admin --> UC8
    Admin --> UC9
    Admin --> UC10
    Admin --> UC11
    Admin --> UC12

    UC6 <-->|Webhook đối soát| SePay
''',

    "spec_usecase_qr": '''flowchart TD
    Customer(("Khách hàng<br/>(Thực khách tại bàn)"))

    subgraph QrSubsystem ["Phân hệ Tự Đặt Món QR Ordering (Khách hàng)"]
        UC_Q1(["Quét mã QR nhận diện Bàn ăn"])
        UC_Q2(["Duyệt Thực đơn & Tìm kiếm món"])
        UC_Q3(["Tùy biến Món ăn (Đường, Đá, Topping)"])
        UC_Q4(["Chọn món thành phần Combo"])
        UC_Q5(["Nhập SĐT tích điểm Khách hàng thân thiết"])
        UC_Q6(["Gửi đơn đặt món tạm (Draft Order)"])
        UC_Q7(["Theo dõi tiến độ chế biến bếp Realtime"])
        UC_Q8(["Tự động đồng bộ giỏ hàng khi Chuyển bàn"])
    end

    Customer --> UC_Q1
    Customer --> UC_Q2
    Customer --> UC_Q3
    Customer --> UC_Q4
    Customer --> UC_Q5
    Customer --> UC_Q6
    Customer --> UC_Q7
    Customer --> UC_Q8

    UC_Q3 -.->|<<extend>>| UC_Q2
    UC_Q4 -.->|<<extend>>| UC_Q2
    UC_Q6 -.->|<<include>>| UC_Q1
''',

    "spec_usecase_pos": '''flowchart TD
    Cashier(("Nhân viên Thu ngân<br/>(Quầy POS)"))

    subgraph PosSubsystem ["Phân hệ Điểm Bán Hàng & Quản trị Sảnh (POS)"]
        UC_P1(["Mở ca thu ngân & Khai báo tiền két"])
        UC_P2(["Quản lý sơ đồ bàn (Mở, Gộp, Chuyển bàn)"])
        UC_P3(["Tiếp nhận & Phê duyệt đơn QR Ordering"])
        UC_P4(["Lập đơn trực tiếp tại bàn & Đơn mang đi"])
        UC_P5(["Điều chỉnh giỏ hàng, ghi chú & Giảm giá"])
        UC_P6(["In phiếu tạm tính tiền khổ K80"])
        UC_P7(["Khởi tạo thanh toán VietQR động Napas 247"])
        UC_P8(["Thu tiền mặt & Tính tiền thừa tự động"])
        UC_P9(["Thực hiện Nạp/Rút tiền két (Till In/Out)"])
        UC_P10(["Đóng ca làm việc & In báo cáo Z-Report"])
    end

    Cashier --> UC_P1
    Cashier --> UC_P2
    Cashier --> UC_P3
    Cashier --> UC_P4
    Cashier --> UC_P5
    Cashier --> UC_P6
    Cashier --> UC_P7
    Cashier --> UC_P8
    Cashier --> UC_P9
    Cashier --> UC_P10

    UC_P7 -.->|<<extend>>| UC_P6
    UC_P8 -.->|<<extend>>| UC_P6
''',

    "spec_usecase_kds": '''flowchart TD
    Chef(("Đầu bếp / Nhân viên bếp<br/>(Kitchen Crew)"))

    subgraph KdsSubsystem ["Phân hệ Điều Phối Nhà Bếp Thời Gian Thực (KDS)"]
        UC_K1(["Lựa chọn Trạm chế biến chuyên môn (Station)"])
        UC_K2(["Tiếp nhận Vé món điện tử tức thời (Ticket)"])
        UC_K3(["Theo dõi đồng hồ cảnh báo quá hạn SLA (10p/15p)"])
        UC_K4(["Cập nhật tiến độ: Nhận nấu (Processing)"])
        UC_K5(["Cập nhật tiến độ: Nấu xong (Done / Ready)"])
        UC_K6(["Bật/Tắt trạng thái Hết món tức thời (Sold-out)"])
        UC_K7(["Yêu cầu Hủy món đang nấu (Cần mã PIN)"])
        UC_K8(["Kích hoạt trừ kho nguyên vật liệu BOM tự động"])
    end

    Chef --> UC_K1
    Chef --> UC_K2
    Chef --> UC_K3
    Chef --> UC_K4
    Chef --> UC_K5
    Chef --> UC_K6
    Chef --> UC_K7
    
    UC_K5 -.->|<<include>>| UC_K8
''',

    "spec_usecase_admin": '''flowchart TD
    Admin(("Quản lý / Quản trị viên / Thủ kho<br/>(BOH Management)"))

    subgraph AdminSubsystem ["Phân hệ Quản Trị Hậu Sảnh & Kiểm Soát (BOH Admin)"]
        UC_A1(["Quản lý Thực đơn, Món ăn & Topping"])
        UC_A2(["Thiết lập Công thức Định mức BOM (Recipe)"])
        UC_A3(["Quản lý Danh mục Nguyên vật liệu & Tồn kho"])
        UC_A4(["Lập Phiếu Nhập kho & Nhà cung cấp"])
        UC_A5(["Tạo Đợt kiểm kê & Cân bằng tồn kho thực tế"])
        UC_A6(["Phê duyệt Yêu cầu Hủy món/Hủy đơn nhạy cảm"])
        UC_A7(["Đối soát Giao dịch Webhook VietQR SePay"])
        UC_A8(["Quản lý Ca làm việc, Nhân viên & Phân quyền RBAC"])
        UC_A9(["Xem Báo cáo Doanh thu & Chi phí Hao hụt"])
    end

    Admin --> UC_A1
    Admin --> UC_A2
    Admin --> UC_A3
    Admin --> UC_A4
    Admin --> UC_A5
    Admin --> UC_A6
    Admin --> UC_A7
    Admin --> UC_A8
    Admin --> UC_A9
''',

    "spec_class_orders_payments": '''classDiagram
    class Order {
        +int Id
        +string OrderNumber
        +int? TableId
        +OrderStatus Status
        +decimal SubTotal
        +decimal DiscountAmount
        +decimal TaxAmount
        +decimal TotalAmount
        +byte[] RowVersion
        +DateTime CreatedAt
        +AddItem(menuItemId, qty, unitPrice)
        +CancelOrder(reason, managerPin)
        +MarkAsPaid()
    }

    class OrderItem {
        +int Id
        +int OrderId
        +int MenuItemId
        +int Quantity
        +decimal UnitPrice
        +decimal TotalPrice
        +KitchenStatus Status
        +string Notes
        +UpdateStatus(newStatus)
        +VoidItem(reason, wasteCharge)
    }

    class OrderItemModifier {
        +int Id
        +int OrderItemId
        +int ModifierOptionId
        +string OptionName
        +decimal ExtraPrice
    }

    class Payment {
        +int Id
        +int OrderId
        +PaymentMethod Method
        +decimal Amount
        +PaymentStatus Status
        +string TransactionRef
        +DateTime CreatedAt
        +ProcessCash(tendered, change)
        +ProcessVietQR(transactionRef)
    }

    class SepayWebhookLog {
        +int Id
        +int SepayTransactionId
        +string Gateway
        +decimal TransferAmount
        +string Content
        +string ReferenceCode
        +bool IsProcessed
        +ValidateIdempotency()
    }

    class VoidLog {
        +int Id
        +int OrderItemId
        +int StaffId
        +int? ApprovedBy
        +string Reason
        +decimal LossAmount
        +DateTime CreatedAt
    }

    Order "1" *-- "1..*" OrderItem : Chứa các dòng món
    OrderItem "1" *-- "0..*" OrderItemModifier : Có các tùy chọn
    Order "1" *-- "0..*" Payment : Thực hiện thanh toán
    Payment "0..1" --> "0..1" SepayWebhookLog : Khớp webhook
    OrderItem "1" --> "0..1" VoidLog : Lưu vết hủy món
''',

    "spec_class_menu_modifiers": '''classDiagram
    class Category {
        +int Id
        +string Name
        +string Code
        +int DisplayOrder
        +bool IsActive
    }

    class MenuItem {
        +int Id
        +int CategoryId
        +string Name
        +string Code
        +decimal BasePrice
        +string ImageUrl
        +bool IsAvailable
        +bool IsSoldOut
        +bool IsCombo
        +ToggleSoldOut(status)
    }

    class ModifierGroup {
        +int Id
        +string Name
        +int MinSelections
        +int MaxSelections
        +bool IsRequired
    }

    class ModifierOption {
        +int Id
        +int ModifierGroupId
        +string Name
        +decimal ExtraPrice
        +bool IsDefault
    }

    class MenuItemModifierGroup {
        +int MenuItemId
        +int ModifierGroupId
    }

    class ComboItem {
        +int Id
        +int ComboMenuItemId
        +int GroupId
        +int AllowedMenuItemId
        +int Quantity
    }

    Category "1" *-- "0..*" MenuItem : Phân loại
    MenuItem "1" *-- "0..*" MenuItemModifierGroup : Liên kết
    ModifierGroup "1" *-- "0..*" MenuItemModifierGroup : Thuộc về
    ModifierGroup "1" *-- "1..*" ModifierOption : Bao gồm các lựa chọn
    MenuItem "1" *-- "0..*" ComboItem : Chứa món thành phần
''',

    "spec_class_inventory_bom": '''classDiagram
    class Material {
        +int Id
        +string Name
        +string Code
        +int BaseUnitId
        +decimal CurrentStock
        +decimal MinStockLevel
        +decimal CostPrice
        +DeductStock(quantity)
        +ReplenishStock(quantity)
    }

    class BaseUnit {
        +int Id
        +string Name
        +string Symbol
    }

    class Recipe {
        +int Id
        +int MenuItemId
        +string Description
        +bool IsActive
    }

    class RecipeItem {
        +int Id
        +int RecipeId
        +int MaterialId
        +decimal QuantityNeeded
        +decimal WastePercentage
    }

    class InventoryTransaction {
        +int Id
        +int MaterialId
        +TransactionType Type
        +decimal QuantityChange
        +decimal RemainingStock
        +string ReferenceNumber
        +DateTime CreatedAt
    }

    class Stocktake {
        +int Id
        +string Code
        +StocktakeStatus Status
        +int CreatedByStaffId
        +DateTime CreatedAt
        +CompleteAndBalanceStock()
    }

    class StocktakeItem {
        +int Id
        +int StocktakeId
        +int MaterialId
        +decimal SystemStock
        +decimal ActualStock
        +decimal Discrepancy
        +string Notes
    }

    Material "1" --> "1" BaseUnit : Đơn vị tính cơ sở
    MenuItem "1" *-- "0..1" Recipe : Công thức định mức BOM
    Recipe "1" *-- "1..*" RecipeItem : Chi tiết tiêu hao
    Material "1" <-- "1..*" RecipeItem : Sử dụng nguyên liệu
    Material "1" *-- "0..*" InventoryTransaction : Lịch sử biến động
    Stocktake "1" *-- "1..*" StocktakeItem : Dòng kiểm đếm
    Material "1" <-- "1..*" StocktakeItem : Kiểm kê vật tư
''',

    "spec_class_shifts_cash": '''classDiagram
    class Shift {
        +int Id
        +int StaffId
        +DateTime OpenTime
        +DateTime? CloseTime
        +decimal OpeningBalance
        +decimal? ClosingCashActual
        +decimal ExpectedCash
        +ShiftStatus Status
        +RecordCashMovement(type, amount, note)
        +CloseShift(actualCash)
    }

    class CashMovement {
        +int Id
        +int ShiftId
        +CashMovementType Type
        +decimal Amount
        +string Reason
        +int? ApprovedBy
        +DateTime CreatedAt
    }

    class User {
        +int Id
        +string Username
        +string PasswordHash
        +string FullName
        +string PinHash
        +int FailedPinAttempts
        +DateTime? PinLockoutUntil
        +bool IsActive
        +VerifyPin(pin)
        +ResetPinLockout()
    }

    class Role {
        +int Id
        +string Name
        +string Code
        +string Permissions
    }

    class Table {
        +int Id
        +int AreaId
        +string TableNumber
        +int Capacity
        +TableStatus Status
        +string QrToken
        +UpdateStatus(newStatus)
        +RegenerateQrToken()
    }

    class Area {
        +int Id
        +string Name
        +int Floor
        +bool IsActive
    }

    Shift "1" *-- "0..*" CashMovement : Các khoản nạp/rút trong ca
    User "1" *-- "0..*" Shift : Thu ngân phụ trách ca
    User "1" --> "1..*" Role : Gán vai trò quyền hạn
    Area "1" *-- "1..*" Table : Không gian sảnh
''',
}

os.makedirs("báo cáo/figures/mermaid", exist_ok=True)

def render_diagram(name, code):
    out_file = f"báo cáo/figures/mermaid/{name}.png"
    clean_code = code.strip()
    j = json.dumps({"code": clean_code, "mermaid": {"theme": "default"}})
    compressed = zlib.compress(j.encode("utf-8"))
    b64 = base64.urlsafe_b64encode(compressed).decode("ascii")
    url = "https://mermaid.ink/img/pako:" + b64
    
    headers = {"User-Agent": "Mozilla/5.0"}
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(req, timeout=15) as resp:
                data = resp.read()
                with open(out_file, "wb") as f:
                    f.write(data)
                print(f"[{name}] Success: {len(data)} bytes")
                return True
        except Exception as ex:
            print(f"[{name}] Attempt {attempt+1} failed: {ex}")
            time.sleep(1)
    return False

if __name__ == "__main__":
    print(f"Rendering {len(diagrams)} new diagrams...")
    for name, code in diagrams.items():
        render_diagram(name, code)
