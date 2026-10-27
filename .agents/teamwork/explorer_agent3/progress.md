# Progress Tracker - Agent 3 (POS/KDS, Inventory Deduction, Cancellation & Add-ons)

Last visited: 2026-10-14T17:33:30Z
Status: COMPLETED

## Tasks
- [x] 0. Read ORIGINAL_REQUEST.md & Project Architecture
- [x] 1. Inventory Deduction Timing & Level (Order vs Item level, exact lines/events)
  - Timing: Deducted ONLY on `KitchenStatus.Done` via `OrderItemKitchenDoneDomainEvent` in `KitchenService.cs:220-228`.
  - Level: Item level (`OrderItemId` in `InventoryService.cs:740`).
  - Gaps: No deduction on POS creation, no deduction on Send to Kitchen, no deduction on Payment. Items without KDS Done (drinks/direct items or forgotten tickets) never get deducted.
- [x] 2. SignalR & Domain Events (Sync vs Async/Outbox, Idempotency & retry risks)
  - Synchronous MediatR in-process invocation, NO Outbox pattern, NO BackgroundWorker.
  - Two-phase `SaveChangesAsync`: first saves `OrderItem.KitchenStatus = Done`, second saves BOM deductions in `InventoryService`. Concurrency error in second save leaves item Done without deduction.
  - Idempotency guards: Sequential retries guarded by status check + `alreadyDeducted` query. Race condition guarded by `Material.RowVersion` (causes 500 error on 2nd request). No unique DB index on `InventoryTransaction`.
- [x] 3. Void / Cancel Matrix (Before cooking, during cooking, after cooking/served, waste/scrap, manager PIN)
  - Whole order cancel converts `SaleDeduction` -> `CancellationWaste` in `CancelOrderService.cs:93`.
  - Critical Defect (P0): `VoidOrderItemAsync` (`PosOrderService.cs:896`) NEVER converts `SaleDeduction` to `CancellationWaste` for individual voided items.
  - Critical Defect (P0): POS UI Manager PIN authorization sends HTTP request with Cashier's JWT token, causing HTTP 403 Forbidden on `CancelOrder` and `VoidOrderItem` endpoints. POS UI swallows error.
  - Critical Defect (P1): BOH Approval Center `ApproveRequestAsync` for `CancelOrder` sets status directly and bypasses `CancelOrderService` (no waste conversion, no refund, no loyalty rollback).
  - Critical Defect (P1): BOH `InventoryLossReport.razor` computes `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0)` (fake 35% formula) instead of aggregating actual `CancellationWaste` transactions.
  - `CancellationReturn = 4` is dead enum code, never used.
- [x] 4. Add-on items / Multiple rounds (Order update vs item append, inventory deduction isolation)
  - Updates existing `Order` record, appends new `OrderItem` records with `KitchenStatus = Pending`.
  - Previous items NOT deducted again; deductions strictly isolated by `OrderItemId`.
  - Bug: `Order.Status` remains `Ready` if first round was already served, not reset to `InPreparation`.
- [x] 5. Test suite execution completion & verification (295/295 passed)
- [x] 6. Compile exhaustive handoff.md report and notify orchestrator
