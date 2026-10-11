# DISPATCH — 2026-10-11T19:20:00Z

## Task Assignment
- Archetype: teamwork_preview_test_writer
- Role: E2E Test Suite Lead (Track 2)
- Working Directory: d:\My Project\.agents\teamwork\e2e_test_writer
- Project Directory: d:\My Project
- Parent Orchestrator ID: a1e31680-d6b1-4328-b285-afb2f318f8f5
- Original Request: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: d:\My Project\PROJECT.md

## Exclusive File Ownership
- `d:\My Project\TEST_INFRA.md`
- `d:\My Project\TEST_READY.md`
- `src/Restaurant.Domain.Tests/E2E/` (all new test files created in this directory)

## Objective: Design & Write Comprehensive Opaque-Box E2E Test Suite
Derive tests strictly from user requirements and specifications (`ORIGINAL_REQUEST.md`, `PROJECT.md § Feature Inventory`, and `docs/specs/`), independent of implementation design.

Follow the systematic 4-tier methodology:
1. **Tier 1 - Feature Coverage**:
   - Enumerate all inventoried features. Provide test cases covering representative happy paths in isolation.
2. **Tier 2 - Boundary & Corner Cases**:
   - Limit testing, empty inputs, negative values, zero values, extreme values.
3. **Tier 3 - Cross-Feature Combinations**:
   - Pairwise feature interactions (e.g. Table change + Order items + Discount, Kitchen Done + Inventory BOM + Cancel Order, etc.).
4. **Tier 4 - Real-World Application Scenarios**:
   - Realistic multi-step dine-in restaurant workflows from opening shift, table seating, ordering, kitchen prep, split bill, payment, till-out, to closing shift.

### Coordination Requirements
1. Create `TEST_INFRA.md` at project root using the project pattern template.
2. Write test cases in `src/Restaurant.Domain.Tests/E2E/`.
3. Verify test execution via `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`.
4. When test suite is ready and passing, create `TEST_READY.md` at project root.
5. Write `handoff.md` in your working directory and notify the orchestrator via `send_message`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. Tests must be genuine assertions against real business logic. DO NOT hardcode trivial passes (`Assert.True(true)`), mock out everything to fake success, or circumvent domain rules.
