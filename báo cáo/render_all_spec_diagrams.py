import os
import re
import urllib.request
import zlib
import base64
import json
import sys
import time

os.makedirs('báo cáo/figures/mermaid', exist_ok=True)

def render_diagram(name, code):
    out_file = f'báo cáo/figures/mermaid/{name}.png'
    clean_code = code.strip()
    j = json.dumps({'code': clean_code, 'mermaid': {'theme': 'default'}})
    compressed = zlib.compress(j.encode('utf-8'))
    b64 = base64.urlsafe_b64encode(compressed).decode('ascii')
    url = 'https://mermaid.ink/img/pako:' + b64
    
    headers = {'User-Agent': 'Mozilla/5.0'}
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(req, timeout=10) as resp:
                data = resp.read()
                with open(out_file, 'wb') as f:
                    f.write(data)
                print(f"[{name}] Success: {len(data)} bytes")
                sys.stdout.flush()
                return out_file
        except Exception as ex:
            print(f"[{name}] Attempt {attempt+1} failed: {ex}")
            sys.stdout.flush()
            time.sleep(1)
    return None

def main():
    spec_path = 'báo cáo/DIAGRAMS_SPEC.md'
    with open(spec_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    names = [
        'spec_arch_layers',
        'spec_arch_sequence',
        'spec_seq_order_kds',
        'spec_state_table',
        'spec_state_order',
        'spec_state_order_item',
        'spec_state_payment',
        'spec_state_shift',
        'spec_state_stocktake',
        'spec_erd_auth',
        'spec_erd_menu_items',
        'spec_erd_menu_modifiers',
        'spec_erd_orders_items',
        'spec_erd_payments_sepay',
        'spec_erd_shifts_cash',
        'spec_erd_loyalty',
        'spec_erd_inventory_bom',
        'spec_erd_module_flowchart',
        'spec_usecase_overall',
        'spec_usecase_qr',
        'spec_usecase_pos',
        'spec_usecase_kds',
        'spec_usecase_admin',
        'spec_class_orders_payments',
        'spec_class_menu_modifiers',
        'spec_class_inventory_bom',
        'spec_class_shifts_cash'
    ]
    
    blocks = re.findall(r'```mermaid\s*(.*?)```', content, re.DOTALL)
    print(f"Rendering {len(blocks)} diagrams...")
    sys.stdout.flush()
    
    for idx, (name, block) in enumerate(zip(names, blocks)):
        render_diagram(name, block)

if __name__ == '__main__':
    main()
