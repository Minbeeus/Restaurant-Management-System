# Pre-Commit Checklist

> **Rule Hierarchy Level:** 17
> **Applies To:** Developers and AI Agents
> **Last Updated:** 2026-07-14

This checklist MUST be reviewed and completed before committing any code changes to the repository.

---

## 1. Code Quality & Standards
- [ ] **Naming Conventions:** All classes, interfaces, variables, and methods follow the project's casing standard (PascalCase for public, camelCase for local parameters, `_camelCase` for private fields).
- [ ] **No Hardcoded UI Text:** All user-facing UI labels, error messages, placeholders, and tooltips are externalized into resource files (`.resx`, JSON translations, etc.).
- [ ] **No Magic Strings or Values:** All technical and business constants are defined in static classes or enums at the Domain/Core layer.
- [ ] **SOLID Compliance:** Verified that new classes have a single responsibility (SRP) and interfaces are segregated into focused roles (ISP).
- [ ] **Clean Exception Handling:** Handled specific exceptions rather than using generic `catch (Exception)`. Assured exceptions do not swallow stack traces.

---

## 2. Architecture & Design boundaries
- [ ] **Clean Architecture Layers:** Verified that Domain has no external dependencies, Application depends only on Domain, and Infrastructure handles database-specific integrations.
- [ ] **No Logic in Controllers:** Controllers only receive API requests, call validators, trigger Application Services, and return HTTP status codes.
- [ ] **No Logic in Views:** HTML templates and views contain no business validation or database queries.
- [ ] **Component Reuse:** Reused existing UI components and business service interfaces rather than generating duplicate structures.
- [ ] **BEM CSS Styling:** All custom styles utilize BEM naming rules and Design tokens (CSS custom properties). No inline styles.

---

## 3. Security
- [ ] **No Committed Secrets:** Checked the diff to ensure no connection strings, private JWT keys, passwords, or API tokens are checked in.
- [ ] **Input Validation:** All entry-point parameters are validated at the API/UI boundary using validators (FluentValidation).
- [ ] **Destructive Action Safety:** All delete or cancel actions require a confirmation modal before triggering the transaction.
- [ ] **Logging Masking:** Verified that sensitive PII (passwords, OTPs, cards) is masked and not logged in plain text.

---

## 4. Build & Testing
- [ ] **Local Compilation:** Ran compiler and verified that build succeeds with zero critical errors or warnings.
- [ ] **Tests Pass:** Executed all unit and integration test suites, and all tests pass green.
- [ ] **New Tests Added:** Written unit tests to cover any new business calculations or conditions introduced in this change.

---

## 5. Database & Migrations
- [ ] **Migration Check:** Verified that migrations are clean and SQL outputs generated via scripting are reviewed for unintentional drop statements.
- [ ] **Soft Delete:** Business data is marked with `IsDeleted` filters rather than run physically destructive SQL deletes.
