# DISPATCH — 2026-10-11T19:04:30Z

## Task Assignment
- Archetype: teamwork_preview_spec_miner
- Working Directory: d:\My Project\.agents\teamwork\survey_spec_miner_1
- Parent Orchestrator ID: a1e31680-d6b1-4328-b285-afb2f318f8f5
- Original Request: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md

## Scope & Objective
Thoroughly probe and extract authoritative requirements from:
1. `d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `d:\My Project\docs\audit\FULL_WORKSPACE_AUDIT.md`
3. All spec files in `d:\My Project\docs\specs\` (`POS-spec.md`, `KDS-spec.md`, `BOH-spec.md`, `ACC-spec.md`, `QR-spec.md`)

## Specific Requirements to Map:
- R1: Security & Auth (VerifyManagerPinAsync, [AllowAnonymous] removal, Blazor -> WebAPI auth chain, eliminate fallback UserId = 1)
- R2: POS & Cashier (Cash & VietQR/SePay payment flows, PosPaymentModal button in PosMain/OrderCart, DB transactions, idempotency)
- R3: Admin Reliability & SystemSettings DB persistence (Remove mock fallbacks in AdminApiClient, persist SystemSettings in DB via EF Core migration)
- R4: SignalR & KDS Realtime (Authenticate RestaurantHub, protect groups by claims, keep 'Ready' state when dishes are done, ensure 1-time BOM deduction event)
- R5: Inventory, BOM & Wastage (Inventory deduction integrity, cancellation waste handling, tests for InventoryService)
- R6: Split Bill & ESC/POS Printing (Split Bill UI and backend transaction, ESC/POS LAN printing abstraction)
- R7: Comprehensive Testing & Documentation (Security integration tests, 100% tests pass, documentation updates)

## Deliverables
Produce a structured handoff report in `d:\My Project\.agents\teamwork\survey_spec_miner_1\handoff.md` detailing:
- Enumerated Feature Inventory with source citations and acceptance criteria
- Business rules, constraints, edge cases, error conditions, role requirements
- When complete, notify the orchestrator via `send_message`.


## 2026-10-11T19:04:45Z
Read d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md and d:\My Project\.agents\teamwork\survey_spec_miner_1\DISPATCH.md.
Investigate docs/audit/FULL_WORKSPACE_AUDIT.md and all specifications in docs/specs/ (POS-spec.md, KDS-spec.md, BOH-spec.md, ACC-spec.md, QR-spec.md).
Extract and structure the authoritative requirements, business rules, constraints, edge cases, and acceptance criteria for R1-R7.
Write your comprehensive handoff report to d:\My Project\.agents\teamwork\survey_spec_miner_1\handoff.md.
When done, message the orchestrator via send_message.
