import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls
from doc_helpers import (
    add_p, add_h1, add_h2, add_h3, set_cell_background, set_cell_margins, set_table_borders
)

def build_cover_pages(doc):
    """
    Tạo trang Bìa ngoài và Bìa trong theo đúng quy chuẩn Đồ án Tốt nghiệp Đại học Phenikaa
    """
    sec_cover = doc.sections[0]
    sec_cover.header.is_linked_to_previous = False
    # Xóa header của trang bìa (không đánh số trang)
    for p in sec_cover.header.paragraphs:
        p.text = ""

    # Viền trang đôi nghệ thuật cho trang bìa theo chuẩn đồ án
    xml_border = '''<w:pgBorders xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" w:offsetFrom="page">
        <w:top w:val="double" w:sz="12" w:space="24" w:color="000000"/>
        <w:left w:val="double" w:sz="12" w:space="24" w:color="000000"/>
        <w:bottom w:val="double" w:sz="12" w:space="24" w:color="000000"/>
        <w:right w:val="double" w:sz="12" w:space="24" w:color="000000"/>
    </w:pgBorders>'''
    sec_cover._sectPr.append(parse_xml(xml_border))

    # --- TRANG BÌA CHÍNH (BÌA NGOÀI) ---
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run("BỘ GIÁO DỤC VÀ ĐÀO TẠO")
    run.font.name = 'Times New Roman'
    run.font.size = Pt(13)
    run.bold = True

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run("TRƯỜNG ĐẠI HỌC PHENIKAA\nKHOA CÔNG NGHỆ THÔNG TIN")
    run.font.name = 'Times New Roman'
    run.font.size = Pt(14)
    run.bold = True

    # Đường phân cách
    p_div = doc.add_paragraph()
    p_div.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_div.paragraph_format.space_after = Pt(36)
    r_div = p_div.add_run("-------------------***-------------------")
    r_div.font.name = 'Times New Roman'
    r_div.font.size = Pt(11)

    p_title_sub = doc.add_paragraph()
    p_title_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title_sub.paragraph_format.space_after = Pt(12)
    r_ts = p_title_sub.add_run("ĐỒ ÁN TỐT NGHIỆP ĐẠI HỌC")
    r_ts.font.name = 'Times New Roman'
    r_ts.font.size = Pt(16)
    r_ts.bold = True

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.line_spacing = 1.3
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r_t = p_title.add_run("XÂY DỰNG HỆ THỐNG QUẢN LÝ NHÀ HÀNG\nVÀ ĐẶT MÓN TẠI BÀN\n(RESTAURANT MANAGEMENT & QR ORDERING SYSTEM)")
    r_t.font.name = 'Times New Roman'
    r_t.font.size = Pt(17)
    r_t.bold = True

    p_spec = doc.add_paragraph()
    p_spec.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_spec.paragraph_format.space_after = Pt(50)
    r_spec = p_spec.add_run("Chuyên ngành: Kỹ thuật Phần mềm / Công nghệ Thông tin")
    r_spec.font.name = 'Times New Roman'
    r_spec.font.size = Pt(13)
    r_spec.italic = True

    # Bảng thông tin sinh viên & giảng viên
    tbl_info = doc.add_table(rows=5, cols=2)
    tbl_info.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_info.autofit = False
    col_widths = [Inches(2.35), Inches(3.45)]

    info_data = [
        ("Sinh viên thực hiện:", "NGUYỄN VĂN A"),
        ("Mã sinh viên:", "20010001"),
        ("Lớp khóa học:", "K14 - CNTT1"),
        ("Cán bộ hướng dẫn:", "TS. TRƯƠNG ĐỨC PHƯƠNG"),
        ("Cán bộ phản biện:", "........................................")
    ]

    for row_idx, (k, v) in enumerate(info_data):
        row = tbl_info.rows[row_idx]
        cell_lbl = row.cells[0]
        cell_val = row.cells[1]
        
        cell_lbl.width = col_widths[0]
        cell_val.width = col_widths[1]
        
        set_cell_margins(cell_lbl, top=40, bottom=40, left=60, right=60)
        set_cell_margins(cell_val, top=40, bottom=40, left=60, right=60)
        
        p_l = cell_lbl.paragraphs[0]
        p_l.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r_l = p_l.add_run(k)
        r_l.font.name = 'Times New Roman'
        r_l.font.size = Pt(13)
        r_l.bold = True
        
        p_v = cell_val.paragraphs[0]
        p_v.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r_v = p_v.add_run(v)
        r_v.font.name = 'Times New Roman'
        r_v.font.size = Pt(13)
        r_v.bold = (row_idx == 0 or row_idx == 3)

    # Ẩn viền bảng thông tin
    for row in tbl_info.rows:
        for cell in row.cells:
            tcPr = cell._tc.get_or_add_tcPr()
            tcBorders = parse_xml(f'<w:tcBorders {nsdecls("w")}><w:top w:val="none"/><w:left w:val="none"/><w:bottom w:val="none"/><w:right w:val="none"/></w:tcBorders>')
            tcPr.append(tcBorders)

    # Footer trang bìa
    p_ft = doc.add_paragraph()
    p_ft.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_ft.paragraph_format.space_before = Pt(70)
    p_ft.paragraph_format.space_after = Pt(0)
    r_ft = p_ft.add_run("HÀ NỘI - 2026")
    r_ft.font.name = 'Times New Roman'
    r_ft.font.size = Pt(13)
    r_ft.bold = True

    # --- TRANG BÌA PHỤ (BÌA TRONG) ---
    doc.add_page_break()

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run("BỘ GIÁO DỤC VÀ ĐÀO TẠO")
    run.font.name = 'Times New Roman'
    run.font.size = Pt(13)
    run.bold = True

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run("TRƯỜNG ĐẠI HỌC PHENIKAA\nKHOA CÔNG NGHỆ THÔNG TIN")
    run.font.name = 'Times New Roman'
    run.font.size = Pt(14)
    run.bold = True

    p_div = doc.add_paragraph()
    p_div.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_div.paragraph_format.space_after = Pt(36)
    r_div = p_div.add_run("-------------------***-------------------")
    r_div.font.name = 'Times New Roman'
    r_div.font.size = Pt(11)

    p_title_sub = doc.add_paragraph()
    p_title_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title_sub.paragraph_format.space_after = Pt(12)
    r_ts = p_title_sub.add_run("ĐỒ ÁN TỐT NGHIỆP ĐẠI HỌC")
    r_ts.font.name = 'Times New Roman'
    r_ts.font.size = Pt(16)
    r_ts.bold = True

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.line_spacing = 1.3
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r_t = p_title.add_run("XÂY DỰNG HỆ THỐNG QUẢN LÝ NHÀ HÀNG\nVÀ ĐẶT MÓN TẠI BÀN\n(RESTAURANT MANAGEMENT & QR ORDERING SYSTEM)")
    r_t.font.name = 'Times New Roman'
    r_t.font.size = Pt(17)
    r_t.bold = True

    p_spec = doc.add_paragraph()
    p_spec.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_spec.paragraph_format.space_after = Pt(50)
    r_spec = p_spec.add_run("Ngành: Công nghệ Thông tin\nMã số ngành: 7480201")
    r_spec.font.name = 'Times New Roman'
    r_spec.font.size = Pt(13)
    r_spec.italic = True

    tbl_info2 = doc.add_table(rows=5, cols=2)
    tbl_info2.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_info2.autofit = False

    for row_idx, (k, v) in enumerate(info_data):
        row = tbl_info2.rows[row_idx]
        cell_lbl = row.cells[0]
        cell_val = row.cells[1]
        
        cell_lbl.width = col_widths[0]
        cell_val.width = col_widths[1]
        
        set_cell_margins(cell_lbl, top=40, bottom=40, left=60, right=60)
        set_cell_margins(cell_val, top=40, bottom=40, left=60, right=60)
        
        p_l = cell_lbl.paragraphs[0]
        p_l.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r_l = p_l.add_run(k)
        r_l.font.name = 'Times New Roman'
        r_l.font.size = Pt(13)
        r_l.bold = True
        
        p_v = cell_val.paragraphs[0]
        p_v.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r_v = p_v.add_run(v)
        r_v.font.name = 'Times New Roman'
        r_v.font.size = Pt(13)
        r_v.bold = (row_idx == 0 or row_idx == 3)

    for row in tbl_info2.rows:
        for cell in row.cells:
            tcPr = cell._tc.get_or_add_tcPr()
            tcBorders = parse_xml(f'<w:tcBorders {nsdecls("w")}><w:top w:val="none"/><w:left w:val="none"/><w:bottom w:val="none"/><w:right w:val="none"/></w:tcBorders>')
            tcPr.append(tcBorders)

    p_ft2 = doc.add_paragraph()
    p_ft2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_ft2.paragraph_format.space_before = Pt(70)
    p_ft2.paragraph_format.space_after = Pt(0)
    r_ft2 = p_ft2.add_run("HÀ NỘI - 2026")
    r_ft2.font.name = 'Times New Roman'
    r_ft2.font.size = Pt(13)
    r_ft2.bold = True


def build_preliminary_pages(doc):
    """
    Tạo các tài liệu ngoài nội dung chính (Đánh số La Mã thường i, ii, iii... ở đầu trang):
    - Nhận xét của Cán bộ hướng dẫn
    - Nhận xét của Cán bộ phản biện
    - Tóm tắt ĐАКLTN (dưới 20 dòng Tiếng Việt + Tiếng Anh)
    - Lời cam đoan
    - Lời cảm ơn
    - Mục lục
    - Danh mục từ viết tắt
    - Danh mục hình ảnh
    - Danh mục bảng biểu
    """
    # Tạo Section mới cho phần mở đầu
    sec_prelim = doc.add_section()
    sec_prelim.header.is_linked_to_previous = False
    sec_prelim.top_margin = Inches(2.5 / 2.54)
    sec_prelim.bottom_margin = Inches(2.5 / 2.54)
    sec_prelim.left_margin = Inches(3.5 / 2.54)
    sec_prelim.right_margin = Inches(2.5 / 2.54)
    sec_prelim.page_width = Inches(21.0 / 2.54)
    sec_prelim.page_height = Inches(29.7 / 2.54)
    
    # Header: Số trang La Mã thường ở giữa đầu trang
    h = sec_prelim.header.paragraphs[0]
    h.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_num = h.add_run()
    r_num._r.append(parse_xml(f'<w:fldSimple {nsdecls("w")} w:instr="PAGE"/>'))
    r_num.font.name = 'Times New Roman'
    r_num.font.size = Pt(10)
    
    # Thiết lập số trang La Mã thường i, ii, iii... bắt đầu từ 1
    sec_prelim._sectPr.append(parse_xml(f'<w:pgNumType {nsdecls("w")} w:fmt="lowerRoman" w:start="1"/>'))

    # ==========================================
    # 1. NHẬN XÉT CỦA CÁN BỘ HƯỚNG DẪN
    # ==========================================
    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("NHẬN XÉT CỦA CÁN BỘ HƯỚNG DẪN")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    add_p(doc, "Họ và tên cán bộ hướng dẫn: TS. TRƯƠNG ĐỨC PHƯƠNG", bold=True, indent=0)
    add_p(doc, "Cơ quan công tác: Khoa Công nghệ Thông tin - Trường Đại học Phenikaa", indent=0)
    add_p(doc, "Họ và tên sinh viên thực hiện: NGUYỄN VĂN A   -   Mã sinh viên: 20010001", indent=0)
    add_p(doc, "Tên đề tài: Xây dựng hệ thống quản lý nhà hàng và đặt món tại bàn (Restaurant Management & QR Ordering System).", indent=0)
    add_p(doc, "Ngành đào tạo: Công nghệ Thông tin   -   Khóa: K14 (2022 - 2026)", indent=0)
    
    add_p(doc, "NỘI DUNG NHẬN XÉT VÀ ĐÁNH GIÁ:", bold=True, space_before=10, indent=0)
    add_p(doc, "1. Về tinh thần, thái độ làm việc của sinh viên trong quá trình thực hiện:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "2. Về khối lượng kiến thức và mức độ hoàn thành nội dung nghiên cứu:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "3. Về tính ứng dụng thực tiễn của sản phẩm phần mềm:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "4. Đánh giá kết quả và Điểm số của cán bộ hướng dẫn:")
    add_p(doc, "• Điểm đánh giá (thang điểm 10): .......... Điểm (Bằng chữ: ....................................................)")
    add_p(doc, "• Kết luận: [   ] Đồng ý cho sinh viên bảo vệ      [   ] Không đồng ý cho sinh viên bảo vệ")

    p_sign = doc.add_paragraph()
    p_sign.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sign.paragraph_format.space_before = Pt(20)
    p_sign.paragraph_format.space_after = Pt(4)
    r_s = p_sign.add_run("Hà Nội, ngày ..... tháng ..... năm 2026\nCÁN BỘ HƯỚNG DẪN\n(Ký và ghi rõ họ tên)\n\n\n\n\n")
    r_s.font.name = 'Times New Roman'
    r_s.font.size = Pt(13)
    r_s.bold = True
    r_name = p_sign.add_run("TS. Trương Đức Phương")
    r_name.font.name = 'Times New Roman'
    r_name.font.size = Pt(13)
    r_name.bold = True

    # ==========================================
    # 2. NHẬN XÉT CỦA CÁN BỘ PHẢN BIỆN
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("NHẬN XÉT CỦA CÁN BỘ PHẢN BIỆN")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    add_p(doc, "Họ và tên cán bộ phản biện: .....................................................................................", bold=True, indent=0)
    add_p(doc, "Cơ quan công tác: .........................................................................................................", indent=0)
    add_p(doc, "Họ và tên sinh viên thực hiện: NGUYỄN VĂN A   -   Mã sinh viên: 20010001", indent=0)
    add_p(doc, "Tên đề tài: Xây dựng hệ thống quản lý nhà hàng và đặt món tại bàn (Restaurant Management & QR Ordering System).", indent=0)
    
    add_p(doc, "NỘI DUNG NHẬN XÉT VÀ ĐÁNH GIÁ:", bold=True, space_before=10, indent=0)
    add_p(doc, "1. Tính cấp thiết và giá trị khoa học của đề tài:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "2. Đánh giá về kết cấu và hình thức trình bày bản báo cáo:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "3. Đánh giá chất lượng sản phẩm phần mềm và kết quả thực nghiệm:")
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, "4. Câu hỏi phản biện dành cho sinh viên tại phiên bảo vệ:")
    add_p(doc, "• Câu hỏi 1: .........................................................................................................................................")
    add_p(doc, "• Câu hỏi 2: .........................................................................................................................................")
    add_p(doc, "5. Kết luận và Điểm số của cán bộ phản biện:")
    add_p(doc, "• Điểm đánh giá (thang điểm 10): .......... Điểm (Bằng chữ: ....................................................)")
    add_p(doc, "• Kết luận: [   ] Đồng ý cho sinh viên bảo vệ trước Hội đồng      [   ] Không đồng ý")

    p_sign2 = doc.add_paragraph()
    p_sign2.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sign2.paragraph_format.space_before = Pt(20)
    p_sign2.paragraph_format.space_after = Pt(4)
    r_s2 = p_sign2.add_run("Hà Nội, ngày ..... tháng ..... năm 2026\nCÁN BỘ PHẢN BIỆN\n(Ký và ghi rõ họ tên)\n\n\n\n\n")
    r_s2.font.name = 'Times New Roman'
    r_s2.font.size = Pt(13)
    r_s2.bold = True
    r_name2 = p_sign2.add_run("..................................................")
    r_name2.font.name = 'Times New Roman'
    r_name2.font.size = Pt(13)
    r_name2.bold = True

    # ==========================================
    # 2b. BIÊN BẢN HỘI ĐỒNG BẢO VỆ ĐỒ ÁN TỐT NGHIỆP
    # ==========================================
    doc.add_page_break()

    p_title_bb = doc.add_paragraph()
    p_title_bb.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title_bb.paragraph_format.space_before = Pt(12)
    p_title_bb.paragraph_format.space_after = Pt(18)
    r_bb = p_title_bb.add_run("BIÊN BẢN HỘI ĐỒNG CHẤM ĐỒ ÁN TỐT NGHIỆP")
    r_bb.font.name = 'Times New Roman'
    r_bb.font.size = Pt(15)
    r_bb.bold = True

    add_p(doc, "Thời gian bảo vệ: Ngày ..... tháng ..... năm 2026", indent=0)
    add_p(doc, "Địa điểm bảo vệ: Phòng .........., Trường Đại học Phenikaa", indent=0)
    add_p(doc, "Hội đồng chấm Đồ án tốt nghiệp theo Quyết định số: .............../QĐ-ĐHP-ĐT", indent=0)
    add_p(doc, "Thành phần Hội đồng:", bold=True, space_before=6, indent=0)
    add_p(doc, "1. Chủ tịch Hội đồng: ........................................................................ - Cơ quan: .....................................................")
    add_p(doc, "2. Thư ký Hội đồng: .......................................................................... - Cơ quan: .....................................................")
    add_p(doc, "3. Ủy viên Phản biện: ........................................................................ - Cơ quan: .....................................................")
    add_p(doc, "4. Ủy viên: ......................................................................................... - Cơ quan: .....................................................")
    add_p(doc, "Sinh viên bảo vệ: NGUYỄN VĂN A - Mã sinh viên: 20010001", indent=0)
    add_p(doc, "Tên đề tài: Xây dựng hệ thống quản lý nhà hàng và đặt món tại bàn (Restaurant Management & QR Ordering System).", indent=0)
    add_p(doc, "Kết quả chấm của Hội đồng:", bold=True, space_before=6, indent=0)
    add_p(doc, "• Điểm tổng kết Đồ án tốt nghiệp: .......... / 10.0 (Bằng chữ: .............................................................................)")
    add_p(doc, "• Xếp loại tốt nghiệp: .....................................................................................................................................")
    add_p(doc, "• Ý kiến kết luận và yêu cầu chỉnh sửa của Hội đồng:", bold=True, indent=0)
    add_p(doc, ".................................................................................................................................................................")
    add_p(doc, ".................................................................................................................................................................")

    tbl_sign_bb = doc.add_table(rows=1, cols=2)
    tbl_sign_bb.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell_tk = tbl_sign_bb.rows[0].cells[0]
    cell_ct = tbl_sign_bb.rows[0].cells[1]
    cell_tk.width = Inches(2.9)
    cell_ct.width = Inches(2.9)

    p_tk = cell_tk.paragraphs[0]
    p_tk.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_tk = p_tk.add_run("THƯ KÝ HỘI ĐỒNG\n(Ký và ghi rõ họ tên)\n\n\n\n\n..................................................")
    r_tk.font.name = 'Times New Roman'
    r_tk.font.size = Pt(12)
    r_tk.bold = True

    p_ct = cell_ct.paragraphs[0]
    p_ct.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_ct = p_ct.add_run("CHỦ TỊCH HỘI ĐỒNG\n(Ký và ghi rõ họ tên)\n\n\n\n\n..................................................")
    r_ct.font.name = 'Times New Roman'
    r_ct.font.size = Pt(12)
    r_ct.bold = True

    for row in tbl_sign_bb.rows:
        for cell in row.cells:
            tcPr = cell._tc.get_or_add_tcPr()
            tcBorders = parse_xml(f'<w:tcBorders {nsdecls("w")}><w:top w:val="none"/><w:left w:val="none"/><w:bottom w:val="none"/><w:right w:val="none"/></w:tcBorders>')
            tcPr.append(tcBorders)

    # ==========================================
    # 2c. BẢN GIẢI TRÌNH CHỈNH SỬA THEO Ý KIẾN HỘI ĐỒNG
    # ==========================================
    doc.add_page_break()

    p_title_gt = doc.add_paragraph()
    p_title_gt.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title_gt.paragraph_format.space_before = Pt(12)
    p_title_gt.paragraph_format.space_after = Pt(18)
    r_gt = p_title_gt.add_run("BẢN GIẢI TRÌNH CHỈNH SỬA ĐỒ ÁN TỐT NGHIỆP\nTHEO Ý KIẾN CỦA HỘI ĐỒNG CHẤM")
    r_gt.font.name = 'Times New Roman'
    r_gt.font.size = Pt(15)
    r_gt.bold = True

    add_p(doc, "Kính gửi: Hội đồng chấm Đồ án tốt nghiệp Khoa Công nghệ Thông tin - Trường Đại học Phenikaa.", italic=True, indent=0)
    add_p(doc, "Tên tôi là: NGUYỄN VĂN A   -   Mã sinh viên: 20010001", indent=0)
    add_p(doc, "Tên đề tài: Xây dựng hệ thống quản lý nhà hàng và đặt món tại bàn (Restaurant Management & QR Ordering System).", indent=0)
    add_p(doc, "Cán bộ hướng dẫn: TS. TRƯƠNG ĐỨC PHƯƠNG", indent=0)
    add_p(doc, "Sau phiên bảo vệ ngày ...../...../2026, căn cứ vào Biên bản họp Hội đồng và các ý kiến đóng góp của các Thầy/Cô, sinh viên đã nghiêm túc tiếp thu và chỉnh sửa, hoàn thiện bản báo cáo đồ án như sau:", space_before=6, indent=0)

    tbl_gt = doc.add_table(rows=4, cols=3)
    tbl_gt.alignment = WD_TABLE_ALIGNMENT.CENTER
    col_w_gt = [Inches(0.5), Inches(2.65), Inches(2.65)]
    headers_gt = ["STT", "Nội dung yêu cầu chỉnh sửa của Hội đồng", "Nội dung sinh viên đã chỉnh sửa và bổ sung"]
    for c_idx, h_text in enumerate(headers_gt):
        cell = tbl_gt.rows[0].cells[c_idx]
        cell.width = col_w_gt[c_idx]
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(h_text)
        r.font.name = 'Times New Roman'
        r.font.size = Pt(11)
        r.bold = True
        set_cell_background(cell, 'F1F5F9')

    gt_rows = [
        ("1", "Rà soát tính nhất quán về phiên bản công nghệ (.NET, C#, EF Core).", "Đã thống nhất toàn bộ tài liệu theo chuẩn .NET 9, C# 13, EF Core 9."),
        ("2", "Chuẩn hóa các biểu mẫu đánh giá, điểm số và số liệu kiểm thử thực tế.", "Đã chuẩn hóa biểu mẫu nhận xét và cập nhật 334 test cases tự động."),
        ("3", "Bổ sung hình ảnh giao diện thực tế và sơ đồ phân rã chức năng.", "Đã bổ sung giao diện 4 phân hệ và sơ đồ FDD, Deployment Diagram.")
    ]
    for r_idx, (stt, req_text, fix_text) in enumerate(gt_rows, start=1):
        row = tbl_gt.rows[r_idx]
        for c_idx, val in enumerate([stt, req_text, fix_text]):
            cell = row.cells[c_idx]
            cell.width = col_w_gt[c_idx]
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if c_idx == 0 else WD_ALIGN_PARAGRAPH.LEFT
            r = p.add_run(val)
            r.font.name = 'Times New Roman'
            r.font.size = Pt(11)

    set_table_borders(tbl_gt)

    p_sign_gt = doc.add_paragraph()
    p_sign_gt.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sign_gt.paragraph_format.space_before = Pt(20)
    p_sign_gt.paragraph_format.space_after = Pt(4)
    r_sgt = p_sign_gt.add_run("Hà Nội, ngày ..... tháng ..... năm 2026\nSINH VIÊN THỰC HIỆN\n(Ký và ghi rõ họ tên)\n\n\n\n\nNGUYỄN VĂN A")
    r_sgt.font.name = 'Times New Roman'
    r_sgt.font.size = Pt(12)
    r_sgt.bold = True

    # ==========================================
    # 3. TÓM TẮT ĐỒ ÁN TỐT NGHIỆP
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(16)
    r = p_title.add_run("TÓM TẮT ĐỒ ÁN TỐT NGHIỆP")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    # Bản tiếng Việt (dưới 20 dòng theo đúng Quy chế Phenikaa)
    add_p(doc, "Đồ án tốt nghiệp tập trung nghiên cứu, thiết kế và phát triển toàn diện Hệ thống Quản lý Nhà hàng và Đặt món tại bàn (Restaurant Management & QR Ordering System) nhằm giải quyết bài toán chuyển đổi số khép kín cho ngành dịch vụ ẩm thực (F&B). Hệ thống áp dụng kiến trúc Clean Architecture kết hợp mô hình Domain-Driven Design trên nền tảng .NET 9, Blazor Server, Entity Framework Core 9 và hệ quản trị Microsoft SQL Server 2022. Giải pháp tích hợp trọn vẹn 4 phân hệ vận hành cốt lõi: (1) Ứng dụng đặt món tự phục vụ tại bàn qua mã QR động (QR Ordering) không cần cài đặt; (2) Màn hình bán hàng thu ngân (POS) quản lý trực quan sơ đồ bàn sảnh; (3) Hệ thống điều phối hiển thị nhà bếp (KDS) đồng bộ trạng thái thời gian thực thông qua SignalR Core; (4) Phân hệ kiểm soát định mức nguyên vật liệu (BOM) trừ kho tự động khi món nấu xong; cùng phân hệ nền tảng quản lý tài khoản, phân quyền RBAC và đối soát thanh toán tự động qua Webhook VietQR (SePay). Kết quả kiểm thử thực nghiệm khẳng định hệ thống vượt qua 334/334 ca kiểm thử tự động (100% Passed) với nền tảng xUnit, đảm bảo độ toàn vẹn dữ liệu giao dịch và khả năng phản hồi thời gian thực ổn định.")
    add_p(doc, "Từ khóa: Quản lý nhà hàng; QR Ordering; Điểm bán hàng (POS); Hệ thống màn hình bếp (KDS); SignalR; Clean Architecture; Định mức kho (BOM); VietQR SePay; .NET 9.", italic=True, space_before=6)

    # Bản tiếng Anh (ABSTRACT)
    p_abs = doc.add_paragraph()
    p_abs.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_abs.paragraph_format.space_before = Pt(18)
    p_abs.paragraph_format.space_after = Pt(12)
    r_abs = p_abs.add_run("ABSTRACT")
    r_abs.font.name = 'Times New Roman'
    r_abs.font.size = Pt(14)
    r_abs.bold = True

    add_p(doc, "This graduation thesis focuses on the comprehensive research, design, and implementation of a full-stack Restaurant Management and Table QR Ordering System tailored to digitalize the Food and Beverage (F&B) industry. Built upon Clean Architecture and Domain-Driven Design principles using .NET 9, C# 13, Blazor Server, Entity Framework Core 9, and Microsoft SQL Server 2022, the system seamlessly integrates four mission-critical operational modules: (1) Frictionless dynamic Table QR Ordering for guests requiring zero app installation; (2) An interactive Point of Sale (POS) console with real-time floor mapping; (3) A Kitchen Display System (KDS) synchronized instantaneously via SignalR Core; (4) Automated Bill of Materials (BOM) inventory depletion upon cooking completion; supported by a robust cross-cutting foundation for RBAC security, cashier shifts, and instant cashless reconciliation leveraging SePay VietQR dynamic webhooks. Comprehensive automated testing with 334 test cases (100% Passed) confirms high operational reliability, strict transaction integrity, and low-latency real-time event broadcasting.")
    add_p(doc, "Keywords: Restaurant Management; QR Ordering; Point of Sale (POS); Kitchen Display System (KDS); SignalR; Clean Architecture; Bill of Materials (BOM); VietQR Webhook; .NET 9.", italic=True, space_before=6)

    # ==========================================
    # 4. LỜI CAM ĐOAN
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("LỜI CAM ĐOAN")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    add_p(doc, "Tôi xin cam đoan bản Đồ án tốt nghiệp với đề tài “Xây dựng hệ thống quản lý nhà hàng và đặt món tại bàn (Restaurant Management & QR Ordering System)” là công trình nghiên cứu khoa học và phát triển sản phẩm phần mềm độc lập do chính tôi thực hiện dưới sự định hướng, hướng dẫn chuyên môn tận tình của TS. Trương Đức Phương.")
    add_p(doc, "Tất cả các số liệu khảo sát thực tế, mô hình kiến trúc, lược đồ cơ sở dữ liệu và mã nguồn chương trình được trình bày trong báo cáo này hoàn toàn trung thực, phản ánh chính xác kết quả xây dựng hệ thống và chưa từng được công bố hay sao chép từ bất kỳ công trình nghiên cứu nào của các tác giả khác.")
    add_p(doc, "Các tài liệu tham khảo, thư viện phần mềm mã nguồn mở và nền tảng công nghệ được sử dụng trong đồ án đều đã được trích dẫn nguồn gốc xuất xứ rõ ràng và đầy đủ theo đúng quy chuẩn học thuật của Trường Đại học Phenikaa.")
    add_p(doc, "Nếu có bất kỳ sự gian lận hay sao chép trái phép nào được phát hiện, tôi xin chịu hoàn toàn trách nhiệm kỷ luật trước Nhà trường và pháp luật.")

    p_sign3 = doc.add_paragraph()
    p_sign3.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sign3.paragraph_format.space_before = Pt(30)
    p_sign3.paragraph_format.space_after = Pt(4)
    r_s3 = p_sign3.add_run("Hà Nội, ngày 20 tháng 06 năm 2026\nSINH VIÊN THỰC HIỆN\n\n\n\n")
    r_s3.font.name = 'Times New Roman'
    r_s3.font.size = Pt(13)
    r_s3.bold = True
    r_name3 = p_sign3.add_run("Nguyễn Văn A")
    r_name3.font.name = 'Times New Roman'
    r_name3.font.size = Pt(13)
    r_name3.bold = True

    # ==========================================
    # 5. LỜI CẢM ƠN
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("LỜI CẢM ƠN")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    add_p(doc, "Lời đầu tiên, em xin được bày tỏ lòng biết ơn sâu sắc và chân thành nhất tới Ban Giám hiệu Trường Đại học Phenikaa, cùng toàn thể quý Thầy, Cô giáo Khoa Công nghệ Thông tin. Trong suốt 4 năm học tập và rèn luyện dưới mái trường, các Thầy Cô đã tận tâm truyền đạt nền tảng kiến thức khoa học máy tính vững chắc, tư duy kỹ thuật và những kinh nghiệm quý báu để em có đủ hành trang tự tin bước vào môi trường làm việc thực tế.")
    add_p(doc, "Đặc biệt, em xin gửi lời cảm ơn trân trọng và sâu sắc nhất tới TS. Trương Đức Phương - Giảng viên trực tiếp hướng dẫn đồ án. Thầy đã luôn dành nhiều thời gian quý báu, tận tình định hướng phương pháp tiếp cận, truyền cảm hứng nghiên cứu và đưa ra những lời khuyên chuyên môn sâu sắc giúp em tháo gỡ nhiều vướng mắc kỹ thuật phức tạp trong suốt quá trình triển khai đề tài.")
    add_p(doc, "Em cũng xin trân trọng gửi lời cảm ơn tới quý Thầy, Cô cán bộ phản biện và các Thầy, Cô trong Hội đồng chấm Đồ án tốt nghiệp đã dành thời gian đọc duyệt kỹ lưỡng, đóng góp những ý kiến học thuật sâu sắc giúp em hoàn thiện bản báo cáo đồ án một cách chuẩn mực và toàn diện nhất.")
    add_p(doc, "Cuối cùng, con xin gửi lời tri ân vô hạn tới Bố Mẹ và những người thân trong gia đình đã luôn là điểm tựa vững chắc, hết lòng động viên và tạo mọi điều kiện tốt nhất cho con trong suốt chặng đường học tập. Xin cảm ơn các bạn bè đồng khóa đã luôn đồng hành, chia sẻ kiến thức và hỗ trợ em hoàn thành tốt đồ án tốt nghiệp này.")

    p_sign4 = doc.add_paragraph()
    p_sign4.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sign4.paragraph_format.space_before = Pt(30)
    p_sign4.paragraph_format.space_after = Pt(4)
    r_s4 = p_sign4.add_run("Hà Nội, ngày 20 tháng 06 năm 2026\nSinh viên thực hiện\n\n\n\n")
    r_s4.font.name = 'Times New Roman'
    r_s4.font.size = Pt(13)
    r_s4.italic = True
    r_name4 = p_sign4.add_run("Nguyễn Văn A")
    r_name4.font.name = 'Times New Roman'
    r_name4.font.size = Pt(13)
    r_name4.bold = True

    # ==========================================
    # 6. MỤC LỤC (TABLE OF CONTENTS)
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("MỤC LỤC")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    # Bảng mục lục có số trang và tab leader chấm
    toc_items = [
        ("LỜI CAM ĐOAN", "vii", True, 0),
        ("LỜI CẢM ƠN", "viii", True, 0),
        ("TÓM TẮT ĐỒ ÁN", "ix", True, 0),
        ("MỤC LỤC", "x", True, 0),
        ("DANH MỤC CÁC TỪ VIẾT TẮT", "xi", True, 0),
        ("DANH MỤC CÁC HÌNH VẼ, ĐỒ THỊ", "xii", True, 0),
        ("DANH MỤC CÁC BẢNG BIỂU", "xiv", True, 0),
        ("MỞ ĐẦU", "1", True, 0),
        ("CHƯƠNG 1. TỔNG QUAN TÀI LIỆU VÀ CƠ SỞ LÝ LUẬN VỀ HỆ THỐNG QUẢN LÝ NHÀ HÀNG", "8", True, 0),
        ("1.1. Cơ sở lý luận về quy trình vận hành và quản lý nhà hàng F&B", "8", False, 1),
        ("1.2. Khảo sát thực trạng quy trình vận hành tại các mô hình nhà hàng ẩm thực", "11", False, 1),
        ("1.3. Tổng quan các hướng nghiên cứu và giải pháp hiện có", "12", False, 1),
        ("1.4. Đặc tả yêu cầu hệ thống phần mềm (Software Requirements Specification - SRS)", "15", False, 1),
        ("1.5. Cơ sở công nghệ và giải pháp kỹ thuật đề xuất", "19", False, 1),
        ("CHƯƠNG 2. PHƯƠNG PHÁP NGHIÊN CỨU VÀ THIẾT KẾ KIẾN TRÚC HỆ THỐNG", "21", True, 0),
        ("2.1. Phương pháp nghiên cứu và quy trình phát triển phần mềm", "21", False, 1),
        ("2.2. Phân tích yêu cầu chức năng và phi chức năng", "22", False, 1),
        ("2.3. Phân tích Ca sử dụng (Use Case Analysis)", "24", False, 1),
        ("2.3.1. Biểu đồ Use Case Tổng thể Toàn Hệ thống", "25", False, 2),
        ("2.3.2. Biểu đồ Use Case Phân rã theo từng Phân hệ Nghiệp vụ", "27", False, 2),
        ("2.3.3. Bảng Đặc tả Chi tiết các Ca sử dụng Cốt lõi (Use Case Specifications)", "29", False, 2),
        ("2.4. Thiết kế Hướng đối tượng và Biểu đồ Lớp (Class Diagrams)", "32", False, 1),
        ("2.4.1. Biểu đồ Lớp Phân hệ Đơn hàng và Thanh toán", "32", False, 2),
        ("2.4.2. Biểu đồ Lớp Phân hệ Thực đơn và Nhóm Tùy chọn", "34", False, 2),
        ("2.4.3. Biểu đồ Lớp Phân hệ Kho hàng và Định mức BOM", "36", False, 2),
        ("2.4.4. Biểu đồ Lớp Phân hệ Ca làm việc và Quản lý Tiền két", "37", False, 2),
        ("2.5. Thiết kế kiến trúc tổng thể hệ thống (Clean Architecture)", "39", False, 1),
        ("2.5.1. Sơ đồ Kiến trúc Phân tầng Clean Architecture", "39", False, 2),
        ("2.5.2. Sơ đồ Tuần tự Xử lý Yêu cầu (Request-Response Flow)", "40", False, 2),
        ("2.6. Thiết kế các Sơ đồ Trạng thái và Luồng Nghiệp vụ Realtime", "42", False, 1),
        ("2.6.1. Sơ đồ Tuần tự Luồng Đặt món và Đồng bộ Realtime", "42", False, 2),
        ("2.6.2. Sơ đồ Chuyển đổi Trạng thái Bàn ăn (Table State Machine)", "43", False, 2),
        ("2.6.3. Sơ đồ Chuyển đổi Trạng thái Đơn hàng và Món ăn", "44", False, 2),
        ("2.6.4. Sơ đồ Trạng thái Thanh toán (Payment State Machine)", "45", False, 2),
        ("2.6.5. Sơ đồ Trạng thái Ca làm việc và Đợt kiểm kê Kho", "46", False, 2),
        ("2.7. Thiết kế Sơ đồ Thực thể Quan hệ ERD (Trích xuất từ CSDL)", "47", False, 1),
        ("2.8. Thiết kế Cấu trúc Thành phần Giao diện (UI/UX Component Architecture)", "53", False, 1),
        ("2.9. Đặc tả Kỹ thuật và Hiện thực hóa Chi tiết 70 Chức năng Hệ thống", "54", False, 1),
        ("2.10. Đặc tả Chi tiết Lược đồ Thực thể Cơ sở Dữ liệu (42 DbSets)", "64", False, 1),
        ("CHƯƠNG 3. HIỆN THỰC HÓA VÀ ĐÁNH GIÁ KẾT QUẢ ĐẠT ĐƯỢC", "67", True, 0),
        ("3.1. Môi trường phát triển và cấu hình công nghệ", "67", False, 1),
        ("3.2. Hiện thực hóa các thuật toán và logic nghiệp vụ cốt lõi", "68", False, 1),
        ("3.2.1. Cơ chế Kiểm soát Tương tranh Lạc quan với RowVersion", "68", False, 2),
        ("3.2.2. Xử lý Webhook SePay Idempotent và Đối soát VietQR Tự động", "70", False, 2),
        ("3.2.3. Cơ chế Định tuyến Nhóm Kết nối SignalR Hub", "71", False, 2),
        ("3.2.4. Thuật toán Tự động Khấu trừ Tồn kho Đa tầng theo Định mức BOM", "72", False, 2),
        ("3.2.5. Cơ chế Bảo mật Mã PIN Quản lý với BCrypt và Giới hạn Đăng nhập Sai", "73", False, 2),
        ("3.3. Hiện thực hóa các giao diện tương tác cốt lõi", "74", False, 1),
        ("3.4. Kiểm thử hệ thống và đánh giá hiệu năng toàn diện", "76", False, 1),
        ("3.4.1. Kết quả Kiểm thử Tự động với Framework xUnit", "76", False, 2),
        ("3.4.2. Bảng Kịch bản Kiểm thử Tích hợp Chi tiết (Integration Tests)", "76", False, 2),
        ("3.4.3. Đánh giá Hiệu năng và Khả năng Chịu tải với k6", "78", False, 2),
        ("3.4.4. Đo lường Độ trễ Giao tiếp Realtime SignalR Core", "79", False, 2),
        ("3.5. Đánh giá kết quả đạt được so với mục tiêu ban đầu", "79", False, 1),
        ("KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN", "80", True, 0),
        ("TÀI LIỆU THAM KHẢO", "83", True, 0),
        ("PHỤ LỤC", "84", True, 0),
        ("Phụ lục A: Hướng dẫn Cài đặt, Cấu hình và Triển khai với Docker Compose", "84", False, 1),
        ("Phụ lục B: Đặc tả Chi tiết RESTful API và Cấu hình Bảo mật", "87", False, 1),
        ("Phụ lục C: Danh mục Mã lỗi Chuẩn hóa RFC 7807 ProblemDetails", "90", False, 1)
    ]

    for title, pg, is_bold, lvl in toc_items:
        p_toc = doc.add_paragraph()
        p_toc.paragraph_format.line_spacing = 1.2
        p_toc.paragraph_format.space_before = Pt(3 if is_bold else 1)
        p_toc.paragraph_format.space_after = Pt(2)
        indent_in = 0.0 if lvl == 0 else (0.25 if lvl == 1 else 0.5)
        p_toc.paragraph_format.left_indent = Inches(indent_in)

        # Tab stop căn phải với chấm leader
        p_toc.paragraph_format.tab_stops.add_tab_stop(Inches(5.9), docx.enum.text.WD_TAB_ALIGNMENT.RIGHT, docx.enum.text.WD_TAB_LEADER.DOTS)
        
        r_txt = p_toc.add_run(title)
        r_txt.font.name = 'Times New Roman'
        r_txt.font.size = Pt(12)
        r_txt.bold = is_bold
        
        r_tab = p_toc.add_run("\t")
        
        r_pg = p_toc.add_run(pg)
        r_pg.font.name = 'Times New Roman'
        r_pg.font.size = Pt(12)
        r_pg.bold = is_bold

    # ==========================================
    # 7. DANH MỤC CÁC TỪ VIẾT TẮT
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("DANH MỤC CÁC TỪ VIẾT TẮT")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    abbreviations = [
        ("POS", "Point of Sale", "Hệ thống / Màn hình điểm bán hàng tại quầy thu ngân"),
        ("KDS", "Kitchen Display System", "Hệ thống màn hình hiển thị và điều phối nhà bếp thời gian thực"),
        ("QR", "Quick Response Code", "Mã phản hồi nhanh dùng để quét gọi món trên thiết bị di động"),
        ("BOM", "Bill of Materials", "Bảng định mức tiêu hao nguyên vật liệu sản xuất của món ăn"),
        ("API", "Application Programming Interface", "Giao diện lập trình ứng dụng giao tiếp giữa Client và Server"),
        ("REST", "Representational State Transfer", "Kiểu kiến trúc thiết kế dịch vụ web truyền tải dữ liệu"),
        ("JWT", "JSON Web Token", "Chuẩn mã hóa thông tin xác thực an toàn giữa các bên"),
        ("RBAC", "Role-Based Access Control", "Mô hình kiểm soát truy cập và phân quyền dựa trên vai trò"),
        ("EF Core", "Entity Framework Core", "Bộ thư viện ánh xạ đối tượng - quan hệ (ORM) cho .NET"),
        ("ORM", "Object-Relational Mapping", "Kỹ thuật ánh xạ dữ liệu bảng trong RDBMS sang đối tượng mã nguồn"),
        ("DTO", "Data Transfer Object", "Đối tượng truyền tải dữ liệu giữa các tầng kiến trúc"),
        ("ACID", "Atomicity, Consistency, Isolation, Durability", "Bốn thuộc tính bảo đảm tính toàn vẹn của giao dịch CSDL"),
        ("3NF", "Third Normal Form", "Dạng chuẩn 3 trong thiết kế chuẩn hóa cơ sở dữ liệu"),
        ("VietQR", "Vietnam Quick Response", "Chuẩn nhận diện thanh toán mã QR quốc gia của Napas"),
        ("BOH", "Back of House", "Khu vực nghiệp vụ hậu sảnh (nhà bếp, quản lý kho, kế toán)"),
        ("FOH", "Front of House", "Khu vực phục vụ tiền sảnh (sảnh nhà hàng, bàn ăn, quầy thu ngân)"),
        ("Z-Report", "End-of-day Cash Register Report", "Báo cáo chốt số liệu tài chính và tiền két khi đóng ca")
    ]

    tbl_abbr = doc.add_table(rows=len(abbreviations) + 1, cols=4)
    tbl_abbr.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_abbr.autofit = False

    col_widths_abbr = [Inches(0.5), Inches(1.0), Inches(2.0), Inches(2.3)]
    headers_abbr = ["STT", "Từ viết tắt", "Thuật ngữ tiếng Anh", "Ý nghĩa / Diễn giải tiếng Việt"]

    for col_idx, h_text in enumerate(headers_abbr):
        cell = tbl_abbr.rows[0].cells[col_idx]
        cell.width = col_widths_abbr[col_idx]
        set_cell_background(cell, "f1f5f9")
        set_cell_margins(cell, top=35, bottom=35, left=50, right=50)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.line_spacing = 1.0
        p.paragraph_format.space_before = Pt(1)
        p.paragraph_format.space_after = Pt(1)
        run = p.add_run(h_text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(10)

    for row_idx, (abbr, en, vi) in enumerate(abbreviations):
        row = tbl_abbr.rows[row_idx + 1]
        data = [str(row_idx + 1), abbr, en, vi]
        for col_idx, val in enumerate(data):
            cell = row.cells[col_idx]
            cell.width = col_widths_abbr[col_idx]
            set_cell_margins(cell, top=20, bottom=20, left=45, right=45)
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if col_idx < 2 else WD_ALIGN_PARAGRAPH.LEFT
            p.paragraph_format.line_spacing = 1.0
            p.paragraph_format.space_before = Pt(1)
            p.paragraph_format.space_after = Pt(1)
            run = p.add_run(val)
            run.font.name = 'Times New Roman'
            run.font.size = Pt(9.0)
            if col_idx == 1:
                run.bold = True

    set_table_borders(tbl_abbr)

    # ==========================================
    # 8. DANH MỤC CÁC HÌNH VẼ, ĐỒ THỊ
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("DANH MỤC CÁC HÌNH VẼ, ĐỒ THỊ")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    figures_list = [
        ("Hình 2.1", "Biểu đồ Use Case Tổng thể Toàn Hệ thống Quản lý Nhà hàng", "26"),
        ("Hình 2.2a", "Biểu đồ Use Case Phân rã Phân hệ Tự gọi món QR Ordering", "27"),
        ("Hình 2.2b", "Biểu đồ Use Case Phân rã Phân hệ Bán hàng POS tại Quầy", "28"),
        ("Hình 2.2c", "Biểu đồ Use Case Phân rã Phân hệ Màn hình Nhà bếp KDS", "28"),
        ("Hình 2.2d", "Biểu đồ Use Case Phân rã Phân hệ Quản trị Hậu sảnh BOH Admin", "29"),
        ("Hình 2.3a", "Biểu đồ lớp phân hệ Đơn hàng, Dòng món và Giao dịch Thanh toán", "33"),
        ("Hình 2.3b", "Biểu đồ lớp phân hệ Danh mục, Món ăn và Nhóm Tùy chọn Modifier", "35"),
        ("Hình 2.3c", "Biểu đồ lớp phân hệ Nguyên vật liệu, Công thức Định lượng BOM và Giao dịch Kho", "36"),
        ("Hình 2.3d", "Biểu đồ lớp phân hệ Ca làm việc, Dòng tiền két và Báo cáo Z-Report", "38"),
        ("Hình 2.4", "Sơ đồ kiến trúc Clean Architecture phân tầng phụ thuộc", "39"),
        ("Hình 2.5", "Sơ đồ tuần tự xử lý yêu cầu hệ thống từ Client đến Database", "41"),
        ("Hình 2.6", "Sơ đồ tuần tự luồng đặt món QR, duyệt POS và chế biến KDS qua SignalR", "42"),
        ("Hình 2.7", "Sơ đồ chuyển đổi trạng thái Bàn ăn Table State Machine", "43"),
        ("Hình 2.8", "Sơ đồ chuyển đổi trạng thái Đơn hàng Order State Machine dạng ngang thoáng", "44"),
        ("Hình 2.9", "Sơ đồ chuyển đổi trạng thái Món ăn trong Bếp OrderItem State Machine dạng ngang thoáng", "45"),
        ("Hình 2.10", "Sơ đồ máy trạng thái Thanh toán Payment State Machine", "45"),
        ("Hình 2.11", "Sơ đồ máy trạng thái Ca làm việc Shift State Machine", "46"),
        ("Hình 2.12", "Sơ đồ máy trạng thái Đợt kiểm kê Kho Stocktake State Machine", "46"),
        ("Hình 2.13", "Sơ đồ ERD phân hệ Người dùng, Phân quyền và Bàn ăn", "47"),
        ("Hình 2.14a", "ERD Danh mục & Món ăn", "48"),
        ("Hình 2.14b", "ERD Tùy chọn Modifiers & Topping", "48"),
        ("Hình 2.15a", "Sơ đồ ERD Đơn hàng, Dòng món chi tiết và Nhật ký Hủy món", "49"),
        ("Hình 2.15b", "ERD Thanh toán & Webhook SePay", "50"),
        ("Hình 2.15c", "ERD Ca làm việc & Dòng tiền két", "50"),
        ("Hình 2.16", "Sơ đồ ERD phân hệ Khách hàng Thân thiết và Tích điểm", "51"),
        ("Hình 2.17", "Sơ đồ ERD phân hệ Kho hàng và Định mức BOM", "52"),
        ("Hình 2.18", "Sơ đồ tổng thể luồng dữ liệu liên module", "53")
    ]

    for fig_id, fig_title, pg in figures_list:
        p_fig = doc.add_paragraph()
        p_fig.paragraph_format.line_spacing = 1.2
        p_fig.paragraph_format.space_before = Pt(2)
        p_fig.paragraph_format.space_after = Pt(2)
        p_fig.paragraph_format.tab_stops.add_tab_stop(Inches(5.9), docx.enum.text.WD_TAB_ALIGNMENT.RIGHT, docx.enum.text.WD_TAB_LEADER.DOTS)
        
        r_id = p_fig.add_run(f"{fig_id}: ")
        r_id.font.name = 'Times New Roman'
        r_id.font.size = Pt(12)
        r_id.bold = True
        
        r_t = p_fig.add_run(fig_title)
        r_t.font.name = 'Times New Roman'
        r_t.font.size = Pt(12)
        
        r_tab = p_fig.add_run("\t")
        
        r_pg = p_fig.add_run(pg)
        r_pg.font.name = 'Times New Roman'
        r_pg.font.size = Pt(12)
        r_pg.bold = True

    # ==========================================
    # 9. DANH MỤC CÁC BẢNG BIỂU
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("DANH MỤC CÁC BẢNG BIỂU")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    tables_list = [
        ("Bảng 1.1", "Ma trận so sánh chuyên sâu giữa các giải pháp thương mại và Hệ thống Đề tài", "14"),
        ("Bảng 1.2a", "Bảng đặc tả Yêu cầu Chức năng Phân hệ QR Ordering và POS (FR-01 đến FR-13)", "16"),
        ("Bảng 1.2b", "Bảng đặc tả Yêu cầu Chức năng Phân hệ KDS, BOM và Quản trị (FR-14 đến FR-25)", "17"),
        ("Bảng 1.3", "Bảng đặc tả Yêu cầu Phi chức năng Hệ thống (Non-Functional Requirements - NFR)", "18"),
        ("Bảng 1.4", "Ma trận Phân quyền và Trách nhiệm RACI trong Vận hành Nhà hàng", "19"),
        ("Bảng 2.1", "Đặc tả Use Case UC-01: Đặt món tại bàn qua mã QR (QR Ordering)", "29"),
        ("Bảng 2.2", "Đặc tả Use Case UC-02: Tiếp nhận và duyệt đơn hàng tại quầy POS", "30"),
        ("Bảng 2.3", "Đặc tả Use Case UC-03: Điều phối và chế biến món ăn tại KDS", "30"),
        ("Bảng 2.4", "Đặc tả Use Case UC-04: Thanh toán hóa đơn và đối soát tự động VietQR", "31"),
        ("Bảng 2.5", "Đặc tả Use Case UC-05: Quản lý ca làm việc và chốt két thu ngân (Z-Report)", "31"),
        ("Bảng 2.6", "Đặc tả Use Case UC-06: Cấu hình định mức BOM và trừ tồn tự động", "32"),
        ("Bảng 2.7", "Đặc tả kỹ thuật tính năng Bán hàng & Phục vụ Bàn (POS-001 đến POS-010)", "56"),
        ("Bảng 2.8", "Đặc tả kỹ thuật tính năng Thanh toán & Quản trị Ca (POS-011 đến POS-021)", "58"),
        ("Bảng 2.9", "Đặc tả kỹ thuật 7 tính năng phân hệ Màn hình Nhà bếp (KDS)", "59"),
        ("Bảng 2.10", "Đặc tả tính năng Thực đơn, Định lượng BOM & Quản lý Kho (BOH-001 đến BOH-010)", "60"),
        ("Bảng 2.11", "Đặc tả tính năng Phê duyệt, Đối soát & Báo cáo (BOH-011 đến BOH-018)", "61"),
        ("Bảng 2.12", "Đặc tả tính năng Nhân sự, Cấu hình & Kiểm toán (BOH-019 đến BOH-024)", "62"),
        ("Bảng 2.13", "Đặc tả kỹ thuật 8 tính năng phân hệ Khách hàng Tự gọi món (QR Ordering)", "63"),
        ("Bảng 2.14", "Đặc tả kỹ thuật 10 tính năng phân hệ Tài khoản & Loyalty (ACC)", "64"),
        ("Bảng 2.15", "Cấu trúc trường chi tiết bảng Orders", "65"),
        ("Bảng 2.16", "Cấu trúc trường chi tiết bảng OrderItems", "66"),
        ("Bảng 2.17", "Cấu trúc trường chi tiết bảng Shifts", "66"),
        ("Bảng 2.18", "Cấu trúc trường chi tiết bảng Materials và Recipes", "67"),
        ("Bảng 3.1", "Bảng tổng hợp kết quả thực thi 334 test cases tự động trên .NET 9", "76"),
        ("Bảng 3.2a", "Bảng kịch bản kiểm thử tích hợp phân hệ POS và KDS (IT-01 đến IT-10)", "77"),
        ("Bảng 3.2b", "Bảng kịch bản kiểm thử tích hợp Quản trị Ca, Kho BOM và Webhook (IT-11 đến IT-20)", "78"),
        ("Bảng 3.3", "Bảng kết quả kiểm thử tải hệ thống bằng công cụ k6", "78"),
        ("Bảng 3.4", "Bảng đo lường độ trễ truyền thông điệp thời gian thực SignalR Hub", "79"),
        ("Bảng 3.5", "Ma trận đối chiếu mục tiêu nghiên cứu và kết quả thực tế", "80"),
        ("Bảng B.1a", "Danh mục API Endpoints Phân hệ Xác thực, Bàn ăn, Thực đơn và KDS", "88"),
        ("Bảng B.1b", "Danh mục API Endpoints Phân hệ Chế biến, Thanh toán, Ca thu ngân và BOH", "89"),
        ("Bảng C.1", "Danh mục mã lỗi chuẩn hóa ProblemDetails RFC 7807", "90")
    ]

    for tbl_id, tbl_title, pg in tables_list:
        p_tbl = doc.add_paragraph()
        p_tbl.paragraph_format.line_spacing = 1.2
        p_tbl.paragraph_format.space_before = Pt(2)
        p_tbl.paragraph_format.space_after = Pt(2)
        p_tbl.paragraph_format.tab_stops.add_tab_stop(Inches(5.9), docx.enum.text.WD_TAB_ALIGNMENT.RIGHT, docx.enum.text.WD_TAB_LEADER.DOTS)
        
        r_id = p_tbl.add_run(f"{tbl_id}: ")
        r_id.font.name = 'Times New Roman'
        r_id.font.size = Pt(12)
        r_id.bold = True
        
        r_t = p_tbl.add_run(tbl_title)
        r_t.font.name = 'Times New Roman'
        r_t.font.size = Pt(12)
        
        r_tab = p_tbl.add_run("\t")
        
        r_pg = p_tbl.add_run(pg)
        r_pg.font.name = 'Times New Roman'
        r_pg.font.size = Pt(12)
        r_pg.bold = True

print("Preliminary pages module ready.")
