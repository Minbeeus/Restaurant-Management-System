# Blazor Frontend Conventions

> **Rule Hierarchy Level:** Extension (Framework-Specific)
> **Applies To:** Frontend (Blazor WebAssembly / Server / Hybrid)
> **Last Updated:** 2026-07-14

## Purpose
This document defines the frontend development standards, component design system, localization practices, state management rules, and interaction safety guidelines when developing user interfaces with Blazor.

## Scope
This convention covers Blazor component structures, parameters, lifecycle methods, form validations, CSS naming (BEM), and localization. It does NOT cover general styling tokens (see [UI Convention](file:///d:/My%20Project/rules/07-ui/ui-convention.md)) or general user flow paradigms (see [UX Convention](file:///d:/My%20Project/rules/08-ux/ux-convention.md)).

## Principles
1. **100% Componentization:** Every user interface view MUST be a composition of modular, isolated Blazor components.
2. **Mandatory Reuse First:** Search for and extend existing components before drafting new ones.
3. **No Hardcoded UI Strings:** All visual text, validation text, placeholder text, and error statements MUST be localized.
4. **BEM Styling Isolation:** Enforce clean CSS naming conventions and isolated styles to prevent visual leakage.

---

## Mandatory Rules

### BLAZOR-001: Component-First Architecture
All frontend views and pages MUST be built as a tree of Blazor components (`.razor`). Writing raw, non-reusable HTML structures directly inside page-level views is strictly prohibited.
- **Rationale:** Ensures that common components (like buttons, input wrappers, modal boxes, and data grids) remain consistent in logic, styling, and accessibility.
- **Consequence:** Raw HTML duplicate copy-pasting creates a fragmented design system, making global layout updates virtually impossible.

### BLAZOR-002: Mandatory Component Reuse
Before creating any new Blazor component, the agent or developer MUST search the codebase for an existing component that satisfies the visual pattern. If a component exists but lacks a specific variant, the developer MUST refactor the existing component to accept parameters supporting that variant rather than creating a duplicate.
- **Rationale:** Enforces DRY, minimizes code footprint, and consolidates bug fixes.
- **Consequence:** Creating redundant menu cards, buttons, or list structures splits the design implementation, leading to inconsistent UI states.

### BLAZOR-003: No Hardcoded UI Text (Mandatory IStringLocalizer)
All Blazor components MUST use `IStringLocalizer<T>` (or localized resource files) to load display strings. Hardcoding string literals for UI headings, paragraph text, button labels, placeholders, or validation states is prohibited.
- **Rationale:** Ensures internationalization (i18n) compatibility from day one.
- **Consequence:** Hardcoded string literals require direct source code edits to translate and will trigger automated code review failures.

### BLAZOR-004: Strongly-Typed, Editor-Required Parameters
All parameters on Blazor components MUST be strongly typed. Any parameter that is critical for the component to function properly MUST be decorated with the `[EditorRequired]` attribute.
- **Rationale:** Enforces compile-time checking of component dependencies, preventing runtime rendering crashes.
- **Consequence:** Failing to pass a critical model or handler triggers silent errors or rendering crashes at runtime.

### BLAZOR-005: EventCallback for Parent Communication
Components that bubble events upward (such as button clicks, selection changes, or form submissions) MUST use `EventCallback` or `EventCallback<T>` to notify parent containers. The use of custom delegates or C# `Action` events is prohibited for standard rendering cycles.
- **Rationale:** `EventCallback` automatically triggers Blazor's state evaluation (`StateHasChanged`) on the parent component after execution, ensuring the UI remains synchronized.
- **Consequence:** Using standard C# delegates (`Action`) does not notify the render tree, requiring manual, error-prone calls to `StateHasChanged()`.

### BLAZOR-006: Destructive Action Modal Confirmation
All Blazor UI interactions that trigger destructive or irreversible changes (e.g., delete item, cancel order, disable user) MUST display a confirmation modal. The action API call MUST NOT be bound directly to the click event of the first trigger button. It MUST only execute inside the modal's confirm callback.
- **Rationale:** Prevents accidental triggers from touch inputs or fat-finger mistakes.
- **Consequence:** Instant deletions lead to high transaction error rates and support workloads.

### BLAZOR-007: Scoped CSS Isolation
Every component that requires custom styling MUST place those styles in a corresponding isolated CSS file (`[ComponentName].razor.css`).
- Styles placed in CSS isolation MUST adhere to BEM CSS naming conventions.
- Writing inline style attributes (`style="..."`) inside Blazor HTML markup is strictly prohibited.
- **Rationale:** Keeps CSS scope restricted to the component, preventing layout pollution across the application.
- **Consequence:** Global stylesheet bloat results in class name collisions that break page structures unexpectedly.

---

## Recommended Practices

### BLAZOR-050: Code-Behind Separation
For complex components containing substantial C# logic (more than 40 lines), developers SHOULD separate C# logic into a partial code-behind class (`[ComponentName].razor.cs`).
- **Rationale:** Keeps markup and business logic separated, improving file readability.

### BLAZOR-051: Eager Disposal of Subscriptions
Components that subscribe to external events (like SignalR hubs, state containers, or timers) MUST implement `IDisposable` or `IAsyncDisposable` and unsubscribe when disposed.
- **Rationale:** Prevents memory leaks and zombie processing cycles.

---

## Anti-patterns

### Direct API Calls in Child Components
Injecting HTTP client or database service references inside nested child components to load data on initialization.
- **Why it's harmful:** Couples UI presentation tightly to network infrastructure, making components non-reusable and impossible to unit test.
- **What to do instead:** Retrieve data in the parent page container and pass it to child components using parameters.

### StateHasChanged Overuse
Calling `StateHasChanged()` repeatedly inside lifecycle methods to force UI updates.
- **Why it's harmful:** Indicates incorrect event patterns, causes unnecessary re-renders, and degrades UI performance.
- **What to do instead:** Use `EventCallback` to bubble UI events and let Blazor manage re-rendering automatically.

---

## Examples

### ✅ Correct (Reusable Button, Safe Modal Confirm, BEM, Localization)

**Custom Reusable Button Component (`src/Restaurant.Web/Components/Button.razor`):**
```razor
<!-- Component isolates visual properties, uses localized parameters -->
<button class="c-btn @ButtonClass" @onclick="HandleClick" disabled="@Disabled" type="@Type">
    @if (IsLoading)
    {
        <span class="c-spinner" aria-hidden="true"></span>
    }
    @ChildContent
</button>
```
```csharp
// Location: src/Restaurant.Web/Components/Button.razor.cs
using Microsoft.AspNetCore.Components;

namespace Restaurant.Web.Components;

public partial class Button
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Variant { get; set; } = "primary"; // primary, secondary, outline, danger
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private string ButtonClass => Variant switch
    {
        "secondary" => "c-btn--secondary",
        "outline" => "c-btn--outline",
        "danger" => "c-btn--danger",
        _ => "c-btn--primary"
    };

    private async Task HandleClick()
    {
        if (!Disabled && !IsLoading && OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
        }
    }
}
```

**Order Card reusing Button and enforcing UX Confirmation Safety (`src/Restaurant.Web/Components/OrderCard.razor`):**
```razor
@inject IStringLocalizer<OrderCard> L

<div class="c-card">
    <div class="c-card__header">
        <h4 class="c-card__title">@L["OrderLabel"] #@Order.OrderCode</h4>
    </div>
    <div class="c-card__body">
        <p class="c-card__text">@L["AmountLabel"]: @Order.TotalAmount.ToString("N0") đ</p>
    </div>
    <div class="c-card__footer">
        <!-- Reuses Button component. Triggers confirm prompt, NOT immediate delete -->
        <Button Variant="danger" OnClick="PromptDelete">
            @L["BtnCancelOrder"]
        </Button>
    </div>
</div>

@if (_showConfirmModal)
{
    <!-- Reusable Confirmation Dialog Modal Component -->
    <ConfirmationModal 
        Title="@L["CancelOrderTitle"]"
        Message="@GetConfirmationMessage()"
        OnConfirm="ExecuteCancel"
        OnCancel="DismissModal"
        IsLoading="_isProcessing" />
}
```
```csharp
// Location: src/Restaurant.Web/Components/OrderCard.razor.cs
using Microsoft.AspNetCore.Components;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components;

public partial class OrderCard
{
    [Parameter, EditorRequired] public OrderDto Order { get; set; } = default!;
    [Parameter] public EventCallback<int> OnOrderCancelled { get; set; }

    private bool _showConfirmModal;
    private bool _isProcessing;

    private void PromptDelete()
    {
        _showConfirmModal = true;
    }

    private string GetConfirmationMessage()
    {
        return string.Format(L["CancelConfirmationText"].Value, Order.OrderCode);
    }

    private async Task ExecuteCancel()
    {
        _isProcessing = true;
        try
        {
            if (OnOrderCancelled.HasDelegate)
            {
                await OnOrderCancelled.InvokeAsync(Order.Id);
            }
            _showConfirmModal = false;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void DismissModal()
    {
        _showConfirmModal = false;
    }
}
```

### ❌ Incorrect (Hardcoded strings, direct API injection in child, direct deletion click)

```razor
@inject HttpClient Http

<!-- ❌ Pollution: Hardcoded visual properties, violating BLAZOR-007 -->
<div style="border: 1px solid red; padding: 15px; margin: 10px;">
    <!-- ❌ Hardcoded Vietnam copy string -->
    <h4>Đơn hàng #@OrderId</h4>
    
    <!-- ❌ Dangerous: Immediate delete action bound directly to click, no verification -->
    <button class="btn btn-danger" @onclick="DeleteOrder">
        Hủy đơn hàng
    </button>
</div>

@code {
    [Parameter] public int OrderId { get; set; }

    private async Task DeleteOrder()
    {
        // ❌ Direct infrastructure API call inside child component, violating clean separation
        await Http.DeleteAsync($"api/v1/orders/{OrderId}");
    }
}
```

---

## Checklist
- [ ] Are all UI views composed entirely of reusable razor components?
- [ ] Have you confirmed that no similar component already exists before creating a new one?
- [ ] Are all display strings loaded via `IStringLocalizer` or resource files?
- [ ] Are all critical parameters decorated with `[EditorRequired]`?
- [ ] Do child components communicate events to parents using `EventCallback`?
- [ ] Do all destructive UI actions prompt a confirmation modal component?
- [ ] Are all component custom styles scoped using `.razor.css` isolated stylesheets?
- [ ] Are there zero inline style attributes (`style="..."`) inside markup tags?
