-- =====================================================
-- Update Existing Records with BranchId = 1
-- Bu script mevcut tüm kayıtların BranchId değerini 1 olarak ayarlar
-- =====================================================

BEGIN;

-- 1. Products tablosunu güncelle
UPDATE "Product"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- 2. Sellers tablosunu güncelle
UPDATE "Sellers"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- 3. Discounts tablosunu güncelle
UPDATE "Discounts"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- 4. Banners tablosunu güncelle
UPDATE "Banners"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- 5. Orders tablosunu güncelle
UPDATE "Orders"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- 6. CustomerAccountTransactions tablosunu güncelle
UPDATE "CustomerAccountTransactions"
SET "BranchId" = 1
WHERE "BranchId" IS NULL;

-- Güncelleme sonuçlarını kontrol et
SELECT 
    'Product' as tablo, 
    COUNT(*) as updated_count 
FROM "Product" 
WHERE "BranchId" = 1

UNION ALL

SELECT 
    'Sellers' as tablo, 
    COUNT(*) as updated_count 
FROM "Sellers" 
WHERE "BranchId" = 1

UNION ALL

SELECT 
    'Discounts' as tablo, 
    COUNT(*) as updated_count 
FROM "Discounts" 
WHERE "BranchId" = 1

UNION ALL

SELECT 
    'Banners' as tablo, 
    COUNT(*) as updated_count 
FROM "Banners" 
WHERE "BranchId" = 1

UNION ALL

SELECT 
    'Orders' as tablo, 
    COUNT(*) as updated_count 
FROM "Orders" 
WHERE "BranchId" = 1

UNION ALL

SELECT 
    'CustomerAccountTransactions' as tablo, 
    COUNT(*) as updated_count 
FROM "CustomerAccountTransactions" 
WHERE "BranchId" = 1;

COMMIT;

-- =====================================================
-- KULLANIM:
-- 1. PostgreSQL veritabanına bağlan
-- 2. Bu scripti çalıştır: psql -d your_database -f update_existing_branchid.sql
-- 3. Sonuçları kontrol et
-- =====================================================
