using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Pos.Components.MenuItemCard;

public partial class MenuItemCard
{

    
    [Parameter, EditorRequired] public MenuItemDto Item { get; set; } = default!;
    [Parameter] public bool ShowImage { get; set; } = true;
    [Parameter] public EventCallback<MenuItemDto> OnItemClicked { get; set; }
    
    private async Task HandleClick()
    {
        if (Item.IsSoldOut) return;
        
        if (OnItemClicked.HasDelegate)
        {
            await OnItemClicked.InvokeAsync(Item);
        }
    }
}
