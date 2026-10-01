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

    [Parameter] public string SearchQuery { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SearchQueryChanged { get; set; }
    [Parameter] public EventCallback OnSearchEnterPressed { get; set; }
    
    private ElementReference _searchInputRef;

    public async Task FocusSearchAsync()
    {
        try
        {
            await _searchInputRef.FocusAsync();
        }
        catch { }
    }

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

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        var val = e.Value?.ToString() ?? string.Empty;
        SearchQuery = val;
        if (SearchQueryChanged.HasDelegate)
        {
            await SearchQueryChanged.InvokeAsync(val);
        }
    }

    private async Task HandleKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (OnSearchEnterPressed.HasDelegate)
            {
                await OnSearchEnterPressed.InvokeAsync();
            }
        }
    }
}
