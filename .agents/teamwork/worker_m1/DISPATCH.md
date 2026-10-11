# DISPATCH — 2026-10-11T19:20:00Z

## Task Assignment
- Archetype: teamwork_preview_worker
- Role: Security & Authentication Engineer (Agent 1)
- Working Directory: d:\My Project\.agents\teamwork\worker_m1
- Project Directory: d:\My Project
- Parent Orchestrator ID: a1e31680-d6b1-4328-b285-afb2f318f8f5
- Original Request: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: d:\My Project\PROJECT.md
- Survey Reports to Read First:
  - `d:\My Project\.agents\teamwork\survey_explorer_backend\handoff.md` (§1.1)
  - `d:\My Project\.agents\teamwork\survey_spec_miner_1\handoff.md` (§1.1, §2.1)
  - `d:\My Project\.agents\teamwork\survey_explorer_frontend\handoff.md` (§1.1)

## Exclusive File Ownership
- `src/Restaurant.Infrastructure/Services/IdentityService.cs`
- `src/Restaurant.WebAPI/Controllers/AuthController.cs`
- `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs`
- `src/Restaurant.WebAPI/Controllers/PaymentsController.cs`
- `src/Restaurant.WebAPI/Controllers/ShiftsController.cs`
- `src/Restaurant.WebAPI/Controllers/CashierToolsController.cs`
- `src/Restaurant.WebAPI/Controllers/ApprovalCenterController.cs`
- `src/Restaurant.WebAPI/Controllers/ReconciliationController.cs`
- `src/Restaurant.WebAPI/Controllers/VouchersController.cs`
- `src/Restaurant.WebAPI/Controllers/DashboardController.cs`
- `src/Restaurant.WebAPI/Controllers/InventoryController.cs` (fallback UserId removal)
- `src/Restaurant.Application/Services/KitchenService.cs` & `InventoryService.cs` (fallback UserId removal)
- `src/Restaurant.Web/Components/Pages/Auth/SharedLogin/SharedLogin.razor.cs`
- `src/Restaurant.Web/Controllers/AccountController.cs`
- `src/Restaurant.Web/Program.cs`
- `src/Restaurant.Web/Services/MockIdentityService.cs`
- `src/Restaurant.Web/Services/TokenAuthenticationStateProvider.cs` or DelegatingHandler
- `src/Restaurant.Domain.Tests/IdentityServiceTests.cs` (or new AuthTests.cs)

## Objective: Implement Milestone 1 (R1: Security & Auth)
1. **Fix `IdentityService.VerifyManagerPinAsync`**:
   - Remove `u.Username == "admin" || u.Username == "manager01"` condition.
   - Remove hardcoded PINs (`000000`, `000003`, `123456`).
   - Query users with role `Admin` or `Manager` (e.g. `u.Role.Name == "Admin" || u.Role.Name == "Manager"`).
   - Verify PIN using `BCrypt.Net.BCrypt.Verify(pinCode, u.PinCodeHash)`.
   - Add lockout mechanism after 5 failed attempts (lockout for 15 minutes, consistent with `PinLoginAsync`).
2. **Lockdown sensitive endpoints**:
   - Remove `[AllowAnonymous]` from:
     - `PosOrdersController`: CancelOrder, VoidOrderItem, MoveTable, MergeTables, ApproveOrder.
     - `PaymentsController`: CashPayment.
     - `ShiftsController`: OpenShift, CloseShift, GetCurrentShift.
     - `CashierToolsController`: TillIn, TillOut, CashAdjustment.
     - `ApprovalCenterController`: ApproveRequest, RejectRequest, ApproveByManagerPin.
     - `ReconciliationController`: SaveShiftReconciliation, GetShiftReconciliationPreview.
     - `VouchersController`: CreateVoucher, UpdateVoucher, DeleteVoucher.
   - Add `[Authorize(Roles = "Admin,Manager")]` to `DashboardController` endpoints.
   - Ensure proper `[Authorize]` / `[Authorize(Roles = "...")]` on all modified endpoints.
3. **Eliminate fallback `UserId = currentUserService.UserId ?? 1`**:
   - Remove all `?? 1` occurrences across controllers and services. Enforce 401 Unauthorized if unauthenticated.
4. **Restore Blazor -> WebAPI auth chain**:
   - `SharedLogin.razor.cs`: Upon successful `PinLoginAsync`, navigate to `AccountController.Login?token={token}&returnUrl={areaUrl}`.
   - `AccountController.cs`: Support `returnUrl` query parameter to redirect appropriately to `/pos`, `/kds`, or `/admin/dashboard`.
   - Set up `RestaurantAuth` cookie and claims.
   - Configure a DelegatingHandler in `Restaurant.Web` so all HTTP client calls to WebAPI automatically include `Authorization: Bearer <token>`.
5. **Verify & Test**:
   - Run `dotnet build src/Restaurant.sln` — must succeed with 0 Warnings and 0 Errors.
   - Run `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj` — all tests must pass, plus add tests verifying VerifyManagerPinAsync logic.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Deliverables
- Write `handoff.md` in `d:\My Project\.agents\teamwork\worker_m1\handoff.md` with:
  - Observation: Files modified, exact diffs
  - Logic Chain: How changes satisfy R1 and maintain backward compatibility
  - Caveats: Any observations
  - Conclusion: Status of M1
  - Verification Method: Exact build and test commands run with complete outputs
- Notify orchestrator via `send_message`.
