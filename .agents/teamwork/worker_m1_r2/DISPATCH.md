# DISPATCH — 2026-10-11T19:55:00Z

## Task Assignment
- Archetype: teamwork_preview_worker
- Role: Security Remediation Engineer (Milestone 1, Iteration 2)
- Working Directory: d:\My Project\.agents\teamwork\worker_m1_r2
- Project Directory: d:\My Project
- Parent Orchestrator ID: a1e31680-d6b1-4328-b285-afb2f318f8f5
- Original Request: d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: d:\My Project\PROJECT.md
- Gate Failure Reports:
  - `d:\My Project\.agents\teamwork\challenger_m1_2\handoff.md` (P0 in ApprovalService & fallback ?? 1)
  - `d:\My Project\.agents\teamwork\reviewer_m1_1\handoff.md` (Rate-limit keying & PosOrdersController roles)

## Exclusive File Ownership
- `src/Restaurant.Infrastructure/Services/ApprovalService.cs`
- `src/Restaurant.Infrastructure/Services/IdentityService.cs`
- `src/Restaurant.Infrastructure/Services/PosOrderService.cs`
- `src/Restaurant.WebAPI/Controllers/PosOrdersController.cs`
- `src/Restaurant.WebAPI/Controllers/SystemController.cs`
- `src/Restaurant.Web/Services/MockIdentityService.cs`
- `src/Restaurant.Domain.Tests/` (test fixes/additions)

## Required Remediation Actions
1. **Critical P0 Fix in `ApprovalService.ApproveByManagerPinAsync`** (`src/Restaurant.Infrastructure/Services/ApprovalService.cs`):
   - Remove `u.Username == "admin" || u.Username == "manager01"` check (L181).
   - Remove hardcoded bypass PINs `000000`, `000003`, `123456` (L185).
   - Remove manager fallback lookup (L201).
   - Authenticate Manager PIN by calling `_identityService.VerifyManagerPinAsync(request.ManagerPin)` OR verifying active users with `Admin` or `Manager` role using `BCrypt.Net.BCrypt.Verify(pin, u.PinCodeHash)`.
   - Throw `UnauthorizedAccessException` on invalid PIN.
2. **Fix Rate-Limiting Keying in `IdentityService.VerifyManagerPinAsync`** (`src/Restaurant.Infrastructure/Services/IdentityService.cs`):
   - Do NOT key lockout on `trimmedPin` (which allows brute-force by rotating PINs). Key consecutive failures on a global manager PIN attempt key (e.g. `"GLOBAL_MGR_PIN_LOCKOUT"`) or caller client identifier.
3. **Fix Role Authorization in `PosOrdersController.cs`**:
   - Ensure `CancelOrder` and `VoidOrderItem` endpoints require `[Authorize(Roles = "Admin,Manager")]`.
4. **Eliminate Remaining `?? 1` Fallbacks**:
   - `src/Restaurant.Infrastructure/Services/PosOrderService.cs:208`: `CreatedByUserId = activeShift?.UserId ?? 1` -> use acting user ID or enforce proper ID.
   - `src/Restaurant.WebAPI/Controllers/SystemController.cs:210`: `UserId = adminUser?.Id ?? 1`.
5. **Clean `MockIdentityService.cs:54`**:
   - Remove residual `pinCode == "123456"`.
6. **Build & Test Verification**:
   - Run `dotnet build src/Restaurant.sln` -> 0 warnings, 0 errors.
   - Run `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj` -> 100% tests pass.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Deliverables
- Write `handoff.md` in your working directory and notify orchestrator via `send_message`.


## 2026-10-11T19:55:34Z
Read d:\My Project\.agents\teamwork\ORIGINAL_REQUEST.md, d:\My Project\PROJECT.md, and d:\My Project\.agents\teamwork\worker_m1_r2\DISPATCH.md.
Also read the failure reports in d:\My Project\.agents\teamwork\challenger_m1_2\handoff.md and reviewer_m1_1\handoff.md.

Implement the 5-point remediation checklist:
1. Fix ApprovalService.ApproveByManagerPinAsync: eliminate u.Username == "admin", bypass PINs 000000/000003/123456, and fallback manager. Use genuine BCrypt verification against Manager/Admin or delegate to IdentityService.
2. Fix rate-limiting keying in IdentityService.VerifyManagerPinAsync: do not key on trimmedPin. Use global/client key so consecutive failed attempts with different PINs trigger lockout after 5 failures.
3. PosOrdersController: ensure CancelOrder and VoidOrderItem enforce [Authorize(Roles = "Admin,Manager")].
4. Remove remaining fallback `?? 1` in PosOrderService.cs:208 and SystemController.cs:210.
5. Remove residual pinCode == "123456" in MockIdentityService.cs.
Run dotnet build src/Restaurant.sln and dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj.
Write handoff report to d:\My Project\.agents\teamwork\worker_m1_r2\handoff.md and notify via send_message.

MANDATORY INTEGRITY WARNING: DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. Integrity violations WILL be detected and your work WILL be rejected.
