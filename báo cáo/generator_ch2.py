import docx
from doc_helpers import add_p, add_h1, add_h2, add_h3, add_table_title, add_figure, add_side_by_side_figures, style_table
from docx.shared import Inches

def build_chapter_2(doc):
    add_h1(doc, "CHƯƠNG 2: PHƯƠNG PHÁP NGHIÊN CỨU VÀ THIẾT KẾ KIẾN TRÚC HỆ THỐNG")

    add_h2(doc, "2.1. Phương pháp nghiên cứu và quy trình phát triển phần mềm")
    
    add_h3(doc, "2.1.1. Phương pháp luận nghiên cứu và thực nghiệm")
    add_p(doc, "Đề tài được triển khai dựa trên phương pháp nghiên cứu thực nghiệm kỹ thuật phần mềm (Empirical Software Engineering), kết hợp giữa khảo sát thực tế tại các cơ sở kinh doanh F&B, mô hình hóa toán học các luồng nghiệp vụ và hiện thực hóa thông qua việc xây dựng một hệ thống phần mềm hoàn chỉnh có khả năng vận hành thực tế.")

    add_p(doc, "Quy trình nghiên cứu và phát triển được tổ chức theo mô hình Agile Scrum lặp linh hoạt, chia làm 4 giai đoạn nối tiếp nhau:")
    add_p(doc, "• Giai đoạn 1 - Khảo sát nghiệp vụ và đặc tả yêu cầu: Nghiên cứu kỹ lưỡng quy trình vận hành chuỗi nhà hàng; phân tích các điểm nghẽn trong chu trình phục vụ và kiểm soát kho; xây dựng bộ tài liệu Đặc tả Kỹ thuật (Spec Kit) bao gồm 70 tính năng chi tiết, xác định rõ Actor, Priority, Luồng chính, Tiêu chí chấp nhận (Given - When - Then) và Bảng dữ liệu tương tác.")
    add_p(doc, "• Giai đoạn 2 - Thiết kế kiến trúc và mô hình dữ liệu: Thiết kế hệ thống theo chuẩn Clean Architecture 5 tầng của Robert C. Martin [11]; thiết kế cơ sở dữ liệu quan hệ gồm 42 thực thể chuẩn hóa; xây dựng các sơ đồ Use Case tổng thể và phân rã, sơ đồ lớp đối tượng (Class Diagrams), sơ đồ luồng dữ liệu (Data Flow Diagrams), sơ đồ chuỗi (Sequence Diagrams) và thiết kế hệ thống giao tiếp thời gian thực SignalR Hub.")
    add_p(doc, "• Giai đoạn 3 - Hiện thực hóa mã nguồn (Implementation): Lập trình phân tầng từ tầng Core Domain theo mô hình thiết kế hướng miền Domain-Driven Design (DDD) của Eric Evans [12], Application Interfaces, Infrastructure Persistence cho đến WebAPI Controllers và tầng giao diện tương tác Blazor Server. Áp dụng phương pháp Phát triển Hướng kiểm thử (Test-Driven Development - TDD) song song với quá trình viết mã.")
    add_p(doc, "• Giai đoạn 4 - Kiểm thử, tối ưu hóa và đánh giá: Xây dựng bộ kịch bản kiểm thử tự động với framework xUnit; kiểm thử độ trễ truyền thông điệp SignalR; kiểm thử tải xử lý giao dịch thanh toán đồng thời; đo lường hiệu năng và tinh chỉnh hệ thống.")

    add_h2(doc, "2.2. Phân tích yêu cầu chức năng và phi chức năng")
    
    add_h3(doc, "2.2.1. Phân tích chi tiết 70 yêu cầu chức năng (Functional Requirements)")
    add_p(doc, "Toàn bộ hệ thống quản lý nhà hàng bao gồm 70 yêu cầu chức năng cốt lõi, được phân chia thành 5 phân hệ nghiệp vụ độc lập nhưng kết nối chặt chẽ:")
    add_p(doc, "• Phân hệ Bán hàng POS (21 Features): Quản lý sơ đồ bàn trực quan; Đơn ăn tại bàn Dine-in; Đơn mang đi chuyên dụng Takeaway; Thêm món, combo, modifier; Đổi điểm thưởng lấy món; Hủy món (Void Item) phân quyền; Hủy đơn; Chuyển bàn, gộp bàn, tách hóa đơn; Khách hàng thân thiết; Tiêu điểm và voucher; Chiết khấu đơn hàng; Xác thực mã PIN Quản lý; In phiếu tạm tính K80; Thanh toán tiền mặt và VietQR/SePay tự động; Duyệt đơn QR; Quản lý ca mở/đóng, nạp rút két (Till In/Out) và in báo cáo kết ca K80 (Z-Report).")
    add_p(doc, "• Phân hệ Màn hình Nhà bếp KDS (7 Features): Xem thẻ đơn theo thứ tự thời gian; Lọc món theo trạm chế biến (Kitchen Stations); Nhận làm món (Processing); Hoàn thành món (Done) và kích hoạt trừ tồn kho BOM tự động; Gom món tổng hợp (Batch Aggregation); Báo hết món (Sold Out); Cảnh báo vi phạm thời gian phục vụ (SLA Alerts); Xem lịch sử món hoàn thành.")
    add_p(doc, "• Phân hệ Quản trị Hậu sảnh BOH Admin (24 Features): Quản lý danh mục, thực đơn, combo, modifier, sơ đồ bàn và QrToken; Cấu hình công thức định lượng BOM cho món và modifier; Quản lý đơn vị tính và quy đổi; Quản lý nhà cung cấp, nhập kho, kiểm kê cân bằng tồn; Quản lý voucher; Khách hàng CRM và hạng thẻ; Trung tâm phê duyệt (Approval Center); Đối soát thanh toán và ca làm việc; Báo cáo doanh thu, món bán chạy, hao hụt kho; Phân quyền nhân sự; Cấu hình trạm bếp, máy POS, POS Policy và System Settings; Nhật ký kiểm toán (Audit Logs).")
    add_p(doc, "• Phân hệ Khách hàng Tự gọi món QR Ordering (8 Features): Quét mã QR nhận diện bàn và cấp phiên; Thực đơn di động Mobile-first; Chọn món kèm tùy chọn và combo; Quản lý giỏ hàng; Nhập thông tin thành viên và gửi đơn; Đồng bộ phiên khi đổi/gộp bàn (TableSessionMoved); Theo dõi tiến độ chế biến; Gọi thêm món vào đơn đang phục vụ.")
    add_p(doc, "• Phân hệ Tài khoản & Khách hàng thân thiết (10 Features): Đăng nhập xác thực kép JWT/Cookie; Đăng xuất; Xác thực mã PIN Quản lý BCrypt có cơ chế khóa tạm thời 15 phút sau 5 lần sai; Đăng ký thành viên; Tích điểm tự động; Tiêu điểm trừ tiền mặt; Đổi điểm lấy món thưởng; Chiết khấu tự động theo hạng thành viên; Lịch sử giao dịch điểm và hoàn trả điểm (Rollback) nguyên tử.")

    add_h3(doc, "2.2.2. Phân tích các yêu cầu phi chức năng (Non-Functional Requirements)")
    add_p(doc, "• Hiệu năng (Performance): Thời gian phản hồi của các thao tác bấm chọn trên giao diện POS và KDS phải đạt dưới 300ms; thời gian hoàn tất một giao dịch thanh toán tiền mặt dưới 500ms; độ trễ truyền phát tín hiệu SignalR giữa các thiết bị dưới 200ms trong mạng nội bộ và dưới 500ms qua Internet.")
    add_p(doc, "• Tính sẵn sàng và chịu tải (Availability & Scalability): Hệ thống phải duy trì hoạt động ổn định liên tục với chỉ số sẵn sàng 99.9%, hỗ trợ phục vụ đồng thời trên 100 bàn ăn với hàng nghìn đơn hàng trong ngày mà không bị suy giảm hiệu năng.")
    add_p(doc, "• Tính bảo mật (Security): Mật khẩu đăng nhập và mã PIN Quản lý bắt buộc phải được mã hóa một chiều bằng thuật toán BCrypt với hệ số Salt an toàn; các API nhạy cảm được bảo vệ nghiêm ngặt bằng JWT Bearer Token và phân quyền theo Claims Role; chống các lỗ hổng bảo mật phổ biến như XSS, CSRF và SQL Injection.")
    add_p(doc, "• Tính toàn vẹn dữ liệu (Data Integrity): Toàn bộ các luồng thay đổi trạng thái phức tạp (thanh toán hóa đơn, trừ kho định lượng BOM, cộng/trừ điểm thành viên) bắt buộc phải được thực thi trong một Database Transaction duy nhất để đảm bảo nguyên lý ACID (Atomicity, Consistency, Isolation, Durability).")

    # ==========================================
    # 2.3. PHÂN TÍCH CA SỬ DỤNG (USE CASE ANALYSIS)
    # ==========================================
    add_h2(doc, "2.3. Phân tích Ca sử dụng (Use Case Analysis)")
    add_p(doc, "Mô hình hóa Ca sử dụng (Use Case Modeling) đóng vai trò then chốt trong việc xác định ranh giới hệ thống, các tác nhân tương tác (Actors) và hành vi chức năng mà hệ thống cung ứng cho từng nhóm người dùng. Toàn bộ hệ thống quản lý nhà hàng phân định rõ ràng 6 tác nhân (5 tác nhân người dùng và 1 tác nhân hệ thống ngoài):")
    add_p(doc, "• Khách hàng (Customer): Người trực tiếp trải nghiệm dịch vụ tại bàn, sử dụng thiết bị di động cá nhân để quét mã QR động, duyệt thực đơn, chọn món kèm tùy chọn modifier và gửi đơn gọi món.")
    add_p(doc, "• Nhân viên Thu ngân (Cashier): Người trực tiếp vận hành máy POS tại quầy, quản lý sơ đồ bàn, duyệt đơn QR, tạo đơn trực tiếp, thao tác thanh toán tiền mặt/chuyển khoản và chốt ca làm việc cá nhân.")
    add_p(doc, "• Đầu bếp / Nhân viên Pha chế (Kitchen Chef): Người vận hành màn hình KDS tại khu vực bếp, tiếp nhận danh sách món cần chế biến theo từng trạm, cập nhật tiến độ nấu và báo hết món realtime.")
    add_p(doc, "• Quản lý nhà hàng tại ca (Store Manager): Người chịu trách nhiệm giám sát ca trực tiếp, sử dụng mã PIN bảo mật (Manager PIN) để phê duyệt các nghiệp vụ nhạy cảm (hủy món đang nấu, hủy đơn hàng, nạp/rút két vượt hạn mức, duyệt chênh lệch két tiền khi đóng ca).")
    add_p(doc, "• Quản trị viên hệ thống (System Administrator): Người có quyền hạn cao nhất trong hệ thống BOH Admin, chịu trách nhiệm quản trị tài khoản nhân sự, phân quyền RBAC, thiết lập danh mục thực đơn, cấu hình công thức định mức BOM, kiểm kê kho và cài đặt tham số toàn hệ thống (System Settings).")
    add_p(doc, "• Cổng thanh toán VietQR SePay (External System Actor): Hệ thống ngoài tương tác tự động thông qua giao thức Webhook nhằm đối soát biến động số dư ngân hàng và xác nhận giao dịch thanh toán tức thời.")

    add_h3(doc, "2.3.1. Biểu đồ Use Case Tổng thể Toàn Hệ thống")
    add_p(doc, "Biểu đồ Use Case tổng thể phác họa bức tranh toàn cảnh về sự tương tác giữa 5 tác nhân với 12 nhóm chức năng nghiệp vụ trọng tâm trong ranh giới hệ thống:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_usecase_overall.png", "2.1", "Biểu đồ Use Case Tổng thể Toàn Hệ thống Quản lý Nhà hàng", width_inch=5.8)

    add_p(doc, "Theo Hình 2.1, ranh giới hệ thống phân chia rõ ràng trách nhiệm của từng tác nhân: Khách hàng kết nối qua phân hệ QR Ordering; Thu ngân kiểm soát toàn diện luồng phục vụ sảnh và thu ngân qua POS; Đầu bếp tập trung vào tiến độ chế biến KDS và kích hoạt trừ tồn kho BOM; Quản lý điều hành các quy trình hậu sảnh BOH; Cổng thanh toán SePay tự động giao tiếp hai chiều với chức năng thanh toán.")

    add_h3(doc, "2.3.2. Biểu đồ Use Case Phân rã theo từng Phân hệ Nghiệp vụ")
    add_p(doc, "Để đi sâu vào chi tiết kỹ thuật của từng phân hệ, các biểu đồ Use Case phân rã dưới đây làm rõ các quan hệ bao hàm (<<include>>) và mở rộng (<<extend>>) giữa các ca sử dụng nội bộ:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_usecase_qr.png", "2.2a", "Biểu đồ Use Case Phân rã Phân hệ Tự gọi món QR Ordering", width_inch=5.2)

    add_p(doc, "Phân hệ QR Ordering (Hình 2.2a) mô tả hành trình trải nghiệm tự phục vụ của khách hàng: Quét mã QR nhận diện bàn là tiền đề kích hoạt phiên gọi món; Khách hàng xem thực đơn, chọn món có thể tùy biến thêm các topping (<<extend>>); Khi gửi đơn gọi món, hệ thống bắt buộc kiểm tra tính hợp lệ của bàn ăn và cấp phiên đặt món tương ứng (<<include>>). Ngoài ra, khách hàng có thể tra cứu lịch sử và theo dõi tiến độ món ăn trên KDS theo thời gian thực.")

    add_figure(doc, "báo cáo/figures/mermaid/spec_usecase_pos.png", "2.2b", "Biểu đồ Use Case Phân rã Phân hệ Bán hàng POS tại Quầy", width_inch=5.4)

    add_p(doc, "Phân hệ Bán hàng POS (Hình 2.2b) thể hiện chuỗi nghiệp vụ vận hành của Thu ngân: Thao tác thanh toán đơn hàng bao hàm in hóa đơn K80 và cập nhật trạng thái bàn ăn (<<include>>); Thu ngân có thể áp dụng mã giảm giá voucher hoặc chiết khấu thành viên (<<extend>>); Các hành động nhạy cảm như Hủy món đang nấu (Void Item) hoặc Hủy toàn bộ đơn hàng bắt buộc phải được mở rộng bởi ca sử dụng 'Xác thực mã PIN Quản lý' (<<extend>>); Quản lý ca làm việc gắn liền với việc lập phiếu nạp/rút két và in báo cáo kết ca K80 (Z-Report).")

    add_figure(doc, "báo cáo/figures/mermaid/spec_usecase_kds.png", "2.2c", "Biểu đồ Use Case Phân rã Phân hệ Màn hình Nhà bếp KDS", width_inch=5.2)

    add_p(doc, "Phân hệ Màn hình Nhà bếp KDS (Hình 2.2c) tối ưu hóa năng suất chế biến: Đầu bếp xem hàng đợi đơn theo thứ tự thời gian; Thao tác hoàn thành món ăn tự động bao hàm việc kích hoạt trừ tồn kho nguyên vật liệu theo định mức BOM (<<include>>) và phát thông điệp SignalR thông báo món đã sẵn sàng phục vụ; Đầu bếp có thể gom các món giống nhau để nấu cùng mẻ (Batch Aggregation) hoặc báo hết món (Sold Out) để khóa đặt hàng tức thì.")

    add_figure(doc, "báo cáo/figures/mermaid/spec_usecase_admin.png", "2.2d", "Biểu đồ Use Case Phân rã Phân hệ Quản trị Hậu sảnh BOH Admin", width_inch=5.4)

    add_p(doc, "Phân hệ BOH Admin (Hình 2.2d) quản trị toàn bộ tài nguyên nhà hàng: Thiết lập công thức BOM cho món ăn và modifier; Thực hiện kiểm kê định kỳ và cân bằng tồn kho; Phê duyệt các yêu cầu hủy món và chiết khấu đặc biệt; Xuất báo cáo doanh thu tài chính và phân tích hao hụt nguyên liệu.")

    add_h3(doc, "2.3.3. Bảng Đặc tả Chi tiết các Ca sử dụng Cốt lõi (Use Case Specifications)")
    add_p(doc, "Dưới đây là 6 bảng đặc tả Use Case chi tiết tương ứng với các luồng nghiệp vụ quan trọng nhất trong hệ thống:")

    # UC-01
    add_table_title(doc, "2.1", "Đặc tả Use Case UC-01: Đặt món tại bàn qua mã QR (QR Ordering)")
    uc1_data = [
        ["Mã Use Case", "UC-01 (Phân hệ QR Ordering)"],
        ["Tên Use Case", "Đặt món tại bàn thông qua quét mã QR di động"],
        ["Tác nhân (Actor)", "Khách hàng (Customer)"],
        ["Điều kiện tiên quyết", "Bàn ăn đã được dán mã QR tĩnh có chứa QrToken hợp lệ; Thiết bị khách có kết nối Internet."],
        ["Luồng sự kiện chính", "1. Khách dùng camera quét mã QR, trình duyệt mở trang web đặt món.\n2. Hệ thống kiểm tra QrToken, nạp danh mục và món ăn đang khả dụng.\n3. Khách chọn món, kích cỡ, mức đường/đá, chọn topping trong ModifierGroup.\n4. Khách kiểm tra giỏ hàng, nhập số điện thoại thành viên (nếu có) và bấm 'Gửi đơn'.\n5. Hệ thống tạo đơn hàng ở trạng thái Draft, gửi thông báo SignalR đến quầy POS."],
        ["Luồng ngoại lệ", "3a. Món ăn bị đầu bếp báo hết (IsSoldOut = true): Hệ thống hiển thị cảnh báo và khóa nút thêm món.\n4a. Bàn đang bị khóa thanh toán (Billing): Khách không thể gửi đơn, màn hình yêu cầu liên hệ nhân viên."],
        ["Điều kiện sau", "Đơn hàng mới ở trạng thái Draft được tạo trong CSDL; Biểu tượng bàn trên POS nhấp nháy màu vàng."],
        ["Quy tắc nghiệp vụ", "Snapshot giá bán tại thời điểm đặt; Mỗi phiên gọi món được gắn chặt với TableSessionId."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc1_data)

    # UC-02
    add_table_title(doc, "2.2", "Đặc tả Use Case UC-02: Tiếp nhận và duyệt đơn hàng tại quầy POS")
    uc2_data = [
        ["Mã Use Case", "UC-02 (Phân hệ POS)"],
        ["Tên Use Case", "Tiếp nhận và Phê duyệt đơn gọi món QR"],
        ["Tác nhân (Actor)", "Nhân viên Thu ngân (Cashier)"],
        ["Điều kiện tiên quyết", "Thu ngân đã đăng nhập hệ thống POS và đang trong ca làm việc mở (Shift Open)."],
        ["Luồng sự kiện chính", "1. Nhận thông báo âm thanh và hiệu ứng nhấp nháy từ bàn có đơn Draft mới.\n2. Thu ngân bấm vào bàn để mở giao diện chi tiết danh sách món khách gọi.\n3. Thu ngân đối soát tính hợp lệ của đơn và nhấn nút 'Duyệt đơn'.\n4. Hệ thống chuyển trạng thái đơn sang Confirmed, tách các dòng món xuống trạm bếp tương ứng.\n5. Phát sự kiện SignalR KitchenTicketReceived đến màn hình KDS nhà bếp."],
        ["Luồng ngoại lệ", "3a. Thu ngân phát hiện khách gọi nhầm hoặc quầy hết món: Bấm 'Từ chối duyệt', nhập lý do từ chối. Đơn hàng chuyển sang Cancelled, thông báo đẩy về máy khách."],
        ["Điều kiện sau", "Đơn chuyển sang Confirmed; Bàn chuyển sang màu đỏ Occupied; Thẻ đơn xuất hiện trên KDS."],
        ["Quy tắc nghiệp vụ", "Thu ngân chỉ được duyệt khi ca làm việc còn hiệu lực; Mọi thao tác từ chối đều ghi AuditLog."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc2_data)

    # UC-03
    add_table_title(doc, "2.3", "Đặc tả Use Case UC-03: Điều phối và chế biến món ăn tại KDS")
    uc3_data = [
        ["Mã Use Case", "UC-03 (Phân hệ KDS)"],
        ["Tên Use Case", "Điều phối chế biến và cập nhật tiến độ món ăn"],
        ["Tác nhân (Actor)", "Đầu bếp / Pha chế (Kitchen Chef)"],
        ["Điều kiện tiên quyết", "Màn hình KDS đã kết nối vào đúng nhóm trạm bếp (KitchenStation Group) qua SignalR."],
        ["Luồng sự kiện chính", "1. Thẻ món mới xuất hiện trên KDS ở trạng thái Pending kèm thời gian đếm xuôi.\n2. Đầu bếp bấm 'Nhận làm': Dòng món chuyển sang Processing, đổi màu vàng cam.\n3. Sau khi nấu xong, đầu bếp bấm 'Hoàn thành': Dòng món chuyển sang Done, đổi màu xanh lá.\n4. Hệ thống tự động kích hoạt trừ tồn kho nguyên vật liệu theo công thức BOM đã cấu hình.\n5. Phát tín hiệu SignalR OrderItemStatusChanged tới máy POS và thiết bị khách hàng."],
        ["Luồng ngoại lệ", "2a. Món ăn pha chế nhanh: Đầu bếp có thể nhấn 'Hoàn thành' trực tiếp từ trạng thái Pending.\n4a. Kho hàng bị âm tồn nguyên vật liệu: Hệ thống vẫn ghi nhận trừ kho và gửi cảnh báo đỏ cho Quản lý."],
        ["Điều kiện sau", "Trạng thái dòng món cập nhật Done; Tồn kho nguyên liệu bị giảm trừ tức thời; Khách nhận thông báo."],
        ["Quy tắc nghiệp vụ", "Trừ kho tự động tuân thủ nguyên tắc Transaction ACID; Nếu toàn bộ món Done, đơn chuyển Completed."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc3_data)

    # UC-04
    add_table_title(doc, "2.4", "Đặc tả Use Case UC-04: Thanh toán hóa đơn và đối soát tự động VietQR")
    uc4_data = [
        ["Mã Use Case", "UC-04 (Phân hệ POS & SePay Gateway)"],
        ["Tên Use Case", "Thanh toán hóa đơn đa phương thức (Tiền mặt / VietQR)"],
        ["Tác nhân (Actor)", "Thu ngân (Cashier), Cổng SePay (External System)"],
        ["Điều kiện tiên quyết", "Đơn hàng đang ở trạng thái Confirmed hoặc Completed; Bàn đang mở."],
        ["Luồng sự kiện chính", "1. Thu ngân bấm 'Thanh toán' trên POS; màn hình hiển thị tổng tiền và các phương thức.\n2. Thu ngân chọn phương thức Chuyển khoản VietQR: POS sinh mã QR động chứa đúng số tiền và nội dung chuyển khoản chuẩn.\n3. Khách quét mã VietQR bằng ứng dụng ngân hàng và hoàn tất chuyển tiền 24/7.\n4. SePay nhận biến động số dư ngân hàng, gửi HTTP POST Webhook tới hệ thống.\n5. Hệ thống xác thực chữ ký số API Token, đối chiếu số tiền và mã hóa đơn.\n6. Cập nhật Payment thành Completed, Order thành Paid; tự động in hóa đơn K80 và giải phóng bàn ăn."],
        ["Luồng ngoại lệ", "2a. Khách chọn Tiền mặt: Thu ngân nhập số tiền khách đưa, hệ thống tính tiền thối, bấm Hoàn tất để đóng đơn.\n5a. Webhook bị gửi lặp: Hệ thống kiểm tra IdempotencyKey và bỏ qua nếu đã xử lý."],
        ["Điều kiện sau", "Đơn hàng chuyển Paid; Bàn ăn giải phóng về Available; Tích điểm thành viên tự động."],
        ["Quy tắc nghiệp vụ", "Giao dịch thanh toán khóa chống sửa đổi; Doanh thu ghi nhận ngay vào ca làm việc hiện tại."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc4_data)

    # UC-05
    add_table_title(doc, "2.5", "Đặc tả Use Case UC-05: Quản lý ca làm việc và chốt két thu ngân (Z-Report)")
    uc5_data = [
        ["Mã Use Case", "UC-05 (Phân hệ POS & Quản lý Ca)"],
        ["Tên Use Case", "Mở ca, quản lý tiền két và đóng ca in báo cáo Z-Report"],
        ["Tác nhân (Actor)", "Nhân viên Thu ngân (Cashier), Quản lý (Manager)"],
        ["Điều kiện tiên quyết", "Thu ngân đã đăng nhập tài khoản; Chưa có ca làm việc nào đang mở trên máy POS này."],
        ["Luồng sự kiện chính", "1. Đầu ca: Thu ngân nhập số tiền mặt đầu ca trong két (OpeningBalance), bấm 'Mở ca'.\n2. Trong ca: Nếu đổi tiền lẻ hoặc nộp bớt tiền, thu ngân lập phiếu Nạp tiền (Till In) hoặc Rút tiền (Till Out).\n3. Cuối ca: Thu ngân kiểm đếm toàn bộ tiền mặt thực tế trong két, nhập số liệu (ActualCash).\n4. Bấm 'Đóng ca': Hệ thống tính toán Tiền lý thuyết (ExpectedCash) và số tiền chênh lệch (DifferenceCash).\n5. Lệnh in báo cáo kết ca K80 (Z-Report) được gửi tới máy in bill; ca làm việc chuyển sang Closed."],
        ["Luồng ngoại lệ", "4a. Tiền chênh lệch vượt quá hạn mức cho phép: Hệ thống yêu cầu Quản lý nhập mã PIN xác nhận giải trình trước khi cho phép đóng ca."],
        ["Điều kiện sau", "Bản ghi Shift được khóa vĩnh viễn; Báo cáo Z-Report được lưu trữ lịch sử để đối soát."],
        ["Quy tắc nghiệp vụ", "Không thể tạo đơn hàng mới nếu chưa mở ca; Không thể đóng ca khi còn bàn chưa thanh toán."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc5_data)

    # UC-06
    add_table_title(doc, "2.6", "Đặc tả Use Case UC-06: Cấu hình định mức BOM và trừ tồn tự động")
    uc6_data = [
        ["Mã Use Case", "UC-06 (Phân hệ BOH Admin & Kho)"],
        ["Tên Use Case", "Thiết lập công thức định mức BOM và tự động trừ kho"],
        ["Tác nhân (Actor)", "Quản lý / Thủ kho (Manager / Admin)"],
        ["Điều kiện tiên quyết", "Danh mục nguyên vật liệu và đơn vị tính đã được khởi tạo trong CSDL."],
        ["Luồng sự kiện chính", "1. Quản trị viên truy cập trang Quản lý Định lượng BOM trên giao diện BOH Admin.\n2. Chọn món ăn hoặc nhóm tùy chọn (ModifierOption) cần lập công thức.\n3. Thêm các nguyên liệu cấu thành (Material), số lượng tiêu hao và đơn vị tính.\n4. Bấm 'Lưu công thức': Hệ thống kiểm tra tính hợp lệ và lưu vào bảng Recipes, RecipeItems.\n5. Khi đầu bếp hoàn thành món ăn trên KDS, Service tự động nhân tỷ lệ và sinh lệnh trừ kho trong InventoryTransactions."],
        ["Luồng ngoại lệ", "3a. Đơn vị tính công thức khác đơn vị tính cơ sở của kho: Hệ thống tự động nhân hệ số quy đổi (ConversionFactor) từ bảng UnitConversions."],
        ["Điều kiện sau", "Công thức BOM có hiệu lực ngay lập tức cho các lượt chế biến tiếp theo."],
        ["Quy tắc nghiệp vụ", "Công thức BOM phiên bản hóa; Hủy món đang nấu (Void Item) phải hạch toán hao hụt nguyên liệu."]
    ]
    style_table(doc, [Inches(1.8), Inches(4.7)], ["Thuộc tính Đặc tả", "Nội dung Chi tiết"], uc6_data)

    # ==========================================
    # 2.3.4. MÔ HÌNH HÓA SƠ ĐỒ TUẦN TỰ CHO CÁC CA SỬ DỤNG CỐT LÕI
    # ==========================================
    add_h3(doc, "2.3.4. Mô hình hóa Sơ đồ Tuần tự cho các Ca sử dụng Cốt lõi (Sequence Diagrams)")
    add_p(doc, "Để mô hình hóa trực quan và chuẩn xác hành vi động (Dynamic Behavioral Modeling) của hệ thống tương ứng với 6 ca sử dụng cốt lõi đã được đặc tả chi tiết tại mục 2.3.3, phần này xây dựng các sơ đồ tuần tự (Sequence Diagrams) chi tiết. Các sơ đồ mô tả tường minh chuỗi thông điệp trao đổi giữa các tác nhân (Actors), các thành phần giao diện người dùng (Presentation Layer), các dịch vụ điều phối nghiệp vụ (Application Layer), các thực thể miền (Domain Layer), hệ thống cơ sở dữ liệu bền vững (Infrastructure Layer) và các hệ thống ngoại vi (Cổng thanh toán SePay, Mạng lưới SignalR Realtime Hub). Chu trình tương tác được chuẩn hóa theo các nguyên tắc hướng đối tượng, tách biệt trách nhiệm và bảo đảm tính toàn vẹn dữ liệu trong môi trường giao dịch đồng thời.")

    # UC-01
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4a_seq_uc01.png", "2.3.4a", "Sơ đồ tuần tự ca sử dụng UC-01: Đặt món tại bàn thông qua quét mã QR di động", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4a mô tả chi tiết luồng xử lý của ca sử dụng UC-01 từ khi khách hàng quét mã QR tại bàn ăn đến khi đơn hàng được tiếp nhận vào hệ thống:")
    add_p(doc, "• Bước 1 - 3 (Khởi tạo yêu cầu): Khách hàng sử dụng trình duyệt di động quét mã QR gắn tại bàn, hệ thống nạp thực đơn điện tử và bảng tùy chọn modifier tương ứng. Sau khi chọn món và điều chỉnh số lượng/topping, khách hàng bấm 'Gửi đơn gọi món'. Trình duyệt gửi yêu cầu HTTP POST tới QrOrderController kèm JWT Table Session Token và DTO dữ liệu đơn hàng. Controller ủy quyền xử lý cho IOrderService thông qua phương thức CreateQrOrderAsync.")
    add_p(doc, "• Bước 4 - 7 (Xử lý nghiệp vụ & Lưu trữ ACID): Tầng Application kiểm tra tính hợp lệ của phiên bàn ăn, đối chiếu thực đơn hiện hành và nạp công thức giá. Service khởi tạo thực thể Order với trạng thái ban đầu là PendingApproval (Chờ thu ngân duyệt), đồng thời khởi tạo các dòng OrderItem và OrderItemModifier. Dữ liệu được đưa vào ApplicationDbContext để thực thi lưu trữ bền vững xuống cơ sở dữ liệu Microsoft SQL Server theo cơ chế giao dịch ACID. Cơ sở dữ liệu cam kết thành công và phản hồi định danh OrderId đã cấp phát.")
    add_p(doc, "• Bước 8 - 12 (Thông báo Realtime & Phản hồi Client): IOrderService kích hoạt sự kiện thời gian thực thông qua RestaurantHub (SignalR), phát thông điệp ReceiveNewOrderNotification tới nhóm kết nối màn hình thu ngân POS. Máy POS lập tức phát tín hiệu chuông cảnh báo và chuyển đổi trạng thái bàn ăn sang màu vàng nhấp nháy (NewOrder). Đồng thời, kết quả xử lý được đóng gói thành Response DTO trả về cho WebAPI Controller, phản hồi mã HTTP 201 Created về thiết bị di động của khách hàng với thông báo gửi đơn thành công và hướng dẫn chờ nhân viên quầy phê duyệt.")

    # UC-02
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4b_seq_uc02.png", "2.3.4b", "Sơ đồ tuần tự ca sử dụng UC-02: Tiếp nhận và Phê duyệt đơn gọi món tại quầy POS", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4b trực quan hóa quy trình thẩm định và phê duyệt đơn gọi món QR của nhân viên thu ngân tại quầy POS:")
    add_p(doc, "• Bước 1 - 3 (Tiếp nhận & Truy vấn chi tiết): Khi màn hình POS hiển thị bàn ăn có đơn đặt món mới (màu vàng nhấp nháy), nhân viên thu ngân nhấn chọn bàn tương ứng. Giao diện Blazor POS UI gọi IPosOrderService.GetOrderDetailsAsync để nạp toàn bộ thông tin chi tiết của đơn hàng, danh sách món ăn, ghi chú đặc biệt của khách và tổng giá trị tạm tính.")
    add_p(doc, "• Bước 4 - 8 (Phê duyệt & Khóa lạc quan RowVersion): Thu ngân kiểm tra tính khả dụng của bàn và các yêu cầu đi kèm, sau đó nhấn nút 'Duyệt đơn' (Approve Order). Lệnh ApproveOrderAsync được gửi xuống tầng Application kèm định danh nhân viên thực hiện (cashierId). Service nạp Aggregate Root Order, kích hoạt phương thức nghiệp vụ nội tại SetStatus(Confirmed) và gán nhân sự phụ trách. Tầng Infrastructure thực thi lệnh SaveChangesAsync với cơ chế kiểm soát tương tranh lạc quan thông qua trường RowVersion, bảo đảm ngăn chặn xung đột nếu có thao tác can thiệp đồng thời.")
    add_p(doc, "• Bước 9 - 12 (Điều phối Bếp KDS & In phiếu K80): Sau khi giao dịch CSDL cam kết thành công, IPosOrderService phát sự kiện thời gian thực KitchenTicketReceived qua SignalR Hub tới đúng các trạm chế biến liên quan (ví dụ: món nấu gửi tới Trạm Bếp Nóng, đồ uống gửi tới Trạm Pha Chế). Màn hình KDS tại bếp nhận tín hiệu tức thì, phát âm thanh báo nhận đơn và thêm vé vào hàng đợi. Đồng thời, máy POS chuyển màu bàn ăn sang xanh đậm (Occupied), cập nhật giao diện thành công và kích hoạt in phiếu chế biến K80 lưu hành nội bộ.")

    # UC-03
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4c_seq_uc03.png", "2.3.4c", "Sơ đồ tuần tự ca sử dụng UC-03: Điều phối chế biến món ăn tại KDS và Tự động trừ kho BOM", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4c mô tả vòng đời chế biến món ăn tại nhà bếp KDS gắn liền với cơ chế tự động trừ tồn kho nguyên vật liệu theo định mức BOM:")
    add_p(doc, "• Bước 1 - 4 (Điều phối trạng thái nấu nướng): Đầu bếp theo dõi hàng đợi vé chế biến trên màn hình KDS, nhấn nút 'Bắt đầu nấu' để chuyển trạng thái dòng món sang Processing (Đang làm). Khi hoàn thành việc nấu nướng, đầu bếp nhấn nút 'Hoàn tất món' (Done). Giao diện KDS kích hoạt phương thức CompleteOrderItemAsync tại IKitchenService.")
    add_p(doc, "• Bước 5 - 8 (Kích hoạt trừ kho định mức BOM tự động): IKitchenService cập nhật trạng thái dòng món OrderItem thành Done, đồng thời kích hoạt dịch vụ định mức IBomService.DeductInventoryForOrderItemAsync. Dịch vụ này truy vấn công thức định lượng tiêu hao (Recipe, RecipeItems) gắn liền với món ăn và các modifier tùy chọn, nhân với số lượng phần ăn và tính ra lượng tiêu hao chính xác cho từng nguyên vật liệu (Material). Một giao dịch cơ sở dữ liệu mở ra: trừ số lượng tồn kho khả dụng Material.CurrentStock, đồng thời ghi một bản ghi lịch sử vào bảng InventoryTransactions với loại giao dịch BOM_Usage. Toàn bộ chu trình trừ kho được thực thi nghiêm ngặt trong Transaction ACID.")
    add_p(doc, "• Bước 9 - 12 (Báo món hoàn tất & Cập nhật hàng đợi): Giao dịch trừ kho hoàn tất, IKitchenService gửi thông báo thời gian thực ItemReadyNotification qua SignalR Hub tới màn hình thu ngân POS và ứng dụng QR của khách hàng để nhân viên chạy bàn tiến hành bưng món. Vé chế biến trên màn hình KDS được đóng lại và tự động chuyển sang danh sách lịch sử món đã hoàn thành.")

    # UC-04
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4d_seq_uc04.png", "2.3.4d", "Sơ đồ tuần tự ca sử dụng UC-04: Thanh toán hóa đơn và Đối soát tự động VietQR qua SePay", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4d minh họa luồng thanh toán không dùng tiền mặt hiện đại thông qua giải pháp đối soát tự động VietQR kết hợp Webhook cổng SePay:")
    add_p(doc, "• Bước 1 - 4 (Sinh mã VietQR động theo hóa đơn): Tại thời điểm khách hàng yêu cầu thanh toán, thu ngân nhấn nút 'Thanh toán' trên máy POS và chọn hình thức 'Chuyển khoản VietQR'. POS UI gọi IPosPaymentService.GenerateVietQrCodeAsync để tạo mã VietQR động chứa chính xác số tài khoản thụ hưởng, số tiền cần trả sau chiết khấu và cú pháp nội dung chuyển khoản chuẩn hóa DH{OrderId}. Mã QR lập tức được hiển thị trực quan trên màn hình phụ hướng về phía khách hàng.")
    add_p(doc, "• Bước 5 - 7 (Giao dịch ngân hàng & Bắt Webhook SePay): Khách hàng mở ứng dụng ngân hàng (Mobile Banking) quét mã VietQR và hoàn tất lệnh chuyển khoản 24/7. Khi tài khoản ngân hàng nhà hàng nhận tiền, cổng thanh toán SePay bắt biến động số dư và bắn một yêu cầu HTTP POST Webhook tới endpoint SepayWebhookController. Controller xác thực tính hợp lệ của Webhook thông qua khóa bí mật API_KEY trong tiêu đề Authorization và bàn giao dữ liệu cho IPosPaymentService.")
    add_p(doc, "• Bước 8 - 12 (Xử lý Idempotent & Tự động in hóa đơn): Dịch vụ tiến hành kiểm tra tính duy nhất của giao dịch thông qua IdempotencyKey trong bảng SepayWebhookLogs nhằm loại trừ triệt để rủi ro xử lý lặp lại Webhook. Sau khi đối soát khớp chính xác số tiền và mã hóa đơn, hệ thống cập nhật Payment.Status = Completed, chuyển đơn hàng sang Paid, cập nhật bàn ăn về Available và cộng điểm tích lũy thành viên. Giao dịch được cam kết ACID, sau đó hệ thống bắn sự kiện SignalR thông báo thành công về máy POS. POS tự động điều khiển máy in nhiệt K80 xuất hóa đơn bán lẻ và kích hoạt rơ-le bật ngăn kéo đựng tiền mặt.")

    # UC-05
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4e_seq_uc05.png", "2.3.4e", "Sơ đồ tuần tự ca sử dụng UC-05: Quản lý ca làm việc và Chốt két thu ngân (Z-Report)", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4e chuẩn hóa chu trình kiểm soát tài chính ca làm việc của thu ngân, phòng chống thất thoát tiền mặt:")
    add_p(doc, "• Bước 1 - 3 (Mở ca làm việc đầu ngày): Khi bắt đầu phiên làm việc, thu ngân đăng nhập vào hệ thống POS, nhập số tiền mặt bàn giao đầu ca (OpeningBalance) và nhấn 'Mở ca'. IShiftService kiểm tra điều kiện tiên quyết (không có ca nào khác đang mở trên máy POS này), khởi tạo một bản ghi Shift mới với trạng thái Open, ghi nhận thời gian bắt đầu và lưu vào cơ sở dữ liệu.")
    add_p(doc, "• Bước 4 - 6 (Biến động dòng tiền két trong ca): Trong quá trình bán hàng, khi phát sinh nhu cầu đổi tiền lẻ hoặc nộp bớt tiền về quỹ an toàn, thu ngân lập phiếu Nạp tiền két (Till In) hoặc Rút tiền két (Till Out). IShiftService ghi nhận giao dịch vào thực thể CashMovement gắn chặt với ShiftId hiện hành và cập nhật số dư tiền mặt lý thuyết trong két.")
    add_p(doc, "• Bước 7 - 12 (Kiểm két, chốt ca & In báo cáo Z-Report): Khi hết ca làm việc, thu ngân tiến hành kiểm đếm toàn bộ tiền mặt thực tế đang có trong két, nhập con số kiểm đếm (ActualCash) và bấm 'Đóng ca'. IShiftService nạp bản ghi Shift, tính toán số tiền mặt lý thuyết theo công thức chặt chẽ: ExpectedCash = OpeningBalance + CashSales + TotalTillIn - TotalTillOut. Tiếp đó, hệ thống xác định chênh lệch tài chính: Difference = ActualCash - ExpectedCash. Bản ghi Shift được cập nhật trạng thái Closed và khóa vĩnh viễn không thể chỉnh sửa (SaveChangesAsync). Đồng thời, lệnh in báo cáo quyết toán kết ca chuẩn hóa (Z-Report) được gửi trực tiếp tới máy in K80 để thu ngân và quản lý ký biên bản bàn giao.")

    # UC-06
    add_figure(doc, "báo cáo/figures/classic_uml/fig_2_3_4f_seq_uc06.png", "2.3.4f", "Sơ đồ tuần tự ca sử dụng UC-06: Cấu hình định mức BOM và Tự động trừ kho", width_inch=5.8)
    add_p(doc, "Sơ đồ tuần tự trong Hình 2.3.4f mô tả luồng thiết lập công thức định mức nguyên vật liệu (BOM) tại phân hệ quản trị BOH Admin và cơ chế kích hoạt trừ tồn kho tự động trong quá trình vận hành thực tế:")
    add_p(doc, "• Bước 1 - 4 (Thiết lập công thức BOM định lượng): Quản lý nhà hàng hoặc thủ kho truy cập giao diện BOH Admin, chọn món ăn hoặc nhóm tùy chọn modifier cần lập định lượng. Người dùng thiết lập danh sách nguyên vật liệu cấu thành (Material), định lượng tiêu hao trên một đơn vị thành phẩm và đơn vị tính tương ứng. Khi nhấn 'Lưu công thức BOM', giao diện gửi DTO dữ liệu tới IInventoryBomService.SaveRecipeAsync. Dịch vụ kiểm tra tính tương thích và nạp hệ số quy đổi đơn vị từ bảng UnitConversions.")
    add_p(doc, "• Bước 5 - 8 (Lưu trữ và Kiểm tra toàn vẹn dữ liệu): IInventoryBomService nạp hoặc khởi tạo mới Aggregate Root Recipe và các thực thể con RecipeItem. Sau khi xác thực quy tắc nghiệp vụ (định lượng tiêu hao phải lớn hơn 0, không có nguyên liệu trùng lặp trong cùng công thức), dữ liệu được lưu bền vững vào CSDL thông qua ApplicationDbContext. Hệ thống phản hồi xác nhận lưu thành công và công thức BOM có hiệu lực thi hành ngay lập tức.")
    add_p(doc, "• Bước 9 - 12 (Trừ kho tự động theo định mức thời gian thực): Trong quá trình vận hành, mỗi khi đầu bếp hoàn thành chế biến món ăn trên màn hình KDS, IKitchenService sẽ kích hoạt lời gọi nội bộ tới IInventoryBomService.DeductInventoryForOrderItemAsync. Dịch vụ tự động áp dụng công thức BOM đã lưu, nhân theo số lượng món thực tế và thực thi trừ số lượng tồn kho CurrentStock của từng nguyên liệu trong một giao dịch ACID khép kín, bảo đảm số liệu tồn kho luôn phản ánh chính xác 100% tình trạng vật lý tại kho bếp.")

    # ==========================================
    # 2.4. THIẾT KẾ HƯỚNG ĐỐI TƯỢNG VÀ BIỂU ĐỒ LỚP (CLASS DIAGRAMS)
    # ==========================================
    # ==========================================
    add_h2(doc, "2.4. Thiết kế Hướng đối tượng và Biểu đồ Lớp (Class Diagrams)")
    add_p(doc, "Thiết kế hướng đối tượng trong hệ thống tuân thủ chặt chẽ các nguyên lý của Thiết kế Hướng miền (Domain-Driven Design - DDD). Các thực thể Domain Entity đóng vai trò là gốc tổng hợp (Aggregate Roots) hoặc thực thể con, đóng gói toàn bộ quy tắc nghiệp vụ và trạng thái dữ liệu. Dưới đây là 4 biểu đồ lớp chi tiết đại diện cho 4 cụm Aggregate trọng yếu của hệ thống:")

    add_h3(doc, "2.4.1. Biểu đồ Lớp Phân hệ Đơn hàng và Thanh toán (Orders & Payments Aggregate)")
    add_p(doc, "Cụm Aggregate Đơn hàng và Thanh toán là trung tâm dữ liệu giao dịch của toàn bộ nhà hàng, quản lý vòng đời từ khi khách gọi món cho đến khi xuất hóa đơn:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_class_orders_payments.png", "2.3a", "Biểu đồ lớp phân hệ Đơn hàng, Dòng món và Giao dịch Thanh toán", width_inch=5.6)

    add_p(doc, "Phân tích cấu trúc lớp trong Hình 2.3a:")
    add_p(doc, "• Lớp `Order` (Aggregate Root): Đóng gói toàn bộ thông tin đơn hàng, sở hữu thuộc tính tương tranh `RowVersion` (byte[]) để thực hiện Optimistic Concurrency Control. Cung cấp các phương thức nghiệp vụ: `RecalculateTotals()` (tính lại tổng tiền sau thuế, giảm giá), `ApplyDiscount()`, `ChangeStatus()`.")
    add_p(doc, "• Lớp `OrderItem`: Đại diện cho từng dòng món, liên kết 1-N với `OrderItemModifier` để lưu vết các tùy chọn đi kèm. Chứa trường `UnitPrice` và `ExtraPrice` dạng snapshot tại thời điểm bán.")
    add_p(doc, "• Lớp `Payment`: Quản lý việc thanh toán đơn hàng theo nhiều hình thức (Cash, VietQR, CreditCard). Chứa trường `TransactionReference` liên kết đối soát 1-1 với bản ghi `SepayWebhookLog`.")
    add_p(doc, "• Lớp `VoidLog`: Lưu vết kiểm toán khi có thao tác hủy món, ghi nhận mã nhân viên (`UserId`), người phê duyệt (`ApprovedBy`), lý do hủy và chi phí hao hụt nguyên liệu.")

    add_h3(doc, "2.4.2. Biểu đồ Lớp Phân hệ Thực đơn và Nhóm Tùy chọn (Menu & Modifiers Aggregate)")
    add_p(doc, "Cấu trúc dữ liệu thực đơn được mô hình hóa linh hoạt nhằm đáp ứng đa dạng yêu cầu của ngành F&B:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_class_menu_modifiers.png", "2.3b", "Biểu đồ lớp phân hệ Danh mục, Món ăn và Nhóm Tùy chọn Modifier", width_inch=5.2)

    add_p(doc, "Phân tích cấu trúc lớp trong Hình 2.3b:")
    add_p(doc, "• Lớp `MenuItem`: Thuộc về một `Category`, chứa cờ `IsSoldOut` (báo hết món tức thời) và `IsCombo`. Cung cấp phương thức `SetSoldOut(bool)` để cập nhật trạng thái khả dụng.")
    add_p(doc, "• Lớp `ModifierGroup`: Đại diện cho một nhóm tùy chọn (ví dụ: 'Mức đường', 'Mức đá', 'Topping chọn thêm'). Thiết lập hai ràng buộc quan trọng: `MinSelections` (số lượng chọn tối thiểu) và `MaxSelections` (số lượng chọn tối đa).")
    add_p(doc, "• Lớp `ModifierOption`: Từng lựa chọn cụ thể trong nhóm, chứa trường `ExtraPrice` quy định mức phụ thu và liên kết tới công thức tiêu hao nguyên liệu tương ứng.")

    add_h3(doc, "2.4.3. Biểu đồ Lớp Phân hệ Kho hàng và Định mức BOM (Inventory & BOM Aggregate)")
    add_p(doc, "Mô hình quản lý kho tự động kết nối chặt chẽ giữa tiến độ nấu của nhà bếp và số lượng tồn kho nguyên vật liệu:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_class_inventory_bom.png", "2.3c", "Biểu đồ lớp phân hệ Nguyên vật liệu, Công thức Định lượng BOM và Giao dịch Kho", width_inch=5.6)

    add_p(doc, "Phân tích cấu trúc lớp trong Hình 2.3c:")
    add_p(doc, "• Lớp `Material`: Đại diện cho nguyên vật liệu thô (thịt, sữa, cà phê hạt...). Thuộc tính `CurrentStock` lưu trữ tồn kho hiện tại, `MinStockLevel` là ngưỡng cảnh báo bổ sung hàng. Phương thức `DeductStock()` và `AddStock()` điều chỉnh tồn kho có kiểm soát.")
    add_p(doc, "• Lớp `Recipe` và `RecipeItem`: Định nghĩa định mức tiêu hao nguyên vật liệu cho từng món ăn hoặc tùy chọn modifier. Phương thức `CalculateConsumption(decimal quantity)` tự động tính toán tổng khối lượng nguyên liệu cần khấu trừ.")
    add_p(doc, "• Lớp `InventoryTransaction`: Sổ cái ghi nhận mọi biến động kho (Purchase, BOMDeduction, Waste, StockAdjustment) phục vụ kiểm toán và báo cáo thất thoát.")

    add_h3(doc, "2.4.4. Biểu đồ Lớp Phân hệ Ca làm việc và Quản lý Tiền két (Shifts & Cash Management)")
    add_p(doc, "Đảm bảo tính minh bạch tài chính của nhân viên thu ngân trong suốt phiên làm việc:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_class_shifts_cash.png", "2.3d", "Biểu đồ lớp phân hệ Ca làm việc, Dòng tiền két và Báo cáo Z-Report", width_inch=5.2)

    add_p(doc, "Phân tích cấu trúc lớp trong Hình 2.3d:")
    add_p(doc, "• Lớp `Shift`: Đại diện cho một ca làm việc thu ngân. Chứa các trường tài chính: `OpeningBalance` (tiền đầu ca), `TotalCashSales` (tổng thu tiền mặt trong ca), `ExpectedCash` (tiền mặt lý thuyết phải có), `ActualCash` (tiền mặt đếm thực tế) và `Difference` (chênh lệch thừa/thiếu). Phương thức `CloseShift()` thực hiện tính toán đối soát và kết ca.")
    add_p(doc, "• Lớp `CashMovement`: Bản ghi từng lần nạp tiền lẻ (Till In) hoặc rút tiền nộp quỹ (Till Out) diễn ra trong ca.")
    add_p(doc, "• Lớp `User` và `Role`: Quản lý định danh nhân sự, phân quyền RBAC và lưu trữ mã PIN Quản lý băm BCrypt.")

    # ==========================================
    # 2.5. THIẾT KẾ KIẾN TRÚC TỔNG THỂ HỆ THỐNG
    # ==========================================
    add_h2(doc, "2.5. Thiết kế kiến trúc tổng thể hệ thống (Clean Architecture)")
    
    add_h3(doc, "2.5.1. Sơ đồ Kiến trúc Phân tầng Clean Architecture")
    add_p(doc, "Kiến trúc hệ thống được thiết kế tuân thủ nghiêm ngặt mô hình Clean Architecture (Onion Architecture) của Robert C. Martin [11] kết hợp các mẫu thiết kế phần mềm doanh nghiệp của Martin Fowler [13], phân định ranh giới phụ thuộc rõ ràng giữa các tầng nhằm cô lập logic nghiệp vụ cốt lõi khỏi các công nghệ framework bên ngoài:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_arch_layers.png", "2.4", "Sơ đồ kiến trúc Clean Architecture phân tầng phụ thuộc (Trích từ docs/ARCHITECTURE.md)", width_inch=4.5)

    add_p(doc, "Theo sơ đồ Hình 2.4, nguyên tắc phụ thuộc một chiều hướng tâm được bảo toàn chặt chẽ:")
    add_p(doc, "• Tầng Presentation (`Restaurant.Web & WebAPI`): Đảm nhiệm hiển thị giao diện Blazor Server tương tác thời gian thực, cung cấp 24 REST Controllers cho Mobile QR và điều phối các kênh giao tiếp WebSocket qua `RestaurantHub`.")
    add_p(doc, "• Tầng Infrastructure (`Restaurant.Infrastructure`): Hiện thực hóa các interface trừu tượng được khai báo ở tầng Application, phụ trách truy xuất cơ sở dữ liệu qua `ApplicationDbContext` (42 DbSets), cấu hình Fluent API, tích hợp cổng thanh toán SePay và mã hóa bảo mật BCrypt.")
    add_p(doc, "• Tầng Application (`Restaurant.Application`): Trung tâm điều phối các ca sử dụng (Use Cases), chứa định nghĩa DTOs, Business Services (`IPosService`, `IKitchenService`, `IInventoryService`), cơ chế xác thực quyền hạn và các quy tắc kiểm tra tính hợp lệ dữ liệu.")
    add_p(doc, "• Tầng Core Domain (`Restaurant.Domain`): Chứa các thực thể cốt lõi (`Order`, `OrderItem`, `Payment`, `Material`, `Shift`), các kiểu liệt kê trạng thái (Enums), Value Objects và Domain Events. Tầng này độc lập tuyệt đối, không phụ thuộc vào bất kỳ thư viện hay framework bên ngoài nào.")

    add_h3(doc, "2.5.2. Sơ đồ Tuần tự Xử lý Yêu cầu (Request-Response Flow)")
    add_p(doc, "Để làm sáng tỏ luồng xử lý xuyên suốt từ khi nhận yêu cầu đến khi lưu trữ dữ liệu bền vững, sơ đồ tuần tự dưới đây mô tả chu trình tương tác hoàn chỉnh giữa các thành phần:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_arch_sequence.png", "2.5", "Sơ đồ tuần tự xử lý yêu cầu hệ thống từ Client đến Database (Trích từ docs/ARCHITECTURE.md)", width_inch=6.0)

    add_p(doc, "Mô tả chi tiết 7 bước trong chu trình thực thi (Hình 2.5):")
    add_p(doc, "1. Gửi Yêu cầu (Client Request): Client (Blazor Server hoặc Mobile QR) gửi yêu cầu HTTP Request kèm JWT Token trong tiêu đề (Authorization Header) tới WebAPI Controllers.")
    add_p(doc, "2. Xác thực và Phân quyền (Auth Filter): Controller thông qua Middleware tiến hành xác thực danh tính (Authentication) và đối chiếu vai trò người dùng (Role-based Authorization Policy) dựa trên các Claims trong token.")
    add_p(doc, "3. Chuyển giao DTO (Application Dispatch): Sau khi xác thực hợp lệ, Controller chuyển tiếp DTO dữ liệu tới Service nghiệp vụ chuyên trách trong tầng Application.")
    add_p(doc, "4. Kiểm tra Quy tắc Nghiệp vụ (Domain Invariants): Service nạp thực thể Domain Entity tương ứng từ cơ sở dữ liệu, thực hiện kiểm tra tính hợp lệ của trạng thái (ví dụ: bàn ăn có đang mở ca không, món ăn có bị khóa hết hàng không, hạn mức chiết khấu có vượt trần không).")
    add_p(doc, "5. Theo dõi Biến động (EF Core Change Tracking): Service gọi `ApplicationDbContext` để ghi nhận các thay đổi vào bộ nhớ theo dõi (Change Tracker) của Entity Framework Core.")
    add_p(doc, "6. Giao dịch Cơ sở Dữ liệu (Atomic Transaction): Tầng Infrastructure mở Database Transaction cấp độ Read Committed, chuyển đổi các thay đổi thành các câu lệnh SQL tối ưu và thực thi bền vững trên Microsoft SQL Server Database.")
    add_p(doc, "7. Đóng gói Phản hồi (Response Mapping): Kết quả xử lý từ Domain Entity được ánh xạ ngược lại thành Response DTO và trả về cho Client cùng mã trạng thái HTTP tiêu chuẩn (200 OK, 400 Bad Request, 403 Forbidden).")
    add_p(doc, "Đặc biệt, hệ thống áp dụng cơ chế Bộ lọc Xử lý Lỗi Toàn cục (Global Exception Handling Middleware). Mọi ngoại lệ phát sinh ngoài ý muốn đều được tự động bắt giữ, chuẩn hóa thành thông điệp lỗi ProblemDetails theo chuẩn RFC 7807 [17] và ghi log kiểm toán chi tiết vào bảng `AuditLogs` trước khi phản hồi về cho người dùng.")

    # ==========================================
    # 2.6. THIẾT KẾ CÁC SƠ ĐỒ TRẠNG THÁI VÀ LUỒNG NGHIỆP VỤ
    # ==========================================
    add_h2(doc, "2.6. Thiết kế các Sơ đồ Trạng thái và Luồng Nghiệp vụ Realtime")
    
    add_h3(doc, "2.6.1. Sơ đồ Tuần tự Luồng Đặt món và Đồng bộ Realtime")
    add_p(doc, "Giao tiếp thời gian thực là xương sống bảo đảm tính đồng bộ tức thời giữa Khách hàng quét QR tại bàn, Nhân viên thu ngân tại quầy POS và Đầu bếp tại trạm KDS:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_seq_order_kds.png", "2.6", "Sơ đồ tuần tự luồng đặt món QR, duyệt POS và chế biến KDS qua SignalR", width_inch=6.2)

    add_p(doc, "Chu trình tương tác SignalR (Hình 2.6) vận hành qua các giai đoạn then chốt:")
    add_p(doc, "• Khách hàng hoàn tất chọn món trên điện thoại, bấm 'Gửi đơn'. Yêu cầu được gửi lên máy chủ tạo bản ghi đơn hàng ở trạng thái `Draft`.")
    add_p(doc, "• Máy chủ kích hoạt phương thức `ReceiveNewOrderNotification` trên SignalR Hub, phát tín hiệu âm thanh và nhấp nháy bàn màu vàng (`NewOrder`) trên giao diện POS.")
    add_p(doc, "• Thu ngân kiểm tra danh sách món, nhấn nút 'Duyệt đơn'. Trạng thái đơn chuyển sang `Confirmed`, hệ thống đồng thời phát sự kiện `KitchenTicketReceived` tới đúng trạm bếp liên quan.")
    add_p(doc, "• Khi đầu bếp hoàn thành chế biến, sự kiện `OrderItemStatusChanged` được phát tới cả POS và điện thoại khách hàng, bảo đảm tiến độ luôn minh bạch.")

    add_h3(doc, "2.6.2. Sơ đồ Chuyển đổi Trạng thái Bàn ăn (Table State Machine)")
    add_p(doc, "Sơ đồ bàn là giao diện trung tâm điều hành toàn bộ hoạt động của sảnh nhà hàng. Trạng thái mỗi bàn ăn được quản lý qua một máy trạng thái hữu hạn chặt chẽ với màu sắc nhận diện trực quan:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_table.png", "2.7", "Sơ đồ chuyển đổi trạng thái Bàn ăn Table State Machine", width_inch=5.5)

    add_p(doc, "Quy tắc chuyển dịch trạng thái bàn ăn (Hình 2.7):")
    add_p(doc, "• `Available` (Bàn trống - Màu Xám): Bàn sẵn sàng đón khách. Khi thu ngân bấm mở bàn hoặc khách quét mã QR, bàn chuyển sang `Occupied`.")
    add_p(doc, "• `Occupied` (Đang có khách - Màu Đỏ): Bàn đang phục vụ bữa ăn. Khi có yêu cầu gọi món mới gửi lên từ QR, bàn chuyển sang `NewOrder`.")
    add_p(doc, "• `NewOrder` (Có đơn chờ duyệt - Màu Vàng nhấp nháy): Cảnh báo thu ngân cần xem xét phê duyệt thực đơn. Sau khi duyệt xong, bàn quay lại `Occupied`.")
    add_p(doc, "• `Billing` (Chờ thanh toán - Màu Xanh lá): Kích hoạt khi thu ngân in phiếu tạm tính K80. Bàn bị khóa không cho đặt thêm món mới.")
    add_p(doc, "• Sau khi giao dịch thanh toán thành công, hệ thống giải phóng bàn ăn về trạng thái `Available` ban đầu.")

    add_h3(doc, "2.6.3. Sơ đồ Chuyển đổi Trạng thái Đơn hàng và Món ăn")
    add_p(doc, "Mối quan hệ hữu cơ giữa Đơn hàng tổng thể (`Order`) và từng dòng món ăn chi tiết (`OrderItem`) được quản lý song song thông qua hai sơ đồ máy trạng thái dạng ngang (Horizontal Pipeline) thoáng đãng, phân tách hoàn toàn các luồng hủy đơn và hủy món:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_order.png", "2.8", "Sơ đồ chuyển đổi trạng thái Đơn hàng Order State Machine dạng ngang thoáng", width_inch=6.0)

    add_p(doc, "Quy tắc vận hành vòng đời Đơn hàng (Hình 2.8):")
    add_p(doc, "• Vòng đời khởi đầu từ trạng thái `Draft` (khi khách gọi món qua QR Ordering) hoặc chuyển thẳng sang `Confirmed` (khi thu ngân trực tiếp tạo đơn tại quầy POS).")
    add_p(doc, "• Sau khi đơn được phê duyệt (`Confirmed`), hệ thống phân phối danh sách món xuống trạm bếp KDS. Khi toàn bộ các món trong đơn đã được đầu bếp chế biến xong, đơn tự động đạt trạng thái `Completed`.")
    add_p(doc, "• Khi khách hoàn tất thanh toán (Tiền mặt hoặc VietQR), đơn chuyển sang trạng thái cuối cùng `Paid` và lưu trữ lịch sử hóa đơn đóng. Trường hợp khách rời bàn trước khi nấu hoặc thu ngân từ chối duyệt, đơn chuyển sang `Cancelled` kèm ghi nhận kiểm toán.")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_order_item.png", "2.9", "Sơ đồ chuyển đổi trạng thái Món ăn trong Bếp OrderItem State Machine dạng ngang thoáng", width_inch=6.0)

    add_p(doc, "Quy tắc vận hành dòng món ăn tại bếp (Hình 2.9):")
    add_p(doc, "• Khi đơn hàng chuyển sang `Confirmed`, toàn bộ các món ăn liên quan lập tức hiển thị trên màn hình trạm bếp KDS ở trạng thái `Pending` (Chờ chế biến).")
    add_p(doc, "• Đầu bếp bấm 'Nhận làm' để chuyển dòng món sang `Processing` (Đang chế biến), phát tín hiệu realtime qua SignalR cập nhật tiến độ tới quầy POS và điện thoại khách hàng. Với các món đơn giản pha chế nhanh, đầu bếp có thể chọn hoàn thành trực tiếp từ hàng đợi.")
    add_p(doc, "• Khi món hoàn tất (`Done`), nhân viên chạy bàn nhận thông báo để bê món phục vụ khách. Trường hợp hủy món (Void Item), nếu món chưa nấu thu ngân có thể hủy trực tiếp; nếu món đang làm dở (`Processing`), bắt buộc phải nhập mã PIN Quản lý phê duyệt và hệ thống tự động ghi nhận nhật ký thất thoát nguyên liệu.")

    add_h3(doc, "2.6.4. Sơ đồ Trạng thái Thanh toán (Payment State Machine)")
    add_p(doc, "Giao dịch thanh toán được quản lý chặt chẽ theo hướng ngang (Horizontal Pipeline) nhằm ngăn chặn thất thoát tài chính và hỗ trợ đối soát tự động:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_payment.png", "2.10", "Sơ đồ máy trạng thái Thanh toán Payment State Machine", width_inch=5.5)

    add_p(doc, "Quy trình thanh toán (Hình 2.10) hỗ trợ hai phương thức:")
    add_p(doc, "• Tiền mặt (Cash): Thu ngân kiểm đếm tiền khách đưa, nhập số tiền, hệ thống tính tiền thối và lập tức chuyển giao dịch sang `Completed`.")
    add_p(doc, "• Chuyển khoản VietQR: Khởi tạo ở trạng thái `Pending`. Hệ thống sinh mã VietQR động chứa số tiền và mã đơn hàng. Khi SePay gửi Webhook xác nhận biến động số dư trùng khớp, giao dịch tự động chuyển sang `Completed` và kích hoạt in hóa đơn.")

    add_h3(doc, "2.6.5. Sơ đồ Trạng thái Ca làm việc và Đợt kiểm kê Kho")
    add_p(doc, "Hai chu trình nghiệp vụ hậu sảnh quan trọng bậc nhất gồm quản lý dòng tiền mặt trong ca và kiểm kê cân bằng kho hàng được chuẩn hóa theo quy trình ngang trực quan:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_shift.png", "2.11", "Sơ đồ máy trạng thái Ca làm việc Shift State Machine", width_inch=5.5)

    add_p(doc, "Quy tắc vận hành Ca làm việc thu ngân (Hình 2.11): Thu ngân mở ca (`Open`), khai báo số tiền mặt ban đầu có trong két (`OpeningBalance`). Trong ca, mọi khoản nạp tiền lẻ (Till In) hoặc rút nộp két (Till Out) đều phải lập phiếu ghi nhận. Kết thúc ca, thu ngân đếm tiền thực tế (`ActualCash`), đối chiếu với tiền lý thuyết (`ExpectedCash`) và in báo cáo kết ca K80 (Z-Report) trước khi đóng ca (`Closed`).")

    add_figure(doc, "báo cáo/figures/mermaid/spec_state_stocktake.png", "2.12", "Sơ đồ máy trạng thái Đợt kiểm kê Kho Stocktake State Machine", width_inch=5.5)

    add_p(doc, "Quy tắc vận hành Đợt kiểm kê kho (Hình 2.12): Bắt đầu kiểm kê (`InProgress`), hệ thống khóa chốt số lượng tồn kho lý thuyết trên sổ sách. Thủ kho tiến hành đếm thực tế tại kho và nhập số liệu. Khi nhấn 'Hoàn tất kiểm kê' (`Completed`), hệ thống tự động sinh giao dịch điều chỉnh cân bằng tồn kho và ghi nhận giá trị hao hụt kiểm kê.")

    # ==========================================
    # 2.7. THIẾT KẾ SƠ ĐỒ THỰC THỂ QUAN HỆ ERD
    # ==========================================
    add_h2(doc, "2.7. Thiết kế Sơ đồ Thực thể Quan hệ ERD (Trích xuất từ CSDL)")
    add_p(doc, "Cơ sở dữ liệu hệ thống được thiết kế chuẩn hóa ở mức Dạng chuẩn 3 (Third Normal Form - 3NF) theo lý thuyết thiết kế cơ sở dữ liệu của C. J. Date [14] gồm 42 thực thể liên kết chặt chẽ. Để đảm bảo tính trực quan và tránh quá tải thị giác, các mô hình thực thể được phân rã theo từng cụm nghiệp vụ chức năng:")

    add_h3(doc, "2.7.1. Sơ đồ ERD Phân hệ Người dùng, Phân quyền và Sơ đồ Sảnh")
    add_p(doc, "Quản trị định danh người dùng và sơ đồ không gian nhà hàng là nền tảng khởi tạo của toàn bộ hệ thống:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_erd_auth.png", "2.13", "Sơ đồ ERD phân hệ Người dùng, Phân quyền và Bàn ăn", width_inch=5.2)

    add_p(doc, "Các thực thể trọng tâm trong Hình 2.13:")
    add_p(doc, "• `Users` và `Roles`: Thiết lập mô hình phân quyền dựa trên vai trò (RBAC). Bảng `Users` lưu trữ mật khẩu băm một chiều BCrypt, mã PIN Quản lý bảo mật và khóa tài khoản tạm thời 15 phút nếu nhập sai quá 5 lần liên tiếp.")
    add_p(doc, "• `Areas` và `Tables`: Quản lý danh mục khu vực sảnh (Tầng 1, Sân vườn, Phòng VIP) và danh sách bàn ăn. Mỗi bàn chứa trường `QrToken` chuỗi ngẫu nhiên 32 ký tự để xác thực phiên gọi món không thể giả mạo.")

    add_h3(doc, "2.7.2. Sơ đồ ERD Phân hệ Thực đơn và Tùy chọn Modifier")
    add_p(doc, "Kiến trúc dữ liệu thực đơn hỗ trợ khả năng tùy biến mạnh mẽ, cho phép một món ăn có nhiều kích cỡ, mức đường/đá và topping đi kèm:")

    add_side_by_side_figures(doc, 
                             "báo cáo/figures/mermaid/spec_erd_menu_items.png", "2.14a", "ERD Danh mục & Món ăn", 2.3,
                             "báo cáo/figures/mermaid/spec_erd_menu_modifiers.png", "2.14b", "ERD Tùy chọn Modifiers & Topping", 3.7)

    add_p(doc, "Quy tắc quan hệ dữ liệu trong Hình 2.14a và Hình 2.14b:")
    add_p(doc, "• `Categories` liên kết 1-N với `MenuItems`. Món ăn chứa cờ `IsSoldOut` (báo hết món realtime từ bếp) và `IsCombo` (món phức hợp).")
    add_p(doc, "• `MenuItemModifierGroups` đóng vai trò bảng cầu nối liên kết nhiều-nhiều (N-N) giữa món ăn và các nhóm tùy chọn (`ModifierGroups`). Mỗi nhóm thiết lập giới hạn `MinSelections` và `MaxSelections`, chứa danh sách `ModifierOptions` kèm đơn giá phụ thu `ExtraPrice` tương ứng.")

    add_h3(doc, "2.7.3. Sơ đồ ERD Phân hệ Đơn hàng, Dòng món và Hủy món")
    add_p(doc, "Mô hình cốt lõi ghi nhận toàn bộ chu trình tiêu dùng của khách hàng và nhật ký điều chỉnh đơn:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_erd_orders_items.png", "2.15a", "Sơ đồ ERD Đơn hàng, Dòng món chi tiết và Nhật ký Hủy món", width_inch=3.8)

    add_p(doc, "Ràng buộc dữ liệu và cơ chế toàn vẹn trong Hình 2.15a:")
    add_p(doc, "• Quan hệ 1-N giữa `Orders` và `OrderItems`: Mỗi đơn hàng chứa danh sách các món ăn đã gọi. Mỗi dòng món lưu trạng thái chế biến độc lập (`KitchenStatus`), cho phép một phần đơn được phục vụ trước trong khi các món phức tạp khác đang tiếp tục nấu.")
    add_p(doc, "• Snapshot giá bán (Price Snapshotting): Đơn giá của món ăn (`UnitPrice`) và tùy chọn (`ExtraPrice`) được sao chép trực tiếp vào thời điểm gọi món. Điều này bảo đảm các báo cáo tài chính lịch sử không bị biến động sai lệch nếu người quản trị thay đổi giá niêm yết trên thực đơn trong tương lai.")
    add_p(doc, "• Kiểm soát tương tranh lạc quan (Optimistic Concurrency Control): Thực thể `Orders` được bổ sung thuộc tính `RowVersion` (kiểu Timestamp trong SQL Server). Khi cập nhật đơn hàng (ví dụ: thêm món, gộp bàn, thanh toán), Entity Framework Core sẽ tự động kiểm tra xem phiên bản dữ liệu có bị thay đổi bởi thao tác khác hay không; nếu phát hiện xung đột, hệ thống ném ra ngoại lệ `DbUpdateConcurrencyException` để rollback và thông báo cho người dùng tải lại dữ liệu mới nhất.")
    add_p(doc, "• Nhật ký hủy món có kiểm toán (`VoidLogs`): Khi thu ngân hủy món, hệ thống không xóa cứng (Hard Delete) dòng bản ghi mà kích hoạt lưu vết vào `VoidLogs`, ghi nhận mã nhân viên thực hiện, mã quản lý phê duyệt, lý do hủy món và chi phí nguyên vật liệu thất thoát.")

    add_h3(doc, "2.7.4. Sơ đồ ERD Phân hệ Thanh toán, SePay Webhook và Quản lý Ca Thu ngân")
    add_p(doc, "Dữ liệu tài chính được cấu trúc thành hai mảng song hành gồm giao dịch thanh toán ngân hàng và quản lý tiền két:")

    add_side_by_side_figures(doc, 
                             "báo cáo/figures/mermaid/spec_erd_payments_sepay.png", "2.15b", "ERD Thanh toán & Webhook SePay", 3.2,
                             "báo cáo/figures/mermaid/spec_erd_shifts_cash.png", "2.15c", "ERD Ca làm việc & Dòng tiền két", 1.9)

    add_p(doc, "Phân tích cấu trúc dữ liệu trong Hình 2.15b và Hình 2.15c:")
    add_p(doc, "• Thanh toán (`Payments`): Hỗ trợ thanh toán tách hóa đơn hoặc nhiều phương thức trên một đơn hàng. Giao dịch chuyển khoản được ánh xạ 1-1 với `SepayWebhookLogs` thông qua mã định danh giao dịch duy nhất để bảo đảm nguyên tắc không xử lý lặp (Idempotent).")
    add_p(doc, "• Quản lý Ca (`Shifts`): Liên kết 1-N với `CashMovements`. Thu ngân nạp tiền lẻ hoặc rút tiền nộp quản lý đều được ghi nhận thời điểm, số tiền và lý do để đối soát sai lệch két cuối ca.")

    add_h3(doc, "2.7.5. Sơ đồ ERD Phân hệ Khách hàng Thân thiết (Loyalty & CRM)")
    add_p(doc, "Cơ chế quản lý hạng thành viên và tích lũy điểm thưởng phục vụ chiến lược giữ chân khách hàng:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_erd_loyalty.png", "2.16", "Sơ đồ ERD phân hệ Khách hàng Thân thiết và Tích điểm", width_inch=3.0)

    add_p(doc, "Quy chế hoạt động trong Hình 2.16:")
    add_p(doc, "• Bảng `Customers` liên kết N-1 với `CustomerTiers` (Đồng, Bạc, Vàng, Kim Cương). Mỗi hạng thẻ quy định tỷ lệ giảm giá tự động trên hóa đơn (`DiscountPercent`).")
    add_p(doc, "• Bảng `LoyaltyTransactions` lưu trữ toàn bộ lịch sử tích điểm (+1 điểm cho mỗi 10.000đ thanh toán) và tiêu điểm (-1.000đ cho mỗi điểm khả dụng). Khi đơn hàng bị hủy, hệ thống kích hoạt giao dịch hoàn điểm (Rollback) nguyên tử.")

    add_h3(doc, "2.7.6. Sơ đồ ERD Phân hệ Kho hàng và Định mức BOM")
    add_p(doc, "Quản lý công thức định lượng tiêu hao nguyên vật liệu (Bill of Materials - BOM) là hạt nhân kiểm soát giá vốn và chống thất thoát kho:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_erd_inventory_bom.png", "2.17", "Sơ đồ ERD phân hệ Kho hàng và Định mức BOM", width_inch=5.8)

    add_p(doc, "Nguyên lý vận hành BOM trong Hình 2.17:")
    add_p(doc, "• Bảng `Materials` lưu danh mục nguyên vật liệu thô (thịt bò, sữa tươi, phô mai...), đơn vị cơ sở (`BaseUnitId`), số lượng tồn thực tế (`CurrentStock`) và ngưỡng cảnh báo tối thiểu (`MinStockLevel`).")
    add_p(doc, "• Bảng `Recipes` và `RecipeItems` bóc tách công thức tiêu hao của từng món ăn. Khi đầu bếp hoàn tất một món trên KDS, hệ thống tự động sinh lệnh trừ kho tức thời theo đúng định lượng mà không cần thủ kho nhập liệu thủ công.")

    add_h3(doc, "2.7.7. Sơ đồ Tổng thể Luồng Dữ liệu Liên module (System Architecture Flowchart)")
    add_p(doc, "Mối quan hệ trao đổi dữ liệu xuyên suốt giữa 5 module chức năng trong toàn bộ hệ thống được tổng hợp trực quan qua lưu đồ kiến trúc dưới đây:")

    add_figure(doc, "báo cáo/figures/mermaid/spec_erd_module_flowchart.png", "2.18", "Sơ đồ tổng thể luồng dữ liệu liên module", width_inch=4.8)

    add_p(doc, "Lưu đồ Hình 2.18 khẳng định tính khép kín và nhất quán của giải pháp: Luồng dữ liệu khởi phát từ tương tác đặt món của Khách hàng (QR Ordering) -> chuyển tiếp đồng bộ qua Quầy bán hàng (POS) -> điều phối tức thời xuống Nhà bếp (KDS) -> kích hoạt trừ kho tự động (BOM) -> đối soát thanh toán ngân hàng (SePay) -> tích điểm thành viên (Loyalty) -> tổng hợp lên bảng chỉ số tài chính (BOH Admin).")

    # ==========================================
    # 2.8. THIẾT KẾ CẤU TRÚC THÀNH PHẦN GIAO DIỆN (UI/UX COMPONENT ARCHITECTURE)
    # ==========================================
    add_h2(doc, "2.8. Thiết kế Cấu trúc Thành phần Giao diện (UI/UX Component Architecture)")
    add_p(doc, "Giao diện hệ thống được xây dựng trên nền tảng Blazor Server kết hợp CSS Tailwind hiện đại. Nhờ mô hình Component-based Architecture, toàn bộ giao diện người dùng được module hóa thành các thành phần tái sử dụng cao:")
    add_p(doc, "• `PosMainComponent`: Thành phần trung tâm của quầy thu ngân, quản lý trạng thái đơn hàng hiện hành, danh sách món trong giỏ, áp dụng voucher và cổng thanh toán. Giao tiếp với `TableGridComponent` thông qua EventCallback khi đổi bàn hoặc gộp đơn.")
    add_p(doc, "• `PosTablesComponent`: Hiển thị sơ đồ sảnh trực quan theo từng khu vực (Floor Tabs). Sử dụng CSS Grid responsive để hiển thị trạng thái động của các bàn ăn; lắng nghe sự kiện SignalR để tự động cập nhật màu sắc bàn và nhấp nháy khi có đơn Draft mới.")
    add_p(doc, "• `KdsDashboardComponent`: Màn hình điều phối chế biến nhà bếp, hiển thị các thẻ đơn dạng Kanban card theo thời gian thực. Cung cấp các nút bấm tương tác một chạm: 'Nhận nấu' (Processing), 'Xong món' (Done), 'Gom mẻ' (Batching) và 'Báo hết' (Sold Out). Tích hợp cảnh báo âm thanh và viền đỏ nhấp nháy khi món ăn chờ quá ngưỡng quy định (SLA Warning).")
    add_p(doc, "• `QrMenuComponent`: Giao diện đặt món di động Mobile-first dành cho khách hàng. Tối ưu hóa cho màn hình cảm ứng điện thoại thông minh, hỗ trợ thanh tìm kiếm tức thì, lọc danh mục theo cuộn mượt (Sticky Category Nav), modal chọn modifier trực quan và theo dõi tiến độ đơn hàng thời gian thực mà không cần tải lại trang.")
    add_p(doc, "• `RecipeManagementComponent`: Giao diện quản trị định lượng BOM trong phân hệ BOH Admin, cho phép người quản lý tìm kiếm nguyên liệu, nhập khối lượng tiêu hao và tự động quy đổi đơn vị tính với độ chính xác cao.")

    print("Chapter 2 fully built with Use Cases, Class Diagrams, State Diagrams, ERD, and UI Architecture!")
