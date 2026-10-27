# Reviewer Handoff Report: POS Order Interface & Business Logic Optimization

## 1. Executive Summary & Skepticism Disclaimer
> [!WARNING] **Skepticism Disclaimer**
> Confidence is high on business logic, backend tax extraction, and automated unit test coverage (291 passing tests), but full end-to-end interactive Blazor Server circuit browser rendering remains reliant on component unit/integration verification rather than headless browser testing (Playwright).

## 2. Issues Found in Prior Attempt and Root Causes
1. **Receipt / Pre-Bill VAT Omission (R6)**
   - **Input:** Requesting K80 receipt via `api/v1/bills/{id}/k80` for an order with total revenue (e.g., 108,000đ).
   - **Expected:** TaxAmount extracted as 8% VAT (`Math.Round(Total - Total / 1.08m) = 8,000đ`) and rendered on the receipt as `Thuế (VAT 8% bóc tách): 8.000 đ`.
   - **Actual:** `BillViewerService.MapToBillDetailDto` hardcoded `TaxAmount = 0m`. As a result, the receipt displayed 0đ and omitted the VAT line (`@if (OrderData.TaxAmount > 0)` never executed).
   - **Root Cause:** Incomplete coverage in `BillViewerService.cs` when mapping `Order` to `BillDetailDto`.

2. **Pizza Standard Sauce Mismatch (R1)**
   - **Input:** Adding specialty pizzas (e.g., `Pizza Heo Nướng BBQ Xé Sợi`, `Pizza Hải Sản Pesto Hạt Thông`, `Pizza Tôm Bơ Tỏi`, `Pizza Hải Sản Tiêu Đen`).
   - **Expected:** The pizza is assigned its specific recipe sauce (e.g., BBQ sauce, Pesto sauce, Garlic Butter sauce, Black Pepper sauce) in `selectedModIds` for kitchen/BOM tracking without charging extra or displaying in cart card modifiers.
   - **Actual:** Prior attempt blindly defaulted to the first default option in the modifier group (`Sốt Cà Chua San Marzano`) for all pizzas, regardless of whether the pizza was BBQ or Pesto.
   - **Root Cause:** Lack of standard recipe matching logic in `PosMain.razor.cs`.

3. **Empty Order Abandonment in Table Management (R7)**
   - **Input:** CSR opens a table (which may have an active empty draft order) and clicks "Hoàn Thành" with an empty cart -> confirms modal to cancel order and free table.
   - **Expected:** Table status is restored to `Available` and the empty draft order is cancelled.
   - **Actual:** `ClearTableAsync` only set `Table.Status = Available` but did not cancel the unpaid empty order in the database. When `GetTablesStatusAsync()` ran, the table still found the uncancelled order. Furthermore, if non-manager CSR triggered empty order cancellation, direct call to `orders/{id}/cancel` returned 403 without releasing the table cleanly.
   - **Root Cause:** `ClearTableAsync` in `PosOrderService.cs` did not cancel associated empty orders.

4. **Reconstructed Order Sauce Leakage in Cart (R1)**
   - **Input:** Loading an existing active table or takeaway order from the backend where previous modifiers included sauce names.
   - **Expected:** Cart modifiers list should strictly exclude sauce names.
   - **Actual:** `ReconstructCartItemsFromOrder` did not populate `ModifierNames` or could leak legacy sauce strings.
   - **Root Cause:** Missing modifier reconstruction and filtering in `PosMain.razor.cs`.

## 3. Changes Made
- `src/Restaurant.Infrastructure/Services/BillViewerService.cs`:
  - Computed 8% extracted VAT in `MapToBillDetailDto`: `TaxAmount = Math.Round(o.TotalAmount - (o.TotalAmount / 1.08m), 0)`.
- `src/Restaurant.Web/Components/Pages/Pos/Components/K80ReceiptModal.razor` & `ReceiptTestPage.razor`:
  - Updated VAT label to `Thuế (VAT 8% bóc tách):` matching `OrderCart.razor` and `PosPaymentModal.razor`.
- `src/Restaurant.Infrastructure/Services/PosOrderService.cs`:
  - Enhanced `ClearTableAsync` to automatically cancel associated empty unpaid orders (`[HỦY ĐƠN RỖNG]`) and broadcast SignalR `Available` status.
- `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs`:
  - Implemented `GetStandardSauceForPizza` helper matching standard pizza sauces (BBQ, Pesto, Garlic Butter, Black Pepper, San Marzano) without extra surcharge.
  - Used standard sauce helper in `AddPizzaToCartWithConfig`, `AddMenuItemToCartWithDefaults`, and `HandleSaveModifyItem`.
  - Filtered out sauce names when reconstructing cart items from existing orders.
  - Safeguarded `ConfirmCancelEmptyOrderAsync` to handle authorization restrictions cleanly and ensure table release.
- `src/Restaurant.Domain.Tests/PosReceiptPrintingTests.cs`:
  - Added `Test9_BillViewerService_ExtractsVat8Percent_FromTotalAmount` verifying VAT 8% extraction on K80 receipt.
  - Added `Test10_ClearTableAsync_CancelsEmptyOrder_AndReleasesTable` verifying table release and empty order cancellation.

## 4. Verification Record
- **Automated Tests:**
  - `dotnet test src/Restaurant.sln`: 291 passed, 0 failed, 0 skipped.
- **Compilation:**
  - `dotnet build src/Restaurant.sln`: Succeeded with 0 Warnings and 0 Errors.
- **Unverified aspects:**
  - Full end-to-end browser automation of SignalR circuit disconnects and browser-level UI interaction.
