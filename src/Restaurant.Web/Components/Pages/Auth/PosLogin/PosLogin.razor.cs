using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.Web.Components.Pages.Auth.PosLogin;

public partial class PosLogin
{
    [Inject]
    protected IIdentityService IdentityService { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    protected QuickLoginRequest Request { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected bool IsError { get; set; }
    
    protected ElementReference containerRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await containerRef.FocusAsync();
            }
            catch
            {
                // Ignore focus errors
            }
        }
    }

    protected async Task HandleKeyDown(KeyboardEventArgs e)
    {
        Console.WriteLine($"Key pressed: {e.Key}");
        if (e.Key == "Enter")
        {
            if (Request.AccessCode.Length == 6 && !IsLoading)
            {
                await ConfirmPin();
            }
        }
        else if (e.Key == "Backspace")
        {
            ClearLast();
        }
        else if (e.Key.Length == 1 && char.IsDigit(e.Key[0]))
        {
            AppendDigit(int.Parse(e.Key));
        }
        StateHasChanged();
    }
    
    private string pinCode = "";

    protected async Task AppendDigit(int digit)
    {
        IsError = false; // Reset error state on new key press
        
        if (pinCode.Length < 6) // Max 6 digits
        {
            pinCode += digit.ToString();
            Request.AccessCode = pinCode;
            StateHasChanged();
        }
        
        Console.WriteLine($"[DEBUG] Đã bấm nút: {digit} | Mã PIN hiện tại: '{pinCode}' | Độ dài: {pinCode.Length}");
        try 
        {
            await JS.InvokeVoidAsync("console.log", $"[BROWSER DEBUG] Đã bấm nút: {digit} | Mã PIN hiện tại: '{pinCode}' | Độ dài: {pinCode.Length}");
        }
        catch { }
    }
    
    protected void ClearLast()
    {
        Console.WriteLine("Clear pin clicked");
        if (pinCode.Length > 0)
        {
            pinCode = pinCode.Substring(0, pinCode.Length - 1);
            Request.AccessCode = pinCode;
            StateHasChanged();
        }
        IsError = false;
    }
    
    protected async Task ConfirmPin()
    {
        if (Request.AccessCode.Length < 6) return;
        
        IsLoading = true;
        IsError = false;
        StateHasChanged(); // Ensure spinner shows up immediately
        
        try
        {
            var response = await IdentityService.QuickLoginAsync(Request);
            
            if (response == null)
            {
                IsError = true;
                Request.AccessCode = ""; // Auto-clear on error
                pinCode = "";
            }
            else
            {
                Console.WriteLine($"Quick login attempt with PIN {Request.AccessCode} successful for user {response.Username}");
                
                // Navigate to POS Dashboard upon success
                NavigationManager.NavigateTo("/pos/order");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during quick login: {ex.Message}");
            IsError = true;
            Request.AccessCode = ""; // Auto-clear on error
            pinCode = "";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
