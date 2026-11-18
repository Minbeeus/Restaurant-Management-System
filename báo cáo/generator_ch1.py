import docx
from doc_helpers import add_p, add_h1, add_h2, add_h3, add_table_title, style_table
from docx.shared import Inches

def build_chapter_1(doc):
    add_h1(doc, "CHƯƠNG 1: TỔNG QUAN TÀI LIỆU VÀ CƠ SỞ LÝ LUẬN VỀ HỆ THỐNG QUẢN LÝ NHÀ HÀNG")

    add_h2(doc, "1.1. Cơ sở lý luận về quy trình vận hành và quản lý nhà hàng F&B")
    
    add_h3(doc, "1.1.1. Đặc thù và chu trình vận hành nghiệp vụ nhà hàng ẩm thực")
    add_p(doc, "Ngành dịch vụ ẩm thực (Food and Beverage - F&B) có những đặc thù vận hành vô cùng độc đáo so with các ngành thương mại bán lẻ truyền thống. Hàng hóa trong nhà hàng bao gồm cả sản phẩm vật chất (món ăn, đồ uống) lẫn dịch vụ trải nghiệm không gian và cung cách phục vụ. Đặc biệt, sản phẩm của nhà hàng có tính chất tiêu hao nhanh, hạn sử dụng ngắn, nguyên vật liệu thô dễ hư hỏng nếu không được bảo quản đúng tiêu chuẩn. Do đó, việc quản lý chuỗi cung ứng, tồn kho và chu trình chế biến đòi hỏi sự đồng bộ và độ chính xác ở mức độ gần như tuyệt đối theo thời gian thực.")

    add_p(doc, "Trong một nhà hàng tiêu chuẩn, chu trình vận hành được chia thành hai khu vực chức năng chính tương tác hữu cơ với nhau:")
    add_p(doc, "1. Khu vực Tiền sảnh (Front of House - FOH): Là nơi diễn ra toàn bộ các hoạt động tiếp xúc trực tiếp với thực khách. Nhân sự FOH bao gồm tiếp tân đón khách (Hostess), nhân viên phục vụ bàn (Waiters/Waitresses), nhân viên tiếp thực (Runners) và nhân viên thu ngân (Cashiers). Các hoạt động trọng tâm bao gồm: chào đón khách hàng, sắp xếp vị trí bàn ăn phù hợp theo số lượng người, tiếp nhận order món ăn và các yêu cầu cá nhân hóa (size, độ ngọt, mức đá, ghi chú dị ứng thực phẩm), bưng bê phục vụ món ăn lên bàn, hỗ trợ các yêu cầu phát sinh trong bữa ăn (gọi thêm món, đổi chỗ ngồi, dọn đĩa thừa) và xử lý thanh toán hóa đơn khi khách dùng bữa xong.")

    add_p(doc, "2. Khu vực Hậu sảnh (Back of House - BOH): Là trái tim sản xuất và quản trị của nhà hàng, bao gồm khu vực nhà bếp (Kitchen), quầy pha chế (Bar), kho bảo quản nguyên vật liệu (Dry storage & Cold storage) và văn phòng quản lý. Nhân sự BOH bao gồm bếp trưởng (Executive Chef), các đầu bếp phụ trách từng trạm (Station Chefs), nhân viên sơ chế, nhân viên pha chế (Bartenders), thủ kho (Stockkeeper) và quản lý nhà hàng (Store Manager). Các hoạt động BOH bao gồm: tiếp nhận lệnh chế biến từ tiền sảnh, phân loại món theo từng trạm chuyên môn (bếp nướng, bếp chiên, bếp lạnh, quầy bar), thực hiện chế biến món ăn theo đúng tiêu chuẩn công thức định lượng (Recipe Specification), kiểm soát thời gian nấu nướng cam kết (SLA), theo dõi tồn kho nguyên vật liệu và tiến hành nhập hàng, kiểm kê định kỳ.")

    add_h3(doc, "1.1.2. Khái niệm và vai trò của các phân hệ chức năng trong nhà hàng")
    add_p(doc, "Để đảm bảo chu trình khép kín giữa Tiền sảnh và Hậu sảnh diễn ra trơn tru, việc ứng dụng công nghệ thông tin đã hình thành nên 4 phân hệ phần mềm cốt lõi:")

    add_p(doc, "• Hệ thống Điểm bán lẻ (Point of Sale - POS): Là trung tâm đầu não điều phối toàn bộ các giao dịch tài chính và dịch vụ tại tiền sảnh. Phần mềm POS cho phép thu ngân quản lý sơ đồ bàn trực quan theo thời gian thực (bàn nào đang trống, bàn nào đang có khách, bàn nào đang chờ thanh toán); tiếp nhận và lập đơn hàng cho cả hai hình thức phục vụ tại chỗ (Dine-in) và mang về (Takeaway); hỗ trợ cấu hình tùy chọn món (Modifiers) và gói món (Combos); thực hiện các thao tác quản lý bàn phức tạp như chuyển bàn, gộp nhiều bàn thành một đơn chung, tách hóa đơn cho nhóm khách đi chung; áp dụng các chương trình khuyến mãi, voucher giảm giá, điểm thưởng thành viên; thực hiện thu ngân đa phương thức và in hóa đơn tạm tính/hóa đơn tài chính. Đồng thời, POS chịu trách nhiệm quản lý ca làm việc (Shift Management), theo dõi số dư tiền mặt đầu ca, các giao dịch nạp/rút tiền quỹ (Till-in/Till-out) và xuất báo cáo kết ca (Z-Report).")

    add_p(doc, "• Hệ thống Hiển thị Nhà bếp (Kitchen Display System - KDS): Là phân hệ kỹ thuật số hóa toàn diện quy trình vận hành trong bếp, thay thế hoàn toàn máy in vé giấy order nhiệt truyền thống. KDS tiếp nhận đơn hàng từ POS hoặc từ điện thoại của khách và lập tức hiển thị dưới dạng các thẻ đơn điện tử (Tickets). Mỗi thẻ hiển thị đầy đủ thông tin: số bàn, mã đơn, danh sách món kèm ghi chú chi tiết và bộ đếm thời gian chờ lũy tiến (Timer). KDS cho phép phân luồng món ăn theo từng trạm chế biến chuyên môn (Kitchen Stations), giúp các đầu bếp chỉ tập trung vào các món thuộc phạm vi của mình. Khi đầu bếp bắt đầu nấu, món ăn được chuyển sang trạng thái 'Đang chế biến' (Processing), và khi nấu xong chuyển sang trạng thái 'Hoàn thành' (Done). Việc này giúp đồng bộ tức thì tiến độ phục vụ lên màn hình POS của thu ngân và điện thoại của khách hàng. Đặc biệt, KDS cung cấp tính năng gom món (Aggregate View), tự động tổng hợp số lượng của cùng một món ăn từ nhiều đơn hàng khác nhau để đầu bếp có thể nấu chung một mẻ lớn, giúp tăng công suất bếp lên đáng kể.")

    add_p(doc, "• Hệ thống Quản trị Hậu sảnh (Back of House - BOH Admin): Là trung tâm quản lý dữ liệu và kế toán kho của nhà hàng. Phân hệ BOH chịu trách nhiệm: Quản lý danh mục thực đơn, danh mục tùy chọn và các gói combo; Thiết lập bảng định mức nguyên vật liệu (Bill of Materials - BOM) cho từng món ăn và từng tùy chọn modifier (ví dụ: 1 cốc trà sữa size L tiêu hao bao nhiêu gram trà, bao nhiêu ml sữa, bao nhiêu gram trân châu); Quản lý danh sách nhà cung cấp và quản lý quy trình nhập kho (Goods Receipt) tự động tính giá vốn nhập hàng; Quản lý đợt kiểm kê kho định kỳ (Stocktake) nhằm đối chiếu số lượng tồn lý thuyết trên phần mềm với số lượng đếm thực tế trong kho, từ đó tự động hạch toán chênh lệch tồn kho; Quản lý chính sách khách hàng thành viên (CRM & Loyalty Tiers); Thiết lập các hạn mức và chính sách bảo mật POS (POS Policy); Trung tâm phê duyệt yêu cầu nhạy cảm (Approval Center); và cung cấp hệ thống báo cáo phân tích kinh doanh đa chiều về doanh thu, lợi nhuận, chi phí nguyên vật liệu (COGS) và hao hụt kho.")

    add_p(doc, "• Hệ thống Khách hàng Tự gọi món tại bàn (QR Ordering): Là phân hệ tự phục vụ (Self-service) vận hành trên nền tảng web di động. Mỗi bàn ăn trong nhà hàng được gắn một mã QR chứa mã định danh bàn (tableId) và mã khóa bảo mật (token). Khách hàng chỉ cần sử dụng camera điện thoại thông minh để quét mã, trình duyệt web sẽ tự động mở trang thực đơn điện tử của đúng bàn đó mà không cần phải tải hay cài đặt bất kỳ ứng dụng nào. Khách hàng có thể tự do duyệt xem hình ảnh món ăn trực quan, tự chọn các tùy chọn size/topping, quản lý giỏ hàng cá nhân, nhập thông tin thành viên để tích điểm và bấm gửi đơn hàng trực tiếp lên quầy thu ngân. Toàn bộ tiến độ chế biến món ăn dưới bếp cũng được cập nhật trực tiếp trên màn hình điện thoại của khách hàng thông qua kết nối thời gian thực.")

    add_h2(doc, "1.2. Khảo sát thực trạng quy trình vận hành tại các mô hình nhà hàng ẩm thực")
    add_p(doc, "Trong khuôn khổ nghiên cứu đề tài, tác giả đã tiến hành tổng hợp dữ liệu khảo sát từ các Báo cáo thị trường Kinh doanh Ẩm thực tại Việt Nam [1], [2] kết hợp tìm hiểu thực tế tại ba mô hình vận hành F&B phổ biến hiện nay: (1) Mô hình Gọi món phục vụ tại bàn truyền thống (Full-service Dine-in); (2) Mô hình Chuỗi cà phê - trà sữa phục vụ nhanh (Fast-casual & Beverage); và (3) Mô hình Lẩu nướng quy mô vừa. Kết quả phân tích cho thấy những điểm nghẽn nghiêm trọng trong quy trình vận hành thủ công hoặc bán tự động hiện nay:")

    add_p(doc, "• Tắc nghẽn tại quầy thu ngân trong khung giờ cao điểm (Peak Hours Bottleneck): Trong các khung giờ trưa (11h30 - 13h00) và tối (18h30 - 20h30), lưu lượng khách dồn về cùng lúc khiến nhân viên phục vụ không kịp tiếp cận bàn để ghi order. Khách hàng trung bình phải chờ từ 7 đến 12 phút mới có thể gọi món, và mất thêm 5 đến 8 phút chờ in phiếu thanh toán, gây ức chế tâm lý và giảm tỷ lệ quay lại của khách hàng [1].")

    add_p(doc, "• Tỷ lệ sai sót cao khi chuyển giao thông tin thủ công: Việc nhân viên ghi chép order bằng giấy hoặc truyền đạt bằng miệng xuống bếp dẫn đến tỷ lệ nhầm lẫn món ăn, quên ghi chú đặc biệt (ví dụ: không hành, ít cay, dị ứng đậu phộng) lên tới 12% - 15% tổng số đơn hàng [2]. Hậu quả là nhà bếp phải nấu lại món, phát sinh chi phí lãng phí nguyên vật liệu và tăng thời gian chờ của khách hàng.")

    add_p(doc, "• Thất thoát nguyên vật liệu do thiếu kiểm soát định mức BOM theo thời gian thực: Đa số các nhà hàng vừa và nhỏ chỉ kiểm kê tồn kho theo tuần hoặc theo tháng. Việc không liên kết giữa món bán ra tại quầy với số lượng nguyên vật liệu tiêu hao trong kho khiến chủ nhà hàng không thể phát hiện tình trạng hao hụt do nhân viên gian lận, làm rơi vãi hoặc hư hỏng. Tỷ lệ thất thoát giá vốn nguyên liệu (COGS) ghi nhận thực tế dao động từ 8% đến 14% trên tổng doanh thu theo đánh giá của các chuyên gia ngành ẩm thực [2].")

    add_p(doc, "• Rủi ro giả mạo biên lai chuyển khoản ngân hàng: Xu hướng thanh toán không tiền mặt tăng trưởng đột biến với hơn 70% khách hàng lựa chọn quét mã QR ngân hàng [1], [3]. Tuy nhiên, hình thức thanh toán mã QR tĩnh dán tại bàn hoặc tại quầy thu ngân tiềm ẩn rủi ro lớn: nhân viên thu ngân phải nhìn màn hình điện thoại của khách để đối chiếu số tiền và tên người nhận. Lợi dụng điều này, nhiều đối tượng đã sử dụng các ứng dụng tạo ảnh biên lai chuyển tiền giả mạo (Fake Bill App) để gian lận chiếm đoạt tiền hàng mà thu ngân không thể phát hiện tức thời.")

    add_h2(doc, "1.3. Tổng quan các hướng nghiên cứu và giải pháp hiện có")
    
    add_h3(doc, "1.3.1. Phân tích các giải pháp thương mại phổ biến trên thị trường")
    add_p(doc, "Thị trường giải pháp công nghệ quản lý F&B tại Việt Nam hiện đang có sự hiện diện của một số nhà cung cấp phần mềm thương mại lớn. Nhằm làm rõ bối cảnh nghiên cứu và xác định vị trí của đề tài, tác giả đã tiến hành khảo sát và phân tích chuyên sâu ba giải pháp tiêu biểu đại diện cho ba phân khúc thị trường dựa trên các báo cáo công nghệ ngành dịch vụ nhà hàng [2], [6]:")

    add_p(doc, "1. Giải pháp KiotViet F&B: KiotViet là một trong những phần mềm quản lý bán hàng phổ biến nhất tại Việt Nam nhờ chi phí thuê bao hợp lý và giao diện tương đối thân thiện. Tuy nhiên, hệ thống F&B của KiotViet vốn được phát triển mở rộng từ nền tảng quản lý bán lẻ tạp hóa truyền thống. Do đó, hệ thống bộc lộ nhiều điểm yếu cố hữu: phân hệ nhà bếp KDS rất đơn giản, chỉ hỗ trợ in phiếu báo chế biến mà thiếu khả năng tương tác hai chiều (đầu bếp không thể bấm nhận làm món hay hoàn thành món trên màn hình); cơ chế đồng bộ dữ liệu giữa các thiết bị dựa trên kỹ thuật HTTP Polling định kỳ từ 3 đến 5 giây, dẫn đến độ trễ lớn và thường xuyên xảy ra tình trạng lệch trạng thái bàn trong giờ cao điểm; tính năng gọi món QR tại bàn chỉ dừng lại ở mức xem menu tĩnh hoặc gửi đơn dạng danh sách thô, không hỗ trợ đồng bộ phiên realtime khi khách đổi bàn.")

    add_p(doc, "2. Giải pháp CukCuk (MISA): CukCuk là sản phẩm chuyên biệt cho ngành F&B của Công ty Cổ phần MISA. CukCuk có thế mạnh lớn về hệ thống báo cáo kế toán tài chính và khả năng kết nối chặt chẽ với phần mềm kế toán MISA SME. Tuy nhiên, kiến trúc của CukCuk khá cồng kềnh, phân chia thành nhiều ứng dụng riêng biệt đòi hỏi cài đặt phức tạp (CukCuk Server, CukCuk Bán hàng, CukCuk Bếp). Việc triển khai đòi hỏi cấu hình mạng nội bộ phức tạp. Phân hệ KDS của CukCuk là một module tính phí riêng và chưa tối ưu hóa tốt cho các thao tác gom món theo mẻ lớn. Cơ chế trừ kho nguyên vật liệu của CukCuk chủ yếu thực hiện vào cuối ngày hoặc khi hóa đơn đã đóng hoàn toàn, không hỗ trợ trừ kho tức thời tại thời điểm đầu bếp hoàn thành món, dẫn đến số liệu tồn kho nguyên liệu trong ngày luôn bị trễ so với thực tế sản xuất.")

    add_p(doc, "3. Giải pháp iPOS.vn: iPOS.vn là giải pháp quản trị F&B lâu năm, được nhiều chuỗi cà phê và nhà hàng lớn tại Việt Nam tin dùng. iPOS sở hữu hệ sinh thái phần cứng và phần mềm chuyên dụng rất bài bản. Tuy nhiên, rào cản lớn nhất của iPOS là chi phí đầu tư ban đầu vô cùng đắt đỏ: hệ thống bắt buộc phải cài đặt trên các máy POS cảm ứng chuyên dụng chạy hệ điều hành Windows bản quyền; hệ thống KDS đòi hỏi các màn hình công nghiệp có chi phí cao; kiến trúc lõi của iPOS vẫn là mô hình Client/Server truyền thống kết nối cơ sở dữ liệu nội bộ qua mạng LAN, gây khó khăn cho việc quản lý từ xa qua Internet; khi muốn triển khai tính năng QR Order, nhà hàng phải tích hợp thêm ứng dụng Zalo Mini App của bên thứ ba, làm phát sinh chi phí trung gian và dữ liệu không được kiểm soát tập trung.")

    add_table_title(doc, "1.1", "Ma trận so sánh chuyên sâu giữa các giải pháp thương mại và Hệ thống Đề tài")
    headers_1_1 = ["Tiêu chí Đánh giá", "KiotViet F&B", "CukCuk (MISA)", "iPOS.vn", "Hệ thống Đề tài"]
    data_1_1 = [
        ["Nền tảng kiến trúc", "Web / Hybrid App", "Client/Server + Cloud", "Desktop C/S nội bộ", "Full Web (.NET 9 + Blazor)"],
        ["Giao thức đồng bộ", "HTTP Polling (3 - 5s)", "Polling / WebSockets", "Local LAN Socket", "SignalR Hub Core (< 200ms)"],
        ["Phân hệ KDS Bếp", "Cơ bản, in phiếu giấy", "Module rời, tính phí", "Đòi hỏi phần cứng đắt tiền", "KDS Web đa trạm, gom món"],
        ["Gọi món tự phục vụ QR", "Web tĩnh, hạn chế", "Web app hỗ trợ QR", "Zalo Mini App phụ trợ", "Full Web Responsive, Auto Sync"],
        ["Trừ kho định mức BOM", "Trừ định kỳ cuối ngày", "Trừ khi đóng hóa đơn", "Trừ khi đóng ca", "Trừ tức thời khi KDS Done"],
        ["Hủy món & Lãng phí", "Không phân loại waste", "Hủy chung vào hóa đơn", "Cần quản lý xóa bill", "Hạch toán Cancellation Waste"],
        ["Thanh toán VietQR", "Mã tĩnh / Nhập tay", "Có liên kết ngân hàng", "Tích hợp ví điện tử", "VietQR động + SePay Webhook"],
        ["Bảo mật ủy quyền POS", "Phân quyền theo nhóm", "Tài khoản đăng nhập", "Thẻ từ / Mật khẩu", "Mã PIN Quản lý BCrypt + Lock"],
        ["Chi phí triển khai", "Trung bình", "Khá cao (tính theo máy)", "Rất cao (phần cứng riêng)", "Tối ưu (chạy trên trình duyệt)"]
    ]
    style_table(doc, [Inches(1.8), Inches(1.3), Inches(1.3), Inches(1.3), Inches(1.8)], headers_1_1, data_1_1)

    add_h3(doc, "1.3.2. Những vấn đề tồn tại và bài toán nghiên cứu đặt ra")
    add_p(doc, "Từ việc phân tích thực trạng các giải pháp trên thị trường, có thể tổng hợp 4 bài toán kỹ thuật lớn mà các hệ thống hiện hành chưa giải quyết triệt để:")

    add_p(doc, "1. Bài toán Trễ dữ liệu và Xung đột giao dịch đồng thời (Concurrency & Latency): Khi nhiều khách hàng tại cùng một bàn cùng quét mã QR để đặt món, hoặc khi khách hàng bấm gửi đơn từ điện thoại đúng thời điểm thu ngân đang thao tác thêm món trên phần mềm POS, cơ chế Polling truyền thống sẽ gây ra độ trễ cập nhật trạng thái bàn. Hệ thống thiếu cơ chế khóa dữ liệu lạc quan (Optimistic Locking) sẽ dẫn đến xung đột ghi đè dữ liệu (Lost Update), làm mất món ăn của khách hoặc gây sai lệch tổng tiền hóa đơn.")

    add_p(doc, "2. Bài toán Toàn vẹn tồn kho định mức BOM và Xử lý Lãng phí do Hủy món (BOM Integrity & Cancellation Waste): Hầu hết các phần mềm hiện nay chỉ thực hiện trừ kho khi hóa đơn được thanh toán thành công. Quy trình này hoàn toàn sai lệch so with thực tế sản xuất của nhà bếp: nguyên vật liệu thực chất đã bị tiêu hao ngay khi đầu bếp chế biến xong món ăn. Nếu khách hàng ngồi ăn nhưng sau đó thông báo hủy món (do đợi lâu hoặc do chất lượng món không đạt), việc không trừ kho hoặc tự động cộng lại nguyên vật liệu thô vào kho sẽ làm sai lệch nghiêm trọng số liệu tồn kho thực tế. Hệ thống cần có thuật toán bóc tách công thức định mức BOM tự động ngay khi món ăn chuyển trạng thái 'Hoàn thành' (Done) trên KDS, và nếu món bị hủy sau đó, chi phí nguyên liệu phải được tự động chuyển sang tài khoản 'Hao hụt hủy món' (Cancellation Waste) để ban giám đốc đối soát lãng phí.")

    add_p(doc, "3. Bài toán Tự động hóa Đối soát Thanh toán không tiền mặt theo thời gian thực (Real-time Payment Reconciliation): Việc thanh toán qua mã QR động với nội dung chuyển khoản duy nhất và cơ chế tiếp nhận Webhook ngân hàng tự động (Automated Bank Webhook Listener) là chìa khóa để triệt tiêu tình trạng gian lận biên lai giả. Hệ thống phải đảm bảo tính Idempotent tuyệt đối (chỉ xử lý giao dịch một lần duy nhất kể cả khi ngân hàng gửi trùng lặp Webhook) và tự động kích hoạt in hóa đơn nhiệt K80 ngay khi tiền nổi trong tài khoản mà không cần bất kỳ sự can thiệp thủ công nào của thu ngân.")

    add_p(doc, "4. Bài toán Phân quyền hai lớp và Kiểm soát dòng tiền mặt trong ca (Two-Factor Authorization & Cash Management): Quầy thu ngân luôn là nơi tiềm ẩn nguy cơ gian lận nội bộ cao nhất. Các thao tác nhạy cảm như xóa món ăn đã nấu, hủy hóa đơn, áp dụng mức giảm giá vượt trần hoặc rút tiền mặt ra khỏi két đều phải được xác thực bằng mã PIN Quản lý (Manager PIN) được mã hóa một chiều an toàn bằng thuật toán BCrypt. Đồng thời, hệ thống phải lưu trữ nhật ký kiểm toán (Audit Log) chi tiết và kiểm soát nghiêm ngặt chênh lệch tiền két (Cash Discrepancy) giữa số tiền lý thuyết trên hệ thống và số tiền thực tế kiểm đếm cuối ca.")

    add_h2(doc, "1.4. Đặc tả yêu cầu hệ thống phần mềm (Software Requirements Specification - SRS)")
    add_p(doc, "Dựa trên kết quả khảo sát thực tế và phân tích các bài toán kỹ thuật, tác giả đã tiến hành chuẩn hóa bộ yêu cầu hệ thống chi tiết bao gồm 25 yêu cầu chức năng trọng tâm và 10 yêu cầu phi chức năng tiêu chuẩn:")

    add_table_title(doc, "1.2a", "Bảng đặc tả Yêu cầu Chức năng Phân hệ QR Ordering và POS (FR-01 đến FR-13)")
    headers_1_2a = ["Mã FR", "Tên Yêu cầu Chức năng", "Phân hệ Nghiệp vụ", "Mô tả Chi tiết Nghiệp vụ", "Độ ưu tiên"]
    data_1_2a = [
        ["FR-01", "Quét QR nhận diện bàn", "QR Ordering", "Xác thực token 32 ký tự, định danh bàn và cấp phiên gọi món", "Bắt buộc"],
        ["FR-02", "Xem thực đơn di động", "QR Ordering", "Hiển thị danh mục, hình ảnh, giá bán, tùy chọn món ăn realtime", "Bắt buộc"],
        ["FR-03", "Tùy biến món & Topping", "QR Ordering", "Hỗ trợ chọn mức đường, đá, kích cỡ và danh sách topping phụ thu", "Bắt buộc"],
        ["FR-04", "Chọn món Combo", "QR Ordering", "Bắt buộc chọn đủ số lượng món thành phần theo quy tắc từng nhóm", "Bắt buộc"],
        ["FR-05", "Gửi đơn gọi món tạm", "QR Ordering", "Tạo bản ghi Order ở trạng thái Draft, phát thông báo chuông tới POS", "Bắt buộc"],
        ["FR-06", "Theo dõi tiến độ nấu", "QR Ordering", "Cập nhật realtime tiến độ từng món: Chờ nấu -> Đang nấu -> Hoàn tất", "Bắt buộc"],
        ["FR-07", "Đồng bộ chuyển bàn", "QR Ordering", "Tự động chuyển phiên và bảo toàn giỏ hàng khi thu ngân chuyển bàn", "Khuyến nghị"],
        ["FR-08", "Quản lý sơ đồ bàn", "POS Thu ngân", "Trực quan hóa trạng thái sảnh qua 4 mã màu: Trống, Có khách, Đơn mới, Chờ tính tiền", "Bắt buộc"],
        ["FR-09", "Duyệt đơn gọi món QR", "POS Thu ngân", "Kiểm tra thực đơn khách gửi, bấm duyệt chuyển lệnh xuống KDS", "Bắt buộc"],
        ["FR-10", "Bán hàng tại quầy & Mang đi", "POS Thu ngân", "Tạo đơn tại bàn hoặc đơn Takeaway, hỗ trợ tìm kiếm món siêu tốc", "Bắt buộc"],
        ["FR-11", "Chuyển / Gộp bàn", "POS Thu ngân", "Chuyển toàn bộ món sang bàn mới hoặc gộp nhiều bàn thành một hóa đơn", "Bắt buộc"],
        ["FR-12", "Hủy món phân quyền", "POS Thu ngân", "Hủy món chưa nấu hoặc yêu cầu mã PIN quản lý nếu món đang nấu dở", "Bắt buộc"],
        ["FR-13", "In phiếu tạm tính K80", "POS Thu ngân", "In bảng kê chi tiết món ăn, thuế VAT, giảm giá trước khi thanh toán", "Bắt buộc"]
    ]
    style_table(doc, [Inches(0.8), Inches(1.8), Inches(1.3), Inches(2.8), Inches(0.8)], headers_1_2a, data_1_2a)

    add_table_title(doc, "1.2b", "Bảng đặc tả Yêu cầu Chức năng Phân hệ KDS, BOM và Quản trị (FR-14 đến FR-25)")
    headers_1_2b = ["Mã FR", "Tên Yêu cầu Chức năng", "Phân hệ Nghiệp vụ", "Mô tả Chi tiết Nghiệp vụ", "Độ ưu tiên"]
    data_1_2b = [
        ["FR-14", "Thanh toán VietQR SePay", "POS Thu ngân", "Sinh mã VietQR động, tự động đóng đơn khi nhận Webhook ngân hàng", "Bắt buộc"],
        ["FR-15", "Thu tiền mặt & Tiền thối", "POS Thu ngân", "Gợi ý mệnh giá tiền mặt, tự động tính tiền thối lại cho khách", "Bắt buộc"],
        ["FR-16", "Quản lý ca làm việc", "POS Thu ngân", "Khai báo tiền két đầu ca, ghi nhận Nạp/Rút tiền (Till In/Out), in Z-Report", "Bắt buộc"],
        ["FR-17", "Lọc món theo trạm bếp", "KDS Nhà bếp", "Phân luồng vé chế biến theo chuyên môn: Bếp nướng, bếp xào, quầy bar", "Bắt buộc"],
        ["FR-18", "Cảnh báo vi phạm SLA", "KDS Nhà bếp", "Đổi màu thẻ đơn: Vàng khi quá 10 phút, Đỏ nhấp nháy khi quá 15 phút", "Bắt buộc"],
        ["FR-19", "Cập nhật tiến độ 1 chạm", "KDS Nhà bếp", "Chạm chuyển trạng thái: Pending -> Processing -> Done", "Bắt buộc"],
        ["FR-20", "Báo hết món tức thời", "KDS Nhà bếp", "Khóa món hết hàng trên POS và làm mờ nút đặt món trên QR Mobile", "Bắt buộc"],
        ["FR-21", "Trừ kho tự động BOM", "Kho hàng BOM", "Tự động bóc tách công thức, giảm tồn nguyên vật liệu khi KDS Done", "Bắt buộc"],
        ["FR-22", "Hạch toán hao hụt hủy món", "Kho hàng BOM", "Chuyển chi phí nguyên liệu món hủy vào tài khoản Cancellation Waste", "Bắt buộc"],
        ["FR-23", "Thiết lập công thức BOM", "BOH Admin", "Định nghĩa định mức tiêu hao nguyên liệu cho từng món ăn và modifier", "Bắt buộc"],
        ["FR-24", "Kiểm kê cân bằng kho", "BOH Admin", "Nhập số liệu đếm thực tế, đối soát chênh lệch và tự cân bằng tồn", "Bắt buộc"],
        ["FR-25", "Báo cáo phân tích kinh doanh", "BOH Admin", "Thống kê doanh thu theo ngày/tháng, món bán chạy, chi phí hao hụt kho", "Bắt buộc"]
    ]
    style_table(doc, [Inches(0.8), Inches(1.8), Inches(1.3), Inches(2.8), Inches(0.8)], headers_1_2b, data_1_2b)

    add_p(doc, "Bên cạnh các yêu cầu chức năng, hệ thống thiết lập bộ tiêu chuẩn chất lượng kỹ thuật nghiêm ngặt nhằm đảm bảo tính ổn định cao trong môi trường sản xuất thực tế:")

    add_table_title(doc, "1.3", "Bảng đặc tả Yêu cầu Phi chức năng Hệ thống (Non-Functional Requirements - NFR)")
    headers_1_3 = ["Mã NFR", "Nhóm Thuộc tính", "Tiêu chuẩn Kỹ thuật Cam kết", "Phương pháp Kiểm chứng"]
    data_1_3 = [
        ["NFR-01", "Hiệu năng phản hồi", "Độ trễ thao tác trên POS và KDS dưới 200ms; thanh toán tiền mặt dưới 500ms", "Đo kiểm thời gian thực qua Chrome DevTools"],
        ["NFR-02", "Đồng bộ Realtime", "Độ trễ truyền phát tín hiệu SignalR giữa các thiết bị dưới 150ms trong mạng LAN", "Ghi log timestamp tại máy phát và máy nhận"],
        ["NFR-03", "Khả năng chịu tải", "Xử lý đồng thời 500 yêu cầu/giây mà không xảy ra lỗi kết nối cơ sở dữ liệu", "Kiểm thử áp lực tải với công cụ k6"],
        ["NFR-04", "Tính sẵn sàng", "Chỉ số sẵn sàng hoạt động đạt 99.9%, hỗ trợ tự động phục hồi kết nối SignalR", "Giám sát hệ thống uptime liên tục"],
        ["NFR-05", "Bảo mật xác thực", "Mật khẩu và mã PIN mã hóa BCrypt; phân quyền API bằng JWT Claims và Role", "Kiểm thử bảo mật kiểm tra chuỗi băm DB"],
        ["NFR-06", "Toàn vẹn dữ liệu", "Mọi luồng thanh toán và trừ kho thực thi trong Transaction cấp độ Read Committed", "Kiểm thử vi phạm ràng buộc ACID"],
        ["NFR-07", "Kiểm soát tương tranh", "Áp dụng Optimistic Concurrency Control với RowVersion chống xung đột Lost Update", "Kiểm thử 2 luồng ghi đồng thời trên 1 Order"],
        ["NFR-08", "Tính Idempotent", "Giao dịch Webhook ngân hàng SePay xử lý chính xác 1 lần, chống xử lý lặp", "Gửi lặp lại 1 payload Webhook 5 lần liên tiếp"],
        ["NFR-09", "Tính tương thích", "Hoạt động trơn tru trên mọi trình duyệt web hiện đại (Chrome, Edge, Safari, iOS)", "Thử nghiệm trên 5 thiết bị phần cứng khác nhau"],
        ["NFR-10", "Dễ bảo trì mở rộng", "Tuân thủ Clean Architecture 5 tầng, mã nguồn có độ ghép nối lỏng (Loose Coupling)", "Đánh giá cấu trúc module và độ độc lập tầng"]
    ]
    style_table(doc, [Inches(0.9), Inches(1.5), Inches(3.2), Inches(1.9)], headers_1_3, data_1_3)

    add_h3(doc, "1.4.1. Ma trận Phân quyền và Trách nhiệm Người dùng (RACI Matrix)")
    add_p(doc, "Để đảm bảo tính minh bạch và tránh chồng chéo trách nhiệm trong quy trình vận hành nhà hàng, ma trận RACI dưới đây phân định rõ ràng quyền hạn của 5 nhóm vai trò người dùng trong hệ thống:")
    add_p(doc, "• R (Responsible): Người trực tiếp thực hiện tác vụ nghiệp vụ.")
    add_p(doc, "• A (Accountable): Người chịu trách nhiệm phê duyệt cao nhất đối với kết quả tác vụ.")
    add_p(doc, "• C (Consulted): Người được tham vấn ý kiến hoặc cung cấp thông tin liên quan.")
    add_p(doc, "• I (Informed): Người nhận thông báo tự động về tiến độ hoặc kết quả tác vụ.")

    add_table_title(doc, "1.4", "Ma trận Phân quyền và Trách nhiệm RACI trong Vận hành Nhà hàng")
    headers_1_4 = ["Nghiệp vụ Trọng tâm", "Khách hàng", "Thu ngân", "Đầu bếp", "Quản lý", "Thủ kho"]
    data_1_4 = [
        ["Quét QR gọi món tại bàn", "R", "I", "I", "I", "I"],
        ["Duyệt đơn gọi món QR", "I", "R", "I", "A", "I"],
        ["Lập đơn tại quầy & Mang đi", "I", "R", "I", "A", "I"],
        ["Chế biến món ăn & Báo xong", "I", "I", "R", "A", "I"],
        ["Hủy món ăn đang chế biến", "I", "R", "C", "A", "I"],
        ["In hóa đơn & Thu tiền mặt", "I", "R", "I", "A", "I"],
        ["Xác nhận thanh toán VietQR", "I", "I", "I", "A", "I"],
        ["Đóng/Mở ca & Khai báo két", "I", "R", "I", "A", "I"],
        ["Cấu hình định mức kho BOM", "I", "I", "C", "A", "R"],
        ["Nhập kho nguyên vật liệu", "I", "I", "I", "A", "R"],
        ["Kiểm kê & Cân bằng kho", "I", "I", "I", "A", "R"],
        ["Xem báo cáo tài chính BOH", "I", "I", "I", "R / A", "I"]
    ]
    style_table(doc, [Inches(2.5), Inches(1.0), Inches(1.0), Inches(1.0), Inches(1.0), Inches(1.0)], headers_1_4, data_1_4)

    add_h2(doc, "1.5. Cơ sở công nghệ và giải pháp kỹ thuật đề xuất")
    add_p(doc, "Nhằm giải quyết triệt để các hạn chế của các giải pháp hiện hành, đề tài đề xuất kiến trúc hệ thống quản lý nhà hàng hiện đại xây dựng trên nền tảng công nghệ Microsoft .NET 9 và Blazor Server:")

    add_p(doc, "• Nền tảng .NET 9 và C# 13: Cung cấp hiệu năng xử lý tính toán cực cao, tối ưu hóa bộ nhớ và khả năng biên dịch AOT (Ahead-of-Time). Hệ sinh thái .NET cung cấp cơ chế Dependency Injection (DI) mạnh mẽ, hỗ trợ triển khai chuẩn mực kiến trúc Clean Architecture phân tầng độc lập [7].")

    add_p(doc, "• Công nghệ Blazor Server: Cho phép xây dựng giao diện người dùng tương tác phong phú (Rich Interactive UI) hoàn toàn bằng ngôn ngữ C# mà không cần viết mã JavaScript phức tạp. Ứng dụng duy trì một kết nối WebSocket SignalR liên tục giữa máy chủ và trình duyệt. Mọi tương tác giao diện (bấm nút, chọn bàn, đổi trạng thái) được gửi lên máy chủ xử lý, tính toán cây DOM ảo và chỉ truyền ngược về trình duyệt phần mã HTML nhị phân (Binary Diff) siêu nhỏ gọn, giúp giao diện đạt tốc độ phản hồi gần như tức thì mà tải mạng lại vô cùng nhẹ [8].")

    add_p(doc, "• Giao tiếp thời gian thực SignalR Core Hub: Là xương sống truyền dẫn thông điệp hai chiều giữa các phân hệ. SignalR tự động lựa chọn giao thức truyền tải tối ưu nhất (WebSockets, Server-Sent Events hoặc Long Polling). Hệ thống triển khai phân nhóm kết nối (Connection Groups) thông minh: nhóm bàn sảnh (`TablesGroup`), nhóm trạm bếp (`KitchenStationsGroup`), nhóm quầy thu ngân (`CashiersGroup`) và nhóm khách hàng theo từng bàn (`TableSessionGroup`). Khi có sự kiện phát sinh (khách gửi đơn mới, đầu bếp nấu xong món, SePay báo tiền về), thông điệp chỉ được phát thanh trực tiếp tới đúng các nhóm liên quan, triệt tiêu hoàn toàn độ trễ dữ liệu [8].")

    add_p(doc, "• Entity Framework Core 9 và Microsoft SQL Server 2022: EF Core đóng vai trò là tầng ánh xạ đối tượng quan hệ (ORM) tối ưu, hỗ trợ lập trình hướng thực thể (Domain Entity Modeling), quản lý phiên bản cơ sở dữ liệu qua Migrations tự động và tích hợp cơ chế Change Tracker mạnh mẽ [9]. Hệ quản trị SQL Server 2022 đảm bảo độ tin cậy giao dịch tuyệt đối, hỗ trợ kiểu dữ liệu RowVersion phục vụ khóa tương tranh lạc quan và chỉ mục phân cụm (Clustered Index) tối ưu hóa truy vấn dữ liệu báo cáo lớn [10].")

    add_p(doc, "• Tích hợp Cổng thanh toán VietQR và Webhook SePay: Hệ thống tự động sinh mã VietQR động tuân thủ tiêu chuẩn kỹ thuật Napas 247 [3]. Mỗi mã QR chứa chính xác số tài khoản ngân hàng của nhà hàng, số tiền cần thanh toán và nội dung chuyển khoản là mã định danh đơn hàng (ví dụ: `DH10452`). SePay cung cấp dịch vụ lắng nghe biến động số dư ngân hàng và phát Webhook tức thời về WebAPI của hệ thống trong vòng chưa đầy 1 giây theo tài liệu tích hợp SePay [18]. Khi nhận Webhook, hệ thống kiểm tra tính hợp lệ của token bảo mật, đối soát mã đơn hàng và số tiền, tự động cập nhật trạng thái đơn thành 'Đã thanh toán' (Paid) và phát lệnh in hóa đơn ra máy in K80 mà thu ngân không cần chạm tay vào bàn phím.")

print("Module Chapter 1 expanded successfully with SRS and RACI matrices!")
