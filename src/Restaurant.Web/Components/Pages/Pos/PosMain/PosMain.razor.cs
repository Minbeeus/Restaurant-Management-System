using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

    protected async Task ShowToastAsync(string message)
    {
        ToastMessage = message;
        StateHasChanged();
        await Task.Delay(3000);
        if (ToastMessage == message)
        {
            ToastMessage = string.Empty;
            StateHasChanged();
        }
    }

    protected List<CategoryDto> Categories { get; set; } = new();
    protected List<MenuItemDto> AllMenuItems { get; set; } = new();
    protected List<ModifierGroupDto> AllModifierGroups { get; set; } = new();

    protected int ActiveCategoryId { get; set; } = 0;
    protected bool ShowImages { get; set; } = true;
    protected string SearchQuery { get; set; } = string.Empty;

    protected List<OrderCartItemLocal> CartItems { get; set; } = new();
    protected decimal CartSubTotal => CartItems.Sum(i => i.Price * i.Quantity);
    protected decimal AppliedDiscountAmount { get; set; } = 0m;

    protected bool ShowModifierModal { get; set; } = false;
    protected MenuItemDetailDto? SelectedItemDetail { get; set; }
    protected List<ModifierGroupDto>? SelectedModifierGroups { get; set; }

    protected bool ShowComboModal { get; set; } = false;
    protected ComboDetailDto? SelectedComboDetail { get; set; }
    protected bool IsComboLoading { get; set; } = false;

    protected bool ShowPaymentModal { get; set; } = false;
    protected bool ShowK80Modal { get; set; } = false;
    protected K80ReceiptDto? ReceiptData { get; set; }

    // 5 Action Toolbar Modals & States
    protected bool ShowMemberModal { get; set; } = false;
    protected CustomerDto? SelectedMember { get; set; }

    protected bool ShowDiscountModal { get; set; } = false;
    protected bool ShowCancelOrderModal { get; set; } = false;
    
    protected bool ShowVoidItemModal { get; set; } = false;
    protected OrderCartItemLocal? VoidingItem { get; set; }

    protected bool ShowTableActionModal { get; set; } = false;
    protected string TableActionType { get; set; } = "Move";

    protected bool IsProcessing { get; set; } = false;

    private HubConnection? _hubConnection;

    protected List<MenuItemDto> FilteredMenuItems =>
        AllMenuItems
            .Where(m => ActiveCategoryId == 0 || m.CategoryId == ActiveCategoryId)
            .Where(m => string.IsNullOrWhiteSpace(SearchQuery) ||
                        m.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(m.Description) && m.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();

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

                        var activeOrderResp = await Http.GetFromJsonAsync<ApiResponseWrapper<TakeawayOrderDto>>($"api/v1/pos/tables/{parsedTableId}/active-order");
                        if (activeOrderResp?.Data != null)
                        {
                            var activeOrder = activeOrderResp.Data;
                            CartItems = activeOrder.Items.Select(i => new OrderCartItemLocal
                            {
                                MenuItemId = i.MenuItemId,
                                Name = i.MenuItemName,
                                Price = i.UnitPrice,
                                Quantity = i.Quantity,
                                Note = i.Note,
                                IsSentToKitchen = true,
                                SelectedModifiers = i.ModifierIds ?? new List<int>()
                            }).ToList();
                        }
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
    protected void HandleSearchQueryChanged(string query) => SearchQuery = query;

    protected void HandleSearchEnterPressed()
    {
        var firstMatch = FilteredMenuItems.FirstOrDefault();
        if (firstMatch != null)
        {
            HandleMenuItemClicked(firstMatch);
            SearchQuery = string.Empty;
        }
    }

    protected void HandleHeaderBackNav()
    {
        if (IsTakeaway)
        {
            Navigation.NavigateTo("/pos/takeaway");
        }
        else
        {
            Navigation.NavigateTo("/pos/tables");
        }
    }

    protected void HandleTransferTable()
    {
        TableActionType = "Move";
        ShowTableActionModal = true;
    }

    protected void HandleMergeTable()
    {
        TableActionType = "Merge";
        ShowTableActionModal = true;
    }

    protected async Task HandleTableActionCompleted(int targetTableId)
    {
        // On success, we just reload the whole page to the new table ID
        _ = ShowToastAsync(TableActionType == "Move" ? "Chuyển bàn thành công!" : "Gộp bàn thành công!");
        await Task.Delay(1000);
        Navigation.NavigateTo($"/pos/order?tableId={targetTableId}", forceLoad: true);
    }

    protected void HandleMenuItemClicked(MenuItemDto item)
    {
        if (item.IsCombo)
        {
            ShowComboModal = true;
            IsComboLoading = true;
            SelectedComboDetail = null;
            
            // Load combo async
            _ = InvokeAsync(async () =>
            {
                try
                {
                    SelectedComboDetail = await Http.GetFromJsonAsync<ComboDetailDto>($"api/v1/combos/{item.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading combo: {ex.Message}");
                    _ = ShowToastAsync("Lỗi: Không tải được thông tin Combo.");
                    ShowComboModal = false;
                }
                finally
                {
                    IsComboLoading = false;
                    StateHasChanged();
                }
            });
            return;
        }

        if (item.ModifierGroups != null && item.ModifierGroups.Any())
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
            ShowModifierModal = true;
            return;
        }

        AddDirectToCart(item);
    }

    private void AddDirectToCart(MenuItemDto item)
    {
        var existing = CartItems.FirstOrDefault(i => i.MenuItemId == item.Id && !i.IsSentToKitchen && (i.SelectedModifiers == null || !i.SelectedModifiers.Any()));
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
                SelectedModifiers = new List<int>(),
                ModifierNames = new List<string>(),
                IsSentToKitchen = false
            });
        }
    }

    protected void HandleConfirmModifier((MenuItemDetailDto item, int qty, List<int> selectedMods) result)
    {
        var modNames = new List<string>();
        if (result.selectedMods != null && result.selectedMods.Any() && result.item.ModifierGroups != null)
        {
            foreach (var group in result.item.ModifierGroups)
            {
                foreach (var opt in group.ModifierOptions.Where(o => result.selectedMods.Contains(o.Id)))
                {
                    modNames.Add(opt.Name);
                }
            }
        }

        CartItems.Add(new OrderCartItemLocal
        {
            MenuItemId = result.item.Id,
            Name = result.item.Name,
            Price = result.item.Price,
            Quantity = result.qty,
            SelectedModifiers = result.selectedMods,
            ModifierNames = modNames,
            IsSentToKitchen = false
        });
    }

    protected void HandleConfirmCombo((ComboDetailDto combo, int qty, List<CartComboItemRequest> selectedItems) result)
    {
        var comboNames = new List<string>();
        foreach (var req in result.selectedItems)
        {
            // Find name
            var optName = "";
            foreach (var group in result.combo.Groups)
            {
                var opt = group.Items.FirstOrDefault(i => i.MenuItemId == req.MenuItemId);
                if (opt != null)
                {
                    optName = opt.MenuItemName;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(optName))
            {
                comboNames.Add($"{req.Quantity}x {optName}");
            }
        }

        CartItems.Add(new OrderCartItemLocal
        {
            MenuItemId = result.combo.Id,
            Name = result.combo.Name,
            Price = result.combo.Price,
            Quantity = result.qty,
            IsSentToKitchen = false,
            SelectedModifiers = new List<int>(), // Combo không dùng modifier thường
            SelectedComboItems = result.selectedItems,
            ComboItemNames = comboNames
        });
    }

    protected void HandleCartQuantityChanged(OrderCartItemLocal item) { }
    protected void HandleCartItemRemoved(OrderCartItemLocal item) => CartItems.Remove(item);

    protected void HandleItemVoidRequested(OrderCartItemLocal item)
    {
        VoidingItem = item;
        ShowVoidItemModal = true;
    }

    protected void HandleVoidCompleted(bool success)
    {
        if (success && VoidingItem != null)
        {
            CartItems.Remove(VoidingItem);
            _ = ShowToastAsync("Đã hủy món " + VoidingItem.Name + " thành công!");
        }
        VoidingItem = null;
    }

    // 1. Action Toolbar: Member Confirmation
    protected void HandleMemberConfirmed(CustomerDto member)
    {
        SelectedMember = member;
        _ = ShowToastAsync("Đã xác nhận thành viên " + member.FullName + " (" + member.TierName + ") cho đơn!");
    }

    // 2. Action Toolbar: Discount Confirmation
    protected void HandleDiscountConfirmed(PosDiscountModal.DiscountAppliedEventArgs args)
    {
        decimal totalDiscount = 0m;
        if (args.Type == "Percent")
        {
            totalDiscount = Math.Round(CartItems.Where(i => args.ItemIds.Contains(i.MenuItemId)).Sum(i => i.Price * i.Quantity) * (args.Value / 100m));
        }
        else
        {
            totalDiscount = Math.Min(CartItems.Where(i => args.ItemIds.Contains(i.MenuItemId)).Sum(i => i.Price * i.Quantity), args.Value);
        }
        AppliedDiscountAmount = totalDiscount;
        _ = ShowToastAsync("Đã áp dụng giảm giá cho đơn hàng!");
    }

    // 3. Action Toolbar: Print Pre-Bill
    protected async Task HandlePrintPreBill()
    {
        if (ActiveOrderId > 0)
        {
            try
            {
                var printResponse = await Http.PostAsync($"api/v1/pos/orders/{ActiveOrderId}/print-temp-bill", null);
                if (!printResponse.IsSuccessStatusCode)
                {
                    _ = ShowToastAsync("Không thể in tạm tính!");
                    return;
                }

                var response = await Http.GetFromJsonAsync<ApiResponseWrapper<K80ReceiptDto>>($"api/v1/bills/{ActiveOrderId}/k80");
                if (response?.Data != null)
                {
                    ReceiptData = response.Data;
                    ShowK80Modal = true;
                    _ = ShowToastAsync("Đã in tạm tính. Bàn đã chuyển sang trạng thái Chờ thanh toán (Màu vàng).");
                }
                else
                {
                    _ = ShowToastAsync("Không thể lấy dữ liệu hóa đơn!");
                }
            }
            catch (Exception ex)
            {
                _ = ShowToastAsync("Lỗi in hóa đơn: " + ex.Message);
            }
        }
        else
        {
            _ = ShowToastAsync("Vui lòng gửi bếp trước khi in tạm tính!");
        }
    }

    // 4. Action Toolbar: Cancel Order Confirmation
    protected async Task HandleOrderCancelledAsync(string reason)
    {
        try
        {
            if (ActiveOrderId > 0)
            {
                var request = new CancelOrderRequest { OrderId = ActiveOrderId, Reason = reason };
                await Http.PostAsJsonAsync($"api/v1/pos/orders/{ActiveOrderId}/cancel", request);
            }
            CartItems.Clear();
            _ = ShowToastAsync("Đã hủy đơn hàng thành công!");
            await Task.Delay(1000);
            HandleHeaderBackNav();
        }
        catch (Exception ex)
        {
            _ = ShowToastAsync("Không thể hủy đơn: " + ex.Message);
        }
    }

    // 5. Action Toolbar: Done (Save, Send to Kitchen & Return)
    protected async Task HandleDoneAsync()
    {
        if (IsProcessing) return;
        IsProcessing = true;

        try
        {
            if (CartItems.Any(i => !i.IsSentToKitchen))
            {
                await HandleSendToKitchenAsync();
            }
            HandleHeaderBackNav();
        }
        catch (Exception ex)
        {
            _ = ShowToastAsync("Lỗi hoàn tất: " + ex.Message);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task HandleSendToKitchenAsync()
    {
        var unsentItems = CartItems.Where(i => !i.IsSentToKitchen).ToList();
        if (!unsentItems.Any()) return;

        int? parsedTableId = null;
        if (!IsTakeaway && !string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
        {
            parsedTableId = tId;
        }

        var request = new SubmitPosOrderRequest
        {
            TableId = parsedTableId,
            CustomerId = SelectedMember?.Id,
            CustomerNote = IsTakeaway ? "Đơn mang về (POS)" : "Đơn ăn tại bàn (POS)",
            Items = unsentItems.Select(i => new CartItemValidateRequest
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                Note = i.Note,
                SelectedModifiers = i.SelectedModifiers ?? new List<int>(),
                SelectedComboItems = i.SelectedComboItems ?? new List<CartComboItemRequest>()
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

                foreach (var item in unsentItems)
                {
                    item.IsSentToKitchen = true;
                }
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
            catch
            {
                // Ignore
            }

            CartItems.Clear();
            ToastMessage = "Thanh toán thành công! Có thể in hóa đơn.";
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
                    SelectedModifiers = new List<int>(),
                    IsSentToKitchen = true
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
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }
}

