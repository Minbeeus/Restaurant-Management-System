using Microsoft.AspNetCore.Components;

namespace Restaurant.Web.Components.Pages.Auth.KdsLogin;

public partial class KdsLogin
{
    [Inject]
    protected NavigationManager NavManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavManager.NavigateTo("/login", replace: true);
    }
}
