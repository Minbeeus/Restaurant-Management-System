import matplotlib.pyplot as plt
import matplotlib.patches as patches
import os

os.makedirs('báo cáo/figures', exist_ok=True)

# Set font family
plt.rcParams['font.sans-serif'] = 'DejaVu Sans'
plt.rcParams['font.family'] = 'sans-serif'

def draw_box(ax, x, y, w, h, text, color='#2563eb', textcolor='white', fontsize=10, bold=True, subtext=''):
    rect = patches.FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.03", 
                                  linewidth=1.5, edgecolor='#1e293b', facecolor=color)
    ax.add_patch(rect)
    full_text = text if not subtext else f"{text}\n{subtext}"
    fontweight = 'bold' if bold else 'normal'
    ax.text(x + w/2, y + h/2, full_text, color=textcolor, fontsize=fontsize,
            ha='center', va='center', fontweight=fontweight, multialignment='center')

def draw_arrow(ax, x1, y1, x2, y2, text='', color='#475569'):
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="->", color=color, lw=1.8, shrinkA=3, shrinkB=3))
    if text:
        ax.text((x1 + x2)/2, (y1 + y2)/2 + 0.02, text, fontsize=8, color='#0f172a', ha='center', va='bottom',
                bbox=dict(boxstyle="square,pad=0.1", fc="white", ec="none", alpha=0.85))

# -------------------------------------------------------------
# 1. HÌNH 2.1: Sơ đồ kiến trúc Clean Architecture 5 tầng
# -------------------------------------------------------------
fig, ax = plt.subplots(figsize=(10, 6.5), dpi=200)
ax.set_xlim(0, 10)
ax.set_ylim(0, 7)
ax.axis('off')

# Presentation Layer
draw_box(ax, 0.5, 5.4, 4.2, 1.2, "Restaurant.Web (Blazor Server)", color='#0284c7', subtext="POS, KDS, BOH Admin, QR Mobile\nInteractiveServer, Components, SignalR Client")
draw_box(ax, 5.3, 5.4, 4.2, 1.2, "Restaurant.WebAPI (ASP.NET Core)", color='#0284c7', subtext="24 REST Controllers, JWT Middleware,\nGlobal Exception & Audit Filter")

# Application Layer
draw_box(ax, 1.5, 3.7, 7.0, 1.2, "Restaurant.Application", color='#16a34a', subtext="Interfaces (IPosServices, IInventoryService, IKitchenService)\nDTOs, Business Rules, Permissions Constants, Validators")

# Domain Layer
draw_box(ax, 2.5, 2.0, 5.0, 1.2, "Restaurant.Domain (Core Kernel)", color='#ea580c', subtext="Entities (Order, OrderItem, Payment, Shift, Material)\nValueObjects, Enums, Domain Events (BOM Deduction)")

# Infrastructure Layer
draw_box(ax, 0.8, 0.3, 8.4, 1.2, "Restaurant.Infrastructure", color='#7c3aed', subtext="EF Core 9 ApplicationDbContext (42 DbSets), SQL Server Persistence,\nSignalR RestaurantHub, SePay Webhook, BCrypt Hashing, File I/O")

# Dependency arrows
draw_arrow(ax, 2.6, 5.4, 3.5, 4.9, "Depends on")
draw_arrow(ax, 7.4, 5.4, 6.5, 4.9, "Depends on")
draw_arrow(ax, 5.0, 3.7, 5.0, 3.2, "Depends on Core Domain")
draw_arrow(ax, 3.0, 0.3 + 1.2, 3.5, 2.0, "Implements Domain")
draw_arrow(ax, 7.0, 0.3 + 1.2, 6.5, 3.7, "Implements Interfaces")

fig.savefig('báo cáo/figures/fig_2_1_architecture.png', bbox_inches='tight')
plt.close(fig)

# -------------------------------------------------------------
# 2. HÌNH 2.2: Luồng đặt món QR & đồng bộ KDS / POS
# -------------------------------------------------------------
fig, ax = plt.subplots(figsize=(10, 5.5), dpi=200)
ax.set_xlim(0, 10)
ax.set_ylim(0, 6)
ax.axis('off')

draw_box(ax, 0.4, 4.2, 2.6, 1.3, "Khách hàng (QR Ordering)", color='#059669', subtext="Quét mã QR bàn ăn\nChọn món, combo & gửi đơn")
draw_box(ax, 3.7, 4.2, 2.6, 1.3, "Thu ngân (POS Sảnh)", color='#d97706', subtext="Nhận tín hiệu NewQrOrder\nKiểm tra & Duyệt đơn hàng")
draw_box(ax, 7.0, 4.2, 2.6, 1.3, "Đầu bếp (KDS Bếp)", color='#dc2626', subtext="Nhận vé món chế biến\nNhận nấu & Báo hoàn tất")

draw_box(ax, 2.5, 1.8, 5.0, 1.4, "Hệ thống Trung tâm (WebAPI + SignalR Hub)", color='#2563eb', subtext="Database Transactions (EF Core 9)\nBroadcast events: TableStatusSync, KitchenTicket,\nOrderItemStatusChanged, TableSessionMoved")

draw_box(ax, 3.2, 0.2, 3.6, 1.0, "Cơ sở Dữ liệu (SQL Server)", color='#475569', subtext="Orders, OrderItems, Tables, BOM Materials")

draw_arrow(ax, 1.7, 4.2, 3.5, 3.2, "1. Gửi đơn Draft")
draw_arrow(ax, 5.0, 3.2, 5.0, 4.2, "2. Thông báo duyệt")
draw_arrow(ax, 5.0, 4.2, 6.0, 3.2, "3. Xác nhận duyệt")
draw_arrow(ax, 7.0, 3.2, 8.3, 4.2, "4. Đẩy vé KDS")
draw_arrow(ax, 8.3, 4.2, 7.5, 3.2, "5. Hoàn thành món")
draw_arrow(ax, 4.0, 2.8, 1.7, 4.2, "6. Cập nhật trạng thái")
draw_arrow(ax, 5.0, 1.8, 5.0, 1.2, "Đọc/Ghi dữ liệu")

fig.savefig('báo cáo/figures/fig_2_2_qr_kds_flow.png', bbox_inches='tight')
plt.close(fig)

# -------------------------------------------------------------
# 3. HÌNH 2.3: Luồng thanh toán VietQR & SePay Webhook
# -------------------------------------------------------------
fig, ax = plt.subplots(figsize=(10, 5.5), dpi=200)
ax.set_xlim(0, 10)
ax.set_ylim(0, 6)
ax.axis('off')

draw_box(ax, 0.5, 4.2, 2.5, 1.3, "Thu ngân (POS Client)", color='#0284c7', subtext="Bấm 'Thanh Toán' -> VietQR\nMở PosPaymentModal")
draw_box(ax, 3.8, 4.2, 2.4, 1.3, "Khách hàng", color='#16a34a', subtext="Quét mã VietQR động\nChuyển khoản Banking")
draw_box(ax, 7.1, 4.2, 2.4, 1.3, "Ngân hàng / SePay", color='#ea580c', subtext="Biến động số dư tài khoản\nBắn Webhook HTTP POST")

draw_box(ax, 3.0, 1.8, 4.0, 1.4, "WebAPI / PaymentsController", color='#7c3aed', subtext="Xác thực HMAC chữ ký số\nKiểm tra Idempotency (Trùng lặp)\nAtomic Transaction cập nhật Order & Payment")

draw_box(ax, 0.5, 1.8, 2.0, 1.4, "SignalR Hub", color='#0284c7', subtext="Broadcast:\nReceivePaymentCompleted")

draw_arrow(ax, 1.7, 4.2, 3.8, 2.8, "1. Yêu cầu tạo QR")
draw_arrow(ax, 4.2, 3.2, 5.0, 4.2, "2. Hiển thị mã QR")
draw_arrow(ax, 6.2, 4.8, 7.1, 4.8, "3. Chuyển khoản")
draw_arrow(ax, 8.3, 4.2, 6.5, 3.2, "4. Webhook payload")
draw_arrow(ax, 3.0, 2.5, 2.5, 2.5, "5. Kích hoạt thông báo")
draw_arrow(ax, 1.5, 3.2, 1.5, 4.2, "6. Đóng modal & in K80")

fig.savefig('báo cáo/figures/fig_2_3_payment_sepay_flow.png', bbox_inches='tight')
plt.close(fig)

# -------------------------------------------------------------
# 4. HÌNH 2.4: Vòng đời chế biến & Trừ kho BOM tự động
# -------------------------------------------------------------
fig, ax = plt.subplots(figsize=(10, 5.0), dpi=200)
ax.set_xlim(0, 10)
ax.set_ylim(0, 5)
ax.axis('off')

draw_box(ax, 0.5, 3.2, 2.0, 1.2, "1. Đặt món (POS/QR)", color='#64748b', subtext="OrderItem trạng thái\nKitchenStatus = Pending")
draw_box(ax, 3.0, 3.2, 2.0, 1.2, "2. Nhận nấu (KDS)", color='#d97706', subtext="KitchenStatus = Processing\nSentToKitchenAt = Now")
draw_box(ax, 5.5, 3.2, 2.0, 1.2, "3. Báo xong (KDS)", color='#16a34a', subtext="KitchenStatus = Done\nCompletedAt = Now")
draw_box(ax, 8.0, 3.2, 1.8, 1.2, "4. Phục vụ", color='#0284c7', subtext="Bưng món lên bàn\nĐơn chuyển Ready")

draw_box(ax, 4.5, 0.8, 4.0, 1.4, "Xử lý Trừ Tồn Kho BOM (InventoryService)", color='#ea580c', subtext="• Tra cứu công thức món (RecipeItems)\n• Tra cứu công thức Modifier (ModifierRecipeItems)\n• Giảm tồn Materials, ghi log InventoryTransactions")

draw_arrow(ax, 2.5, 3.8, 3.0, 3.8, "")
draw_arrow(ax, 5.0, 3.8, 5.5, 3.8, "")
draw_arrow(ax, 7.5, 3.8, 8.0, 3.8, "")
draw_arrow(ax, 6.5, 3.2, 6.5, 2.2, "Kích hoạt sự kiện hoàn thành")

fig.savefig('báo cáo/figures/fig_2_4_bom_lifecycle.png', bbox_inches='tight')
plt.close(fig)

# -------------------------------------------------------------
# 5. HÌNH 2.5: Quy trình Quản lý Ca, Tiền Két & Approval PIN
# -------------------------------------------------------------
fig, ax = plt.subplots(figsize=(10, 5.2), dpi=200)
ax.set_xlim(0, 10)
ax.set_ylim(0, 5)
ax.axis('off')

draw_box(ax, 0.5, 3.2, 2.5, 1.2, "Bắt đầu ca (Open Shift)", color='#0284c7', subtext="Chọn máy POS (Register)\nKhai báo tiền đầu ca (OpeningBalance)")
draw_box(ax, 3.7, 3.2, 2.6, 1.2, "Vận hành trong ca", color='#16a34a', subtext="Bán hàng, thu tiền mặt/QR\nNạp tiền lẻ (Till In)")
draw_box(ax, 7.0, 3.2, 2.6, 1.2, "Kết thúc ca (Close Shift)", color='#d97706', subtext="Đếm két tiền thực tế (ActualCash)\nIn báo cáo Z-Report K80")

draw_box(ax, 3.7, 0.6, 2.6, 1.4, "Nghiệp vụ Nhạy cảm", color='#dc2626', subtext="• Hủy món đã nấu (Void Item)\n• Hủy đơn hàng (Cancel Order)\n• Rút tiền két (Till Out > Hạn mức)")

draw_box(ax, 7.0, 0.6, 2.6, 1.4, "Xác thực Quản lý", color='#7c3aed', subtext="• Nhập mã PIN Quản lý BCrypt\n• Hoặc gửi phê duyệt ApprovalCenter\n• Khóa 15 phút nếu sai quá 5 lần")

draw_arrow(ax, 3.0, 3.8, 3.7, 3.8, "")
draw_arrow(ax, 6.3, 3.8, 7.0, 3.8, "")
draw_arrow(ax, 5.0, 3.2, 5.0, 2.0, "Yêu cầu nhạy cảm")
draw_arrow(ax, 6.3, 1.3, 7.0, 1.3, "Ủy quyền an toàn")

fig.savefig('báo cáo/figures/fig_2_5_shift_pin_workflow.png', bbox_inches='tight')
plt.close(fig)

print("Generated 5 flow diagrams successfully!")
