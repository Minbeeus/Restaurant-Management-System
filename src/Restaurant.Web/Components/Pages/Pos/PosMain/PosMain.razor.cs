using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Restaurant.Application.DTOs;
using Restaurant.Web.Components.Pages.Pos.Components;
using Restaurant.Web.Components.Pages.Pos.Components.OrderCart;

namespace Restaurant.Web.Components.Pages.Pos.PosMain;

public partial class PosMain : IAsyncDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public HttpClient Http { get; set; } = default!;

    [SupplyParameterFromQuery] public string TableId { get; set; } = default!;
    [SupplyParameterFromQuery] public string OrderId { get; set; } = default!;
    [SupplyParameterFromQuery] public bool IsTakeaway { get; set; } = false;

    protected string CurrentTableName { get; set; } = "Chọn bàn...";
    protected string CurrentOrderCode { get; set; } = "N/A";
    protected int ActiveOrderId { get; set; } = 0;

    protected bool ShowAlert { get; set; } = false;
    protected string NewOrderTableName { get; set; } = "";
    protected string ToastMessage { get; set; } = string.Empty;

    protected List<CategoryDto> Categories { get; set; } = new();
    protected List<MenuItemDto> AllMenuItems { get; set; } = new();
    protected List<ModifierGroupDto> AllModifierGroups { get; set; } = new();

    protected int ActiveCategoryId { get; set; } = 0;
    protected bool ShowImages { get; set; } = true;

    protected List<OrderCartItemLocal> CartItems { get; set; } = new();
    protected decimal CartSubTotal => CartItems.Sum(i => i.Price * i.Quantity);

    protected bool ShowComboModal { get; set; } = false;
    protected MenuItemDetailDto? SelectedItemDetail { get; set; }
    protected List<ModifierGroupDto>? SelectedModifierGroups { get; set; }

    protected bool ShowPaymentModal { get; set; } = false;

    // 5 Action Toolbar Modals & States
    protected bool ShowMemberModal { get; set; } = false;
    protected CustomerDto? SelectedMember { get; set; }

    protected bool ShowDiscountModal { get; set; } = false;
    protected bool ShowCancelOrderModal { get; set; } = false;

    protected bool IsProcessing { get; set; } = false;

    private HubConnection? _hubConnection;

    protected List<MenuItemDto> FilteredMenuItems =>
        ActiveCategoryId == 0
            ? AllMenuItems
            : AllMenuItems.Where(m => m.CategoryId == ActiveCategoryId).ToList();

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(OrderId) && int.TryParse(OrderId, out int parsedOrderId))
        {
            IsTakeaway = true;
            await LoadExistingTakeawayOrderAsync(parsedOrderId);
        }
        else if (IsTakeaway)
        {
            CurrentTableName = "Đơn Mang Về";
            CurrentOrderCode = $"#TAK-{DateTime.Now.Ticks % 10000}";
        }
        else if (!string.IsNullOrEmpty(TableId))
        {
            CurrentTableName = $"Bàn {TableId}";
            CurrentOrderCode = $"#ORD-{DateTime.Now.Ticks % 10000}";
            await CheckExistingTableOrderAsync();
        }

        try
        {
            var categoriesTask = Http.GetFromJsonAsync<List<CategoryDto>>("api/v1/categories");
            var menuItemsTask = Http.GetFromJsonAsync<List<MenuItemDto>>("api/v1/menu-items/active");
            var modifiersTask = Http.GetFromJsonAsync<List<ModifierGroupDto>>("api/v1/modifiers/groups");

            await Task.WhenAll(categoriesTask, menuItemsTask, modifiersTask);

            Categories = await categoriesTask ?? new List<CategoryDto>();
            AllMenuItems = await menuItemsTask ?? new List<MenuItemDto>();
            AllModifierGroups = await modifiersTask ?? new List<ModifierGroupDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching POS menu data: {ex.Message}");
        }

        // Hub Connection setup
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5011/hubs/restaurant")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string>("NewQrOrderReceived", (tableIdStr, tableName) =>
        {
            ShowAlert = true;
            NewOrderTableName = tableName;
            InvokeAsync(StateHasChanged);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR error: {ex.Message}");
        }
    }

    private async Task CheckExistingTableOrderAsync()
    {
        if (int.TryParse(TableId, out int parsedTableId))
        {
            try
            {
                var tablesResult = await Http.GetFromJsonAsync<ApiResponseWrapper<List<TableStatusOverviewDto>>>("api/v1/pos/tables/overview");
                var matchingTable = tablesResult?.Data?.FirstOrDefault(t => t.TableId == parsedTableId);
                if (matchingTable != null)
                {
                    CurrentTableName = matchingTable.TableName;
                    if (matchingTable.CurrentOrderId.HasValue && matchingTable.CurrentOrderId.Value > 0)
                    {
                        ActiveOrderId = matchingTable.CurrentOrderId.Value;
                        CurrentOrderCode = matchingTable.CurrentOrderCode ?? $"#ORD-{ActiveOrderId}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking existing table order: {ex.Message}");
            }
        }
    }

    protected void HandleCategoryChanged(int categoryId) => ActiveCategoryId = categoryId;
    protected void HandleImageToggle(bool show) => ShowImages = show;

    protected void HandleMenuItemClicked(MenuItemDto item)
    {
        if (AllModifierGroups.Any())
        {
            SelectedItemDetail = new MenuItemDetailDto
            {
                Id = item.Id,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                Name = item.Name,
                Price = item.Price,
                Description = item.Description,
                ImageUrl = item.ImageUrl,
                IsCombo = item.IsCombo,
                ModifierGroups = AllModifierGroups
            };
            SelectedModifierGroups = AllModifierGroups;
            ShowComboModal = true;
            return;
        }

        AddDirectToCart(item);
    }

    private void AddDirectToCart(MenuItemDto item)
    {
        var existing = CartItems.FirstOrDefault(i => i.MenuItemId == item.Id && (i.SelectedModifiers == null || !i.SelectedModifiers.Any()));
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            CartItems.Add(new OrderCartItemLocal
            {
                MenuItemId = item.Id,
                Name = item.Name,
                Price = item.Price,
                Quantity = 1,
                SelectedModifiers = new List<int>()
            });
        }
    }

    protected void HandleConfirmCombo((MenuItemDetailDto item, int qty, List<int> selectedMods) result)
    {
        CartItems.Add(new OrderCartItemLocal
        {
            MenuItemId = result.item.Id,
            Name = result.item.Name,
            Price = result.item.Price,
            Quantity = result.qty,
            SelectedModifiers = result.selectedMods
        });
    }

    protected void HandleCartQuantityChanged(OrderCartItemLocal item) { }
    protected void HandleCartItemRemoved(OrderCartItemLocal item) => CartItems.Remove(item);

    // 1. Action Toolbar: Member Confirmation
    protected void HandleMemberConfirmed(CustomerDto member)
    {
        SelectedMember = member;
        ToastMessage = $"Đã xác nhận thành viên {member.FullName} ({member.TierName}) cho đơn!";
    }

    // 2. Action Toolbar: Discount Confirmation
    protected void HandleDiscountConfirmed(PosDiscountModal.DiscountAppliedEventArgs args)
    {
        foreach (var item in CartItems.Where(i => args.ItemIds.Contains(i.MenuItemId)))
        {
            if (args.Type == "Percent")
            {
                item.Price = Math.Round(item.Price * (1 - (args.Value / 100m)));
            }
            else
            {
                item.Price = Math.Max(0, item.Price - args.Value);
            }
        }
        ToastMessage = "Đã áp dụng giảm giá cho đơn hàng!";
    }

    // 3. Action Toolbar: Print Pre-Bill
    protected void HandlePrintPreBill()
    {
        ToastMessage = $"Đang gửi lệnh in hóa đơn tạm tính cho {CurrentTableName}...";
    }

    // 4. Action Toolbar: Cancel Order Confirmation
    protected async Task HandleOrderCancelledAsync(string reason)
    {
        try
        {
            if (ActiveOrderId > 0)
            {
                var request = new CancelOrderRequest { OrderId = ActiveOrderId, Reason = reason };
                await Http.PostAsJsonAsync("api/v1/pos/orders/cancel", request);
            }
            CartItems.Clear();
            ToastMessage = "Đã hủy đơn hàng thành công!";
            await Task.Delay(1000);
            Navigation.NavigateTo("/pos/tables");
        }
        catch (Exception ex)
        {
            ToastMessage = $"Không thể hủy đơn: {ex.Message}";
        }
    }

    // 5. Action Toolbar: Done (Save, Send to Kitchen & Return)
    protected async Task HandleDoneAsync()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            if (CartItems.Any())
            {
                await HandleSendToKitchenAsync();
            }
            if (IsTakeaway)
            {
                Navigation.NavigateTo("/pos/takeaway");
            }
            else
            {
                Navigation.NavigateTo("/pos/tables");
            }
        }
        catch (Exception ex)
        {
            ToastMessage = $"Lỗi hoàn tất: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task HandleSendToKitchenAsync()
    {
        if (!CartItems.Any()) return;

        int? parsedTableId = null;
        if (!IsTakeaway && !string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
        {
            parsedTableId = tId;
        }

        var request = new SubmitPosOrderRequest
        {
            TableId = parsedTableId,
            CustomerNote = IsTakeaway ? "Đơn mang về (POS)" : "Đơn ăn tại bàn (POS)",
            Items = CartItems.Select(i => new CartItemValidateRequest
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                Note = i.Note,
                SelectedModifiers = i.SelectedModifiers ?? new List<int>(),
                SelectedComboItems = new List<int>()
            }).ToList()
        };

        try
        {
            var response = await Http.PostAsJsonAsync("api/v1/pos/orders/submit", request);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<SubmitPosOrderResponse>>();
                if (body?.Data != null)
                {
                    ActiveOrderId = body.Data.OrderId;
                    CurrentOrderCode = body.Data.OrderCode;
                }

                CartItems.Clear();
                ToastMessage = "Đã gửi đơn xuống Bếp thành công!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ToastMessage = $"Gửi đơn thất bại: {error}";
            }
        }
        catch (Exception ex)
        {
            ToastMessage = $"Lỗi gửi đơn: {ex.Message}";
        }
    }

    protected async Task HandlePayAsync()
    {
        if (!CartItems.Any()) return;

        if (ActiveOrderId == 0)
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
            CartItems.Clear();
            ToastMessage = "Thanh toán thành công! Đang trở về sơ đồ bàn...";
            await Task.Delay(1200);
            Navigation.NavigateTo("/pos/tables");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private async Task LoadExistingTakeawayOrderAsync(int orderId)
    {
        try
        {
            var resp = await Http.GetFromJsonAsync<ApiResponseWrapper<TakeawayOrderDto>>($"api/v1/pos/takeaway/orders/{orderId}");
            if (resp?.Data != null)
            {
                var order = resp.Data;
                ActiveOrderId = order.OrderId;
                CurrentOrderCode = order.OrderCode;
                CurrentTableName = $"Đơn Mang Về ({order.OrderCode})";

                CartItems = order.Items.Select(i => new OrderCartItemLocal
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.MenuItemName,
                    Price = i.UnitPrice,
                    Quantity = i.Quantity,
                    Note = i.Note,
                    SelectedModifiers = new List<int>()
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading takeaway order {orderId}: {ex.Message}");
        }
    }

    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
