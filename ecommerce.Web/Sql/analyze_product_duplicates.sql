-- ====================================================================
-- PRODUCT TABLOSU ANALİZ SCRIPT'İ
-- ====================================================================
-- Product tablosundaki kayıtları analiz eder ve mükerrer kayıtları tespit eder
-- ====================================================================

\echo '═══════════════════════════════════════════════════════════'
\echo '📊 PRODUCT ANALİZ BAŞLATILDI'
\echo '═══════════════════════════════════════════════════════════'
\echo ''

\echo '1️⃣ Genel İstatistikler...'
\echo ''
-- 1. Genel İstatistikler
SELECT 
    'Product' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "Product"
UNION ALL
SELECT 
    'ProductGroupCodes' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductGroupCodes"
UNION ALL
SELECT 
    'Unique GroupCode' AS "Tablo",
    COUNT(DISTINCT "GroupCode") AS "Toplam Kayıt"
FROM "ProductGroupCodes"
WHERE "GroupCode" IS NOT NULL AND "GroupCode" != ''
UNION ALL
SELECT 
    'ProductOtoIsmails' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductOtoIsmails"
UNION ALL
SELECT 
    'ProductRemars' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductRemars"
UNION ALL
SELECT 
    'ProductBasbugs' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductBasbugs"
UNION ALL
SELECT 
    'ProductDegas' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductDegas"
ORDER BY "Tablo";

\echo ''
\echo '2️⃣ ProductGroupCodes olmayan Product''lar...'
\echo ''
-- 2. ProductGroupCodes olmayan Product'lar
SELECT 
    'Product WITHOUT GroupCode' AS "Kategori",
    COUNT(*) AS "Sayı"
FROM "Product" p
LEFT JOIN "ProductGroupCodes" pg ON pg."ProductId" = p."Id"
WHERE pg."Id" IS NULL;

\echo ''
\echo '3️⃣ Aynı GroupCode''a sahip Product''lar (mükerrer tespiti)...'
\echo ''
-- 3. Aynı GroupCode'a sahip Product'lar (mükerrer tespiti)
SELECT 
    'Product WITH Duplicate GroupCode' AS "Kategori",
    COUNT(*) AS "Sayı"
FROM (
    SELECT 
        pg."GroupCode",
        COUNT(DISTINCT pg."ProductId") AS product_count
    FROM "ProductGroupCodes" pg
    WHERE pg."GroupCode" IS NOT NULL 
      AND pg."GroupCode" != ''
    GROUP BY pg."GroupCode"
    HAVING COUNT(DISTINCT pg."ProductId") > 1
) duplicates;

\echo ''
\echo '4️⃣ Detaylı mükerrer analizi (ilk 20)...'
\echo ''
-- 4. Detaylı mükerrer analizi (ilk 20)
SELECT 
    pg."GroupCode",
    COUNT(DISTINCT pg."ProductId") AS "Product Sayısı",
    STRING_AGG(DISTINCT p."Name", ' | ' ORDER BY p."Name") AS "Örnek Ürün İsimleri",
    STRING_AGG(DISTINCT p."Id"::text, ', ' ORDER BY p."Id"::text) AS "Product ID'leri"
FROM "ProductGroupCodes" pg
JOIN "Product" p ON p."Id" = pg."ProductId"
WHERE pg."GroupCode" IS NOT NULL 
  AND pg."GroupCode" != ''
GROUP BY pg."GroupCode"
HAVING COUNT(DISTINCT pg."ProductId") > 1
ORDER BY COUNT(DISTINCT pg."ProductId") DESC
LIMIT 20;

\echo ''
\echo '5️⃣ SellerId bazlı Product sayıları...'
\echo ''
-- 5. SellerId bazlı Product sayıları
SELECT 
    COALESCE(p."SellerId", 0) AS "SellerId",
    COUNT(*) AS "Product Sayısı"
FROM "Product" p
GROUP BY p."SellerId"
ORDER BY COUNT(*) DESC;

\echo ''
\echo '6️⃣ Aynı isim ve markaya sahip farklı Product''lar (potansiyel mükerrer)...'
\echo ''
-- 6. Aynı isim ve markaya sahip farklı Product'lar (potansiyel mükerrer)
SELECT 
    'Product WITH Same Name+Brand' AS "Kategori",
    COUNT(*) AS "Sayı"
FROM (
    SELECT 
        p."Name",
        p."BrandId",
        COUNT(*) AS product_count
    FROM "Product" p
    WHERE p."Name" IS NOT NULL 
      AND p."BrandId" IS NOT NULL
    GROUP BY p."Name", p."BrandId"
    HAVING COUNT(*) > 1
) same_name_brand;

\echo ''
\echo '7️⃣ Aynı Oems''e sahip farklı Product''lar (potansiyel mükerrer)...'
\echo ''
-- 7. Aynı Oems'e sahip farklı Product'lar (potansiyel mükerrer)
SELECT 
    'Product WITH Same Oems' AS "Kategori",
    COUNT(*) AS "Sayı"
FROM (
    SELECT 
        p."Oems",
        COUNT(*) AS product_count
    FROM "Product" p
    WHERE p."Oems" IS NOT NULL 
      AND p."Oems" != ''
    GROUP BY p."Oems"
    HAVING COUNT(*) > 1
) same_oems;

\echo ''
\echo '8️⃣ Detaylı Oems mükerrer analizi (ilk 20)...'
\echo ''
-- 8. Detaylı Oems mükerrer analizi (ilk 20)
SELECT 
    p."Oems",
    COUNT(*) AS "Product Sayısı",
    STRING_AGG(DISTINCT p."Name", ' | ' ORDER BY p."Name") AS "Örnek Ürün İsimleri",
    STRING_AGG(DISTINCT p."Id"::text, ', ' ORDER BY p."Id"::text) AS "Product ID'leri"
FROM "Product" p
WHERE p."Oems" IS NOT NULL 
  AND p."Oems" != ''
GROUP BY p."Oems"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC
LIMIT 20;

\echo ''
\echo '9️⃣ ProductGroupCodes olmayan ama Oems''i olan Product''lar...'
\echo ''
\echo '═══════════════════════════════════════════════════════════'
\echo '✅ ANALİZ TAMAMLANDI'
\echo '═══════════════════════════════════════════════════════════'
-- 9. ProductGroupCodes olmayan ama Oems'i olan Product'lar
SELECT 
    'Product WITH Oems BUT NO GroupCode' AS "Kategori",
    COUNT(*) AS "Sayı"
FROM "Product" p
LEFT JOIN "ProductGroupCodes" pg ON pg."ProductId" = p."Id"
WHERE p."Oems" IS NOT NULL 
  AND p."Oems" != ''
  AND pg."Id" IS NULL;
