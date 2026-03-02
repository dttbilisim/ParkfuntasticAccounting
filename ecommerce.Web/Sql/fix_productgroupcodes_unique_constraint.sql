-- ====================================================================
-- ProductGroupCodes Unique Constraint Düzeltme Script
-- ====================================================================
-- SORUN: idx_pgc_groupcode unique constraint'i aynı GroupCode için
--        farklı ProductId'lerin eklenmesini engelliyor.
-- ÇÖZÜM: Unique constraint'i kaldırıp normal index'e çeviriyoruz.
--        Aynı GroupCode için farklı ProductId'ler eklenebilir olacak.
-- ====================================================================

-- 1. Mevcut unique constraint/index'i kontrol et
DO $$
DECLARE
    constraint_exists BOOLEAN;
    index_exists BOOLEAN;
BEGIN
    -- Unique constraint var mı kontrol et
    SELECT EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'idx_pgc_groupcode'
    ) INTO constraint_exists;
    
    -- Unique index var mı kontrol et
    SELECT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE indexname = 'idx_pgc_groupcode' 
        AND indexdef LIKE '%UNIQUE%'
    ) INTO index_exists;
    
    IF constraint_exists THEN
        RAISE NOTICE '🔍 Unique constraint "idx_pgc_groupcode" bulundu. Kaldırılıyor...';
        ALTER TABLE "ProductGroupCodes" DROP CONSTRAINT IF EXISTS "idx_pgc_groupcode";
        RAISE NOTICE '✅ Unique constraint kaldırıldı.';
    ELSIF index_exists THEN
        RAISE NOTICE '🔍 Unique index "idx_pgc_groupcode" bulundu. Kaldırılıyor...';
        DROP INDEX IF EXISTS "idx_pgc_groupcode";
        RAISE NOTICE '✅ Unique index kaldırıldı.';
    ELSE
        RAISE NOTICE 'ℹ️ "idx_pgc_groupcode" unique constraint/index bulunamadı.';
    END IF;
END $$;

-- 2. Normal (non-unique) index oluştur (performans için)
-- NOT: Bu index unique değil, aynı GroupCode için birden fazla kayıt olabilir
CREATE INDEX IF NOT EXISTS idx_productgroupcodes_groupcode 
ON "ProductGroupCodes" ("GroupCode") 
WHERE "GroupCode" IS NOT NULL;

-- 3. (ProductId, GroupCode) kombinasyonu için unique constraint ekle (opsiyonel)
-- NOT: Bu, aynı ProductId için aynı GroupCode'un iki kez eklenmesini engeller
--      Ancak farklı ProductId'ler için aynı GroupCode eklenebilir
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'uq_productgroupcodes_productid_groupcode'
    ) THEN
        CREATE UNIQUE INDEX IF NOT EXISTS uq_productgroupcodes_productid_groupcode
        ON "ProductGroupCodes" ("ProductId", "GroupCode")
        WHERE "GroupCode" IS NOT NULL;
        RAISE NOTICE '✅ (ProductId, GroupCode) unique index oluşturuldu.';
    ELSE
        RAISE NOTICE 'ℹ️ (ProductId, GroupCode) unique index zaten mevcut.';
    END IF;
END $$;

-- 4. Index'leri ANALYZE et
ANALYZE "ProductGroupCodes";

-- 5. Sonuçları göster
DO $$
BEGIN
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ ProductGroupCodes unique constraint düzeltmesi tamamlandı!';
    RAISE NOTICE '   • Aynı GroupCode için farklı ProductId''ler eklenebilir';
    RAISE NOTICE '   • Aynı (ProductId, GroupCode) kombinasyonu tekrar eklenemez';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

-- 6. Index bilgilerini göster
SELECT 
    schemaname,
    tablename,
    indexname,
    CASE 
        WHEN indexdef LIKE '%UNIQUE%' THEN 'UNIQUE'
        ELSE 'NORMAL'
    END AS index_type,
    indexdef
FROM pg_indexes
WHERE tablename = 'ProductGroupCodes' 
  AND (indexname LIKE '%groupcode%' OR indexname LIKE '%productid_groupcode%')
ORDER BY indexname;
