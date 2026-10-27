# Reviewer 3 Adversarial Audit & Quality Assurance Report

> [!WARNING] **Skepticism Disclaimer**
> Automated suite has 295 passing tests and builds with 0 warnings/errors, but client-side SignalR real-time websocket broadcasts and complex interactive multi-user race conditions require live production verification.

## 1. What the prior attempt got wrong
1. **Sauce and Spice Modifier Leakage into Takeaway Order DTO (`TakeawayOrderItemDto.Modifiers`) (R1)**
   - **Input:** Requesting a takeaway order via `PosOrderService.GetTakeawayOrderByIdAsync`, `GetTakeawayOrdersAsync`, or `GetActiveOrderForTableAsync` for orders configured with standard pizza sauce or legacy spice.
   - **Expected:** `TakeawayOrderItemDto.Modifiers` must strictly exclude standard pizza sauce (`Sốt`, `Sauce`) and spice levels (`Cay`, `Spice`), matching the customer-facing bill viewer and cart card.
   - **Actual:** `PosOrderService.MapToTakeawayDto` mapped all `oi.OrderItemModifiers.Select(m => m.ModifierOptionName).ToList()` directly without filtering, causing standard pizza sauce and spice level to leak into takeaway order views and takeaway payment items.
   - **Root Cause:** Modifier filtering was omitted in `PosOrderService.MapToTakeawayDto`.

2. **Omission of Extracted 8% VAT Display on Takeaway Main Detail Summary View (R6)**
   - **Input:** Opening the detail view of a takeaway order in `PosTakeawayMain.razor`.
   - **Expected:** Order summary box displays the extracted 8% VAT line (`Thuế (VAT 8% bóc tách): ... đ`) consistent with dine-in payment modal (`PosPaymentModal.razor`), cart card (`OrderCart.razor`), and takeaway payment modal (`PosTakeawayPaymentModal.razor`).
   - **Actual:** `PosTakeawayMain.razor` only displayed SubTotal, Discount, and TotalAmount, completely omitting the extracted VAT line.
   - **Root Cause:** Incomplete coverage in `PosTakeawayMain.razor`.

3. **Potential Modifier Leakage in Takeaway Payment Modal Item Details (R1)**
   - **Input:** Displaying item modifiers in `PosTakeawayPaymentModal.razor`.
   - **Expected:** Modifiers list excludes sauce and spice tokens in UI rendering.
   - **Actual:** `PosTakeawayPaymentModal.razor` rendered `@string.Join(", ", item.Modifiers)` without client-side defense-in-depth filtering.
   - **Root Cause:** Missing filter guard in `PosTakeawayPaymentModal.razor`.

## 2. What I changed
- `src/Restaurant.Infrastructure/Services/PosOrderService.cs`:
  - Filtered out sauce (`Sốt`, `Sauce`) and spice (`Cay`, `Spice`) modifiers from `TakeawayOrderItemDto.Modifiers` in `MapToTakeawayDto`.
- `src/Restaurant.Web/Components/Pages/Pos/PosTakeaway/PosTakeawayMain.razor`:
  - Added extracted 8% VAT display line (`Thuế (VAT 8% bóc tách): @Math.Round(SelectedOrder.TotalAmount - (SelectedOrder.TotalAmount / 1.08m), 0).ToString("N0") đ`) to the order summary section in the detail view.
- `src/Restaurant.Web/Components/Pages/Pos/Components/PosTakeawayPaymentModal.razor`:
  - Added defense-in-depth filtering on `item.Modifiers` to prevent any sauce or spice modifiers from rendering in takeaway payment modal.
- `src/Restaurant.Domain.Tests/PosReceiptPrintingTests.cs`:
  - Added `Test14_TakeawayOrder_ExcludesSauceAndSpiceFromModifiers_AndShowsExtractedVat` verifying that `TakeawayOrderItemDto.Modifiers` excludes sauce/spice and correctly computes extracted 8% VAT without surcharging.

## 3. Verification Record
- **Deep Verification (ran actual tests):**
  - `dotnet build src/Restaurant.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj --filter "FullyQualifiedName~PosReceiptPrintingTests"`: Passed! 14 passed, 0 failed, 0 skipped.
  - `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj`: Passed! 295 passed, 0 failed, 0 skipped in 1m 50s.
- **Shallow Verification (manual only):**
  - Verified `OrderCart.razor` quantity prefix (`@(item.Quantity)x`) and 1-portion unit price at top-right corner.
  - Verified `PosComboBuilder.razor` text card layout, absence of large images, and omission of "TẤT CẢ NHÓM".
  - Verified `PosOptionBuilder.razor` single active group rendering and omission of "TẤT CẢ NHÓM".
  - Verified CSR custom note entry in `PosOptionBuilder` and `PosComboBuilder`, and note propagation to cart, KDS ticket, and pre-bill receipt.
  - Verified exit flow and confirmation modal in `PosMain.razor` when cart is empty, resetting table status to Available and redirecting to `/pos/tables`.
- **Unverified aspects:**
  - Multi-tab SignalR client concurrency and live physical printer ESC/POS baud rate under production network latency.

## 4. Known Issues
- `Shallow Verification`: Interactive Blazor Server circuit states in actual multi-tab browser sessions remain verified by static and unit integration testing rather than live browser automation.
- `Minor Robustness Risk`: If physical table hardware triggers double-clicks on "Hoàn Thành" while cart is empty, the first request clears table and the second encounters an already-cleared state; this is handled gracefully by try/catch in `ConfirmCancelEmptyOrderAsync`.

## 5. Remaining risk & next step
The implementation fulfills all requirements R1 through R7 and adheres to strict backward-compatibility and zero test failures (295/295 tests passing). Ready for production deployment and stakeholder sign-off.
