# Milestone 1 (R1: Security & Auth) Handoff Report

## 1. Observation
- **Original PIN Verification Flaws**:
  - `src/Restaurant.Infrastructure/Services/IdentityService.cs` contained hardcoded manager username filtering (`u.Username == "admin" || u.Username == "manager01"`), bypass PINs (`pin == "000000"`, `pin == "000003"`, `pin == "123456"`), plain equality check without role enforcement, and no rate limiting or lockout.
  - `src/Restaurant.Web/Services/MockIdentityService.cs` contained bypass checks for `000000` and `000003`.
- **Sensitive Controller Authorization Gaps**:
  - `[AllowAnonymous]` was present across sensitive mutation actions in `PosOrdersController` (`ApproveOrder`, `MoveTable`, `MergeTables`, `CancelOrder`, `VoidOrderItem`), `PaymentsController`, `ShiftsController` (`OpenShift`, `CloseShift`, `ReopenShift`), `CashierToolsController` (`PerformTillIn`, `PerformTillOut`, `PerformCashAdjustment`), `ApprovalCenterController` (`CreateRequest`, `ApproveRequest`, `RejectRequest`), `ReconciliationController` (`SaveShiftReconciliation`), and `VouchersController`.
  - `DashboardController` lacked role restrictions.
- **Audit/Security User ID Fallback Gaps**:
  - Multiple controller and service actions contained silent fallback defaults: `currentUserService.UserId ?? 1` or `orderItem.Order?.CreatedByUserId ?? 1`, masking unauthenticated requests and corrupting audit logs.
- **Blazor -> WebAPI Auth Chain Broken**:
  - In `SharedLogin.razor.cs`, upon successful PIN authentication, the page previously redirected directly to `/pos` or `/kds` via client-side Blazor routing without establishing the `RestaurantAuth` authentication cookie or updating `AuthenticationStateProvider`.
  - WebAPI HTTP clients registered in `src/Restaurant.Web/Program.cs` did not attach the Bearer JWT token to outgoing requests.
  - `PosHeader.razor` displayed a static hardcoded cashier name ("Lê Thị Thu").
- **Verification Run Results**:
  - Solution build: `dotnet build src/Restaurant.sln` -> `0 Warning(s)`, `0 Error(s)`.
  - Test suite: `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName!~Restaurant.Domain.Tests.E2E"` -> `Passed! - Failed: 0, Passed: 100, Skipped: 0, Total: 100` (80 baseline + 20 M1 tests).

---

## 2. Logic Chain
1. **Manager PIN Security Enforcement**:
   - Removed all hardcoded username filters and bypass PINs in `IdentityService.VerifyManagerPinAsync`.
   - Queried active users belonging to either the `Admin` or `Manager` role (via `Role.Name == "Admin" || Role.Name == "Manager"`).
   - Validated PINs strictly using `BCrypt.Net.BCrypt.Verify(pin, u.PinHash)`.
   - Added rate-limiting and account lockout: 5 consecutive failed attempts lock the manager PIN verification for 15 minutes, throwing `InvalidOperationException("MANAGER_PIN_LOCKED:...")`.
   - Exposed `ResetManagerPinAttempts()` for testing and administrative reset.
   - Updated `AuthController.VerifyManagerPin` to catch lockout exceptions and return HTTP 429 (`PIN_LOCKED`).
2. **Controller Lockdown & Role-Based Access Control**:
   - Audited all 8 sensitive controllers and applied explicit `[Authorize(Roles = "...")]` attributes:
     - `PosOrdersController`: `[Authorize(Roles = "Admin,Manager,Cashier")]` on order actions; Manager/Admin required for cancellation and item voiding.
     - `PaymentsController`: `[Authorize(Roles = "Admin,Manager,Cashier")]` on `CashPayment`; `SepayWebhook` retained `[AllowAnonymous]` protected by HMAC signature verification.
     - `ShiftsController`: `[Authorize(Roles = "Admin,Manager,Cashier")]` on shift lifecycle actions.
     - `CashierToolsController`: `[Authorize(Roles = "Admin,Manager,Cashier")]` on till actions.
     - `ApprovalCenterController`: `[Authorize(Roles = "Admin,Manager,Cashier")]` on requests, restricted approvals/rejections to `Admin,Manager`.
     - `ReconciliationController`: `[Authorize(Roles = "Admin,Manager")]` on shift reconciliations.
     - `VouchersController`: `[Authorize(Roles = "Admin")]` on voucher create/update/delete mutations; `[AllowAnonymous]` retained on customer voucher validation.
     - `DashboardController`: `[Authorize(Roles = "Admin,Manager")]` on metrics queries.
3. **Elimination of `UserId ?? 1` Fallbacks**:
   - Replaced all instances of `_currentUserService.UserId ?? 1` across controllers (`PosOrdersController`, `ShiftsController`, `CashierToolsController`, `ApprovalCenterController`, `ReconciliationController`, `InventoryController`) with strict checks returning `Unauthorized()` (HTTP 401) when `UserId` is null.
   - Removed fallback defaults in `KitchenService` and `InventoryService` audit logging.
4. **Blazor Auth State & Bearer Token Propagation**:
   - In `SharedLogin.razor.cs`, after obtaining the JWT token via PIN login, the component triggers a navigation to `/Account/Login?token={jwt}&returnUrl={dest}` with `forceLoad: true`.
   - `AccountController.Login` decrypts the token claims, generates the `RestaurantAuth` authentication cookie, and redirects to the sanitized `returnUrl`.
   - Created `BlazorAuthorizationMessageHandler` inheriting `DelegatingHandler` that extracts the `jwt_token` claim from `AuthenticationStateProvider` and attaches `Authorization: Bearer <token>` to all HTTP requests to WebAPI.
   - In `Program.cs`, attached `BlazorAuthorizationMessageHandler` to both `Api` and `AdminApiClient`.
   - Updated `PosHeader.razor` to dynamically display the authenticated user's name and role using `<AuthorizeView>`.
   - Removed all bypass PINs from `MockIdentityService.cs`.
5. **Comprehensive Automated Test Coverage**:
   - Created `src/Restaurant.Domain.Tests/IdentityServiceTests.cs` (7 unit tests): valid admin/manager PIN passes, cashier PIN fails, invalid PIN fails, bypass PINs fail, lockout triggers after 5 failed attempts, null/empty PIN handling.
   - Created `src/Restaurant.Domain.Tests/AuthorizationIntegrationTests.cs` (13 integration tests): unauthenticated requests to all 13 critical endpoints verify HTTP 401 Unauthorized enforcement.

---

## 3. Caveats
- E2E tests (`Tier1` through `Tier4`) in `Restaurant.Domain.Tests.E2E` exercise cross-milestone features assigned to other agents (e.g., table status release & UI cash payment flow in M2, SignalR hub JWT authentication in M4, waste conversion in M5, table merge in M6). All M1 tests (`F01_R1_01` through `F05_R1_05`) in E2E pass completely.
- WebAPI in-memory lockout for manager PIN is tracked per application process lifecycle. For multi-node distributed environments, this would typically migrate to Redis.

---

## 4. Conclusion
Milestone 1 (R1: Security & Auth) is fully implemented, verified, and ready.
- Hardcoded manager PIN bypasses and hardcoded username filtering are eliminated.
- PIN verification uses genuine BCrypt hashing against active users with the `Admin` or `Manager` role, enforced with 5-failure 15-minute rate-limiting/lockout.
- All 8 sensitive controllers are secured with role-based authorization; anonymous access and silent fallback user IDs (`?? 1`) are eliminated in favor of strict HTTP 401 Unauthorized responses.
- The Blazor authentication flow preserves session claims across navigation and automatically attaches Bearer JWTs to backend API calls.
- Solution builds with **0 warnings and 0 errors**. All 100 baseline and auth unit/integration tests pass 100%.

---

## 5. Verification Method
1. **Build Solution**:
   ```bash
   dotnet build src/Restaurant.sln
   ```
   *Expected output*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

2. **Run Unit & Integration Tests**:
   ```bash
   dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName!~Restaurant.Domain.Tests.E2E"
   ```
   *Expected output*: `Passed! - Failed: 0, Passed: 100, Skipped: 0, Total: 100`.

3. **Verify IdentityService Unit Tests**:
   ```bash
   dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName~IdentityServiceTests"
   ```
   *Expected output*: 7 passed.

4. **Verify Authorization Integration Tests**:
   ```bash
   dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName~AuthorizationIntegrationTests"
   ```
   *Expected output*: 13 passed (HTTP 401 returned for all unauthenticated sensitive endpoints).
