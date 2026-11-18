import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

def set_cell_background(cell, fill_color):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_color}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=50, bottom=50, left=80, right=80):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:bottom w:w="{bottom}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>')
    tcPr.append(tcMar)

def set_table_borders(table):
    tblBorders = parse_xml(f'''
        <w:tblBorders {nsdecls("w")}>
            <w:top w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:left w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:bottom w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:right w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:insideH w:val="single" w:sz="4" w:space="0" w:color="888888"/>
            <w:insideV w:val="single" w:sz="4" w:space="0" w:color="888888"/>
        </w:tblBorders>
    ''')
    table._tbl.tblPr.append(tblBorders)

def add_p(doc, text, bold=False, italic=False, size=13, align=WD_ALIGN_PARAGRAPH.JUSTIFY, space_before=0, space_after=6, indent=0.5):
    p = doc.add_paragraph()
    p.alignment = align
    p.paragraph_format.line_spacing = 1.5
    p.paragraph_format.space_before = Pt(space_before)
    p.paragraph_format.space_after = Pt(space_after)
    
    # Chuẩn hóa khoảng cách thụt lề cho gạch đầu dòng (Hanging Indent)
    if text.startswith('• ') or text.startswith('- '):
        p.paragraph_format.left_indent = Inches(0.4)
        p.paragraph_format.first_line_indent = Inches(-0.2)
    elif indent > 0:
        p.paragraph_format.first_line_indent = Inches(indent)
        p.paragraph_format.left_indent = Inches(0)
    else:
        p.paragraph_format.first_line_indent = Inches(0)
        p.paragraph_format.left_indent = Inches(0)

    run = p.add_run(text)
    run.bold = bold
    run.italic = italic
    run.font.name = 'Times New Roman'
    run.font.size = Pt(size)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p

def add_h1(doc, text):
    p = doc.add_paragraph(style='Heading 1')
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p.paragraph_format.line_spacing = 1.5
    p.paragraph_format.space_before = Pt(18)
    p.paragraph_format.space_after = Pt(10)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.bold = True
    run.font.name = 'Times New Roman'
    run.font.size = Pt(14)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p

def add_h2(doc, text):
    p = doc.add_paragraph(style='Heading 2')
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p.paragraph_format.line_spacing = 1.5
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.bold = True
    run.font.name = 'Times New Roman'
    run.font.size = Pt(13)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p

def add_h3(doc, text):
    p = doc.add_paragraph(style='Heading 3')
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p.paragraph_format.line_spacing = 1.5
    p.paragraph_format.space_before = Pt(8)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.bold = True
    run.italic = True
    run.font.name = 'Times New Roman'
    run.font.size = Pt(13)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p

def add_table_title(doc, table_id, title):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(f"Bảng {table_id}: {title}")
    run.bold = True
    run.italic = True
    run.font.name = 'Times New Roman'
    run.font.size = Pt(12)
    return p

def add_figure(doc, fig_path, fig_id, caption, width_inch=5.8):
    p_img = doc.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(6)
    p_img.paragraph_format.space_after = Pt(3)
    p_img.paragraph_format.keep_with_next = True
    run_img = p_img.add_run()
    run_img.add_picture(fig_path, width=Inches(width_inch))
    
    p_cap = doc.add_paragraph()
    p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_cap.paragraph_format.space_before = Pt(2)
    p_cap.paragraph_format.space_after = Pt(8)
    run_cap = p_cap.add_run(f"Hình {fig_id}: {caption}")
    run_cap.italic = True
    run_cap.font.name = 'Times New Roman'
    run_cap.font.size = Pt(12)

def add_side_by_side_figures(doc, fig_path1, fig_id1, cap1, width1, fig_path2, fig_id2, cap2, width2):
    tbl = doc.add_table(rows=1, cols=2)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    
    # Remove all borders for clean layout
    tblBorders = parse_xml(f'''
        <w:tblBorders {nsdecls("w")}>
            <w:top w:val="none" w:sz="0" w:space="0" w:color="auto"/>
            <w:left w:val="none" w:sz="0" w:space="0" w:color="auto"/>
            <w:bottom w:val="none" w:sz="0" w:space="0" w:color="auto"/>
            <w:right w:val="none" w:sz="0" w:space="0" w:color="auto"/>
            <w:insideH w:val="none" w:sz="0" w:space="0" w:color="auto"/>
            <w:insideV w:val="none" w:sz="0" w:space="0" w:color="auto"/>
        </w:tblBorders>
    ''')
    tbl._tbl.tblPr.append(tblBorders)
    
    # Left cell
    c1 = tbl.cell(0, 0)
    p1 = c1.paragraphs[0]
    p1.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p1.paragraph_format.space_before = Pt(4)
    p1.paragraph_format.space_after = Pt(2)
    p1.paragraph_format.keep_with_next = True
    r1 = p1.add_run()
    r1.add_picture(fig_path1, width=Inches(width1))
    
    p1_cap = c1.add_paragraph()
    p1_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p1_cap.paragraph_format.space_before = Pt(2)
    p1_cap.paragraph_format.space_after = Pt(6)
    r1_cap = p1_cap.add_run(f"Hình {fig_id1}: {cap1}")
    r1_cap.italic = True
    r1_cap.font.name = 'Times New Roman'
    r1_cap.font.size = Pt(11)
    
    # Right cell
    c2 = tbl.cell(0, 1)
    p2 = c2.paragraphs[0]
    p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p2.paragraph_format.space_before = Pt(4)
    p2.paragraph_format.space_after = Pt(2)
    p2.paragraph_format.keep_with_next = True
    r2 = p2.add_run()
    r2.add_picture(fig_path2, width=Inches(width2))
    
    p2_cap = c2.add_paragraph()
    p2_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p2_cap.paragraph_format.space_before = Pt(2)
    p2_cap.paragraph_format.space_after = Pt(6)
    r2_cap = p2_cap.add_run(f"Hình {fig_id2}: {cap2}")
    r2_cap.italic = True
    r2_cap.font.name = 'Times New Roman'
    r2_cap.font.size = Pt(11)

def style_table(doc, col_widths, headers, data, font_size=9.0):
    # Tự động chuẩn hóa tổng độ rộng bảng không vượt quá chiều rộng trang in (5.85 inches / 14.86 cm)
    raw_total = sum([w.inches if hasattr(w, 'inches') else (w / 914400.0 if w > 10000 else float(w)) for w in col_widths])
    if raw_total > 5.85:
        scale = 5.85 / raw_total
        col_widths = [Inches((w.inches if hasattr(w, 'inches') else (w / 914400.0 if w > 10000 else float(w))) * scale) for w in col_widths]

    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.style = 'Table Grid'
    
    # Explicit table borders (dòng kẻ bảng đầy đủ: viền ngoài và các dòng kẻ ngang/dọc bên trong)
    tblBorders = parse_xml(f'''
        <w:tblBorders {nsdecls("w")}>
            <w:top w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:left w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:bottom w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:right w:val="single" w:sz="6" w:space="0" w:color="000000"/>
            <w:insideH w:val="single" w:sz="4" w:space="0" w:color="444444"/>
            <w:insideV w:val="single" w:sz="4" w:space="0" w:color="444444"/>
        </w:tblBorders>
    ''')
    table._tbl.tblPr.append(tblBorders)
    
    # Header row
    hdr_cells = table.rows[0].cells
    hdr_tr = table.rows[0]._tr.get_or_add_trPr()
    hdr_tr.append(parse_xml(f'<w:tblHeader {nsdecls("w")}/>'))
    hdr_tr.append(parse_xml(f'<w:cantSplit {nsdecls("w")}/>'))
    
    for idx, header_text in enumerate(headers):
        hdr_cells[idx].text = header_text
        set_cell_background(hdr_cells[idx], "E5E7EB")
        set_cell_margins(hdr_cells[idx], top=50, bottom=50, left=70, right=70)
        p = hdr_cells[idx].paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.line_spacing = 1.05
        p.paragraph_format.space_before = Pt(1)
        p.paragraph_format.space_after = Pt(1)
        p.paragraph_format.keep_with_next = True
        run = p.runs[0]
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(font_size)
    
    # Data rows
    total_data_rows = len(data)
    for row_idx, row_data in enumerate(data):
        row = table.add_row()
        row_tr = row._tr.get_or_add_trPr()
        row_tr.append(parse_xml(f'<w:cantSplit {nsdecls("w")}/>'))
        
        row_cells = row.cells
        is_not_last = (row_idx < total_data_rows - 1)
        for col_idx, text in enumerate(row_data):
            row_cells[col_idx].text = text
            set_cell_margins(row_cells[col_idx], top=35, bottom=35, left=70, right=70)
            p = row_cells[col_idx].paragraphs[0]
            p.paragraph_format.line_spacing = 1.05
            p.paragraph_format.space_before = Pt(1)
            p.paragraph_format.space_after = Pt(1)
            if is_not_last:
                p.paragraph_format.keep_with_next = True
            if col_idx == 0 or (len(row_data) > 3 and col_idx in [0, 2, 3]):
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            else:
                p.alignment = WD_ALIGN_PARAGRAPH.LEFT
            run = p.runs[0]
            run.font.name = 'Times New Roman'
            run.font.size = Pt(font_size)

    for row in table.rows:
        for idx, width in enumerate(col_widths):
            row.cells[idx].width = width
            
    return table
