# UX Design Conventions

> **Rule Hierarchy Level:** 08
> **Applies To:** Frontend
> **Last Updated:** 2026-07-14

## Purpose
This document defines the UX principles and interaction patterns required to build intuitive, safe, and resilient user interfaces. It ensures consistent navigation, feedback loops, forms, and validation patterns.

## Scope
This convention covers user flow designs, confirmation states, form interactions, loading states, error handling in UI, and empty states. It does NOT cover visual system details (see [UI Convention](file:///d:/My%20Project/rules/07-ui/ui-convention.md)) or semantic markup structure (see [Accessibility Convention](file:///d:/My%20Project/rules/09-accessibility/accessibility-convention.md)).

## Principles
1. **Interaction Safety First:** Prevent accidental data loss or destructive state changes by enforcing explicit confirmations.
2. **Clear Feedback Loops:** The system MUST always keep users informed of processing states, successes, and errors.
3. **Graceful Error Recovery:** Don't just report errors; help the user recover and proceed.
4. **Consistency of Flow:** Use predictable navigation, cancel, and submit actions throughout the system.

---

## Mandatory Rules

### UX-001: Destructive Action Safety
All destructive actions (including deletes, cancellations, disabling accounts, discarding edits, or critical status updates) MUST present a Confirmation Modal/Popup to the user. The action MUST NOT execute directly upon clicking the trigger button.
- **Rationale:** Prevents catastrophic data loss or workflow disruption caused by accidental clicks.
- **Consequence:** Immediate data changes without confirmation result in negative user experiences, lost work, and excessive support tickets.

### UX-002: Descriptive Confirmation Message
The confirmation modal MUST clearly display the specific item or action being affected. Generic confirmation prompts (e.g., "Are you sure?") are prohibited. All text inside the modal MUST be localized.
- **Rationale:** Users must understand the exact consequence of their confirmation.
- **Consequence:** A user might confirm a delete thinking they are deleting a single item, when they are actually deleting a whole section.

### UX-003: Loading State Indicators
Any asynchronous operation taking longer than 300ms MUST display an explicit loading state (e.g., skeleton screen, spinner, loading overlay) and disable the action button to prevent multiple submissions.
- **Rationale:** Prevents double-submitting data (such as charging a card twice or creating duplicate orders) and assures the user the app is working.
- **Consequence:** Double-clicks on slow APIs create duplicate entities, corrupt transactions, and frustrate users.

### UX-004: Contextual Form Validation
Forms MUST validate user input inline and show immediate, clear error feedback rather than waiting for a full form submission or page reload. Error messages MUST be user-friendly, localized, and explain how to correct the input.
- **Rationale:** Simplifies error resolution and prevents cognitive overload during data entry.
- **Consequence:** Submitting a long form only to have it reload with a generic error block at the top is frustrating and leads to high cart/form abandonment.

---

## Recommended Practices

### UX-050: Auto-Save for Long Forms
Forms requiring substantial input (e.g., more than 5 fields or creative text entry) SHOULD automatically cache draft states to local storage to prevent data loss on session expiration or connectivity drops.
- **Rationale:** Protects user progress from unexpected interruptions.

### UX-051: Predictable "Back" Behavior
The "Back" or "Cancel" button SHOULD always return the user to the logical parent view and warn them if they have unsaved changes.
- **Rationale:** Establishes navigation confidence and prevents accidental navigation loss.

---

## Anti-patterns

### The Direct Trigger Delete
Hooking up an API endpoint callback directly to a trash icon `@onclick` or `click` event without showing a dialog first.
- **Why it's harmful:** Extremely prone to fat-finger mistakes on mobile, resulting in data loss.
- **What to do instead:** Open a confirmation modal component on click, and only call the API inside the modal's confirm callback.

### Silent Failures
An API call fails, and the UI simply stops loading without showing an error banner, toast, or message.
- **Why it's harmful:** Users assume the app is broken or frozen, leading to repeated clicks or closing the page.
- **What to do instead:** Show a localized toast error message with an option to retry the action.

---

## Examples

### ✅ Correct (Safe interaction, Loading states, Localization)

**UX Interaction Pattern (Blazor Example):**
```razor
<!-- OrderItem Row -->
<div class="c-list-item">
    <span>@Item.Name</span>
    <!-- Safe action: calls show confirmation modal instead of deleting directly -->
    <Button Variant="outline-danger" OnClick="OnPromptDelete">
        @Localizer["BtnDelete"]
    </Button>
</div>

<!-- Reusable Confirmation Modal Component -->
@if (_showDeleteConfirmation)
{
    <ConfirmationModal 
        Title="@Localizer["ConfirmDeleteTitle"]"
        Message="@GetFormattedConfirmationMessage()"
        OnConfirm="OnConfirmDelete"
        OnCancel="OnCancelDelete"
        IsLoading="_isDeleting" />
}

@code {
    private bool _showDeleteConfirmation;
    private bool _isDeleting;

    private void OnPromptDelete()
    {
        _showDeleteConfirmation = true;
    }

    private string GetFormattedConfirmationMessage()
    {
        // Dynamic, localized message referencing the exact entity affected
        return string.Format(Localizer["ConfirmDeleteItemMessage"].Value, Item.Name);
    }

    private async Task OnConfirmDelete()
    {
        _isDeleting = true; // Shows loading inside modal & disables buttons
        try
        {
            await OrderService.DeleteItemAsync(Item.Id);
            _showDeleteConfirmation = false;
            await OnItemDeleted.InvokeAsync();
        }
        catch (Exception ex)
        {
            NotificationService.ShowError(Localizer["ErrorDeleteFailed"]);
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private void OnCancelDelete()
    {
        _showDeleteConfirmation = false;
    }
}
```

### ❌ Incorrect (Direct API calls, hardcoded text, no confirmation)

```razor
<!-- API call bound directly to button, hardcoded string, no safe guard -->
<button class="btn-danger" @onclick="() => OrderService.DeleteItemAsync(Item.Id)">
    Xóa món
</button>
```

---

## Checklist
- [ ] Do all destructive, irreversible actions trigger a Confirmation Modal?
- [ ] Does the confirmation message explicitly state the name of the entity being modified/deleted?
- [ ] Are buttons disabled and loading indicators shown during asynchronous operations?
- [ ] Are validation errors displayed inline with localized, helpful recovery text?
- [ ] Does the "Cancel" or "Back" button warn users if there are dirty form edits?
