# Antigravity Rule System — Master Overview

> **Version:** 1.0.0
> **Last Updated:** 2025-07-14
> **Maintainer:** Engineering Team

---

## 1. What Is the Rule System?

The Antigravity Rule System is a structured collection of engineering convention documents that govern how software is designed, built, tested, and maintained. It serves two audiences:

- **Human developers** — Provides clear, unambiguous standards so every contributor makes consistent decisions regardless of experience level.
- **AI agents (Antigravity)** — Supplies deterministic rules the agent MUST follow when generating code, reviewing pull requests, proposing architectures, or making any engineering decision.

Every convention document follows a uniform template, uses RFC 2119 keywords for precision, and is organized in a strict hierarchy so that conflicts are resolved predictably.

---

## 2. How to Use This System

### For Human Developers

1. **Before starting work**, consult the relevant convention(s) for the area you are working in.
2. **During code review**, reference specific Rule IDs (e.g., `ARCH-003`, `CODE-012`) when requesting changes.
3. **When in doubt**, follow the hierarchy — higher-level rules always win.
4. **To propose changes**, submit a pull request against the convention document with rationale.

### For AI Agents

1. **Load all convention documents** at the beginning of each session or task.
2. **When generating code**, cross-check output against every applicable convention.
3. **When rules conflict**, apply the hierarchy: lower Rule Hierarchy Level numbers take precedence.
4. **When no rule covers a scenario**, fall back to the Principles section of the nearest applicable convention, then to `01-engineering-standards.md`.
5. **Always cite Rule IDs** in explanations and commit messages.

---

## 3. Rule Hierarchy

Rules are organized into 17 levels. **Lower numbers represent higher authority.** A rule at Level 01 overrides any conflicting rule at Level 02 or below. This is absolute — there are no exceptions.

```
┌─────────────────────────────────────────────────────────┐
│  01  Engineering Standards         (Supreme authority)   │
├─────────────────────────────────────────────────────────┤
│  02  Architecture & Project Structure                    │
├─────────────────────────────────────────────────────────┤
│  03  Coding Standards                                    │
├─────────────────────────────────────────────────────────┤
│  04  Naming Conventions                                  │
├─────────────────────────────────────────────────────────┤
│  05  Error Handling & Logging                            │
├─────────────────────────────────────────────────────────┤
│  06  Security Conventions                                │
├─────────────────────────────────────────────────────────┤
│  07  Database Conventions                                │
├─────────────────────────────────────────────────────────┤
│  08  API Design Conventions                              │
├─────────────────────────────────────────────────────────┤
│  09  Testing Conventions                                 │
├─────────────────────────────────────────────────────────┤
│  10  UI/UX Conventions                                   │
├─────────────────────────────────────────────────────────┤
│  11  Performance Conventions                             │
├─────────────────────────────────────────────────────────┤
│  12  Documentation Conventions                           │
├─────────────────────────────────────────────────────────┤
│  13  Version Control & CI/CD Conventions                 │
├─────────────────────────────────────────────────────────┤
│  14  Dependency Management                               │
├─────────────────────────────────────────────────────────┤
│  15  Localization & Internationalization                  │
├─────────────────────────────────────────────────────────┤
│  16  DevOps & Deployment                                 │
├─────────────────────────────────────────────────────────┤
│  17  Checklists & Templates          (Lowest authority)  │
└─────────────────────────────────────────────────────────┘
```

### Hierarchy Rules

| Principle | Description |
|---|---|
| **Downward inheritance** | Lower-level conventions inherit and refine higher-level rules. They add specificity but MUST NOT contradict. |
| **Upward reference** | Lower-level conventions SHOULD reference the higher-level rule they refine (e.g., "Per `ENG-005`, …"). |
| **Conflict resolution** | If two conventions at the same level conflict, the one with the lower number wins. If ambiguity remains, `01-engineering-standards.md` is the tiebreaker. |
| **Extension deference** | Framework-specific extensions (in `extensions/`) MUST NOT contradict their parent core convention. They only add framework-specific detail. |

---

## 4. Numbering System

Each convention document is prefixed with a two-digit number (`01` through `17`) that determines:

1. **Authority level** — Lower numbers have higher authority.
2. **Reading order** — New team members SHOULD read conventions in numerical order.
3. **Dependency direction** — Convention `N` may depend on conventions `1` through `N-1` but MUST NOT depend on conventions `N+1` and above.

### Rule ID Format

Every individual rule within a convention has a unique ID:

```
{PREFIX}-{NNN}

Examples:
  ENG-001    → First rule in Engineering Standards
  ARCH-014   → Fourteenth rule in Architecture & Project Structure
  CODE-003   → Third rule in Coding Standards
  SEC-007    → Seventh rule in Security Conventions
```

| Convention | Prefix |
|---|---|
| 01 — Engineering Standards | `ENG` |
| 02 — Architecture & Project Structure | `ARCH` |
| 03 — Coding Standards | `CODE` |
| 04 — Naming Conventions | `NAME` |
| 05 — Error Handling & Logging | `ERR` |
| 06 — Security Conventions | `SEC` |
| 07 — Database Conventions | `DB` |
| 08 — API Design Conventions | `API` |
| 09 — Testing Conventions | `TEST` |
| 10 — UI/UX Conventions | `UI` |
| 11 — Performance Conventions | `PERF` |
| 12 — Documentation Conventions | `DOC` |
| 13 — Version Control & CI/CD | `VCS` |
| 14 — Dependency Management | `DEP` |
| 15 — Localization & Internationalization | `L10N` |
| 16 — DevOps & Deployment | `OPS` |
| 17 — Checklists & Templates | `CHK` |

---

## 5. RFC 2119 Keyword Reference

All convention documents use keywords as defined in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). The table below summarizes their meaning:

| Keyword | Meaning | Violation Severity |
|---|---|---|
| **MUST** | Absolute requirement. No exceptions. | 🔴 Blocker — Code MUST NOT be merged. |
| **MUST NOT** | Absolute prohibition. No exceptions. | 🔴 Blocker — Code MUST NOT be merged. |
| **SHOULD** | Strong recommendation. May be skipped only with documented justification in the PR description. | 🟡 Warning — Reviewer MUST request justification. |
| **SHOULD NOT** | Discouraged. May be done only with documented justification in the PR description. | 🟡 Warning — Reviewer MUST request justification. |
| **MAY** | Truly optional. No justification needed. | 🟢 Info — No action required. |

> [!IMPORTANT]
> When these keywords appear in **UPPERCASE BOLD** within convention documents, they carry their RFC 2119 meaning. When they appear in lowercase in normal prose, they carry their everyday English meaning.

---

## 6. Directory Structure

```
rules/
├── README.md                              ← You are here
├── GLOSSARY.md                            ← Shared terminology definitions
│
├── 01-engineering-standards.md            ← SOLID, DRY, reuse, no hardcoded strings
├── 02-architecture-and-project-structure.md
├── 03-coding-standards.md
├── 04-naming-conventions.md
├── 05-error-handling-and-logging.md
├── 06-security-conventions.md
├── 07-database-conventions.md
├── 08-api-design-conventions.md
├── 09-testing-conventions.md
├── 10-ui-ux-conventions.md
├── 11-performance-conventions.md
├── 12-documentation-conventions.md
├── 13-version-control-and-cicd.md
├── 14-dependency-management.md
├── 15-localization-and-internationalization.md
├── 16-devops-and-deployment.md
├── 17-checklists-and-templates.md
│
└── extensions/                            ← Framework-specific conventions
    ├── aspnet-conventions.md              ← ASP.NET Core / Web API specifics
    ├── blazor-conventions.md              ← Blazor component & UI specifics
    └── sqlserver-conventions.md           ← SQL Server / EF Core specifics
```

### Core Conventions (01–17)

These are **technology-agnostic** wherever possible. They define *what* must be done, not *how* a specific framework does it.

### Extensions (`extensions/`)

Extensions add **framework-specific** rules that implement or refine the core conventions. They:

- **MUST** reference the core Rule ID they extend (e.g., "This rule extends `DB-005`").
- **MUST NOT** contradict any core convention rule.
- **MAY** introduce framework-specific Rule IDs with their own prefix (e.g., `ASPNET-001`, `BLAZOR-003`, `SQLSRV-010`).

| Extension File | Covers | Extends Conventions |
|---|---|---|
| `aspnet-conventions.md` | ASP.NET Core middleware, DI registration, controller patterns, minimal APIs, JWT configuration | 02, 03, 06, 08 |
| `blazor-conventions.md` | Blazor component architecture, BEM CSS, SignalR integration, component reuse, design tokens | 03, 10, 15 |
| `sqlserver-conventions.md` | SQL Server specifics, EF Core 9.0 configuration, migration strategy, indexing, stored procedures | 07, 11 |

---

## 7. How to Extend the System

### Adding a New Extension

1. Create a new file under `extensions/` with the naming pattern `{framework}-conventions.md`.
2. Follow the exact same template as core conventions (Purpose, Scope, Principles, Mandatory Rules, etc.).
3. Assign a unique Rule ID prefix (e.g., `REACT-`, `FLUTTER-`, `DOCKER-`).
4. In the Scope section, explicitly list which core conventions (by number) the extension refines.
5. Update this README's directory structure and extension table.

### Adding Rules to an Existing Convention

1. Assign the next sequential Rule ID within that convention's prefix.
2. Include all required fields: Rule statement, Rationale, and Consequence.
3. If the new rule relates to another convention, add a cross-reference.
4. Update the convention's `Last Updated` date.

### Proposing a New Core Convention

This is rare and requires significant justification. The hierarchy (01–17) is designed to be comprehensive. Before proposing a new core convention:

1. Verify the topic is not already covered by an existing convention.
2. Verify it cannot be an extension instead.
3. If it truly is a new cross-cutting concern, propose it as a PR with the full template and a justification for its hierarchy position.

---

## 8. Cross-Referencing Rules

When one convention's rules relate to another convention, use this format:

```markdown
As required by `ENG-005` (see [01-engineering-standards](./01-engineering-standards.md)),
all user-facing strings MUST be externalized.
```

### Cross-Reference Guidelines

- **MUST** use the Rule ID, not just the convention name.
- **SHOULD** include a Markdown link to the referenced document.
- **MUST NOT** duplicate the full text of the referenced rule — summarize and link.
- **SHOULD** cross-reference in both directions when two rules are tightly coupled.

---

## 9. Quick Reference — Which Convention to Consult

| Scenario | Primary Convention | Also Consult |
|---|---|---|
| Setting up a new project or module | 02 — Architecture | 01 — Engineering Standards |
| Writing a new class or function | 03 — Coding Standards | 04 — Naming Conventions |
| Choosing a variable or method name | 04 — Naming Conventions | 03 — Coding Standards |
| Adding error handling or logging | 05 — Error Handling | 03 — Coding Standards |
| Implementing authentication/authorization | 06 — Security | 08 — API Design |
| Creating or modifying database tables | 07 — Database | `extensions/sqlserver-conventions.md` |
| Designing a REST endpoint | 08 — API Design | `extensions/aspnet-conventions.md` |
| Writing tests | 09 — Testing | 03 — Coding Standards |
| Building a UI component | 10 — UI/UX | `extensions/blazor-conventions.md` |
| Optimizing slow queries or pages | 11 — Performance | 07 — Database, 10 — UI/UX |
| Writing docs, comments, or READMEs | 12 — Documentation | — |
| Branching, merging, or CI pipeline | 13 — Version Control | 16 — DevOps |
| Adding a NuGet/npm package | 14 — Dependency Mgmt | 06 — Security |
| Externalizing strings / multi-language | 15 — Localization | 10 — UI/UX |
| Deploying to staging or production | 16 — DevOps | 13 — Version Control |
| Pre-merge self-review | 17 — Checklists | All applicable conventions |

---

## 10. Core Tenets (Cross-Cutting)

These four tenets are embedded throughout every convention and are called out here for emphasis:

### 10.1 Strict SOLID Compliance

Every convention reinforces SOLID principles where applicable. Classes, modules, components, and services MUST each have a single responsibility, depend on abstractions, and be open for extension but closed for modification.

### 10.2 No Hardcoded Text

All user-facing strings, error messages, validation messages, labels, and tooltips MUST be externalized to resource files, constants classes, or localization systems. No magic strings in logic code. See `15-localization-and-internationalization.md` for full rules.

### 10.3 Component-Based UI

UI MUST be built with reusable components. Inline or ad-hoc HTML/CSS is prohibited. Before creating a new component, developers MUST verify no existing component serves the purpose. See `10-ui-ux-conventions.md` and `extensions/blazor-conventions.md`.

### 10.4 Mandatory Reuse

Always prefer reusing existing code, components, services, and utilities over creating duplicates. Duplication is treated as technical debt. See `01-engineering-standards.md` for the foundational rule.

---

## 11. Document Template

Every convention document MUST follow this template structure:

```markdown
# [Convention Name]

> **Rule Hierarchy Level:** [01-17]
> **Applies To:** [Backend / Frontend / Mobile / Database / All]
> **Last Updated:** YYYY-MM-DD

## Purpose
## Scope
## Principles
## Mandatory Rules
## Recommended Practices
## Anti-patterns
## Examples
## Checklist
```

See any existing convention document for a complete example.

---

## 12. Versioning & Change Log

- Convention documents follow [Semantic Versioning](https://semver.org/) at the system level.
- Each document tracks its own `Last Updated` date.
- Breaking changes to MUST-level rules require a major version bump of the Rule System.
- New SHOULD-level rules or clarifications require a minor version bump.
- Typo fixes and formatting changes require a patch version bump.

---

## Glossary

For shared terminology definitions used across all conventions, see [GLOSSARY.md](./GLOSSARY.md).
