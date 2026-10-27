# BRIEFING — 2026-10-14T17:35:00Z

## Mission
Comprehensive audit of BOM (Bill of Materials), Recipe Management, Unit Conversion, and Consumption Norms across UI, API, Domain, EF Core, and Tests.

## 🔒 My Identity
- Archetype: explorer
- Roles: BOM, Recipe, and Unit Conversion Auditor
- Working directory: d:\My Project\.agents\teamwork\explorer_agent1
- Original parent: 326badef-f4ff-4494-a54a-1a9ac14c000c
- Milestone: Comprehensive BOM & Inventory Audit (Agent 1)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.
- Only write inside d:\My Project\.agents\teamwork\explorer_agent1\
- Classify findings: DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE.
- Prioritize: P0, P1, P2, P3.
- Every finding MUST cite exact file path, line numbers, class/method name, and code snippet evidence.

## Current Parent
- Conversation ID: 326badef-f4ff-4494-a54a-1a9ac14c000c
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs`, `AuthAndMenuEntities.cs`
  - `src/Restaurant.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`, `ApplicationDbContext.cs`
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs`, `KitchenService.cs`, `PosOrderService.cs`, `CancelOrderService.cs`
  - `src/Restaurant.WebAPI/Controllers/InventoryController.cs`
  - `src/Restaurant.Web/Components/Pages/Admin/Menu/RecipeManagement.razor`, `ModifierManagement.razor`
  - `src/Restaurant.Web/Components/Pages/Admin/Inventory/MaterialManagement.razor`, `GoodsReceiptManagement.razor`, `StocktakeManagement.razor`
  - `src/Restaurant.Domain.Tests/` (BohServicesTests, ComboEndToEndQaTests, Tier1_FeatureCoverageTests, E2ETestContext)
- **Key findings**:
  1. `UnitConversion` table is dormant / never queried in deductions or goods receipts (BACKEND_ONLY, P0).
  2. `decimal(18,2)` limits precision, truncating small grams (0.001kg) to 0.00kg (BROKEN, P0).
  3. `ModifierRecipe` has backend entities/services but 100% missing UI in `ModifierManagement.razor` (BACKEND_ONLY, P0).
  4. Single-item BOM works for base units; Combo child items BOM deductions work, but Combo Header BOM is ignored in KitchenService (DONE_VERIFIED for children / BROKEN for header packaging).
  5. Semi-finished goods (Multi-level BOM) are architecturally unsupported (NOT_STARTED, P1).
  6. Void item after kitchen done leaks inventory (no reversing transactions) (BROKEN, P1).
  7. `InventoryLossReport.razor` uses fake 35% hardcoded waste calculation in `InventoryService.cs:734` (BROKEN, P1).
  8. `MaterialManagement.razor:197` hardcodes UOMs with option value 6 ("Lon") misaligned with DB ID 6 ("Gói") (BROKEN, P1).
- **Unexplored areas**: None within Agent 1 scope. Complete investigation accomplished.

## Key Decisions Made
- Fully documented all 13 critical findings with exact code references and evidence snippets in handoff.md.
- Prepared 5 business decisions required for Lead Architect / Product Owner.

## Artifact Index
- progress.md — Liveness heartbeat and step tracker
- DISPATCH.md — Received dispatch message
- handoff.md — Final comprehensive 5-component audit report
