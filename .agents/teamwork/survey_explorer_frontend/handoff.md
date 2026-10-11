# BÁO CÁO ĐIỀU TRA CHI TIẾT FRONTEND / BLAZOR (HANDOFF REPORT)

- **Người thực hiện**: `survey_explorer_frontend` (Teamwork Explorer Archetype)
- **Đối tượng báo cáo**: Parent Orchestrator (`a1e31680-d6b1-4328-b285-afb2f318f8f5`)
- **Mục tiêu**: Điều tra toàn diện hiện trạng mã nguồn UI/Blazor Server trong `src/Restaurant.Web` (Restaurant.sln), đối chiếu và định vị chính xác khoảng cách (gap analysis) theo các yêu cầu R1, R2, R3, R6.
- **Ngày hoàn thành**: 2026-10-11T19:20:00Z
- **Độ tin cậy & Chế độ**: Điều tra Read-Only (không trực tiếp sửa đổi mã nguồn sản phẩm).

---

## 1. OBSERVATION (Quan sát Trực tiếp & Bằng chứng Mã nguồn)

### 1.1. Yêu cầu R1: Chuỗi Xác thực (Auth Chain), Bearer Token & Cookie `RestaurantAuth`

#### Quan sát R1.1 — Cơ chế Đăng nhập PIN (`SharedLogin`) làm đứt gãy chuỗi xác thực
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Auth/SharedLogin/SharedLogin.razor.cs` (Dòng 95 - 120)
- **Mã nguồn thực tế**:
  ```csharp
  var response = await AuthApiClient.PinLoginAsync(new PinLoginRequest { PinCode = pinCode });

  if (response == null)
  {
      IsError = true;
      ErrorMessage = "Mã PIN không đúng, vui lòng thử lại.";
      pinCode = ""; // Auto clear on failure
  }
  else
  {
      AuthenticatedUserFullName = response.FullName;
      AuthenticatedRoleName = response.RoleName;

      if (string.Equals(response.DestinationArea, "BOTH", StringComparison.OrdinalIgnoreCase))
      {
          ShowAreaSelector = true;
      }
      else if (string.Equals(response.DestinationArea, "KDS", StringComparison.OrdinalIgnoreCase))
      {
          NavigationManager.NavigateTo("/kds");
      }
      else
      {
          NavigationManager.NavigateTo("/pos");
      }
  }
  ```
- **Hiện tượng**: `response.Token` (chứa JWT hợp lệ trả về từ WebAPI) bị **bỏ quên hoàn toàn**. `SharedLogin` không gửi token này sang `AccountController.Login`, không thực hiện thiết lập Cookie `RestaurantAuth`, không lưu token vào circuit state, và điều hướng bằng Blazor SPA navigation (`forceLoad = false`). Do đó, sau khi đăng nhập PIN, Blazor Circuit hoàn toàn là Anonymous (`User.Identity.IsAuthenticated == false`).

#### Quan sát R1.2 — `AccountController.cs` bị giới hạn cứng và thiếu hỗ trợ phân vùng POS/KDS
- **Tập tin**: `src/Restaurant.Web/Controllers/AccountController.cs` (Dòng 12 - 68)
- **Mã nguồn thực tế**:
  ```csharp
  [HttpGet("Login")]
  public async Task<IActionResult> Login([FromQuery] string token)
  {
      ...
      claims.Add(new Claim("jwt_token", token));
      ...
      await HttpContext.SignInAsync(
          CookieAuthenticationDefaults.AuthenticationScheme, 
          new ClaimsPrincipal(claimsIdentity), 
          authProperties);

      return Redirect("/admin/dashboard"); // CỐ ĐỊNH, KHÔNG CÓ returnUrl!
  }
  ```
- **Hiện tượng**: `AccountController.Login` chỉ hỗ trợ redirect cố định về `/admin/dashboard`. Khi `SharedLogin` chuyển hướng để set cookie cho nhân viên POS/KDS, action này thiếu query parameter `returnUrl` (hoặc `destinationArea`) để điều hướng về `/pos` hoặc `/kds`.

#### Quan sát R1.3 — Thiếu `DelegatingHandler` tự động đính kèm `Authorization: Bearer <token>`
- **Tập tin**: `src/Restaurant.Web/Program.cs` (Dòng 39 - 48)
- **Mã nguồn thực tế**:
  ```csharp
  // HttpClients
  builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri("http://localhost:5011/"));
  builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
  builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client => client.BaseAddress = new Uri("http://localhost:5011/"));

  builder.Services.AddScoped<DashboardApiClient>();
  builder.Services.AddScoped<MenuApiClient>();
  builder.Services.AddScoped<TableApiClient>();
  builder.Services.AddHttpClient<AdminApiClient>(client => client.BaseAddress = new Uri("http://localhost:5011/"));
  ```
- **Hiện tượng**:
  1. Các HttpClient typed như `AdminApiClient`, `DashboardApiClient`, `MenuApiClient`, `TableApiClient` đang phải tự gọi phương thức private `SetAuthorizationHeaderAsync()` thủ công trước mỗi request (ví dụ: `AdminApiClient.cs:25-42`, `DashboardApiClient.cs:19-39`, `MenuApiClient.cs:19-35`, `TableApiClient.cs:20-35`).
  2. Rất nhiều Razor components trực tiếp inject `HttpClient Http` (lấy từ scoped client `"Api"`) nhưng `"Api"` không hề có `DelegatingHandler` để tự gắn Bearer token.
  3. Cụ thể các file inject `HttpClient Http` trực tiếp và gọi endpoint backend không kèm token:
     - `Components/Pages/Admin/Approvals/ApprovalCenter.razor:5` (gọi `api/v1/approvals/*`)
     - `Components/Pages/Pos/Components/PosPaymentModal.razor:4` (gọi `api/v1/payments/cash`, `api/v1/payments/generate-vietqr`)
     - `Components/Pages/Pos/Components/PosVoidItemModal.razor:4` (gọi void item API)
     - `Components/Pages/Pos/Components/PosTableActionModal.razor:3` (gọi move/merge table API)
     - `Components/Pages/Pos/Components/PosCashierToolsModal.razor:3` (gọi `api/v1/shifts/*`, `api/v1/cashier-tools/*`)
     - `Components/Pages/Admin/Shifts/ShiftManagement.razor:5` (gọi shifts API)
     - `Components/Pages/Admin/Reconciliation/PaymentReconciliation.razor:5` (gọi reconciliation API)
     - `Components/Pages/Pos/PosTables.razor:11` (gọi tables & orders API)
     - `Components/Pages/Pos/PosMain/PosMain.razor.cs:14, 976` (gọi `api/v1/pos/orders/{orderId}/cancel`)
     - `Components/Pages/Kds/KdsMain/KdsDashboard.razor.cs:11, 165` (gọi `api/v1/kds/items/{orderItemId}/status`)
  Khi các endpoint này được bảo vệ bằng `[Authorize]` ở Backend (theo R1), tất cả các cuộc gọi trên sẽ lập tức bị lỗi `401 Unauthorized`.

#### Quan sát R1.4 — Dữ liệu cứng Mock Nhân viên & PIN Bypass
- **Tập tin**: `src/Restaurant.Web/Services/MockIdentityService.cs` (Dòng 52 - 55)
  - `public Task<bool> VerifyManagerPinAsync(string pinCode) => Task.FromResult(pinCode == "000000" || pinCode == "000003" || pinCode == "123456");`
- **Tập tin**: `src/Restaurant.Web/Components/Layout/PosHeader.razor` (Dòng 129 - 130)
  - Tên thu ngân hiển thị bị hardcode tĩnh: `<span class="text-xs font-bold leading-tight">Lê Thị Thu</span>`, avatar hardcode `LT`. Không đọc từ danh tính thực tế của `AuthenticationState`.

---

### 1.2. Yêu cầu R2: Hoàn thiện Luồng Thanh Toán POS & Nghiệp vụ Quầy Thu Ngân

#### Quan sát R2.1 — Nút "Thanh Toán" bị thiếu trên UI của `PosMain.razor`
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor` (Dòng 48 - 101)
- **Mã nguồn thực tế (Header Toolbar)**:
  - Chỉ có 5 nút:
    1. `ShowMemberModal = true` (Thành Viên - Dòng 51)
    2. `ShowDiscountModal = true` (Giảm Giá - Dòng 58)
    3. `HandlePrintPreBill` (In Hóa Đơn - Dòng 65)
    4. `ShowCancelOrderModal = true` (Hủy Đơn - Dòng 80)
    5. `HandleDoneAsync` (Xác Nhận & Gửi Bếp - Dòng 88)
  - **Không hề có nút "Thanh Toán"** trên thanh công cụ này!

#### Quan sát R2.2 — Nút "Thanh Toán" bị thiếu ở chân giỏ hàng `OrderCart.razor`
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor` (Dòng 155 - 167)
- **Mã nguồn thực tế**:
  ```razor
  <!-- Actions -->
  @if (Items != null && Items.Any())
  {
      <div class="pos-cart__actions p-3 bg-surface border-t border-outline-variant">
          <button class="w-full py-2 bg-error/10 text-error hover:bg-error hover:text-white rounded-xl font-extrabold text-sm flex items-center justify-center gap-1.5 shadow-sm transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  disabled="@(SelectedItem == null || _isProcessing)" 
                  @onclick="HandleGlobalDeleteAsync">
              <span class="material-symbols-outlined text-lg">delete</span>
              <span>Xóa Món Đang Chọn</span>
          </button>
      </div>
  }
  ```
- **Hiện tượng**: Dưới chân giỏ hàng chỉ có duy nhất nút `Xóa Món Đang Chọn`. Không có nút `Thanh Toán` để thu ngân bấm trực tiếp khi đơn đã có món.

#### Quan sát R2.3 — Logic Code-behind đã có sẵn nhưng bị "ngắt kết nối" với Razor markup
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs` (Dòng 110, 1077 - 1113)
- **Mã nguồn thực tế**:
  ```csharp
  protected bool ShowPaymentModal { get; set; } = false;

  protected async Task HandlePayAsync()
  {
      if (!CartItems.Any()) return;

      if (ActiveOrderId == 0 || CartItems.Any(i => !i.IsSentToKitchen))
      {
          await HandleSendToKitchenAsync();
      }

      if (ActiveOrderId > 0)
      {
          ShowPaymentModal = true;
      }
  }

  protected async Task HandlePaymentCompletedAsync(bool isPaid)
  {
      if (isPaid)
      {
          try
          {
              var response = await Http.GetFromJsonAsync<ApiResponseWrapper<K80ReceiptDto>>($"api/v1/bills/{ActiveOrderId}/k80");
              if (response?.Data != null)
              {
                  ReceiptData = response.Data;
                  ShowK80Modal = true;
              }
          }
          catch { }

          CartItems.Clear();
          ToastMessage = "Thanh toán thành công! Có thể in hóa đơn.";
      }
  }
  ```
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor.cs` (Dòng 37, 105 - 119)
  - Đã khai báo: `[Parameter] public EventCallback OnPay { get; set; }`
  - Đã viết sẵn: `private async Task HandlePayAsync() { if (OnPay.HasDelegate) await OnPay.InvokeAsync(); }`
- **Khoảng cách (Gap)**:
  1. Trong `PosMain.razor`, thẻ `<OrderCart ... />` (dòng 107-122) **không truyền tham số `OnPay="HandlePayAsync"`**.
  2. Trong `PosMain.razor` (dòng 346-381), **không hề khai báo component `<PosPaymentModal ... />`** (trong khi các modal khác như `PosMemberModal`, `PosDiscountModal`, `PosCancelOrderModal`, `K80ReceiptModal` đều được khai báo đầy đủ).

#### Quan sát R2.4 — Modal Thanh Toán `PosPaymentModal.razor`
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/Components/PosPaymentModal.razor`
- **Mã nguồn thực tế**:
  - Hỗ trợ đầy đủ 2 tab: `Tiền Mặt` (`ProcessCashPaymentRequest` gửi tới `api/v1/payments/cash`) và `Chuyển Khoản VietQR` (`GenerateVietQrRequest` gửi tới `api/v1/payments/generate-vietqr`).
  - Lắng nghe SignalR Hub `"OrderPaid"` để tự động hoàn tất khi SePay webhook kích hoạt.
  - Tích hợp in tạm tính `PrintTempBillAsync` gọi tới `api/v1/pos/orders/{OrderId}/print-temp-bill`.
  - **Khiếm khuyết**: `_hubConnection` (dòng 250-254) kết nối Hub không truyền JWT token (`.WithUrl("http://localhost:5011/hubs/restaurant")`).

#### Quan sát R2.5 — Tính toàn vẹn của Phiếu In Tạm Tính K80 (`K80ReceiptModal.razor`)
- **Tập tin**: `src/Restaurant.Web/Components/Pages/Pos/Components/K80ReceiptModal.razor`
- **Mã nguồn thực tế**:
  - Dòng 13: `<span>@(OrderData.IsPaid ? "Hóa Đơn Thanh Toán (K80 - 80mm)" : "Phiếu Tạm Tính Pre-Bill (K80 - 80mm)")</span>`
  - Dòng 58-63: Nếu `!OrderData.IsPaid`, hiển thị dòng cảnh báo `(Phiếu đối chiếu món - Chưa thanh toán)`.
  - Dòng 326-329: `PrintReceiptAsync()` chỉ gọi `await JS.InvokeVoidAsync("window.print")`.
  - **Đánh giá**: Hoàn toàn tuân thủ yêu cầu R2 — việc in tạm tính không tự ý đánh dấu đơn là Paid.

---

### 1.3. Yêu cầu R3: Xóa Dữ liệu Mock Tĩnh trong `AdminApiClient` & Bổ sung Alert/Toast UI

#### Quan sát R3.1 — Danh sách phương thức Mock tĩnh cần xóa bỏ trong `AdminApiClient.cs`
- **Tập tin**: `src/Restaurant.Web/Services/AdminApiClient.cs`
- **Các hàm mock tĩnh công khai/nội bộ**:
  1. `GetMaterialsAsync()` (dòng 55-71) -> fallback về `GetMockMaterials()` (dòng 651-658) khi gặp ngoại lệ.
  2. `GetSuppliersAsync()` (dòng 99-115) -> fallback về `GetMockSuppliers()` (dòng 660-664) khi gặp ngoại lệ.
  3. `GetUsersAsync()` (dòng 334-347) -> fallback về `GetMockUsers()` (dòng 666-672) khi gặp ngoại lệ.
  4. `GetOrderHistoryAsync()` (dòng 368-405) -> fallback về `GetMockOrders()` (dòng 674-679) khi gặp ngoại lệ.
  5. `GetCustomersAsync()` (dòng 425-438) -> fallback về `GetMockCustomers()` (dòng 681-686) khi gặp ngoại lệ.
  6. `GetVouchersAsync()` (dòng 443-472) -> fallback về `GetMockVouchers()` (dòng 688-692) khi gặp ngoại lệ.
  7. `GetRolesAsync()` (dòng 509-516) -> hardcoded fallback danh sách 4 RoleDto.
  8. `GetRegistersAsync()` (dòng 530-535) -> hardcoded fallback 2 máy POS.
  9. `GetKitchenStationsAsync()` (dòng 563-568) -> hardcoded fallback 3 trạm bếp.
  10. `GetAuditLogsAsync()` (dòng 598-600) -> hardcoded fallback 1 bản ghi AuditLogDto.

#### Quan sát R3.2 — Hiện trạng xử lý lỗi trên các trang Quản trị (Admin Razor Pages)
- **`MaterialManagement.razor`** (dòng 272-274): `catch (Exception ex) { Console.WriteLine(...) }` -> Lỗi mạng bị giấu nhẹm vào Console, không hiển thị bất kỳ thông báo Toast hay Alert nào lên màn hình.
- **`SupplierManagement.razor`** (dòng 175-182): `LoadData` chỉ có `try ... finally`, không có `catch`. Nếu `AdminApiClient` throw exception thì màn hình có nguy cơ crash circuit.
- **`OrderHistory.razor`** (dòng 366-374): `LoadData` chỉ có `try ... finally`, không có `catch` hay alert banner.
- **`CustomerManagement.razor`** (dòng 15-21, 76, 93): Đã có markup hiển thị alert banner `@if (!string.IsNullOrEmpty(_errorMessage))`, nhưng do `AdminApiClient.GetCustomersAsync()` nuốt lỗi và trả về Mock nên alert này chưa bao giờ được kích hoạt.
- **`VoucherManagement.razor`** (dòng 154-161): Chỉ có `try ... finally`, không có thông báo lỗi UI.
- **Component dùng chung**: `src/Restaurant.Web/Components/UI/Feedback/AppToast.razor` đã được dựng sẵn (hỗ trợ các type `Success`, `Error`, `Info`, Material styling) nhưng **chưa từng được gọi ở bất cứ trang nào**.

#### Quan sát R3.3 — Cấu hình Hệ thống (`SystemSettings`)
- **Tập tin Backend**: `src/Restaurant.WebAPI/Controllers/SystemController.cs` (Dòng 16, 238-250)
  - `private static SystemSettingsDto _currentSettings = new SystemSettingsDto();`
  - Biến tĩnh lưu trên RAM nên restart server sẽ mất trắng cấu hình.
- **Tập tin Frontend**: `src/Restaurant.Web/Components/Pages/Admin/SystemAdmin/SystemSettings.razor`
  - Form UI đã hoàn thiện đầy đủ các trường (Tên nhà hàng, Địa chỉ, Hotline, MST, VAT %, Phí DV %, Ký hiệu tiền tệ, AutoSendKitchen, Header/Footer K80).
  - Đã kết nối gọi `ApiClient.GetSystemSettingsAsync()` và `ApiClient.SaveSystemSettingsAsync(_settings)`. Frontend hoàn toàn tương thích khi Backend chuyển sang lưu DB.

---

### 1.4. Yêu cầu R6: Tính năng Tách Hóa Đơn (Split Bill POS-008) & Cấu hình In Nhiệt ESC/POS

#### Quan sát R6.1 — Tính năng Tách Hóa Đơn (Split Bill POS-008)
- **Tình trạng hiện tại**: **CHƯA ĐƯỢC TRIỂN KHAI (NOT STARTED)** trong toàn bộ codebase (xác nhận bởi `docs/audit/FULL_WORKSPACE_AUDIT.md:48, 75`).
- **Hiện trạng UI**:
  - `PosMain.razor`: Chưa có nút kích hoạt Tách đơn.
  - `PosTables.razor` (dòng 150-162): Chỉ có nút `Chuyển Bàn` (`OpenMoveModal`) và `Gộp Bàn` (`OpenMergeModal`), chưa có nút `Tách Bàn / Tách Đơn`.
  - `PosTableActionModal.razor`: Chỉ hỗ trợ `ActionType == "Move"` hoặc `"Merge"`, không hỗ trợ tách món.
  - Chưa tồn tại component modal cho phép chọn món, chọn số lượng muốn chuyển sang đơn mới và xem tính toán lại Subtotal, VAT 8% của cả 2 đơn.

#### Quan sát R6.2 — In Nhiệt K80 ESC/POS Mạng LAN (IP:Port 9100)
- **Tình trạng hiện tại**:
  - Toàn bộ việc in phiếu K80 hiện tại đều phụ thuộc vào trình duyệt qua Javascript `window.print` (`K80ReceiptModal.razor:326-329`, `ReceiptTestPage.razor:30`).
  - Chưa có bất kỳ Interface hoặc Service Adapter nào (`IEscPosPrinterService`, `NetworkPrinterAdapter`) để gửi ESC/POS raw command (byte stream qua TCP socket port 9100) tới máy in nhiệt cứng IP LAN.
  - `SystemSettings.razor` chưa có ô cấu hình IP máy in quầy thu ngân (ví dụ `192.168.1.200:9100`) và nút "In test máy in LAN".

---

## 2. LOGIC CHAIN (Chuỗi Suy Luận Từ Quan Sát Đến Kết Luận)

```
[Quan sát R1.1] SharedLogin bỏ qua response.Token, không gọi AccountController, không set cookie
         │
         ▼
[Logic L1.1] Blazor Circuit ở trạng thái Anonymous; CascadingAuthenticationState không có Claims
         │
         ▼
[Quan sát R1.3] Program.cs không cấu hình DelegatingHandler gắn Bearer token cho HttpClient
         │
         ▼
[Logic L1.2] Mọi lời gọi API từ Blazor (cả HttpClient trực tiếp lẫn typed ApiClients) gửi đi KHÔNG kèm Bearer token
         │
         ▼
[Hệ quả C1] Khi Backend đóng các endpoint nhạy cảm bằng [Authorize] (R1), toàn bộ UI POS, KDS và Admin sẽ bị lỗi 401 Unauthorized ngay lập tức.
```

```
[Quan sát R2.3] PosMain.razor.cs đã có sẵn HandlePayAsync(), ShowPaymentModal, HandlePaymentCompletedAsync()
[Quan sát R2.3] OrderCart.razor.cs đã có sẵn [Parameter] OnPay và HandlePayAsync()
         │
         ▼
[Quan sát R2.1, R2.2] PosMain.razor và OrderCart.razor chưa đặt nút button trên markup, và chưa khai báo <PosPaymentModal>
         │
         ▼
[Logic L2.1] Tính năng thanh toán tại quầy không bị hỏng về mặt thuật toán, mà bị "mất liên kết" ở tầng biểu diễn (presentation gap).
         │
         ▼
[Hệ quả C2] Chỉ cần bổ sung nút bấm tại Toolbar / Footer Cart và khai báo <PosPaymentModal> là hoàn thiện 100% luồng thanh toán POS.
```

```
[Quan sát R3.1] AdminApiClient nuốt lỗi ngoại lệ và trả về 6 hàm mock tĩnh GetMock*()
         │
         ▼
[Quan sát R3.2] Các trang Admin (Material, Supplier, OrderHistory, Voucher) không có Alert/Toast hiển thị lỗi mạng
         │
         ▼
[Logic L3.1] Khi server gặp sự cố hoặc mất mạng, người quản trị bị "đánh lừa" rằng dữ liệu vẫn bình thường do đang xem dữ liệu giả.
         │
         ▼
[Hệ quả C3] Cần xóa bỏ hoàn toàn 6 hàm GetMock*() và các inline mock; cho phép ném lỗi hoặc trả lỗi chuẩn mực, đồng thời tích hợp AppToast / Error Banner vào từng trang quản trị.
```

```
[Quan sát R6.1, R6.2] Không có bất kỳ dòng code nào cho POS-008 Split Bill và ESC/POS LAN Port 9100
         │
         ▼
[Logic L6.1] R6 là yêu cầu mở rộng tính năng mới (New Feature Implementation), đòi hỏi thiết kế trọn gói từ UI Modal đến Service Adapter.
```

---

## 3. CAVEATS (Vấn đề Cần Lưu ý & Phạm vi Chưa Khảo sát)

1. **Bảo toàn Sơ đồ Bàn 34 Bàn (Floorplan Baseline)**:
   - Nghiêm cấm mọi sửa đổi vào tọa độ (X, Y), kích thước (Width, Height), AreaId và tên 34 bàn tại tầng 1 trong `TableManagement.razor`, `AreaManagement.razor`, và `TableFloorplanSeederTests.cs`.
2. **Khách hàng QR Gọi Món (Anonymous QR Session)**:
   - Các trang `QrMenu.razor` và `QrOrderStatus.razor` dành cho thực khách quét mã QR tại bàn không có tài khoản nhân viên. Khi xây dựng `DelegatingHandler` tự động đính kèm token, phải xử lý trường hợp không có token (Anonymous) thì không chặn request hoặc gây crash, mà gửi request nguyên bản cho các endpoint public.
3. **Môi trường Phần cứng In Nhiệt Thực tế**:
   - Do kiểm thử trong môi trường container/máy phát triển không có máy in nhiệt LAN thật cắm vào cổng 9100, lớp trừu tượng `IEscPosPrinterService` cần có chế độ Mock/Emulation hoặc timeout ngắn (1-2s) với thông báo lỗi thân thiện khi không kết nối được IP đích, song song với việc giữ nguyên `window.print` của `K80ReceiptModal`.

---

## 4. CONCLUSION (Kết Luận & Giải Pháp Đề Xuất Chi Tiết Cho Các Agent Thực Thi)

### 4.1. Đề xuất cho Agent 1 (Security & Auth): Khôi phục Chuỗi Xác thực Blazor -> WebAPI
1. **Cập nhật `AccountController.cs`**:
   - Bổ sung tham số `returnUrl`:
     ```csharp
     [HttpGet("Login")]
     public async Task<IActionResult> Login([FromQuery] string token, [FromQuery] string? returnUrl = null)
     {
         ...
         var redirectPath = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) 
             ? returnUrl 
             : "/admin/dashboard";
         return Redirect(redirectPath);
     }
     ```
2. **Cập nhật `SharedLogin.razor.cs`**:
   - Khi `PinLoginAsync` thành công, lưu token và redirect qua `AccountController` với `forceLoad: true`:
     ```csharp
     if (string.Equals(response.DestinationArea, "BOTH", StringComparison.OrdinalIgnoreCase))
     {
         _pendingToken = response.Token;
         ShowAreaSelector = true;
     }
     else
     {
         var targetPath = string.Equals(response.DestinationArea, "KDS", StringComparison.OrdinalIgnoreCase) ? "/kds" : "/pos";
         NavigationManager.NavigateTo($"/Account/Login?token={response.Token}&returnUrl={Uri.EscapeDataString(targetPath)}", forceLoad: true);
     }
     ```
   - Trong `SelectArea(string path)`:
     ```csharp
     NavigationManager.NavigateTo($"/Account/Login?token={_pendingToken}&returnUrl={Uri.EscapeDataString(path)}", forceLoad: true);
     ```
3. **Tạo `BlazorAuthorizationMessageHandler.cs` trong `Restaurant.Web/Services`**:
   - Đăng ký làm `DelegatingHandler` cho `IHttpClientFactory`:
     ```csharp
     public class BlazorAuthorizationMessageHandler : DelegatingHandler
     {
         private readonly AuthenticationStateProvider _authStateProvider;
         public BlazorAuthorizationMessageHandler(AuthenticationStateProvider authStateProvider)
         {
             _authStateProvider = authStateProvider;
         }
         protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
         {
             try
             {
                 var authState = await _authStateProvider.GetAuthenticationStateAsync();
                 var token = authState.User.FindFirst("jwt_token")?.Value;
                 if (!string.IsNullOrEmpty(token))
                 {
                     request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                 }
             }
             catch { }
             return await base.SendAsync(request, cancellationToken);
         }
     }
     ```
   - Đăng ký vào `Program.cs`:
     ```csharp
     builder.Services.AddScoped<BlazorAuthorizationMessageHandler>();
     builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri("http://localhost:5011/"))
         .AddHttpMessageHandler<BlazorAuthorizationMessageHandler>();
     ```
4. **Cập nhật `PosHeader.razor`**:
   - Đọc tên và vai trò nhân viên từ `AuthenticationStateProvider` thay vì hardcode "Lê Thị Thu".

---

### 4.2. Đề xuất cho Agent 2 (POS & Cashier): Kết nối Hoàn Chỉnh Giao Diện Thanh Toán
1. **Bổ sung nút "Thanh Toán" trong `PosMain.razor` (Header Toolbar)**:
   - Thêm nút kế bên nút "In Hóa Đơn":
     ```razor
     <button class="px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-black text-sm flex items-center gap-2 shadow-sm transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
             disabled="@(CartItems == null || !CartItems.Any() || IsProcessing)"
             @onclick="HandlePayAsync">
         <span class="material-symbols-outlined text-[20px]">payments</span>
         <span>Thanh Toán</span>
     </button>
     ```
2. **Khai báo `<PosPaymentModal>` trong `PosMain.razor`**:
   - Thêm vào cuối `PosMain.razor`:
     ```razor
     <PosPaymentModal
         @bind-IsOpen="ShowPaymentModal"
         OrderId="@ActiveOrderId"
         OrderCode="@CurrentOrderCode"
         TableName="@CurrentTableName"
         SubTotal="@CartSubTotal"
         AppliedDiscount="@AppliedDiscountAmount"
         CustomerId="@SelectedMember?.Id"
         CustomerPhone="@SelectedMember?.PhoneNumber"
         OnPaymentCompleted="HandlePaymentCompletedAsync" />
     ```
3. **Bổ sung nút "Thanh Toán" vào chân giỏ hàng `OrderCart.razor`**:
   - Thêm nút vào khối `pos-cart__actions`:
     ```razor
     <div class="pos-cart__actions p-3 bg-surface border-t border-outline-variant space-y-2">
         <button class="w-full py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-black text-sm flex items-center justify-center gap-2 shadow-sm transition-all cursor-pointer disabled:opacity-50"
                 disabled="@(!Items.Any() || _isProcessing)"
                 @onclick="HandlePayAsync">
             <span class="material-symbols-outlined text-lg">payments</span>
             <span>Thanh Toán (@Total.ToString("N0")đ)</span>
         </button>
         @if (SelectedItem != null)
         {
             <button class="w-full py-1.5 bg-error/10 text-error hover:bg-error hover:text-white rounded-xl font-bold text-xs flex items-center justify-center gap-1.5 transition-all cursor-pointer"
                     @onclick="HandleGlobalDeleteAsync">
                 <span class="material-symbols-outlined text-sm">delete</span>
                 <span>Xóa món đang chọn</span>
             </button>
         }
     </div>
     ```
   - Trong `PosMain.razor`: truyền tham số `OnPay="HandlePayAsync"` vào component `<OrderCart ... />`.
4. **Cập nhật `PosPaymentModal.razor`**:
   - Bổ sung access token cho HubConnection:
     ```csharp
     _hubConnection = new HubConnectionBuilder()
         .WithUrl("http://localhost:5011/hubs/restaurant", options =>
         {
             options.AccessTokenProvider = async () =>
             {
                 var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                 return authState.User.FindFirst("jwt_token")?.Value;
             };
         })
         .WithAutomaticReconnect()
         .Build();
     ```

---

### 4.3. Đề xuất cho Agent 3 (Admin & Reliability): Gỡ bỏ Mock & Bổ sung Alert Banner
1. **Dọn sạch `AdminApiClient.cs`**:
   - Xóa bỏ 6 hàm fallback `GetMockMaterials()`, `GetMockSuppliers()`, `GetMockUsers()`, `GetMockOrders()`, `GetMockCustomers()`, `GetMockVouchers()`.
   - Trong các hàm `GetMaterialsAsync`, `GetSuppliersAsync`, `GetUsersAsync`, `GetOrderHistoryAsync`, `GetCustomersAsync`, `GetVouchersAsync`: khi `_httpClient` gặp lỗi hoặc trả status code không thành công, ném ngoại lệ hoặc trả về danh sách rỗng kèm logging rõ ràng; không trả dữ liệu giả.
   - Xóa bỏ các hardcoded role, register, station, audit log trong `GetRolesAsync`, `GetRegistersAsync`, `GetKitchenStationsAsync`, `GetAuditLogsAsync`.
2. **Cập nhật các trang Admin**:
   - `MaterialManagement.razor`, `SupplierManagement.razor`, `OrderHistory.razor`, `VoucherManagement.razor`:
     Bổ sung biến `_errorMessage` và hiển thị Alert Box màu đỏ khi tải dữ liệu thất bại:
     ```razor
     @if (!string.IsNullOrEmpty(_errorMessage))
     {
         <div class="p-4 mb-6 bg-red-50 dark:bg-red-950/40 border border-red-300 dark:border-red-800 rounded-xl text-red-800 dark:text-red-300 text-xs font-bold flex items-center justify-between shadow-2xs">
             <div class="flex items-center gap-2">
                 <span class="material-symbols-outlined text-lg">error</span>
                 <span>@_errorMessage</span>
             </div>
             <button class="hover:bg-red-200/50 p-1 rounded-lg" @onclick="() => _errorMessage = null">✕</button>
         </div>
     }
     ```

---

### 4.4. Đề xuất cho Agent 6 (Split Bill & Printing): Xây dựng UI Tách Đơn & ESC/POS
1. **Xây dựng component `PosSplitBillModal.razor`**:
   - Vị trí: `src/Restaurant.Web/Components/Pages/Pos/Components/PosSplitBillModal.razor`
   - Giao diện 2 cột:
     - Cột trái: Đơn hiện tại (Món ăn, số lượng gốc, số lượng giữ lại).
     - Nút chuyển món qua lại (`>` và `<`).
     - Cột phải: Đơn tách mới (Món ăn được chuyển, số lượng tách).
     - Tính toán lại Subtotal, VAT 8%, Tổng cộng tức thời cho cả 2 đơn.
   - Nút "Xác Nhận Tách Hóa Đơn": gửi payload `SplitOrderRequest { SourceOrderId, TargetItems = [...] }` tới endpoint Backend.
   - Thêm nút mở modal này vào toolbar của `PosMain.razor` và menu thao tác của `PosTables.razor`.
2. **Thiết kế Abstraction In Nhiệt ESC/POS Mạng LAN**:
   - Tạo `IEscPosPrinterService` trong `Restaurant.Application/Interfaces/`:
     ```csharp
     public interface IEscPosPrinterService
     {
         Task<bool> PrintReceiptAsync(K80ReceiptDto receipt, string ipAddress, int port = 9100);
         Task<bool> TestPrinterConnectionAsync(string ipAddress, int port = 9100);
     }
     ```
   - Tạo `EscPosNetworkPrinterService` trong `Restaurant.Infrastructure/Services/`:
     Sử dụng `TcpClient` mở kết nối tới `ipAddress:port`, gửi lệnh ESC/POS chuẩn (khởi tạo, căn giữa, in đậm, in bảng món, cắt giấy `GS V 66 0`).
   - Cập nhật `SystemSettings.razor`: Bổ sung 2 trường `PrinterIpAddress` và `PrinterPort` (mặc định 9100), cùng nút "In Test Thử".
   - Cập nhật `K80ReceiptModal.razor`: Bổ sung nút "IN QUA MẠNG LAN (ESC/POS)" gọi backend print service bên cạnh nút in trình duyệt hiện tại.

---

## 5. VERIFICATION METHOD (Phương Pháp Tự Xác Minh Độc Lập)

Người nhận báo cáo hoặc tester có thể kiểm tra độc lập các quan sát trên bằng cách:

1. **Xác minh thiếu nút Thanh toán**:
   - Mở file `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor`, kiểm tra từ dòng 48 đến 101. Xác nhận không có thẻ `<button>` nào gọi `HandlePayAsync`.
   - Tìm kiếm `<PosPaymentModal` trong `PosMain.razor`. Xác nhận không có kết quả tìm kiếm nào.
2. **Xác minh đứt chuỗi Token tại PIN Login**:
   - Mở `src/Restaurant.Web/Components/Pages/Auth/SharedLogin/SharedLogin.razor.cs`, kiểm tra dòng 104-120. Xác nhận biến `response.Token` không được sử dụng ở bất kỳ dòng lệnh nào.
3. **Xác minh các hàm Mock trong `AdminApiClient.cs`**:
   - Chạy lệnh grep: `grep_search "GetMock" "src/Restaurant.Web/Services/AdminApiClient.cs"` -> Xác nhận 6 phương thức mock tồn tại ở dòng 651-692 và được gọi làm fallback ngầm.
4. **Kiểm tra biên dịch Solution**:
   - Chạy lệnh `dotnet build src/Restaurant.sln` -> Xác nhận hiện tại build thành công 0 warning, 0 error.
   - Chạy lệnh `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj` -> Xác nhận 80/80 tests pass.
