using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Restaurant.Application.DTOs;
using Restaurant.Web.Components.Pages.Pos.Components;
using Restaurant.Web.Components.Pages.Pos.Components.OrderCart;

namespace Restaurant.Web.Components.Pages.Pos.PosMain;

public partial class PosMain : IAsyncDisposable
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public HttpClient Http { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string TableId { get; set; } = default!;

    protected string CurrentTableName { get; set; } = "Chọn bàn...";
    protected string CurrentOrderCode { get; set; } = "N/A";
    
    protected bool ShowAlert { get; set; } = false;
    protected string NewOrderTableName { get; set; } = "";

    protected List<CategoryDto> Categories { get; set; } = new();
    protected List<MenuItemDto> AllMenuItems { get; set; } = new();
    
    protected int ActiveCategoryId { get; set; } = 0; // 0 = All
    protected bool ShowImages { get; set; } = true;
    
    protected List<OrderCartItemLocal> CartItems { get; set; } = new();
    
    protected List<MenuItemDto> FilteredMenuItems => 
        ActiveCategoryId == 0 
            ? AllMenuItems 
            : AllMenuItems.Where(m => m.CategoryId == ActiveCategoryId).ToList();

    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(TableId))
        {
            CurrentTableName = $"Bàn {TableId}";
            CurrentOrderCode = $"#ORD-{DateTime.Now.Ticks % 10000}";
        }

        try
        {
            var categoriesData = await Http.GetFromJsonAsync<List<CategoryDto>>("api/v1/categories");
            if (categoriesData != null)
            {
                Categories = categoriesData;
            }

            var menuItemsData = await Http.GetFromJsonAsync<List<MenuItemDto>>("api/v1/menu-items/active");
            if (menuItemsData != null)
            {
                AllMenuItems = menuItemsData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
        }

        // Hub Connection setup
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/restaurant"))
            .Build();

        hubConnection.On<string, string>("NewQrOrderReceived", (tableId, tableName) =>
        {
            ShowAlert = true;
            NewOrderTableName = tableName;
            InvokeAsync(StateHasChanged);
        });

        await hubConnection.StartAsync();
    }
    
    protected void HandleCategoryChanged(int categoryId)
    {
        ActiveCategoryId = categoryId;
    }
    
    protected void HandleImageToggle(bool show)
    {
        ShowImages = show;
    }
    
    protected bool ShowComboModal { get; set; } = false;
    protected MenuItemDetailDto? SelectedItemDetail { get; set; }
    protected List<ModifierGroupDto>? SelectedModifierGroups { get; set; }

    protected void HandleMenuItemClicked(MenuItemDto item)
    {
        try
        {
            // Always fetch detail to see if it has modifiers
            // For QR we used QrOrderController, here we can use QrOrderController's GetMenuItemDetail logic or create our own.
            // Since we didn't expose GetMenuItemDetail in PosOrdersController, we'll try to fetch from API or just check if it's a combo/has modifiers.
            // Actually, we can fetch from MenuItemsController ? Wait, MenuItemsController only returns base items.
            // Let's use the QR endpoint for simplicity: GET /api/v1/qr/orders/status ? No, that's status.
            // Is there an API for menu item detail? 
            // Wait, QrMenuController has GET /api/v1/qr/menu/items/{id} ? I need to check QrMenuController.
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
        }

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

    protected void HandleCartQuantityChanged(OrderCartItemLocal item)
    {
        // Object reference updated directly by OrderCart component
    }
    
    protected void HandleCartItemRemoved(OrderCartItemLocal item)
    {
        CartItems.Remove(item);
    }
    
    protected async Task HandleSendToKitchen()
    {
        if (!CartItems.Any()) return;
        
        int parsedTableId = 1;
        if (!string.IsNullOrEmpty(TableId) && int.TryParse(TableId, out int tId))
        {
            parsedTableId = tId;
        }

        var request = new SubmitPosOrderRequest
        {
            TableId = parsedTableId,
            CustomerNote = "Đơn gọi từ POS",
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
                CartItems.Clear();
                Console.WriteLine("Sent order to kitchen successfully!");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to send order: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending order: {ex.Message}");
        }
    }
    
    protected void HandlePay()
    {
        Console.WriteLine("Initiating payment...");
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
