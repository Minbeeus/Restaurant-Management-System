using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Restaurant.Application.Common;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public class AdminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly ILogger<AdminApiClient>? _logger;

    public AdminApiClient(
        HttpClient httpClient, 
        AuthenticationStateProvider? authenticationStateProvider = null,
        ILogger<AdminApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        if (_authenticationStateProvider == null) return;
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = user.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                }
            }
        }
        catch (InvalidOperationException) { }
    }

    public class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public object? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // ==========================================
    // 1. INVENTORY - MATERIALS
    // ==========================================
    public async Task<List<MaterialDto>> GetMaterialsAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<MaterialDto>>>("api/v1/inventory/materials");
            if (res?.Data != null) return res.Data;

            var raw = await _httpClient.GetFromJsonAsync<List<MaterialDto>>("api/v1/inventory/materials");
            if (raw != null) return raw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching materials: {ex.Message}");
        }
        return GetMockMaterials();
    }

    public async Task<MaterialDto?> CreateMaterialAsync(CreateMaterialRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/materials", request);
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<MaterialDto>>();
        return res?.Data ?? await response.Content.ReadFromJsonAsync<MaterialDto>();
    }

    public async Task UpdateMaterialAsync(int id, CreateMaterialRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"api/v1/inventory/materials/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMaterialAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"api/v1/inventory/materials/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ==========================================
    // 2. INVENTORY - SUPPLIERS
    // ==========================================
    public async Task<List<SupplierDto>> GetSuppliersAsync(bool includeInactive = false)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<SupplierDto>>>($"api/v1/inventory/suppliers?includeInactive={includeInactive}");
            if (res?.Data != null) return res.Data;

            var raw = await _httpClient.GetFromJsonAsync<List<SupplierDto>>($"api/v1/inventory/suppliers?includeInactive={includeInactive}");
            if (raw != null) return raw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching suppliers: {ex.Message}");
        }
        return GetMockSuppliers();
    }

    public async Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/suppliers", request);
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<SupplierDto>>();
        return res?.Data ?? await response.Content.ReadFromJsonAsync<SupplierDto>();
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int id, CreateSupplierRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"api/v1/inventory/suppliers/{id}", request);
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<SupplierDto>>();
        return res?.Data ?? await response.Content.ReadFromJsonAsync<SupplierDto>();
    }

    public async Task DeleteSupplierAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"api/v1/inventory/suppliers/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> ToggleSupplierStatusAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsync($"api/v1/inventory/suppliers/{id}/toggle-status", null);
        return response.IsSuccessStatusCode;
    }

    // ==========================================
    // 3. INVENTORY - GOODS RECEIPTS (NHẬP KHO)
    // ==========================================
    public async Task<List<GoodsReceiptDto>> GetGoodsReceiptsAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<GoodsReceiptDto>>>("api/v1/inventory/goods-receipts");
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching goods receipts: {ex.Message}");
        }
        return new List<GoodsReceiptDto>();
    }

    public async Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<GoodsReceiptDto>>($"api/v1/inventory/goods-receipts/{id}");
            return res?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching goods receipt {id}: {ex.Message}");
            return null;
        }
    }

    public async Task CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/purchase-orders", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> CreateGoodsReceiptAsync(CreatePurchaseOrderRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/purchase-orders", request);
        return response.IsSuccessStatusCode;
    }

    // ==========================================
    // 4. INVENTORY - STOCK AUDITS / KIỂM KÊ KHO
    // ==========================================
    public async Task<List<StocktakeDto>> GetStocktakesAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<StocktakeDto>>>("api/v1/inventory/stocktakes");
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching stocktakes: {ex.Message}");
        }
        return new List<StocktakeDto>();
    }

    public async Task<StocktakeDto?> GetStocktakeByIdAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<StocktakeDto>>($"api/v1/inventory/stocktakes/{id}");
            return res?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching stocktake {id}: {ex.Message}");
            return null;
        }
    }

    public async Task ConductStockAuditAsync(StockAuditRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/audit", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<MaterialDto>> GetMaterialsBySupplierAsync(int supplierId)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<MaterialDto>>>($"api/v1/inventory/suppliers/{supplierId}/materials");
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching supplier materials: {ex.Message}");
        }
        return new List<MaterialDto>();
    }

    public async Task<bool> CreateBlindStocktakeAsync(CreateBlindStocktakeRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/stocktakes/blind", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReconcileStocktakeAsync(int id, ReconcileStocktakeRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/v1/inventory/stocktakes/{id}/reconcile", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<InventoryLossSummaryDto> GetInventoryLossSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var query = new List<string>();
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

            var url = $"api/v1/reports/loss{qs}";
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<InventoryLossSummaryDto>>(url);
            if (res?.Data != null) return res.Data;

            // Fallback to legacy endpoint
            var legacyRes = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<InventoryLossSummaryDto>>($"api/v1/inventory/reports/loss{qs}");
            if (legacyRes?.Data != null) return legacyRes.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching loss summary: {ex.Message}");
        }

        return new InventoryLossSummaryDto
        {
            TotalVarianceAmount = 0,
            TotalWasteAmount = 0,
            DiscrepancyCount = 0
        };
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var query = new List<string>();
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

            var url = $"api/v1/reports/revenue{qs}";
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<RevenueReportDto>>(url);
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching revenue report: {ex.Message}");
        }

        return new RevenueReportDto();
    }

    public async Task<TopSellingReportDto> GetTopMoversReportAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 50)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var query = new List<string> { $"limit={limit}" };
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            var qs = "?" + string.Join("&", query);

            var url = $"api/v1/reports/top-movers{qs}";
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<TopSellingReportDto>>(url);
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching top movers report: {ex.Message}");
        }

        return new TopSellingReportDto();
    }

    // ==========================================
    // 5. RECIPES / BOM (BILL OF MATERIALS)
    // ==========================================
    public async Task<MenuItemRecipeDto?> GetMenuItemRecipeAsync(int menuItemId)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<MenuItemRecipeDto>>($"api/v1/inventory/recipes/menu-item/{menuItemId}");
            return res?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading recipe for menu item {menuItemId}: {ex.Message}");
            return null;
        }
    }

    public async Task SetMenuItemRecipeAsync(SetMenuItemRecipeRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/recipes/menu-item", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ModifierOptionRecipeDto?> GetModifierOptionRecipeAsync(int modifierOptionId)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<ModifierOptionRecipeDto>>($"api/v1/inventory/recipes/modifier-option/{modifierOptionId}");
            return res?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading recipe for modifier option {modifierOptionId}: {ex.Message}");
            return null;
        }
    }

    public async Task SetModifierRecipeAsync(SetModifierOptionRecipeRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/recipes/modifier-option", request);
        response.EnsureSuccessStatusCode();
    }

    // ==========================================
    // 6. STAFF / USER MANAGEMENT
    // ==========================================
    public async Task<List<UserDto>> GetUsersAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<UserDto>>("api/v1/auth/users");
            if (result != null) return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users: {ex.Message}");
        }
        return GetMockUsers();
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/users", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsync($"api/v1/auth/users/{userId}/toggle-status", null);
        return response.IsSuccessStatusCode;
    }

    // ==========================================
    // 7. ORDERS HISTORY & BILLS
    // ==========================================
    public async Task<List<OrderDto>> GetOrderHistoryAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<BillDetailDto>>>("api/v1/bills");
            if (res?.Data != null)
            {
                return res.Data.Select(b => new OrderDto
                {
                    Id = b.OrderId,
                    OrderId = b.OrderId,
                    OrderCode = b.OrderCode,
                    TableName = b.TableName,
                    TableAreaName = b.TableAreaName,
                    SubTotal = b.SubTotal,
                    DiscountAmount = b.DiscountAmount,
                    TotalAmount = b.TotalAmount,
                    Status = b.OrderStatusName,
                    OrderStatusName = b.OrderStatusName,
                    OrderType = b.OrderType,
                    CreatedAt = b.CreatedAt,
                    CashierName = b.CashierName,
                    CustomerName = b.CustomerName,
                    CustomerPhone = b.CustomerPhone,
                    PaymentMethodName = b.PaymentMethodName,
                    CashReceived = b.CashReceived,
                    ChangeAmount = b.ChangeAmount,
                    Items = b.Items
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching order history: {ex.Message}");
        }

        return GetMockOrders();
    }

    public async Task<K80ReceiptDto?> GetOrderK80ReceiptAsync(int orderId)
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<K80ReceiptDto>>($"api/v1/bills/{orderId}/k80");
            return res?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching K80 receipt: {ex.Message}");
            return null;
        }
    }

    // ==========================================
    // 8. CUSTOMERS / LOYALTY
    // ==========================================
    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<CustomerDto>>("api/v1/loyalty/customers");
            if (result != null) return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching customers: {ex.Message}");
        }
        return GetMockCustomers();
    }

    // ==========================================
    // 9. VOUCHERS / MARKETING
    // ==========================================
    public async Task<List<VoucherDto>> GetVouchersAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<Restaurant.Domain.Entities.Voucher>>>("api/v1/vouchers");
            if (res?.Data != null)
            {
                return res.Data.Select(v => new VoucherDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    DiscountType = v.DiscountType == 2 ? "FixedAmount" : "Percentage",
                    DiscountValue = v.DiscountValue,
                    MinOrderAmount = v.MinOrderAmount,
                    MaxDiscountAmount = v.MaxDiscountAmount,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    IsActive = v.IsActive
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching vouchers from API: {ex.Message}");
        }
        return GetMockVouchers();
    }

    public async Task<bool> CreateVoucherAsync(CreateVoucherRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/vouchers", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateVoucherAsync(int id, CreateVoucherRequest request)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"api/v1/vouchers/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteVoucherAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"api/v1/vouchers/{id}");
        return response.IsSuccessStatusCode;
    }

    // ==========================================
    // SYSTEM MANAGEMENT (ROLES, REGISTERS, STATIONS, AUDIT, SETTINGS, POLICY)
    // ==========================================
    public async Task<List<RoleDto>> GetRolesAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<RoleDto>>>("api/v1/system/roles");
            return response?.Data ?? new List<RoleDto>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching roles");
            return new List<RoleDto>
            {
                new RoleDto { Id = 1, Name = "Admin", Description = "Quản trị viên toàn quyền hệ thống", UserCount = 1, Permissions = new() { "Toàn quyền quản trị", "Quản lý nhân sự & tài khoản", "Xem & phân bổ kho hàng" } },
                new RoleDto { Id = 2, Name = "Manager", Description = "Quản lý ca, duyệt hủy món, quản lý kho", UserCount = 1, Permissions = new() { "Quản lý ca làm việc", "Duyệt void/cancel món", "Nhập kho & kiểm kê" } },
                new RoleDto { Id = 3, Name = "Cashier", Description = "Thu ngân, thanh toán và in hóa đơn", UserCount = 1, Permissions = new() { "Mở ca & Đóng ca", "Tạo đơn hàng", "Thanh toán", "In hóa đơn" } },
                new RoleDto { Id = 4, Name = "Chef", Description = "Bếp trưởng và điều phối KDS", UserCount = 1, Permissions = new() { "Xem màn hình KDS", "Đổi trạng thái món", "Xem định lượng món (BOM)" } }
            };
        }
    }

    public async Task<List<RegisterDto>> GetRegistersAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<RegisterDto>>>("api/v1/system/registers");
            return response?.Data ?? new List<RegisterDto>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching registers");
            return new List<RegisterDto>
            {
                new RegisterDto { Id = 1, Code = "POS-01", Name = "Quầy Thu Ngân Chính (Tầng 1)", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-1) },
                new RegisterDto { Id = 2, Code = "POS-02", Name = "Quầy Thu Ngân Bar & Đồ Uống", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-1) }
            };
        }
    }

    public async Task<bool> CreateRegisterAsync(CreateRegisterRequest req)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/system/registers", req);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleRegisterStatusAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsync($"api/v1/system/registers/{id}/toggle-status", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<KitchenStationDto>> GetKitchenStationsAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<KitchenStationDto>>>("api/v1/system/kitchen-stations");
            return response?.Data ?? new List<KitchenStationDto>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching kitchen stations");
            return new List<KitchenStationDto>
            {
                new KitchenStationDto { Id = 1, Code = "HOT", Name = "Bếp Nóng & Mì Ý", SortOrder = 1, IsActive = true, MenuItemCount = 8 },
                new KitchenStationDto { Id = 2, Code = "PIZZA", Name = "Bếp Nướng Pizza Lò Củi", SortOrder = 2, IsActive = true, MenuItemCount = 12 },
                new KitchenStationDto { Id = 3, Code = "BAR", Name = "Quầy Pha Chế Bar", SortOrder = 3, IsActive = true, MenuItemCount = 6 }
            };
        }
    }

    public async Task<bool> CreateKitchenStationAsync(CreateKitchenStationRequest req)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/system/kitchen-stations", req);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleKitchenStationStatusAsync(int id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsync($"api/v1/system/kitchen-stations/{id}/toggle-status", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<AuditLogDto>>>("api/v1/system/audit-logs");
            return response?.Data ?? new List<AuditLogDto>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching audit logs");
            return new List<AuditLogDto>
            {
                new AuditLogDto { Id = 1, UserName = "admin", Action = "System.Init", EntityName = "System", Details = "Khởi tạo hệ thống BOH", CreatedAt = DateTime.Now.AddHours(-1) }
            };
        }
    }

    public async Task<SystemSettingsDto> GetSystemSettingsAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<SystemSettingsDto>>("api/v1/system/settings");
            return response?.Data ?? new SystemSettingsDto();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching settings");
            return new SystemSettingsDto();
        }
    }

    public async Task<bool> SaveSystemSettingsAsync(SystemSettingsDto settings)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("api/v1/system/settings", settings);
        return response.IsSuccessStatusCode;
    }

    public async Task<PosPolicyOptions> GetPosPolicyAsync()
    {
        await SetAuthorizationHeaderAsync();
        try
        {
            var policy = await _httpClient.GetFromJsonAsync<PosPolicyOptions>("api/pos/policy");
            return policy ?? new PosPolicyOptions();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching POS policy");
            return new PosPolicyOptions();
        }
    }

    public async Task<bool> UpdatePosPolicyAsync(PosPolicyOptions policy)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync("api/pos/policy", policy);
        return response.IsSuccessStatusCode;
    }

    // ==========================================
    // MOCK DATA FALLBACKS FOR ROBUST OFFLINE / TEST
    // ==========================================
    private List<MaterialDto> GetMockMaterials() => new()
    {
        new MaterialDto { Id = 1, Name = "Bột mì Tipo 00", UnitName = "Kg", CurrentStock = 120.5m, MinStockLevel = 30m, CostPerUnit = 25000, IsActive = true },
        new MaterialDto { Id = 2, Name = "Phô mai Mozzarella Fresh", UnitName = "Kg", CurrentStock = 14.2m, MinStockLevel = 20m, CostPerUnit = 180000, IsActive = true },
        new MaterialDto { Id = 3, Name = "Sốt cà chua San Marzano", UnitName = "Lít", CurrentStock = 45.0m, MinStockLevel = 15m, CostPerUnit = 45000, IsActive = true },
        new MaterialDto { Id = 4, Name = "Lá húng tây Basil tươi", UnitName = "Kg", CurrentStock = 1.8m, MinStockLevel = 2.0m, CostPerUnit = 120000, IsActive = true },
        new MaterialDto { Id = 5, Name = "Thịt bò Xay đút lò", UnitName = "Kg", CurrentStock = 28.5m, MinStockLevel = 10m, CostPerUnit = 220000, IsActive = true }
    };

    private List<SupplierDto> GetMockSuppliers() => new()
    {
        new SupplierDto { Id = 1, Name = "Công ty TNHH Thực phẩm Ý Việt", ContactPerson = "Nguyễn Văn Hùng", PhoneNumber = "0901234567", Address = "123 Quận 1, TP.HCM", IsActive = true },
        new SupplierDto { Id = 2, Name = "Nông sản Sạch Đà Lạt", ContactPerson = "Trần Thị Mai", PhoneNumber = "0987654321", Address = "45 Phường 3, Đà Lạt", IsActive = true }
    };

    private List<UserDto> GetMockUsers() => new()
    {
        new UserDto { Id = 1, Username = "admin", FullName = "Quản trị viên Hệ thống", RoleName = "Admin", PhoneNumber = "0912345678", IsActive = true },
        new UserDto { Id = 2, Username = "manager01", FullName = "Nguyễn Hoàng Nam", RoleName = "Manager", PhoneNumber = "0934567890", IsActive = true },
        new UserDto { Id = 3, Username = "cashier01", FullName = "Lê Thị Thu", RoleName = "Cashier", PhoneNumber = "0945678901", IsActive = true },
        new UserDto { Id = 4, Username = "chef01", FullName = "Phạm Quốc Bảo", RoleName = "Chef", PhoneNumber = "0956789012", IsActive = true }
    };

    private List<OrderDto> GetMockOrders() => new()
    {
        new OrderDto { Id = 101, OrderCode = "ORD-20260918-001", TableName = "Bàn 04", TableAreaName = "Tầng 1", SubTotal = 320000, TotalAmount = 320000, Status = "Paid", OrderType = "DineIn", CreatedAt = DateTime.Now.AddHours(-2) },
        new OrderDto { Id = 102, OrderCode = "ORD-20260918-002", TableName = "Bàn 12", TableAreaName = "Sân vườn", SubTotal = 540000, TotalAmount = 540000, Status = "Completed", OrderType = "DineIn", CreatedAt = DateTime.Now.AddHours(-1) },
        new OrderDto { Id = 103, OrderCode = "ORD-20260918-003", TableName = "Mang đi", TableAreaName = "Quầy POS", SubTotal = 150000, TotalAmount = 150000, Status = "Confirmed", OrderType = "Takeaway", CreatedAt = DateTime.Now.AddMinutes(-30) }
    };

    private List<CustomerDto> GetMockCustomers() => new()
    {
        new CustomerDto { Id = 1, FullName = "Nguyễn Văn An", PhoneNumber = "0909123456", TierName = "Vàng (Gold)", DiscountPercent = 10, TotalPoints = 1250, AvailablePoints = 450 },
        new CustomerDto { Id = 2, FullName = "Trần Thị Bích", PhoneNumber = "0918234567", TierName = "Bạc (Silver)", DiscountPercent = 5, TotalPoints = 620, AvailablePoints = 220 },
        new CustomerDto { Id = 3, FullName = "Hoàng Minh Trí", PhoneNumber = "0933345678", TierName = "Kim Cương (Diamond)", DiscountPercent = 15, TotalPoints = 3400, AvailablePoints = 1100 }
    };

    private List<VoucherDto> GetMockVouchers() => new()
    {
        new VoucherDto { Id = 1, Code = "MOZZARELLA10", DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 200000, MaxDiscountAmount = 50000, StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(20), UsageLimit = 100, UsedCount = 34, IsActive = true },
        new VoucherDto { Id = 2, Code = "WELCOME50K", DiscountType = "FixedAmount", DiscountValue = 50000, MinOrderAmount = 300000, MaxDiscountAmount = 50000, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(15), UsageLimit = 50, UsedCount = 18, IsActive = true }
    };
}

public class OrderDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string TableAreaName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OrderStatusName { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string PaymentMethodName { get; set; } = "Tiền Mặt";
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public List<BillItemDto> Items { get; set; } = new();

    public int DisplayId => OrderId > 0 ? OrderId : Id;
    public string DisplayStatus => !string.IsNullOrEmpty(OrderStatusName) ? OrderStatusName : Status;
}

public class VoucherDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}
