import win32com.client
import os
import shutil

src = os.path.abspath('báo cáo/Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx')
dst = os.path.abspath('báo cáo/temp_inspect.docx')
shutil.copyfile(src, dst)

word = win32com.client.DispatchEx('Word.Application')
word.Visible = False
word.DisplayAlerts = False

try:
    doc = word.Documents.Open(dst, ReadOnly=True)
    
    keywords = [
        'BỘ GIÁO DỤC',
        'NHẬN XÉT CỦA CÁN BỘ HƯỚNG DẪN',
        'NHẬN XÉT CỦA CÁN BỘ PHẢN BIỆN',
        'TÓM TẮT ĐỒ ÁN',
        'LỜI CAM ĐOAN',
        'LỜI CẢM ƠN',
        'MỤC LỤC',
        'DANH MỤC CÁC TỪ VIẾT TẮT',
        'DANH MỤC CÁC HÌNH VẼ',
        'DANH MỤC CÁC BẢNG BIỂU',
        'MỞ ĐẦU',
        'CHƯƠNG 1',
        'CHƯƠNG 2',
        'CHƯƠNG 3',
        'KẾT LUẬN',
        'TÀI LIỆU THAM KHẢO',
        'PHỤ LỤC A',
        'PHỤ LỤC B'
    ]
    
    found_pages = {}
    for p in doc.Paragraphs:
        txt = p.Range.Text.strip()
        for kw in keywords:
            if kw in txt and kw not in found_pages:
                page = p.Range.Information(3) # wdActiveEndPageNumber
                sec = p.Range.Information(2)
                found_pages[kw] = (sec, page)
                break
                
    for kw, (s, pg) in found_pages.items():
        print(f"{kw.encode('ascii', 'ignore').decode()}: Sec {s}, Pg {pg}")
        
    doc.Close(False)
finally:
    word.Quit()
    if os.path.exists(dst):
        os.remove(dst)
