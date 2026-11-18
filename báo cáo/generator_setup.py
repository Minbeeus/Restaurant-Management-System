import os
import sys
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

def create_document():
    doc = docx.Document()
    
    # Configure document settings to automatically update TOC, Figures, and Tables fields on open
    try:
        element = parse_xml(r'<w:updateFields %s w:val="true"/>' % nsdecls('w'))
        doc.settings.element.append(element)
    except Exception:
        pass
    
    # Setup page margins for the first section (Cover)
    sec = doc.sections[0]
    sec.top_margin = Inches(2.5 / 2.54)
    sec.bottom_margin = Inches(2.5 / 2.54)
    sec.left_margin = Inches(3.5 / 2.54)
    sec.right_margin = Inches(2.5 / 2.54)
    sec.page_width = Inches(21.0 / 2.54)
    sec.page_height = Inches(29.7 / 2.54)

    # Base Normal Style
    style = doc.styles['Normal']
    font = style.font
    font.name = 'Times New Roman'
    font.size = Pt(13)
    font.color.rgb = RGBColor(0, 0, 0)
    style.paragraph_format.line_spacing = 1.5
    style.paragraph_format.space_after = Pt(6)
    style.paragraph_format.space_before = Pt(0)

    # Configure Heading styles for Word Table of Contents recognition
    for h_name, h_size, is_bold, is_italic, sp_b, sp_a in [
        ('Heading 1', 14, True, False, 18, 10),
        ('Heading 2', 13, True, False, 12, 6),
        ('Heading 3', 13, True, True, 8, 4)
    ]:
        try:
            h_style = doc.styles[h_name]
            h_style.font.name = 'Times New Roman'
            h_style.font.size = Pt(h_size)
            h_style.font.bold = is_bold
            h_style.font.italic = is_italic
            h_style.font.color.rgb = RGBColor(0, 0, 0)
            h_style.paragraph_format.line_spacing = 1.5
            h_style.paragraph_format.space_before = Pt(sp_b)
            h_style.paragraph_format.space_after = Pt(sp_a)
            h_style.paragraph_format.keep_with_next = True
        except Exception:
            pass

    return doc

def create_main_section(doc):
    """
    Tạo phân vùng cho phần Nội dung chính (Từ Mở đầu đến hết Kết luận):
    - Đánh số trang Ả Rập 1, 2, 3... bắt đầu từ 1.
    - Vị trí: Ở giữa, phía trên đầu mỗi trang giấy.
    """
    sec_main = doc.add_section()
    sec_main.header.is_linked_to_previous = False
    
    # Căn lề chuẩn
    sec_main.top_margin = Inches(2.5 / 2.54)
    sec_main.bottom_margin = Inches(2.5 / 2.54)
    sec_main.left_margin = Inches(3.5 / 2.54)
    sec_main.right_margin = Inches(2.5 / 2.54)
    sec_main.page_width = Inches(21.0 / 2.54)
    sec_main.page_height = Inches(29.7 / 2.54)
    
    # Header: Số trang Ả Rập ở giữa đầu trang
    h = sec_main.header.paragraphs[0]
    h.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_num = h.add_run()
    r_num._r.append(parse_xml(f'<w:fldSimple {nsdecls("w")} w:instr="PAGE"/>'))
    r_num.font.name = 'Times New Roman'
    r_num.font.size = Pt(10)
    
    # Thiết lập số trang Ả Rập bắt đầu từ 1
    sec_main._sectPr.append(parse_xml(f'<w:pgNumType {nsdecls("w")} w:fmt="decimal" w:start="1"/>'))
    return sec_main

print("Document setup module ready.")
