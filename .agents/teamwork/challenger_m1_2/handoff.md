# Milestone 1 (R1: Security & Auth) Challenger Report

**Verdict**: **REJECT**

---

## 1. Observation

### 1.1 Positive Verifications (Implemented Correctly by Worker)
1. **Anonymous Request Lockdown (HTTP 401)**:
   - Verified across 36 sensitive endpoints in `PosOrdersController`, `PaymentsController`, `ShiftsController`, `CashierToolsController`, `ApprovalCenterController`, `ReconciliationController`, `VouchersController`, `DashboardController`, and `InventoryController`.
   - All 36 endpoints returned `HttpStatusCode.Unauthorized` (HTTP 401) when invoked without a Bearer token.
2. **Cashier Role Restriction on Manager/Admin Endpoints (HTTP 403)**:
   - Verified across 18 administrative endpoints (Dashboard summaries, top-movers, charts, sales-feeds, Reconciliation previews/saves/summaries, Approval approve/reject, Voucher CRUD, Inventory purchase orders & stock audits, System settings, Auth admin/manager test endpoints).
   - All 18 endpoints returned `HttpStatusCode.Forbidden` (HTTP 403) when invoked with a valid Cashier JWT token.
3. **Null User ID Claim Handling (HTTP 401 instead of Admin fallback)**:
   - Verified across 15 endpoints requiring user identity (`/orders/{id}/cancel`, `/orders/{orderId}/items/{orderItemId}/void`, `/shifts/current`, `/shifts/open`, `/shifts/close`, `/shifts/{id}/reopen`, `/cashier/till-in/{shiftId}`, `/cashier/till-out/{shiftId}`, `/cashier/cash-adjustment/{shiftId}`, `/approvals/create`, `/approvals/{id}/approve`, `/approvals/{id}/reject`, `/reconciliation/save`, `/inventory/purchase-orders`, `/inventory/stock-audit`).
   - All 15 endpoints returned `HttpStatusCode.Unauthorized` (HTTP 401) when the token lacked `NameIdentifier` / `sub`, and none silently defaulted to `UserId = 1`.
4. **Manager PIN Lockout & Bypass Rejection on AuthController**:
   - `POST /api/v1/auth/verify-manager-pin` returned `isValid: false` for pins `000000`, `000003`, `999999`.
   - On the 5th consecutive failure with a pin, the endpoint responded with `HttpStatusCode.TooManyRequests` (HTTP 429, code `PIN_LOCKED`).

---

### 1.2 Critical Defect Discovered: P0 Security Bypass in `ApprovalService.ApproveByManagerPinAsync`
In [`src/Restaurant.Infrastructure/Services/ApprovalService.cs#L176-L213`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/ApprovalService.cs#L176-L213):
```csharp
176: var pin = request.ManagerPin.Trim();
177: var employeeCodeWithPrefix = "NV" + pin;
178: 
179: var managerUser = await _context.Users
180:     .Include(u => u.Role)
181:     .FirstOrDefaultAsync(u => (u.EmployeeCode == pin || u.EmployeeCode == employeeCodeWithPrefix || u.Username == "admin" || u.Username == "manager01") && u.IsActive);
182: 
183: if (managerUser == null || (managerUser.Role.Name != "Admin" && managerUser.Role.Name != "Manager"))
184: {
185:     if (pin != "000000" && pin != "000003" && pin != "123456")
186:     {
...
197:         throw new UnauthorizedAccessException("Mã PIN Quản lý không đúng hoặc không có quyền duyệt.");
198:     }
199: 
200:     // Fallback manager
201:     managerUser = await _context.Users.FirstOrDefaultAsync(u => u.Role.Name == "Admin" || u.Role.Name == "Manager");
202:     if (managerUser == null) throw new UnauthorizedAccessException("Không tìm thấy tài khoản Quản lý hợp lệ.");
203: }
...
208: return await ApproveRequestAsync(managerUser.Id, new ProcessApprovalRequestDto
209: {
210:     ApprovalRequestId = request.ApprovalRequestId,
211:     ApprovalNote = string.IsNullOrEmpty(request.ApprovalNote) ? "Duyệt nhanh qua PIN POS" : request.ApprovalNote
212: });
```
**Empirical Evidence**:
In `AdversarialAuthorizationTests.ApprovalService_ApproveByManagerPinAsync_EmpiricalVulnerability_AcceptsBogusPinDueToHardcodedAdminQuery`:
- A cashier created a pending discount approval request (Amount = 100,000 VNĐ).
- The cashier called `ApproveByManagerPinAsync` with `ManagerPin: "000000"` (or any arbitrary invalid PIN string).
- Result: The approval was granted and executed as `manager01`!
  `approvedResult.Status == (int)ApprovalStatus.Executed (4)`.
  `approvedResult.ApprovedByUserId == managerUser.Id`.
- **Reason**:
  1. Line 181 includes `u.Username == "admin" || u.Username == "manager01"` in the LINQ search query, so `managerUser` is ALWAYS matched regardless of what PIN was supplied.
  2. Line 185 still contains hardcoded bypass PINs `000000`, `000003`, and `123456`.
  3. Line 201 falls back to the first Admin/Manager user in the database.
  4. The method never verifies the PIN against BCrypt hash (`BCrypt.Verify(pin, u.PinCodeHash)`).
  5. The endpoint `POST /api/v1/approvals/manager-pin-approve` exposes this method to Cashiers.

---

### 1.3 Additional Fallback Defaults Remaining in Codebase
1. [`src/Restaurant.Infrastructure/Services/PosOrderService.cs#L207-L208`](file:///d:/My%20Project/src/Restaurant.Infrastructure/Services/PosOrderService.cs#L207-L208):
   ```csharp
   ShiftId = activeShift?.Id ?? 1,
   CreatedByUserId = activeShift?.UserId ?? 1,
   ```
   In `MergeTablesAsync`, when the target table does not have an active order, it generates an order falling back to `CreatedByUserId = 1`.
2. [`src/Restaurant.WebAPI/Controllers/SystemController.cs#L210`](file:///d:/My%20Project/src/Restaurant.WebAPI/Controllers/SystemController.cs#L210):
   ```csharp
   UserId = adminUser?.Id ?? 1,
   ```
   In `GetAuditLogs()`, if audit logs are empty, it generates an initial entry falling back to `UserId = adminUser?.Id ?? 1`.

---

## 2. Logic Chain

1. **Requirement R1 / Acceptance Criteria 103 & 106 (`ORIGINAL_REQUEST.md`) explicitly state**:
   - Criterion 103: `Endpoint /api/v1/auth/verify-manager-pin chỉ trả về true khi mã PIN khớp với mã băm của User có Role Manager/Admin trong DB. Nhập mã PIN sai hoặc các mã 000000, 123456, 999999 đều trả về false.`
   - Criterion 106: `Không còn bất kỳ mã PIN hardcoded hoặc fallback UserId = 1 nào trong code logic nghiệp vụ.`
   - Scope item 46: `ApprovalCenterController: Duyệt yêu cầu (ApproveRequest), Từ chối (RejectRequest), Duyệt bằng PIN (ApproveByManagerPin).`
2. **Observation 1.2 proves**:
   - `ApprovalService.ApproveByManagerPinAsync` (called directly by `ApprovalCenterController.ApproveByManagerPin`) was not updated by worker_m1.
   - It retains the hardcoded username check `u.Username == "admin" || u.Username == "manager01"`.
   - It retains bypass PINs `000000`, `000003`, and `123456`.
   - It retains manager fallback lookup on line 201.
   - Any cashier can bypass approval controls on discounts, refunds, and till-outs simply by passing `"000000"` or any arbitrary string.
3. **Observation 1.3 proves**:
   - Fallback `?? 1` patterns remain in `PosOrderService.cs:208` and `SystemController.cs:210`.
4. **Therefore**: Milestone 1 does not satisfy Acceptance Criteria 103 & 106, leaving an active P0 security bypass open in production business logic.

---

## 3. Caveats
- Worker_m1 did excellent work locking down the controller attributes and fixing `IdentityService.VerifyManagerPinAsync`, which resulted in 74/74 passing tests in `AdversarialAuthorizationTests.cs` for 401/403/null-claims/lockout.
- The defect is located specifically in `src/Restaurant.Infrastructure/Services/ApprovalService.cs` where the manager PIN approval logic was duplicated and neglected during the M1 overhaul.
- Per agent role constraints (Review-only), the challenger did not fix `ApprovalService.cs` or `PosOrderService.cs`, leaving remediation to the worker.

---

## 4. Conclusion

**Verdict: REJECT**

Milestone 1 must be rejected and returned to `worker_m1` for the following remediation:
1. **Fix `ApprovalService.ApproveByManagerPinAsync`**:
   - Remove `u.Username == "admin" || u.Username == "manager01"` from line 181.
   - Remove bypass PINs `000000`, `000003`, `123456` from line 185.
   - Remove fallback manager assignment on line 201.
   - Delegate PIN verification directly to `_identityService.VerifyManagerPinAsync(request.ManagerPin)` or verify against active users with `Admin` or `Manager` role using `BCrypt.Net.BCrypt.Verify(pin, u.PinCodeHash)`.
2. **Eliminate Remaining `?? 1` Fallbacks**:
   - Remove `CreatedByUserId = activeShift?.UserId ?? 1` in `PosOrderService.cs:208` (enforce passing acting user ID from `CurrentUserService`).
   - Remove `UserId = adminUser?.Id ?? 1` in `SystemController.cs:210`.

---

## 5. Verification Method

1. **Verify Authorization Boundaries (401 / 403 / Null Claims / Lockout)**:
   ```bash
   dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName~AdversarialAuthorizationTests"
   ```
   *Expected*: 74 passed.
2. **Inspect Vulnerability in `ApprovalService.cs`**:
   View lines 176–205 of `src/Restaurant.Infrastructure/Services/ApprovalService.cs`. Observe `u.Username == "admin"`, `pin != "000000"`, and fallback manager.
3. **Inspect Fallback Defaults**:
   View `src/Restaurant.Infrastructure/Services/PosOrderService.cs` line 208 and `src/Restaurant.WebAPI/Controllers/SystemController.cs` line 210.
4. **Invalidation Condition**:
   The rejection is resolved once `ApprovalService.ApproveByManagerPinAsync` rejects invalid/bypass PINs with `UnauthorizedAccessException` and all fallback `?? 1` expressions are removed.
