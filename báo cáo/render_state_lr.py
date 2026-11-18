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
    url = f'https://mermaid.ink/img/pako:' + b64
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

shift_lr = """
flowchart LR
    A([Bắt đầu]) --> B[Mở ca: Khai báo két ban đầu]
    B --> C[Phục vụ bán hàng: Nạp rút tiền két]
    C --> D[Kết ca: Kiểm đếm tiền thực tế]
    D --> E[In Z-Report & Đối soát chênh lệch]
    E --> F([Khóa ca])
    
    style A fill:#e2e8f0,stroke:#64748b
    style B fill:#dbeafe,stroke:#2563eb,stroke-width:2px
    style C fill:#fef3c7,stroke:#d97706
    style D fill:#fee2e2,stroke:#dc2626
    style E fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style F fill:#e2e8f0,stroke:#64748b
"""

stocktake_lr = """
flowchart LR
    A([Bắt đầu]) --> B[Tạo đợt: Ghi nhận tồn lý thuyết]
    B --> C[Thực địa: Đếm số lượng thực tế]
    C --> D[Đối chiếu: Tính độ lệch chênh lệch]
    D --> E[Cân bằng: Cập nhật tồn & Ghi nhận hao hụt]
    E --> F([Hoàn tất])
    
    style A fill:#e2e8f0,stroke:#64748b
    style B fill:#dbeafe,stroke:#2563eb,stroke-width:2px
    style C fill:#fef3c7,stroke:#d97706
    style D fill:#fee2e2,stroke:#dc2626
    style E fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style F fill:#e2e8f0,stroke:#64748b
"""

render_code('spec_state_shift_lr', shift_lr)
render_code('spec_state_stocktake_lr', stocktake_lr)
