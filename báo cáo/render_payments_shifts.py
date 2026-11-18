import urllib.request
import zlib
import base64
import json

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

# Payments & SePay & Vouchers
payments_erd = """
erDiagram
    Orders ||--o{ Payments : "thanh toán bằng"
    Payments ||--o| SepayWebhookLogs : "đối soát"
    Orders ||--o| Vouchers : "áp dụng"
    
    Orders {
        int Id PK
        nvarchar OrderCode "Mã đơn hàng"
        decimal TotalAmount "Tổng tiền"
        nvarchar VoucherCode "Mã voucher"
    }
    Payments {
        int Id PK
        int OrderId FK "Mã đơn"
        int PaymentMethod "Tiền mặt / VietQR"
        decimal Amount "Số tiền"
        nvarchar TransactionRef "Mã GD"
        int Status "Completed / Failed"
        datetime2 CompletedAt "Thời điểm"
    }
    SepayWebhookLogs {
        int Id PK
        int PaymentId FK "Khớp đơn"
        nvarchar TransactionId "Mã SePay"
        decimal Amount "Số tiền"
        nvarchar Content "Nội dung"
        bit IsProcessed "Đã xử lý"
    }
    Vouchers {
        int Id PK
        nvarchar Code "Mã giảm giá"
        decimal DiscountValue "Mức giảm"
        decimal MinOrderAmount "Đơn tối thiểu"
        bit IsActive "Kích hoạt"
    }
"""

# Shifts & Cash Movements
shifts_erd = """
erDiagram
    Users ||--o{ Shifts : "mở ca làm việc"
    Shifts ||--o{ CashMovements : "biến động két"
    
    Users {
        int Id PK
        nvarchar Username "Tên đăng nhập"
        nvarchar FullName "Họ tên thu ngân"
        nvarchar Role "Vai trò"
    }
    Shifts {
        int Id PK
        int UserId FK "Thu ngân"
        decimal OpeningBalance "Tiền đầu ca"
        decimal ActualCashCounted "Tiền kiểm đếm"
        decimal Difference "Chênh lệch"
        int Status "Open / Closed"
        datetime2 OpenedAt "Giờ mở ca"
        datetime2 ClosedAt "Giờ kết ca"
    }
    CashMovements {
        int Id PK
        int ShiftId FK "Mã ca"
        int Type "TillIn / TillOut"
        decimal Amount "Số tiền nạp/rút"
        nvarchar Reason "Lý do nạp/rút"
        datetime2 CreatedAt "Thời điểm"
    }
"""

render_code('spec_erd_payments_sepay', payments_erd)
render_code('spec_erd_shifts_cash', shifts_erd)
