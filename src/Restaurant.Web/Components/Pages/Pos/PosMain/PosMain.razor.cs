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

    protected List<PizzaSalesConfiguration> PizzaConfigurations { get; set; } = new();
    protected PizzaSalesConfiguration? SelectedPizzaConfig { get; set; }
    protected bool IsPizzaConfigSelected { get; set; } = false;

    protected bool IsActiveCategoryPizza
    {
        get
        {
            var activeCat = Categories.FirstOrDefault(c => c.Id == ActiveCategoryId);
            if (activeCat != null)
            {
                if (activeCat.Name.Contains("Melts", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (activeCat.Name.Contains("Pizza", StringComparison.OrdinalIgnoreCase) ||
                    (activeCat.NameEn != null && activeCat.NameEn.Contains("Pizza", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return AllMenuItems.Any(m => m.CategoryId == ActiveCategoryId &&
                m.ModifierGroups != null &&
                m.ModifierGroups.Any(g => g.Id == 1 || g.Name.Contains("Kích thước", StringComparison.OrdinalIgnoreCase)));
        }
    }

    protected string ActiveCategoryName => Categories.FirstOrDefault(c => c.Id == ActiveCategoryId)?.DisplayName ?? "Món Ăn";

    protected string GetCategoryIcon(string name)
    {
        if (string.IsNullOrEmpty(name)) return "restaurant_menu";
        var n = name.ToLowerInvariant();
        if (n.Contains("đổi điểm") || n.Contains("loyalty") || n.Contains("quà tặng") || n.Contains("reward")) return "stars";
        if (n.Contains("pizza")) return "local_pizza";
        if (n.Contains("melts")) return "bakery_dining";
        if (n.Contains("mì") || n.Contains("pasta") || n.Contains("spaghetti")) return "ramen_dining";
        if (n.Contains("gà") || n.Contains("chicken")) return "kebab_dining";
        if (n.Contains("khai vị") || n.Contains("appetizer")) return "tapas";
        if (n.Contains("kèm") || n.Contains("side")) return "fastfood";
        if (n.Contains("salad")) return "nutrition";
        if (n.Contains("uống") || n.Contains("drink") || n.Contains("nước") || n.Contains("beverage")) return "local_bar";
        if (n.Contains("tráng miệng") || n.Contains("dessert") || n.Contains("kem") || n.Contains("bánh")) return "icecream";
        if (n.Contains("kid") || n.Contains("trẻ em")) return "child_care";
        if (n.Contains("combo")) return "lunch_dining";
        return "restaurant_menu";
    }

    protected int ActiveCategoryId { get; set; } = 0;
    protected bool ShowImages { get; set; } = false;
    protected string SearchQuery { get; set; } = string.Empty;

    protected List<OrderCartItemLocal> CartItems { get; set; } = new();
    protected decimal CartSubTotal => CartItems.Sum(i => i.Price * i.Quantity);
    protected decimal AppliedDiscountAmount { get; set; } = 0m;

    protected bool IsMenuLoading { get; set; } = true;
    protected bool IsMenuLoadingError { get; set; } = false;

    protected bool ShowModifierModal { get; set; } = false;
    protected MenuItemDetailDto? SelectedItemDetail { get; set; }
    protected List<ModifierGroupDto>? SelectedModifierGroups { get; set; }

    protected bool IsOptionBuilderActive { get; set; } = false;

    protected bool IsComboBuilderActive { get; set; } = false;
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
    protected bool ShowEmptyOrderCancelModal { get; set; } = false;
    
    protected bool ShowVoidItemModal { get; set; } = false;
    protected OrderCartItemLocal? VoidingItem { get; set; }

    protected bool ShowTableActionModal { get; set; } = false;
    protected string TableActionType { get; set; } = "Move";

    private static bool IsPizzaSauceGroup(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Contains("Sốt Nền", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Chọn Sốt Nền Pizza", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Sốt Pizza", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Select Pizza Sauce", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Pizza Sauce", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpiceGroup(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Contains("Mức Độ Cay", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Cay", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Spice Level", StringComparison.OrdinalIgnoreCase);
    }

    private static ModifierOptionDto? GetStandardSauceForPizza(MenuItemDto item, ModifierGroupDto sauceGroup)
    {
        if (sauceGroup.ModifierOptions == null || !sauceGroup.ModifierOptions.Any()) return null;

        var text = (item.Name + " " + item.Description).ToLowerInvariant();

        if (text.Contains("pesto"))
        {
            var opt = sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive && o.Name.Contains("Pesto", StringComparison.OrdinalIgnoreCase));
            if (opt != null) return opt;
        }

        if (text.Contains("bbq"))
        {
            var opt = sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive && o.Name.Contains("BBQ", StringComparison.OrdinalIgnoreCase));
            if (opt != null) return opt;
        }

        if (text.Contains("bơ tỏi") || text.Contains("garlic butter"))
        {
            var opt = sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive && o.Name.Contains("Bơ Tỏi", StringComparison.OrdinalIgnoreCase));
            if (opt != null) return opt;
        }

        if (text.Contains("tiêu đen") || text.Contains("black pepper"))
        {
            var opt = sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive && o.Name.Contains("Tiêu Đen", StringComparison.OrdinalIgnoreCase));
            if (opt != null) return opt;
        }

        return sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive && o.IsDefault)
               ?? sauceGroup.ModifierOptions.FirstOrDefault(o => o.IsActive);
    }

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

            Categories = (await categoriesTask ?? new List<CategoryDto>())
                .Where(c => c.IsActive &&
                            !c.Name.Contains("Pizza Ý Thượng hạng", StringComparison.OrdinalIgnoreCase) &&
                            !c.Name.Contains("Pizza Thượng Hạng", StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.SortOrder)
                .ToList();

            AllMenuItems = await menuItemsTask ?? new List<MenuItemDto>();
            AllModifierGroups = await modifiersTask ?? new List<ModifierGroupDto>();
            InitializePizzaConfigurations();

            // Re-link any items from removed categories to main pizza category
            var mainPizzaCat = Categories.FirstOrDefault(c =>
                c.Name.Contains("Pizza", StringComparison.OrdinalIgnoreCase) &&
                !c.Name.Contains("Melts", StringComparison.OrdinalIgnoreCase));

            if (mainPizzaCat != null)
            {
                foreach (var item in AllMenuItems.Where(m =>
                    m.CategoryName != null &&
                    (m.CategoryName.Contains("Pizza Ý Thượng hạng", StringComparison.OrdinalIgnoreCase) ||
                     m.CategoryName.Contains("Pizza Thượng Hạng", StringComparison.OrdinalIgnoreCase))))
                {
                    item.CategoryId = mainPizzaCat.Id;
                    item.CategoryName = mainPizzaCat.DisplayName;
                }
            }

            // Default to first real category if any, never 0 / 'Tất cả'
            if (ActiveCategoryId == 0 && Categories.Any())
            {
                ActiveCategoryId = Categories.First().Id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching POS menu data: {ex.Message}");
            IsMenuLoadingError = true;
        }
        finally
        {
            IsMenuLoading = false;
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
                            CartItems = ReconstructCartItemsFromOrder(activeOrder.Items);
                            if (SelectedMember == null && !string.IsNullOrEmpty(activeOrder.CustomerPhone))
                            {
                                try
                                {
                                    SelectedMember = await Http.GetFromJsonAsync<CustomerDto>($"api/v1/customers/by-phone/{activeOrder.CustomerPhone}");
                                }
                                catch { }
                            }
                            if (activeOrder.DiscountAmount > 0)
                            {
                                AppliedDiscountAmount = activeOrder.DiscountAmount;
                            }
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

    protected string GetCleanCategoryName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        int parenIndex = name.IndexOf('(');
        if (parenIndex > 0)
        {
            return name.Substring(0, parenIndex).Trim();
        }
        return name.Trim();
    }

    protected void HandleCategoryChanged(int categoryId)
    {
        ActiveCategoryId = categoryId;
        SelectedPizzaConfig = null;
        IsPizzaConfigSelected = false; // Reset to Step 1 (Select Size & Crust) when switching category
    }

    protected void HandleBackToConfigSelection()
    {
        SelectedPizzaConfig = null;
        IsPizzaConfigSelected = false;
        StateHasChanged();
    }
    protected void HandleImageToggle(bool show) => ShowImages = show;
    protected void HandleSearchQueryChanged(string query) => SearchQuery = query;

    protected void HandleSearchKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            HandleSearchEnterPressed();
        }
    }

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

    protected string? EditingCartItemId { get; set; }

    private void InitializePizzaConfigurations()
    {
        var sizeGroup = AllModifierGroups.FirstOrDefault(g => g.Id == 1 || g.Name.Contains("Kích thước", StringComparison.OrdinalIgnoreCase));
        var crustGroup = AllModifierGroups.FirstOrDefault(g => g.Id == 1002 || g.Name.Contains("Đế", StringComparison.OrdinalIgnoreCase));

        if (sizeGroup == null || crustGroup == null) return;

        var activeSizes = sizeGroup.ModifierOptions.Where(o => o.IsActive).OrderBy(o => o.SortOrder).ToList();
        var activeCrusts = crustGroup.ModifierOptions.Where(o => o.IsActive).OrderBy(o => o.SortOrder).ToList();

        var list = new List<PizzaSalesConfiguration>();

        foreach (var size in activeSizes)
        {
            string sizeShort = size.Name.Contains("Nhỏ") ? "Cỡ nhỏ" :
                               size.Name.Contains("Vừa") ? "Cỡ vừa" :
                               size.Name.Contains("Lớn") ? "Cỡ lớn" : size.Name;

            bool isSmall = size.Name.Contains("Nhỏ", StringComparison.OrdinalIgnoreCase);

            foreach (var crust in activeCrusts)
            {
                // Business rule: Viền phô mai / xúc xích không áp dụng cho Cỡ nhỏ (20cm)
                bool isStuffedCrust = crust.Name.Contains("Viền", StringComparison.OrdinalIgnoreCase);
                if (isSmall && isStuffedCrust)
                {
                    continue; // Skip invalid combination
                }

                string crustShort = crust.Name;
                if (crust.Name.Contains("Dày")) crustShort = "Đế dày";
                else if (crust.Name.Contains("Mỏng")) crustShort = "Đế mỏng";
                else if (crust.Name.Contains("Kéo Tay")) crustShort = "Đế kéo tay";
                else if (crust.Name.Contains("Phô Mai Nung")) crustShort = "Viền phô mai";
                else if (crust.Name.Contains("Xúc Xích")) crustShort = "Viền xúc xích";

                var config = new PizzaSalesConfiguration
                {
                    Id = $"{size.Id}_{crust.Id}",
                    SizeOptionId = size.Id,
                    SizeName = size.Name,
                    CrustOptionId = crust.Id,
                    CrustName = crust.Name,
                    Label = $"{sizeShort} – {crustShort}",
                    ExtraPrice = size.ExtraPrice + crust.ExtraPrice,
                    IsDefault = (size.IsDefault || size.Name.Contains("Vừa")) && (crust.IsDefault || crust.Name.Contains("Dày"))
                };
                list.Add(config);
            }
        }

        PizzaConfigurations = list;

        // Default to null so no configuration card is pre-selected
        SelectedPizzaConfig = null;
        IsPizzaConfigSelected = false;
    }

    protected void SelectPizzaConfig(PizzaSalesConfiguration config)
    {
        SelectedPizzaConfig = config;
        IsPizzaConfigSelected = true; // Sau khi chọn cỡ - đế bánh thì chuyển thành giao diện chọn bánh ở dưới
        StateHasChanged();
    }

    protected void HandleMenuItemClicked(MenuItemDto item)
    {
        // Check if item is a loyalty point redemption item
        if (item.PointsCost > 0 || item.IsRewardItem)
        {
            if (SelectedMember == null)
            {
                _ = ShowToastAsync("Vui lòng chọn hoặc tra cứu Thành Viên trước khi chọn món đổi điểm tích lũy!");
                ShowMemberModal = true;
                return;
            }

            int currentUsedPoints = CartItems.Where(i => i.IsRedemption).Sum(i => i.PointsCost * i.Quantity);
            int requiredPoints = item.PointsCost;
            if (currentUsedPoints + requiredPoints > SelectedMember.AvailablePoints)
            {
                _ = ShowToastAsync($"Thành viên {SelectedMember.FullName} chỉ còn {SelectedMember.AvailablePoints - currentUsedPoints} điểm khả dụng, không đủ {requiredPoints} điểm để đổi món này!");
                return;
            }

            var existingRedeem = CartItems.FirstOrDefault(i => i.MenuItemId == item.Id && i.IsRedemption && !i.IsSentToKitchen);
            if (existingRedeem != null)
            {
                existingRedeem.Quantity += 1;
            }
            else
            {
                CartItems.Add(new OrderCartItemLocal
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Price = 0m,
                    Quantity = 1,
                    IsRedemption = true,
                    PointsCost = item.PointsCost,
                    NeedsConfiguration = false,
                    IsSentToKitchen = false
                });
            }
            _ = ShowToastAsync($"Đã thêm '{item.Name}' (Đổi {item.PointsCost} điểm) vào giỏ hàng!");
            StateHasChanged();
            return;
        }

        if (item.IsCombo)
        {
            IsComboBuilderActive = true;
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
                    IsComboBuilderActive = false;
                }
                finally
                {
                    IsComboLoading = false;
                    StateHasChanged();
                }
            });
            return;
        }

        // Check if item is Pizza
        bool isPizza = item.ModifierGroups != null && item.ModifierGroups.Any(g => g.Id == 1 || g.Name.Contains("Kích thước", StringComparison.OrdinalIgnoreCase));
        if (isPizza && SelectedPizzaConfig != null)
        {
            AddPizzaToCartWithConfig(item, SelectedPizzaConfig);
            return;
        }

        // Other items: Add with default options
        AddMenuItemToCartWithDefaults(item);
    }

    private void AddPizzaToCartWithConfig(MenuItemDto item, PizzaSalesConfiguration config)
    {
        var selectedModIds = new List<int> { config.SizeOptionId, config.CrustOptionId };
        var modNames = new List<string> { config.SizeName, config.CrustName };
        decimal extraPriceTotal = config.ExtraPrice;

        // Satisfy other required groups (e.g. Sauce - Group 1003) with default option
        if (item.ModifierGroups != null && item.ModifierGroups.Any())
        {
            foreach (var group in item.ModifierGroups)
            {
                if (group.Id == 1 || group.Id == 1002 || 
                    group.Name.Contains("Kích thước", StringComparison.OrdinalIgnoreCase) || 
                    group.Name.Contains("Đế", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Handled by config
                }

                // Remove spice completely
                if (IsSpiceGroup(group.Name))
                {
                    continue;
                }

                if (group.IsRequired)
                {
                    ModifierOptionDto? def = null;
                    if (IsPizzaSauceGroup(group.Name))
                    {
                        def = GetStandardSauceForPizza(item, group);
                    }
                    else
                    {
                        def = group.ModifierOptions.FirstOrDefault(o => o.IsActive && o.IsDefault)
                              ?? group.ModifierOptions.FirstOrDefault(o => o.IsActive);
                    }

                    if (def != null)
                    {
                        selectedModIds.Add(def.Id);
                        // Standardize Pizza sauce: never show sauce name in item modifiers list, and do not charge extra
                        if (!IsPizzaSauceGroup(group.Name))
                        {
                            modNames.Add(def.Name + (def.ExtraPrice > 0 ? $" (+{def.ExtraPrice:N0}đ)" : ""));
                            extraPriceTotal += def.ExtraPrice;
                        }
                    }
                }
            }
        }

        decimal unitPrice = item.Price + extraPriceTotal;

        // Check if identical un-sent item configuration already exists in the cart
        var existing = CartItems.FirstOrDefault(i =>
            !i.IsSentToKitchen &&
            i.MenuItemId == item.Id &&
            !i.NeedsConfiguration &&
            string.IsNullOrEmpty(i.Note) &&
            AreModifiersIdentical(i.SelectedModifiers, selectedModIds));

        if (existing != null)
        {
            existing.Quantity++;
            // No toast notification on item quantity increase
        }
        else
        {
            var newItem = new OrderCartItemLocal
            {
                MenuItemId = item.Id,
                Name = item.Name,
                Price = unitPrice,
                Quantity = 1,
                SelectedModifiers = selectedModIds,
                ModifierNames = modNames,
                NeedsConfiguration = false,
                IsSentToKitchen = false
            };
            CartItems.Add(newItem);
            // No toast notification on item addition
        }
    }

    private void AddMenuItemToCartWithDefaults(MenuItemDto item)
    {
        var selectedModIds = new List<int>();
        var modNames = new List<string>();
        decimal extraPriceTotal = 0m;
        bool needsConfig = false;

        if (item.ModifierGroups != null && item.ModifierGroups.Any())
        {
            foreach (var group in item.ModifierGroups)
            {
                if (IsSpiceGroup(group.Name)) continue;

                if (IsPizzaSauceGroup(group.Name))
                {
                    var stdSauce = GetStandardSauceForPizza(item, group);
                    if (stdSauce != null)
                    {
                        selectedModIds.Add(stdSauce.Id);
                    }
                    continue;
                }

                var defaultOpts = group.ModifierOptions.Where(o => o.IsActive && o.IsDefault).ToList();
                if (defaultOpts.Any())
                {
                    foreach (var def in defaultOpts.Take(group.MaxSelections))
                    {
                        selectedModIds.Add(def.Id);
                        modNames.Add(def.Name + (def.ExtraPrice > 0 ? $" (+{def.ExtraPrice:N0}đ)" : ""));
                        extraPriceTotal += def.ExtraPrice;
                    }
                }
                else if (group.IsRequired)
                {
                    // Fallback to first available active option to satisfy required group
                    var firstOpt = group.ModifierOptions.FirstOrDefault(o => o.IsActive);
                    if (firstOpt != null && group.MinSelections > 0)
                    {
                        selectedModIds.Add(firstOpt.Id);
                        if (!IsPizzaSauceGroup(group.Name))
                        {
                            modNames.Add(firstOpt.Name + (firstOpt.ExtraPrice > 0 ? $" (+{firstOpt.ExtraPrice:N0}đ)" : ""));
                        }
                        extraPriceTotal += firstOpt.ExtraPrice;
                    }
                    else
                    {
                        needsConfig = true;
                    }
                }
            }
        }

        decimal unitPrice = item.Price + extraPriceTotal;

        // Check if an identical un-sent item configuration already exists in the cart
        var existing = CartItems.FirstOrDefault(i =>
            !i.IsSentToKitchen &&
            i.MenuItemId == item.Id &&
            !i.NeedsConfiguration &&
            !needsConfig &&
            string.IsNullOrEmpty(i.Note) &&
            AreModifiersIdentical(i.SelectedModifiers, selectedModIds));

        if (existing != null)
        {
            existing.Quantity++;
            // No toast notification on item quantity increase
        }
        else
        {
            var newItem = new OrderCartItemLocal
            {
                MenuItemId = item.Id,
                Name = item.Name,
                Price = unitPrice,
                Quantity = 1,
                SelectedModifiers = selectedModIds,
                ModifierNames = modNames,
                NeedsConfiguration = needsConfig,
                IsSentToKitchen = false
            };
            CartItems.Add(newItem);
            // No toast notification on item addition
        }
    }

    protected void HandleCartItemClicked(OrderCartItemLocal item)
    {
        // Don't allow modifying sent items or combos here
        if (item.IsSentToKitchen || (item.SelectedComboItems != null && item.SelectedComboItems.Any()))
        {
            return;
        }

        var menuItem = AllMenuItems.FirstOrDefault(m => m.Id == item.MenuItemId);
        if (menuItem == null)
        {
            return;
        }

        // Clean modifier groups: filter out pizza sauce and spice level groups
        var groups = menuItem.ModifierGroups?
            .Where(g => !IsPizzaSauceGroup(g.Name) && !IsSpiceGroup(g.Name))
            .ToList() ?? new List<ModifierGroupDto>();

        SelectedItemDetail = new MenuItemDetailDto
        {
            Id = menuItem.Id,
            CategoryId = menuItem.CategoryId,
            CategoryName = menuItem.CategoryName,
            Name = menuItem.Name,
            Price = menuItem.Price,
            Description = menuItem.Description,
            ImageUrl = menuItem.ImageUrl,
            IsCombo = menuItem.IsCombo,
            ModifierGroups = groups
        };
        SelectedModifierGroups = groups;
        EditingCartItemId = item.Id;
        IsOptionBuilderActive = true;
    }

    protected void HandleSaveModifyItem((List<int> newSelectedModIds, string? note) result)
    {
        if (string.IsNullOrEmpty(EditingCartItemId)) return;

        var targetItem = CartItems.FirstOrDefault(i => i.Id == EditingCartItemId);
        if (targetItem == null) return;

        var menuItem = AllMenuItems.FirstOrDefault(m => m.Id == targetItem.MenuItemId);
        decimal basePrice = menuItem?.Price ?? targetItem.Price;

        var modNames = new List<string>();
        decimal extraPriceTotal = 0m;

        // Preserve any hidden required sauce option that was in targetItem.SelectedModifiers or standard recipe
        var preservedSauceIds = new List<int>();
        if (menuItem?.ModifierGroups != null)
        {
            foreach (var group in menuItem.ModifierGroups.Where(g => IsPizzaSauceGroup(g.Name)))
            {
                if (targetItem.SelectedModifiers != null)
                {
                    var optInGroup = group.ModifierOptions.FirstOrDefault(o => targetItem.SelectedModifiers.Contains(o.Id));
                    if (optInGroup != null)
                    {
                        preservedSauceIds.Add(optInGroup.Id);
                    }
                }

                if (!preservedSauceIds.Any())
                {
                    var std = GetStandardSauceForPizza(menuItem, group);
                    if (std != null)
                    {
                        preservedSauceIds.Add(std.Id);
                    }
                }
            }
        }

        var finalModIds = new List<int>(result.newSelectedModIds ?? new List<int>());
        foreach (var sauceId in preservedSauceIds)
        {
            if (!finalModIds.Contains(sauceId))
            {
                finalModIds.Add(sauceId);
            }
        }

        if (finalModIds.Any() && SelectedModifierGroups != null)
        {
            foreach (var group in SelectedModifierGroups)
            {
                if (IsPizzaSauceGroup(group.Name) || IsSpiceGroup(group.Name))
                {
                    continue;
                }

                foreach (var opt in group.ModifierOptions.Where(o => finalModIds.Contains(o.Id)))
                {
                    modNames.Add(opt.Name + (opt.ExtraPrice > 0 ? $" (+{opt.ExtraPrice:N0}đ)" : ""));
                    extraPriceTotal += opt.ExtraPrice;
                }
            }
        }

        // Apply changes to the EXACT row being edited
        targetItem.SelectedModifiers = finalModIds;
        targetItem.ModifierNames = modNames;
        targetItem.Note = string.IsNullOrWhiteSpace(result.note) ? null : result.note.Trim();
        targetItem.Price = basePrice + extraPriceTotal;
        targetItem.NeedsConfiguration = false;

        // Return to Menu view
        IsOptionBuilderActive = false;
        EditingCartItemId = null;
        SelectedItemDetail = null;
        _ = ShowToastAsync($"Đã cập nhật tùy chọn cho {targetItem.Name}");
    }

    protected void HandleCancelModifyItem()
    {
        IsOptionBuilderActive = false;
        EditingCartItemId = null;
        SelectedItemDetail = null;
    }

    private static bool AreModifiersIdentical(List<int>? list1, List<int>? list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;
        var s1 = list1.OrderBy(x => x).ToList();
        var s2 = list2.OrderBy(x => x).ToList();
        return s1.SequenceEqual(s2);
    }

    protected void HandleConfirmCombo((ComboDetailDto combo, int qty, List<CartComboItemRequest> selectedItems, string? note) result)
    {
        var comboNames = new List<string>();
        decimal additionalTotal = 0m;

        foreach (var req in result.selectedItems)
        {
            var optName = "";
            if (result.combo.Groups != null)
            {
                foreach (var group in result.combo.Groups)
                {
                    var opt = group.Items.FirstOrDefault(i => (req.ComboItemId > 0 && i.Id == req.ComboItemId) || i.MenuItemId == req.MenuItemId);
                    if (opt != null)
                    {
                        optName = opt.DisplayName;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(optName) && result.combo.FixedAndAddOnItems != null)
            {
                var fixedOpt = result.combo.FixedAndAddOnItems.FirstOrDefault(i => (req.ComboItemId > 0 && i.Id == req.ComboItemId) || i.MenuItemId == req.MenuItemId);
                if (fixedOpt != null)
                {
                    optName = fixedOpt.DisplayName;
                }
            }

            additionalTotal += req.AdditionalPrice * req.Quantity;

            if (!string.IsNullOrEmpty(optName))
            {
                var extraText = req.AdditionalPrice > 0 ? $" (+{req.AdditionalPrice:N0}đ)" : "";
                comboNames.Add($"{req.Quantity}x {optName}{extraText}");
            }
        }

        decimal comboUnitPrice = result.combo.Price + (result.qty > 0 ? (additionalTotal / result.qty) : 0m);
        string? cleanNote = string.IsNullOrWhiteSpace(result.note) ? null : result.note.Trim();

        // Check if identical combo already exists in Cart (not yet sent to kitchen)
        var existingCombo = CartItems.FirstOrDefault(i =>
            !i.IsSentToKitchen &&
            i.MenuItemId == result.combo.Id &&
            string.Equals(i.Note, cleanNote, StringComparison.Ordinal) &&
            AreComboSelectionsIdentical(i.SelectedComboItems, result.selectedItems));

        if (existingCombo != null)
        {
            existingCombo.Quantity += result.qty;
            if (existingCombo.SelectedComboItems != null)
            {
                foreach (var item in existingCombo.SelectedComboItems)
                {
                    var matching = result.selectedItems.FirstOrDefault(s => (s.ComboItemId > 0 && s.ComboItemId == item.ComboItemId) || s.MenuItemId == item.MenuItemId);
                    if (matching != null)
                    {
                        item.Quantity += matching.Quantity;
                    }
                }
            }
        }
        else
        {
            CartItems.Add(new OrderCartItemLocal
            {
                MenuItemId = result.combo.Id,
                Name = result.combo.Name,
                Price = comboUnitPrice,
                Quantity = result.qty,
                Note = cleanNote,
                IsSentToKitchen = false,
                SelectedModifiers = new List<int>(),
                SelectedComboItems = result.selectedItems,
                ComboItemNames = comboNames
            });
        }

        IsComboBuilderActive = false;
        SelectedComboDetail = null;
        // No toast notification on combo addition
    }

    protected void HandleCancelComboBuilder()
    {
        IsComboBuilderActive = false;
        SelectedComboDetail = null;
    }

    private static bool AreComboSelectionsIdentical(List<CartComboItemRequest>? list1, List<CartComboItemRequest>? list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;

        var sorted1 = list1.OrderBy(x => x.ComboItemId).ThenBy(x => x.MenuItemId).ToList();
        var sorted2 = list2.OrderBy(x => x.ComboItemId).ThenBy(x => x.MenuItemId).ToList();

        for (int i = 0; i < sorted1.Count; i++)
        {
            if (sorted1[i].ComboItemId != sorted2[i].ComboItemId ||
                sorted1[i].MenuItemId != sorted2[i].MenuItemId ||
                sorted1[i].Quantity != sorted2[i].Quantity ||
                sorted1[i].AdditionalPrice != sorted2[i].AdditionalPrice)
            {
                return false;
            }
        }
        return true;
    }

    protected void HandleCartQuantityChanged(OrderCartItemLocal item) { }
    protected void HandleCartItemRemoved(OrderCartItemLocal item) => CartItems.Remove(item);

    protected void HandleItemVoidRequested(OrderCartItemLocal item)
    {
        VoidingItem = item;
        ShowVoidItemModal = true;
    }

    protected async Task HandleVoidCompleted((bool Success, string Reason, string ManagerPin) voidArgs)
    {
        if (voidArgs.Success && VoidingItem != null)
        {
            if (VoidingItem.IsSentToKitchen && VoidingItem.OrderItemId.HasValue && ActiveOrderId > 0)
            {
                try
                {
                    var request = new VoidOrderItemRequest 
                    { 
                        Reason = string.IsNullOrWhiteSpace(voidArgs.Reason) ? "Yêu cầu hủy từ POS" : voidArgs.Reason,
                        ManagerPin = voidArgs.ManagerPin
                    };

                    var response = await Http.PostAsJsonAsync($"api/v1/pos/orders/{ActiveOrderId}/items/{VoidingItem.OrderItemId.Value}/void", request);
                    if (response.IsSuccessStatusCode)
                    {
                        _ = ShowToastAsync($"Đã hủy món {VoidingItem.Name} trên hệ thống thành công!");
                        // Reload order items to reflect current status
                        if (int.TryParse(TableId, out _))
                        {
                            await CheckExistingTableOrderAsync();
                        }
                        else
                        {
                            await LoadExistingTakeawayOrderAsync(ActiveOrderId);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _ = ShowToastAsync($"Không thể hủy món {VoidingItem.Name} trên server: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error voiding item: {ex.Message}");
                    _ = ShowToastAsync($"Lỗi khi hủy món: {ex.Message}");
                }
            }
            else
            {
                CartItems.Remove(VoidingItem);
                _ = ShowToastAsync($"Đã hủy món {VoidingItem.Name} khỏi giỏ hàng thành công!");
            }
        }
        VoidingItem = null;
    }

    // 1. Action Toolbar: Member Confirmation
    protected void HandleMemberConfirmed(CustomerDto member)
    {
        SelectedMember = member;
        _ = ShowToastAsync($"Đã xác nhận thành viên {member.FullName} ({member.TierName}{(member.DiscountPercent > 0 ? $" - Giảm {member.DiscountPercent:G29}% món lẻ" : "")}) cho đơn!");
        StateHasChanged();
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

    // 3. Action Toolbar: Print Pre-Bill (Luồng bắt buộc: Kiểm tra đơn hàng -> Gửi bếp -> In hóa đơn)
    protected async Task HandlePrintPreBill()
    {
        if (IsProcessing) return;

        // 1. Kiểm tra đơn hàng có món hay không
        if (CartItems == null || !CartItems.Any())
        {
            _ = ShowToastAsync("Đơn hàng đang trống, không thể in hóa đơn!");
            return;
        }

        // 2. Kiểm tra các món đã đáp ứng yêu cầu cấu hình bắt buộc hay chưa
        var unconfigured = CartItems.FirstOrDefault(i => i.NeedsConfiguration);
        if (unconfigured != null)
        {
            _ = ShowToastAsync($"Món '{unconfigured.Name}' chưa hoàn tất cấu hình tùy chọn bắt buộc. Vui lòng bấm vào món để chỉnh sửa trước khi in hóa đơn!");
            return;
        }

        IsProcessing = true;
        StateHasChanged();

        try
        {
            // 4 & 5. Lưu/xác nhận các thay đổi và gửi các món chưa gửi xuống bếp
            var unsentItems = CartItems.Where(i => !i.IsSentToKitchen).ToList();
            if (unsentItems.Any())
            {
                int? parsedTableId = null;
                if (!IsTakeaway && !string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
                {
                    parsedTableId = tId;
                }

                var submitRequest = new SubmitPosOrderRequest
                {
                    TableId = parsedTableId,
                    CustomerId = SelectedMember?.Id,
                    CustomerName = SelectedMember?.FullName,
                    CustomerPhone = SelectedMember?.PhoneNumber,
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

                var submitResponse = await Http.PostAsJsonAsync("api/v1/pos/orders/submit", submitRequest);
                if (!submitResponse.IsSuccessStatusCode)
                {
                    var error = await submitResponse.Content.ReadAsStringAsync();
                    _ = ShowToastAsync($"Gửi bếp thất bại: {error}. Chưa thể in hóa đơn!");
                    return; // DỪNG LẠI: Không giả vờ thành công, không in hóa đơn!
                }

                var submitResult = await submitResponse.Content.ReadFromJsonAsync<ApiResponseWrapper<SubmitPosOrderResponse>>();
                if (submitResult?.Data != null)
                {
                    ActiveOrderId = submitResult.Data.OrderId;
                    CurrentOrderCode = submitResult.Data.OrderCode;
                }

                foreach (var item in unsentItems)
                {
                    item.IsSentToKitchen = true;
                }
            }

            if (ActiveOrderId <= 0)
            {
                _ = ShowToastAsync("Không tìm thấy mã đơn hàng để in hóa đơn!");
                return;
            }

            // 6. Backend xác nhận gửi bếp thành công -> Thực hiện in hóa đơn tạm tính (Pre-bill)
            var printResponse = await Http.PostAsync($"api/v1/pos/orders/{ActiveOrderId}/print-temp-bill", null);
            if (!printResponse.IsSuccessStatusCode)
            {
                // Thể hiện rõ: Đã gửi bếp, nhưng in hóa đơn thất bại; cho phép bấm in lại mà không gửi trùng món
                _ = ShowToastAsync("Đã gửi bếp, nhưng in hóa đơn thất bại! Vui lòng bấm 'In Hóa Đơn' để thử lại.");
                return;
            }

            // 7. Hoàn thành và gửi bếp + In hóa đơn -> Quay về màn hình Sơ đồ bàn kèm mã đơn
            if (IsTakeaway)
            {
                Navigation.NavigateTo("/pos/takeaway");
            }
            else
            {
                Navigation.NavigateTo($"/pos/tables?printOrderId={ActiveOrderId}");
            }
        }
        catch (Exception ex)
        {
            _ = ShowToastAsync("Lỗi xử lý gửi bếp / in hóa đơn: " + ex.Message);
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    // 4. Action Toolbar: Cancel Order Confirmation
    protected async Task HandleOrderCancelledAsync((string Reason, string ManagerPin) cancelArgs)
    {
        try
        {
            if (ActiveOrderId > 0)
            {
                var request = new CancelOrderRequest 
                { 
                    OrderId = ActiveOrderId, 
                    Reason = cancelArgs.Reason,
                    ManagerPin = cancelArgs.ManagerPin
                };

                var response = await Http.PostAsJsonAsync($"api/v1/pos/orders/{ActiveOrderId}/cancel", request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _ = ShowToastAsync($"Không thể hủy đơn trên server: {response.StatusCode}");
                    return; // DO NOT clear cart or navigate back on failure!
                }

                if (string.IsNullOrWhiteSpace(cancelArgs.ManagerPin))
                {
                    _ = ShowToastAsync("Đã gửi yêu cầu hủy đơn tới Trung tâm phê duyệt!");
                    await Task.Delay(1000);
                    HandleHeaderBackNav();
                    return;
                }
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

        // If cart is completely empty, show confirmation modal to cancel order and release table
        if (CartItems == null || !CartItems.Any())
        {
            ShowEmptyOrderCancelModal = true;
            return;
        }

        // Check if any items need configuration
        var unconfigured = CartItems.FirstOrDefault(i => i.NeedsConfiguration);
        if (unconfigured != null)
        {
            _ = ShowToastAsync($"Món '{unconfigured.Name}' chưa hoàn tất cấu hình tùy chọn bắt buộc. Vui lòng bấm vào món để chỉnh sửa trước khi hoàn thành!");
            return;
        }

        IsProcessing = true;
        try
        {
            if (CartItems.Any(i => !i.IsSentToKitchen))
            {
                await HandleSendToKitchenAsync();
            }

            // Only navigate away if all items were sent successfully
            if (!CartItems.Any(i => !i.IsSentToKitchen))
            {
                HandleHeaderBackNav();
            }
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

    protected async Task ConfirmCancelEmptyOrderAsync()
    {
        if (IsProcessing) return;
        IsProcessing = true;
        try
        {
            if (ActiveOrderId > 0)
            {
                try
                {
                    var request = new CancelOrderRequest { OrderId = ActiveOrderId, Reason = "Hủy lượt gọi món khi giỏ hàng trống" };
                    await Http.PostAsJsonAsync($"api/v1/pos/orders/{ActiveOrderId}/cancel", request);
                }
                catch
                {
                    // Ignore if manager role required or handled by clear
                }
            }

            if (!IsTakeaway && !string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
            {
                try
                {
                    await Http.PostAsync($"api/v1/pos/tables/{tId}/clear", null);
                }
                catch
                {
                    // Ignore if already cleared
                }
            }

            CartItems.Clear();
            ShowEmptyOrderCancelModal = false;
            HandleHeaderBackNav();
        }
        catch (Exception ex)
        {
            _ = ShowToastAsync("Lỗi khi hủy đơn hàng: " + ex.Message);
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    protected async Task HandleSendToKitchenAsync()
    {
        var unsentItems = CartItems.Where(i => !i.IsSentToKitchen).ToList();
        if (!unsentItems.Any()) return;

        // REQUIREMENT: Một món chưa cấu hình đủ không được phép vượt qua validation để gửi bếp
        var unconfigured = unsentItems.FirstOrDefault(i => i.NeedsConfiguration);
        if (unconfigured != null)
        {
            _ = ShowToastAsync($"Món '{unconfigured.Name}' chưa hoàn tất cấu hình tùy chọn bắt buộc. Vui lòng bấm vào món để chỉnh sửa trước khi gửi bếp!");
            return;
        }

        int? parsedTableId = null;
        if (!IsTakeaway && !string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
        {
            parsedTableId = tId;
        }

        var request = new SubmitPosOrderRequest
        {
            TableId = parsedTableId,
            CustomerId = SelectedMember?.Id,
            CustomerName = SelectedMember?.FullName,
            CustomerPhone = SelectedMember?.PhoneNumber,
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
                CartItems = ReconstructCartItemsFromOrder(order.Items);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading takeaway order {orderId}: {ex.Message}");
        }
    }

    private List<OrderCartItemLocal> ReconstructCartItemsFromOrder(List<TakeawayOrderItemDto>? orderItems)
    {
        if (orderItems == null || !orderItems.Any())
        {
            return new List<OrderCartItemLocal>();
        }

        // Active non-voided items (Status != 1 Voided)
        var activeItems = orderItems.Where(i => i.Status != 1).ToList();
        var reconstructed = new List<OrderCartItemLocal>();

        // Find combo headers (items that have children pointing to their OrderItemId)
        var childMap = activeItems
            .Where(i => i.ParentItemId.HasValue)
            .GroupBy(i => i.ParentItemId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process top-level items (ParentItemId == null)
        foreach (var item in activeItems.Where(i => !i.ParentItemId.HasValue))
        {
            var cartItem = new OrderCartItemLocal
            {
                OrderItemId = item.OrderItemId,
                MenuItemId = item.MenuItemId,
                Name = item.MenuItemName,
                Price = item.UnitPrice,
                Quantity = item.Quantity,
                Note = item.Note,
                IsSentToKitchen = true,
                SelectedModifiers = item.ModifierIds ?? new List<int>(),
                ModifierNames = item.Modifiers?.Where(m => !m.Contains("Sốt", StringComparison.OrdinalIgnoreCase) &&
                                                           !m.Contains("Sauce", StringComparison.OrdinalIgnoreCase) &&
                                                           !m.Contains("Cay", StringComparison.OrdinalIgnoreCase) &&
                                                           !m.Contains("Spice", StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<string>()
            };

            // If this item has children, it's a Combo header
            if (childMap.TryGetValue(item.OrderItemId, out var children))
            {
                cartItem.IsCombo = true;
                cartItem.SelectedComboItems = children.Select(c => new CartComboItemRequest
                {
                    ComboItemId = c.ComboGroupId ?? 0,
                    MenuItemId = c.MenuItemId,
                    Quantity = c.Quantity,
                    AdditionalPrice = c.UnitPrice
                }).ToList();

                cartItem.ComboItemNames = children.Select(c =>
                {
                    var extraText = c.UnitPrice > 0 ? $" (+{c.UnitPrice:N0}đ)" : "";
                    return $"{c.Quantity}x {c.MenuItemName}{extraText}";
                }).ToList();
            }
            else
            {
                var matched = AllMenuItems.FirstOrDefault(m => m.Id == item.MenuItemId);
                if (matched != null && matched.IsCombo)
                {
                    cartItem.IsCombo = true;
                }
                if (item.UnitPrice == 0 && (item.MenuItemName.Contains("Đổi") || (matched != null && matched.IsRewardItem)))
                {
                    cartItem.IsRedemption = true;
                    cartItem.PointsCost = matched?.PointsCost ?? 0;
                }
            }

            reconstructed.Add(cartItem);
        }

        // Fallback: If any orphan children exist whose parent was already voided/not found, add them
        var processedChildIds = childMap.Values.SelectMany(x => x).Select(x => x.OrderItemId).ToHashSet();
        foreach (var orphan in activeItems.Where(i => i.ParentItemId.HasValue && !processedChildIds.Contains(i.OrderItemId)))
        {
            reconstructed.Add(new OrderCartItemLocal
            {
                OrderItemId = orphan.OrderItemId,
                MenuItemId = orphan.MenuItemId,
                Name = orphan.MenuItemName,
                Price = orphan.UnitPrice,
                Quantity = orphan.Quantity,
                Note = orphan.Note,
                IsSentToKitchen = true,
                SelectedModifiers = orphan.ModifierIds ?? new List<int>(),
                ModifierNames = orphan.Modifiers?.Where(m => !m.Contains("Sốt", StringComparison.OrdinalIgnoreCase) &&
                                                             !m.Contains("Sauce", StringComparison.OrdinalIgnoreCase) &&
                                                             !m.Contains("Cay", StringComparison.OrdinalIgnoreCase) &&
                                                             !m.Contains("Spice", StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<string>()
            });
        }

        return reconstructed;
    }

    private class ApiResponseWrapper<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }
}

public class PizzaSalesConfiguration
{
    public string Id { get; set; } = string.Empty;
    public int SizeOptionId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public int CrustOptionId { get; set; }
    public string CrustName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsDefault { get; set; }
}

