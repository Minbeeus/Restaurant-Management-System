# UI Design Conventions

> **Rule Hierarchy Level:** 07
> **Applies To:** Frontend
> **Last Updated:** 2026-07-14

## Purpose
This document defines the UI standards and component-driven architecture for the frontend application. It enforces visual consistency, design token usage, CSS naming conventions, component isolation, and the strict elimination of hardcoded text in UI files.

## Scope
This convention covers visual systems (colors, typography, spacing), component structure, CSS naming (BEM), and localization of UI text. It does NOT cover user flows (see [UX Convention](file:///d:/My%20Project/rules/08-ux/ux-convention.md)) or accessibility rules (see [Accessibility Convention](file:///d:/My%20Project/rules/09-accessibility/accessibility-convention.md)).

## Principles
1. **Component-First Architecture:** The UI MUST be constructed as a tree of modular, reusable components.
2. **Design Token Consistency:** All layouts MUST consume central CSS variables instead of raw magic values.
3. **Strict Separation of Concerns:** Markup and styles are isolated to the component level; layouts must not write custom, one-off styles.
4. **i18n Readiness:** Every user-facing label or text fragment MUST be loaded from a localization system.

---

## Mandatory Rules

### UI-001: Component-Based UI Construction
All user interface elements MUST be built as reusable components. Writing ad-hoc, inline HTML structures or custom CSS directly within page templates for core UI elements is strictly prohibited.
- **Rationale:** Prevents layout fragmentation and duplicate markup, ensuring that changes to any design pattern update everywhere simultaneously.
- **Consequence:** Ad-hoc styling causes code bloat, visual inconsistency, and massive refactoring costs.

### UI-002: Mandatory Component Reuse
Before creating any new UI component, the developer or agent MUST search the codebase for an existing component that satisfies the requirement. If a similar component exists but lacks a specific variant, the existing component MUST be extended (e.g., via parameters/properties) rather than duplicated.
- **Rationale:** Ensures clean code, stops duplicate component classes, and maintains a single source of truth for UI elements.
- **Consequence:** Creating redundant cards or buttons fragments the design system and violates DRY.

### UI-003: No Hardcoded UI Text (Zero Magic Strings)
All user-facing UI text, including labels, headings, buttons, placeholder text, and error messages, MUST NOT be hardcoded as string literals in markup files. They MUST be loaded dynamically from localized resource files (e.g., `.resx`, JSON, or translation files).
- **Rationale:** Enables multi-language localization (i18n), separates copy-writing from logic, and simplifies text reviews.
- **Consequence:** Hardcoded strings require source code modification to translate and break internationalization efforts.

### UI-004: Design Tokens for Styles
All CSS styles MUST use Design System Tokens (CSS custom properties defined at `:root`) for colors, font sizing, spacing, borders, shadows, and transitions. Hardcoding raw hex codes, pixel sizes, or direct font families in component CSS is strictly prohibited.
- **Rationale:** Guarantees that global styling changes (like dark mode or brand refreshes) require updating only token files.
- **Consequence:** Raw hex values and pixel counts scatter design choices across hundreds of files, making themes unmaintainable.

### UI-005: BEM Naming Convention
All custom CSS classes MUST follow the **Block-Element-Modifier (BEM)** naming methodology. 
- Format: `.c-[block]__[element]--[modifier]`
- The prefix `c-` MUST be used for custom components (e.g., `.c-card`, `.c-btn`).
- **Rationale:** Prevents style pollution, provides self-documenting CSS class hierarchies, and ensures component styling is predictable.
- **Consequence:** Nested CSS selectors or generic class names (`.title`, `.active`) cause selector collisions and layout bugs.

---

## Recommended Practices

### UI-050: Touch Targets
All interactive UI elements (buttons, links, form inputs) SHOULD have a minimum touch target size of 48x48 pixels to facilitate ease of interaction on mobile devices.
- **Rationale:** Enhances usability for users on mobile touchscreens or with limited fine motor control.

### UI-051: CSS Isolation
Components SHOULD use CSS isolation (such as scoped CSS or component-isolated files) to prevent styling rules from leaking out and affecting other parts of the DOM.
- **Rationale:** Keeps CSS simple and prevents global selector conflicts.

---

## Anti-patterns

### The Ad-hoc inline CSS layout
Writing styling properties directly inside HTML attributes (`style="margin-top: 15px; color: #ff0000;"`).
- **Why it's harmful:** Bypasses the central design token system, makes modifications impossible to do globally, and bloats DOM payloads.
- **What to do instead:** Use design tokens and assign them via semantic CSS classes (e.g., `<span class="c-text--error">`).

### Redundant Component Creation
Creating `FoodItemCard.razor` when there is already a generic `ProductCard.razor` that can be configured with a parameter.
- **Why it's harmful:** Violates YAGNI and DRY, leads to code duplication, and increases codebase maintenance.
- **What to do instead:** Refactor the existing `ProductCard` to support a configuration parameter or slot (RenderFragment) for custom food data.

---

## Examples

### ✅ Correct (BEM, Design Tokens, Localization, Component-First)

**HTML/Component Structure:**
```html
<!-- Localized strings loaded via component bindings or localization framework -->
<div class="c-card c-card--featured">
  <div class="c-card__header">
    <h3 class="c-card__title">@Localizer["FeaturedProductTitle"]</h3>
  </div>
  <div class="c-card__body">
    <p class="c-card__desc">@Localizer["FeaturedProductDesc"]</p>
  </div>
  <div class="c-card__footer">
    <Button Variant="outline">@Localizer["BtnLearnMore"]</Button>
    <Button Variant="primary">@Localizer["BtnBuyNow"]</Button>
  </div>
</div>
```

**CSS Stylesheet using Design Tokens:**
```css
/* Component Scoped Styles */
.c-card {
  background-color: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-sm);
  padding: var(--spacing-md);
  transition: box-shadow var(--transition-speed) ease;
}

.c-card--featured {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
}

.c-card__title {
  color: var(--text-primary);
  font-family: var(--font-primary);
  font-size: var(--font-size-lg);
  margin-bottom: var(--spacing-sm);
}

.c-card__desc {
  color: var(--text-secondary);
  font-size: var(--font-size-md);
}
```

### ❌ Incorrect (Hardcoded text, Raw CSS values, No BEM, Inline styles)

```html
<!-- HARDCODED text, no component reuse, raw layout styling -->
<div style="padding: 15px; border: 1px solid #ccc; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <h3 style="color: #E2725B; font-size: 18px; margin: 0 0 10px 0;">Cà phê Espresso</h3>
  <p style="color: #6C757D;">Cà phê pha máy nguyên chất từ hạt Robusta đậm đà.</p>
  <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 15px;">
    <span style="font-weight: bold; color: #212529;">35.000 đ</span>
    <!-- Hardcoded button class, not using the shared button component -->
    <button class="btn btn-primary" onclick="addToCart()">Thêm món</button>
  </div>
</div>
```

---

## Checklist
- [ ] Are all UI elements built as reusable components rather than inline code?
- [ ] Have you verified that no similar UI component already exists in the system before creating a new one?
- [ ] Are all user-facing strings, labels, and validation messages externalized to localization resource files?
- [ ] Are all CSS values (colors, margins, padding, border-radius) referencing Design Tokens (CSS variables)?
- [ ] Do all class names follow the BEM naming format starting with the proper prefix (e.g., `c-`)?
