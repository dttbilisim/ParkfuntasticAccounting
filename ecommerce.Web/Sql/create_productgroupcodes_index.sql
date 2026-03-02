-- ====================================================================
-- ProductGroupCodes.GroupCode Index Oluşturma Script
-- ====================================================================
-- Bu index sync_products_from_otoismails procedure'ünün performansı için KRİTİK!
-- Procedure çalışmadan ÖNCE bu index'i oluşturun.
-- ====================================================================

-- Index oluşturma (CONCURRENTLY - tabloyu lock'lamadan)
-- NOT: CONCURRENTLY kullanıldığı için transaction dışında çalıştırılmalı
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_productgroupcodes_groupcode 
ON "ProductGroupCodes" ("GroupCode") 
WHERE "GroupCode" IS NOT NULL;

-- Index oluşturulduktan sonra ANALYZE çalıştırın
ANALYZE "ProductGroupCodes";

-- Index'in oluşturulduğunu kontrol edin
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'ProductGroupCodes' 
  AND indexname = 'idx_productgroupcodes_groupcode';
