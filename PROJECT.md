# Project: Pizza Restaurant Management System (POS, KDS, BOH Admin & QR Ordering)

## Architecture
The system is an enterprise restaurant management platform built on .NET 9 / ASP.NET Core 9, Blazor Server, SQL Server, Entity Framework Core 9, SignalR, and xUnit.

### Architectural Layers & Boundaries
1. **Restaurant.Domain** (`src/Restaurant.Domain`):
   - Pure domain models, entities, enums, domain events, business aggregates (`Order`, `OrderItem`, `Payment`, `Table`, `Shift`, `Material`, `MenuItem`, `InventoryTransaction`, `SystemSettings`).
   - Zero external infrastructure dependencies.
2. **Restaurant.Application** (`src/Restaurant.Application`):
   - Application contracts, interfaces (`IIdentityService`, `IInventoryService`, `IPaymentService`, `IKitchenService`, `ICurrentUserService`, `IEscPosPrinterService`), DTOs, domain event handlers, business validators.
3. **Restaurant.Infrastructure** (`src/Restaurant.Infrastructure`):
   - EF Core 9 `ApplicationDbContext` (42+ DbSets, migrations, auditable entity interceptors), SQL Server persistence.
   - SignalR Hubs (`RestaurantHub`), SePay webhook handler, BCrypt identity hashing, system clock.
4. **Restaurant.WebAPI** (`src/Restaurant.WebAPI`):
   - REST API Controllers (`AuthController`, `PosOrdersController`, `PaymentsController`, `ShiftsController`, `CashierToolsController`, `ApprovalCenterController`, `ReconciliationController`, `VouchersController`, `DashboardController`, `SystemController`, `InventoryController`).
   - JWT Bearer Authentication & Claims RBAC middleware, Swagger/OpenAPI.
5. **Restaurant.Web** (`src/Restaurant.Web`):
   - Interactive Blazor Server UI (POS, KDS, BOH Admin, Cashier, Tables), Cookie Authentication (`RestaurantAuth`) with Circuit token propagation, API Clients (`AdminApiClient`, `AuthApiClient`, `PosOrderApiClient`, typed HttpClients with DelegatingHandler).
6. **Restaurant.Domain.Tests** (`src/Restaurant.Domain.Tests`):
   - Unit tests, domain integration tests, security authorization tests, in-memory DB fixtures.

---

## Feature Inventory
Every feature from the specification kit, audit report, and requirements R1-R7 is mapped below:

| # | Feature ID / Name | Description | Milestone | Source |
|---|---|---|---|---|
| 1 | R1-01: Manager PIN Verification | Fix VerifyManagerPinAsync: remove admin bypass, BCrypt verify with Admin/Manager role, 5-failure lockout | M1 | R1, FULL_WORKSPACE_AUDIT §1.1 |
| 2 | R1-02: Endpoint Authorization Lockdown | Remove [AllowAnonymous], add [Authorize(Roles = ...)] across 8 sensitive WebAPI controllers | M1 | R1, FULL_WORKSPACE_AUDIT §1.2 |
| 3 | R1-03: Eliminate UserId Fallback | Remove all `UserId ?? 1` fallback occurrences, enforce 401 Unauthorized when unauthenticated | M1 | R1, FULL_WORKSPACE_AUDIT §1.3 |
| 4 | R1-04: Blazor Auth Cookie & JWT Chain | Fix SharedLogin PIN flow to establish RestaurantAuth cookie via AccountController, set circuit state | M1 | R1, FULL_WORKSPACE_AUDIT §1.4 |
| 5 | R1-05: HttpClient DelegatingHandler | Ensure all Blazor HttpClients automatically attach Authorization: Bearer <jwt_token> | M1 | R1, FULL_WORKSPACE_AUDIT §1.4 |
| 6 | R2-01: PosMain Direct Pay Button | Add prominent "Thanh Toán" button to PosMain action header when order has items | M2 | R2, POS-004 |
| 7 | R2-02: OrderCart Direct Pay Button | Add "Thanh Toán" button to bottom of OrderCart.razor | M2 | R2, POS-004 |
| 8 | R2-03: PosPaymentModal Integration | Render PosPaymentModal in PosMain with complete parameter bindings and callbacks | M2 | R2, POS-004 |
| 9 | R2-04: Cash Payment Flow | Transactional cash payment processing, change calculation, receipt trigger, table status release | M2 | R2, POS-004, PAY-001 |
| 10 | R2-05: VietQR & SePay Webhook | Generate VietQR dynamic code, idempotent SePay webhook transaction processing | M2 | R2, POS-005, PAY-002 |
| 11 | R2-06: Table Status Auto-Release | Set table status to Available upon successful order payment completion | M2 | R2, POS-004 |
| 12 | R2-07: K80 Receipt Non-Destructive Preview | Ensure K80 pre-bill preview (Billing) never changes order status to Paid | M2 | R2, POS-007 |
| 13 | R3-01: Remove AdminApiClient Mock Data | Eliminate all GetMockMaterials, GetMockSuppliers, GetMockUsers, etc. from AdminApiClient | M3 | R3, BOH-001..018 |
| 14 | R3-02: Explicit Error UI Alerts | Display clear toast/alert messages on BOH admin UI when API calls fail or offline | M3 | R3, BOH-spec |
| 15 | R3-03: SystemSettings Entity & DbSet | Create SystemSettings entity in Domain and DbSet in ApplicationDbContext | M3 | R3, BOH-017 |
| 16 | R3-04: EF Core Migration for SystemSettings | Generate and apply EF Core migration for persistent system settings | M3 | R3, BOH-017 |
| 17 | R3-05: SystemController DB Persistence | Update SystemController to read/write SystemSettings from database instead of RAM static variable | M3 | R3, BOH-017 |
| 18 | R4-01: RestaurantHub JWT Authentication | Secure SignalR RestaurantHub with [Authorize] and configure OnMessageReceived in Program.cs | M4 | R4, KDS-001 |
| 19 | R4-02: Claims-Based Group Assignment | Assign CashiersGroup and ChefsGroup based on verified JWT role claims, not clientType query | M4 | R4, KDS-001 |
| 20 | R4-03: Order Ready State Preservation | Ensure order transitions to Ready (not Completed/Paid) when all dishes reach Done on KDS | M4 | R4, KDS-004 |
| 21 | R4-04: 1-Time BOM Deduction Event | Maintain idempotency and single execution of OrderItemKitchenDoneDomainEvent | M4 | R4, KDS-005, BOM-001 |
| 22 | R5-01: BOM Inventory Deduction Integrity | Verify and protect automatic ingredient deductions from main recipes and modifier recipes | M5 | R5, BOM-001, BOH-005 |
| 23 | R5-02: Cancellation Waste Handling | Ensure CancelOrderService converts cooked item deductions into CancellationWaste | M5 | R5, BOM-003, POS-012 |
| 24 | R5-03: Void Item Cancellation Waste | Ensure VoidOrderItemAsync converts deductions to CancellationWaste for cooked items | M5 | R5, POS-013 |
| 25 | R5-04: InventoryService Test Suite | Comprehensive unit and integration tests for BOM deduction, Goods Receipt, and Stocktake Balance | M5 | R5, FULL_WORKSPACE_AUDIT §1.5 |
| 26 | R6-01: Split Bill UI Component | Modal allowing cashiers to select items and quantities to split from active order to new order | M6 | R6, POS-008 |
| 27 | R6-02: Split Bill Backend Transaction | Transactional API creating new order, adjusting quantities, recalculating subtotal, VAT 8%, discount | M6 | R6, POS-008 |
| 28 | R6-03: ESC/POS Network LAN Printer Abstraction | IEscPosPrinterService interface and implementation for LAN Port 9100 thermal receipt printing | M6 | R6, POS-007 |
| 29 | R7-01: Security Integration Tests (401/403) | Automated integration tests verifying 401 Unauthorized and 403 Forbidden across all endpoints | M7 | R7, ACC-spec |
| 30 | R7-02: VerifyManagerPin Test Suite | Tests verifying PIN validation, BCrypt match, bypass rejection, and brute-force lockout | M7 | R7, ACC-spec |
| 31 | R7-03: Full Regression Test Suite Pass | 100% pass of existing 80 tests + new test suite with 0 warnings and 0 errors | M7 | R7, Acceptance Criteria 4 |
| 32 | R7-04: Documentation Updates | Complete FULL_WORKSPACE_AUDIT.md and create IMPLEMENTATION_COMPLETION_REPORT.md | M7 | R7, Acceptance Criteria 4 |

---

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|---|---|---|---|
| M1 | Security & Authentication Overhaul (R1) | VerifyManagerPinAsync, [AllowAnonymous] removal, Blazor auth chain, eliminate UserId=1 | none | IN_PROGRESS |
| M2 | POS Cashiering & Payment Flow Integrity (R2) | PosMain/OrderCart payment button, PosPaymentModal, Cash & SePay VietQR, table status reset | M1 | PLANNED |
| M3 | BOH Admin Reliability & SystemSettings DB Persistence (R3) | AdminApiClient mock removal, SystemSettings entity & DbSet, EF Core migration, error toast | M1 | PLANNED |
| M4 | SignalR Hub Security & KDS Realtime Integrity (R4) | RestaurantHub JWT auth, claims-based group assignment, Ready state preservation, 1-time BOM event | M1 | PLANNED |
| M5 | Inventory BOM Deductions & Wastage Lifecycle (R5) | BOM deduction integrity, CancelOrder & VoidItem cancellation waste, InventoryService tests | M1, M4 | PLANNED |
| M6 | Split Bill POS-008 & ESC/POS LAN Printing (R6) | Split Bill modal & transactional API, IEscPosPrinterService LAN abstraction | M1, M2 | PLANNED |
| M7 | Final E2E Test Pass, Adversarial Hardening & Audit Report (R7) | Security tests (401/403), 100% pass of all tests, Tier 5 adversarial tests, completion report | M1, M2, M3, M4, M5, M6, E2E-Track | PLANNED |

In parallel:
- **E2E Testing Track**: Independent requirement-driven test infrastructure and Tiers 1-4 test scenario suite resulting in `TEST_READY.md`.

---

## Interface Contracts

### M1 (Auth & Security) ↔ All Modules & Blazor UI
- **JWT Claims**: `NameIdentifier` (int UserId), `Name` (string FullName), `Role` (string: "Admin", "Manager", "Cashier", "Chef", "Staff").
- **Blazor -> WebAPI**: HttpClients must attach `Authorization: Bearer <jwt_token>`. Unauthenticated requests return HTTP `401 Unauthorized`. Insufficient role returns HTTP `403 Forbidden`.
- **Manager PIN Endpoint**: `POST /api/v1/auth/verify-manager-pin` with `{ pinCode: string }` -> `{ success: bool, data: { isValid: bool }, error: { code: string, message: string } }`.
  - BCrypt verification against active users with Role `Admin` or `Manager`.
  - Rate limited / lockout after 5 consecutive failed attempts (15-minute lock).

### M2 (POS Cashiering) ↔ WebAPI Payments & Orders
- **Cash Payment**: `POST /api/v1/payments/cash` with `{ orderId: int, amountTendered: decimal, notes?: string }`
  - Atomic DB Transaction: validates order status, creates `Payment` record, updates `Order.Status = Paid`, updates `Table.Status = Available`, triggers Domain Events.
- **VietQR / SePay**: `POST /api/v1/payments/generate-vietqr` -> returns QR URL & payload; `POST /api/v1/payments/sepay-webhook` -> validated HMAC signature, idempotent log check, atomic update.

### M3 (System Settings) ↔ ApplicationDbContext & WebAPI
- **Entity**: `SystemSettings` in `Restaurant.Domain.Entities.System`:
  - `Id` (int PK), `RestaurantName` (string), `Address` (string), `Phone` (string), `TaxNumber` (string), `DefaultVatPercent` (decimal = 8.0), `ServiceChargePercent` (decimal = 0.0), `CurrencySymbol` (string = "đ"), `ReceiptHeader` (string), `ReceiptFooter` (string), `EnableAutoSendKitchen` (bool = true), `EnableTableTimer` (bool = false), `UpdatedAt` (DateTime), `UpdatedByUserId` (int?).
- **Endpoints**: `GET /api/v1/system/settings` (AllowAnonymous or Authenticated), `POST /api/v1/system/settings` (`[Authorize(Roles = "Admin")]`).

### M4 (SignalR & KDS) ↔ WebAPI & Blazor Clients
- **Hub Endpoint**: `/hubs/restaurant`
  - Requires JWT token in Query string `access_token` during WebSocket handshake, authenticated via `OnMessageReceived`.
  - Group joins: `ChefsGroup` if `User.IsInRole("Chef") || User.IsInRole("Admin")`; `CashiersGroup` if `User.IsInRole("Cashier") || User.IsInRole("Admin")`.
- **Kitchen Event**: `POST /api/v1/kds/items/{orderItemId}/status` with `status: Done` -> transitions item to `Done`. If all items in order are `Done`, sets `Order.Status = Ready`. Publishes `OrderItemKitchenDoneDomainEvent` exactly once.

### M5 (Inventory) ↔ Orders & Void/Cancel Workflows
- **Cancellation Waste**: When an order or item with `KitchenStatus.Done` is cancelled/voided, its `InventoryTransaction` of type `SaleDeduction` has its type updated to `CancellationWaste` with note containing cancellation reason.

### M6 (Split Bill) ↔ WebAPI & UI
- **Endpoint**: `POST /api/v1/pos/orders/{id}/split` with:
  - Request: `{ originalOrderId: int, itemsToSplit: [{ orderItemId: int, splitQuantity: int }], targetTableId?: int }`
  - Response: `{ success: bool, originalOrder: OrderDto, newOrder: OrderDto }`
  - Executed inside a single DB Transaction with recalculation of Subtotal, VAT (8%), Discounts for both orders.

### M6 (Printing) ↔ Thermal Printers
- **Interface**: `IEscPosPrinterService` in `Restaurant.Application.Common.Interfaces`:
  - `Task<bool> PrintReceiptAsync(Order order, string printerIp, int printerPort = 9100);`
  - `Task<bool> PrintKitchenTicketAsync(OrderItem item, string printerIp, int printerPort = 9100);`
  - `Task<bool> TestPrinterConnectionAsync(string printerIp, int printerPort = 9100);`

---

## Code Layout
```
src/
├── Restaurant.Domain/
│   ├── Common/              # BaseEntity, AuditableEntity, DomainEvent
│   ├── Entities/            # Order, OrderItem, Payment, Table, Shift, Material, MenuItem, System/SystemSettings
│   └── Enums/               # OrderStatus, KitchenStatus, TableStatus, PaymentMethod, InventoryTransactionType
├── Restaurant.Application/
│   ├── Common/              # Interfaces (IApplicationDbContext, IIdentityService, IInventoryService, IEscPosPrinterService)
│   ├── DTOs/                # OrderDtos, PaymentDtos, SystemDtos, InventoryDtos
│   └── Services/            # Business services & handlers
├── Restaurant.Infrastructure/
│   ├── Persistence/         # ApplicationDbContext, Configurations, Interceptors
│   ├── Migrations/          # EF Core Migrations (including AddSystemSettingsTable)
│   ├── Hubs/                # RestaurantHub
│   └── Services/            # IdentityService, PaymentService, EscPosPrinterService
├── Restaurant.WebAPI/
│   ├── Controllers/         # Auth, PosOrders, Payments, Shifts, CashierTools, System, Approvals, Dashboard, Inventory
│   └── Program.cs           # JWT configuration, OnMessageReceived for SignalR, DI
├── Restaurant.Web/
│   ├── Components/
│   │   ├── Pages/Pos/       # PosMain, PosTables, OrderCart, PosPaymentModal, SplitBillModal
│   │   ├── Pages/Kds/       # KdsDashboard
│   │   ├── Pages/Admin/     # SystemSettings, Materials, Suppliers, Shifts, Approvals
│   │   └── Pages/Auth/      # SharedLogin
│   ├── Services/            # AdminApiClient, AuthApiClient, TokenAuthenticationStateProvider, DelegatingHandler
│   └── Program.cs           # Cookie authentication, HttpClient configuration
└── Restaurant.Domain.Tests/ # Unit tests, Integration tests, Security tests, Fixtures
```
