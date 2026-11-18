import os
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
    
    with open('báo cáo/exact_item_pages.txt', 'w', encoding='utf-8') as out:
        out.write("=== HEADINGS ===\n")
        for p in doc.Paragraphs:
            s_name = p.Style.NameLocal
            txt = p.Range.Text.strip()
            if s_name in ['Heading 1', 'Heading 2', 'Heading 3']:
                adj_p = p.Range.Information(1) # Adjusted printed page number
                sec_num = p.Range.Information(2)
                out.write(f"[{s_name}] Sec {sec_num} | Pg {adj_p} | {txt}\n")
                
        out.write("\n=== FIGURES ===\n")
        for i in range(1, doc.InlineShapes.Count + 1):
            s = doc.InlineShapes(i)
            adj_p = s.Range.Information(1)
            try:
                caption = s.Range.Paragraphs(1).Next().Range.Text.strip()
            except:
                caption = ""
            out.write(f"Fig {i:2d} | Pg {adj_p:2d} | {caption}\n")
            
        out.write("\n=== TABLES ===\n")
        for i in range(1, doc.Tables.Count + 1):
            tbl = doc.Tables(i)
            if tbl.Rows.Count <= 1:
                continue
            adj_p = tbl.Range.Characters.First.Information(1)
            try:
                prev_p = tbl.Range.Paragraphs(1).Previous().Range.Text.strip()
            except:
                prev_p = ""
            out.write(f"Tbl {i:2d} | Pg {adj_p:2d} | {prev_p}\n")
            
    doc.Close(False)
    print("Exact item pages written successfully!")
finally:
    word.Quit()
    if os.path.exists(dst):
        os.remove(dst)
