using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Pos.Components.CategoryTabs;

public partial class CategoryTabs
{

    
    [Parameter, EditorRequired] public List<CategoryDto> Categories { get; set; } = new();
    [Parameter] public int ActiveCategoryId { get; set; }
    [Parameter] public EventCallback<int> OnCategoryChanged { get; set; }
    
    [Parameter] public bool ShowImages { get; set; }
    [Parameter] public EventCallback<bool> OnImageToggleChanged { get; set; }
    
    private async Task HandleTabClick(int categoryId)
    {
        if (ActiveCategoryId != categoryId)
        {
            if (OnCategoryChanged.HasDelegate)
            {
                await OnCategoryChanged.InvokeAsync(categoryId);
            }
        }
    }
    
    private async Task HandleImageToggle(bool value)
    {
        if (OnImageToggleChanged.HasDelegate)
        {
            await OnImageToggleChanged.InvokeAsync(value);
        }
    }
}
