import os
import urllib.request
import zlib
import base64
import json
import time

os.makedirs("báo cáo/figures/mermaid", exist_ok=True)

diagrams = {
    # Variation 1 for Order: flowchart LR
    "test_order_clean_lr": '''flowchart LR
    Init(( )) --> |Khách quét QR| Draft["Draft<br/>Đơn tạm"]
    Init --> |Thu ngân tạo POS| Confirmed["Confirmed<br/>Đã xác nhận"]
    Draft --> |Thu ngân duyệt| Confirmed
    Draft --> |Từ chối duyệt| Cancelled["Cancelled<br/>Đã hủy đơn"]
    Confirmed --> |Hủy đơn cần PIN| Cancelled
    Confirmed --> |Bếp nấu xong| Completed["Completed<br/>Hoàn thành món"]
    Confirmed --> |Thanh toán ngay| Paid["Paid<br/>Đã thanh toán"]
    Completed --> |Khách thanh toán| Paid
    Paid --> Done(((Hoàn tất)))
    Cancelled --> Closed(((Đóng)))

    style Init fill:#334155,stroke:#0f172a,stroke-width:2px
    style Done fill:#ede9fe,stroke:#7c3aed,stroke-width:3px
    style Closed fill:#fee2e2,stroke:#ef4444,stroke-width:3px
    style Draft fill:#f8fafc,stroke:#64748b,stroke-width:1.5px
    style Confirmed fill:#eff6ff,stroke:#3b82f6,stroke-width:1.5px
    style Completed fill:#fef3c7,stroke:#f59e0b,stroke-width:1.5px
    style Paid fill:#ecfdf5,stroke:#10b981,stroke-width:1.5px
    style Cancelled fill:#fef2f2,stroke:#ef4444,stroke-width:1.5px
''',

    # Variation 2 for Order: Clean TD with planar separate routing
    "test_order_clean_td": '''flowchart TD
    Init(( )) --> |Khách quét QR| Draft["Draft<br/>Đơn tạm"]
    Init --> |Thu ngân tạo POS| Confirmed["Confirmed<br/>Đã xác nhận"]
    Draft --> |Thu ngân duyệt đơn| Confirmed

    Confirmed --> |Bếp nấu xong món| Completed["Completed<br/>Hoàn tất nấu"]
    Confirmed --> |Thanh toán trực tiếp| Paid["Paid<br/>Đã thanh toán"]
    Completed --> |Khách thanh toán| Paid
    Paid --> Done(((Hoàn tất)))

    Draft -.-> |Từ chối duyệt QR| CancelQR["Từ chối duyệt"]
    Confirmed -.-> |Hủy đơn cần mã PIN| CancelPOS["Hủy đơn có PIN"]
    CancelQR --> Cancelled["Cancelled<br/>Đã hủy đơn"]
    CancelPOS --> Cancelled
    Cancelled --> Closed(((Đóng)))

    style Init fill:#334155,stroke:#0f172a,stroke-width:2px
    style Done fill:#ede9fe,stroke:#7c3aed,stroke-width:3px
    style Closed fill:#fee2e2,stroke:#ef4444,stroke-width:3px
    style Draft fill:#f8fafc,stroke:#64748b,stroke-width:1.5px
    style Confirmed fill:#eff6ff,stroke:#3b82f6,stroke-width:1.5px
    style Completed fill:#fef3c7,stroke:#f59e0b,stroke-width:1.5px
    style Paid fill:#ecfdf5,stroke:#10b981,stroke-width:1.5px
    style Cancelled fill:#fef2f2,stroke:#ef4444,stroke-width:1.5px
    style CancelQR fill:#fff1f2,stroke:#fca5a5,stroke-dasharray: 3 3
    style CancelPOS fill:#fff1f2,stroke:#fca5a5,stroke-dasharray: 3 3
''',

    # Variation 3 for Order: Pure StateDiagram-v2 LR
    "test_order_state_lr": '''stateDiagram-v2
    direction LR
    [*] --> Draft : Khách quét QR gọi món
    [*] --> Confirmed : Thu ngân lập tại POS
    Draft --> Confirmed : Thu ngân duyệt đơn
    Draft --> Cancelled : Từ chối duyệt đơn QR
    Confirmed --> Cancelled : Hủy đơn (Cần mã PIN)
    Confirmed --> Completed : Toàn bộ món bếp nấu xong
    Confirmed --> Paid : Thanh toán ngay món đóng chai
    Completed --> Paid : Khách thanh toán hóa đơn
    Paid --> [*] : Hoàn tất đơn hàng
    Cancelled --> [*] : Kết thúc đơn hủy
''',

    # Variation 1 for OrderItem: flowchart LR
    "test_item_clean_lr": '''flowchart LR
    Init(( )) --> |Đơn chuyển Confirmed| Pending["Pending<br/>Chờ chế biến"]
    Pending --> |Đầu bếp nhận làm| Processing["Processing<br/>Đang chế biến"]
    Pending --> |Đầu bếp làm xong nhanh| Done["Done<br/>Hoàn thành"]
    Processing --> |Đầu bếp hoàn tất| Done
    Done --> Serve(((Bê món)))

    Pending --> |Hủy món chưa nấu| Cancelled["Cancelled<br/>Hủy món Void"]
    Processing --> |Hủy đang nấu cần PIN| Cancelled
    Cancelled --> EndVoid(((Kết thúc)))

    style Init fill:#334155,stroke:#0f172a,stroke-width:2px
    style Serve fill:#ede9fe,stroke:#7c3aed,stroke-width:3px
    style EndVoid fill:#fee2e2,stroke:#ef4444,stroke-width:3px
    style Pending fill:#f8fafc,stroke:#64748b,stroke-width:1.5px
    style Processing fill:#fef3c7,stroke:#f59e0b,stroke-width:1.5px
    style Done fill:#ecfdf5,stroke:#10b981,stroke-width:1.5px
    style Cancelled fill:#fef2f2,stroke:#ef4444,stroke-width:1.5px
''',

    # Variation 2 for OrderItem: StateDiagram-v2 LR
    "test_item_state_lr": '''stateDiagram-v2
    direction LR
    [*] --> Pending : Đơn chuyển sang Confirmed
    Pending --> Processing : Bếp bấm Nhận làm trên KDS
    Pending --> Done : Bếp bấm Hoàn thành nhanh
    Processing --> Done : Bếp bấm Hoàn thành trên KDS
    Pending --> Cancelled : Thu ngân hủy món chưa nấu
    Processing --> Cancelled : Hủy món đang nấu (Cần PIN)
    Done --> [*] : Bê món phục vụ khách
    Cancelled --> [*] : Ghi nhận hủy và trừ hao hụt
''',
}

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
    for name, code in diagrams.items():
        render_diagram(name, code)
