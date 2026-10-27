# BÁO CÁO AUDIT TOÀN DIỆN: ĐỊNH MỨC NGUYÊN LIỆU (BOM), QUY ĐỔI ĐƠN VỊ VÀ ĐỊNH LƯỢNG MÓN ĂN
**Dự án:** Hệ thống Quản trị Nhà hàng Pizza POS / KDS / BOH  
**Tác nhân thực hiện:** Agent 1 (BOM, Recipe & Unit Conversion Auditor)  
**Thời gian hoàn thành:** 2026-10-14T17:35:00Z  
**Phạm vi:** Database Schema, Domain Entities, EF Core, Services, Controllers, Blazor UI, Unit & Integration Tests.  
**Chế độ:** READ-ONLY INVESTIGATION (Tuân thủ nghiêm ngặt: không sửa code, không tạo migration, không sửa database).

---

## MỤC LỤC
1. [TỔNG QUAN KẾT QUẢ AUDIT & BẢNG ĐIỂM HOÀN THIỆN](#1-tổng-quan-kết-quả-audit--bảng-điểm-hoàn-thiện)
2. [MA TRẬN PHÂN LOẠI CHI TIẾT TỪNG CHỨC NĂNG (FINDINGS MATRIX)](#2-ma-trận-phân-loại-chi-tiết-từng-chức-năng-findings-matrix)
3. [ĐÁNH GIÁ CHI TIẾT THEO TỪNG PHÂN VÙNG NGHIỆP VỤ](#3-đánh-giá-chi-tiết-theo-từng-phân-vùng-nghiệp-vụ)
   - 3.1. [Cấu trúc Entity & Database Schema của BOM / Recipe](#31-cấu-trúc-entity--database-schema-của-bom--recipe)
   - 3.2. [Đơn vị tính (UOM) và Hệ số quy đổi (Unit Conversion)](#32-đơn-vị-tính-uom-và-hệ-số-quy-đổi-unit-conversion)
   - 3.3. [Rủi ro Độ chính xác Thập phân (Decimal Precision) & Sai số làm tròn](#33-rủi-ro-độ-chính-xác-thập-phân-decimal-precision--sai-số-làm-tròn)
   - 3.4. [Định mức Món đơn (Single Item BOM) & Hao hụt định mức (Yield Rate)](#34-định-mức-món-đơn-single-item-bom--hao-hụt-định-mức-yield-rate)
   - 3.5. [Định mức Combo & Set Menu (Combo BOM & Child Item Deductions)](#35-định-mức-combo--set-menu-combo-bom--child-item-deductions)
   - 3.6. [Định mức Topping, Modifier & Nhóm tùy chọn (Modifier BOM)](#36-định-mức-topping-modifier--nhóm-tùy-chọn-modifier-bom)
   - 3.7. [Bán thành phẩm & Công thức nhiều cấp (Semi-Finished Goods / Multi-Level BOM)](#37-bán-thành-phẩm--công-thức-nhiều-cấp-semi-finished-goods--multi-level-bom)
   - 3.8. [Xử lý Hủy món (Void Item) & Hủy đơn đối với Tồn kho BOM](#38-xử-lý-hủy-món-void-item--hủy-đơn-đối-với-tồn-kho-bom)
   - 3.9. [Tính toàn vẹn Báo cáo Hao hụt (Inventory Loss Calculation Anomaly)](#39-tính-toàn-vẹn-báo-cáo-hao-hụt-inventory-loss-calculation-anomaly)
   - 3.10. [Đánh giá Độ bao phủ Test Suite hiện hữu (Testing Gaps)](#310-đánh-giá-độ-bao-phủ-test-suite-hiện-hữu-testing-gaps)
4. [5-COMPONENT HANDOFF REPORT THEO QUY CHUẨN TEAMWORK](#4-5-component-handoff-report-theo-quy-chuẩn-teamwork)
   - 4.1. [Observation (Bằng chứng thực chứng)](#41-observation-bằng-chứng-thực-chứng)
   - 4.2. [Logic Chain (Chuỗi suy luận nhân quả)](#42-logic-chain-chuỗi-suy-luận-nhân-quả)
   - 4.3. [Caveats (Giới hạn & Điểm chưa thẩm định)](#43-caveats-giới-hạn--điểm-chưa-thẩm-định)
   - 4.4. [Conclusion (Kết luận tổng thể)](#44-conclusion-kết-luận-tổng-thể)
   - 4.5. [Verification Method (Phương pháp tái kiểm chứng)](#45-verification-method-phương-pháp-tái-kiểm-chứng)
5. [DANH SÁCH QUYẾT ĐỊNH NGHIỆP VỤ BẮT BUỘC (BUSINESS DECISIONS REQUIRED)](#5-danh-sách-quyết-định-nghiệp-vụ-bắt-buộc-business-decisions-required)

---

## 1. TỔNG QUAN KẾT QUẢ AUDIT & BẢNG ĐIỂM HOÀN THIỆN

Qua rà soát chuyên sâu toàn bộ codebase ASP.NET Core 9, Blazor Server, EF Core 9, SQL Server và xUnit tests tại `d:\My Project`, Agent 1 ghi nhận bức tranh thực tế về hệ thống BOM và Quy đổi đơn vị tính:

| Tiêu chí | Trạng thái kỹ thuật | Đánh giá kiến trúc |
|---|---|---|
| **BOM Món đơn (Single Item BOM)** | **PARTIAL** | Đã có Entity, CRUD API, trừ kho khi Kitchen Done, 32 công thức mẫu. Thiếu tỷ lệ hao hụt (yield), UI ép chọn đơn vị gốc của vật tư. |
| **BOM Combo (Set Menu)** | **DONE_VERIFIED** (với con) / **BROKEN** (với cha) | Trừ kho theo món con được chọn là chuẩn xác. Nhưng nếu cấu hình BOM cho món Combo cha (ví dụ bao bì combo), hệ thống bỏ qua không trừ. |
| **BOM Topping & Modifier** | **BACKEND_ONLY** | Đã có Entity (`ModifierRecipe`, `ModifierRecipeItem`), Service, Controller và logic trừ kho trong code. Tuy nhiên **100% không có UI**, không có dữ liệu mẫu và không có test nào. Không có cơ chế co giãn định lượng theo Size bánh (Small/Medium/Large). |
| **Quy đổi Đơn vị (Unit Conversion)** | **BACKEND_ONLY / DORMANT / BROKEN** | Bảng `UnitConversions` tồn tại nhưng **hoàn toàn không được sử dụng** trong bất kỳ phép tính trừ kho hay nhập kho nào. Độ chính xác `decimal(18,2)` làm tròn 0.001kg thành 0.00kg. UI không có màn hình quản lý quy đổi. |
| **Quản lý Đơn vị Tính (UOM CRUD)** | **BACKEND_ONLY / BROKEN** | Entity `UnitOfMeasure` có 8 đơn vị seed sẵn. Không có API Controller CRUD. UI `MaterialManagement.razor` hardcode danh sách đơn vị trong HTML và bị lệch ID nghiêm trọng (ID 6 trong HTML là Lon, nhưng DB là Gói). |
| **Bán thành phẩm (Multi-level BOM)** | **NOT_STARTED** | Không có thực thể, không có schema, không có logic nấu sơ chế/làm vỏ bánh. Đế bánh pizza và sốt cà chua bị coi như hàng mua ngoài sẵn có. |
| **Tính nhất quán Hủy món (Void BOM)** | **BROKEN** | Hủy món sau khi bếp đã hoàn thành (Kitchen Done) không hoàn kho và không ghi nhận hao hụt vào sổ cái. Nguyên liệu bị thất thoát không dấu vết. |
| **Báo cáo Hao hụt Kho (Loss Report)** | **BROKEN** | Hàm `GetInventoryLossSummaryAsync` hardcode công thức `TotalWasteAmount = totalLoss * 0.35m` (35% giả lập) thay vì tổng hợp từ `InventoryTransactions`. |

---

## 2. MA TRẬN PHÂN LOẠI CHI TIẾT TỪNG CHỨC NĂNG (FINDINGS MATRIX)

| Mã ID | Tên chức năng / Nghiệp vụ | Phân loại | Ưu tiên | File nguồn & Dòng tham chiếu | Tóm tắt phát hiện |
|---|---|---|---|---|---|
| **F01-BOM-DEF** | Cấu hình định lượng món đơn (`SetMenuItemRecipe`) | `PARTIAL` | **P1** | `InventoryService.cs:258-295`<br>`RecipeManagement.razor:315-325` | Lưu được công thức món đơn, nhưng UI ép cứng `UnitOfMeasureId` theo vật tư, không cho chọn đơn vị khác. Thiếu hệ số hao hụt tự nhiên (Yield/Scrap). |
| **F02-BOM-DED** | Khấu trừ kho tự động khi món nấu xong (`KitchenStatus.Done`) | `DONE_VERIFIED` | **P1** | `KitchenService.cs:220-228`<br>`DeductInventoryOnOrderItemKitchenDoneHandler.cs:18`<br>`InventoryService.cs:740-835` | Trừ kho chính xác dựa trên RecipeItems * OrderItem.Quantity; có chống trừ trùng qua `InventoryTransactions`. Đã có integration tests kiểm chứng. |
| **F03-COMBO-DED**| Khấu trừ kho món trong Combo | `DONE_VERIFIED` | **P2** | `KitchenService.cs:223-228`<br>`ComboEndToEndQaTests.cs:193-199` | Món con trong Combo khi Done được trừ kho chính xác theo BOM của món con đó. Header combo bị bỏ qua. |
| **F04-COMBO-HDR**| Khấu trừ kho BOM bao bì/vỏ hộp của Combo cha | `BROKEN` | **P2** | `KitchenService.cs:223-226`<br>`RecipeManagement.razor:37-47` | UI cho phép gán BOM vào Combo cha, nhưng `KitchenService` chặn hoàn toàn việc phát event trừ kho cho Combo Header. Bao bì combo không bao giờ được trừ. |
| **F05-MOD-RECIPE**| Định mức Topping, Modifier & Size (`ModifierRecipe`) | `BACKEND_ONLY` | **P0** | `InventoryService.cs:297-334, 784-798`<br>`ModifierManagement.razor:1-472` | Backend có sẵn Entity, Service, Controller và logic trừ kho, nhưng UI hoàn toàn không có tính năng thiết lập định lượng cho Modifier. Không có seed data và không có test. |
| **F06-MOD-SCALE** | Co giãn định lượng món theo Size bánh (S/M/L) | `NOT_STARTED` | **P1** | `SampleDataSeeder.cs:271-276, 842-849`<br>`InventoryService.cs:756-782` | Bánh Pizza Margherita luôn trừ 1 đế vừa 25cm dù khách chọn Size Nhỏ (20cm) hay Size Lớn (30cm). Không có cơ chế thay thế vật tư (Replacement) hoặc nhân hệ số Size (Scaling Factor). |
| **F07-UOM-CONV** | Hệ số quy đổi đơn vị (`UnitConversion`) | `BACKEND_ONLY` / `BROKEN` | **P0** | `InventoryService.cs:775-781, 938-982`<br>`20260723205718_InitialCreate.cs:345` | Bảng `UnitConversions` là bảng "chết", không được dùng trong bất kỳ hàm trừ kho hay nhập kho nào. Cột `ConversionFactor` bị giới hạn `decimal(18,2)` làm tròn 0.001 thành 0.00. |
| **F08-UOM-UI**   | Quản lý đơn vị tính và ánh xạ trên UI | `BROKEN` | **P1** | `MaterialManagement.razor:190-198`<br>`SampleDataSeeder.cs:107-116` | UI hardcode danh sách UOM từ 1 đến 6 trong thẻ `<select>`. Lệch ID: chọn "Lon" (value 6) sẽ lưu thành "Gói/Bịch" trong DB. |
| **F09-PRECISION**| Độ chính xác số thập phân định lượng nhỏ | `BROKEN` | **P0** | `20260723205718_InitialCreate.cs:562, 598`<br>`EntityConfigurations.cs:219-239` | EF Core không cấu hình `HasPrecision`, SQL Server mặc định `decimal(18,2)`. Định lượng gia vị nhỏ (2.5g muối = 0.0025kg, lá basil) bị làm tròn về 0.00 hoặc sai lệch 100%. |
| **F10-SEMI-GOOD**| Bán thành phẩm / Công thức nhiều cấp (Multi-level BOM)| `NOT_STARTED` | **P1** | `CustomerAndInventoryEntities.cs:50-67, 87-96` | Không có thực thể Bán thành phẩm, không có liên kết giữa Material và Recipe, không có phiếu sơ chế/nấu cốt. Đế bánh, cốt sốt bị coi là mua đứt ngoài chợ. |
| **F11-VOID-LEAK**| Rò rỉ tồn kho khi hủy món đã chế biến (`VoidOrderItem`) | `BROKEN` | **P1** | `PosOrderService.cs:896-970`<br>`CancelOrderService.cs:85-96` | Hủy món (`VoidOrderItemAsync`) chỉ đổi trạng thái món thành Voided và ghi log, không điều chỉnh giao dịch kho đã trừ. Tồn kho bị hao hụt âm thầm. |
| **F12-WASTE-RPT**| Giả lập số liệu hao hụt kho trong Báo cáo | `BROKEN` | **P1** | `InventoryService.cs:734`<br>`InventoryLossReport.razor:28-31` | `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0)` nhân cứng 35% từ chênh lệch kiểm kê thay vì truy vấn các giao dịch `CancellationWaste` thực tế. |
| **F13-TEST-GAP** | Khoảng trống kiểm thử BOM & Unit Conversion | `NOT_STARTED` | **P1** | `Restaurant.Domain.Tests/` | 0 test kiểm thử quy đổi đơn vị; 0 test kiểm thử `ModifierRecipe`; 0 test kiểm thử sai số làm tròn số thập phân. |

---

## 3. ĐÁNH GIÁ CHI TIẾT THEO TỪNG PHÂN VÙNG NGHIỆP VỤ

### 3.1. Cấu trúc Entity & Database Schema của BOM / Recipe
**Tệp tin khảo sát:**
- `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs` (Dòng 41–115)
- `src/Restaurant.Domain/Entities/AuthAndMenuEntities.cs` (Dòng 75–98)
- `src/Restaurant.Infrastructure/Persistence/ApplicationDbContext.cs` (Dòng 46–52)
- `src/Restaurant.Infrastructure/Persistence/Configurations/EntityConfigurations.cs` (Dòng 219–239)

**Bằng chứng code thực tế:**
```csharp
// CustomerAndInventoryEntities.cs:80-96
public class Recipe : AuditableEntity
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

public class RecipeItem : AuditableEntity
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal Quantity { get; set; }
}
```

**Nhận định kỹ thuật:**
1. **Quan hệ 1 - 1 giữa MenuItem và Recipe:** Mỗi món ăn chỉ có tối đa 1 Recipe cha (`Recipe.MenuItemId`), trong đó chứa nhiều dòng nguyên liệu thô `RecipeItems`.
2. **Khuyết thiếu cấu hình Precision trên EF Core:** Cả `RecipeItem.Quantity` lẫn `Material.CurrentStock`, `Material.CostPerUnit` đều không được khai báo `.HasPrecision()` trong `EntityConfigurations.cs`. Migration ban đầu `20260723205718_InitialCreate.cs` sinh ra kiểu dữ liệu `decimal(18,2)`.
3. **Thiếu trường dữ liệu quản lý công thức:**
   - Không có trường `BatchSize` / `YieldQuantity` (Định mức trên mẻ hay trên 1 suất). Mặc định hệ thống ngầm hiểu là cho 1 suất món ăn.
   - Không có trường `ScrapPercentage` / `WastageRate` (Hệ số hao hụt chế biến, ví dụ thịt rút xương hao 15%, rau củ gọt vỏ hao 10%).

---

### 3.2. Đơn vị tính (UOM) và Hệ số quy đổi (Unit Conversion)
**Tệp tin khảo sát:**
- `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs` (Dòng 41–48, 69–78)
- `src/Restaurant.Infrastructure/Services/InventoryService.cs` (Dòng 770–782, 938–982)
- `src/Restaurant.WebAPI/Controllers/InventoryController.cs` (Dòng 434–460)
- `src/Restaurant.Web/Components/Pages/Admin/Inventory/MaterialManagement.razor` (Dòng 190–198)

**Bằng chứng code thực tế - Bảng UnitConversions bị "bỏ rơi" trong tính toán:**
Trong `InventoryService.cs` hàm `DeductInventoryForOrderItemAsync`:
```csharp
// InventoryService.cs:774-781
if (recipe != null)
{
    foreach (var ri in recipe.RecipeItems)
    {
        var needed = ri.Quantity * orderItem.Quantity;
        if (materialDeductions.ContainsKey(ri.MaterialId))
            materialDeductions[ri.MaterialId] += needed;
        else
            materialDeductions[ri.MaterialId] = needed;
    }
}
```
Và trong `InventoryService.cs` hàm `CreatePurchaseOrderAsync`:
```csharp
// InventoryService.cs:148-166
var receiptItem = new GoodsReceiptItem
{
    GoodsReceiptId = goodsReceipt.Id,
    MaterialId = item.MaterialId,
    UnitOfMeasureId = material.UnitOfMeasureId, // Luôn gán cứng theo đơn vị gốc của vật tư!
    Quantity = item.Quantity,
    UnitCost = item.UnitCost,
    TotalCost = item.Quantity * item.UnitCost
};
...
material.CurrentStock += item.Quantity; // Cộng trực tiếp, không qua quy đổi!
```

**Nhận định kỹ thuật:**
1. **Hoàn toàn không có logic quy đổi đơn vị runtime:** Mặc dù `RecipeItem` có trường `UnitOfMeasureId`, nhưng khi trừ kho, hệ thống lấy thẳng `ri.Quantity * orderItem.Quantity` trừ vào `material.CurrentStock`. Nếu một vật tư có đơn vị lưu kho là `Kg`, mà trong Recipe khai báo 100 `g`, hệ thống sẽ trừ thẳng **100 Kg**!
2. **Không hỗ trợ Đơn vị mua hàng (Purchase Unit):** Phiếu nhập kho (`CreatePurchaseOrderRequest`) chỉ truyền `MaterialId`, `Quantity`, `UnitCost`. Hệ thống không cho phép nhập theo đơn vị bao bì của Nhà cung cấp (như 1 Bao = 50kg, 1 Thùng = 24 lon). Người dùng buộc phải tự quy đổi bằng tay trước khi nhập số liệu.
3. **Lệch pha dữ liệu UI (Hardcoded Enum Mismatch):**
Trong `MaterialManagement.razor`:
```razor
<!-- MaterialManagement.razor:190-198 -->
<select @bind="_model.UnitOfMeasureId">
    <option value="1">Kg (Kilogram)</option>
    <option value="2">g (Gram)</option>
    <option value="3">L (Lít)</option>
    <option value="4">ml (Mililít)</option>
    <option value="5">Pcs (Cái/Chiếc)</option>
    <option value="6">Lon (Hộp)</option>
</select>
```
Trong khi cơ sở dữ liệu thực tế do `SampleDataSeeder.cs` (dòng 108–115) khởi tạo là:
`1: Kg`, `2: g`, `3: L`, `4: ml`, `5: Pcs`, `6: Gói`, `7: Lon`, `8: Chai`.  
=> **Hậu quả:** Khi người dùng tạo một nguyên liệu nước ngọt và chọn "Lon (Hộp)", hệ thống gửi `UnitOfMeasureId = 6`, lưu vào DB là **Gói / Bịch**!

---

### 3.3. Rủi ro Độ chính xác Thập phân (Decimal Precision) & Sai số làm tròn
**Tệp tin khảo sát:**
- `src/Restaurant.Infrastructure/Migrations/20260723205718_InitialCreate.cs` (Dòng 316–318, 345, 562, 598)
- `src/Restaurant.Web/Components/Pages/Admin/Menu/RecipeManagement.razor` (Dòng 147)

**Bằng chứng code thực tế:**
Trong migration `20260723205718_InitialCreate.cs`:
```csharp
// Dòng 345:
ConversionFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
// Dòng 562:
Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
```
Trong UI `RecipeManagement.razor`:
```razor
<!-- Dòng 147: -->
<input type="number" step="0.01" @bind="row.Quantity" placeholder="Số lượng" ... />
```

**Phân tích rủi ro kinh doanh & toán học:**
1. **Quy đổi từ Gram sang Kilogram:**  
   $1 \text{ gram} = 0.001 \text{ kg}$.  
   Với kiểu `decimal(18,2)`, giá trị $0.001$ trong SQL Server hoặc EF Core sẽ bị làm tròn thành **$0.00$**!  
   Nếu sử dụng hệ số này, $500 \text{ gram} \times 0.00 = 0 \text{ kg}$ (không trừ kho), hoặc phát sinh lỗi chia cho 0 (`DivideByZeroException`) nếu quy đổi ngược.
2. **Định mức gia vị / hương liệu nhỏ:**  
   Trong chế biến pizza và bếp nóng, các loại men nở, muối, hạt tiêu, lá thơm (oregano, basil) được định mức ở mức $2.5\text{g} = 0.0025\text{kg}$ hoặc $0.5\text{g} = 0.0005\text{kg}$.  
   Nếu quản lý kho theo `Kg` và nhập định mức bằng `Kg`:
   - Nhập `0.0025` bị cắt gọt về `0.00` (thất thoát không kiểm soát được) hoặc làm tròn lên `0.01` ($10\text{g}$ - gấp 4 lần thực tế!).
   - Trên UI, trường `step="0.01"` chặn người dùng không thể nhập số có 3 chữ số thập phân trở lên.
3. **Quy đổi két / thùng không chia hết cho 10:**  
   Một thùng bia/nước ngọt 24 lon: 1 lon = $1/24 \approx 0.041666...$ thùng. Lưu `decimal(18,2)` thành `0.04`, gây sai lệch $4.16\%$ mỗi lon, tích lũy hàng nghìn lon sẽ gây âm kho ảo hoặc lệch sổ cái nghiêm trọng.

---

### 3.4. Định mức Món đơn (Single Item BOM) & Hao hụt định mức (Yield Rate)
**Tệp tin khảo sát:**
- `src/Restaurant.Web/Components/Pages/Admin/Menu/RecipeManagement.razor`
- `src/Restaurant.Infrastructure/Persistence/SampleDataSeeder.cs` (Dòng 837–1132)

**Hiện trạng triển khai:**
1. **Dữ liệu hạt giống (Seed Data):** Rất phong phú, đã cấu hình chuẩn 32 món đơn phổ biến (10 Pizza, 2 Melts, 5 Pasta, 4 Chicken, 2 Salad, 2 Cơm đút lò, 3 Đồ uống, 2 Nước ngọt lon).
2. **Cơ chế thiết lập công thức trên UI (`RecipeManagement.razor`):**
   - Cho phép chọn món ăn từ danh sách bên trái.
   - Cho phép thêm từng dòng nguyên liệu và số lượng.
   - Khi lưu (`SaveRecipe` dòng 315–325), gọi API `SetMenuItemRecipeAsync` để xóa toàn bộ dòng cũ và ghi đè dòng mới.
3. **Điểm bất cập:**
   - Dòng 322: `UnitOfMeasureId = _availableMaterials.FirstOrDefault(m => m.Id == r.MaterialId)?.UnitOfMeasureId ?? 1`. Giao diện không cho đầu bếp chọn đơn vị tính mong muốn khi định lượng mà tự động gán theo đơn vị lưu kho của vật tư.
   - Hoàn toàn chưa có khái niệm tỷ lệ hao hụt (Yield Rate / Scrap Rate) khi sơ chế và nấu nướng.

---

### 3.5. Định mức Combo & Set Menu (Combo BOM & Child Item Deductions)
**Tệp tin khảo sát:**
- `src/Restaurant.Infrastructure/Services/KitchenService.cs` (Dòng 220–228)
- `src/Restaurant.Infrastructure/Services/PosOrderService.cs` (Dòng 580–675)
- `src/Restaurant.Domain.Tests/ComboEndToEndQaTests.cs` (Dòng 148–199)

**Bằng chứng code thực tế:**
```csharp
// KitchenService.cs:220-228
if (newStatus == (int)KitchenStatus.Done && oldStatus != (int)KitchenStatus.Done)
{
    // Do not deduct for Combo header itself; deduct for child items and standalone items
    bool isComboHeader = item.MenuItem != null && item.MenuItem.IsCombo && !item.ParentItemId.HasValue;
    if (!isComboHeader)
    {
        await _mediator.Publish(new OrderItemKitchenDoneDomainEvent(item.Id, item.OrderId));
    }
}
```

**Nhận định kỹ thuật:**
1. **Cơ chế trừ kho món con hoạt động chuẩn xác (`DONE_VERIFIED`):** Khi đơn hàng combo được gửi xuống bếp, POS sinh ra 1 OrderItem cha (Header) và N OrderItem con (`ParentItemId = header.Id`). Khi bếp nấu xong từng món con và bấm "Done", domain event `OrderItemKitchenDoneDomainEvent` được kích hoạt riêng cho từng món con, từ đó trừ kho nguyên liệu cấu thành món con đó theo đúng công thức của món con. Test `ComboEndToEndQaTests.TC01` đã chứng minh điều này.
2. **Xung đột nghiệp vụ đối với Combo Header (`BROKEN`):**
   - Trong thực tế nhà hàng, Combo thường tiêu tốn các vật tư đóng gói riêng (như Vỏ hộp combo lớn, Túi giữ nhiệt combo, Đồ chơi tặng kèm, Ly combo).
   - Nếu Admin vào màn hình `RecipeManagement.razor` và cấu hình BOM cho món Combo (ví dụ gán "Vỏ hộp Combo" = 1 Cái), hệ thống vẫn lưu vào DB. Nhưng khi vận hành, dòng code 223–224 tại `KitchenService.cs` sẽ **bỏ qua hoàn toàn Combo Header**. Do đó, các vật tư bao bì gắn với Combo cha sẽ **không bao giờ được khấu trừ kho**!

---

### 3.6. Định mức Topping, Modifier & Nhóm tùy chọn (Modifier BOM)
**Tệp tin khảo sát:**
- `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs` (Dòng 98–114)
- `src/Restaurant.Infrastructure/Services/InventoryService.cs` (Dòng 297–334, 762–798)
- `src/Restaurant.WebAPI/Controllers/InventoryController.cs` (Dòng 161–175, 205–225)
- `src/Restaurant.Web/Services/AdminApiClient.cs` (Dòng 309, 324)
- `src/Restaurant.Web/Components/Pages/Admin/Menu/ModifierManagement.razor` (Toàn bộ file)

**Bằng chứng thực tế:**
Backend đã triển khai đầy đủ hạ tầng cho `ModifierRecipe`:
- Entity `ModifierRecipe` và `ModifierRecipeItem` được định nghĩa chuẩn trong EF Core.
- Service `SetModifierOptionRecipeAsync` và `GetModifierOptionRecipeAsync` hoạt động tốt.
- `DeductInventoryForOrderItemAsync` (dòng 784–798) duyệt qua `orderItem.OrderItemModifiers`, tìm `ModifierRecipe` tương ứng và cộng dồn số lượng cần trừ kho:
```csharp
foreach (var mod in orderItem.OrderItemModifiers)
{
    var modRecipe = modifierRecipes.FirstOrDefault(r => r.ModifierOptionId == mod.ModifierOptionId);
    if (modRecipe != null)
    {
        foreach (var mri in modRecipe.ModifierRecipeItems)
        {
            var needed = mri.Quantity * orderItem.Quantity;
            if (materialDeductions.ContainsKey(mri.MaterialId))
                materialDeductions[mri.MaterialId] += needed;
            else
                materialDeductions[mri.MaterialId] = needed;
        }
    }
}
```
**Tuy nhiên phát hiện 3 lỗ hổng nghiêm trọng:**
1. **Thiếu hoàn toàn giao diện người dùng (100% UI Missing - `BACKEND_ONLY`):**  
   Kiểm tra `ModifierManagement.razor` chỉ có chức năng sửa tên nhóm, thêm tùy chọn và phụ thu tiền (`ExtraPrice`). Không có bất kỳ nút, tab hay modal nào để chọn nguyên liệu và định mức tiêu hao cho Topping/Option. Mặc dù `AdminApiClient.cs` có sẵn 2 hàm `GetModifierOptionRecipeAsync` và `SetModifierRecipeAsync`, nhưng không có dòng code nào trong Blazor gọi tới chúng.
2. **Không có dữ liệu mẫu và không có kiểm thử:**  
   Trong `SampleDataSeeder.cs`, có 10 nhóm tùy chọn với hàng chục Option (Thêm Phô Mai Mozzarella, Thêm Pepperoni, Trân châu đen...), nhưng **0 Option nào có ModifierRecipe**. Trong `Restaurant.Domain.Tests`, có **0 test case nào kiểm tra luồng trừ kho của Topping**.
3. **Khiếm khuyết cấu trúc định lượng theo Kích thước (Size Scaling Void):**  
   Nhà hàng pizza bán bánh Size S (20cm), Size M (25cm), Size L (30cm). Trong công thức gốc, pizza mặc định dùng "Đế Pizza Vừa 25cm". Khi khách chọn tùy chọn "Size L (30cm)", hệ thống POS thêm một modifier "Size L (+100,000đ)". Khi khấu trừ kho, hệ thống **vẫn trừ 1 Đế 25cm của công thức gốc**, và không thể tự động đổi sang Đế 30cm hay tăng lượng phô mai/sốt tương ứng. Hệ thống hiện tại chỉ hỗ trợ cộng thêm (additive), không hỗ trợ thay thế (replace) hay nhân hệ số tỷ lệ (scale factor).

---

### 3.7. Bán thành phẩm & Công thức nhiều cấp (Semi-Finished Goods / Multi-Level BOM)
**Tệp tin khảo sát:**
- `src/Restaurant.Domain/Entities/CustomerAndInventoryEntities.cs`
- `src/Restaurant.Infrastructure/Persistence/SampleDataSeeder.cs`

**Nhận định kỹ thuật (`NOT_STARTED`):**
1. **Mô hình dữ liệu phẳng 1 cấp (Single-level Flat Model):**  
   Trong `CustomerAndInventoryEntities.cs`:
   - `RecipeItem.MaterialId` bắt buộc trỏ tới bảng `Materials`.
   - Bảng `Materials` không có trường liên kết nào tới `Recipe` hay `MenuItem`.
   - Không có trường phân loại vật tư (`MaterialType`: RawMaterial, SemiFinished, FinishedGood).
2. **Thực tế tại nhà hàng Pizza:**  
   Nhà hàng làm vỏ bánh từ bột mì Tipo 00, men nở, dầu olive, muối; nấu sốt cà chua từ cà chua tươi, tỏi, dầu olive, lá basil.  
   Nhưng trong codebase hiện tại, "Đế Pizza Vừa 25cm" và "Sốt cà chua San Marzano" được định nghĩa như những nguyên liệu thô độc lập mua đứt từ nhà cung cấp.
3. Không có cơ chế Lệnh sản xuất sơ chế (Preparation Order / Batch Production) để ghi nhận: "Xuất kho 25kg bột mì + 0.5kg men nở để nhập kho 100 chiếc đế pizza 25cm".

---

### 3.8. Xử lý Hủy món (Void Item) & Hủy đơn đối với Tồn kho BOM
**Tệp tin khảo sát:**
- `src/Restaurant.Infrastructure/Services/PosOrderService.cs` (Dòng 896–970)
- `src/Restaurant.Infrastructure/Services/CancelOrderService.cs` (Dòng 85–97)

**Bằng chứng code thực tế tại `PosOrderService.cs` (Hủy từng món):**
```csharp
// PosOrderService.cs:924-936
targetItem.Status = (int)OrderItemStatus.Voided;
_context.VoidLogs.Add(new VoidLog
{
    OrderId = order.Id,
    OrderItemId = targetItem.Id,
    MenuItemId = targetItem.MenuItemId,
    MenuItemName = targetItem.MenuItemName,
    Quantity = targetItem.Quantity,
    UnitPrice = targetItem.UnitPrice,
    Reason = voidReason,
    VoidByUserId = userId,
    CreatedAt = utcNow
});
// Tính lại tiền đơn hàng, Commit Transaction, KHÔNG HỀ ĐỘNG TỚI INVENTORYTRANSACTION!
```

**Nhận định kỹ thuật (`BROKEN` - Rò rỉ kho):**
1. **Trường hợp hủy món lẻ (`VoidOrderItemAsync`):**  
   Nếu món ăn đã được bếp hoàn thành (`KitchenStatus.Done`), nguyên liệu BOM đã bị trừ trong kho và một bản ghi `InventoryTransactionType.SaleDeduction` đã được tạo ra.  
   Khi thu ngân hoặc quản lý thực hiện Hủy món (Void), `PosOrderService` chỉ cập nhật trạng thái món thành `Voided`, ghi nhật ký `VoidLog`, trừ tiền hóa đơn. **Hệ thống hoàn toàn không xử lý tồn kho**: không tạo giao dịch bù trừ hoàn kho (`CancellationReturn`), cũng không chuyển đổi sang hao hụt (`CancellationWaste`). Tồn kho vật tư bị trừ vĩnh viễn dù món bị hủy và nhà hàng không thu được tiền.
2. **Trường hợp hủy toàn bộ đơn (`CancelOrderAsync`):**  
   `CancelOrderService` có tìm các giao dịch `SaleDeduction` và đổi loại thành `CancellationWaste`. Tuy nhiên, cách làm này **sửa trực tiếp (mutate) trên dòng lịch sử cũ** (`deduction.TransactionType = CancellationWaste`) thay vì tạo một dòng giao dịch đảo ngược (reversing transaction) mới, vi phạm nguyên tắc bất biến (immutability) của sổ cái kế toán kho.

---

### 3.9. Tính toàn vẹn Báo cáo Hao hụt (Inventory Loss Calculation Anomaly)
**Tệp tin khảo sát:**
- `src/Restaurant.Infrastructure/Services/InventoryService.cs` (Dòng 703–738)
- `src/Restaurant.Web/Components/Pages/Admin/Reports/InventoryLossReport.razor` (Dòng 28–31)

**Bằng chứng code thực tế:**
```csharp
// InventoryService.cs:729-736
var totalLoss = Math.Abs(items.Sum(si => si.DifferenceValue));

return new InventoryLossSummaryDto
{
    TotalVarianceAmount = totalLoss,
    TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0), // HARDCODED 35% GIẢ LẬP!
    DiscrepancyCount = items.Count,
    DiscrepancyItems = discrepancyList
};
```

**Nhận định kỹ thuật (`BROKEN`):**
Trên giao diện `InventoryLossReport.razor`, có 2 thẻ số liệu:
1. "Chênh lệch âm ghi nhận qua các đợt kiểm kê thực tế" (`TotalVarianceAmount`).
2. "Hao hụt tự nhiên & hủy chế biến" (`TotalWasteAmount`).
Trong code backend, `TotalWasteAmount` hoàn toàn không được tính từ các giao dịch hủy món/hủy đơn mang mã `CancellationWaste` trong bảng `InventoryTransactions`, mà lấy số tiền chênh lệch kiểm kê nhân bừa với $0.35$ ($35\%$). Đây là lỗi làm giả số liệu báo cáo tài chính/kho (Mock calculation artifact).

---

### 3.10. Đánh giá Độ bao phủ Test Suite hiện hữu (Testing Gaps)
**Tệp tin khảo sát:**
- Toàn bộ 21 tệp trong `src/Restaurant.Domain.Tests/`

**Kết quả rà soát chi tiết:**
- Tổng số lượng test hiện có trong solution chạy qua 100%.
- **Tuy nhiên, đối với phạm vi BOM & Quy đổi đơn vị:**
  - `UnitConversion`: **0 test** (Không có bất kỳ kiểm thử nào cho việc tạo quy đổi hay áp dụng quy đổi).
  - `ModifierRecipe`: **0 test** (Không có test nào kiểm tra việc lưu định mức modifier hoặc trừ kho modifier).
  - `SetMenuItemRecipe`: **0 test** (Không có test kiểm tra API lưu công thức món ăn).
  - Khấu trừ kho BOM trong `Tier1_FeatureCoverageTests.cs` (F22) và `ComboEndToEndQaTests.cs` (TC01) chỉ kiểm tra trường hợp lý tưởng nhất: `RecipeItem` có đơn vị cùng loại với `Material` (Kg với Kg, Lon với Lon) và số thập phân chẵn ($0.2\text{kg}$, $0.1\text{kg}$). Hoàn toàn không có boundary/stress test cho sai số thập phân hoặc lệch đơn vị.

---

## 4. 5-COMPONENT HANDOFF REPORT THEO QUY CHUẨN TEAMWORK

### 4.1. Observation (Bằng chứng thực chứng)
1. **Lệch đơn vị trong trừ kho:** `ri.Quantity * orderItem.Quantity` được trừ trực tiếp vào `material.CurrentStock` tại `InventoryService.cs:776` mà không hề tham chiếu bảng `UnitConversions`.
2. **Độ chính xác thập phân:** Cột `ConversionFactor` trong `20260723205718_InitialCreate.cs:345` và `Quantity` trong dòng 562 được sinh ra là `decimal(18,2)`, không thể lưu trữ các tỷ lệ quy đổi như $0.001$ ($1\text{g} = 0.001\text{kg}$) hay $1/24$.
3. **Lệch ID đơn vị trên giao diện:** `MaterialManagement.razor:197` hardcode `<option value="6">Lon (Hộp)</option>`, trong khi `SampleDataSeeder.cs:113` gán ID 6 cho "Gói / Bịch", ID 7 mới là "Lon".
4. **Bỏ rơi Combo Header khi khấu trừ BOM:** `KitchenService.cs:223` loại bỏ `isComboHeader`, khiến mọi BOM bao bì gán cho combo cha không bao giờ được khấu trừ.
5. **Giao diện Modifier BOM hoàn toàn biến mất:** `ModifierManagement.razor` không có trường nhập nguyên liệu, mặc dù backend `InventoryService.cs:297` và `AdminApiClient.cs:324` đã sẵn sàng API.
6. **Không xử lý tồn kho khi Void món:** `PosOrderService.cs:896-970` (`VoidOrderItemAsync`) không hoàn kho cũng không ghi nhận hao hụt vào `InventoryTransactions` đối với món đã nấu xong.
7. **Báo cáo hao hụt giả lập:** `InventoryService.cs:734` tính `TotalWasteAmount = Math.Round(totalLoss * 0.35m, 0)`.

### 4.2. Logic Chain (Chuỗi suy luận nhân quả)
1. Do `ApplicationDbContext` và `EntityConfigurations` không cấu hình `.HasPrecision()` cho các cột số lượng và hệ số, EF Core mặc định dùng `decimal(18,2)` của SQL Server.
2. Do lưu `decimal(18,2)`, các đơn vị đo lường nhỏ ($g$, $ml$) khi quy đổi sang đơn vị chuẩn kho ($Kg$, $L$) bị làm tròn về $0.00$ hoặc sai lệch $\ge 50\%$.
3. Do thiếu tầng chuyển đổi đơn vị runtime trong `DeductInventoryForOrderItemAsync`, hệ thống buộc các nhà phát triển UI phải ép cứng nguyên liệu trong công thức theo đơn vị lưu kho của vật tư.
4. Do UI `RecipeManagement.razor` ép dùng đơn vị gốc và `step="0.01"`, người dùng không thể nhập công thức định lượng chính xác cho các món cần lượng nhỏ gia vị (muối, tiêu, lá thơm).
5. Do `ModifierManagement.razor` chưa hoàn thiện phần giao diện BOM, toàn bộ các tùy chọn thêm (Topping, phô mai thêm, trân châu) được bán ra mà không hề trừ kho nguyên vật liệu, gây ra tình trạng thực tế kho bị hụt nhưng hệ thống vẫn báo đủ.
6. Do `VoidOrderItemAsync` không có logic xử lý giao dịch kho bù trừ, mọi món ăn bị khách trả lại hoặc nhân viên nhập sai sau khi bếp đã hoàn thành đều dẫn đến việc số liệu tồn kho trên phần mềm cao hơn thực tế (thất thoát ngầm).

### 4.3. Caveats (Giới hạn & Điểm chưa thẩm định)
- Đợt audit này được thực hiện ở chế độ READ-ONLY nghiêm ngặt, không chỉnh sửa bất kỳ dòng mã nguồn nào, không chạy migration và không can thiệp database.
- Không kiểm thử hiệu năng truy vấn (Performance/Load testing) của LINQ Join giữa Recipe, ModifierRecipe và Material khi hàng trăm đơn hàng được xác nhận đồng thời trên SignalR.
- Chưa khảo sát các thiết bị IoT/Cân điện tử cắm ngoài (nếu nhà hàng có kế hoạch tích hợp cân điện tử tại quầy pha chế/bếp).

### 4.4. Conclusion (Kết luận tổng thể)
Hệ thống BOM và Quản lý Kho hiện tại đạt mức độ hoàn thiện ở mức **Cơ bản cho món đơn giản** (Base single-item BOM deduction is functional with 0 error build and passing regression tests). Tuy nhiên, hệ thống **CHƯA SẴN SÀNG CHO VẬN HÀNH THỰC TẾ (NOT PRODUCTION-READY)** đối với mô hình nhà hàng Pizza & Đồ uống phức tạp do:
1. Thiếu cơ chế quy đổi đơn vị thực tế (Unit Conversion Dormancy).
2. Lỗ hổng độ chính xác thập phân (`decimal(18,2)` truncation).
3. Thiếu hoàn toàn giao diện thiết lập BOM cho Topping/Modifier.
4. Lỗ hổng thất thoát kho khi hủy món (Void Inventory Leak).
5. Số liệu báo cáo hao hụt kho bị giả lập 35%.

### 4.5. Verification Method (Phương pháp tái kiểm chứng)
1. **Kiểm tra biên dịch & Test Suite:**
   Chạy lệnh: `dotnet test src/Restaurant.Domain.Tests/Restaurant.Domain.Tests.csproj` (Xác minh 100% test pass).
2. **Kiểm chứng rủi ro làm tròn thập phân:**
   Mở file `src/Restaurant.Infrastructure/Migrations/20260723205718_InitialCreate.cs` tại dòng 345, 562 để xác minh kiểu cột `decimal(18,2)`.
3. **Kiểm chứng bảng UnitConversions bị bỏ rơi:**
   Tìm kiếm toàn bộ solution với từ khóa `_context.UnitConversions`. Chỉ thấy xuất hiện tại `ApplicationDbContext.cs`, `InventoryController.cs` và `InventoryService.cs` (hàm CRUD), tuyệt đối không xuất hiện trong hàm trừ kho hay nhập kho.
4. **Kiểm chứng lệch ID đơn vị:**
   So sánh `MaterialManagement.razor` (dòng 190–198) với `SampleDataSeeder.cs` (dòng 108–134).
5. **Kiểm chứng số liệu giả lập 35%:**
   Mở `src/Restaurant.Infrastructure/Services/InventoryService.cs` dòng 734.

---

## 5. DANH SÁCH QUYẾT ĐỊNH NGHIỆP VỤ BẮT BUỘC (BUSINESS DECISIONS REQUIRED)

Trước khi tiến hành sửa code và chạy migration ở các giai đoạn sau, Lead Architect và Product Owner cần thống nhất 5 quyết định nghiệp vụ sau:

1. **Quyết định về Đơn vị cơ sở và Độ chính xác số thập phân (UOM Precision):**
   - *Lựa chọn A:* Giữ đơn vị lưu kho là đơn vị lớn (Kg, L), nâng cấp toàn bộ cột số lượng và hệ số trong DB lên `decimal(18, 4)` hoặc `decimal(18, 5)` để lưu được $0.001\text{kg}$ ($1\text{g}$).
   - *Lựa chọn B:* Chuyển đổi toàn bộ đơn vị cơ sở trong kho về đơn vị nhỏ nhất (Gram, Mililít, Chiếc). Khi đó mọi phép tính đều là số nguyên hoặc số thập phân đơn giản, triệt tiêu sai số làm tròn.
2. **Quyết định về Định mức Bánh Pizza theo Size (Size Scaling vs Separate Items):**
   - *Lựa chọn A:* Tách mỗi size pizza thành một MenuItem độc lập (Pizza Margherita S, Pizza Margherita M, Pizza Margherita L), mỗi món có 1 Recipe chuẩn riêng.
   - *Lựa chọn B:* Giữ cấu trúc 1 MenuItem kèm Modifier Size, nhưng nâng cấp bảng `ModifierRecipes` hỗ trợ cờ `IsReplacement = true` (thay thế nguyên liệu đế bánh) và `Multiplier = 1.4` (nhân định lượng topping).
3. **Quyết định về Bao bì Combo:**
   - Khi bán Combo, có khấu trừ vỏ hộp/túi combo của Combo Header không? Nếu có, cần bỏ điều kiện chặn Combo Header trong `KitchenService.cs`.
4. **Quyết định về Xử lý Tồn kho khi Hủy món (Voided Item Policy):**
   - Khi hủy món đã nấu xong trên KDS, cho phép nhân viên chọn lý do:
     - Hoàn kho (`CancellationReturn` - nếu món chưa chạm vào hoặc đóng gói sẵn như lon nước).
     - Ghi nhận hao hụt (`CancellationWaste` - nếu món đã chế biến hỏng/bỏ đi).
5. **Quyết định về Bán thành phẩm (Multi-level BOM):**
   - Nhà hàng quản lý bột bánh và sốt tự nấu theo quy trình sản xuất nội bộ (cần bảng Lệnh sơ chế / Batch Production), hay coi như nguyên liệu thô mua ngoài?
