# BÁO CÁO KHẢO SÁT KIẾN TRÚC BACKEND (BACKEND SURVEY & GAP ANALYSIS REPORT)

> **Tác nhân:** `survey_explorer_backend` (Teamwork Preview Explorer)  
> **Thời điểm:** 2026-10-11T19:12:00Z  
> **Solution mục tiêu:** `src/Restaurant.sln`  
> **Các project kiểm tra:** `Restaurant.Domain`, `Restaurant.Application`, `Restaurant.Infrastructure`, `Restaurant.WebAPI`, `Restaurant.Domain.Tests`  
> **Phạm vi khảo sát:** Yêu cầu R1 (Security & Auth), R3 (Settings Persistence), R4 (SignalR & KDS), R5 (Inventory & BOM), R7 (QA & Security Testing)

---

## 1. OBSERVATION (QUAN SÁT THỰC TẾ TRÊN CODEBASE)

### 1.1. Yêu cầu R1: Bảo mật, Xác thực & Phân quyền

#### A. Hàm `VerifyManagerPinAsync` tại `IdentityService.cs`
- **Đường dẫn file:** `src/Restaurant.Infrastructure/Services/IdentityService.cs`
- **Dòng code:** L239 - L253
- **Chữ ký phương thức:** `public async Task<bool> VerifyManagerPinAsync(string pinCode)`
- **Đoạn code nguyên văn:**
  ```csharp
  public async Task<bool> VerifyManagerPinAsync(string pinCode)
  {
      if (string.IsNullOrWhiteSpace(pinCode)) return false;
      var employeeCodeWithPrefix = "NV" + pinCode;
      var user = await _context.Users
          .Include(u => u.Role)
          .FirstOrDefaultAsync(u => (u.EmployeeCode == pinCode || u.EmployeeCode == employeeCodeWithPrefix || u.Username == "admin" || u.Username == "manager01") && u.IsActive);

      if (user != null && (user.Role.Name == "Admin" || user.Role.Name == "Manager"))
      {
          return true;
      }

      return pinCode == "000000" || pinCode == "000003" || pinCode == "123456";
  }
  ```
- **Hiện trạng quan sát:**
  1. Biểu thức LINQ chứa `u.Username == "admin" || u.Username == "manager01"`. Do tài khoản admin luôn tồn tại trong DB, `user` luôn khác `null` và `user.Role.Name == "Admin"` luôn đúng bất kể giá trị `pinCode` là gì. Kết quả luôn trả về `true` cho mọi chuỗi PIN.
  2. Tồn tại mã PIN bypass cứng: `000000`, `000003`, `123456`.
  3. Không thực hiện so khớp mã băm mật khẩu `BCrypt.Net.BCrypt.Verify(pinCode, u.PinCodeHash)`.
  4. Không có cơ chế Rate Limiting / Lockout sau 5 lần nhập sai như trong `PinLoginAsync` (L121-L175).

#### B. Endpoint `verify-manager-pin` tại `AuthController.cs`
- **Đường dẫn file:** `src/Restaurant.WebAPI/Controllers/AuthController.cs`
- **Dòng code:** L132 - L144
- **Đoạn code nguyên văn:**
  ```csharp
  [HttpPost("verify-manager-pin")]
  [AllowAnonymous]
  public async Task<IActionResult> VerifyManagerPin([FromBody] VerifyPinRequest request)
  {
      var isValid = await _identityService.VerifyManagerPinAsync(request.PinCode);
      return Ok(new
      {
          success = isValid,
          data = new { isValid },
          error = isValid ? null : new { code = "INVALID_PIN", message = "Mã PIN Quản lý không đúng." },
          timestamp = DateTime.UtcNow
      });
  }
  ```
- **Hiện trạng quan sát:** Endpoint mở `[AllowAnonymous]`.

#### C. Các endpoint nhạy cảm bị gắn cờ `[AllowAnonymous]`
1. **`PosOrdersController.cs` (`src/Restaurant.WebAPI/Controllers/PosOrdersController.cs`):**
   - L107: `[HttpPost("orders/{id}/approve")]` `[AllowAnonymous]` (Duyệt đơn QR)
   - L136: `[HttpPost("tables/move")]` `[AllowAnonymous]` (Chuyển bàn)
   - L182: `[HttpPost("tables/merge")]` `[AllowAnonymous]` (Gộp bàn)
   - L211: `[HttpPost("orders/{id}/cancel")]` `[AllowAnonymous]` (Hủy đơn hàng)
   - L242: `[HttpPost("orders/{orderId}/items/{orderItemId}/void")]` `[AllowAnonymous]` (Hủy món)
   - L272: `[HttpPost("orders/submit")]` `[AllowAnonymous]` (Gửi đơn POS)
   - L317: `[HttpPost("orders/{id}/print-temp-bill")]` `[AllowAnonymous]` (In tạm tính)
   - L346: `[HttpPost("tables/{id}/clear")]` `[AllowAnonymous]` (Dọn bàn)
2. **`PaymentsController.cs` (`src/Restaurant.WebAPI/Controllers/PaymentsController.cs`):**
   - L65: `[HttpPost("cash")]` `[AllowAnonymous]` (Thanh toán tiền mặt)
   - L34: `[HttpPost("sepay-webhook")]` `[AllowAnonymous]` (Webhook bên thứ 3 xác thực qua `X-SePay-Signature` HMAC - hợp lệ theo kiến trúc nhưng cần kiểm tra chặt chẽ)
3. **`ShiftsController.cs` (`src/Restaurant.WebAPI/Controllers/ShiftsController.cs`):**
   - L10: `[AllowAnonymous]` gắn trên toàn bộ Controller. Ảnh hưởng: `GetCurrentShift` (L23), `OpenShift` (L53), `CloseShift` (L84), `GetAllShifts` (L117), `ReopenShift` (L131).
4. **`CashierToolsController.cs` (`src/Restaurant.WebAPI/Controllers/CashierToolsController.cs`):**
   - L10: `[AllowAnonymous]` gắn trên toàn bộ Controller. Ảnh hưởng: `PerformTillIn` (L25), `PerformTillOut` (L40), `PerformCashAdjustment` (L55), `GetShiftCashMovements` (L70), `GetAllCashMovements` (L84).
5. **`ApprovalCenterController.cs` (`src/Restaurant.WebAPI/Controllers/ApprovalCenterController.cs`):**
   - L10: `[AllowAnonymous]` gắn trên toàn bộ Controller. Ảnh hưởng: `GetPendingRequests` (L23), `GetRequestsHistory` (L37), `CreateRequest` (L51), `ApproveRequest` (L66), `RejectRequest` (L82), `ApproveByManagerPin` (L98).
6. **`ReconciliationController.cs` (`src/Restaurant.WebAPI/Controllers/ReconciliationController.cs`):**
   - L10: `[AllowAnonymous]` gắn trên toàn bộ Controller. Ảnh hưởng: `GetShiftReconciliationPreview` (L25), `SaveShiftReconciliation` (L39), `GetStoreSalesSummary` (L54).
7. **`VouchersController.cs` (`src/Restaurant.WebAPI/Controllers/VouchersController.cs`):**
   - L11: `[AllowAnonymous]` gắn trên toàn bộ Controller. Ảnh hưởng: `GetVouchers` (L128), `CreateVoucher` (L135), `UpdateVoucher` (L163), `DeleteVoucher` (L182), `ValidateVoucher` (L22).
8. **`DashboardController.cs` (`src/Restaurant.WebAPI/Controllers/DashboardController.cs`):**
   - L8-L46: Không có bất kỳ thuộc tính `[Authorize]` nào. Toàn bộ các API thống kê (`summary`, `sales-chart`, `top-movers`, `activity-feed`) mở tự do.

#### D. Fallback định danh `UserId = currentUserService.UserId ?? 1`
Tìm thấy 16 vị trí gán ngầm `UserId = 1` (Admin) khi `UserId` là `null`:
- `ApprovalCenterController.cs`: L55, L71, L87
- `InventoryController.cs`: L113, L291, L305
- `CashierToolsController.cs`: L29, L44, L59
- `PosOrdersController.cs`: L214, L245
- `ReconciliationController.cs`: L43
- `ShiftsController.cs`: L25, L55, L87, L135
- `KitchenService.cs`: L206 (`var actingUserId = _currentUserService?.UserId ?? 1;`)
- `InventoryService.cs`: L825 (`CreatedByUserId = orderItem.Order?.CreatedByUserId ?? 1;`)

---

### 1.2. Yêu cầu R3: Cấu hình Hệ thống (`SystemSettings`)

- **Đường dẫn file:** `src/Restaurant.WebAPI/Controllers/SystemController.cs`
- **Dòng code:** L16, L238 - L250
- **Đoạn code nguyên văn:**
  ```csharp
  private static SystemSettingsDto _currentSettings = new SystemSettingsDto();
  ...
  [HttpGet("settings")]
  public ActionResult GetSettings()
  {
      return Ok(new { success = true, data = _currentSettings });
  }

  [HttpPost("settings")]
  [Authorize(Roles = "Admin")]
  public ActionResult SaveSettings([FromBody] SystemSettingsDto settings)
  {
      _currentSettings = settings ?? new SystemSettingsDto();
      return Ok(new { success = true, data = _currentSettings, message = "Cập nhật cấu hình hệ thống thành công" });
  }
  ```
- **Hiện trạng quan sát trong Database & EF Core:**
  1. `Restaurant.Domain`: Không có entity `SystemSettings` (hoặc `RestaurantSetting`).
  2. `src/Restaurant.Infrastructure/Persistence/ApplicationDbContext.cs`: Có 42 DbSets, không có `DbSet<SystemSettings>`.
  3. `Restaurant.Application/DTOs/SystemDtos.cs` (L59-L72): Đã có định nghĩa `SystemSettingsDto` với 11 trường: `RestaurantName`, `Address`, `Phone`, `TaxNumber`, `DefaultVatPercent`, `ServiceChargePercent`, `CurrencySymbol`, `ReceiptHeader`, `ReceiptFooter`, `EnableAutoSendKitchen`, `EnableTableTimer`.
  4. Thư mục `src/Restaurant.Infrastructure/Migrations/`: Đã có lịch sử 8 migration và `ApplicationDbContextModelSnapshot.cs`.

---

### 1.3. Yêu cầu R4: SignalR & KDS Realtime

#### A. `RestaurantHub.cs` (`src/Restaurant.Infrastructure/Hubs/RestaurantHub.cs`)
- **Dòng code:** L8, L42 - L50
- **Đoạn code nguyên văn:**
  ```csharp
  [AllowAnonymous]
  public class RestaurantHub : Hub<IRestaurantHubClient>
  {
      ...
      var clientType = httpContext?.Request.Query["clientType"].ToString();
      if (clientType == "pos")
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, CashiersGroup);
      }
      else if (clientType == "kds")
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, ChefsGroup);
      }
  ```
- **Hiện trạng quan sát:**
  1. Hub được đánh dấu `[AllowAnonymous]`.
  2. Client tự ý truyền `?clientType=pos` hoặc `?clientType=kds` vào Query String để tự gia nhập nhóm `CashiersGroup` hoặc `ChefsGroup` mà không cần xác thực token JWT.
  3. `Restaurant.WebAPI/Program.cs` cấu hình `AddJwtBearer` (L86-L98) nhưng thiếu `JwtBearerEvents.OnMessageReceived` để đọc token từ `context.Request.Query["access_token"]` khi SignalR kết nối qua WebSockets/Long Polling.

#### B. Chuyển trạng thái đơn khi KDS hoàn thành (`KitchenService.cs`)
- **Dòng code:** L232 - L238
- **Đoạn code nguyên văn:**
  ```csharp
  var isAllDone = allItemsInOrder.All(oi => oi.KitchenStatus == (int)KitchenStatus.Done);
  if (isAllDone && item.Order.Status != (int)OrderStatus.Completed && item.Order.Status != (int)OrderStatus.Cancelled)
  {
      // Bếp hoàn thành toàn bộ -> Order.Status = Ready (Sẵn sàng phục vụ), KHÔNG tự động chuyển Completed trước thanh toán
      item.Order.Status = (int)OrderStatus.Ready;
      await _context.SaveChangesAsync();
  }
  ```
- **Hiện trạng quan sát:** Logic nghiệp vụ đã tuân thủ chính xác yêu cầu R4: khi tất cả món hoàn thành, đơn chuyển sang `Ready` và không tự ý chuyển thành `Paid` hay `Completed`.

#### C. Phát Domain Event trừ kho BOM 1 lần duy nhất
- **Dòng code tại `KitchenService.cs`:** L217 - L225
  ```csharp
  if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
  {
      bool isComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue;
      if (!isComboHeader)
      {
          await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
      }
  }
  ```
- **Dòng code tại `InventoryService.cs`:** L742 - L747
  ```csharp
  var alreadyDeducted = await _context.InventoryTransactions
      .AnyAsync(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId == orderItemId && t.TransactionType == (int)InventoryTransactionType.SaleDeduction);
      
  if (alreadyDeducted) return;
  ```
- **Hiện trạng quan sát:** Đã có 2 lớp bảo vệ chống trừ kho trùng lặp:
  1. Chỉ phát sự kiện khi `oldStatus != Done` và `newStatus == Done` (và không phải combo header).
  2. Handler kiểm tra sự tồn tại của giao dịch trừ kho trong bảng `InventoryTransactions` trước khi thực thi.

---

### 1.4. Yêu cầu R5: Quản lý Kho, BOM & Hủy đơn

#### A. Trừ kho BOM & Tồn kho âm (`InventoryService.cs`)
- **Dòng code:** L740 - L841 (`DeductInventoryForOrderItemAsync`)
- **Hiện trạng quan sát:**
  1. Tính toán định lượng nguyên vật liệu từ công thức món chính (`Recipes`) và công thức tùy chọn (`ModifierRecipes`).
  2. Dòng 814: `material.CurrentStock -= deductQty;`. Hệ thống cho phép tồn kho âm để đảm bảo không chặn hoạt động ra món của bếp trong giờ cao điểm.
  3. Dòng 829-832 & 837-840: Nếu `CurrentStock <= MinStockLevel`, hệ thống thu thập và phát cảnh báo tồn kho thấp qua SignalR (`_notificationService.NotifyLowStockWarningAsync`).

#### B. Nhập kho & Tính giá vốn bình quân gia quyền (`InventoryService.cs`)
- **Dòng code:** L121 - L190 (`CreatePurchaseOrderAsync` / Goods Receipt)
- **Hiện trạng quan sát:**
  - Dòng 158-164: Công thức tính giá vốn bình quân:  
    `NewCost = (OldStock * OldCost + NewQty * NewCost) / (OldStock + NewQty)`.
  - Tồn kho được cộng dồn chính xác (`material.CurrentStock += item.Quantity;`).
  - Ghi nhận `InventoryTransaction` loại `Import`.

#### C. Kiểm kê cân đối kho (`InventoryService.cs`)
- **Dòng code:** L192 - L256 (`AuditStockAsync`)
- **Hiện trạng quan sát:**
  - Gán `material.CurrentStock = item.ActualStock;`.
  - Tính chênh lệch `Difference = ActualStock - SystemQuantity` và `DifferenceValue = Difference * CostPerUnit`.
  - Ghi nhận `InventoryTransaction` loại `AuditAdjustment`.

#### D. Xử lý tồn kho khi hủy đơn (`CancelOrderService.cs`)
- **Dòng code:** L85 - L96 (`CancelOrderAsync`)
- **Đoạn code nguyên văn:**
  ```csharp
  var orderItemIds = order.OrderItems.Select(oi => oi.Id).ToList();
  var saleDeductions = await _context.InventoryTransactions
      .Where(t => t.ReferenceType == SystemConstants.ReferenceTypes.OrderItem && t.ReferenceId.HasValue && orderItemIds.Contains(t.ReferenceId.Value) && t.TransactionType == (int)InventoryTransactionType.SaleDeduction)
      .ToListAsync();

  foreach (var deduction in saleDeductions)
  {
      deduction.TransactionType = (int)InventoryTransactionType.CancellationWaste;
      var wasteText = string.Format(_localizer["CANCELLATION_WASTE_NOTE"] ?? "[WASTE] Cancelled Order {0} | Origin: {1}", order.OrderCode, deduction.Note);
      deduction.Note = wasteText;
  }
  ```
- **Hiện trạng quan sát:** Món đã nấu xong bị hủy không hoàn trả lại kho thô mà chuyển thành `CancellationWaste` (hao hụt hủy đơn). Điều này đúng với thực tế vận hành nhà hàng.
- **Khiếm khuyết phát hiện:** Trong `PosOrderService.VoidOrderItemAsync` (L864-L960), khi hủy món lẻ đã có trạng thái `KitchenStatus.Done`, hệ thống chỉ ghi `VoidLog` nhưng chưa cập nhật giao dịch trừ kho trước đó thành `CancellationWaste`.

---

### 1.5. Yêu cầu R7: Kiểm thử & Khảo sát Bộ Test Hiện có

- **Lệnh thực thi:** `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`
- **Kết quả thực tế:**
  ```text
  Passed!  - Failed: 0, Passed: 80, Skipped: 0, Total: 80, Duration: 16 s - Restaurant.Domain.Tests.dll (net9.0)
  ```
- **Cấu trúc test suite hiện tại:**
  - 14 test classes: `TableFloorplanSeederTests` (5), `PosReceiptPrintingTests` (8), `ComboBackendIntegrityTests` (17), `ComboEndToEndQaTests` (11), `CashierToolsAndApprovalWorkflowTests` (12), `SharedPinAuthenticationTests` (6), `KdsServicesTests` (4), `PosOrderServiceTests` (5), `AuditableEntityInterceptorTests` (3), `BohServicesTests` (3), `AuthorizationIntegrationTests` (2), `CancelOrderServiceTests` (1), `IdentityServiceTests` (2), `UnitTest1` (1).
- **Hạ tầng kiểm thử tích hợp:**
  - `AuthorizationIntegrationTests.cs` (L14-L24) đã thiết lập `IClassFixture<WebApplicationFactory<Program>>` và chạy trên InMemory database. Đây là nền tảng hoàn hảo để viết các bài test xác thực 401/403.
- **Những khoảng trống kiểm thử quan trọng:**
  1. Chưa có bài test nào kiểm tra hàm `VerifyManagerPinAsync` (đó là lý do bug P0 tồn tại mà 80 tests vẫn passed).
  2. Chưa có test 401 Unauthorized khi gửi request ẩn danh tới các endpoint nhạy cảm.
  3. Chưa có test 403 Forbidden khi Cashier cố gọi API của Admin/Manager.
  4. Chưa có bộ test độc lập dành riêng cho `InventoryService` (nhập kho, kiểm kê, định lượng trừ kho, cảnh báo hết hàng).

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

```mermaid
graph TD
    A[u.Username == 'admin' trong FirstOrDefaultAsync] -->|Luôn tìm thấy admin| B[VerifyManagerPinAsync luôn trả về true]
    B --> C[Bất kỳ ai nhập PIN gì cũng được duyệt quyền quản lý]
    
    D[Frontend Blazor SharedLogin chưa lưu JWT vào Session] -->|Cần chạy được tính năng| E[Developer gắn cờ AllowAnonymous hàng loạt Controller]
    E --> F[UserId ?? 1 gán mọi thao tác cho Admin]
    F --> G[Lỗ hổng P0 hủy đơn, rút két, sửa voucher không kiểm soát]
    
    H[SystemController dùng biến static _currentSettings] -->|Chưa có bảng SystemSettings| I[Mất cấu hình nhà hàng sau khi restart server]
    
    J[RestaurantHub [AllowAnonymous] + query clientType] -->|Bất kỳ kết nối nào| K[Tự ý vào CashiersGroup / ChefsGroup]
    
    L[80/80 Tests Passed nhưng không có test VerifyManagerPin / 401] -->|Thiếu độ bao phủ an ninh| M[Hệ thống chưa đủ điều kiện cho UAT/Production]
```

1. **Từ quan sát 1.1.A & 1.1.B:** LINQ query trong `VerifyManagerPinAsync` chứa điều kiện `u.Username == "admin"`. Do bản ghi admin luôn có trong DB, query luôn trả về User admin và biểu thức `(user.Role.Name == "Admin" || user.Role.Name == "Manager")` luôn trả về `true`. Hệ quả: Bất kỳ chuỗi PIN nào người dùng nhập vào cũng được hệ thống chấp nhận là Quản lý hợp lệ.
2. **Từ quan sát 1.1.C & 1.1.D:** Vì giao diện `SharedLogin` chưa truyền token JWT trong `HttpClient`, các lập trình viên đã gán `[AllowAnonymous]` trên các Controller nghiệp vụ chính (`PosOrders`, `Payments`, `Shifts`, `CashierTools`, `ApprovalCenter`, `Reconciliation`, `Vouchers`) và dùng fallback `currentUserService.UserId ?? 1`. Hệ quả: Bất kỳ ai trong mạng LAN đều có thể gọi API rút tiền két, hủy đơn hàng, mở/đóng ca và hệ thống tự động quy kết thao tác đó cho Admin.
3. **Từ quan sát 1.2:** Biến `_currentSettings` trong `SystemController` là biến static trong RAM. Khi ứng dụng restart, cấu hình nhà hàng (VAT, tên, địa chỉ, hotline) quay về giá trị mặc định. Bắt buộc phải tạo entity `SystemSettings`, thêm DbSet vào `ApplicationDbContext` và tạo EF Core migration.
4. **Từ quan sát 1.3.A:** `RestaurantHub` cho phép client tự tham gia `CashiersGroup` và `ChefsGroup` qua query string `clientType=pos` và `clientType=kds` mà không kiểm tra token. Đồng thời `Program.cs` chưa cấu hình `OnMessageReceived` để đọc JWT token từ WebSocket query string `access_token`. Khi đóng `[AllowAnonymous]`, cần cấu hình đúng sự kiện này.
5. **Từ quan sát 1.4 & 1.5:** Logic trừ kho 1 lần và chuyển đơn sang `Ready` đã chuẩn xác. Tuy nhiên `VoidOrderItemAsync` còn thiếu bước chuyển `SaleDeduction` sang `CancellationWaste`. Bộ test 80 tests hiện tại chỉ bao quát logic nghiệp vụ cơ bản, hoàn toàn thiếu test kiểm thử an ninh (401/403) và test độc lập cho `InventoryService`.

---

## 3. CAVEATS (GIẢ ĐỊNH & KHU VỰC CHƯA KHẢO SÁT)

1. **Giả định về SignalR QR Client:** Khách hàng bàn quét mã QR (`QrOrderStatus.razor`) hiện kết nối vào SignalR Hub để nghe trạng thái món tại bàn của mình (`TableGroup_{tableId}`). Nếu `RestaurantHub` áp dụng `[Authorize]`, khách hàng bàn cần được cấp một Guest Token ẩn danh ngắn hạn, HOẶC Hub cho phép kết nối ẩn danh nhưng các group `CashiersGroup`, `ChefsGroup`, `StationGroup` bắt buộc yêu cầu Claims xác thực (User.IsInRole), trong khi `TableGroup` kiểm tra mã bàn/token bàn.
2. **Khu vực Frontend Blazor:** Nhiệm vụ này tập trung vào backend (`Restaurant.Domain`, `Restaurant.Application`, `Restaurant.Infrastructure`, `Restaurant.WebAPI`, `Restaurant.Domain.Tests`). Luồng xử lý Cookie và circuit JWT tại `SharedLogin.razor.cs` thuộc phạm vi khảo sát frontend của agent phụ trách frontend.
3. **Phần cứng In nhiệt:** Khảo sát tập trung vào code backend. Kết nối cổng mạng LAN 9100 ESC/POS thuộc phạm vi module in ấn (R6).

---

## 4. CONCLUSION (KẾT LUẬN & KIẾN NGHỊ TRIỂN KHAI)

Hệ thống có nền tảng kiến trúc Clean Architecture rất tốt, logic nghiệp vụ KDS và trừ kho BOM đã được xây dựng bài bản. Tuy nhiên, **tồn tại 2 lỗ hổng an ninh cấp độ P0 (Critical)** và **3 khiếm khuyết độ tin cậy cấp độ P1/P2** cần được khắc phục theo lộ trình phân công sau:

### 4.1. Nhiệm vụ cho Agent 1 (Security & Auth - P0/P1)
1. **Sửa `IdentityService.VerifyManagerPinAsync`:**
   - Xóa `u.Username == "admin" || u.Username == "manager01"` khỏi LINQ.
   - Xóa hardcoded PINs (`000000`, `000003`, `123456`).
   - Lọc danh sách active user có `Role.Name` là "Admin" hoặc "Manager", kiểm tra bằng `BCrypt.Net.BCrypt.Verify(pinCode, u.PinCodeHash)`.
   - Bổ sung Rate Limiting (khóa sau 5 lần sai trong 15 phút) dùng dictionary `_managerPinAttempts`.
2. **Gỡ bỏ `[AllowAnonymous]` và áp dụng `[Authorize]`:**
   - `PosOrdersController`: Hủy đơn, Void món, Đổi bàn, Gộp bàn, Duyệt đơn QR -> `[Authorize(Roles = "Cashier,Manager,Admin")]`.
   - `PaymentsController`: `CashPayment` -> `[Authorize(Roles = "Cashier,Manager,Admin")]`.
   - `ShiftsController`: Thay `[AllowAnonymous]` bằng `[Authorize(Roles = "Cashier,Manager,Admin")]`.
   - `CashierToolsController`: Thay `[AllowAnonymous]` bằng `[Authorize(Roles = "Cashier,Manager,Admin")]`.
   - `ApprovalCenterController`: `[Authorize(Roles = "Cashier,Manager,Admin")]` cho xem/tạo yêu cầu; `[Authorize(Roles = "Manager,Admin")]` cho approve/reject.
   - `ReconciliationController`: `[Authorize(Roles = "Manager,Admin")]`.
   - `VouchersController`: CRUD Voucher -> `[Authorize(Roles = "Admin")]`.
   - `DashboardController`: Bổ sung `[Authorize(Roles = "Admin,Manager")]`.
3. **Xóa bỏ fallback `UserId ?? 1`:**
   - Thay thế toàn bộ bằng kiểm tra: `if (!_currentUserService.UserId.HasValue) return Unauthorized();`.

### 4.2. Nhiệm vụ cho Agent 3 (Admin & Reliability - P1)
1. Tạo Entity `SystemSettings` trong `Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs` (hoặc file riêng):
   ```csharp
   public class SystemSettings : AuditableEntity
   {
       public string RestaurantName { get; set; } = string.Empty;
       public string Address { get; set; } = string.Empty;
       public string Phone { get; set; } = string.Empty;
       public string TaxNumber { get; set; } = string.Empty;
       public decimal DefaultVatPercent { get; set; } = 8.0m;
       public decimal ServiceChargePercent { get; set; } = 5.0m;
       public string CurrencySymbol { get; set; } = "đ";
       public string ReceiptHeader { get; set; } = string.Empty;
       public string ReceiptFooter { get; set; } = string.Empty;
       public bool EnableAutoSendKitchen { get; set; } = true;
       public bool EnableTableTimer { get; set; } = true;
   }
   ```
2. Khai báo `DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();` trong `ApplicationDbContext.cs`.
3. Bổ sung seed dữ liệu mặc định vào `DbInitializer.cs`.
4. Tạo và áp dụng Migration: `AddSystemSettingsTable`.
5. Cập nhật `SystemController.cs` đọc và ghi trực tiếp từ `ApplicationDbContext.SystemSettings`.

### 4.3. Nhiệm vụ cho Agent 4 (KDS & Realtime - P1)
1. Trong `Restaurant.WebAPI/Program.cs`: Bổ sung `JwtBearerEvents.OnMessageReceived` đọc `context.Request.Query["access_token"]` cho đường dẫn `/hubs/restaurant`.
2. Trong `RestaurantHub.cs`: Xóa `httpContext?.Request.Query["clientType"]`. Phân quyền nhóm `CashiersGroup` và `ChefsGroup` thuần túy dựa trên Claims `ClaimTypes.Role` của token xác thực.

### 4.4. Nhiệm vụ cho Agent 5 (Inventory & BOM - P2)
1. Trong `PosOrderService.VoidOrderItemAsync`: Bổ sung xử lý khi món hủy đã có `KitchenStatus == Done`, tìm giao dịch `InventoryTransaction` loại `SaleDeduction` tương ứng và chuyển đổi sang `CancellationWaste`.
2. Giữ nguyên cơ chế cho phép tồn kho âm kèm thông báo cảnh báo `NotifyLowStockWarningAsync`.

### 4.5. Nhiệm vụ cho Agent 7 (QA & Security Testing - P0/P1/P2)
1. Thêm unit tests cho `IdentityService.VerifyManagerPinAsync` trong `IdentityServiceTests.cs`.
2. Thêm integration tests an ninh `SecurityIntegrationTests.cs` (kế thừa `IClassFixture<WebApplicationFactory<Program>>`) xác minh mã HTTP `401 Unauthorized` và `403 Forbidden`.
3. Thêm bộ test `InventoryServiceTests.cs` kiểm thử các luồng nhập kho, kiểm kê và trừ kho định lượng.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

### 5.1. Lệnh kiểm tra biên dịch
```powershell
dotnet build src/Restaurant.sln
```
*Tiêu chí đạt:* Build succeeded với **0 Error, 0 Warning**.

### 5.2. Lệnh chạy bộ kiểm thử hiện tại
```powershell
dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj
```
*Tiêu chí đạt:* **80/80 Tests Passed**.

### 5.3. Các file kiểm tra độc lập (Inspect Files)
1. `src/Restaurant.Infrastructure/Services/IdentityService.cs:239-253` (Kiểm tra xóa bỏ `u.Username == "admin"` và mã PIN hardcoded).
2. `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs` (Kiểm tra gỡ bỏ `[AllowAnonymous]`).
3. `src/Restaurant.WebAPI/Controllers/SystemController.cs:16` (Kiểm tra xóa biến static `_currentSettings`).
4. `src/Restaurant.Infrastructure/Hubs/RestaurantHub.cs:42-50` (Kiểm tra xóa bỏ `clientType`).
5. `src/Restaurant.Infrastructure/Persistence/ApplicationDbContext.cs` (Kiểm tra có `DbSet<SystemSettings>`).

### 5.4. Điều kiện vô hiệu hóa kết luận (Invalidation Conditions)
- Nếu sau khi gỡ `[AllowAnonymous]`, bất kỳ test nào trong 80 bài test cũ bị fail do thiếu token xác thực trong fixture, cần cập nhật fixture test để đính kèm Token phù hợp (như mẫu trong `AuthorizationIntegrationTests.cs`).
