import urllib.request
import zlib
import base64
import json
import os

def render_code(name, code):
    out_file = f'báo cáo/figures/mermaid/{name}.png'
    clean_code = code.strip()
    j = json.dumps({'code': clean_code, 'mermaid': {'theme': 'default'}})
    compressed = zlib.compress(j.encode('utf-8'))
    b64 = base64.urlsafe_b64encode(compressed).decode('ascii')
    url = 'https://mermaid.ink/img/pako:' + b64
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        with urllib.request.urlopen(req, timeout=12) as r:
            data = r.read()
            with open(out_file, 'wb') as f:
                f.write(data)
            print(f"Successfully rendered: {name} ({len(data)} bytes)")
            return out_file
    except Exception as ex:
        print(f"Failed to render {name}: {ex}")
        return None

# 1. Menu: Split into Menu Items (2.11a) and Menu Modifiers (2.11b)
menu_items_erd = """
erDiagram
    Categories ||--o{ MenuItems : "chứa"
    Categories {
        int Id PK
        nvarchar Name "Tên danh mục"
        int SortOrder "Thứ tự"
        bit IsActive "Kích hoạt"
        datetime2 CreatedAt "Ngày tạo"
    }
    MenuItems {
        int Id PK
        int CategoryId FK "Mã danh mục"
        nvarchar Name "Tên món"
        decimal Price "Đơn giá"
        nvarchar ImageUrl "Ảnh"
        bit IsSoldOut "Hết hàng"
        bit IsCombo "Là combo"
        bit IsActive "Kích hoạt"
    }
"""

menu_modifiers_erd = """
erDiagram
    MenuItems ||--o{ MenuItemModifierGroups : "liên kết"
    ModifierGroups ||--o{ MenuItemModifierGroups : "liên kết"
    ModifierGroups ||--o{ ModifierOptions : "chứa"
    
    MenuItems {
        int Id PK
        nvarchar Name "Tên món chính"
        decimal Price "Giá bán"
    }
    MenuItemModifierGroups {
        int Id PK
        int MenuItemId FK
        int ModifierGroupId FK
        int SortOrder
    }
    ModifierGroups {
        int Id PK
        nvarchar Name "Tên nhóm (Size/Topping)"
        bit IsRequired "Bắt buộc chọn"
        int MinSelections "Chọn tối thiểu"
        int MaxSelections "Chọn tối đa"
    }
    ModifierOptions {
        int Id PK
        int ModifierGroupId FK
        nvarchar Name "Tên tùy chọn"
        decimal ExtraPrice "Phụ thu"
        bit IsDefault "Mặc định"
    }
"""

# 2. Orders & Payments: Split into Orders & Items (2.12a) and Payments & Shifts (2.12b)
orders_items_erd = """
erDiagram
    Orders ||--o{ OrderItems : "chứa các món"
    OrderItems ||--o{ OrderItemModifiers : "kèm tùy chọn"
    Orders ||--o{ VoidLogs : "nhật ký hủy"
    
    Orders {
        int Id PK
        nvarchar OrderCode "Mã đơn"
        int OrderType "DineIn / Takeaway"
        int TableId FK "Bàn phục vụ"
        int Status "Draft / Confirmed / Paid"
        decimal SubTotal "Tổng tiền món"
        decimal TotalAmount "Thực thu"
        datetime2 CreatedAt "Thời gian tạo"
    }
    OrderItems {
        int Id PK
        int OrderId FK "Mã đơn"
        int MenuItemId FK "Mã món"
        nvarchar MenuItemName "Tên món"
        decimal UnitPrice "Đơn giá"
        int Quantity "Số lượng"
        int KitchenStatus "Pending/Processing/Done"
        datetime2 CompletedAt "Bếp xong"
    }
    OrderItemModifiers {
        int Id PK
        int OrderItemId FK "Dòng món"
        int ModifierOptionId FK "Tùy chọn"
        nvarchar ModifierOptionName "Tên"
        decimal ExtraPrice "Phụ thu"
    }
    VoidLogs {
        int Id PK
        int OrderId FK "Mã đơn"
        int OrderItemId FK "Dòng món hủy"
        int Quantity "Số lượng hủy"
        nvarchar Reason "Lý do hủy"
        int VoidByUserId FK "Người duyệt"
        datetime2 CreatedAt "Thời điểm hủy"
    }
"""

payments_shifts_erd = """
erDiagram
    Shifts ||--o{ Orders : "trong ca"
    Shifts ||--o{ Payments : "thu ngân xử lý"
    Orders ||--o{ Payments : "thanh toán bằng"
    Payments ||--o| SepayWebhookLogs : "đối soát tự động"
    
    Shifts {
        int Id PK
        int UserId FK "Thu ngân mở ca"
        decimal OpeningBalance "Tiền két đầu ca"
        decimal ActualCashCounted "Tiền thực tế"
        decimal Difference "Chênh lệch két"
        int Status "Open / Closed"
        datetime2 OpenedAt "Bắt đầu ca"
        datetime2 ClosedAt "Kết thúc ca"
    }
    Orders {
        int Id PK
        nvarchar OrderCode "Mã đơn hàng"
        decimal TotalAmount "Số tiền cần thanh toán"
        nvarchar VoucherCode "Mã giảm giá"
    }
    Payments {
        int Id PK
        int OrderId FK "Đơn hàng"
        int ShiftId FK "Ca làm việc"
        int PaymentMethod "Tiền mặt / VietQR"
        decimal Amount "Số tiền trả"
        nvarchar TransactionRef "Mã tham chiếu"
        int Status "Completed / Failed"
        datetime2 CompletedAt "Thời điểm trả"
    }
    SepayWebhookLogs {
        int Id PK
        int PaymentId FK "Thanh toán khớp"
        nvarchar TransactionId "Mã SePay"
        decimal Amount "Số tiền chuyển"
        nvarchar Content "Nội dung CK"
        bit IsProcessed "Đã xử lý"
    }
"""

render_code('spec_erd_menu_items', menu_items_erd)
render_code('spec_erd_menu_modifiers', menu_modifiers_erd)
render_code('spec_erd_orders_items', orders_items_erd)
render_code('spec_erd_payments_shifts', payments_shifts_erd)
