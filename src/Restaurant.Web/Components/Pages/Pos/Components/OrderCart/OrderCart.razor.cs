using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Restaurant.Web.Components.Pages.Pos.Components.OrderCart;

public class OrderCartItemLocal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int? OrderItemId { get; set; }
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public List<int>? SelectedModifiers { get; set; }
    public List<string>? ModifierNames { get; set; }
    public List<Restaurant.Application.DTOs.CartComboItemRequest>? SelectedComboItems { get; set; }
    public List<string>? ComboItemNames { get; set; }
    public string? Note { get; set; }
    public bool IsSentToKitchen { get; set; } = false;
    public bool NeedsConfiguration { get; set; } = false;
}

public partial class OrderCart
{
    [Parameter] public string TableName { get; set; } = "Mang về";
    [Parameter] public string OrderCode { get; set; } = "#NEW";
    [Parameter] public List<OrderCartItemLocal> Items { get; set; } = new();
    [Parameter] public string? SelectedItemId { get; set; }
    [Parameter] public bool IsTakeaway { get; set; } = false;
    [Parameter] public decimal AppliedDiscount { get; set; } = 0m;
    
    [Parameter] public EventCallback<OrderCartItemLocal> OnQuantityChanged { get; set; }
    [Parameter] public EventCallback<OrderCartItemLocal> OnItemRemoved { get; set; }
    [Parameter] public EventCallback<OrderCartItemLocal> OnItemVoidRequested { get; set; }
    [Parameter] public EventCallback<OrderCartItemLocal> OnItemClicked { get; set; }
    [Parameter] public EventCallback OnSendToKitchen { get; set; }
    [Parameter] public EventCallback OnPay { get; set; }
    [Parameter] public EventCallback OnTransferTable { get; set; }
    [Parameter] public EventCallback OnMergeTable { get; set; }
    [Parameter] public EventCallback OnPrintPreBill { get; set; }
    
    private decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    private decimal Discount => AppliedDiscount;
    private decimal Tax => Math.Round(Math.Max(0, Subtotal - Discount) * 0.08m, 0); // 8% VAT
    private decimal Total => Math.Max(0, Subtotal - Discount + Tax);

    private int UnsentItemsCount => Items.Count(i => !i.IsSentToKitchen);
    
    private bool _isProcessing;

    public OrderCartItemLocal? SelectedItem { get; set; }

    private async Task HandleItemClickAsync(OrderCartItemLocal item)
    {
        SelectedItem = item;
        if (OnItemClicked.HasDelegate)
        {
            await OnItemClicked.InvokeAsync(item);
        }
        StateHasChanged();
    }
    
    private async Task HandleUpdateQuantityAsync(OrderCartItemLocal item, int newQuantity)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        
        try
        {
            if (newQuantity <= 0)
            {
                if (OnItemRemoved.HasDelegate)
                    await OnItemRemoved.InvokeAsync(item);
            }
            else
            {
                item.Quantity = newQuantity;
                if (OnQuantityChanged.HasDelegate)
                    await OnQuantityChanged.InvokeAsync(item);
            }
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    
    private async Task HandleSendToKitchenAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            if (OnSendToKitchen.HasDelegate)
                await OnSendToKitchen.InvokeAsync();
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    
    private async Task HandlePayAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            if (OnPay.HasDelegate)
                await OnPay.InvokeAsync();
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task HandlePrintPreBillAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            if (OnPrintPreBill.HasDelegate)
                await OnPrintPreBill.InvokeAsync();
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task HandleGlobalDeleteAsync()
    {
        if (_isProcessing || SelectedItem == null) return;
        _isProcessing = true;
        try
        {
            if (SelectedItem.IsSentToKitchen)
            {
                if (OnItemVoidRequested.HasDelegate)
                    await OnItemVoidRequested.InvokeAsync(SelectedItem);
            }
            else
            {
                if (OnItemRemoved.HasDelegate)
                    await OnItemRemoved.InvokeAsync(SelectedItem);
            }
            SelectedItem = null;
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
}
