# BRIEFING — 2026-10-14T17:33:00Z

## Mission
Comprehensive audit of POS/KDS flow, inventory deduction triggers, item/order cancellation (void matrix & waste/scrap), and add-on item handling across UI, SignalR, Application/Domain, DB, and Tests.

## 🔒 My Identity
- Archetype: explorer
- Roles: auditor, investigator, analyzer
- Working directory: d:\My Project\.agents\teamwork\explorer_agent3
- Original parent: 326badef-f4ff-4494-a54a-1a9ac14c000c
- Milestone: Comprehensive POS/KDS & Inventory & Cancellation Audit

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- TUYỆT ĐỐI KHÔNG SỬA CODE, KHÔNG TẠO MIGRATION, KHÔNG RESET HOẶC SỬA DATABASE TRONG LẦN CHẠY NÀY.
- Follow full data flow: UI -> SignalR / Controller -> Application/Domain -> DB.
- Classify findings: DONE_VERIFIED, DONE_UNVERIFIED, PARTIAL, UI_ONLY, BACKEND_ONLY, BROKEN, NOT_STARTED, NEEDS_BUSINESS_DECISION, OUT_OF_SCOPE.
- Prioritize: P0 to P3.
- Exact evidence: file path, line numbers, class/method, code snippet.

## Current Parent
- Conversation ID: 326badef-f4ff-4494-a54a-1a9ac14c000c
- Updated: 2026-10-14T17:33:00Z

## Investigation State
- **Explored paths**:
  - `src/Restaurant.Infrastructure/Services/KitchenService.cs`
  - `src/Restaurant.Infrastructure/Services/InventoryService.cs`
  - `src/Restaurant.Infrastructure/Services/PosOrderService.cs`
  - `src/Restaurant.Infrastructure/Services/CancelOrderService.cs`
  - `src/Restaurant.Infrastructure/Services/PaymentService.cs`
  - `src/Restaurant.Infrastructure/Services/ApprovalService.cs`
  - `src/Restaurant.Infrastructure/Handlers/DeductInventoryOnOrderItemKitchenDoneHandler.cs`
  - `src/Restaurant.Infrastructure/Hubs/RestaurantHub.cs`
  - `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs`
  - `src/Restaurant.WebAPI/Controllers/KdsController.cs`
  - `src/Restaurant.WebAPI/Controllers/ApprovalCenterController.cs`
  - `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs`
  - `src/Restaurant.Web/Components/Pages/Pos/Components/PosCancelOrderModal.razor`
  - `src/Restaurant.Web/Components/Pages/Pos/Components/PosVoidItemModal.razor`
  - `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor`
  - `src/Restaurant.Web/Components/Pages/Kds/KdsMain/KdsDashboard.razor`
  - `src/Restaurant.Web/Components/Pages/Admin/Reports/InventoryLossReport.razor`
  - `src/Restaurant.Domain.Tests/` (All test suites: 295 passed tests)
- **Key findings**:
  1. BOM deduction is triggered ONLY on `KitchenStatus.Done` via `OrderItemKitchenDoneDomainEvent` (cấp món, không trừ khi tạo POS, gửi bếp, hay thanh toán).
  2. No Outbox pattern: In-process MediatR with two-phase split SaveChanges creates silent permanent inventory deduction loss on transient concurrency error.
  3. `VoidOrderItemAsync` NEVER converts `SaleDeduction` to `CancellationWaste` for cooked items (P0 bug masked by incomplete test `F24_R5_03`).
  4. POS Cashier authorization for Cancel/Void via Manager PIN fails with 403 Forbidden due to `[Authorize(Roles = "Admin,Manager")]` on API controller while sending Cashier token; UI swallows error.
  5. BOH `InventoryLossReport.razor` uses fake `Math.Round(totalLoss * 0.35m, 0)` formula; actual `CancellationWaste` records in DB are ignored.
  6. Add-on items isolate deductions properly by `OrderItemId` without double deduction, but fail to reset `Order.Status` from `Ready` to `InPreparation`.
  7. BOM deduction ignores `UnitConversion` tables and multiplies quantities directly.
- **Unexplored areas**: None within Agent 3 scope.

## Key Decisions Made
- Fully documented all 11 core findings with exact line numbers and code evidence in `handoff.md`.
- Executed `dotnet test` confirming 295/295 tests pass, verifying existing test suite gaps.

## Artifact Index
- `d:\My Project\.agents\teamwork\explorer_agent3\DISPATCH.md` — Inbound instructions
- `d:\My Project\.agents\teamwork\explorer_agent3\progress.md` — Liveness heartbeat and step tracking
- `d:\My Project\.agents\teamwork\explorer_agent3\BRIEFING.md` — Situational awareness memory
- `d:\My Project\.agents\teamwork\explorer_agent3\handoff.md` — Final audit deliverable report
