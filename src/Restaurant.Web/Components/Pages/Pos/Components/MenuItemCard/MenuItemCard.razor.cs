using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Pos.Components.MenuItemCard;

public partial class MenuItemCard
{

    
    [Parameter, EditorRequired] public MenuItemDto Item { get; set; } = default!;
    [Parameter] public bool ShowImage { get; set; } = true;
    [Parameter] public decimal ExtraPriceForConfig { get; set; } = 0m;
    [Parameter] public string? ConfigBadgeText { get; set; }
    [Parameter] public EventCallback<MenuItemDto> OnItemClicked { get; set; }
    
    protected string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(Item?.Name)) return string.Empty;
            var name = Item.Name.Trim();
            // Loyalty reward items keep full descriptive name
            if (Item.PointsCost > 0 || Item.IsRewardItem)
            {
                return name;
            }
            if (name.StartsWith("Pizza ", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(6).Trim();
            }
            return name;
        }
    }

    private async Task HandleClick()
    {
        if (Item.IsSoldOut) return;
        
        if (OnItemClicked.HasDelegate)
        {
            await OnItemClicked.InvokeAsync(Item);
        }
    }
}
