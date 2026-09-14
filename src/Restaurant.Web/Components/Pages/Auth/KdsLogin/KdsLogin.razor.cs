using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.Web.Components.Pages.Auth.KdsLogin;

public partial class KdsLogin
{
    [Inject]
    protected IIdentityService IdentityService { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;
    
    protected QuickLoginRequest Request { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected bool IsError { get; set; }
    
    protected void KeyPress(string key)
    {
        IsError = false; // Reset error state on new key press
        
        if (Request.AccessCode.Length < 6) // Max 6 digits
        {
            Request.AccessCode += key;
        }
    }
    
    protected void ClearPin()
    {
        Request.AccessCode = "";
        IsError = false;
    }
    
    protected async Task ConfirmPin()
    {
        if (string.IsNullOrEmpty(Request.AccessCode)) return;
        
        IsLoading = true;
        IsError = false;
        
        try
        {
            var response = await IdentityService.QuickLoginAsync(Request);
            
            if (response == null)
            {
                IsError = true;
                Request.AccessCode = ""; // Auto-clear on error
            }
            else
            {
                Console.WriteLine($"Quick login attempt with PIN {Request.AccessCode} successful for chef {response.Username}");
                
                // Navigate to KDS Dashboard upon success
                NavigationManager.NavigateTo("/kds");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during quick login: {ex.Message}");
            IsError = true;
            Request.AccessCode = ""; // Auto-clear on error
        }
        finally
        {
            IsLoading = false;
        }
    }
}
