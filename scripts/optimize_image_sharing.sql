-- ====================================================================
-- OPTIMIZED PRODUCT IMAGE SHARING SCRIPT (FAST V2)
-- ====================================================================
-- Optimizasyon: Büyük JOIN yerine, önce adayları filtrele.
-- ====================================================================

DO $$ BEGIN
    RAISE NOTICE '🚀 Script başlatıldı...';
END $$;

-- Temp tabloları temizle
DROP TABLE IF EXISTS same_name_products;
DROP TABLE IF EXISTS potential_sources;
DROP TABLE IF EXISTS best_source_products;
DROP TABLE IF EXISTS target_products;
DROP TABLE IF EXISTS image_sharing_map;

-- Step 1: Aynı isme sahip ürün gruplarını bul
DO $$ BEGIN
    RAISE NOTICE '📊 Step 1: Aynı isme sahip ürün grupları bulunuyor...';
END $$;

CREATE TEMP TABLE same_name_products AS
SELECT 
    "Name"
FROM "Product"
WHERE "Status" = 1
GROUP BY "Name"
HAVING COUNT(*) > 1;

CREATE INDEX idx_snp_name ON same_name_products ("Name");
ANALYZE same_name_products;

DO $$ 
DECLARE count_res INT;
BEGIN
    SELECT COUNT(*) INTO count_res FROM same_name_products;
    RAISE NOTICE '✅ % ürün grubu bulundu', count_res;
END $$;

-- Step 2: Kaynak adaylarını (resmi olanları) bul
-- Bu sorgu önceki scriptte çok yavaştı çünkü tüm Product tablosunu tarıyordu.
-- Şimdi sadece ismine göre filtrelenmiş ürünlerin resimlerine bakacağız.
DO $$ BEGIN
    RAISE NOTICE '📸 Step 2: Kaynak adayları (resmi olanlar) hazırlanıyor...';
END $$;

CREATE TEMP TABLE potential_sources AS
SELECT 
    p."Name",
    p."Id" as "ProductId",
    COUNT(pi."Id") as "ImgCount"
FROM "Product" p
JOIN same_name_products snp ON snp."Name" = p."Name" -- Filtreleme burası
JOIN "ProductImages" pi ON pi."ProductId" = p."Id" AND pi."Status" = 1
WHERE p."Status" = 1
GROUP BY p."Name", p."Id";

CREATE INDEX idx_ps_name_count ON potential_sources ("Name", "ImgCount" DESC);
ANALYZE potential_sources;

-- Step 3: Her grup için EN İYİ kaynağı seç
DO $$ BEGIN
    RAISE NOTICE '🏆 Step 3: En iyi kaynaklar seçiliyor...';
END $$;

CREATE TEMP TABLE best_source_products AS
SELECT DISTINCT ON ("Name")
    "Name",
    "ProductId" as source_product_id,
    "ImgCount"
FROM potential_sources
ORDER BY "Name", "ImgCount" DESC;

CREATE INDEX idx_bsp_name ON best_source_products ("Name");
ANALYZE best_source_products;

DO $$ 
DECLARE count_res INT;
BEGIN
    SELECT COUNT(*) INTO count_res FROM best_source_products;
    RAISE NOTICE '✅ % kaynak ürün seçildi', count_res;
END $$;

-- Step 4: Hedefleri (resimsizleri) bul ve eşleştir
DO $$ BEGIN
    RAISE NOTICE '🎯 Step 4: Hedefler bulunup eşleştiriliyor...';
END $$;

CREATE TEMP TABLE image_sharing_map AS
SELECT 
    bsp.source_product_id,
    p."Id" as target_product_id
FROM "Product" p
JOIN best_source_products bsp ON bsp."Name" = p."Name"
WHERE p."Status" = 1
  -- Hedefin hiç resmi olmamalı (bu kontrolü hızlandırmak için NOT EXISTS yerine LEFT JOIN kullanabiliriz ama bu da ok)
  AND NOT EXISTS (
      SELECT 1 FROM "ProductImages" pi WHERE pi."ProductId" = p."Id" AND pi."Status" = 1
  )
  -- Kendisi olmamalı
  AND p."Id" <> bsp.source_product_id;

DO $$ 
DECLARE count_res INT;
BEGIN
    SELECT COUNT(*) INTO count_res FROM image_sharing_map;
    RAISE NOTICE '✅ % kopyalama işlemi yapılacak', count_res;
END $$;

-- Step 5: BATCH Kopyalama
DO $$
DECLARE
    batch_size INT := 5000;
    total_updated INT := 0;
    row_count INT;
BEGIN
    RAISE NOTICE '📦 Step 5: Fotoğraflar kopyalanıyor...';
    
    LOOP
        -- Batch Ekle
        WITH batch AS (
            SELECT source_product_id, target_product_id 
            FROM image_sharing_map 
            LIMIT batch_size
        ),
        deleted_from_queue AS (
            DELETE FROM image_sharing_map 
            WHERE target_product_id IN (SELECT target_product_id FROM batch)
            RETURNING source_product_id, target_product_id
        )
        INSERT INTO "ProductImages" (
            "ProductId", "Order", "FileGuid", "FileName", "Root", "Status", "CreatedDate", "CreatedId"
        )
        SELECT 
            dq.target_product_id,
            pi."Order", pi."FileGuid", pi."FileName", pi."Root", 1, NOW(), 0
        FROM deleted_from_queue dq
        JOIN "ProductImages" pi ON pi."ProductId" = dq.source_product_id AND pi."Status" = 1
        ON CONFLICT DO NOTHING;
        
        GET DIAGNOSTICS row_count = ROW_COUNT;
        total_updated := total_updated + row_count;
        
        RAISE NOTICE '   + % resim eklendi (Toplam: %)', row_count, total_updated;
        
        -- Eğer batch boşsa çık
        IF row_count = 0 THEN EXIT; END IF;
        
        -- Commit her adımda yapılamaz (DO blok içinde), ama batch olduğu için RAM şişmez.
    END LOOP;
    
    RAISE NOTICE '🎉 İŞLEM TAMAMLANDI. Toplam % yeni resim kaydı oluşturuldu.', total_updated;
END $$;
