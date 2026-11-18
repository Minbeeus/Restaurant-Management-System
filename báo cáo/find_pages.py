import os
import win32com.client as win32

word = win32.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = False

doc_path = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
doc = word.Documents.Open(doc_path, False, True)

print("Total pages:", doc.ComputeStatistics(2))

# Check paragraphs and see which page they are on
print("--- HEADINGS AND TABLES ---")
for i, para in enumerate(doc.Paragraphs):
    text = para.Range.Text.strip()
    if text.startswith("CHƯƠNG") or text.startswith("2.") or text.startswith("3.") or text.startswith("Bảng 2.") or text.startswith("Hình 2."):
        # Get page number
        page = para.Range.Information(1) # wdActiveEndPageNumber = 1
        if 20 <= page <= 45:
            print(f"Page {page:2d}: {text[:70]}")

doc.Close(False)
word.Quit()
