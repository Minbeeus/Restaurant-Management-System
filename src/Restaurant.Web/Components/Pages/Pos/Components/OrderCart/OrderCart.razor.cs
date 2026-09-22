using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Restaurant.Web.Components.Pages.Pos.Components.OrderCart;

public class OrderCartItemLocal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public List<int>? SelectedModifiers { get; set; }
    public string? Note { get; set; }
}

public partial class OrderCart
{

    
    [Parameter] public string TableName { get; set; } = "Mang về";
    [Parameter] public string OrderCode { get; set; } = "#NEW";
    [Parameter] public List<OrderCartItemLocal> Items { get; set; } = new();
    
    [Parameter] public EventCallback<OrderCartItemLocal> OnQuantityChanged { get; set; }
    [Parameter] public EventCallback<OrderCartItemLocal> OnItemRemoved { get; set; }
    [Parameter] public EventCallback OnSendToKitchen { get; set; }
    [Parameter] public EventCallback OnPay { get; set; }
    
    private decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    private decimal Discount => 0; // Hardcoded for demo
    private decimal Tax => Subtotal * 0.08m; // 8% VAT
    private decimal Total => Subtotal - Discount + Tax;
    
    private bool _isProcessing;
    
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
}
