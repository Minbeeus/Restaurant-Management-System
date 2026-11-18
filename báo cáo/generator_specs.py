import docx
from doc_helpers import add_p, add_h2, add_h3, add_table_title, style_table
from docx.shared import Inches

def append_deep_specifications(doc):
    add_h2(doc, "2.9. Đặc tả Kỹ thuật và Hiện thực hóa Chi tiết 70 Chức năng Hệ thống")
    add_p(doc, "Nhằm phục vụ công tác đối soát, kiểm thử nghiệm thu và bàn giao công nghệ, dưới đây là bảng đặc tả chi tiết toàn bộ 70 chức năng cốt lõi của hệ thống được trích xuất và đối chiếu trực tiếp từ bộ tài liệu Đặc tả Kỹ thuật (Spec Kit 2.0) và mã nguồn C# .NET 9 thực tế của dự án.")
    add_p(doc, "Quy trình đặc tả chức năng được xây dựng dựa trên phương pháp Phát triển Hướng Đặc tả (Specification-Driven Development - SDD). Mỗi tính năng nghiệp vụ đều được chuẩn hóa với các trường thông tin then chốt: Mã định danh chức năng (Feature ID), Tên tính năng, Tác nhân thực hiện (Actor), Mức độ ưu tiên theo khung MoSCoW (Must Have, Should Have, Nice to Have), Tóm tắt luồng nghiệp vụ chi tiết và Danh sách các bảng cơ sở dữ liệu tương tác trong phạm vi giao dịch.")
    add_p(doc, "Hệ thống phân định ranh giới tác vụ rõ ràng dựa trên ma trận phân quyền 5 vai trò (Role Matrix): (1) Thu ngân (Cashier) phụ trách bán hàng, mở bàn, in tạm tính và thanh toán; (2) Đầu bếp/Pha chế (Chef/Bartender) tiếp nhận chế biến tại trạm KDS và báo hoàn thành trừ kho; (3) Thủ kho (Stockkeeper) lập phiếu nhập kho và kiểm kê cân bằng tồn; (4) Quản lý (Manager) phê duyệt hủy món đã nấu, hủy đơn, giảm giá vượt trần và nộp tiền két; (5) Quản trị viên (Admin) sở hữu toàn quyền cấu hình danh mục, thực đơn, công thức BOM, tài khoản nhân sự và tra cứu nhật ký kiểm toán.")
    add_p(doc, "Về mức độ ưu tiên: Hệ thống tập trung tối đa nguồn lực vào 62 tính năng thuộc nhóm Bắt buộc (Must Have - chiếm 88.6%), bảo đảm toàn bộ chu trình phục vụ từ khi khách bước vào quán cho đến khi hoàn tất thanh toán và đóng ca diễn ra trơn tru, không có bất kỳ điểm nghẽn gián đoạn nào. 8 tính năng còn lại thuộc nhóm Nên có (Should Have) và Tiện ích (Nice to Have) giúp tối ưu hóa công suất vận hành bếp và nâng cao trải nghiệm khách hàng thân thiết.")

    # 1. POS Specs - Part 1
    add_h3(doc, "2.9.1. Đặc tả Nhóm Tính năng Bán hàng & Phục vụ Bàn (POS)")
    add_p(doc, "Phân hệ Bán hàng tại quầy (POS) là trái tim vận hành của khu vực tiền sảnh, phụ trách toàn bộ chu trình từ mở bàn, tiếp nhận gọi món, chuyển bàn, gộp bàn cho đến điều phối in ấn hóa đơn:")
    pos_part1 = [
        ("POS-001", "Xem Sơ đồ Bàn ăn", "Cashier", "Must Have", "Hiển thị trực quan bàn ăn theo khu vực tầng. Đồng bộ màu sắc realtime: Xám (Available), Đỏ (Occupied), Vàng (New Order), Xanh lá (Billing).", "Areas, Tables"),
        ("POS-002", "Tạo Đơn Ăn tại Bàn", "Cashier", "Must Have", "Chọn bàn trống mở phiên phục vụ (Session). Bàn chuyển màu đỏ (Occupied), tạo bản ghi Order trạng thái Confirmed.", "Orders, Tables"),
        ("POS-003", "Bán Mang đi (Takeaway)", "Cashier", "Must Have", "Giao diện độc lập (PosTakeawayMain.razor) phục vụ khách mang về, thanh toán nhanh không qua sơ đồ bàn.", "Orders, OrderItems"),
        ("POS-004", "Thêm Món, Combo & Tùy chọn", "Cashier", "Must Have", "Thêm món vào OrderCart. Tùy chỉnh size/topping qua PosOptionBuilder, chọn món combo qua PosComboBuilder.", "MenuItems, Modifiers"),
        ("POS-005", "Đổi Điểm Thưởng Lấy Món", "Cashier", "Must Have", "Chọn món thuộc nhóm Đổi Điểm với đơn giá 0đ. Hệ thống kiểm tra số điểm tích lũy và trừ điểm khi thanh toán.", "MenuItems, Loyalty"),
        ("POS-006", "Hủy Món (Void Item)", "Cashier, Manager", "Must Have", "Hủy món chưa nấu trực tiếp; Hủy món đã nấu (Done) yêu cầu PIN Quản lý và hạch toán Cancellation Waste.", "OrderItems, VoidLogs"),
        ("POS-007", "Hủy Đơn Hàng (Cancel Order)", "Cashier, Manager", "Must Have", "Hủy toàn bộ đơn đang phục vụ qua PosCancelOrderModal, yêu cầu lý do và mã PIN Quản lý, giải phóng bàn.", "Orders, ApprovalRequests"),
        ("POS-008", "Chuyển Bàn (Table Transfer)", "Cashier", "Must Have", "Chuyển đơn từ bàn cũ sang bàn mới. Bắn sự kiện TableSessionMoved để điện thoại khách tự cập nhật URL bàn.", "Tables, Orders"),
        ("POS-009", "Gộp Bàn (Table Merge)", "Cashier", "Must Have", "Gộp các bàn đang có khách vào chung 1 đơn chính, dồn món và giải phóng các bàn phụ về trạng thái trống.", "Tables, Orders"),
        ("POS-010", "Tách Hóa Đơn (Split Bill)", "Cashier", "Should Have", "Tách một phần món ăn hoặc số lượng từ đơn hiện tại sang một đơn mới để thanh toán riêng biệt.", "Orders, OrderItems")
    ]
    add_table_title(doc, "2.7", "Đặc tả kỹ thuật tính năng Bán hàng & Phục vụ Bàn (POS-001 đến POS-010)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], pos_part1)

    # 1. POS Specs - Part 2
    add_h3(doc, "2.9.2. Đặc tả Nhóm Tính năng Thanh toán, Quản trị Ca & Thu ngân (POS)")
    add_p(doc, "Nhóm tính năng thanh toán và quản trị ca làm việc đóng vai trò then chốt trong việc bảo đảm an toàn tài chính, ngăn ngừa thất thoát dòng tiền mặt và tự động hóa quy trình đối soát giao dịch ngân hàng.")
    add_p(doc, "Quy trình mở ca bắt buộc thu ngân phải chọn đúng mã máy POS vật lý (Register) và kiểm đếm chính xác số tiền mặt ban đầu có sẵn trong két (OpeningBalance). Mọi biến động tiền mặt phát sinh trong ca không bắt nguồn từ hóa đơn bán hàng (chẳng hạn như đổi tiền lẻ cho khách - Till In, hoặc trích tiền két nộp quản lý - Till Out) đều bắt buộc phải tạo phiếu CashMovement ghi rõ lý do và thời điểm thực hiện.")
    add_p(doc, "Nhằm ngăn chặn các hành vi gian lận nội bộ, toàn bộ các thao tác nhạy cảm tại quầy thu ngân (như hủy món đã nấu xong, hủy toàn bộ đơn hàng đang phục vụ, áp dụng mức chiết khấu vượt trần quy định DiscountLimit) đều bị chặn lại và yêu cầu nhập mã PIN Quản lý (Manager PIN). Mã PIN được mã hóa một chiều bằng thuật toán BCrypt với hệ số Salt an toàn; nếu nhập sai quá 5 lần liên tiếp, hệ thống lập tức khóa quyền xác thực tạm thời trong 15 phút và phát thông báo cảnh báo về trung tâm điều hành.")
    add_p(doc, "Về kênh thanh toán không tiền mặt, hệ thống tích hợp giải pháp VietQR động theo tiêu chuẩn Napas 247. Khi thu ngân chọn hình thức chuyển khoản, hệ thống sinh mã QR chứa số tiền chính xác và mã đơn hàng duy nhất. Máy chủ kết nối thường trực với cổng SePay qua Webhook an toàn; ngay khi người mua chuyển khoản thành công, tín hiệu Webhook được phân tích và phát sóng qua SignalR Hub tới giao diện POS trong chưa đầy 1 giây, tự động đóng cửa sổ thanh toán và kích hoạt máy in K80 in hóa đơn giao dịch.")
    pos_part2 = [
        ("POS-011", "Tra cứu Khách Thành viên", "Cashier", "Must Have", "Mở modal PosMemberModal tra cứu qua SĐT/Tên, hiển thị hạng thẻ, điểm khả dụng và tỷ lệ chiết khấu theo hạng.", "Customers, Tiers"),
        ("POS-012", "Tiêu Điểm & Áp Voucher", "Cashier", "Must Have", "Quy đổi điểm tích lũy trừ trực tiếp tiền mặt; Quét hoặc nhập mã voucher khuyến mãi giảm giá theo chương trình.", "Orders, Vouchers"),
        ("POS-013", "Chiết khấu Giảm giá Đơn", "Cashier, Manager", "Must Have", "Mở PosDiscountModal giảm giá theo % hoặc số tiền. Nếu vượt hạn mức DiscountLimit, yêu cầu mã PIN Quản lý.", "Orders, ApprovalRequests"),
        ("POS-014", "Xác thực PIN Quản lý", "Manager, Cashier", "Must Have", "Mở modal PosManagerPinModal nhập PIN 4-6 số ủy quyền thao tác nhạy cảm, băm BCrypt, khóa 15m nếu sai 5 lần.", "Users, AuditLogs"),
        ("POS-015", "In Tạm tính Phiếu K80", "Cashier", "Must Have", "In phiếu tạm tính cho khách xem trước, bàn chuyển sang màu xanh lá (Billing), đơn hàng không bị đóng.", "Orders, Tables"),
        ("POS-016", "Thanh toán Tiền mặt", "Cashier", "Must Have", "Nhập tiền khách đưa, tự tính tiền thối, cập nhật Order sang Paid, giải phóng bàn ăn và in hóa đơn K80.", "Orders, Payments"),
        ("POS-017", "Thanh toán VietQR Tự động", "Cashier, System", "Must Have", "Tạo mã VietQR động Napas 247, lắng nghe Webhook SePay qua SignalR, tự động đóng modal và in bill.", "Payments, SepayLogs"),
        ("POS-018", "Duyệt Đơn từ QR Ordering", "Cashier", "Must Have", "Nhận đơn Draft do khách quét QR gửi lên, kiểm tra danh sách món và bấm 'Duyệt đơn' để chuyển xuống bếp.", "Orders, OrderItems"),
        ("POS-019", "Mở Ca & Khai báo Két", "Cashier", "Must Have", "Bắt đầu ca làm việc bằng việc chọn máy POS (Register) và nhập số tiền mặt ban đầu trong két (OpeningBalance).", "Shifts, Registers"),
        ("POS-020", "Công cụ Thu ngân (Cashier Tools)", "Cashier, Manager", "Must Have", "Cung cấp PosCashierToolsModal nạp tiền lẻ (Till In), rút nộp két (Till Out) và kiểm kê tiền mặt giữa ca.", "CashMovements, Shifts"),
        ("POS-021", "Kết Ca & In Báo cáo K80", "Cashier", "Must Have", "Đếm két tiền cuối ca, đối soát chênh lệch lý thuyết và thực tế, in báo cáo Z-Report trực tiếp ra máy in K80.", "Shifts, CashMovements")
    ]
    add_table_title(doc, "2.8", "Đặc tả kỹ thuật tính năng Thanh toán & Quản trị Ca (POS-011 đến POS-021)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], pos_part2)

    # 2. KDS Specs
    add_h3(doc, "2.9.3. Đặc tả Chi tiết 7 Chức năng Phân hệ Màn hình Nhà bếp (KDS)")
    add_p(doc, "Phân hệ Nhà bếp KDS số hóa toàn diện quy trình chế biến, loại bỏ hoàn toàn phiếu giấy truyền thống, tự động điều phối món ăn theo trạm chế biến chuyên biệt và cảnh báo vi phạm thời gian phục vụ (SLA):")
    kds_specs = [
        ("KDS-001", "Xem Thẻ Đơn Đa Trạm Bếp", "Chef, Bartender", "Must Have", "Hiển thị vé món theo thứ tự thời gian, hỗ trợ lọc theo trạm chế biến (Bếp Pizza, Bếp Chiên, Quầy Bar) hoặc xem toàn bộ.", "Orders, OrderItems"),
        ("KDS-002", "Nhận Làm Món (Processing)", "Chef", "Must Have", "Đầu bếp chạm vào món chuyển trạng thái sang Processing, đồng bộ tiến độ thời gian thực lên POS và điện thoại khách.", "OrderItems"),
        ("KDS-003", "Hoàn Thành & Trừ Kho BOM", "Chef, System", "Must Have", "Chuyển món sang Done, tự động kích hoạt bóc tách công thức định mức BOM trừ tồn kho Materials nguyên tử và Idempotent.", "Recipes, Materials"),
        ("KDS-004", "Gom Món Tổng hợp", "Chef", "Should Have", "Chế độ tổng hợp số lượng của các món giống nhau từ nhiều đơn chờ để đầu bếp nấu chung một mẻ lớn tối ưu công suất.", "OrderItems, MenuItems"),
        ("KDS-005", "Báo Hết Món (Sold Out)", "Chef", "Must Have", "Đầu bếp bấm báo hết món trên KDS, lập tức vô hiệu hóa món đó trên menu của POS và QR Ordering qua SignalR.", "MenuItems"),
        ("KDS-006", "Cảnh báo Vi phạm SLA", "Chef, System", "Must Have", "Tự động đổi màu thẻ (vàng sau 10m, đỏ nhấp nháy sau 15m), phát âm thanh cảnh báo và bắn tín hiệu SLA lên POS.", "OrderItems, Orders"),
        ("KDS-007", "Xem Lịch sử Đơn Hoàn thành", "Chef", "Nice to Have", "Cho phép chuyển đổi giao diện xem lại danh sách các món và đơn hàng đã hoàn tất chế biến trong ca làm việc.", "Orders, OrderItems")
    ]
    add_table_title(doc, "2.9", "Đặc tả kỹ thuật 7 tính năng phân hệ Màn hình Nhà bếp (KDS)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], kds_specs)

    # 3. BOH Specs - Part 1
    add_h3(doc, "2.9.4. Đặc tả Nhóm Quản trị Thực đơn, Định lượng BOM & Kho bãi (BOH Admin)")
    add_p(doc, "Phân hệ Hậu sảnh BOH Admin là trung tâm cấu hình toàn bộ danh mục thực phẩm, định mức nguyên vật liệu tiêu hao (BOM), sơ đồ sảnh và quy trình nhập mua kiểm kê kho bãi:")
    boh_part1 = [
        ("BOH-001", "Quản lý Danh mục Món", "Manager, Admin", "Must Have", "CRUD danh mục món ăn hiển thị trên POS và QR Menu, ngăn chặn xóa danh mục đang chứa món ăn hoạt động.", "Categories"),
        ("BOH-002", "Quản lý Thực đơn & Món", "Manager, Admin", "Must Have", "CRUD món ăn, cấu hình hình ảnh, giá bán, mô tả, liên kết nhóm tùy chọn modifier và cờ đổi điểm thưởng.", "MenuItems, Categories"),
        ("BOH-003", "Quản lý Gói Combo", "Manager, Admin", "Must Have", "Thiết lập combo và các nhóm lựa chọn món thành phần bắt buộc/tùy chọn kèm giá phụ thu nếu có.", "ComboGroups, ComboItems"),
        ("BOH-004", "Quản lý Tùy chọn & Topping", "Manager, Admin", "Must Have", "Quản lý nhóm Modifier (Size, Topping, Đường, Đá) và cấu hình ràng buộc MinSelections/MaxSelections.", "ModifierGroups, Options"),
        ("BOH-005", "Quản lý Sơ đồ Bàn ăn", "Manager, Admin", "Must Have", "Tạo khu vực sảnh (Tầng 1, Tầng 2), tạo bàn ăn và tự động sinh chuỗi QrToken ngẫu nhiên 32 ký tự.", "Areas, Tables"),
        ("BOH-006", "Cấu hình Định lượng BOM", "Admin", "Must Have", "Thiết lập công thức nguyên vật liệu tiêu hao cho cả món chính (Recipes) và từng tùy chọn modifier (ModifierRecipes).", "Recipes, Materials"),
        ("BOH-007", "Quản lý Đơn vị Tính", "Admin", "Must Have", "Định nghĩa đơn vị đo lường (kg, gram, ml, ly) và hệ số quy đổi giữa đơn vị nhập mua và đơn vị định lượng sản xuất.", "UnitOfMeasures"),
        ("BOH-008", "Quản lý Nhà cung cấp", "Manager, Admin", "Must Have", "CRUD danh bạ đối tác cung cấp nguyên vật liệu phục vụ công tác nhập mua kho hàng.", "Suppliers"),
        ("BOH-009", "Nhập kho (Goods Receipt)", "Stockkeeper, Admin", "Must Have", "Lập phiếu nhập kho, tự động tính tổng tiền nhập và cập nhật tăng số lượng tồn kho nguyên vật liệu thô.", "GoodsReceipts, Materials"),
        ("BOH-010", "Kiểm kê Cân bằng Tồn", "Stockkeeper, Manager", "Must Have", "Tạo đợt kiểm kê đếm thực tế, tự động đối chiếu chênh lệch và tạo giao dịch điều chỉnh cân bằng tồn kho.", "Stocktakes, Transactions")
    ]
    add_table_title(doc, "2.10", "Đặc tả tính năng Thực đơn, Định lượng BOM & Quản lý Kho (BOH-001 đến BOH-010)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], boh_part1)

    # 3. BOH Specs - Part 2
    add_h3(doc, "2.9.5. Đặc tả Nhóm Phê duyệt, Đối soát & Báo cáo Thống kê (BOH Admin)")
    add_p(doc, "Nhóm tính năng phục vụ công tác thanh tra, đối soát doanh thu đa kênh, kiểm soát hao hụt kho và phê duyệt các nghiệp vụ nhạy cảm:")
    boh_part2 = [
        ("BOH-011", "Quản lý Voucher Giảm giá", "Admin", "Must Have", "Thiết lập mã giảm giá theo số tiền cố định hoặc %, hạn mức giảm tối đa, đơn hàng tối thiểu và hạn dùng.", "Vouchers"),
        ("BOH-012", "Quản lý Khách hàng & Hạng", "Admin, Manager", "Must Have", "Quản lý danh sách thành viên CRM, xem lịch sử tích/tiêu điểm và cấu hình tỷ lệ giảm giá theo hạng thẻ.", "Customers, Tiers"),
        ("BOH-013", "Trung tâm Phê duyệt", "Admin, Manager", "Must Have", "Xử lý duyệt hoặc từ chối các yêu cầu nhạy cảm từ POS: hủy món đã nấu, hủy đơn, giảm giá vượt trần, rút két.", "ApprovalRequests"),
        ("BOH-014", "Đối soát Thanh toán & Ca", "Admin, Manager", "Must Have", "Xem danh sách ca, đối soát doanh thu tiền mặt, chuyển khoản VietQR và số chênh lệch két của từng thu ngân.", "Shifts, Reconciliations"),
        ("BOH-015", "Lịch sử Đơn hàng", "Admin, Manager", "Must Have", "Tra cứu toàn bộ đơn hàng theo ngày, trạng thái, bàn ăn; xem chi tiết hóa đơn K80 và nhật ký chế biến.", "Orders, OrderItems"),
        ("BOH-016", "Báo cáo Doanh thu", "Admin, Manager", "Must Have", "Thống kê tổng hợp doanh thu theo ngày/tháng/năm, phân rã theo phương thức thanh toán kèm biểu đồ trực quan.", "Orders, Payments"),
        ("BOH-017", "Báo cáo Món Bán chạy", "Admin, Manager", "Must Have", "Thống kê xếp hạng các món ăn có số lượng bán nhiều nhất và mang lại doanh số cao nhất trong kỳ.", "OrderItems, MenuItems"),
        ("BOH-018", "Báo cáo Hao hụt Kho", "Admin, Manager", "Must Have", "Tổng hợp giá trị thất thoát do lệch kiểm kê và chi phí nguyên vật liệu do hủy món ăn (Cancellation Waste).", "Materials, Stocktakes")
    ]
    add_table_title(doc, "2.11", "Đặc tả tính năng Phê duyệt, Đối soát & Báo cáo (BOH-011 đến BOH-018)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], boh_part2)

    # 3. BOH Specs - Part 3
    add_h3(doc, "2.9.6. Đặc tả Nhóm Nhân sự, Phân quyền & Cấu hình Vận hành (BOH Admin)")
    add_p(doc, "Nhóm tính năng thiết lập hạ tầng quản trị, phân quyền nhân sự theo vai trò RBAC và cài đặt các tham số chính sách vận hành cửa hàng:")
    boh_part3 = [
        ("BOH-019", "Quản lý Nhân sự & Quyền", "Admin", "Must Have", "CRUD tài khoản nhân viên, gán vai trò (Admin, Manager, Cashier, Chef) và phân quyền chi tiết Permissions.", "Users, Roles"),
        ("BOH-020", "Cấu hình Trạm Bếp KDS", "Admin", "Must Have", "Định nghĩa các trạm chế biến (Bếp Pizza, Bếp Lạnh, Quầy Bar) và gán danh mục món ăn tương ứng.", "KitchenStations"),
        ("BOH-021", "Quản lý Máy POS Register", "Admin", "Must Have", "Quản lý danh mục các máy thu ngân (Registers) trong nhà hàng để gắn liền với phiên mở ca làm việc.", "Registers, Shifts"),
        ("BOH-022", "Chính sách POS Policy", "Admin", "Must Have", "Cấu hình hạn mức rút két (TillOutLimit), trần chiết khấu (DiscountLimit) và thời gian cooldown mã PIN.", "PosPolicyOptions"),
        ("BOH-023", "Cấu hình Hệ thống", "Admin", "Must Have", "Cấu hình tên quán, địa chỉ, số điện thoại, mã số thuế, VAT mặc định 8%, lời chào chân trang bill K80.", "SystemSettings"),
        ("BOH-024", "Nhật ký Kiểm toán", "Admin", "Must Have", "Tra cứu nhật ký các thao tác nhạy cảm trong hệ thống nhằm mục đích thanh tra và ngăn chặn gian lận.", "AuditLogEntries")
    ]
    add_table_title(doc, "2.12", "Đặc tả tính năng Nhân sự, Cấu hình & Kiểm toán (BOH-019 đến BOH-024)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], boh_part3)

    # 4. QR Specs
    add_h3(doc, "2.9.7. Đặc tả Chi tiết 8 Chức năng Phân hệ Khách hàng Tự gọi món (QR Ordering)")
    add_p(doc, "Phân hệ QR Ordering mang lại trải nghiệm gọi món hiện đại theo định hướng Mobile-first, cho phép khách hàng chủ động chọn món, theo dõi trạng thái chế biến mà không cần nhân viên đứng cạnh phục vụ:")
    qr_specs = [
        ("QR-001", "Quét QR & Nhận diện Bàn", "Customer", "Must Have", "Quét mã QR tại bàn, xác thực tableId và token, cấp phiên session an toàn và tải thực đơn bàn ăn.", "Tables, Areas"),
        ("QR-002", "Xem Thực đơn Di động", "Customer", "Must Have", "Giao diện Mobile-first xem món theo danh mục cuộn ngang, đồng bộ món hết hàng realtime qua SignalR.", "MenuItems, Categories"),
        ("QR-003", "Chọn Món & Combo Tùy chọn", "Customer", "Must Have", "Chọn món, chọn topping và chọn món thành phần combo qua ngăn kéo trượt QrBottomDrawer.", "ModifierGroups, Combo"),
        ("QR-004", "Quản lý Giỏ hàng Tạm", "Customer", "Must Have", "Modal xem giỏ hàng QrCartModal kiểm tra danh sách món, tùy chọn, số lượng và tổng tiền tạm tính.", "Client Session"),
        ("QR-005", "Nhập Thông tin & Gửi Đơn", "Customer", "Must Have", "Nhập SĐT để tích điểm, gửi đơn Draft lên POS; chặn đặt nếu bàn đang thanh toán, chống spam 30s.", "Orders, OrderItems"),
        ("QR-006", "Đồng bộ Phiên Đổi/Gộp Bàn", "Customer, System", "Must Have", "Nhận sự kiện TableSessionMoved khi POS đổi/gộp bàn, tự động cập nhật URL và bàn mới mà không mất đơn.", "Tables, Orders"),
        ("QR-007", "Theo dõi Tiến độ Nấu nướng", "Customer", "Must Have", "Màn hình QrOrderStatus theo dõi thời gian thực từng món ăn: Chờ duyệt -> Đang nấu -> Hoàn tất.", "Orders, OrderItems"),
        ("QR-008", "Gọi Thêm Món Vào Đơn Đang Ăn", "Customer", "Must Have", "Khách đang ngồi ăn chọn thêm món mới, tự động gộp vào đơn hiện tại ở trạng thái chờ thu ngân duyệt.", "Orders, OrderItems")
    ]
    add_table_title(doc, "2.13", "Đặc tả kỹ thuật 8 tính năng phân hệ Khách hàng Tự gọi món (QR Ordering)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], qr_specs)

    # 5. ACC Specs
    add_h3(doc, "2.9.8. Đặc tả Chi tiết 10 Chức năng Phân hệ Tài khoản & Khách hàng thân thiết (ACC)")
    add_p(doc, "Phân hệ Tài khoản và Khách hàng thân thiết phụ trách quản lý phiên làm việc của nhân sự, mã hóa bảo mật BCrypt và hiện thực hóa chương trình Loyalty tích lũy điểm thưởng:")
    acc_specs = [
        ("ACC-001", "Đăng nhập Hệ thống", "All Staff", "Must Have", "Đăng nhập bằng Username/Password, trả về JWT Token và thiết lập Cookie RestaurantAuth cho Blazor.", "Users, Roles"),
        ("ACC-002", "Đăng xuất Hệ thống", "All Staff", "Must Have", "Xóa toàn bộ token và cookie xác thực, chuyển hướng về trang đăng nhập an toàn.", "Session"),
        ("ACC-003", "Xác thực PIN Quản lý", "Admin, Manager", "Must Have", "Nhập mã PIN ủy quyền tại quầy, băm bảo mật BCrypt, khóa tạm thời 15 phút nếu sai quá 5 lần.", "Users, AuditLogs"),
        ("ACC-004", "Đăng ký Khách Thành viên", "Cashier, Manager", "Must Have", "Đăng ký thành viên mới tại quầy POS bằng SĐT duy nhất, tự động gán hạng Đồng ban đầu.", "Customers, Tiers"),
        ("ACC-005", "Tích Điểm Loyalty Tự động", "System", "Must Have", "Tự động tích điểm theo giá trị thanh toán thực tế của hóa đơn, cập nhật thăng hạng thẻ tự động.", "Customers, Loyalty"),
        ("ACC-006", "Tiêu Điểm Trừ Tiền Mặt", "Cashier", "Must Have", "Quy đổi điểm khả dụng thành tiền mặt giảm trừ trực tiếp vào đơn hàng tại quầy POS.", "Orders, Customers"),
        ("ACC-007", "Đổi Điểm Lấy Món Thưởng", "Cashier", "Must Have", "Đổi điểm tích lũy lấy món ăn/đồ uống thuộc nhóm Đổi Điểm với đơn giá 0đ.", "MenuItems, Customers"),
        ("ACC-008", "Chiết khấu Theo Hạng Thẻ", "System, Cashier", "Must Have", "Tự động áp dụng tỷ lệ chiết khấu (%) theo hạng thành viên (CustomerTier.DiscountPercent).", "Customers, Orders"),
        ("ACC-009", "Tra cứu Lịch sử Điểm", "Admin, Manager", "Must Have", "Tra cứu nhật ký biến động điểm tích lũy của khách hàng phục vụ đối soát và kiểm tra khiếu nại.", "LoyaltyTransactions"),
        ("ACC-010", "Xử lý Hủy đơn & Rollback Điểm", "Admin, Manager", "Must Have", "Tự động hoàn lại điểm đã tiêu và thu hồi điểm đã tích của đơn hủy trong 1 Database Transaction.", "Orders, Loyalty")
    ]
    add_table_title(doc, "2.14", "Đặc tả kỹ thuật 10 tính năng phân hệ Tài khoản & Loyalty (ACC)")
    style_table(doc, [Inches(1.0), Inches(1.8), Inches(1.1), Inches(1.0), Inches(2.7), Inches(1.2)], 
                ["Mã FT", "Tên Tính năng", "Actor", "Ưu tiên", "Mô tả Luồng Nghiệp vụ", "Bảng tương tác"], acc_specs)

    # 6. Database schema detailed tables
    add_h2(doc, "2.10. Đặc tả Chi tiết Lược đồ Thực thể Cơ sở Dữ liệu (42 DbSets)")
    add_p(doc, "Dưới đây là đặc tả chi tiết cấu trúc trường, kiểu dữ liệu, ràng buộc khóa chính (PK), khóa ngoại (FK) và quy tắc nghiệp vụ của các bảng dữ liệu cốt lõi trong hệ thống quản lý nhà hàng:")

    add_h3(doc, "2.10.1. Bảng Đơn hàng (Orders) và Dòng món ăn (OrderItems)")
    add_p(doc, "Bảng `Orders` lưu trữ thông tin hóa đơn bán hàng tổng thể, trang bị khóa lạc quan `RowVersion` để giải quyết xung đột khi nhiều thu ngân cùng truy cập một đơn hàng:")
    add_table_title(doc, "2.15", "Cấu trúc trường chi tiết bảng Orders")
    orders_fields = [
        ["Id", "int", "PK, Identity", "Mã định danh duy nhất của đơn hàng"],
        ["OrderCode", "nvarchar(30)", "Unique, Not Null", "Mã hiển thị của đơn (ví dụ: ORD-20261030-001)"],
        ["TableId", "int", "FK -> Tables.Id, Nullable", "Mã bàn ăn (Dine-in), để trống nếu là Takeaway"],
        ["UserId", "int", "FK -> Users.Id, Nullable", "Mã nhân viên thu ngân mở đơn"],
        ["CustomerId", "int", "FK -> Customers.Id, Nullable", "Mã khách hàng thân thiết được liên kết"],
        ["Status", "int", "Not Null", "Trạng thái đơn (0: Draft, 1: Confirmed, 2: Paid, 3: Cancelled)"],
        ["OrderType", "int", "Not Null", "Loại hình phục vụ (0: DineIn, 1: Takeaway, 2: Delivery)"],
        ["SubTotal", "decimal(18,2)", "Not Null", "Tổng tiền món ăn nguyên giá trước chiết khấu"],
        ["DiscountAmount", "decimal(18,2)", "Not Null", "Số tiền được giảm giá (chiết khấu/voucher/tiêu điểm)"],
        ["VatAmount", "decimal(18,2)", "Not Null", "Tiền thuế GTGT tính trên đơn hàng (mặc định 8%)"],
        ["TotalAmount", "decimal(18,2)", "Not Null", "Tổng số tiền thực tế khách hàng phải thanh toán"],
        ["RowVersion", "byte[]", "RowVersion", "Khóa kiểm soát tương tranh lạc quan (Optimistic Concurrency)"],
        ["CreatedAt", "datetime2", "Not Null", "Thời điểm tạo đơn hàng"]
    ]
    style_table(doc, [Inches(1.4), Inches(1.3), Inches(1.8), Inches(4.3)], ["Tên Trường", "Kiểu Dữ liệu", "Ràng buộc", "Mô tả Nghiệp vụ"], orders_fields)

    add_p(doc, "Bảng `OrderItems` chi tiết hóa từng món ăn trong đơn, lưu trữ đơn giá snapshot và trạng thái chế biến tại bếp:")
    add_table_title(doc, "2.16", "Cấu trúc trường chi tiết bảng OrderItems")
    orderitems_fields = [
        ["Id", "int", "PK, Identity", "Mã định danh duy nhất của dòng món ăn"],
        ["OrderId", "int", "FK -> Orders.Id, Not Null", "Mã đơn hàng liên kết"],
        ["MenuItemId", "int", "FK -> MenuItems.Id, Not Null", "Mã món ăn trên thực đơn"],
        ["Quantity", "int", "Not Null", "Số lượng suất món được gọi"],
        ["UnitPrice", "decimal(18,2)", "Not Null", "Đơn giá snapshot tại thời điểm gọi món"],
        ["TotalPrice", "decimal(18,2)", "Not Null", "Tổng tiền của dòng món (bao gồm phụ thu modifier)"],
        ["KitchenStatus", "int", "Not Null", "Trạng thái chế biến bếp (0: Pending, 1: Processing, 2: Done)"],
        ["KitchenStationId", "int", "FK -> KitchenStations.Id", "Mã trạm bếp được phân công chế biến món này"],
        ["SentToKitchenAt", "datetime2", "Nullable", "Thời điểm bắt đầu gửi hoặc nhận nấu tại bếp"],
        ["CompletedAt", "datetime2", "Nullable", "Thời điểm đầu bếp bấm hoàn tất món ăn"],
        ["Note", "nvarchar(255)", "Nullable", "Ghi chú chế biến của khách (ví dụ: không cay, ít đường)"]
    ]
    style_table(doc, [Inches(1.5), Inches(1.3), Inches(1.8), Inches(4.2)], ["Tên Trường", "Kiểu Dữ liệu", "Ràng buộc", "Mô tả Nghiệp vụ"], orderitems_fields)

    add_h3(doc, "2.10.2. Bảng Ca làm việc (Shifts) và Biến động Két tiền (CashMovements)")
    add_p(doc, "Bảng `Shifts` theo dõi toàn bộ tiền mặt vào/ra trong phiên làm việc của nhân viên thu ngân, phục vụ đối soát kết ca:")
    add_table_title(doc, "2.17", "Cấu trúc trường chi tiết bảng Shifts")
    shifts_fields = [
        ["Id", "int", "PK, Identity", "Mã định danh ca làm việc"],
        ["UserId", "int", "FK -> Users.Id, Not Null", "Mã nhân viên thu ngân phụ trách ca"],
        ["RegisterId", "int", "FK -> Registers.Id, Not Null", "Mã máy tính tiền POS được chỉ định trong ca"],
        ["StartTime", "datetime2", "Not Null", "Thời điểm bắt đầu mở ca làm việc"],
        ["EndTime", "datetime2", "Nullable", "Thời điểm kết ca làm việc"],
        ["Status", "int", "Not Null", "Trạng thái ca (0: Open - Đang mở, 1: Closed - Đã đóng)"],
        ["OpeningBalance", "decimal(18,2)", "Not Null", "Số tiền mặt ban đầu có sẵn trong két tiền"],
        ["ExpectedCash", "decimal(18,2)", "Not Null", "Số tiền mặt lý thuyết hệ thống tính toán"],
        ["ActualCash", "decimal(18,2)", "Not Null", "Số tiền mặt thực tế thu ngân đếm được khi kết ca"],
        ["Difference", "decimal(18,2)", "Not Null", "Chênh lệch giữa tiền thực tế và lý thuyết (thừa/thiếu)"],
        ["ClosingNote", "nvarchar(500)", "Nullable", "Ghi chú giải trình của thu ngân khi kết thúc ca"]
    ]
    style_table(doc, [Inches(1.5), Inches(1.3), Inches(1.8), Inches(4.2)], ["Tên Trường", "Kiểu Dữ liệu", "Ràng buộc", "Mô tả Nghiệp vụ"], shifts_fields)

    add_h3(doc, "2.10.3. Bảng Nguyên vật liệu (Materials) và Công thức Định lượng (Recipes)")
    add_p(doc, "Bảng `Materials` và `Recipes` cấu thành hạt nhân quản lý định lượng tiêu hao nguyên liệu BOM, tự động trừ tồn kho khi món hoàn tất:")
    add_table_title(doc, "2.18", "Cấu trúc trường chi tiết bảng Materials và Recipes")
    materials_fields = [
        ["Materials.Id", "int", "PK, Identity", "Mã nguyên vật liệu thô"],
        ["Materials.Code", "nvarchar(50)", "Unique, Not Null", "Mã quản lý nguyên liệu (ví dụ: MAT-THIT-BO)"],
        ["Materials.Name", "nvarchar(150)", "Not Null", "Tên nguyên vật liệu"],
        ["Materials.BaseUnitId", "int", "FK -> UnitOfMeasures", "Mã đơn vị tính cơ sở (kg, gram, lít, lon)"],
        ["Materials.CurrentStock", "decimal(18,4)", "Not Null", "Số lượng tồn kho thực tế hiện tại"],
        ["Materials.MinStockLevel", "decimal(18,4)", "Not Null", "Ngưỡng cảnh báo tồn kho tối thiểu"],
        ["Materials.CostPrice", "decimal(18,2)", "Not Null", "Giá vốn bình quân của nguyên vật liệu"],
        ["Recipes.Id", "int", "PK, Identity", "Mã công thức định lượng món ăn"],
        ["Recipes.MenuItemId", "int", "FK -> MenuItems.Id", "Mã món ăn được thiết lập công thức"],
        ["RecipeItems.MaterialId", "int", "FK -> Materials.Id", "Mã nguyên liệu thô cấu thành nên món ăn"],
        ["RecipeItems.Quantity", "decimal(18,4)", "Not Null", "Định mức tiêu hao nguyên liệu cho 1 đơn vị món"]
    ]
    style_table(doc, [Inches(1.7), Inches(1.3), Inches(1.8), Inches(4.0)], ["Tên Trường", "Kiểu Dữ liệu", "Ràng buộc", "Mô tả Nghiệp vụ"], materials_fields)

print("Module Append Specs optimized with balanced tables and deep technical narrative!")
