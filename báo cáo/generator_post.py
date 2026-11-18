import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from doc_helpers import (
    add_p, add_h1, add_h2, add_h3, set_cell_background, set_cell_margins, set_table_borders, style_table, add_table_title
)

def add_code_block_post(doc, code_str, caption=None):
    if caption:
        p_cap = doc.add_paragraph()
        p_cap.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p_cap.paragraph_format.space_before = Pt(6)
        p_cap.paragraph_format.space_after = Pt(2)
        r_cap = p_cap.add_run(caption)
        r_cap.font.name = 'Times New Roman'
        r_cap.font.size = Pt(10)
        r_cap.italic = True
        r_cap.font.color.rgb = RGBColor(71, 85, 105)

    p_code = doc.add_paragraph()
    p_code.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p_code.paragraph_format.left_indent = Inches(0.3)
    p_code.paragraph_format.space_before = Pt(2)
    p_code.paragraph_format.space_after = Pt(6)
    p_code.paragraph_format.line_spacing = 1.15
    r = p_code.add_run(code_str.strip())
    r.font.name = 'Consolas'
    r.font.size = Pt(9.0)
    r.font.color.rgb = RGBColor(30, 41, 59)

def build_post_content(doc):
    """
    Tạo các tài liệu kết thúc ngoài nội dung chính:
    - TÀI LIỆU THAM KHẢO (Theo chuẩn IEEE / ISO 690)
    - PHỤ LỤC A: HƯỚNG DẪN CÀI ĐẶT, CẤU HÌNH VÀ TRIỂN KHAI VỚI DOCKER COMPOSE
    - PHỤ LỤC B: ĐẶC TẢ CHI TIẾT RESTFUL API VÀ CẤU HÌNH BẢO MẬT
    - PHỤ LỤC C: DANH MỤC MÃ LỖI CHUẨN HÓA RFC 7807 PROBLEMDETAILS
    """
    doc.add_page_break()

    # ==========================================
    # TÀI LIỆU THAM KHẢO (REFERENCES)
    # ==========================================
    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("TÀI LIỆU THAM KHẢO")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(15)
    r.bold = True

    references = [
        "[1] iPOS.vn, \"Báo cáo thị trường Kinh doanh Ẩm thực tại Việt Nam năm 2023 - 2024,\" Công ty Cổ phần iPOS.vn, Hà Nội, 2024. [Trực tuyến]. Địa chỉ: https://ipos.vn/bao-cao-thi-truong-f-and-b-viet-nam-2023/ [Truy cập: 15/03/2026].",
        "[2] Hiệp hội Doanh nghiệp Dịch vụ Ẩm thực Việt Nam (RAV), \"Báo cáo chuyển đổi số và xu hướng tự động hóa vận hành ngành dịch vụ F&B tại Việt Nam,\" Diễn đàn Ẩm thực Việt Nam, Hà Nội, 2023.",
        "[3] Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS), \"Tiêu chuẩn kỹ thuật định dạng mã phản hồi nhanh phục vụ thanh toán (VietQR Specification),\" Quyết định số 08/QĐ-HĐQT-NAPAS, Hà Nội, 2021.",
        "[4] Bộ Giáo dục và Đào tạo, \"Quy chế đào tạo trình độ đại học,\" Thông tư số 08/2021/TT-BGDĐT, ngày 18 tháng 03 năm 2021.",
        "[5] Trường Đại học Phenikaa, \"Quy định về thực tập tốt nghiệp và đồ án/khóa luận tốt nghiệp trình độ đại học,\" Quyết định số 1439/QĐ-ĐHP-ĐT, ngày 07/11/2022.",
        "[6] Toast Inc., \"Restaurant Technology Outlook Report: Streamlining Front and Back of House Operations,\" Boston, MA, USA, Research Report, 2023. [Online]. Available: https://pos.toasttab.com/resources/restaurant-technology-report.",
        "[7] Microsoft Corporation, \".NET Architecture Guides: Clean Architecture on Modern .NET,\" Microsoft Learn Documentation, 2024. [Online]. Available: https://learn.microsoft.com/dotnet/architecture/.",
        "[8] Microsoft Corporation, \"ASP.NET Core Blazor Fundamentals & Real-time Web Communication with SignalR Core,\" Microsoft Learn Documentation, 2024. [Online]. Available: https://learn.microsoft.com/aspnet/core/blazor/.",
        "[9] Microsoft Corporation, \"Entity Framework Core: Performance Tuning, Migrations and Concurrency Management,\" Microsoft Docs, 2024. [Online]. Available: https://learn.microsoft.com/ef/core/.",
        "[10] A. Silberschatz, H. F. Korth, and S. Sudarshan, Database System Concepts, 7th ed. New York, NY, USA: McGraw-Hill Education, 2019. ISBN: 978-0078022159.",
        "[11] R. C. Martin, Clean Architecture: A Craftsman's Guide to Software Structure and Design, 1st ed. Boston, MA, USA: Prentice Hall, 2017. ISBN: 978-0134494166.",
        "[12] E. Evans, Domain-Driven Design: Tackling Complexity in the Heart of Software, 1st ed. Boston, MA, USA: Addison-Wesley Professional, 2003. ISBN: 978-0321125217.",
        "[13] M. Fowler, Patterns of Enterprise Application Architecture, 1st ed. Boston, MA, USA: Addison-Wesley Longman Publishing, 2002. ISBN: 978-0321127426.",
        "[14] C. J. Date, An Introduction to Database Systems, 8th ed. Boston, MA, USA: Addison-Wesley, 2003. ISBN: 978-0321197849.",
        "[15] R. T. Fielding, \"Architectural Styles and the Design of Network-based Software Architectures,\" Ph.D. dissertation, Department of Information and Computer Science, University of California, Irvine, CA, USA, 2000.",
        "[16] M. Jones, J. Bradley, and N. Sakimura, \"JSON Web Token (JWT),\" Internet Engineering Task Force (IETF), RFC 7519, May 2015. DOI: 10.17487/RFC7519.",
        "[17] M. Nottingham and E. Wilde, \"Problem Details for HTTP APIs,\" Internet Engineering Task Force (IETF), RFC 7807, Mar. 2016. DOI: 10.17487/RFC7807.",
        "[18] SePay, \"Tài liệu kỹ thuật tích hợp Cổng thanh toán tự động VietQR và Webhook đối soát biến động số dư,\" SePay Developer Portal, 2024. [Trực tuyến]. Địa chỉ: https://sepay.vn/docs/ [Truy cập: 10/02/2026]."
    ]

    for ref in references:
        p_ref = doc.add_paragraph()
        p_ref.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        p_ref.paragraph_format.line_spacing = 1.3
        p_ref.paragraph_format.space_before = Pt(2)
        p_ref.paragraph_format.space_after = Pt(4)
        p_ref.paragraph_format.left_indent = Inches(0.4)
        p_ref.paragraph_format.first_line_indent = Inches(-0.4)
        
        run = p_ref.add_run(ref)
        run.font.name = 'Times New Roman'
        run.font.size = Pt(12)

    # ==========================================
    # PHỤ LỤC A
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("PHỤ LỤC A: HƯỚNG DẪN CÀI ĐẶT, CẤU HÌNH VÀ TRIỂN KHAI HỆ THỐNG")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(14)
    r.bold = True

    add_h2(doc, "A.1. Yêu cầu Môi trường Phần cứng và Phần mềm")
    add_p(doc, "Để biên dịch và vận hành ổn định toàn bộ giải pháp hệ thống quản lý nhà hàng, môi trường máy chủ và máy trạm phát triển cần đáp ứng các tiêu chuẩn kỹ thuật tối thiểu sau:")
    add_p(doc, "• Hệ điều hành: Windows 10/11 Pro (64-bit), Windows Server 2019/2022 hoặc Ubuntu Linux 22.04 LTS.")
    add_p(doc, "• Bộ vi xử lý (CPU): Tối thiểu 4 nhân (x86_64), khuyến nghị Intel Core i5 / AMD Ryzen 5 thế hệ 10 trở lên.")
    add_p(doc, "• Bộ nhớ trong (RAM): Tối thiểu 8 GB RAM (khuyến nghị 16 GB để chạy đồng thời API, Blazor Server và SQL Server).")
    add_p(doc, "• Môi trường thực thi .NET: .NET SDK phiên bản 9.0 trở lên.")
    add_p(doc, "• Hệ quản trị cơ sở dữ liệu: Microsoft SQL Server 2022 (Developer hoặc Standard Edition) kèm công cụ SSMS.")
    add_p(doc, "• Trình duyệt web hỗ trợ HTML5, WebSockets và Camera (cho QR Scanner): Google Chrome 110+, Microsoft Edge 110+, Safari iOS 15+.")

    add_h2(doc, "A.2. Cấu hình Triển khai với Docker Compose (docker-compose.yml)")
    add_p(doc, "Hệ thống hỗ trợ đóng gói container hóa toàn diện, cho phép khởi chạy máy chủ ứng dụng và cơ sở dữ liệu chỉ với một câu lệnh `docker compose up -d`:")

    docker_compose_str = '''version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: restaurant_sqlserver
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=PasswordStrong2026!
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PasswordStrong2026! -Q 'SELECT 1'"]
      interval: 10s
      timeout: 5s
      retries: 5

  restaurant_app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: restaurant_webapp
    depends_on:
      sqlserver:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=RestaurantDb;User Id=sa;Password=PasswordStrong2026!;TrustServerCertificate=True;
      - JwtSettings__Secret=ChuoiKhoaBiMatBaoMatCaoJWTTokenCleanArchitecture2026
      - SePay__ApiToken=SEPAY_SECURE_WEBHOOK_API_KEY_TOKEN
    ports:
      - "5000:80"
      - "5001:443"
    restart: always

volumes:
  sql_data:'''
    add_code_block_post(doc, docker_compose_str, "Cấu hình Docker Compose đa dịch vụ (docker-compose.yml)")

    add_h2(doc, "A.3. Các Bước Khởi tạo Cơ sở Dữ liệu và Seed Data Ban đầu")
    add_p(doc, "1. Bước 1: Mở cửa sổ dòng lệnh PowerShell hoặc Terminal tại thư mục `src/Restaurant.Infrastructure`.")
    add_p(doc, "2. Bước 2: Thực thi lệnh cập nhật cơ sở dữ liệu từ bộ Migration có sẵn:")
    add_p(doc, "   `dotnet ef database update --startup-project ../Restaurant.Web`", italic=True)
    add_p(doc, "3. Bước 3: Sau khi quá trình Migration hoàn tất, hệ thống tự động chạy tệp `DbInitializer.cs` để gieo dữ liệu mẫu ban đầu (Seed Data) bao gồm: tài khoản quản trị Admin (`admin/Admin@123`), tài khoản thu ngân, danh mục 30 món ăn tiêu biểu, 10 nhóm tùy chọn modifier và cấu hình 15 bàn ăn.")

    add_h2(doc, "A.4. Khởi động Ứng dụng")
    add_p(doc, "• Khởi động Toàn bộ Hệ thống Web & API: `dotnet run --project src/Restaurant.Web` (mặc định mở tại `https://localhost:5001`).")
    add_p(doc, "• Truy cập Quầy Bán hàng POS: Đường dẫn `https://localhost:5001/pos` trên trình duyệt máy POS.")
    add_p(doc, "• Truy cập Màn hình Nhà bếp KDS: Đường dẫn `https://localhost:5001/kds` trên màn hình bếp.")
    add_p(doc, "• Truy cập Giao diện QR Ordering: Quét mã QR trên bàn hoặc mở đường dẫn `https://localhost:5001/qr?tableId=1&token={token}` trên điện thoại di động.")

    # ==========================================
    # PHỤ LỤC B
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("PHỤ LỤC B: ĐẶC TẢ CHI TIẾT RESTFUL API VÀ CẤU HÌNH BẢO MẬT")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(14)
    r.bold = True

    add_p(doc, "Hệ thống cung cấp tập hợp 35+ RESTful API Endpoints phục vụ toàn bộ chu trình trao đổi dữ liệu. Bảng B.1a và B.1b tổng hợp các Endpoint trọng yếu theo phân quyền RBAC:")

    api_endpoints_a = [
        ("POST", "/api/v1/auth/login", "Xác thực danh tính người dùng và cấp phát mã JWT", "Public"),
        ("POST", "/api/v1/auth/refresh-token", "Làm mới JWT token khi hết hạn truy cập", "Public"),
        ("GET", "/api/v1/tables", "Lấy sơ đồ danh sách toàn bộ bàn ăn và trạng thái hiện tại", "Cashier, Admin"),
        ("POST", "/api/v1/tables/{id}/open", "Mở bàn mới để đón khách vào sảnh", "Cashier"),
        ("GET", "/api/v1/menu/categories", "Lấy danh mục thực đơn kèm danh sách món ăn đang bán", "Public"),
        ("POST", "/api/v1/orders/draft", "Khách hàng gửi đơn gọi món từ ứng dụng QR Ordering", "Public (QR Token)"),
        ("PUT", "/api/v1/orders/{id}/approve", "Thu ngân duyệt đơn QR chuyển lệnh xuống nhà bếp", "Cashier"),
        ("GET", "/api/v1/kitchen/tickets", "Lấy danh sách các đơn món đang chờ nấu trên KDS", "Kitchen, Chef")
    ]

    add_table_title(doc, "B.1a", "Danh mục API Endpoints Phân hệ Xác thực, Bàn ăn, Thực đơn và KDS")
    tbl_api_a = doc.add_table(rows=len(api_endpoints_a) + 1, cols=4)
    tbl_api_a.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_api_a.autofit = False

    col_widths_api = [Inches(0.9), Inches(2.0), Inches(1.8), Inches(1.15)]
    headers_api = ["Phương thức", "Đường dẫn (URL Endpoint)", "Chức năng nghiệp vụ", "Phân quyền (RBAC)"]

    for col_idx, h_text in enumerate(headers_api):
        cell = tbl_api_a.rows[0].cells[col_idx]
        cell.width = col_widths_api[col_idx]
        set_cell_background(cell, "f1f5f9")
        set_cell_margins(cell, top=40, bottom=40, left=60, right=60)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = p.add_run(h_text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(10.5)

    for row_idx, (method, url, desc, role) in enumerate(api_endpoints_a):
        row = tbl_api_a.rows[row_idx + 1]
        data = [method, url, desc, role]
        for col_idx, val in enumerate(data):
            cell = row.cells[col_idx]
            cell.width = col_widths_api[col_idx]
            set_cell_margins(cell, top=30, bottom=30, left=50, right=50)
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if col_idx in [0, 3] else WD_ALIGN_PARAGRAPH.LEFT
            run = p.add_run(val)
            run.font.name = 'Consolas' if col_idx == 1 else 'Times New Roman'
            run.font.size = Pt(9.0 if col_idx == 1 else 9.5)
            if col_idx == 0:
                run.bold = True
                if val == "POST":
                    run.font.color.rgb = RGBColor(16, 185, 129)
                elif val == "GET":
                    run.font.color.rgb = RGBColor(59, 130, 246)
                elif val == "PUT":
                    run.font.color.rgb = RGBColor(245, 158, 11)

    set_table_borders(tbl_api_a)

    api_endpoints_b = [
        ("PUT", "/api/v1/kitchen/items/{id}/status", "Cập nhật trạng thái món: Pending -> Processing -> Done", "Kitchen, Chef"),
        ("POST", "/api/v1/payments/create-qr", "Khởi tạo thanh toán VietQR động kèm thông tin SePay", "Cashier"),
        ("POST", "/api/v1/webhooks/sepay", "Tiếp nhận webhook thông báo biến động số dư ngân hàng", "SePay API Key"),
        ("POST", "/api/v1/shifts/open", "Khai báo tiền két đầu ca và bắt đầu phiên thu ngân", "Cashier"),
        ("POST", "/api/v1/shifts/{id}/close", "Kiểm tiền két, in báo cáo Z-Report và khóa sổ ca", "Cashier, Manager"),
        ("POST", "/api/v1/inventory/stocktake", "Tạo phiên kiểm kê kho và cân bằng tồn kho thực tế", "Warehouse, Admin"),
        ("GET", "/api/v1/reports/revenue", "Truy vấn báo cáo doanh thu tài chính theo ngày/tuần/tháng", "Manager, Admin")
    ]

    doc.add_page_break()
    add_table_title(doc, "B.1b", "Danh mục API Endpoints Phân hệ Chế biến, Thanh toán, Ca thu ngân và BOH")
    tbl_api_b = doc.add_table(rows=len(api_endpoints_b) + 1, cols=4)
    tbl_api_b.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_api_b.autofit = False

    for col_idx, h_text in enumerate(headers_api):
        cell = tbl_api_b.rows[0].cells[col_idx]
        cell.width = col_widths_api[col_idx]
        set_cell_background(cell, "f1f5f9")
        set_cell_margins(cell, top=40, bottom=40, left=60, right=60)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = p.add_run(h_text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(10.5)

    for row_idx, (method, url, desc, role) in enumerate(api_endpoints_b):
        row = tbl_api_b.rows[row_idx + 1]
        data = [method, url, desc, role]
        for col_idx, val in enumerate(data):
            cell = row.cells[col_idx]
            cell.width = col_widths_api[col_idx]
            set_cell_margins(cell, top=30, bottom=30, left=50, right=50)
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if col_idx in [0, 3] else WD_ALIGN_PARAGRAPH.LEFT
            run = p.add_run(val)
            run.font.name = 'Consolas' if col_idx == 1 else 'Times New Roman'
            run.font.size = Pt(9.0 if col_idx == 1 else 9.5)
            if col_idx == 0:
                run.bold = True
                if val == "POST":
                    run.font.color.rgb = RGBColor(16, 185, 129)
                elif val == "GET":
                    run.font.color.rgb = RGBColor(59, 130, 246)
                elif val == "PUT":
                    run.font.color.rgb = RGBColor(245, 158, 11)

    set_table_borders(tbl_api_b)

    # ==========================================
    # PHỤ LỤC C: DANH MỤC MÃ LỖI RFC 7807 PROBLEMDETAILS
    # ==========================================
    doc.add_page_break()

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(12)
    p_title.paragraph_format.space_after = Pt(18)
    r = p_title.add_run("PHỤ LỤC C: DANH MỤC MÃ LỖI CHUẨN HÓA RFC 7807 PROBLEMDETAILS")
    r.font.name = 'Times New Roman'
    r.font.size = Pt(14)
    r.bold = True

    add_p(doc, "Toàn bộ phản hồi lỗi từ máy chủ đều được chuẩn hóa theo định dạng RFC 7807 ProblemDetails. Bảng C.1 liệt kê danh mục 12 mã lỗi chuẩn của hệ thống:")

    error_codes = [
        ("ERR_AUTH_INVALID_CREDENTIALS", "401 Unauthorized", "Sai tên đăng nhập hoặc mật khẩu", "Hiển thị thông báo yêu cầu nhập lại"),
        ("ERR_AUTH_PIN_INVALID", "403 Forbidden", "Mã PIN Quản lý không chính xác", "Thông báo số lần thử còn lại"),
        ("ERR_AUTH_PIN_LOCKED", "423 Locked", "Mã PIN bị khóa 15 phút do nhập sai 5 lần", "Vô hiệu hóa nút xác thực, đếm ngược"),
        ("ERR_CONCURRENCY_CONFLICT", "409 Conflict", "Xung đột phiên bản dữ liệu (RowVersion)", "Yêu cầu tải lại dữ liệu mới nhất"),
        ("ERR_ORDER_TABLE_OCCUPIED", "400 Bad Request", "Bàn ăn đã có đơn hàng đang phục vụ", "Chọn bàn khác hoặc chọn ghép đơn"),
        ("ERR_ORDER_ITEM_SOLDOUT", "400 Bad Request", "Món ăn đã hết hàng trên KDS", "Đổi món khác, cập nhật thực đơn"),
        ("ERR_ORDER_CANNOT_CANCEL", "400 Bad Request", "Không thể hủy đơn hàng đã thanh toán", "Liên hệ Quản lý để lập phiếu hoàn tiền"),
        ("ERR_PAYMENT_AMOUNT_MISMATCH", "400 Bad Request", "Số tiền chuyển khoản không đủ khớp đơn", "Yêu cầu khách chuyển thêm phần thiếu"),
        ("ERR_SEPAY_DUPLICATE_WEBHOOK", "200 OK", "Webhook gửi lặp lại (Idempotent catch)", "Bỏ qua, không xử lý lại"),
        ("ERR_SHIFT_ALREADY_OPEN", "400 Bad Request", "Đã có ca làm việc đang mở trên máy này", "Phải đóng ca hiện tại trước"),
        ("ERR_SHIFT_UNRESOLVED_ORDERS", "400 Bad Request", "Còn bàn ăn chưa thanh toán khi đóng ca", "Hoàn tất thanh toán các bàn trước khi đóng ca"),
        ("ERR_INVENTORY_NEGATIVE_STOCK", "422 Unprocessable", "Tồn kho nguyên vật liệu không đủ trừ", "Vẫn ghi nhận âm tồn và gửi cảnh báo đỏ Admin")
    ]

    add_table_title(doc, "C.1", "Danh mục mã lỗi chuẩn hóa ProblemDetails RFC 7807")
    tbl_err = doc.add_table(rows=len(error_codes) + 1, cols=4)
    tbl_err.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_err.autofit = False

    col_widths_err = [Inches(1.8), Inches(0.95), Inches(1.85), Inches(1.25)]
    headers_err = ["Mã Lỗi Hệ Thống", "Mã HTTP", "Nguyên Nhân Phát Sinh", "Hướng Xử Lý Client"]

    for col_idx, h_text in enumerate(headers_err):
        cell = tbl_err.rows[0].cells[col_idx]
        cell.width = col_widths_err[col_idx]
        set_cell_background(cell, "f1f5f9")
        set_cell_margins(cell, top=35, bottom=35, left=50, right=50)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = p.add_run(h_text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(10.0)

    for row_idx, (code, status, cause, fix) in enumerate(error_codes):
        row = tbl_err.rows[row_idx + 1]
        data = [code, status, cause, fix]
        for col_idx, val in enumerate(data):
            cell = row.cells[col_idx]
            cell.width = col_widths_err[col_idx]
            set_cell_margins(cell, top=25, bottom=25, left=45, right=45)
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if col_idx == 1 else WD_ALIGN_PARAGRAPH.LEFT
            run = p.add_run(val)
            run.font.name = 'Consolas' if col_idx in [0, 1] else 'Times New Roman'
            run.font.size = Pt(8.5 if col_idx in [0, 1] else 9.0)
            if col_idx == 0:
                run.bold = True
                run.font.color.rgb = RGBColor(220, 38, 38)
            elif col_idx == 1:
                run.font.color.rgb = RGBColor(79, 70, 229)

    set_table_borders(tbl_err)

    print("Post content module expanded with Docker Compose and Appendix C (Error Codes)!")
