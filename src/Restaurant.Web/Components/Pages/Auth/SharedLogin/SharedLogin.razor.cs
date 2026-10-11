using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Restaurant.Application.DTOs;
using Restaurant.Web.Services;

namespace Restaurant.Web.Components.Pages.Auth.SharedLogin;

public partial class SharedLogin
{
    [Inject]
    protected IAuthApiClient AuthApiClient { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    protected string pinCode = "";
    protected bool IsLoading { get; set; }
    protected bool IsError { get; set; }
    protected string ErrorMessage { get; set; } = string.Empty;

    protected bool ShowAreaSelector { get; set; }
    protected string AuthenticatedUserFullName { get; set; } = string.Empty;
    protected string AuthenticatedRoleName { get; set; } = string.Empty;
    protected string _pendingToken = string.Empty;

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
        if (e.Key == "Enter")
        {
            if (pinCode.Length == 6 && !IsLoading)
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
    }

    protected void AppendDigit(int digit)
    {
        IsError = false;
        ErrorMessage = string.Empty;

        if (pinCode.Length < 6)
        {
            pinCode += digit.ToString();
            StateHasChanged();
        }
    }

    protected void ClearLast()
    {
        if (pinCode.Length > 0)
        {
            pinCode = pinCode.Substring(0, pinCode.Length - 1);
            StateHasChanged();
        }
        IsError = false;
        ErrorMessage = string.Empty;
    }

    protected async Task ConfirmPin()
    {
        if (pinCode.Length < 6) return;

        IsLoading = true;
        IsError = false;
        ErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var response = await AuthApiClient.PinLoginAsync(new PinLoginRequest { PinCode = pinCode });

            if (response == null)
            {
                IsError = true;
                ErrorMessage = "Mã PIN không đúng, vui lòng thử lại.";
                pinCode = ""; // Auto clear on failure
            }
            else
            {
                AuthenticatedUserFullName = response.FullName;
                AuthenticatedRoleName = response.RoleName;

                if (string.Equals(response.DestinationArea, "BOTH", StringComparison.OrdinalIgnoreCase))
                {
                    _pendingToken = response.Token;
                    ShowAreaSelector = true;
                }
                else
                {
                    string targetPath;
                    if (string.Equals(response.DestinationArea, "KDS", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = "/kds";
                    }
                    else if (string.Equals(response.DestinationArea, "ADMIN", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = "/admin/dashboard";
                    }
                    else
                    {
                        targetPath = "/pos";
                    }

                    var loginUrl = $"/Account/Login?token={Uri.EscapeDataString(response.Token)}&returnUrl={Uri.EscapeDataString(targetPath)}";
                    NavigationManager.NavigateTo(loginUrl, forceLoad: true);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            IsError = true;
            ErrorMessage = ex.Message;
            pinCode = "";
        }
        catch (Exception ex)
        {
            IsError = true;
            ErrorMessage = "Lỗi xác thực hệ thống: " + ex.Message;
            pinCode = "";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void SelectArea(string path)
    {
        ShowAreaSelector = false;
        if (!string.IsNullOrEmpty(_pendingToken))
        {
            var loginUrl = $"/Account/Login?token={Uri.EscapeDataString(_pendingToken)}&returnUrl={Uri.EscapeDataString(path)}";
            NavigationManager.NavigateTo(loginUrl, forceLoad: true);
        }
        else
        {
            NavigationManager.NavigateTo(path);
        }
    }
}
