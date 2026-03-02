-- ====================================================================
-- PRODUCT IMAGE SHARING SCRIPT (FIXED - BATCHED + DETAILED LOGS)
-- ====================================================================
-- Amaç: Aynı isme sahip ürünler için fotoğrafları paylaş
-- Optimizasyon: Batch processing + Detaylı progress log + Duplicate kontrolü
-- ====================================================================

-- Temizlik: Transaction state reset
ROLLBACK;

BEGIN;

-- Temp tabloları baştan temizle (önceki çalışmadan kalma ihtimaline karşı)
DROP TABLE IF EXISTS same_name_products;
DROP TABLE IF EXISTS best_source_products;
DROP TABLE IF EXISTS target_products;
DROP TABLE IF EXISTS image_sharing_map;

DO $$ BEGIN
    RAISE NOTICE '🚀 Script başlatıldı...';
END $$;

-- Step 1: Aynı isme sahip ürün gruplarını bul
DO $$ BEGIN
    RAISE NOTICE '📊 Step 1: Aynı isme sahip ürün grupları bulunuyor...';
END $$;
CREATE TEMP TABLE same_name_products AS
SELECT 
    "Name",
    COUNT(*) as product_count,
    ARRAY_AGG("Id" ORDER BY "Id") as product_ids
FROM "Product"
WHERE "Status" = 1
GROUP BY "Name"
HAVING COUNT(*) > 1;

DO $$
DECLARE
    group_count INT;
BEGIN
    SELECT COUNT(*) INTO group_count FROM same_name_products;
    RAISE NOTICE '✅ % ürün grubu bulundu', group_count;
END $$;

-- Step 2: En fazla fotoğrafı olan kaynak ürünleri seç
DO $$ BEGIN
    RAISE NOTICE '📸 Step 2: Kaynak ürünler (en çok fotoğraflı) seçiliyor...';
END $$;
CREATE TEMP TABLE best_source_products AS
SELECT DISTINCT ON (p."Name")
    p."Name",
    p."Id" as source_product_id,
    COUNT(pi."Id") as image_count
FROM "Product" p
INNER JOIN "ProductImages" pi ON pi."ProductId" = p."Id" AND pi."Status" = 1
WHERE p."Name" IN (SELECT "Name" FROM same_name_products)
  AND p."Status" = 1
GROUP BY p."Name", p."Id"
ORDER BY p."Name", COUNT(pi."Id") DESC;

DO $$
DECLARE
    source_count INT;
    total_images INT;
BEGIN
    SELECT COUNT(*) INTO source_count FROM best_source_products;
    SELECT SUM(image_count) INTO total_images FROM best_source_products;
    RAISE NOTICE '✅ % kaynak ürün seçildi (toplam % fotoğraf)', source_count, total_images;
END $$;

-- Step 3: Fotoğrafı olmayan hedef ürünler
DO $$ BEGIN
    RAISE NOTICE '🎯 Step 3: Hedef ürünler (fotoğrafsız) belirleniyor...';
END $$;
CREATE TEMP TABLE target_products AS
SELECT 
    p."Id" as target_product_id,
    p."Name"
FROM "Product" p
WHERE p."Name" IN (SELECT "Name" FROM same_name_products)
  AND p."Status" = 1
  AND NOT EXISTS (
      SELECT 1 FROM "ProductImages" pi 
      WHERE pi."ProductId" = p."Id" AND pi."Status" = 1
  );

DO $$
DECLARE
    target_count INT;
BEGIN
    SELECT COUNT(*) INTO target_count FROM target_products;
    RAISE NOTICE '✅ % hedef ürün bulundu', target_count;
END $$;

-- Step 4: Kaynak-hedef mapping
DO $$ BEGIN
    RAISE NOTICE '🔗 Step 4: Kaynak-hedef eşleştirme yapılıyor...';
END $$;
CREATE TEMP TABLE image_sharing_map AS
SELECT 
    bsp.source_product_id,
    tp.target_product_id,
    bsp."Name",
    bsp.image_count
FROM best_source_products bsp
INNER JOIN target_products tp ON tp."Name" = bsp."Name";

-- Index ekle (performans için)
CREATE INDEX idx_image_sharing_map_source ON image_sharing_map(source_product_id);
CREATE INDEX idx_image_sharing_map_target ON image_sharing_map(target_product_id);

DO $$
DECLARE
    mapping_count INT;
    estimated_inserts INT;
BEGIN
    SELECT COUNT(*) INTO mapping_count FROM image_sharing_map;
    SELECT SUM(image_count) INTO estimated_inserts FROM image_sharing_map;
    RAISE NOTICE '✅ % eşleştirme yapıldı → Tahmini % fotoğraf kopyalanacak', mapping_count, estimated_inserts;
END $$;

-- Step 5: BATCH olarak fotoğraf kopyalama (FIXED)
DO $$ BEGIN
    RAISE NOTICE '📦 Step 5: Fotoğraflar kopyalanıyor (BATCH)...';
END $$;

DO $$
DECLARE
    batch_size INT := 5000;  -- Batch size küçültüldü (10000 -> 5000)
    total_inserted INT := 0;
    batch_inserted INT;
    batch_num INT := 1;
    total_mappings INT;
    processed_mappings INT := 0;
    last_processed_id INT := 0;
    current_batch_mappings INT;
BEGIN
    SELECT COUNT(*) INTO total_mappings FROM image_sharing_map;
    
    IF total_mappings = 0 THEN
        RAISE NOTICE '⚠️ Kopyalanacak fotoğraf bulunamadı.';
    ELSE
        -- BATCH döngüsü (FIXED: ID bazlı pagination)
        LOOP
            -- Her batch'te batch_size kadar mapping işle (ID bazlı)
            WITH batch_mappings AS (
                SELECT source_product_id, target_product_id, image_count
                FROM image_sharing_map
                WHERE source_product_id > last_processed_id
                ORDER BY source_product_id
                LIMIT batch_size
            ),
            -- Sadece henüz kopyalanmamış fotoğrafları seç
            images_to_copy AS (
                SELECT DISTINCT
                    bm.target_product_id,
                    pi."Order",
                    pi."FileGuid",
                    pi."FileName",
                    pi."Root"
                FROM batch_mappings bm
                INNER JOIN "ProductImages" pi ON pi."ProductId" = bm.source_product_id
                WHERE pi."Status" = 1
                  -- FIXED: Aynı fotoğraf zaten varsa ekleme (FileGuid veya FileName kontrolü)
                  AND NOT EXISTS (
                      SELECT 1 FROM "ProductImages" existing
                      WHERE existing."ProductId" = bm.target_product_id
                        AND existing."Status" = 1
                        AND (
                            existing."FileGuid" = pi."FileGuid"
                            OR existing."FileName" = pi."FileName"
                        )
                  )
            )
            INSERT INTO "ProductImages" (
                "ProductId",
                "Order",
                "FileGuid",
                "FileName",
                "Root",
                "Status",
                "CreatedDate",
                "CreatedId"
            )
            SELECT 
                itc.target_product_id,
                itc."Order",
                itc."FileGuid",
                itc."FileName",
                itc."Root",
                1,
                NOW(),
                0
            FROM images_to_copy itc
            ON CONFLICT DO NOTHING;  -- Ek güvenlik için
            
            GET DIAGNOSTICS batch_inserted = ROW_COUNT;
            total_inserted := total_inserted + batch_inserted;
            
            -- Son işlenen ID'yi güncelle
            SELECT COALESCE(MAX(source_product_id), last_processed_id) INTO last_processed_id
            FROM image_sharing_map
            WHERE source_product_id <= last_processed_id + batch_size;
            
            -- İlerleme hesapla
            SELECT COUNT(*) INTO current_batch_mappings
            FROM image_sharing_map
            WHERE source_product_id <= last_processed_id;
            
            RAISE NOTICE '  ✓ Batch %: % fotoğraf eklendi (Toplam: % / İlerleme: % %%)', 
                batch_num, 
                batch_inserted, 
                total_inserted,
                ROUND((current_batch_mappings::NUMERIC / total_mappings::NUMERIC) * 100, 1);
            
            -- Eğer son batch küçükse veya tamamlandıysa dur
            EXIT WHEN last_processed_id >= (SELECT MAX(source_product_id) FROM image_sharing_map);
            
            batch_num := batch_num + 1;
            
            -- Güvenlik: Çok fazla batch olursa dur
            IF batch_num > 1000 THEN
                RAISE NOTICE '⚠️ Maksimum batch sayısına ulaşıldı. Durduruluyor...';
                EXIT;
            END IF;
        END LOOP;
    END IF;
    
    RAISE NOTICE '🎉 TAMAMLANDI! Toplam % fotoğraf kopyalandı', total_inserted;
END $$;

-- Temizlik
DROP TABLE IF EXISTS same_name_products;
DROP TABLE IF EXISTS best_source_products;
DROP TABLE IF EXISTS target_products;
DROP TABLE IF EXISTS image_sharing_map;

COMMIT;

DO $$ BEGIN
    RAISE NOTICE '✅ Script başarıyla tamamlandı!';
END $$;
