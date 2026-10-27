using Microsoft.EntityFrameworkCore;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;

namespace Restaurant.Infrastructure.Persistence;

public static class SampleDataSeeder
{
    public static async Task SeedSampleRestaurantDataAsync(ApplicationDbContext context)
    {
        // ============================================================
        // 1. KITCHEN STATIONS (7 Stations)
        // ============================================================
        var stations = new List<(string Code, string Name, int SortOrder)>
        {
            ("PIZZA", "Bếp Nướng Pizza", 1),
            ("GRILL", "Bếp Nướng BBQ", 2),
            ("FRYER", "Bếp Chiên Gà & Snack", 3),
            ("PASTA", "Bếp Mì Ý & Cơm", 4),
            ("SALAD", "Bếp Nguội & Salad", 5),
            ("BAR", "Quầy Pha Chế & Đồ Uống", 6),
            ("DESSERT", "Quầy Tráng Miệng & Bánh", 7)
        };

        var stationMap = new Dictionary<string, KitchenStation>();
        foreach (var s in stations)
        {
            var existing = await context.KitchenStations.FirstOrDefaultAsync(ks => ks.Code == s.Code);
            if (existing == null)
            {
                existing = new KitchenStation
                {
                    Code = s.Code,
                    Name = s.Name,
                    SortOrder = s.SortOrder,
                    IsActive = true
                };
                await context.KitchenStations.AddAsync(existing);
                await context.SaveChangesAsync();
            }
            stationMap[s.Code] = existing;
        }

        // ============================================================
        // 2. CATEGORIES (13 Categories)
        // ============================================================
        var categoriesData = new List<(string Name, string NameEn, string Desc, string DescEn, int SortOrder)>
        {
            ("Pizza Truyền Thống", "Traditional Pizza", "Pizza Ý nướng giòn công thức cổ điển", "Classic crisp Italian pizzas", 1),
            ("Pizza Kẹp Giòn Melts", "Crispy Melts", "Vỏ bánh giòn rụm kẹp đẫm nhân phô mai", "Crispy folded crust loaded with cheese", 2),
            ("Mì Ý & Lasagna", "Pasta & Lasagna", "Mì Ý tươi xào sốt truyền thống & Lasagna đút lò", "Fresh pasta & oven-baked lasagna", 3),
            ("Gà Giòn & Gà Nướng", "Crispy & Grilled Chicken", "Đùi gà, cánh gà giòn & gà nướng sốt đậm vị", "Crispy wings, drumsticks & sauced chicken", 4),
            ("Khai Vị", "Appetizers", "Món ăn nhẹ khởi đầu bữa tiệc", "Delicious bites to begin your meal", 5),
            ("Món Ăn Kèm & Snack", "Sides & Snacks", "Khoai tây, bánh mì bơ tỏi & phô mai que", "French fries, garlic bread & cheese snacks", 6),
            ("Salad Tươi Mát", "Fresh Salad", "Rau củ hữu cơ Đà Lạt kết hợp sốt đặc trưng", "Fresh organic greens with signature dressings", 7),
            ("Cơm Đút Lò", "Baked Rice", "Cơm nướng phô mai đút lò phong cách Âu", "Cheesy European-style baked rice", 8),
            ("Thức Uống & Trà", "Drinks & Fresh Tea", "Nước ngọt có ga, trà trái cây & trà sữa", "Sodas, fruit teas & fresh bubble teas", 9),
            ("Bánh & Tráng Miệng", "Desserts & Ice Cream", "Bánh ngọt châu Âu & kem mát lạnh", "European cakes & chilled ice creams", 10),
            ("Thực Đơn Trẻ Em", "Kids Menu", "Khẩu phần phù hợp & dinh dưỡng cho bé", "Fun, sized & balanced meals for children", 11),
            ("Combo Tiết Kiệm", "Value Combos", "Set đồ ăn tiết kiệm cho gia đình & bạn bè", "Best value sets for pairs, families & parties", 12)
        };

        var catMap = new Dictionary<string, Category>();
        foreach (var c in categoriesData)
        {
            var existing = await context.Categories.FirstOrDefaultAsync(cat => cat.Name == c.Name || cat.NameEn == c.NameEn);
            if (existing == null)
            {
                existing = new Category
                {
                    Name = c.Name,
                    NameEn = c.NameEn,
                    Description = c.Desc,
                    DescriptionEn = c.DescEn,
                    SortOrder = c.SortOrder,
                    IsActive = true
                };
                await context.Categories.AddAsync(existing);
                await context.SaveChangesAsync();
            }
            catMap[c.NameEn] = existing;
        }

        // Deactivate and merge removed categories ("Pizza Thượng Hạng", "Pizza Ý Thượng hạng")
        var oldPizzaCats = await context.Categories
            .Where(c => c.Name.Contains("Pizza Ý Thượng hạng") || c.Name.Contains("Pizza Thượng Hạng"))
            .ToListAsync();
        var mainTradPizza = catMap.ContainsKey("Traditional Pizza") ? catMap["Traditional Pizza"] : null;
        if (mainTradPizza != null)
        {
            foreach (var opc in oldPizzaCats)
            {
                var affectedItems = await context.MenuItems.Where(m => m.CategoryId == opc.Id).ToListAsync();
                foreach (var ai in affectedItems)
                {
                    ai.CategoryId = mainTradPizza.Id;
                }
                opc.IsActive = false;
            }
            await context.SaveChangesAsync();
        }

        // ============================================================
        // 3. UNITS OF MEASURE (8 UOMs)
        // ============================================================
        var uoms = new List<(string Name, string Abbr)>
        {
            ("Kilogram", "Kg"),
            ("Gram", "g"),
            ("Lít", "L"),
            ("Mililít", "ml"),
            ("Cái / Miếng", "Pcs"),
            ("Gói / Bịch", "Gói"),
            ("Lon", "Lon"),
            ("Chai", "Chai")
        };

        var uomMap = new Dictionary<string, UnitOfMeasure>();
        foreach (var u in uoms)
        {
            var existing = await context.UnitOfMeasures.FirstOrDefaultAsync(um => um.Abbreviation == u.Abbr || um.Name == u.Name);
            if (existing == null)
            {
                existing = new UnitOfMeasure
                {
                    Name = u.Name,
                    Abbreviation = u.Abbr,
                    CreatedAt = DateTime.UtcNow
                };
                await context.UnitOfMeasures.AddAsync(existing);
                await context.SaveChangesAsync();
            }
            uomMap[u.Abbr] = existing;
        }

        // ============================================================
        // 4. MATERIALS & INGREDIENTS (>= 60 Materials + 5 LOW STOCK)
        // ============================================================
        var materialsData = new List<(string Name, string NameEn, string Uom, decimal Stock, decimal MinStock, decimal Cost)>
        {
            // Dough & Flour
            ("Bột mì Pizza Tipo 00", "Tipo 00 Pizza Flour", "Kg", 150m, 30m, 28000m),
            ("Men nở Pizza Instant", "Instant Pizza Yeast", "g", 2500m, 500m, 150m),
            ("Dầu Olive nguyên chất", "Extra Virgin Olive Oil", "ml", 15000m, 3000m, 220m),
            ("Muối tinh khiết", "Refined Salt", "g", 50000m, 10000m, 15m),
            ("Đường tinh luyện", "Refined Sugar", "g", 40000m, 10000m, 25m),
            ("Đế Pizza Nhỏ 20cm", "Small Pizza Dough 20cm", "Pcs", 60m, 20m, 12000m),
            ("Đế Pizza Vừa 25cm", "Medium Pizza Dough 25cm", "Pcs", 100m, 30m, 18000m),
            ("Đế Pizza Lớn 30cm", "Large Pizza Dough 30cm", "Pcs", 80m, 25m, 24000m),

            // Cheese
            ("Phô mai Mozzarella sợi", "Shredded Mozzarella Cheese", "Kg", 35m, 15m, 190000m),
            ("Phô mai Mozzarella tươi khối", "Fresh Block Mozzarella", "Kg", 1.5m, 10m, 220000m), // LOW STOCK 1
            ("Phô mai Cheddar lát", "Sliced Cheddar Cheese", "Kg", 12m, 5m, 230000m),
            ("Phô mai bột Parmesan", "Grated Parmesan Cheese", "g", 8000m, 2000m, 350m),
            ("Phô mai Cream Cheese", "Cream Cheese", "Kg", 8m, 3m, 210000m),
            ("Hỗn hợp 4 loại Phô mai", "Four Cheese Blend", "Kg", 10m, 4m, 240000m),

            // Meat
            ("Xúc xích Ý Pepperoni", "Italian Pepperoni", "Kg", 20m, 8m, 210000m),
            ("Thịt xông khói Bacon", "Smoked Bacon", "Kg", 18m, 6m, 185000m),
            ("Giăm bông Ham", "Cooked Ham", "Kg", 15m, 5m, 160000m),
            ("Xúc xích Ý cay Sausage", "Italian Spicy Sausage", "Kg", 14m, 5m, 175000m),
            ("Thịt bò băm đút lò", "Minced Beef", "Kg", 25m, 10m, 230000m),
            ("Thịt bò nướng BBQ", "BBQ Beef Slices", "Kg", 12m, 5m, 260000m),
            ("Ức gà phi lê tươi", "Fresh Chicken Breast", "Kg", 30m, 10m, 95000m),
            ("Đùi gà góc tư", "Chicken Thighs", "Kg", 45m, 15m, 85000m),
            ("Cánh gà tươi", "Fresh Chicken Wings", "Kg", 35m, 12m, 110000m),
            ("Thịt heo xé Pulled Pork", "Pulled Pork", "Kg", 10m, 4m, 180000m),

            // Seafood
            ("Tôm sú nõn đông lạnh", "Peeled Shrimp", "Kg", 2.0m, 8m, 280000m), // LOW STOCK 2
            ("Mực ống cắt khoanh", "Squid Rings", "Kg", 15m, 5m, 210000m),
            ("Nghêu thịt đông lạnh", "Clam Meat", "Kg", 8m, 3m, 120000m),
            ("Thanh cua Surimi", "Crab Stick", "Kg", 1.0m, 5m, 140000m), // LOW STOCK 3
            ("Cá ngừ ngâm dầu", "Canned Tuna in Oil", "Lon", 40m, 15m, 32000m),
            ("Sò điệp thịt", "Scallop Meat", "Kg", 6m, 2m, 380000m),
            ("Phi lê cá hồi tươi", "Fresh Salmon Fillet", "Kg", 0.8m, 4m, 420000m), // LOW STOCK 4

            // Vegetables
            ("Nấm mỡ tươi", "Button Mushrooms", "Kg", 15m, 5m, 80000m),
            ("Hành tây trắng", "White Onions", "Kg", 30m, 10m, 22000m),
            ("Ớt chuông xanh", "Green Bell Peppers", "Kg", 16m, 5m, 45000m),
            ("Ớt chuông đỏ", "Red Bell Peppers", "Kg", 14m, 5m, 55000m),
            ("Bắp ngọt hạt", "Sweet Corn Kernels", "Kg", 20m, 6m, 35000m),
            ("Dứa tươi gọt vỏ", "Fresh Pineapples", "Kg", 25m, 8m, 20000m),
            ("Cà chua tươi", "Fresh Tomatoes", "Kg", 35m, 10m, 25000m),
            ("Cà chua bi Cherry", "Cherry Tomatoes", "Kg", 12m, 4m, 45000m),
            ("Oliu đen cắt lát", "Sliced Black Olives", "g", 8000m, 2000m, 160m),
            ("Bông cải xanh Broccoli", "Fresh Broccoli", "Kg", 10m, 3m, 50000m),
            ("Xà lách Romaine", "Romaine Lettuce", "Kg", 18m, 6m, 45000m),
            ("Dưa leo sạch", "Clean Cucumbers", "Kg", 20m, 5m, 18000m),
            ("Cà rốt Đà Lạt", "Da Lat Carrots", "Kg", 20m, 5m, 20000m),
            ("Lá Basil tươi", "Fresh Basil Leaves", "g", 300m, 1000m, 150m), // LOW STOCK 5

            // Sauces & Dressings
            ("Sốt cà chua San Marzano", "San Marzano Pizza Sauce", "L", 40m, 15m, 45000m),
            ("Sốt nướng BBQ khói", "Smoky BBQ Sauce", "L", 20m, 8m, 65000m),
            ("Sốt bơ tỏi Garlic Butter", "Garlic Butter Sauce", "L", 15m, 5m, 85000m),
            ("Sốt Pesto Ý Hạt thông", "Italian Pesto Sauce", "L", 8m, 3m, 190000m),
            ("Sốt kem béo Cooking Cream", "Cooking Cream Sauce", "L", 25m, 10m, 90000m),
            ("Sốt tiêu đen Black Pepper", "Black Pepper Sauce", "L", 12m, 4m, 70000m),
            ("Sốt phô mai Cheddar", "Cheddar Cheese Sauce", "L", 10m, 4m, 110000m),
            ("Sốt Mayonnaise Nhật", "Japanese Mayonnaise", "L", 20m, 6m, 55000m),
            ("Sốt mù tạt mật ong", "Honey Mustard Sauce", "L", 10m, 3m, 75000m),
            ("Sốt Bulgogi Hàn Quốc", "Korean Bulgogi Sauce", "L", 12m, 4m, 80000m),
            ("Sốt cay Gochujang", "Korean Spicy Sauce", "L", 15m, 5m, 75000m),
            ("Sốt Tom Yum Thái", "Thai Tom Yum Sauce", "L", 10m, 3m, 85000m),
            ("Sốt Caesar Dressing", "Caesar Dressing", "L", 12m, 4m, 95000m),
            ("Sốt mè rang Sesame", "Roasted Sesame Dressing", "L", 15m, 5m, 85000m),
            ("Dầu giấm Ý Vinaigrette", "Italian Vinaigrette", "L", 10m, 3m, 60000m),

            // Pasta & Rice
            ("Mì Spaghetti Ý khô", "Dry Italian Spaghetti", "Kg", 40m, 15m, 55000m),
            ("Mì Penne ống Ý", "Dry Penne Pasta", "Kg", 25m, 10m, 55000m),
            ("Lá mì Lasagna Ý", "Lasagna Sheets", "Kg", 15m, 5m, 85000m),
            ("Gạo thơm Jasmine", "Jasmine Fragrant Rice", "Kg", 50m, 20m, 22000m),

            // Drinks, Snacks & Packaging
            ("Lon Pepsi 320ml", "Pepsi 320ml Can", "Lon", 350m, 100m, 7500m),
            ("Lon Pepsi Không Calo 320ml", "Pepsi Zero 320ml Can", "Lon", 200m, 60m, 7500m),
            ("Lon 7Up 320ml", "7Up 320ml Can", "Lon", 250m, 80m, 7500m),
            ("Lon Mirinda Cam 320ml", "Mirinda Orange 320ml Can", "Lon", 200m, 60m, 7500m),
            ("Nước khoáng Aquafina 500ml", "Aquafina 500ml", "Chai", 180m, 50m, 4500m),
            ("Siro Đào đóng chai", "Peach Syrup", "L", 15m, 5m, 120000m),
            ("Siro Vải thơm", "Lychee Syrup", "L", 12m, 4m, 130000m),
            ("Cốt trà đen hảo hạng", "Premium Black Tea", "g", 8000m, 2000m, 220m),
            ("Bột Matcha Nhật Bản", "Japanese Matcha Powder", "g", 3000m, 1000m, 450m),
            ("Sữa tươi tiệt trùng", "Pasteurized Fresh Milk", "L", 30m, 10m, 32000m),
            ("Khoai tây cắt thẳng đông lạnh", "Frozen French Fries", "Kg", 60m, 20m, 48000m),
            ("Dầu chiên chuyên dụng", "Commercial Frying Oil", "L", 80m, 30m, 38000m)
        };

        var matMap = new Dictionary<string, Material>();
        foreach (var m in materialsData)
        {
            var uom = uomMap.ContainsKey(m.Uom) ? uomMap[m.Uom] : uomMap["Kg"];
            var existing = await context.Materials.FirstOrDefaultAsync(mat => mat.Name == m.Name || mat.NameEn == m.NameEn);
            if (existing == null)
            {
                existing = new Material
                {
                    UnitOfMeasureId = uom.Id,
                    Name = m.Name,
                    NameEn = m.NameEn,
                    CurrentStock = m.Stock,
                    MinStockLevel = m.MinStock,
                    CostPerUnit = m.Cost,
                    IsActive = true
                };
                await context.Materials.AddAsync(existing);
                await context.SaveChangesAsync();
            }
            else
            {
                // Ensure stock is updated for test cases (e.g. Low Stock)
                existing.CurrentStock = m.Stock;
                existing.MinStockLevel = m.MinStock;
                existing.CostPerUnit = m.Cost;
            }
            matMap[m.Name] = existing;
            matMap[m.NameEn] = existing;
        }
        await context.SaveChangesAsync();

        // ============================================================
        // 5. MODIFIER GROUPS & OPTIONS
        // ============================================================
        var modGroupsData = new List<(string Code, string Name, string NameEn, bool Req, int Min, int Max, int Sort, List<(string Name, string NameEn, decimal Extra, bool Def, int Sort)> Options)>
        {
            ("SIZE", "Chọn Kích thước Pizza", "Select Pizza Size", true, 1, 1, 1, new()
            {
                ("Nhỏ (Small - 20cm)", "Small (20cm)", 0m, true, 1),
                ("Vừa (Medium - 25cm)", "Medium (25cm)", 50000m, false, 2),
                ("Lớn (Large - 30cm)", "Large (30cm)", 100000m, false, 3)
            }),
            ("CRUST", "Chọn Kiểu Đế Pizza", "Select Pizza Crust", true, 1, 1, 2, new()
            {
                ("Đế Dày Truyền Thống", "Traditional Thick Crust", 0m, true, 1),
                ("Đế Mỏng Giòn", "Thin & Crispy Crust", 0m, false, 2),
                ("Đế Kéo Tay Kiểu Ý", "Hand-tossed Italian Crust", 0m, false, 3),
                ("Viền Phô Mai Nung Chảy", "Stuffed Cheese Crust", 35000m, false, 4),
                ("Viền Xúc Xích Phô Mai", "Sausage & Cheese Crust", 45000m, false, 5)
            }),
            ("SAUCE", "Chọn Sốt Nền Pizza", "Select Pizza Sauce", true, 1, 1, 3, new()
            {
                ("Sốt Cà Chua San Marzano", "San Marzano Tomato Sauce", 0m, true, 1),
                ("Sốt BBQ Khói Đậm Đà", "Smoky BBQ Sauce", 0m, false, 2),
                ("Sốt Bơ Tỏi Thơm Lừng", "Garlic Butter Sauce", 0m, false, 3),
                ("Sốt Pesto Ý Hạt Thông", "Italian Pesto Sauce", 15000m, false, 4),
                ("Sốt Tiêu Đen Đặc Trưng", "Black Pepper Sauce", 0m, false, 5)
            }),
            ("CHEESE", "Chọn Thêm Phô Mai", "Extra Cheese Options", false, 0, 1, 4, new()
            {
                ("Phô mai Tiêu Chuẩn", "Standard Cheese", 0m, true, 1),
                ("Thêm Phô Mai Mozzarella", "Extra Mozzarella", 20000m, false, 2),
                ("Thêm Phô Mai Cheddar Vàng", "Extra Cheddar", 20000m, false, 3),
                ("Thêm Phô Mai Parmesan Ý", "Extra Parmesan", 25000m, false, 4)
            }),
            ("TOPPING", "Thêm Topping Yêu Thích", "Extra Toppings", false, 0, 5, 5, new()
            {
                ("Xúc xích Ý Pepperoni", "Extra Pepperoni", 25000m, false, 1),
                ("Thịt xông khói Bacon", "Extra Bacon", 25000m, false, 2),
                ("Giăm bông Ham", "Extra Ham", 20000m, false, 3),
                ("Bò băm đút lò", "Extra Minced Beef", 30000m, false, 4),
                ("Bò nướng BBQ", "Extra BBQ Beef", 30000m, false, 5),
                ("Gà nướng xé", "Extra Grilled Chicken", 25000m, false, 6),
                ("Heo nướng BBQ", "Extra Pulled Pork", 25000m, false, 7),
                ("Tôm tươi nõn", "Extra Shrimp", 35000m, false, 8),
                ("Mực ống cắt khoanh", "Extra Squid", 35000m, false, 9),
                ("Thanh cua Surimi", "Extra Crab Stick", 20000m, false, 10),
                ("Nấm mỡ tươi", "Extra Button Mushrooms", 15000m, false, 11),
                ("Hành tây nướng", "Extra Roasted Onions", 10000m, false, 12),
                ("Ớt chuông xanh đỏ", "Extra Bell Peppers", 10000m, false, 13),
                ("Bắp ngọt hạt", "Extra Sweet Corn", 10000m, false, 14),
                ("Dứa tươi ngọt", "Extra Pineapples", 10000m, false, 15),
                ("Cà chua bi Cherry", "Extra Cherry Tomatoes", 15000m, false, 16),
                ("Oliu đen Hy Lạp", "Extra Black Olives", 15000m, false, 17),
                ("Bông cải xanh", "Extra Broccoli", 10000m, false, 18)
            }),
            ("SPICE", "Mức Độ Cay", "Spice Level", true, 1, 1, 6, new()
            {
                ("Không Cay", "No Spice", 0m, true, 1),
                ("Cay Nhẹ", "Mild Spice", 0m, false, 2),
                ("Cay Vừa", "Medium Spice", 0m, false, 3),
                ("Cay Nhiều", "Hot Spice", 0m, false, 4)
            }),
            ("SALAD_DRESSING", "Chọn Sốt Salad", "Select Salad Dressing", true, 1, 1, 7, new()
            {
                ("Sốt Caesar Truyền Thống", "Classic Caesar Dressing", 0m, true, 1),
                ("Sốt Thousand Island", "Thousand Island Dressing", 0m, false, 2),
                ("Sốt Mè Rang Nhật Bản", "Roasted Sesame Dressing", 0m, false, 3),
                ("Sốt Dầu Giấm Vinaigrette", "Italian Vinaigrette", 0m, false, 4),
                ("Sốt Chanh Kem Lemon Cream", "Lemon Cream Dressing", 0m, false, 5)
            }),
            ("DRINK_SUGAR", "Chọn Mức Đường", "Select Sugar Level", true, 1, 1, 8, new()
            {
                ("100% Đường (Chuẩn vị)", "100% Sugar (Standard)", 0m, true, 1),
                ("70% Đường", "70% Sugar", 0m, false, 2),
                ("50% Đường", "50% Sugar", 0m, false, 3),
                ("30% Đường", "30% Sugar", 0m, false, 4),
                ("0% Đường (Không ngọt)", "0% Sugar (Unsweetened)", 0m, false, 5)
            }),
            ("DRINK_ICE", "Chọn Mức Đá", "Select Ice Level", true, 1, 1, 9, new()
            {
                ("100% Đá (Chuẩn mát)", "100% Ice (Standard)", 0m, true, 1),
                ("70% Đá", "70% Ice", 0m, false, 2),
                ("50% Đá", "50% Ice", 0m, false, 3),
                ("30% Đá", "30% Ice", 0m, false, 4),
                ("Không Đá", "No Ice", 0m, false, 5)
            }),
            ("DRINK_TOPPING", "Thêm Topping Thức Uống", "Extra Drink Toppings", false, 0, 3, 10, new()
            {
                ("Trân Châu Đen Dẻo", "Chewy Black Pearls", 10000m, false, 1),
                ("Thạch Trái Cây Hỗn Hợp", "Mixed Fruit Jelly", 10000m, false, 2),
                ("Hạt É Tươi Thanh Mát", "Fresh Basil Seeds", 10000m, false, 3),
                ("Miếng Đào Ngâm Giòn", "Peach Slices", 15000m, false, 4),
                ("Quả Vải Ngâm Ngọt Dịu", "Lychee Pieces", 15000m, false, 5)
            })
        };

        var modGroupMap = new Dictionary<string, ModifierGroup>();
        foreach (var mg in modGroupsData)
        {
            var existingMg = await context.ModifierGroups
                .Include(g => g.ModifierOptions)
                .FirstOrDefaultAsync(g => g.Name == mg.Name || g.NameEn == mg.NameEn);

            if (existingMg == null)
            {
                existingMg = new ModifierGroup
                {
                    Name = mg.Name,
                    NameEn = mg.NameEn,
                    IsRequired = mg.Req,
                    MinSelections = mg.Min,
                    MaxSelections = mg.Max,
                    SortOrder = mg.Sort
                };
                await context.ModifierGroups.AddAsync(existingMg);
                await context.SaveChangesAsync();
            }

            // Options
            foreach (var opt in mg.Options)
            {
                var existingOpt = existingMg.ModifierOptions.FirstOrDefault(o => o.Name == opt.Name || o.NameEn == opt.NameEn ||
                    (mg.Code == "SIZE" && ((opt.Sort == 1 && (o.Name.Contains("Small") || o.Name.Contains("Nhỏ") || o.Name.Contains("Size S") || o.Name.Contains("Size M (25cm)"))) ||
                                           (opt.Sort == 2 && (o.Name.Contains("Medium") || o.Name.Contains("Vừa") || o.Name.Contains("Size M (25cm -") || o.Name.Contains("Size L (30cm)"))) ||
                                           (opt.Sort == 3 && (o.Name.Contains("Large") || o.Name.Contains("Lớn") || o.Name.Contains("Size L (30cm -"))))));
                if (existingOpt == null)
                {
                    existingOpt = new ModifierOption
                    {
                        ModifierGroupId = existingMg.Id,
                        Name = opt.Name,
                        NameEn = opt.NameEn,
                        ExtraPrice = opt.Extra,
                        IsDefault = opt.Def,
                        SortOrder = opt.Sort,
                        IsActive = true
                    };
                    await context.ModifierOptions.AddAsync(existingOpt);
                }
                else
                {
                    existingOpt.Name = opt.Name;
                    existingOpt.NameEn = opt.NameEn;
                    existingOpt.ExtraPrice = opt.Extra;
                    existingOpt.IsDefault = opt.Def;
                    existingOpt.SortOrder = opt.Sort;
                    existingOpt.IsActive = true;
                }
            }
            await context.SaveChangesAsync();

            // Deactivate or remove any extra options in SIZE group that are not in the 3 standardized sizes
            if (mg.Code == "SIZE")
            {
                var validNames = mg.Options.Select(o => o.Name).ToHashSet();
                var extraOpts = existingMg.ModifierOptions.Where(o => !validNames.Contains(o.Name)).ToList();
                foreach (var extra in extraOpts)
                {
                    var isUsed = await context.OrderItemModifiers.AnyAsync(oim => oim.ModifierOptionId == extra.Id);
                    if (!isUsed)
                    {
                        context.ModifierOptions.Remove(extra);
                    }
                    else
                    {
                        extra.IsActive = false;
                    }
                }
                await context.SaveChangesAsync();
            }

            modGroupMap[mg.Code] = existingMg;
        }

        // ============================================================
        // 6. MENU ITEMS (>= 105 Items)
        // ============================================================
        var itemsData = new List<(string CatName, string StationCode, string Name, string NameEn, string Desc, string DescEn, decimal Price, bool IsCombo, bool IsActive, int Sort, string Img)>
        {
            // --- PIZZA TRUYỀN THỐNG (10) ---
            ("Traditional Pizza", "PIZZA", "Pizza Margherita Mozzarella", "Margherita Mozzarella Pizza", "Phô mai Mozzarella tươi, sốt cà chua San Marzano & lá Basil", "Fresh Mozzarella, San Marzano tomato sauce & basil", 149000m, false, true, 1, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Pepperoni Xúc Xích Ý", "Italian Pepperoni Pizza", "Xúc xích Pepperoni cay nồng nướng vàng cùng phô mai ngập tràn", "Spicy Pepperoni sausage with bubbling melted cheese", 179000m, false, true, 2, "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Hawaiian Dứa & Giăm Bông", "Hawaiian Ham & Pineapple Pizza", "Giăm bông nướng thơm lừng kết hợp dứa nhiệt đới chua ngọt", "Tender ham paired with sweet juicy tropical pineapple", 169000m, false, true, 3, "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Thập Cẩm Supreme", "Supreme Meat & Veggie Pizza", "Bò băm, xúc xích, thịt xông khói, ớt chuông & nấm mỡ", "Minced beef, sausage, bacon, bell peppers & mushrooms", 189000m, false, true, 4, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Bò Nướng BBQ", "BBQ Beef Pizza", "Thịt bò nướng sốt BBQ khói đậm đà, hành tây & phô mai", "Smoky BBQ sliced beef with roasted onions & cheese", 199000m, false, true, 5, "https://images.unsplash.com/photo-1593560708920-61dd98c46a4e?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Bò Bulgogi Sốt Hàn", "Korean Bulgogi Beef Pizza", "Bò ướp sốt Bulgogi mềm mọng, mè rang thơm bùi", "Tender beef marinated in sweet-savory Bulgogi sauce", 199000m, false, true, 6, "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Gà Phô Mai Nướng", "Cheesy Grilled Chicken Pizza", "Gà nướng thảo mộc đẫm sốt kem phô mai béo ngậy", "Herbed grilled chicken with creamy melted cheese blend", 179000m, false, true, 7, "https://images.unsplash.com/photo-1594007654729-407eedc4be65?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Gà Nướng Nấm Tiêu Đen", "Black Pepper Chicken Mushroom Pizza", "Thịt gà nướng nấm mỡ với sốt tiêu đen cay nhẹ kích thích", "Chicken & mushrooms in mild fragrant black pepper sauce", 179000m, false, true, 8, "https://images.unsplash.com/photo-1588315029754-2dd089d39a1a?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Cá Ngừ Thanh Cua", "Tuna & Crab Stick Pizza", "Cá ngừ thơm bùi kết hợp thanh cua ngọt dịu & bắp non", "Flaked tuna and sweet crab stick with juicy sweet corn", 189000m, false, true, 9, "https://images.unsplash.com/photo-1571407970349-bc81e7e96d47?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Rau Củ Chay Nấm Mỡ", "Vegetarian Garden Pizza", "Nấm mỡ, ớt chuông, cà chua bi, bắp & oliu đen thanh nhẹ", "Fresh mushrooms, bell peppers, tomatoes, corn & olives", 149000m, false, true, 10, "https://images.unsplash.com/photo-1541745537411-b8046dc6d66c?w=500"),

            // --- PIZZA TRUYỀN THỐNG (TIẾP TỤC HẢI SẢN & BÒ) ---
            ("Traditional Pizza", "PIZZA", "Pizza Hải Sản Nhiệt Đới", "Tropical Seafood Pizza", "Tôm tươi, mực ống, thanh cua & dứa sốt Thousand Island", "Fresh shrimp, squid, crab stick and pineapple", 219000m, false, true, 11, "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Hải Sản Tiêu Đen", "Black Pepper Seafood Pizza", "Tôm sú, mực tươi nướng xém cạnh cùng sốt tiêu đen Phú Quốc", "Shrimp & squid seared with Phu Quoc black pepper sauce", 219000m, false, true, 12, "https://images.unsplash.com/photo-1571997478779-2adcbbe9ab2f?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Hải Sản Pesto Hạt Thông", "Pesto Seafood Pine Nut Pizza", "Hải sản cao cấp quyện trong sốt húng tây Pesto hạt thông Ý", "Seafood tossed in authentic Genovese basil pine nut pesto", 229000m, false, true, 13, "https://images.unsplash.com/photo-1589187151003-0dd4e638a50e?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Tôm Bơ Tỏi Thượng Hạng", "Garlic Butter Shrimp Pizza", "Tôm nõn béo ngậy sốt bơ tỏi thơm lừng rắc lá mùi tây", "Plump shrimp swimming in garlic herb butter sauce", 229000m, false, true, 14, "https://images.unsplash.com/photo-1544982503-9f984c14501a?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Bốn Phô Mai Quattro Formaggi", "Quattro Formaggi Four Cheese Pizza", "Mozzarella, Cheddar, Parmesan & Cream Cheese ngập ngụa", "Mozzarella, Cheddar, Parmesan and rich Cream Cheese", 209000m, false, true, 15, "https://images.unsplash.com/photo-1573821663912-569905455b1c?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Bò & Hải Sản Surf 'n' Turf", "Surf 'n' Turf Beef & Shrimp Pizza", "Sự kết hợp đỉnh cao giữa thăn bò nướng mềm và tôm nõn giòn", "Luxury combination of tender beef and crisp shrimp", 249000m, false, true, 16, "https://images.unsplash.com/photo-1595708684082-a173bb3a06c5?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Heo Nướng BBQ Xé Sợi", "Pulled Pork Smoky BBQ Pizza", "Thịt heo xé đút lò ủ sốt BBQ cay ngọt cùng hành tây nướng", "Slow-cooked pulled pork drizzled with smoky sweet BBQ", 189000m, false, true, 17, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Deluxe Meat Lovers", "Deluxe Meat Lovers Pizza", "Bò băm, thịt xông khói, xúc xích Ý, ham & gà nướng giòn", "Five premium meats for true carnivore satisfaction", 239000m, false, true, 18, "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza Seafood Supreme Hoàng Gia", "Royal Seafood Supreme Pizza", "Tôm, mực, sò điệp nướng vàng phủ phô mai nướng chảy", "Shrimp, calamari, scallops with golden roasted cheese", 259000m, false, true, 19, "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=500"),
            ("Traditional Pizza", "PIZZA", "Pizza House Special Minbeeus", "Minbeeus Signature House Special", "Công thức độc quyền với bò xông khói, tôm bơ tỏi & 4 loại cheese", "House special signature with smoked beef, garlic shrimp & 4 cheese", 269000m, false, true, 20, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500"),
            // Inactive items for testing
            ("Traditional Pizza", "PIZZA", "Pizza Nấm Truffle Thượng Hạng", "Black Truffle Mushroom Pizza", "Dầu nấm Truffle đen quý hiếm & phô mai nướng củi (Tạm ngưng)", "Rare black truffle oil with wood-fired cheese (Inactive)", 299000m, false, false, 21, ""),
            ("Traditional Pizza", "PIZZA", "Pizza Bò Wagyu Nướng Tảng", "Wagyu Beef Cut Pizza", "Thịt bò Wagyu vân mỡ nướng sốt vang đỏ (Tạm ngưng mùa)", "Japanese Wagyu beef slices with red wine sauce (Inactive)", 329000m, false, false, 22, ""),

            // --- MELTS (5) ---
            ("Crispy Melts", "PIZZA", "Melts Bò BBQ Phô Mai", "BBQ Beef Crispy Melts", "Vỏ bánh giòn kẹp bò nướng BBQ đậm vị & phô mai kéo sợi", "Folded crispy crust packed with BBQ beef and melted cheese", 89000m, false, true, 1, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=500"),
            ("Crispy Melts", "PIZZA", "Melts Gà Nướng Nấm Mỡ", "Grilled Chicken Mushroom Melts", "Thịt gà nướng nấm sốt kem béo ngậy thơm ngon", "Herbed chicken, mushrooms and cream inside golden crust", 79000m, false, true, 2, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=500"),
            ("Crispy Melts", "PIZZA", "Melts Pepperoni Giòn Rụm", "Pepperoni Cheesy Melts", "Xúc xích Pepperoni cay kẹp phô mai Mozzarella ngập tràn", "Spicy pepperoni with overflowing melted mozzarella", 85000m, false, true, 3, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=500"),
            ("Crispy Melts", "PIZZA", "Melts Hải Sản Pesto Thơm Bùi", "Pesto Seafood Melts", "Tôm & mực sốt Pesto xanh kẹp bánh nướng giòn", "Tender shrimp and calamari with fragrant basil pesto", 99000m, false, true, 4, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=500"),
            ("Crispy Melts", "PIZZA", "Melts Bốn Phô Mai Kéo Sợi", "Four Cheese Melts", "Dành cho tín đồ phô mai với 4 tầng cheese tan chảy", "Pure cheese lover delight with four molten cheeses", 85000m, false, true, 5, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=500"),

            // --- PASTA & LASAGNA (10) ---
            ("Pasta & Lasagna", "PASTA", "Mì Ý Bolognese Bò Băm", "Spaghetti Bolognese", "Mì Ý sợi truyền thống sốt thịt bò băm hầm cà chua đút lò", "Classic slow-simmered minced beef tomato sauce spaghetti", 119000m, false, true, 1, "https://images.unsplash.com/photo-1551183053-bf91a1d81141?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Carbonara Kem Thịt Khói", "Spaghetti Carbonara", "Sốt kem béo ngậy, lòng đỏ trứng, thịt xông khói & Parmesan", "Creamy egg yolk sauce with crisp bacon and parmesan", 129000m, false, true, 2, "https://images.unsplash.com/photo-1612874742237-6526221588e3?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Gà Nấm Sốt Kem Tươi", "Chicken Mushroom Cream Pasta", "Thịt gà xào nấm mỡ trong sốt kem béo mịn thơm ngát", "Sautéed chicken and mushrooms in rich velvety cream sauce", 119000m, false, true, 3, "https://images.unsplash.com/photo-1546549032-9571cd6b27df?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Hải Sản Sốt Cà Chua", "Seafood Marinara Pasta", "Tôm nõn, mực ống xào sốt cà chua Marinara chua dịu", "Shrimp and squid tossed in zesty Italian marinara", 139000m, false, true, 4, "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Hải Sản Sốt Tiêu Đen", "Black Pepper Seafood Pasta", "Hải sản tươi xào mì Ý cùng sốt tiêu đen dậy mùi cay nhẹ", "Seafood spaghetti with aromatic crushed black pepper sauce", 139000m, false, true, 5, "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Tôm Sốt Kem Bơ Tỏi", "Garlic Butter Shrimp Cream Pasta", "Tôm tươi sốt kem bơ tỏi rắc phô mai Parmesan bào mịn", "Pan-seared shrimp in garlic butter cream sauce", 149000m, false, true, 6, "https://images.unsplash.com/photo-1551183053-bf91a1d81141?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Xúc Xích Cay Aglio Olio", "Spicy Sausage Aglio Olio", "Mì xào tỏi ớt dầu olive với xúc xích Ý nướng cay thơm lừng", "Garlic, chili, olive oil spaghetti with sliced spicy sausage", 109000m, false, true, 7, "https://images.unsplash.com/photo-1546549032-9571cd6b27df?w=500"),
            ("Pasta & Lasagna", "PASTA", "Mì Ý Bò Nướng Sốt Kem Nấm", "Creamy Mushroom Beef Pasta", "Thịt thăn bò mềm xào mì ống Penne sốt kem nấm béo", "Penne tossed with beef strips in savory mushroom cream", 139000m, false, true, 8, "https://images.unsplash.com/photo-1612874742237-6526221588e3?w=500"),
            ("Pasta & Lasagna", "PASTA", "Lasagna Bò Băm Đút Lò", "Baked Beef Lasagna", "Từng lớp mì lasagna xen kẽ bò băm, sốt kem & phô mai nung", "Layered pasta sheets with meat ragu, bechamel & melted cheese", 149000m, false, true, 9, "https://images.unsplash.com/photo-1574894709920-11b28e7367e3?w=500"),
            ("Pasta & Lasagna", "PASTA", "Lasagna Hải Sản Phô Mai Đút Lò", "Baked Seafood Lasagna", "Lasagna hải sản phủ phô mai nướng vàng rộm thơm nức", "Rich seafood layered pasta with golden bubbly mozzarella", 159000m, false, true, 10, "https://images.unsplash.com/photo-1574894709920-11b28e7367e3?w=500"),

            // --- GÀ GIÒN & GÀ NƯỚNG (20 + 1 INACTIVE) ---
            ("Crispy & Grilled Chicken", "FRYER", "Gà Nugget Chiên Giòn (6 miếng)", "Crispy Chicken Nuggets (6 pcs)", "Gà viên không xương lăn bột chiên vàng giòn rụm", "Tender bite-sized chicken nuggets fried to perfection", 59000m, false, true, 1, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Tôm Nugget Phô Mai (5 miếng)", "Cheesy Shrimp Nuggets (5 pcs)", "Tôm bọc phô mai tan chảy lăn bột xù chiên giòn", "Shrimp bites stuffed with molten cheese", 69000m, false, true, 2, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Cá Nugget Giòn Sốt Tartar", "Fish Nuggets with Tartar Sauce", "Cá phi lê tẩm bột chiên vàng kèm sốt Tartar chua béo", "Crispy fish fillet bites served with creamy tartar sauce", 65000m, false, true, 3, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Tôm Donut Chiên Xù (3 miếng)", "Shrimp Donut Rings (3 pcs)", "Chả tôm tạo hình donut tẩm bột chiên xù giòn tan", "Donut-shaped minced shrimp fried golden brown", 69000m, false, true, 4, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Đùi Gà Giòn Truyền Thống (1 cái)", "Traditional Crispy Drumstick (1 pc)", "Đùi gà tươi ướp gia vị chiên giòn da mọng thịt", "Crispy golden fried chicken drumstick, juicy inside", 42000m, false, true, 5, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Đùi Gà Sốt BBQ Nướng Đậm Đà", "BBQ Glazed Drumstick", "Đùi gà nướng quét sốt BBQ thơm lừng da bóng giòn", "Grilled drumstick brushed with smoky sweet barbecue sauce", 45000m, false, true, 6, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Đùi Gà Sốt Bulgogi Hàn Quốc", "Korean Bulgogi Drumstick", "Đùi gà nướng sốt tương ngọt Bulgogi rắc mè rang", "Korean sweet soy glazed juicy chicken drumstick", 45000m, false, true, 7, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Đùi Gà Lắc Trứng Muối Hoàng Kim", "Salted Egg Yolk Drumstick", "Đùi gà giòn lắc bột sốt trứng muối thơm bùi béo ngậy", "Crispy chicken drumstick tossed in rich salted egg yolk", 49000m, false, true, 8, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Đùi Gà Sốt Cay Hàn Quốc", "Spicy Korean Glazed Drumstick", "Đùi gà giòn sốt ớt cay ngọt Gochujang chuẩn vị Seoul", "Sweet spicy Gochujang glazed fried drumstick", 45000m, false, true, 9, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Đùi Gà Tom Yum Cay Thái", "Thai Tom Yum Chicken Drumstick", "Đùi gà ướp gia vị Tom Yum chua cay kích thích vị giác", "Tangy and spicy Thai Tom Yum flavored chicken drumstick", 45000m, false, true, 10, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Đùi Gà Phủ Phô Mai Nung Chảy", "Cheesy Melted Chicken Drumstick", "Đùi gà nướng phủ lớp phô mai Mozzarella đút lò nung chảy", "Grilled drumstick smothered with melted mozzarella cheese", 49000m, false, true, 11, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Cánh Gà Sốt BBQ (4 miếng)", "BBQ Glazed Chicken Wings (4 pcs)", "4 cánh gà nướng sốt BBQ đậm đà thơm khói", "4 grilled wings glazed with sticky smoky barbecue sauce", 89000m, false, true, 12, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "GRILL", "Cánh Gà Sốt BBQ (6 miếng)", "BBQ Glazed Chicken Wings (6 pcs)", "6 cánh gà nướng sốt BBQ thơm lừng đậm vị cho nhóm", "6 party wings glazed with sticky smoky barbecue sauce", 129000m, false, true, 13, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Cánh Gà Chiên Giòn Karaage (4 miếng)", "Crispy Karaage Wings (4 pcs)", "Cánh gà chiên giòn phong cách Nhật da mỏng rụm", "Japanese-style crispy savory fried chicken wings", 85000m, false, true, 14, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Cánh Gà Chiên Giòn Karaage (6 miếng)", "Crispy Karaage Wings (6 pcs)", "6 cánh gà chiên giòn thơm ngon tuyệt hảo", "6 pieces of extra crunchy Japanese karaage wings", 125000m, false, true, 15, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Cánh Gà Cay Hàn Quốc (4 miếng)", "Spicy Korean Glazed Wings (4 pcs)", "Cánh gà giòn sốt ớt cay ngọt Hàn Quốc rắc mè", "4 crispy wings coated in sticky spicy-sweet Korean sauce", 89000m, false, true, 16, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Cánh Gà Cay Hàn Quốc (6 miếng)", "Spicy Korean Glazed Wings (6 pcs)", "6 cánh gà cay ngọt cực đã ăn là mê", "6 pieces of mouthwatering spicy Korean glazed wings", 129000m, false, true, 17, "https://images.unsplash.com/photo-1527477321005-4d45d724b8c4?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Gà Popcorn Không Xương Sốt BBQ", "Boneless Popcorn Chicken BBQ", "Gà miếng không xương chiên giòn lắc sốt BBQ", "Bite-sized boneless fried chicken tossed in BBQ glaze", 79000m, false, true, 18, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Gà Popcorn Không Xương Cay Giòn", "Spicy Boneless Popcorn Chicken", "Gà viên giòn rụm tẩm ớt bột cay nồng kích thích", "Spicy seasoning tossed boneless chicken bites", 79000m, false, true, 19, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Crispy & Grilled Chicken", "FRYER", "Gà Không Xương Lắc Trứng Muối", "Salted Egg Boneless Chicken", "Gà chiên giòn phủ sốt bột trứng muối hoàng kim", "Tender fried chicken tossed in rich golden salted egg sauce", 85000m, false, true, 20, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            // Inactive chicken item
            ("Crispy & Grilled Chicken", "FRYER", "Gà Rán Sốt Hạt Dẻ Cười Pistachio", "Pistachio Glazed Fried Chicken", "Gà rán phủ sốt hạt dẻ béo thơm (Tạm ngưng phục vụ)", "Fried chicken with pistachio nut glaze (Inactive)", 99000m, false, false, 21, ""),

            // --- KHAI VỊ & MÓN ĂN KÈM (12) ---
            ("Sides & Snacks", "FRYER", "Khoai Tây Chiên Cắt Thẳng", "Classic French Fries", "Khoai tây nhập khẩu chiên vàng giòn rắc muối tinh", "Golden crispy salted French fries", 39000m, false, true, 1, "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=500"),
            ("Sides & Snacks", "FRYER", "Khoai Tây Lắc Phô Mai", "Cheese Shaker French Fries", "Khoai tây chiên giòn lắc bột phô mai béo thơm", "French fries shaken in savory sweet cheese seasoning", 49000m, false, true, 2, "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=500"),
            ("Sides & Snacks", "GRILL", "Khoai Tây Đút Lò Bacon & Cheese", "Baked Bacon & Cheese Fries", "Khoai tây phủ ngập phô mai & thịt xông khói đút lò", "Oven baked fries topped with melted cheese and crisp bacon", 59000m, false, true, 3, "https://images.unsplash.com/photo-1586190848861-99aa4a171e90?w=500"),
            ("Sides & Snacks", "GRILL", "Bánh Mì Bơ Tỏi Nướng Giòn", "Crispy Garlic Bread (4 pcs)", "4 lát bánh mì bơ tỏi thảo mộc nướng vàng giòn rụm", "4 slices of toasted French baguette with garlic herb butter", 35000m, false, true, 4, "https://images.unsplash.com/photo-1573140247632-f8fd74997d5c?w=500"),
            ("Sides & Snacks", "FRYER", "Bánh Cuộn Phô Mai Que Mozzarella", "Mozzarella Cheese Sticks (4 pcs)", "Phô mai Mozzarella lăn bột xù chiên kéo sợi siêu dài", "Gooey stretchy melted mozzarella breaded cheese sticks", 55000m, false, true, 5, "https://images.unsplash.com/photo-1548340748-6d2b7d7da280?w=500"),
            ("Sides & Snacks", "FRYER", "Phô Mai Viên Chiên Giòn Xù", "Crispy Cheese Balls (6 pcs)", "Viên phô mai chiên xù vàng ươm cắn ngập miệng", "6 bite-sized crispy golden mozzarella cheese balls", 49000m, false, true, 6, "https://images.unsplash.com/photo-1548340748-6d2b7d7da280?w=500"),
            ("Appetizers", "GRILL", "Bánh Bắp Nachos Sốt Phô Mai & Bò", "Beef & Cheese Loaded Nachos", "Bánh bắp giòn rụm phủ thịt bò băm & sốt phô mai ấm", "Crunchy tortilla chips loaded with seasoned beef and warm cheese", 69000m, false, true, 7, "https://images.unsplash.com/photo-1513456852971-30c0b8199d4d?w=500"),
            ("Appetizers", "FRYER", "Mực Khoanh Chiên Giòn Calamari", "Crispy Fried Calamari", "Mực ống tẩm bột gia vị chiên giòn kèm sốt chanh ớt", "Tender calamari rings fried crisp with dipping sauce", 89000m, false, true, 8, "https://images.unsplash.com/photo-1599488615731-7e5c2823ff28?w=500"),
            ("Appetizers", "GRILL", "Bắp Ngọt Đút Lò Phô Mai", "Cheesy Baked Sweet Corn", "Bắp ngọt hạt bơ đút lò phủ phô mai nướng vàng cháy xém", "Sweet corn baked in butter and gooey melted mozzarella", 45000m, false, true, 9, "https://images.unsplash.com/photo-1551754655-cd27e38d2076?w=500"),
            ("Appetizers", "GRILL", "Xúc Xích Đức Nướng Thảo Mộc", "Grilled German Herb Sausages", "Xúc xích nướng căng mọng da giòn rụm sốt mù tạt", "Grilled German savory sausages with mustard dip", 55000m, false, true, 10, "https://images.unsplash.com/photo-1541544741938-0af808871cc0?w=500"),
            ("Appetizers", "PASTA", "Soup Kem Nấm Gà Nướng", "Cream of Mushroom & Chicken Soup", "Súp kem béo ngậy nấu từ nấm mỡ & thịt ức gà xé", "Velvety cream soup with button mushrooms and chicken", 49000m, false, true, 11, "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=500"),
            ("Appetizers", "PASTA", "Soup Hải Sản Chua Cay Ý", "Zesty Italian Seafood Soup", "Súp hải sản nấu cùng cà chua và thảo mộc chua thanh", "Hearty seafood soup simmered with tomato and herbs", 55000m, false, true, 12, "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=500"),

            // --- SALAD TƯƠI MÁT (6) ---
            ("Fresh Salad", "SALAD", "Salad Caesar Gà Nướng Tiêu Đen", "Grilled Chicken Caesar Salad", "Rau Romaine giòn, gà nướng tiêu, bánh mì sấy & Parmesan", "Romaine lettuce, black pepper chicken, croutons & parmesan", 89000m, false, true, 1, "https://images.unsplash.com/photo-1550304943-4f24f54ddde9?w=500"),
            ("Fresh Salad", "SALAD", "Salad Gà Nướng Sốt Mè Rang", "Roasted Sesame Chicken Salad", "Xà lách xà lách, gà nướng, dưa leo & sốt mè rang bùi béo", "Fresh garden greens with chicken and roasted sesame dressing", 89000m, false, true, 2, "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=500"),
            ("Fresh Salad", "SALAD", "Salad Tôm Nướng Trái Cây", "Grilled Shrimp & Fruit Salad", "Tôm nướng, dứa, cà chua bi, xà lách & sốt chanh mật ong", "Seared shrimp, fresh pineapple, cherry tomatoes and citrus sauce", 99000m, false, true, 3, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=500"),
            ("Fresh Salad", "SALAD", "Salad Cá Ngừ Ngâm Dầu", "Mediterranean Tuna Salad", "Cá ngừ, oliu đen, bắp ngọt, cà chua & sốt giấm Ý", "Flaked tuna, black olives, sweet corn and Italian dressing", 89000m, false, true, 4, "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=500"),
            ("Fresh Salad", "SALAD", "Salad Thịt Bò Áp Chảo", "Pan-Seared Beef Garden Salad", "Thăn bò áp chảo mềm ngọt trên nền rau củ quả sốt tiêu", "Juicy tender seared beef strips on fresh mixed greens", 109000m, false, true, 5, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=500"),
            ("Fresh Salad", "SALAD", "Salad Rau Củ Thanh Mát Vinaigrette", "Italian Garden Green Salad", "Rau xà lách tổng hợp, dưa leo, cà chua bi sốt dầu giấm", "Crisp mixed greens with cucumbers and Italian vinaigrette", 69000m, false, true, 6, "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=500"),

            // --- CƠM ĐÚT LÒ (5) ---
            ("Baked Rice", "PASTA", "Cơm Bò Nướng Đút Lò Phô Mai", "Cheesy Baked Beef Rice", "Cơm trộn sốt BBQ nướng bò mềm phủ phô mai nung chảy", "Baked rice topped with savory beef and bubbly mozzarella", 99000m, false, true, 1, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),
            ("Baked Rice", "PASTA", "Cơm Gà BBQ Đút Lò Phô Mai", "Cheesy Baked BBQ Chicken Rice", "Cơm nướng gà sốt BBQ đậm đà đẫm phô mai béo ngậy", "Oven baked rice with BBQ glazed chicken and melted cheese", 89000m, false, true, 2, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),
            ("Baked Rice", "PASTA", "Cơm Hải Sản Đút Lò Phô Mai", "Cheesy Baked Seafood Rice", "Tôm và mực tươi nướng cơm sốt cà chua phô mai vàng rộm", "Seafood baked rice with tomato herb sauce and mozzarella", 109000m, false, true, 3, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),
            ("Baked Rice", "PASTA", "Cơm Đùi Gà Giòn Sốt Teriyaki", "Crispy Chicken Teriyaki Rice", "Đùi gà chiên giòn ăn kèm cơm nóng & sốt tương Nhật ngọt", "Crispy fried drumstick over rice with sweet teriyaki glaze", 85000m, false, true, 4, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),
            ("Baked Rice", "PASTA", "Cơm Chiên Tỏi Trứng Oliu", "Garlic Egg & Olive Fried Rice", "Cơm chiên tỏi thơm hạt tơi xốp điểm xuyết trứng và oliu", "Fragrant garlic fried rice tossed with eggs and black olives", 69000m, false, true, 5, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),

            // --- THỨC UỐNG & TRÀ (16 + 1 INACTIVE) ---
            ("Drinks & Fresh Tea", "BAR", "Pepsi Lon 320ml", "Pepsi 320ml Can", "Nước ngọt có ga sảng khoái mát lạnh đóng lon", "Chilled refreshing classic Pepsi 320ml can", 22000m, false, true, 1, "https://images.unsplash.com/photo-1629203851122-3726ecdf080e?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Pepsi Không Calo Lon 320ml", "Pepsi Zero Sugar 320ml Can", "Nước ngọt có ga không đường không calo mát lạnh", "Zero sugar, zero calorie refreshing Pepsi can", 22000m, false, true, 2, "https://images.unsplash.com/photo-1629203851122-3726ecdf080e?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Pepsi Chanh Muối Lon 320ml", "Pepsi Lime Salt 320ml Can", "Vị chanh muối đã khát cân bằng vị béo của món ăn", "Zesty salted lime flavored cold Pepsi can", 22000m, false, true, 3, "https://images.unsplash.com/photo-1629203851122-3726ecdf080e?w=500"),
            ("Drinks & Fresh Tea", "BAR", "7Up Hương Chanh Lon 320ml", "7Up Lemon 320ml Can", "Nước ngọt vị chanh thanh mát giải nhiệt tức thì", "Crisp refreshing lemon-lime flavored 7Up soda can", 22000m, false, true, 4, "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Mirinda Hương Cam Lon 320ml", "Mirinda Orange 320ml Can", "Vị cam ngọt ngào có ga hấp dẫn cả người lớn và trẻ em", "Fruity sparkling orange flavored soda can", 22000m, false, true, 5, "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Mirinda Soda Kem Lon 320ml", "Mirinda Cream Soda 320ml Can", "Soda kem ngọt dịu thơm thơm ngậy ngậy đã khát", "Sweet smooth vanilla cream flavored soda can", 22000m, false, true, 6, "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Nước Suối Aquafina 500ml", "Aquafina Water 500ml", "Nước khoáng tinh khiết đóng chai mát lạnh", "Chilled pure bottled water 500ml", 15000m, false, true, 7, "https://images.unsplash.com/photo-1559839914-ba2ac55d9985?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Chai Pepsi Lớn 1.5L", "Pepsi 1.5L Bottle", "Chai lớn chia sẻ cùng bạn bè và gia đình", "Big sharing 1.5L bottle of classic Pepsi", 39000m, false, true, 8, "https://images.unsplash.com/photo-1629203851122-3726ecdf080e?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Chai 7Up Lớn 1.5L", "7Up 1.5L Bottle", "Chai 7Up 1.5L cho bàn tiệc thêm sảng khoái", "Big sharing 1.5L bottle of refreshing 7Up", 39000m, false, true, 9, "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Chai Mirinda Cam 1.5L", "Mirinda Orange 1.5L Bottle", "Chai Mirinda Cam 1.5L thơm ngọt bùng nổ hương vị", "Big sharing 1.5L bottle of fizzy orange Mirinda", 39000m, false, true, 10, "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Trà Đào Cam Sả Tươi", "Fresh Peach Orange Lemongrass Tea", "Trà đen thanh mát kết hợp lát đào giòn & hương sả", "Refreshing black tea brewed with fresh peach, orange & lemongrass", 39000m, false, true, 11, "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Trà Vải Hạt É Thanh Mát", "Lychee Basil Seed Iced Tea", "Vị ngọt dịu từ quả vải tươi cùng hạt é mát ruột", "Iced black tea blended with sweet lychee fruit and basil seeds", 39000m, false, true, 12, "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Trà Sữa Trà Đen Trân Châu", "Classic Black Milk Tea with Pearls", "Trà đen đậm đà pha sữa tươi béo ngậy kèm trân châu dẻo", "Bold black milk tea loaded with chewy tapioca boba pearls", 45000m, false, true, 13, "https://images.unsplash.com/photo-1558857563-b37cf5a349bc?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Trà Sữa Matcha Nhật Bản", "Japanese Matcha Milk Tea", "Bột matcha nguyên chất từ Nhật Bản hòa quyện sữa tươi", "Pure stone-ground Japanese green tea matcha with fresh milk", 49000m, false, true, 14, "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Soda Ý Hương Đào Tươi", "Italian Peach Sparkler Soda", "Soda có ga hòa siro đào thơm mát và lát chanh tươi", "Sparkling soda with fragrant peach syrup and fresh mint", 35000m, false, true, 15, "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=500"),
            ("Drinks & Fresh Tea", "BAR", "Soda Chanh Tươi Bạc Hà", "Lemon Mint Mojito Soda", "Chanh tươi ép lấy nước, lá bạc hà giã nhuyễn và soda sảng khoái", "Virgin mojito with freshly squeezed lemon, mint and fizzy soda", 35000m, false, true, 16, "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=500"),
            // Inactive drink item
            ("Drinks & Fresh Tea", "BAR", "Rượu Vang Ý Chianti Thượng Hạng", "Chianti Classico Italian Wine", "Vang đỏ Ý hảo hạng ủ thùng gỗ sồi (Tạm ngưng phục vụ)", "Premium Italian oak-aged Chianti red wine (Inactive)", 390000m, false, false, 17, ""),

            // --- BÁNH & TRÁNG MIỆNG (6) ---
            ("Desserts & Ice Cream", "DESSERT", "Bánh Chocolate Lava Tan Chảy", "Warm Chocolate Lava Cake", "Bánh nướng nóng hổi với lõi sô-cô-la đen tan chảy ngọt ngào", "Warm baked cake with gooey molten dark chocolate center", 49000m, false, true, 1, "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=500"),
            ("Desserts & Ice Cream", "DESSERT", "Bánh Brownie Hạt Óc Chó", "Walnut Fudge Brownie", "Bánh brownie dẻo bùi vị hạt óc chó và sô-cô-la đậm đà", "Rich fudgy chocolate brownie packed with crunchy walnuts", 45000m, false, true, 2, "https://images.unsplash.com/photo-1589218436045-ee320057f443?w=500"),
            ("Desserts & Ice Cream", "DESSERT", "Bánh Cheesecake New York", "Classic New York Cheesecake", "Bánh phô mai nướng mịn màng béo ngậy đế bánh quy giòn", "Velvety baked cream cheesecake with buttery graham crust", 55000m, false, true, 3, "https://images.unsplash.com/photo-1533134242443-d4fd215305ad?w=500"),
            ("Desserts & Ice Cream", "DESSERT", "Bánh Táo Nướng Quế Apple Pie", "Cinnamon Baked Apple Pie", "Bánh táo nướng vỏ ngàn lớp nhân táo quế chua ngọt ấm áp", "Flaky puff pastry stuffed with spiced cinnamon glazed apples", 49000m, false, true, 4, "https://images.unsplash.com/photo-1568571780765-9276ac8b75a2?w=500"),
            ("Desserts & Ice Cream", "DESSERT", "Kem Vanilla Hạt Dẻ Thơm Bùi", "Vanilla Bean Ice Cream", "Viên kem vani nguyên chất mềm mượt rắc hạnh nhân nướng", "Creamy vanilla bean ice cream scoop with toasted almonds", 29000m, false, true, 5, "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=500"),
            ("Desserts & Ice Cream", "DESSERT", "Kem Chocolate Bỉ Đậm Vị", "Belgian Chocolate Ice Cream", "Viên kem sô-cô-la Bỉ đắng nhẹ ngọt hậu say đắm", "Rich Belgian dark chocolate ice cream scoop", 29000m, false, true, 6, "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=500"),

            // --- THỰC ĐƠN TRẺ EM (5) ---
            ("Kids Menu", "PIZZA", "Kids Mini Pizza Xúc Xích Phô Mai", "Kids Mini Sausage Pizza", "Pizza cỡ nhỏ 15cm phô mai thơm và xúc xích dễ ăn cho bé", "Cute 15cm small crust pizza with mild sausage and mild cheese", 59000m, false, true, 1, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500"),
            ("Kids Menu", "PASTA", "Kids Mì Ý Bò Băm Cỡ Nhỏ", "Kids Spaghetti Bolognese", "Mì Ý sợi ngắn sốt bò băm cà chua ngọt dịu cho bé", "Kid-sized spaghetti with sweet mild beef bolognese sauce", 55000m, false, true, 2, "https://images.unsplash.com/photo-1551183053-bf91a1d81141?w=500"),
            ("Kids Menu", "FRYER", "Kids Gà Nugget Hình Khủng Long", "Kids Dino Chicken Nuggets (5 pcs)", "5 miếng gà viên tạo hình thú ngộ nghĩnh chiên vàng giòn", "5 dinosaur shaped crispy chicken nuggets that kids love", 49000m, false, true, 3, "https://images.unsplash.com/photo-1562967914-608f82629710?w=500"),
            ("Kids Menu", "FRYER", "Kids Đùi Gà Giòn Sữa Dinh Dưỡng", "Kids Crispy Milk Drumstick", "1 đùi gà chiên giòn không cay tẩm bột sữa thơm dịu", "1 mild sweet milk breaded non-spicy crispy chicken drumstick", 45000m, false, true, 4, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Kids Menu", "PASTA", "Kids Cơm Nắm Phô Mai Trứng", "Kids Cheesy Rice Balls (3 pcs)", "3 viên cơm nắm trộn trứng và phô mai nướng thơm ngon", "3 adorable soft rice balls stuffed with scrambled egg and cheese", 49000m, false, true, 5, "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500"),

            // --- COMBOS (10 COMBOS) ---
            ("Value Combos", "PIZZA", "Combo Gia Đình Pizza & Pasta", "Family Pizza & Pasta Combo", "1 Pizza tùy chọn + 1 Mì Ý tùy chọn + 1 Khoai tây chiên + 1 Nước ngọt", "1 Choice Pizza + 1 Choice Pasta + 1 Fries + 1 Choice Drink", 349000m, true, true, 1, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500"),
            ("Value Combos", "PIZZA", "Combo Couple Hẹn Hò 2 Người", "Couple Romance Pizza Combo", "1 Pizza tùy chọn + 1 Món ăn kèm + 2 Đồ uống tự chọn", "1 Choice Pizza + 1 Choice Side + 2 Choice Drinks", 249000m, true, true, 2, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500"),
            ("Value Combos", "PIZZA", "Combo Party Tụ Họp 4-6 Người", "Party Gathering Feast Combo", "2 Pizza tùy chọn + 1 Phần gà + 1 Món ăn kèm + 4 Đồ uống", "2 Choice Pizzas + 1 Chicken + 1 Side + 4 Drinks", 549000m, true, true, 3, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500"),
            ("Value Combos", "PIZZA", "Combo Cá Nhân My Box", "My Box Solo Value Set", "1 Pizza mini tùy chọn + 1 Món ăn kèm + 1 Nước ngọt", "1 Choice Pizza + 1 Choice Side + 1 Choice Drink", 129000m, true, true, 4, "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500"),
            ("Value Combos", "PIZZA", "Combo Đại Tiệc Family Feast", "Grand Family Feast Combo", "2 Pizza tùy chọn + 1 Mì Ý + 1 Gà chiên + 1 Món kèm + 4 Nước", "2 Pizzas + 1 Pasta + 1 Chicken + 1 Side + 4 Drinks", 629000m, true, true, 5, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500"),
            ("Value Combos", "FRYER", "Combo Gà Giòn Chicken Lover", "Chicken Lover Value Combo", "1 Phần gà sốt tùy chọn + 1 Khoai tây lắc + 2 Nước ngọt", "1 Choice Chicken + 1 Choice Side + 2 Choice Drinks", 229000m, true, true, 6, "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=500"),
            ("Value Combos", "PIZZA", "Combo Hải Sản Seafood Set", "Premium Seafood Set Combo", "1 Pizza hải sản tùy chọn + 1 Salad tươi + 2 Nước ngọt", "1 Choice Seafood Pizza + 1 Choice Salad + 2 Choice Drinks", 369000m, true, true, 7, "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=500"),
            ("Value Combos", "PIZZA", "Combo Trẻ Em Kids Meal", "Kids Fun Meal Combo", "1 Món chính trẻ em + 1 Khoai chiên + 1 Đồ uống + 1 Kem ngọt", "1 Kids Main + 1 Fries + 1 Drink + 1 Ice Cream", 109000m, true, true, 8, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500"),
            ("Value Combos", "PIZZA", "Combo Nâng Cấp Pizza Upgrade", "Pizza Upgrade Master Combo", "1 Pizza tùy chọn (có phụ thu) + 1 Món ăn kèm + 1 Nước ngọt", "1 Pizza + 1 Side + 1 Drink with Upgrades", 299000m, true, true, 9, "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500"),
            ("Value Combos", "PIZZA", "Combo Tự Chọn Build Your Box", "Build Your Own Box Combo", "Tự do chọn 1 Pizza + 1 Gà giòn + 1 Món ăn kèm + 1 Đồ uống", "1 Choice Pizza + 1 Chicken + 1 Side + 1 Drink", 179000m, true, true, 10, "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500")
        };

        var itemMap = new Dictionary<string, MenuItem>();
        foreach (var it in itemsData)
        {
            var cat = catMap.ContainsKey(it.CatName) ? catMap[it.CatName] : catMap.Values.First();
            var st = stationMap.ContainsKey(it.StationCode) ? stationMap[it.StationCode] : stationMap.Values.First();

            var existing = await context.MenuItems.FirstOrDefaultAsync(m => m.Name == it.Name || m.NameEn == it.NameEn);
            if (existing == null)
            {
                existing = new MenuItem
                {
                    CategoryId = cat.Id,
                    KitchenStationId = st.Id,
                    Name = it.Name,
                    NameEn = it.NameEn,
                    Description = it.Desc,
                    DescriptionEn = it.DescEn,
                    Price = it.Price,
                    IsCombo = it.IsCombo,
                    IsActive = it.IsActive,
                    SortOrder = it.Sort,
                    ImageUrl = string.IsNullOrEmpty(it.Img) ? null : it.Img
                };
                await context.MenuItems.AddAsync(existing);
                await context.SaveChangesAsync();
            }
            else
            {
                existing.CategoryId = cat.Id;
                existing.KitchenStationId = st.Id;
                existing.Price = it.Price;
                existing.IsCombo = it.IsCombo;
                existing.IsActive = it.IsActive;
                if (!string.IsNullOrEmpty(it.Img) && string.IsNullOrEmpty(existing.ImageUrl))
                {
                    existing.ImageUrl = it.Img;
                }
            }
            itemMap[it.Name] = existing;
            itemMap[it.NameEn] = existing;
        }
        await context.SaveChangesAsync();

        // ============================================================
        // 7. ATTACH MODIFIER GROUPS TO MENU ITEMS
        // ============================================================
        // Attach Pizza modifiers (Size, Crust, Sauce, Cheese, Topping, Spice) to all Pizza items
        var pizzaItems = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Pizza") && !m.IsCombo).ToListAsync();
        var pizzaModGroupCodes = new[] { "SIZE", "CRUST", "SAUCE", "CHEESE", "TOPPING", "SPICE" };
        foreach (var p in pizzaItems)
        {
            int order = 1;
            foreach (var code in pizzaModGroupCodes)
            {
                if (modGroupMap.TryGetValue(code, out var mg))
                {
                    var exists = await context.MenuItemModifierGroups.AnyAsync(mmg => mmg.MenuItemId == p.Id && mmg.ModifierGroupId == mg.Id);
                    if (!exists)
                    {
                        await context.MenuItemModifierGroups.AddAsync(new MenuItemModifierGroup
                        {
                            MenuItemId = p.Id,
                            ModifierGroupId = mg.Id,
                            SortOrder = order++
                        });
                    }
                }
            }
        }

        // Attach Salad dressings to all Salad items
        var saladItems = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Salad")).ToListAsync();
        if (modGroupMap.TryGetValue("SALAD_DRESSING", out var saladMg))
        {
            foreach (var s in saladItems)
            {
                var exists = await context.MenuItemModifierGroups.AnyAsync(mmg => mmg.MenuItemId == s.Id && mmg.ModifierGroupId == saladMg.Id);
                if (!exists)
                {
                    await context.MenuItemModifierGroups.AddAsync(new MenuItemModifierGroup
                    {
                        MenuItemId = s.Id,
                        ModifierGroupId = saladMg.Id,
                        SortOrder = 1
                    });
                }
            }
        }

        // Attach Drink Sugar, Ice, Toppings to freshly made drinks (Tea, Soda, Milk Tea)
        var freshDrinkItems = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Drink") && (m.Name.Contains("Trà") || m.Name.Contains("Soda"))).ToListAsync();
        var drinkModGroupCodes = new[] { "DRINK_SUGAR", "DRINK_ICE", "DRINK_TOPPING" };
        foreach (var d in freshDrinkItems)
        {
            int order = 1;
            foreach (var code in drinkModGroupCodes)
            {
                if (modGroupMap.TryGetValue(code, out var mg))
                {
                    var exists = await context.MenuItemModifierGroups.AnyAsync(mmg => mmg.MenuItemId == d.Id && mmg.ModifierGroupId == mg.Id);
                    if (!exists)
                    {
                        await context.MenuItemModifierGroups.AddAsync(new MenuItemModifierGroup
                        {
                            MenuItemId = d.Id,
                            ModifierGroupId = mg.Id,
                            SortOrder = order++
                        });
                    }
                }
            }
        }
        await context.SaveChangesAsync();

        // ============================================================
        // 8. COMBOS CONFIGURATION (10 Complete Combos with Groups & Items)
        // ============================================================
        // Helper lists for combo choices
        var allPizzas = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Pizza") && !m.IsCombo && m.IsActive).ToListAsync();
        var allPastas = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Pasta") && m.IsActive).ToListAsync();
        var allChickens = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Chicken") && m.IsActive).ToListAsync();
        var allSides = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && (m.Category.NameEn.Contains("Sides") || m.Category.NameEn.Contains("Appetizers")) && m.IsActive).ToListAsync();
        var allSalads = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Salad") && m.IsActive).ToListAsync();
        var allDrinks = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Drink") && m.IsActive && !m.Name.Contains("Chai")).ToListAsync();
        var allDesserts = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Dessert") && m.IsActive).ToListAsync();
        var allKidsMains = await context.MenuItems.Where(m => m.Category != null && m.Category.NameEn != null && m.Category.NameEn.Contains("Kids") && m.IsActive).ToListAsync();

        var friesItem = allSides.FirstOrDefault(s => s.Name.Contains("Khoai Tây Chiên Cắt Thẳng")) ?? allSides.First();

        // --- COMBO 1: Combo Gia Đình (Fixed: Khoai Tây Chiên; Choice: Pizza, Pasta, Drink) ---
        await SeedSingleComboAsync(context, itemMap["Combo Gia Đình Pizza & Pasta"],
            fixedItems: new List<(MenuItem Item, int Qty, decimal AddPrice)>
            {
                (friesItem, 1, 0m)
            },
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Món Pizza", 1, 1, true, allPizzas.Take(8).Select(p => (p, p.Price > 200000m ? 30000m : 0m)).ToList()),
                ("Chọn 1 Món Mì Ý", 1, 1, true, allPastas.Take(6).Select(p => (p, p.Price > 130000m ? 20000m : 0m)).ToList()),
                ("Chọn 1 Thức Uống", 1, 1, true, allDrinks.Take(6).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 2: Combo Couple (Pizza 1, Side 1, Drink 2) ---
        await SeedSingleComboAsync(context, itemMap["Combo Couple Hẹn Hò 2 Người"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Pizza", 1, 1, true, allPizzas.Take(6).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Món Ăn Kèm", 1, 1, true, allSides.Take(5).Select(s => (s, 0m)).ToList()),
                ("Chọn 2 Thức Uống", 2, 2, true, allDrinks.Take(6).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 3: Combo Party (Pizza 2, Chicken 1, Side 1, Drink 4) ---
        await SeedSingleComboAsync(context, itemMap["Combo Party Tụ Họp 4-6 Người"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 2 Pizza", 2, 2, true, allPizzas.Take(8).Select(p => (p, p.Price > 210000m ? 25000m : 0m)).ToList()),
                ("Chọn 1 Phần Gà", 1, 1, true, allChickens.Where(c => c.Name.Contains("Cánh Gà")).Take(4).Select(c => (c, 0m)).ToList()),
                ("Chọn 1 Món Ăn Kèm", 1, 1, true, allSides.Take(6).Select(s => (s, 0m)).ToList()),
                ("Chọn 4 Thức Uống", 4, 4, true, allDrinks.Take(6).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 4: Combo Cá Nhân My Box (Pizza 1, Side 1, Drink 1) ---
        await SeedSingleComboAsync(context, itemMap["Combo Cá Nhân My Box"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Pizza", 1, 1, true, allPizzas.Take(5).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Món Kèm", 1, 1, true, allSides.Take(4).Select(s => (s, 0m)).ToList()),
                ("Chọn 1 Nước Uống", 1, 1, true, allDrinks.Take(5).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 5: Combo Đại Tiệc Family Feast (Pizza 2, Pasta 1, Chicken 1, Side 1, Drink 4) ---
        await SeedSingleComboAsync(context, itemMap["Combo Đại Tiệc Family Feast"],
            fixedItems: new List<(MenuItem Item, int Qty, decimal AddPrice)>
            {
                (friesItem, 2, 0m)
            },
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 2 Pizza", 2, 2, true, allPizzas.Take(10).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Mì Ý", 1, 1, true, allPastas.Take(5).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Món Gà", 1, 1, true, allChickens.Take(5).Select(c => (c, 0m)).ToList()),
                ("Chọn 4 Đồ Uống", 4, 4, true, allDrinks.Take(6).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 6: Combo Gà Giòn Chicken Lover (Chicken 1, Side 1, Drink 2) ---
        await SeedSingleComboAsync(context, itemMap["Combo Gà Giòn Chicken Lover"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Phần Gà Giòn", 1, 1, true, allChickens.Where(c => c.Name.Contains("Cánh") || c.Name.Contains("Đùi")).Take(6).Select(c => (c, 0m)).ToList()),
                ("Chọn 1 Món Khoai / Bánh Mì", 1, 1, true, allSides.Take(4).Select(s => (s, 0m)).ToList()),
                ("Chọn 2 Nước Uống", 2, 2, true, allDrinks.Take(5).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 7: Combo Hải Sản Seafood Set (Seafood Pizza 1, Salad 1, Drink 2) ---
        await SeedSingleComboAsync(context, itemMap["Combo Hải Sản Seafood Set"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Pizza Hải Sản", 1, 1, true, allPizzas.Where(p => p.Name.Contains("Hải Sản") || p.Name.Contains("Tôm")).Take(4).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Món Salad Tươi", 1, 1, true, allSalads.Take(4).Select(s => (s, 0m)).ToList()),
                ("Chọn 2 Thức Uống", 2, 2, true, allDrinks.Take(5).Select(d => (d, 0m)).ToList())
            });

        // --- COMBO 8: Combo Trẻ Em Kids Meal (Kids Main 1, Fries 1, Drink 1, Dessert 1) ---
        await SeedSingleComboAsync(context, itemMap["Combo Trẻ Em Kids Meal"],
            fixedItems: new List<(MenuItem Item, int Qty, decimal AddPrice)>
            {
                (friesItem, 1, 0m)
            },
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Món Chính Trẻ Em", 1, 1, true, allKidsMains.Take(4).Select(k => (k, 0m)).ToList()),
                ("Chọn 1 Đồ Uống", 1, 1, true, allDrinks.Take(4).Select(d => (d, 0m)).ToList()),
                ("Chọn 1 Món Tráng Miệng", 1, 1, true, allDesserts.Take(3).Select(ds => (ds, 0m)).ToList())
            });

        // --- COMBO 9: MASTER TEST COMBO (Pizza Upgrade Combo: Fixed Item + Pizza Choice with Upgrades + Side + Drink) ---
        await SeedSingleComboAsync(context, itemMap["Combo Nâng Cấp Pizza Upgrade"],
            fixedItems: new List<(MenuItem Item, int Qty, decimal AddPrice)>
            {
                (friesItem, 1, 0m)
            },
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Pizza Thượng Hạng", 1, 1, true, allPizzas.Take(8).Select(p => (p, p.Price > 210000m ? 35000m : 0m)).ToList()),
                ("Chọn 1 Món Ăn Kèm Nâng Cấp", 1, 1, true, allSides.Take(5).Select(s => (s, s.Price > 50000m ? 15000m : 0m)).ToList()),
                ("Chọn 1 Đồ Uống Yêu Thích", 1, 1, true, allDrinks.Take(6).Select(d => (d, d.Name.Contains("Trà Sữa") ? 10000m : 0m)).ToList())
            });

        // --- COMBO 10: Build Your Box (Pizza 1, Chicken 1, Side 1, Drink 1) ---
        await SeedSingleComboAsync(context, itemMap["Combo Tự Chọn Build Your Box"],
            fixedItems: null,
            groups: new List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)>
            {
                ("Chọn 1 Pizza", 1, 1, true, allPizzas.Take(6).Select(p => (p, 0m)).ToList()),
                ("Chọn 1 Phần Gà", 1, 1, true, allChickens.Take(4).Select(c => (c, 0m)).ToList()),
                ("Chọn 1 Món Ăn Kèm", 1, 1, true, allSides.Take(4).Select(s => (s, 0m)).ToList()),
                ("Chọn 1 Nước Uống", 1, 1, true, allDrinks.Take(5).Select(d => (d, 0m)).ToList())
            });

        // ============================================================
        // 9. BOM & RECIPES (>= 30 Menu Items with Recipe & RecipeItems)
        // ============================================================
        var bomDefinitions = new List<(string ItemName, List<(string MatName, string Uom, decimal Qty)> Ingredients)>
        {
            // 1. Pizza Margherita
            ("Pizza Margherita Mozzarella", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.12m),
                ("Lá Basil tươi", "g", 5m),
                ("Dầu Olive nguyên chất", "ml", 10m)
            }),
            // 2. Pizza Pepperoni
            ("Pizza Pepperoni Xúc Xích Ý", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.11m),
                ("Xúc xích Ý Pepperoni", "Kg", 0.06m)
            }),
            // 3. Pizza Hawaiian
            ("Pizza Hawaiian Dứa & Giăm Bông", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.11m),
                ("Giăm bông Ham", "Kg", 0.05m),
                ("Dứa tươi gọt vỏ", "Kg", 0.05m)
            }),
            // 4. Pizza Thập Cẩm Supreme
            ("Pizza Thập Cẩm Supreme", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.10m),
                ("Thịt bò băm đút lò", "Kg", 0.04m),
                ("Thịt xông khói Bacon", "Kg", 0.03m),
                ("Nấm mỡ tươi", "Kg", 0.03m),
                ("Ớt chuông xanh", "Kg", 0.03m)
            }),
            // 5. Pizza Bò Nướng BBQ
            ("Pizza Bò Nướng BBQ", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt nướng BBQ khói", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.10m),
                ("Thịt bò nướng BBQ", "Kg", 0.08m),
                ("Hành tây trắng", "Kg", 0.03m)
            }),
            // 6. Pizza Hải Sản Nhiệt Đới
            ("Pizza Hải Sản Nhiệt Đới", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.10m),
                ("Tôm sú nõn đông lạnh", "Kg", 0.05m),
                ("Mực ống cắt khoanh", "Kg", 0.04m),
                ("Thanh cua Surimi", "Kg", 0.03m),
                ("Dứa tươi gọt vỏ", "Kg", 0.04m)
            }),
            // 7. Pizza Bốn Phô Mai
            ("Pizza Bốn Phô Mai Quattro Formaggi", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Phô mai Mozzarella sợi", "Kg", 0.08m),
                ("Phô mai Cheddar lát", "Kg", 0.04m),
                ("Phô mai Cream Cheese", "Kg", 0.03m),
                ("Phô mai bột Parmesan", "g", 15m)
            }),
            // 8. Pizza Tôm Bơ Tỏi
            ("Pizza Tôm Bơ Tỏi Thượng Hạng", new()
            {
                ("Đế Pizza Vừa 25cm", "Pcs", 1m),
                ("Sốt bơ tỏi Garlic Butter", "L", 0.08m),
                ("Phô mai Mozzarella sợi", "Kg", 0.10m),
                ("Tôm sú nõn đông lạnh", "Kg", 0.08m),
                ("Cà chua bi Cherry", "Kg", 0.03m)
            }),
            // 9. Melts Bò BBQ
            ("Melts Bò BBQ Phô Mai", new()
            {
                ("Đế Pizza Nhỏ 20cm", "Pcs", 1m),
                ("Sốt nướng BBQ khói", "L", 0.04m),
                ("Phô mai Mozzarella sợi", "Kg", 0.06m),
                ("Thịt bò nướng BBQ", "Kg", 0.05m)
            }),
            // 10. Melts Pepperoni
            ("Melts Pepperoni Giòn Rụm", new()
            {
                ("Đế Pizza Nhỏ 20cm", "Pcs", 1m),
                ("Sốt cà chua San Marzano", "L", 0.04m),
                ("Phô mai Mozzarella sợi", "Kg", 0.06m),
                ("Xúc xích Ý Pepperoni", "Kg", 0.04m)
            }),
            // 11. Mì Ý Bolognese
            ("Mì Ý Bolognese Bò Băm", new()
            {
                ("Mì Spaghetti Ý khô", "Kg", 0.10m),
                ("Thịt bò băm đút lò", "Kg", 0.07m),
                ("Sốt cà chua San Marzano", "L", 0.12m),
                ("Phô mai bột Parmesan", "g", 10m)
            }),
            // 12. Mì Ý Carbonara
            ("Mì Ý Carbonara Kem Thịt Khói", new()
            {
                ("Mì Spaghetti Ý khô", "Kg", 0.10m),
                ("Thịt xông khói Bacon", "Kg", 0.05m),
                ("Sốt kem béo Cooking Cream", "L", 0.08m),
                ("Phô mai bột Parmesan", "g", 15m)
            }),
            // 13. Mì Ý Gà Nấm
            ("Mì Ý Gà Nấm Sốt Kem Tươi", new()
            {
                ("Mì Spaghetti Ý khô", "Kg", 0.10m),
                ("Ức gà phi lê tươi", "Kg", 0.06m),
                ("Nấm mỡ tươi", "Kg", 0.04m),
                ("Sốt kem béo Cooking Cream", "L", 0.08m)
            }),
            // 14. Mì Ý Hải Sản
            ("Mì Ý Hải Sản Sốt Cà Chua", new()
            {
                ("Mì Spaghetti Ý khô", "Kg", 0.10m),
                ("Tôm sú nõn đông lạnh", "Kg", 0.04m),
                ("Mực ống cắt khoanh", "Kg", 0.04m),
                ("Sốt cà chua San Marzano", "L", 0.12m)
            }),
            // 15. Lasagna Bò Băm
            ("Lasagna Bò Băm Đút Lò", new()
            {
                ("Lá mì Lasagna Ý", "Kg", 0.08m),
                ("Thịt bò băm đút lò", "Kg", 0.08m),
                ("Sốt cà chua San Marzano", "L", 0.10m),
                ("Phô mai Mozzarella sợi", "Kg", 0.05m),
                ("Sốt kem béo Cooking Cream", "L", 0.05m)
            }),
            // 16. Khoai Tây Chiên
            ("Khoai Tây Chiên Cắt Thẳng", new()
            {
                ("Khoai tây cắt thẳng đông lạnh", "Kg", 0.15m),
                ("Dầu chiên chuyên dụng", "L", 0.02m),
                ("Muối tinh khiết", "g", 3m)
            }),
            // 17. Khoai Tây Lắc Phô Mai
            ("Khoai Tây Lắc Phô Mai", new()
            {
                ("Khoai tây cắt thẳng đông lạnh", "Kg", 0.15m),
                ("Dầu chiên chuyên dụng", "L", 0.02m),
                ("Phô mai bột Parmesan", "g", 15m)
            }),
            // 18. Bánh Mì Bơ Tỏi
            ("Bánh Mì Bơ Tỏi Nướng Giòn", new()
            {
                ("Sốt bơ tỏi Garlic Butter", "L", 0.04m),
                ("Lá Basil tươi", "g", 2m)
            }),
            // 19. Đùi Gà Giòn
            ("Đùi Gà Giòn Truyền Thống (1 cái)", new()
            {
                ("Đùi gà góc tư", "Kg", 0.18m),
                ("Dầu chiên chuyên dụng", "L", 0.03m),
                ("Muối tinh khiết", "g", 2m)
            }),
            // 20. Đùi Gà BBQ
            ("Đùi Gà Sốt BBQ Nướng Đậm Đà", new()
            {
                ("Đùi gà góc tư", "Kg", 0.18m),
                ("Sốt nướng BBQ khói", "L", 0.04m)
            }),
            // 21. Cánh Gà BBQ (4 miếng)
            ("Cánh Gà Sốt BBQ (4 miếng)", new()
            {
                ("Cánh gà tươi", "Kg", 0.22m),
                ("Sốt nướng BBQ khói", "L", 0.05m)
            }),
            // 22. Cánh Gà Cay Hàn Quốc (4 miếng)
            ("Cánh Gà Cay Hàn Quốc (4 miếng)", new()
            {
                ("Cánh gà tươi", "Kg", 0.22m),
                ("Sốt cay Gochujang", "L", 0.05m)
            }),
            // 23. Gà Nugget
            ("Gà Nugget Chiên Giòn (6 miếng)", new()
            {
                ("Ức gà phi lê tươi", "Kg", 0.12m),
                ("Dầu chiên chuyên dụng", "L", 0.02m)
            }),
            // 24. Salad Caesar Gà
            ("Salad Caesar Gà Nướng Tiêu Đen", new()
            {
                ("Xà lách Romaine", "Kg", 0.10m),
                ("Ức gà phi lê tươi", "Kg", 0.06m),
                ("Sốt Caesar Dressing", "L", 0.04m),
                ("Phô mai bột Parmesan", "g", 10m)
            }),
            // 25. Salad Rau Củ
            ("Salad Rau Củ Thanh Mát Vinaigrette", new()
            {
                ("Xà lách Romaine", "Kg", 0.10m),
                ("Cà chua bi Cherry", "Kg", 0.03m),
                ("Dưa leo sạch", "Kg", 0.03m),
                ("Dầu giấm Ý Vinaigrette", "L", 0.03m)
            }),
            // 26. Cơm Bò Nướng Đút Lò
            ("Cơm Bò Nướng Đút Lò Phô Mai", new()
            {
                ("Gạo thơm Jasmine", "Kg", 0.08m),
                ("Thịt bò nướng BBQ", "Kg", 0.06m),
                ("Phô mai Mozzarella sợi", "Kg", 0.05m),
                ("Sốt nướng BBQ khói", "L", 0.03m)
            }),
            // 27. Cơm Gà BBQ
            ("Cơm Gà BBQ Đút Lò Phô Mai", new()
            {
                ("Gạo thơm Jasmine", "Kg", 0.08m),
                ("Ức gà phi lê tươi", "Kg", 0.06m),
                ("Phô mai Mozzarella sợi", "Kg", 0.05m),
                ("Sốt nướng BBQ khói", "L", 0.03m)
            }),
            // 28. Trà Đào Cam Sả
            ("Trà Đào Cam Sả Tươi", new()
            {
                ("Cốt trà đen hảo hạng", "g", 5m),
                ("Siro Đào đóng chai", "L", 0.03m),
                ("Đường tinh luyện", "g", 15m)
            }),
            // 29. Trà Vải Hạt É
            ("Trà Vải Hạt É Thanh Mát", new()
            {
                ("Cốt trà đen hảo hạng", "g", 5m),
                ("Siro Vải thơm", "L", 0.03m),
                ("Đường tinh luyện", "g", 15m)
            }),
            // 30. Trà Sữa Trà Đen
            ("Trà Sữa Trà Đen Trân Châu", new()
            {
                ("Cốt trà đen hảo hạng", "g", 8m),
                ("Sữa tươi tiệt trùng", "L", 0.08m),
                ("Đường tinh luyện", "g", 20m)
            }),
            // 31. Pepsi Lon
            ("Pepsi Lon 320ml", new()
            {
                ("Lon Pepsi 320ml", "Lon", 1m)
            }),
            // 32. 7Up Lon
            ("7Up Hương Chanh Lon 320ml", new()
            {
                ("Lon 7Up 320ml", "Lon", 1m)
            })
        };

        foreach (var def in bomDefinitions)
        {
            if (!itemMap.TryGetValue(def.ItemName, out var menuItem)) continue;

            var existingRecipe = await context.Recipes
                .Include(r => r.RecipeItems)
                .FirstOrDefaultAsync(r => r.MenuItemId == menuItem.Id);

            if (existingRecipe == null)
            {
                existingRecipe = new Recipe
                {
                    MenuItemId = menuItem.Id
                };
                await context.Recipes.AddAsync(existingRecipe);
                await context.SaveChangesAsync();
            }

            foreach (var ing in def.Ingredients)
            {
                if (!matMap.TryGetValue(ing.MatName, out var mat)) continue;
                var uom = uomMap.ContainsKey(ing.Uom) ? uomMap[ing.Uom] : uomMap["Kg"];

                var existingItem = existingRecipe.RecipeItems.FirstOrDefault(ri => ri.MaterialId == mat.Id);
                if (existingItem == null)
                {
                    existingItem = new RecipeItem
                    {
                        RecipeId = existingRecipe.Id,
                        MaterialId = mat.Id,
                        UnitOfMeasureId = uom.Id,
                        Quantity = ing.Qty
                    };
                    await context.RecipeItems.AddAsync(existingItem);
                }
                else
                {
                    existingItem.Quantity = ing.Qty;
                    existingItem.UnitOfMeasureId = uom.Id;
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedSingleComboAsync(
        ApplicationDbContext context,
        MenuItem comboItem,
        List<(MenuItem Item, int Qty, decimal AddPrice)>? fixedItems,
        List<(string Name, int Min, int Max, bool Req, List<(MenuItem Item, decimal AddPrice)> Choices)> groups)
    {
        // 1. Fixed Items
        if (fixedItems != null && fixedItems.Any())
        {
            int order = 1;
            foreach (var fix in fixedItems)
            {
                var existing = await context.ComboItems.FirstOrDefaultAsync(ci => ci.ComboId == comboItem.Id && ci.ComboGroupId == null && ci.MenuItemId == fix.Item.Id);
                if (existing == null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = comboItem.Id,
                        ComboGroupId = null,
                        MenuItemId = fix.Item.Id,
                        ComponentType = (int)ComboComponentType.Fixed,
                        DefaultQuantity = fix.Qty,
                        AdditionalPrice = fix.AddPrice,
                        SortOrder = order++,
                        IsActive = true
                    });
                }
                else
                {
                    existing.DefaultQuantity = fix.Qty;
                    existing.AdditionalPrice = fix.AddPrice;
                }
            }
            await context.SaveChangesAsync();
        }

        // 2. Choice Groups
        int groupOrder = 1;
        foreach (var g in groups)
        {
            var existingGroup = await context.ComboGroups
                .Include(cg => cg.ComboItems)
                .FirstOrDefaultAsync(cg => cg.ComboId == comboItem.Id && cg.Name == g.Name);

            if (existingGroup == null)
            {
                existingGroup = new ComboGroup
                {
                    ComboId = comboItem.Id,
                    Name = g.Name,
                    MinSelections = g.Min,
                    MaxSelections = g.Max,
                    IsRequired = g.Req,
                    SortOrder = groupOrder++
                };
                await context.ComboGroups.AddAsync(existingGroup);
                await context.SaveChangesAsync();
            }
            else
            {
                existingGroup.MinSelections = g.Min;
                existingGroup.MaxSelections = g.Max;
                existingGroup.IsRequired = g.Req;
                existingGroup.SortOrder = groupOrder++;
            }

            int itemOrder = 1;
            foreach (var ch in g.Choices)
            {
                var existingItem = existingGroup.ComboItems.FirstOrDefault(ci => ci.MenuItemId == ch.Item.Id);
                if (existingItem == null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = comboItem.Id,
                        ComboGroupId = existingGroup.Id,
                        MenuItemId = ch.Item.Id,
                        ComponentType = (int)ComboComponentType.Choice,
                        DefaultQuantity = 1,
                        AdditionalPrice = ch.AddPrice,
                        SortOrder = itemOrder++,
                        IsActive = true
                    });
                }
                else
                {
                    existingItem.AdditionalPrice = ch.AddPrice;
                    existingItem.SortOrder = itemOrder++;
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
