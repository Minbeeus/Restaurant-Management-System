# Git Conventions

> **Rule Hierarchy Level:** 13
> **Applies To:** All
> **Last Updated:** 2026-07-14

## Purpose
This document establishes the standards for repository management, branching strategies, commit messages, and the code integration process. It ensures a clean, traceable commit history, painless merge flows, and robust CI/CD gatekeeping.

## Scope
This convention covers branch naming, Conventional Commits formatting, Pull Request reviews, Git ignore rules, and release tagging. It does NOT cover infrastructure deployments (see [Deployment Convention](file:///d:/My%20Project/rules/15-deployment/deployment-convention.md)).

## Principles
1. **Traceable History:** Every commit MUST clearly document the intent and context of the changes.
2. **Short-Lived Branches:** Feature branches should remain focused and merge into the main integration branch quickly.
3. **Automated Gatekeeping:** Every pull request must satisfy automated quality checks before manual code review.
4. **Semantic Releases:** Releases must follow strict semantic versioning to protect consumers from breaking changes.

---

## Mandatory Rules

### GIT-001: Branch Naming Pattern
All branch names MUST follow a standard hierarchical naming format using lowercase letters and hyphens:
- Format: `[category]/[ticket-number]-[short-description]`
- Categories MUST be one of: `feature/` (new features), `bugfix/` (bug fixes), `hotfix/` (urgent production patches), `refactor/` (code redesign), `docs/` (documentation updates), or `chore/` (build updates).
- Example: `feature/pos-104-payment-idempotency`
- **Rationale:** Ensures all team members and automated build scripts can immediately categorize the purpose of any branch.
- **Consequence:** Arbitrary branch names (`my-changes`, `fix1`) complicate pipeline tracking and obscure branch ownership.

### GIT-002: Conventional Commits Format
All commit messages MUST follow the **Conventional Commits 1.0.0** specification.
- Format: `<type>(<scope>): <description>`
- Types MUST be one of: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`.
- The subject line MUST NOT exceed 72 characters, must start with a lowercase letter, and must not end with a period.
- **Rationale:** Standardizes commit history, enabling automated changelog generation and automatic semantic version calculation.
- **Consequence:** Cryptic commits (`fixed bug`, `changes`) force developers to read code diffs simply to understand the commit history.

### GIT-003: No Committed Secrets
Secrets, API keys, connection strings, personal tokens, and environment-specific configuration files MUST NOT be committed to git.
- **Rationale:** Exposing credentials in public or private repositories poses a major security compromise.
- **Consequence:** Exposing security tokens leads to database breaches and compromised deployment infrastructure.

### GIT-004: Clean Commit History
Before a Pull Request is merged into the main integration branch, the branch history MUST be squashed or rebased to remove redundant, non-descriptive WIP commits (e.g., "wip", "typo", "fix build").
- **Rationale:** Keeps the main branch history clean and understandable, making it easy to identify regressions or roll back features.
- **Consequence:** Main branch history cluttered with hundreds of intermediate syntax error fixes obscures actual feature integrations.

---

## Recommended Practices

### GIT-050: Small Pull Requests
Pull Requests SHOULD contain fewer than 400 lines of code changes and address a single user story or bug.
- **Rationale:** Small, focused PRs receive higher quality reviews, catch more bugs, and merge with minimal conflict.

### GIT-051: Keep Branch Synced
Developers SHOULD regularly merge or rebase their feature branches against the main development branch to prevent complex merge conflicts at the end of the sprint.
- **Rationale:** Resolving minor conflicts early is significantly safer than resolving large, monolithic conflicts.

---

## Anti-patterns

### Commit-and-Fix Loop
Committing syntax errors, compiling locally on the CI pipeline, and committing subsequent line changes (`fix build`, `fix build 2`, `actually fixed`).
- **Why it's harmful:** Floods git history with noise and wastes build runner resources.
- **What to do instead:** Run compile and test scripts locally before pushing code to the remote repository.

### Giant Monolithic PRs
Submitting a Pull Request that implements 5 different features across 50 files and 3,000 lines of code.
- **Why it's harmful:** Reviews become superficial, security holes are missed, and merge conflicts become paralyzing.
- **What to do instead:** Break the task down into incremental, independent PRs with feature flags if necessary.

---

## Examples

### ✅ Correct (Hierarchical branch, Conventional commit, scoped)

**Branch Name:** `feature/pos-104-payment-idempotency`

**Commit Message:**
```text
feat(pos): add payment idempotency key validation

- Add validation for X-Idempotency-Key header on checkout endpoints
- Cache payment tokens in memory database for 120 seconds
- Return 409 Conflict status code on duplicate request keys

Closes: #POS-104
```

### ❌ Incorrect (Non-standard branch, vague commit, containing credentials)

**Branch Name:** `working-branch`

**Commit Message:**
```text
updated the config connection string to Server=prod-sql;User=admin;Password=SecretPass123; and fixed controller error
```

---

## Checklist
- [ ] Does your branch name follow the `category/ticket-short-description` format?
- [ ] Do all commit messages conform to the Conventional Commits format?
- [ ] Have you verified that no connection strings, passwords, or secrets are in the diff?
- [ ] Have you squashed intermediate, duplicate, or "wip" commits before merging?
- [ ] Does the Pull Request focus on a single ticket, containing minimal changes?
