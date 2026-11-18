import os
import sys
import win32com.client as win32

sys.stdout.reconfigure(encoding='utf-8')

word = win32.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = False

doc_path = os.path.abspath('báo cáo/temp_inspect.docx')
doc = word.Documents.Open(doc_path, False, True)

print(f"Total Pages: {doc.ComputeStatistics(2)}")

print("\n--- FIGURES AND THEIR PAGES ---")
for i in range(1, doc.InlineShapes.Count + 1):
    s = doc.InlineShapes(i)
    page = s.Range.Information(1)
    try:
        caption = s.Range.Paragraphs(1).Next().Range.Text.strip()
    except:
        caption = ""
    w_in = s.Width / 72.0
    h_in = s.Height / 72.0
    print(f"Fig {i:2d}: Page {page:2d} | Size: {w_in:.2f}x{h_in:.2f} in ({s.Width:5.1f}x{s.Height:5.1f} pt) | {caption[:65]}")
    sys.stdout.flush()

print("\n--- TABLES AND THEIR PAGES ---")
for i in range(1, doc.Tables.Count + 1):
    tbl = doc.Tables(i)
    p_start = tbl.Range.Characters.First.Information(1)
    p_end = tbl.Range.Characters.Last.Information(1)
    try:
        prev_p = tbl.Range.Paragraphs(1).Previous().Range.Text.strip()
    except:
        prev_p = ""
    print(f"Table {i:2d}: Page {p_start:2d}->{p_end:2d} (Span: {p_end - p_start + 1}) | {prev_p[:65]}")
    sys.stdout.flush()

doc.Close(False)
word.Quit()
print("Inspection completed successfully!")
