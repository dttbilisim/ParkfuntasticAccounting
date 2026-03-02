-- Manuel ProductUnit Ekleme Script'i
-- Tüm mevcut Product'lar için UnitId=2 (Adet) ve UnitValue=1 ekler
-- Lock etmez, güvenli şekilde çalışır

-- Önce kaç ürünün ProductUnit'i olmadığını görelim
SELECT COUNT(DISTINCT p."Id") as "ProductsWithoutUnit"
FROM "Product" p
LEFT JOIN "ProductUnits" pu ON pu."ProductId" = p."Id" AND pu."UnitId" = 2
WHERE p."Status" != 0 -- Deleted değil
  AND pu."Id" IS NULL;

-- ProductUnit kayıtlarını ekle
INSERT INTO "ProductUnits" ("ProductId", "UnitId", "UnitValue", "Status", "CreatedId", "CreatedDate")
SELECT 
    p."Id" as "ProductId",
    2 as "UnitId",           -- 2 = Adet
    1 as "UnitValue",         -- 1 = Varsayılan dönüşüm değeri
    1 as "Status",            -- Active
    1 as "CreatedId",         -- System user
    NOW() as "CreatedDate"
FROM "Product" p
LEFT JOIN "ProductUnits" pu ON pu."ProductId" = p."Id" AND pu."UnitId" = 2
WHERE p."Status" != 0       -- Deleted olmayan ürünler
  AND pu."Id" IS NULL       -- Henüz ProductUnit kaydı olmayan
ON CONFLICT ("ProductId", "UnitId") DO NOTHING;

-- Sonuç kontrolü
SELECT COUNT(*) as "TotalProductUnits" FROM "ProductUnits" WHERE "UnitId" = 2;

-- Detaylı kontrol (isteğe bağlı)
SELECT 
    COUNT(DISTINCT p."Id") as "TotalActiveProducts",
    COUNT(DISTINCT pu."ProductId") as "ProductsWithUnit",
    COUNT(DISTINCT p."Id") - COUNT(DISTINCT pu."ProductId") as "ProductsStillMissing"
FROM "Product" p
LEFT JOIN "ProductUnits" pu ON pu."ProductId" = p."Id" AND pu."UnitId" = 2
WHERE p."Status" != 0;
