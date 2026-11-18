import os
import win32com.client as win32

word = win32.gencache.EnsureDispatch('Word.Application')
word.Visible = False

doc_path = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
doc = word.Documents.Open(doc_path)

print("=== INLINE SHAPES (IMAGES) AND THEIR PAGES ===")
for i in range(1, doc.InlineShapes.Count + 1):
    shape = doc.InlineShapes(i)
    page = shape.Range.Information(1) # 1 = wdActiveEndPageNumber
    h = shape.Height
    w = shape.Width
    # try to get caption (paragraph next)
    try:
        next_para = shape.Range.Paragraphs(1).Next().Range.Text.strip()
    except:
        next_para = ""
    print(f"Shape {i}: Page {page} | Size: {w:.1f} x {h:.1f} pt | Caption: {next_para[:60]}")

doc.Close()
word.Quit()
