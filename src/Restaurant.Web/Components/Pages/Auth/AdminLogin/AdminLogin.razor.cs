using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Auth.AdminLogin;

public partial class AdminLogin
{

    
    protected LoginRequest Request { get; set; } = new();
    protected bool RememberMe { get; set; }
    protected bool IsLoading { get; set; }
    
    protected string PasswordInputType { get; set; } = "password";
    protected string PasswordIcon { get; set; } = "visibility";

    protected void TogglePasswordVisibility()
    {
        if (PasswordInputType == "password")
        {
            PasswordInputType = "text";
            PasswordIcon = "visibility_off";
        }
        else
        {
            PasswordInputType = "password";
            PasswordIcon = "visibility";
        }
    }
    
    protected async Task HandleLogin()
    {
        IsLoading = true;
        
        try
        {
            // Simulate API call for now since we're just building UI
            await Task.Delay(1000); 
            Console.WriteLine($"Login attempt for {Request.Username}");
            
            // TODO: Call IIdentityService.LoginAsync(Request)
        }
        finally
        {
            IsLoading = false;
        }
    }
}
