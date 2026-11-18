import os
import sys
import shutil
import win32com.client as win32

src = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
dst = os.path.abspath('báo cáo/temp_inspect.docx')
shutil.copyfile(src, dst)

word = win32.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = False

try:
    doc = word.Documents.Open(dst, False, True)
    total_pages = doc.ComputeStatistics(2)
    
    with open('báo cáo/fast_trace_output.txt', 'w', encoding='utf-8') as out:
        out.write(f"TOTAL_PAGES: {total_pages}\n\n")
        
        out.write("--- FIGURES ---\n")
        for i in range(1, doc.InlineShapes.Count + 1):
            s = doc.InlineShapes(i)
            page = s.Range.Information(1)
            try:
                caption = s.Range.Paragraphs(1).Next().Range.Text.strip()
            except:
                caption = ""
            w_in = s.Width / 72.0
            h_in = s.Height / 72.0
            out.write(f"Fig {i:2d} | Page {page:2d} | {w_in:.2f}x{h_in:.2f} in | {caption[:70]}\n")
            
        out.write("\n--- TABLES ---\n")
        multi_page_count = 0
        for i in range(1, doc.Tables.Count + 1):
            tbl = doc.Tables(i)
            # ignore 1-row side-by-side figure container tables
            if tbl.Rows.Count <= 1:
                continue
            p_start = tbl.Range.Characters.First.Information(1)
            p_end = tbl.Range.Characters.Last.Information(1)
            span = p_end - p_start + 1
            if span > 1:
                multi_page_count += 1
            try:
                prev_p = tbl.Range.Paragraphs(1).Previous().Range.Text.strip()
            except:
                prev_p = ""
            out.write(f"Table {i:2d} | Page {p_start:2d}->{p_end:2d} (Span: {span}) | {prev_p[:70]}\n")
            
        out.write(f"\nMulti-page tables count: {multi_page_count}\n")
        
        out.write("\n--- HEADINGS ---\n")
        headings = doc.GetCrossReferenceItems(1) # wdRefTypeHeading
        for idx, h in enumerate(headings):
            out.write(f"H {idx+1:2d}: {h.strip()}\n")

    doc.Close(False)
    print("Fast trace completed!")
finally:
    word.Quit()
    if os.path.exists(dst):
        os.remove(dst)
