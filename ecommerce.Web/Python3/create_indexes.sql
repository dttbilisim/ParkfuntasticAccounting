-- SellerProduct Indexing için Performans Optimizasyonu
-- Mevcut index'leri kontrol ettikten sonra sadece eksikleri oluştur

-- NOT: Aşağıdaki index'ler ZATEN MEVCUT (tekrar oluşturmaya gerek yok):
-- ✅ idx_selleritems_modifieddate (1.7 MB)
-- ✅ idx_selleritems_productid_stock (7.8 MB)
-- ✅ idx_selleritems_id_stock (7.8 MB)
-- ✅ idx_product_modifieddate (7 MB)
-- ✅ idx_datdatas_composite (28 MB)
-- ✅ idx_productgroupcodes_groupcode_gin (104 MB)
-- ✅ idx_productcategories_productid (37 MB)
-- ✅ idx_productimages_productid (8 KB)
-- ✅ idx_brand_id (32 KB)

-- 1. Materialized View için index'ler (bunlar EKSİK olabilir)
CREATE INDEX IF NOT EXISTS idx_mv_dotparts_partnumber 
    ON "mv_dotparts_joined"("PartNumber");

CREATE INDEX IF NOT EXISTS idx_mv_dotparts_composite 
    ON "mv_dotparts_joined"("VehicleTypeKey", "ManufactureKey", "BaseModelKey");

-- 2. Product için composite index (opsiyonel - genelde gerekmiyor)
-- CREATE INDEX IF NOT EXISTS idx_product_id_brandid 
--     ON "Product"("Id", "BrandId");

-- 7. ANALYZE - İstatistikleri güncelle
ANALYZE "SellerItems";
ANALYZE "Product";
ANALYZE "ProductGroupCodes";
ANALYZE "DatDatas";
ANALYZE "mv_dotparts_joined";
ANALYZE "ProductCategories";
ANALYZE "ProductImages";
ANALYZE "Brand";

-- 8. Index boyutlarını kontrol et
SELECT 
    schemaname,
    relname as tablename,
    indexrelname as indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
  AND (relname LIKE '%Seller%' 
       OR relname LIKE '%Product%' 
       OR relname = 'DatDatas'
       OR relname = 'Brand')
ORDER BY pg_relation_size(indexrelid) DESC;

-- 9. İndex kullanım istatistikleri
SELECT 
    schemaname,
    relname as tablename,
    indexrelname as indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
  AND idx_scan > 0
ORDER BY idx_scan DESC
LIMIT 20;
