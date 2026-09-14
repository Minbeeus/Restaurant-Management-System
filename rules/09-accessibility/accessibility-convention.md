# Accessibility Conventions

> **Rule Hierarchy Level:** 09
> **Applies To:** Frontend
> **Last Updated:** 2026-07-14

## Purpose
This document establishes the digital accessibility requirements for the frontend application. It mandates compliance with Web Content Accessibility Guidelines (WCAG) 2.1 AA standards, ensuring that users of assistive technologies, screen readers, and keyboard-only navigation can fully operate the software.

## Scope
This convention covers HTML structure (Semantic HTML), ARIA roles and labels, keyboard accessibility, color contrast, and screen reader announcements. It does NOT cover CSS layout techniques (see [UI Convention](file:///d:/My%20Project/rules/07-ui/ui-convention.md)).

## Principles
1. **Semantic HTML First:** Use native HTML elements for their intended semantic purpose.
2. **Keyboard Operability:** All interactive components MUST be fully navigable and operable using a keyboard alone.
3. **Contrast and Visibility:** All text and critical UI boundaries must remain clear and legible across varying color schemes.
4. **Predictable Navigation:** Maintain a logical, linear structure for tab indexes and focus behaviors.

---

## Mandatory Rules

### A11Y-001: Semantic HTML Usage
All page layouts and components MUST use native HTML5 semantic elements (e.g., `<header>`, `<nav>`, `<main>`, `<section>`, `<article>`, `<aside>`, `<footer>`, `<time>`) instead of generic styling container blocks (e.g., `<div>` or `<span>`) for page structure.
- **Rationale:** Screen readers and search engine crawlers rely on semantic markup to build a coherent map of the interface.
- **Consequence:** Div-soup structures prevent screen readers from jumping to key landmarks, destroying navigation capabilities for visually impaired users.

### A11Y-002: Keyboard Navigation & Focus Indicators
All interactive elements (buttons, inputs, dropdowns, links) MUST be focusable and fully functional via keyboard navigation (Tab, Enter, Spacebar, Arrow keys). The visual focus indicator (`outline`) MUST NOT be disabled or hidden.
- **Rationale:** Keyboard-only users need to see exactly where the focus ring is located to interact with the interface.
- **Consequence:** Hiding outline rings makes keyboard navigation impossible as the user cannot determine which element is currently selected.

### A11Y-003: Form Label Association
All form inputs MUST be explicitly associated with a corresponding `<label>` element using the `for` attribute (or nested structure). When utilizing custom components, the `aria-label` or `aria-labelledby` attributes MUST be present.
- **Rationale:** Screen readers require this binding to announce what the input field represents when it receives focus.
- **Consequence:** Non-associated fields result in screen readers reading "Text box" with zero context, causing errors in form submission.

### A11Y-004: Alt Text for Non-Text Content
All images, icons, and illustrations MUST include an `alt` attribute. 
- Descriptive images must have alt text explaining their content in localized text.
- Decorative images or icons accompanied by adjacent descriptive text MUST have an empty `alt=""` tag to be skipped by screen readers.
- **Rationale:** Provides context to blind or low-vision users who cannot see the graphical asset.
- **Consequence:** Missing alt attributes cause screen readers to announce file names, which are useless and disruptive.

### A11Y-005: Color Contrast Compliance
Text and essential visual elements MUST achieve a minimum contrast ratio of 4.5:1 against their background (3:1 for text larger than 18pt or bold). Color MUST NOT be the sole indicator of meaning, state changes, or validation status.
- **Rationale:** Ensures readability for users with color blindness, low vision, or poor ambient lighting.
- **Consequence:** Low contrast leaves text unreadable, and using red text as the only indicator of a validation error isolates colorblind users.

---

## Recommended Practices

### A11Y-050: ARIA Landmark Roles
Custom structural elements or legacy widgets SHOULD be decorated with ARIA landmark roles (`role="dialog"`, `role="alert"`, `role="navigation"`) when native semantic elements cannot be utilized.
- **Rationale:** Enhances screen reader parsing of legacy code or complex JavaScript controls.

### A11Y-051: Skip Navigation Link
A "Skip to Main Content" hidden skip link SHOULD be placed at the top of the body page to allow keyboard users to bypass global navigation headers.
- **Rationale:** Saves keyboard users from having to tab through dozens of header links on every page transition.

---

## Anti-patterns

### The Button-Div Abuse
Using a `div` or `span` as a button and attaching a click event listener (`<div onclick="doSomething()">`).
- **Why it's harmful:** Divs are not in the natural tab cycle, do not react to the Space or Enter keys, and do not advertise themselves as clickable to screen readers.
- **What to do instead:** Use a native `<button>` element and style it as needed.

### Color-Only Warnings
Indicating validation status or critical actions purely by changing the background or text color (e.g., turning an input border red to signal an error).
- **Why it's harmful:** Red-green colorblind users cannot perceive this change.
- **What to do instead:** Accompany the color change with an explicit icon and descriptive validation text (`"Invalid email format"`).

---

## Examples

### ✅ Correct (Semantic elements, Alt text, Explicit form labeling)

**Semantic Layout & Form (HTML5):**
```html
<header class="pos-header">
  <nav aria-label="Main Navigation">
    <a href="/dashboard">@Localizer["NavDashboard"]</a>
    <a href="/orders">@Localizer["NavOrders"]</a>
  </nav>
</header>

<main id="main-content">
  <section class="c-profile-form">
    <h1>@Localizer["ProfileTitle"]</h1>
    
    <div class="c-field-group">
      <!-- Input explicitly tied to label via 'for' and 'id' -->
      <label for="user-email">@Localizer["LabelEmail"]</label>
      <input type="email" id="user-email" name="email" required aria-describedby="email-help" />
      <span id="email-help" class="c-help-text">@Localizer["HelpEmailText"]</span>
    </div>

    <div class="c-action-group">
      <!-- Native button element with image that has alt text -->
      <button type="submit" class="c-btn c-btn--primary">
        <img src="/icons/save.svg" alt="" aria-hidden="true" />
        @Localizer["BtnSaveProfile"]
      </button>
    </div>
  </section>
</main>
```

### ❌ Incorrect (Non-semantic markup, div button, missing alt and labels)

```html
<!-- Generic structural container, missing h1, div buttons, no labels -->
<div class="header">
  <div class="logo">
    <img src="/images/logo.png"> <!-- Missing alt attribute -->
  </div>
</div>

<div class="content">
  <span class="title">Thông tin tài khoản</span> <!-- Non-semantic heading -->
  
  <div class="form-row">
    <span>Email:</span>
    <input type="text" name="email"> <!-- Missing label connection -->
  </div>

  <!-- A div acting as a button with no keyboard accessibility -->
  <div class="save-btn" onclick="saveData()">
    Lưu thông tin
  </div>
</div>
```

---

## Checklist
- [ ] Are HTML5 semantic landmarks (`<header>`, `<nav>`, `<main>`, `<section>`, `<footer>`) used for layout?
- [ ] Do all interactive elements receive keyboard focus, and is the visual focus ring visible?
- [ ] Are all form inputs bound to a `<label>` element with matching `for` and `id` tags?
- [ ] Do all descriptive images have alt text, and do decorative icons have `alt=""` or `aria-hidden="true"`?
- [ ] Are validation error states announced with text and symbols, and do color contrast ratios pass WCAG AA (4.5:1)?
