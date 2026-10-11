# E2E Test Infrastructure & Specification Matrix

> **Project:** Pizza Restaurant Management System (POS, KDS, BOH Admin & QR Ordering)  
> **Test Suite:** Comprehensive Opaque-Box E2E Testing Suite  
> **Location:** `src/Restaurant.Domain.Tests/E2E/`  
> **Execution:** `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`  
> **Methodology:** 4-Tier Opaque-Box Requirement-Driven Testing (Tiers 1 to 4)  
> **Status:** Active & Verifiable  

---

## 1. Architecture & Testing Strategy

### 1.1 Opaque-Box Philosophy
All test cases in this suite are derived strictly from business requirements and specification documents (`ORIGINAL_REQUEST.md`, `PROJECT.md`, `docs/specs/POS-spec.md`, `KDS-spec.md`, `BOH-spec.md`, `ACC-spec.md`, `QR-spec.md`), completely independent of internal implementation details. The suite treats the restaurant platform as an opaque system, verifying inputs, domain aggregates, database state mutations, and emitted events against authoritative specifications.

### 1.2 Isolation & Independence
- Each test class and method sets up its own isolated database state using dedicated in-memory database instances (`Guid.NewGuid().ToString()`).
- Zero shared mutable state between tests; tests can run concurrently or in any execution order.
- Domain entities, application services, interceptors, and EF Core transactions execute authentic production code paths without mock bypasses for core business logic.

### 1.3 4-Tier Test Architecture
```
┌────────────────────────────────────────────────────────────────────────┐
│               Tier 4: Real-World Application Scenarios                │
│  - Multi-step realistic dine-in, split bill, and cancellation lifecycles│
├────────────────────────────────────────────────────────────────────────┤
│               Tier 3: Cross-Feature Combinations                       │
│  - Pairwise interactions: Table Move + BOM + Cancel Waste + SePay      │
├────────────────────────────────────────────────────────────────────────┤
│               Tier 2: Boundary & Corner Cases                          │
│  - Negative values, zero tender, loyalty caps, status transition guards│
├────────────────────────────────────────────────────────────────────────┤
│               Tier 1: Feature Coverage (32 Features)                   │
│  - Isolated happy-path coverage of all 32 inventoried system features  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Comprehensive 32-Feature Traceability Matrix

| Feature ID | Feature Name | Tier | Primary Test Class | Authoritative Spec |
|---|---|---|---|---|
| **R1-01** | Manager PIN Verification | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R1, ACC-spec, FULL_WORKSPACE_AUDIT §1.1 |
| **R1-02** | Endpoint Authorization Lockdown | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R1, FULL_WORKSPACE_AUDIT §1.2 |
| **R1-03** | Eliminate UserId Fallback | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R1, FULL_WORKSPACE_AUDIT §1.3 |
| **R1-04** | Blazor Auth Cookie & JWT Chain | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R1, FULL_WORKSPACE_AUDIT §1.4 |
| **R1-05** | HttpClient DelegatingHandler Header | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R1, FULL_WORKSPACE_AUDIT §1.4 |
| **R2-01** | PosMain Direct Pay Button | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-004 |
| **R2-02** | OrderCart Direct Pay Button | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-004 |
| **R2-03** | PosPaymentModal Integration | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-004 |
| **R2-04** | Cash Payment Flow & Change | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-013, PAY-001 |
| **R2-05** | VietQR & SePay Webhook Idempotency | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-014, PAY-002 |
| **R2-06** | Table Status Auto-Release | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-013, POS-014 |
| **R2-07** | K80 Receipt Non-Destructive Preview | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R2, POS-012 |
| **R3-01** | Remove AdminApiClient Mock Data | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R3, BOH-001..018 |
| **R3-02** | Explicit Error UI Alerts | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R3, BOH-spec |
| **R3-03** | SystemSettings Entity & DbSet | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R3, BOH-017 |
| **R3-04** | EF Core Persistence of SystemSettings | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R3, BOH-017 |
| **R3-05** | SystemController DB Read/Write | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R3, BOH-017 |
| **R4-01** | RestaurantHub JWT Authentication | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R4, KDS-001 |
| **R4-02** | Claims-Based Group Assignment | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R4, KDS-001 |
| **R4-03** | Order Ready State Preservation | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R4, KDS-003, KDS-004 |
| **R4-04** | 1-Time BOM Deduction Event | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R4, KDS-005, BOM-001 |
| **R5-01** | BOM Inventory Deduction Integrity | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R5, BOM-001, BOH-006 |
| **R5-02** | Cancellation Waste Handling | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R5, BOM-003, POS-012 |
| **R5-03** | Void Item Cancellation Waste | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R5, POS-005, POS-013 |
| **R5-04** | InventoryService Test Suite | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R5, FULL_WORKSPACE_AUDIT §1.5 |
| **R6-01** | Split Bill UI Component Model | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R6, POS-008 |
| **R6-02** | Split Bill Backend Transaction | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R6, POS-008 |
| **R6-03** | ESC/POS Network LAN Printer Contract | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R6, POS-007 |
| **R7-01** | Security Integration Tests (401/403) | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R7, ACC-spec |
| **R7-02** | VerifyManagerPin Test Suite | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R7, ACC-spec |
| **R7-03** | Full Regression Test Suite Pass | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R7, AC-4 |
| **R7-04** | Documentation Updates Verification | Tier 1 | `Tier1_FeatureCoverageTests.cs` | R7, AC-4 |

---

## 3. Tier Specifications

### Tier 1: Feature Coverage (32 Features)
- **Goal:** Verify that every feature from the inventory executes its authoritative happy path correctly in isolation.
- **Coverage:**
  - `R1-01 .. R1-05`: PIN hashing verification, role authorization claim validation, JWT generation, header propagation.
  - `R2-01 .. R2-07`: Cart items payment enablement, cash payment transaction, VietQR generation, SePay webhook, table release, pre-bill K80 status preservation.
  - `R3-01 .. R3-05`: Direct DB persistence for materials, error contracts, SystemSettings entity & DbSet operations.
  - `R4-01 .. R4-04`: JWT token authentication for SignalR hub, role group dispatching, `Ready` state (not `Completed` or `Paid`) on kitchen done, domain event dispatch.
  - `R5-01 .. R5-04`: Recipe deduction calculation, cancellation waste conversion for cooked items, void item waste conversion, goods receipt stock increase, stocktake balance.
  - `R6-01 .. R6-03`: Split bill selection validation, transactional split conservation arithmetic, ESC/POS printer contract.
  - `R7-01 .. R7-04`: 401/403 security enforcement, PIN verification suite, regression suite pass, documentation compliance.

### Tier 2: Boundary & Corner Cases
- **Goal:** Exercise boundary values, limits, empty inputs, negative values, and illegal state transitions.
- **Test Scenarios:**
  - `TC-BND-01`: Cash tendered < order total amount -> throws `InvalidOperationException` with explicit mismatch message.
  - `TC-BND-02`: Cash tendered == order total amount -> exact change (0 VND), order status marked `Paid`.
  - `TC-BND-03`: Cash tendered with large denomination -> change calculated accurately without arithmetic overflow.
  - `TC-BND-04`: Submitting payment for already paid order -> rejected with error.
  - `TC-BND-05`: Idempotent duplicate SePay webhook payload -> returns true immediately without double crediting.
  - `TC-BND-06`: Move table to an occupied table -> rejected with `TARGET_TABLE_OCCUPIED`.
  - `TC-BND-07`: Merge table with empty source table list -> returns false safely.
  - `TC-BND-08`: Void item with empty reason -> fallback reason applied and logged to `VoidLogs`.
  - `TC-BND-09`: Void item on cancelled or paid order -> returns false.
  - `TC-BND-10`: Revert kitchen item from `Done` to `Pending`/`Processing` -> rejected (returns null).
  - `TC-BND-11`: Redeem 0 loyalty points or more points than customer available -> throws `InvalidOperationException`.
  - `TC-BND-12`: Voucher with subtotal below `MinOrderAmount` -> validation fails.
  - `TC-BND-13`: Voucher percentage discount capped by `MaxDiscountAmount` -> discount does not exceed cap.
  - `TC-BND-14`: Open shift when user already has an active open shift -> rejected with `SHIFT_ALREADY_OPEN`.
  - `TC-BND-15`: Close shift with physical cash count variance -> records variance accurately in Z-Report without crashing.
  - `TC-BND-16`: Stocktake reconciliation with zero difference -> zero inventory adjustment, status marked completed.
  - `TC-BND-17`: Submit order with empty cart or non-existent items -> throws `InvalidOperationException`.

### Tier 3: Cross-Feature Combinations
- **Goal:** Verify pairwise and multi-feature interaction safety across boundaries.
- **Test Scenarios:**
  - `TC-XF-01`: Table Move + Menu Ordering + Voucher Discount + Cash Payment + Auto-Release Table.
  - `TC-XF-02`: Kitchen Done + BOM Deduction + Order Cancellation -> Conversion to `CancellationWaste` in audit log.
  - `TC-XF-03`: QR Order Draft + Cashier Approval + Kitchen Processing + SLA Delay Alert + Kitchen Done (`Ready` status).
  - `TC-XF-04`: Combo Order + Void Combo Header -> Cascading void to child items + subtotal recalculation.
  - `TC-XF-05`: Dual Payment Collision: Cash paid first, SePay bank transfer arrives later -> Logged in `SepayWebhookLogs` with `ErrorLog` and cashier alert.
  - `TC-XF-06`: Shift Lifecycle: Open Shift + Till-In + Cash Sales + Till-Out + Blind Stocktake + Close Shift Reconciliation.
  - `TC-XF-07`: Loyalty Points Redemption + Voucher + Cash Payment + Loyalty Points Accrual & Tier Promotion.
  - `TC-XF-08`: Table Merge + Multi-Item Transfer + Kitchen Tickets Synchronization.
  - `TC-XF-09`: Goods Receipt -> Stock Increase -> Kitchen Done BOM Deduction -> Stocktake Audit Adjustment.

### Tier 4: Real-World Scenarios
- **Goal:** Simulate authentic end-to-end multi-step restaurant operations.
- **Workflows:**
  - **Scenario A (Lunch Peak Dine-In Workflow):**
    1. Cashier opens shift with 1,000,000 VND float.
    2. Table 01 is occupied; guests order Combo Family, Pizza, and drinks.
    3. Order submitted; kitchen ticket generated on KDS.
    4. Chef processes and completes all items; order transitions to `Ready`.
    5. BOM ingredients deducted from inventory.
    6. Cashier prints K80 pre-bill; table moves to `Billing` (order remains unpaid).
    7. Customer provides phone number, redeems 100 loyalty points (5,000 VND).
    8. Cashier collects cash with change; order marked `Paid`, table released to `Available`.
    9. Customer earns new loyalty points.
  - **Scenario B (Family Gathering Bill Splitting & Multi-Method Settlement):**
    1. Large table seated and food prepared to `Done`.
    2. Guests request bill splitting.
    3. Order is split into two independent orders; item quantities and totals preserved.
    4. Order 1 is settled via VietQR / SePay bank transfer webhook.
    5. Order 2 is settled via Cash.
    6. Both orders marked `Paid`, table released to `Available`.
    7. Cashier closes shift with end-of-day Z-Report reconciliation.
  - **Scenario C (QR Self-Ordering, Kitchen Delay & Emergency Cancellation Waste):**
    1. Guest scans table QR token; submits Draft order.
    2. Cashier reviews and approves order; table turns Occupied.
    3. Kitchen ticket starts cooking; SLA delay alert triggers (> 15 minutes).
    4. Food finished cooking; BOM raw materials deducted.
    5. Guest experiences emergency; Manager approves order cancellation.
    6. BOM `SaleDeduction` transactions convert to `CancellationWaste`.
    7. Inventory loss summary reflects the waste; table released to `Available`.

---

## 4. Test Verification & Execution Command

```powershell
# Run the entire test suite
dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj

# Run only E2E test suite
dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName~Restaurant.Domain.Tests.E2E"
```

## 5. Mandatory Integrity Guarantee
- No trivial passes (`Assert.True(true)`).
- No circumvented business constraints or fake mocks.
- Every assertion evaluates state changes in database entities, financial totals, or status enumerations.
