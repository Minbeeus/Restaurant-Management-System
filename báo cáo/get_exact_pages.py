import win32com.client
import os
import shutil

src = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
dst = os.path.abspath('báo cáo/temp_inspect.docx')
shutil.copyfile(src, dst)

word = win32com.client.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = 0

try:
    doc = word.Documents.Open(dst, ReadOnly=True, ConfirmConversions=False)
    pages = doc.ComputeStatistics(2)
    words = doc.ComputeStatistics(0)
    print(f"TOTAL_PAGES: {pages}")
    print(f"TOTAL_WORDS: {words}")
    
    # Check each section
    for i, sec in enumerate(doc.Sections):
        p_count = sec.Range.Information(3)
        print(f"Section {i+1}: End Page {p_count}")

    doc.Close(False)
finally:
    word.Quit()
    if os.path.exists(dst):
        os.remove(dst)
