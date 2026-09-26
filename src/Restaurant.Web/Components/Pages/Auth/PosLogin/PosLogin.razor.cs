using Microsoft.AspNetCore.Components;

namespace Restaurant.Web.Components.Pages.Auth.PosLogin;

public partial class PosLogin
{
    [Inject]
    protected NavigationManager NavManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavManager.NavigateTo("/login", replace: true);
    }
}
