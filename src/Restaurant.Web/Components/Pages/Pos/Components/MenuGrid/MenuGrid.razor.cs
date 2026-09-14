using Microsoft.AspNetCore.Components;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Pos.Components.MenuGrid;

public partial class MenuGrid
{
    [Parameter, EditorRequired] public List<MenuItemDto> Items { get; set; } = new();
    [Parameter] public bool ShowImage { get; set; } = true;
    [Parameter] public EventCallback<MenuItemDto> OnItemClicked { get; set; }
    
    private async Task HandleItemClicked(MenuItemDto item)
    {
        if (OnItemClicked.HasDelegate)
        {
            await OnItemClicked.InvokeAsync(item);
        }
    }
}
