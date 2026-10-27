# Handoff Report: Implementer 1 - POS Order Interface & Business Logic Optimization

## 1. Summary of Work Done
Comprehensive optimization of the POS Order interface and business logic in `Restaurant.Web`:
- **R1: Standardize Pizza sauce & remove spiciness group**:
  - Filtered out sauce group ("Sốt Nền", "Chọn Sốt Nền Pizza", "Select Pizza Sauce") and spiciness group ("Mức Độ Cay", "Cay", "Spice Level") from `PosOptionBuilder`.
  - Maintained sauce default option ID in `selectedModIds` for backend recipe/BOM pricing and validation while strictly suppressing sauce name from `modNames` in item cards and cart cards.
  - Excluded spice level completely from options and modifiers.
- **R2: CSR free-text note field**:
  - Added dedicated CSR Custom Note input to `PosOptionBuilder` and `PosComboBuilder`.
  - Supported opening `PosOptionBuilder` even for single items/drinks without extra modifier groups to allow CSRs to attach free-text kitchen notes.
  - Attached custom note to `OrderCartItemLocal.Note`, propagated through `SubmitPosOrderRequest` / `CartItemValidateRequest`, rendered on `OrderCart` cards with icon, and persisted into `OrderItem.Note` for KDS and bill printing.
  - Kept items/combos with different notes separate in the cart.
- **R3: Removal of "TẤT CẢ NHÓM" / "Tất cả nhóm" tab filter**:
  - Removed "TẤT CẢ NHÓM" / "Tất cả nhóm" tab buttons in both `PosOptionBuilder` and `PosComboBuilder`.
  - Auto-selects the first active modifier group by `SortOrder` on load.
- **R4: Standardization of Cart item cards**:
  - Displayed quantity prefix badge (e.g., `1x`, `2x`) at the beginning of each item name.
  - Displayed 1-portion unit price (`Price`) at the top-right corner.
  - Cleaned up modifiers list display and highlighted notes.
- **R5: POS Combo Builder interface alignment**:
  - Removed large images in `PosComboBuilder` and replaced them with compact text-only cards (`min-h-[90px]`, item name, fee/Miễn phí badge, touch +/- controls) matching `MenuItemCard` POS design.
  - Replaced thumbnail images in the summary sidebar with clean icon indicators.
  - Added CSR note input in sidebar.
- **R6: Pricing & VAT Mechanics (VAT 8% extraction)**:
  - Menu prices already include VAT.
  - Updated calculation in `OrderCart.razor.cs` and `PosPaymentModal.razor`:
    - `Total = Math.Max(0, Subtotal - Discount)`
    - `Tax = Math.Round(Total - (Total / 1.08m), 0)` (VAT 8% extracted from total, never added on top).
  - Updated VAT labels to specify `(VAT 8% bóc tách)`.
- **R7: Header & Order Exit flow**:
  - Removed `<- Sơ đồ bàn` back button and separator in `PosMain.razor` header.
  - Renamed kitchen action button to "Hoàn Thành".
  - If cart is empty: clicking "Hoàn Thành" triggers confirmation modal `ShowEmptyOrderCancelModal`. Confirming cancels active order (if created), releases table to `Available` (`POST api/v1/pos/tables/{id}/clear`), and navigates to `/pos/tables`.
  - If cart has items: validates required item configurations, submits unsent items to kitchen, and navigates to `/pos/tables` on success.

## 2. Files Modified
1. `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor.cs`: VAT extraction calculation.
2. `src/Restaurant.Web/Components/Pages/Pos/Components/OrderCart/OrderCart.razor`: Quantity badge `[Qty]x`, top-right unit price, VAT label.
3. `src/Restaurant.Web/Components/Pages/Pos/Components/PosPaymentModal.razor`: VAT extraction calculation and label.
4. `src/Restaurant.Web/Components/Pages/Pos/Components/PosOptionBuilder.razor`: Clean groups (filter sauce/spice), auto-select first group, CSR note field, updated callbacks.
5. `src/Restaurant.Web/Components/Pages/Pos/Components/PosComboBuilder.razor`: Remove all-groups tab, text-only POS cards, sidebar icon replacement, CSR note field.
6. `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor`: Remove header back button, rename "Hoàn Thành", wire empty-cart modal dialog, pass notes.
7. `src/Restaurant.Web/Components/Pages/Pos/PosMain/PosMain.razor.cs`: Sauce/spice filtering helpers, `ShowEmptyOrderCancelModal`, `ConfirmCancelEmptyOrderAsync`, `HandleDoneAsync`, note mapping in `HandleSaveModifyItem` and `HandleConfirmCombo`, item click handler.

## 3. Verification
- `dotnet build src/Restaurant.sln`: Succeeded with 0 Warnings, 0 Errors.
- `dotnet test src/Restaurant.sln --no-build`: All 289 tests passed (0 failures, 0 skipped).
