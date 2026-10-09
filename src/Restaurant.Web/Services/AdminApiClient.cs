using System.Net.Http.Json;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Services;

public class AdminApiClient
{
    private readonly HttpClient _httpClient;

    public AdminApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Inventory - Materials
    public async Task<List<MaterialDto>> GetMaterialsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<MaterialDto>>("api/inventory/materials");
            return result ?? new List<MaterialDto>();
        }
        catch
        {
            return GetMockMaterials();
        }
    }

    public async Task<MaterialDto?> CreateMaterialAsync(CreateMaterialRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/materials", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MaterialDto>();
    }

    public async Task UpdateMaterialAsync(int id, CreateMaterialRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/inventory/materials/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMaterialAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/inventory/materials/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Inventory - Suppliers
    public async Task<List<SupplierDto>> GetSuppliersAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<SupplierDto>>("api/inventory/suppliers");
            return result ?? new List<SupplierDto>();
        }
        catch
        {
            return GetMockSuppliers();
        }
    }

    public async Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/suppliers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDto>();
    }

    // Inventory - Purchase Orders / Goods Receipts
    public async Task CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/purchase-orders", request);
        response.EnsureSuccessStatusCode();
    }

    // Inventory - Stock Audit
    public async Task ConductStockAuditAsync(StockAuditRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/stock-audit", request);
        response.EnsureSuccessStatusCode();
    }

    // Recipe Management
    public async Task SetMenuItemRecipeAsync(SetMenuItemRecipeRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/recipes/menu-item", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetModifierRecipeAsync(SetModifierOptionRecipeRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/recipes/modifier-option", request);
        response.EnsureSuccessStatusCode();
    }

    // Staff / User Management
    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<UserDto>>("api/auth/users");
            return result ?? new List<UserDto>();
        }
        catch
        {
            return GetMockUsers();
        }
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/users", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    // Orders History
    public async Task<List<OrderDto>> GetOrderHistoryAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<BillDetailDto>>("api/pos/orders");
            if (result != null)
            {
                return result.Select(b => new OrderDto
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

        return new List<OrderDto>();
    }

    public async Task<K80ReceiptDto?> GetOrderK80ReceiptAsync(int orderId)
    {
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

    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    // Customers / Loyalty
    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<CustomerDto>>("api/v1/loyalty/customers");
        return result ?? new List<CustomerDto>();
    }

    // Vouchers
    public Task<List<VoucherDto>> GetVouchersAsync()
    {
        return Task.FromResult(GetMockVouchers());
    }

    // Mock Data Fallbacks for UI preview when BE is running standalone
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
        new CustomerDto { Id = 1, FullName = "Nguyễn Văn An", PhoneNumber = "0909123456", TierName = "Vàng (Gold)", TotalPoints = 1250, AvailablePoints = 450 },
        new CustomerDto { Id = 2, FullName = "Trần Thị Bích", PhoneNumber = "0918234567", TierName = "Bạc (Silver)", TotalPoints = 620, AvailablePoints = 220 },
        new CustomerDto { Id = 3, FullName = "Hoàng Minh Trí", PhoneNumber = "0933345678", TierName = "Kim Cương (Diamond)", TotalPoints = 3400, AvailablePoints = 1100 }
    };

    private List<VoucherDto> GetMockVouchers() => new()
    {
        new VoucherDto { Id = 1, Code = "MOZZARELLA10", DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 200000, MaxDiscountAmount = 50000, StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(20), UsageLimit = 100, UsedCount = 34, IsActive = true },
        new VoucherDto { Id = 2, Code = "WELCOME50K", DiscountType = "FixedAmount", DiscountValue = 50000, MinOrderAmount = 300000, MaxDiscountAmount = 50000, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(15), UsageLimit = 50, UsedCount = 18, IsActive = true }
    };
}



public class CreateUserRequest
{
    public int RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? PinCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
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
