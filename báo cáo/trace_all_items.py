import win32com.client
import os
import shutil

src = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
dst = os.path.abspath('báo cáo/temp_trace.docx')
shutil.copyfile(src, dst)

word = win32com.client.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = 0

try:
    doc = word.Documents.Open(dst, ReadOnly=True, ConfirmConversions=False)
    
    print("--- TRACING PARAGRAPHS & HEADINGS ---")
    results = []
    for p in doc.Paragraphs:
        style_name = p.Style.NameLocal
        txt = p.Range.Text.strip()
        if not txt:
            continue
        page = p.Range.Information(3) # wdActiveEndPageNumber
        sec = p.Range.Information(2)
        
        # Check if heading or figure or table title
        if style_name in ['Heading 1', 'Heading 2', 'Heading 3'] or txt.startswith('Hình ') or txt.startswith('Bảng '):
            results.append((sec, page, style_name, txt[:100]))
            
    with open('báo cáo/trace_results.txt', 'w', encoding='utf-8') as f:
        for sec, page, style, txt in results:
            f.write(f"Sec {sec} | Page {page} | {style} | {txt}\n")
            
    print(f"Traced {len(results)} items. Saved to báo cáo/trace_results.txt")

    # Check table pages (start and end page of each table)
    print("\n--- CHECKING TABLES PAGE SPAN ---")
    multi_page_tables = []
    for i, tbl in enumerate(doc.Tables):
        # ignore 1-row layout tables (e.g. side-by-side figures)
        if tbl.Rows.Count <= 2:
            continue
        start_p = tbl.Range.Information(3)
        end_p = tbl.Rows(tbl.Rows.Count).Range.Information(3)
        span = end_p - start_p + 1
        if span > 1:
            multi_page_tables.append((i+1, start_p, end_p, span))
        print(f"Table {i+1}: Start Pg {start_p}, End Pg {end_p}, Span {span}")
        
    print(f"\nMulti-page tables count: {len(multi_page_tables)}")
    if multi_page_tables:
        for t_idx, sp, ep, span in multi_page_tables:
            print(f"  WARNING: Table {t_idx} spans {span} pages (Pg {sp} - {ep})")

    doc.Close(False)
finally:
    word.Quit()
    if os.path.exists(dst):
        os.remove(dst)
