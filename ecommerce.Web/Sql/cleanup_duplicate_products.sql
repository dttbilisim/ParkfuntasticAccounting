-- ====================================================================
-- MÜKERRER PRODUCT KAYITLARINI TEMİZLEME SCRIPT'İ
-- ====================================================================
-- Bu script, aynı GroupCode'a sahip mükerrer Product kayıtlarını temizler
-- Her GroupCode için sadece 1 Product kalacak (en son oluşturulan veya en yüksek ID'li)
-- ====================================================================

DO $$
DECLARE
    v_duplicate_count INTEGER;
    v_products_to_delete INTEGER;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
BEGIN
    v_start_time := clock_timestamp();
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🧹 MÜKERRER PRODUCT TEMİZLEME İŞLEMİ BAŞLATILDI';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Mükerrer kayıtları tespit et
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔍 Mükerrer kayıtlar tespit ediliyor...';
    
    CREATE TEMP TABLE duplicate_products ON COMMIT DROP AS
    WITH product_groupcodes AS (
        SELECT 
            p."Id" AS "ProductId",
            pg."GroupCode",
            p."CreatedDate",
            ROW_NUMBER() OVER (
                PARTITION BY pg."GroupCode" 
                ORDER BY p."CreatedDate" DESC, p."Id" DESC
            ) AS rn
        FROM "Product" p
        INNER JOIN "ProductGroupCodes" pg ON pg."ProductId" = p."Id"
        WHERE pg."GroupCode" IS NOT NULL 
          AND pg."GroupCode" != ''
    )
    SELECT "ProductId", "GroupCode"
    FROM product_groupcodes
    WHERE rn > 1;  -- Sadece mükerrer olanlar (rn=1 olanlar kalacak)
    
    SELECT COUNT(*) INTO v_duplicate_count FROM duplicate_products;
    RAISE NOTICE '✅ % mükerrer Product kaydı tespit edildi (% saniye)', 
        v_duplicate_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    IF v_duplicate_count = 0 THEN
        RAISE NOTICE 'ℹ️ Mükerrer kayıt bulunamadı. İşlem sonlandırılıyor.';
        RETURN;
    END IF;
    
    -- 2. Her GroupCode için kalacak Product'ı belirle (en son oluşturulan veya en yüksek ID'li)
    CREATE TEMP TABLE products_to_keep ON COMMIT DROP AS
    WITH product_groupcodes AS (
        SELECT 
            p."Id" AS "ProductId",
            pg."GroupCode",
            p."CreatedDate",
            ROW_NUMBER() OVER (
                PARTITION BY pg."GroupCode" 
                ORDER BY p."CreatedDate" DESC, p."Id" DESC
            ) AS rn
        FROM "Product" p
        INNER JOIN "ProductGroupCodes" pg ON pg."ProductId" = p."Id"
        WHERE pg."GroupCode" IS NOT NULL 
          AND pg."GroupCode" != ''
    )
    SELECT "ProductId", "GroupCode"
    FROM product_groupcodes
    WHERE rn = 1;  -- Her GroupCode için sadece 1 Product kalacak
    
    -- 3. SellerItems'ları güncelle (silinecek Product'ların SellerItems'larını kalacak Product'a taşı)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 SellerItems güncelleniyor...';
    
    -- Önce çakışmaları çöz (aynı SellerId + ProductId kombinasyonu varsa)
    DELETE FROM "SellerItems" si
    USING duplicate_products dp
    JOIN products_to_keep ptk ON ptk."GroupCode" = dp."GroupCode"
    WHERE si."ProductId" = dp."ProductId"
      AND EXISTS (
          SELECT 1 FROM "SellerItems" si2 
          WHERE si2."SellerId" = si."SellerId" 
            AND si2."ProductId" = ptk."ProductId"
      );
    
    -- Sonra SellerItems'ları yeni Product'a taşı
    UPDATE "SellerItems" si
    SET "ProductId" = ptk."ProductId",
        "ModifiedDate" = NOW()
    FROM duplicate_products dp
    JOIN products_to_keep ptk ON ptk."GroupCode" = dp."GroupCode"
    WHERE si."ProductId" = dp."ProductId"
      AND NOT EXISTS (
          SELECT 1 FROM "SellerItems" si2 
          WHERE si2."SellerId" = si."SellerId" 
            AND si2."ProductId" = ptk."ProductId"
      );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % SellerItem güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 4. ProductImages'ları güncelle ve mükerrer kayıtları temizle
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductImages güncelleniyor ve mükerrer kayıtlar temizleniyor...';
    
    -- Önce mükerrer ProductImages'ları tespit et ve sil (aynı ProductId + FileName veya FileGuid)
    CREATE TEMP TABLE duplicate_productimages ON COMMIT DROP AS
    WITH productimages_ranked AS (
        SELECT 
            pi."Id",
            pi."ProductId",
            pi."FileName",
            pi."FileGuid",
            ROW_NUMBER() OVER (
                PARTITION BY pi."ProductId", COALESCE(pi."FileName", ''), COALESCE(pi."FileGuid", '')
                ORDER BY pi."Id" ASC
            ) AS rn
        FROM "ProductImages" pi
    )
    SELECT "Id"
    FROM productimages_ranked
    WHERE rn > 1;  -- Sadece mükerrer olanlar (rn=1 olanlar kalacak)
    
    SELECT COUNT(*) INTO v_products_to_delete FROM duplicate_productimages;
    IF v_products_to_delete > 0 THEN
        DELETE FROM "ProductImages" pi
        WHERE pi."Id" IN (SELECT "Id" FROM duplicate_productimages);
        RAISE NOTICE '✅ % mükerrer ProductImage silindi', v_products_to_delete;
    END IF;
    
    -- Sonra ProductImages'ları yeni Product'a taşı (duplicate Product'lar için)
    UPDATE "ProductImages" pi
    SET "ProductId" = ptk."ProductId"
    FROM duplicate_products dp
    JOIN products_to_keep ptk ON ptk."GroupCode" = dp."GroupCode"
    WHERE pi."ProductId" = dp."ProductId";
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % ProductImage güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 5. ProductCategories'ları güncelle
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductCategories güncelleniyor...';
    
    -- Önce çakışmaları çöz
    DELETE FROM "ProductCategories" pc
    USING duplicate_products dp
    JOIN products_to_keep ptk ON ptk."GroupCode" = dp."GroupCode"
    WHERE pc."ProductId" = dp."ProductId"
      AND EXISTS (
          SELECT 1 FROM "ProductCategories" pc2 
          WHERE pc2."ProductId" = ptk."ProductId" 
            AND pc2."CategoryId" = pc."CategoryId"
      );
    
    -- Sonra ProductCategories'ları yeni Product'a taşı
    UPDATE "ProductCategories" pc
    SET "ProductId" = ptk."ProductId"
    FROM duplicate_products dp
    JOIN products_to_keep ptk ON ptk."GroupCode" = dp."GroupCode"
    WHERE pc."ProductId" = dp."ProductId"
      AND NOT EXISTS (
          SELECT 1 FROM "ProductCategories" pc2 
          WHERE pc2."ProductId" = ptk."ProductId" 
            AND pc2."CategoryId" = pc."CategoryId"
      );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % ProductCategory güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 6. ProductGroupCodes'ları temizle (mükerrer Product'ların GroupCode'larını sil)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductGroupCodes temizleniyor...';
    
    DELETE FROM "ProductGroupCodes" pg
    USING duplicate_products dp
    WHERE pg."ProductId" = dp."ProductId";
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % ProductGroupCode silindi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 7. Son olarak mükerrer Product'ları sil
    v_step_start := clock_timestamp();
    RAISE NOTICE '🗑️ Mükerrer Product kayıtları siliniyor...';
    
    DELETE FROM "Product" p
    WHERE p."Id" IN (SELECT "ProductId" FROM duplicate_products);
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % mükerrer Product silindi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 8. Özet
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ PRODUCT TEMİZLEME İŞLEMİ TAMAMLANDI';
    RAISE NOTICE '   Silinen Product: %', v_products_to_delete;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', 
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
END $$;

-- ====================================================================
-- MÜKERRER PRODUCTIMAGES KAYITLARINI TEMİZLEME
-- ====================================================================
-- Aynı ProductId + FileName kombinasyonuna sahip mükerrer ProductImages kayıtlarını temizler
-- ====================================================================

DO $$
DECLARE
    v_duplicate_count INTEGER;
    v_images_to_delete INTEGER;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
BEGIN
    v_start_time := clock_timestamp();
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🧹 MÜKERRER PRODUCTIMAGES TEMİZLEME İŞLEMİ BAŞLATILDI';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Mükerrer ProductImages kayıtlarını tespit et
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔍 Mükerrer ProductImages kayıtları tespit ediliyor...';
    
    CREATE TEMP TABLE duplicate_productimages ON COMMIT DROP AS
    WITH productimages_ranked AS (
        SELECT 
            pi."Id",
            pi."ProductId",
            COALESCE(pi."FileName", '') AS "FileName",
            pi."CreatedDate",
            ROW_NUMBER() OVER (
                PARTITION BY pi."ProductId", COALESCE(pi."FileName", '')
                ORDER BY pi."CreatedDate" DESC, pi."Id" DESC
            ) AS rn
        FROM "ProductImages" pi
        WHERE pi."ProductId" IS NOT NULL
    )
    SELECT "Id", "ProductId", "FileName"
    FROM productimages_ranked
    WHERE rn > 1;  -- Sadece mükerrer olanlar (rn=1 olanlar kalacak)
    
    SELECT COUNT(*) INTO v_duplicate_count FROM duplicate_productimages;
    RAISE NOTICE '✅ % mükerrer ProductImage kaydı tespit edildi (% saniye)', 
        v_duplicate_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    IF v_duplicate_count = 0 THEN
        RAISE NOTICE 'ℹ️ Mükerrer ProductImage kaydı bulunamadı. İşlem sonlandırılıyor.';
        RETURN;
    END IF;
    
    -- 2. Mükerrer ProductImages kayıtlarını sil
    v_step_start := clock_timestamp();
    RAISE NOTICE '🗑️ Mükerrer ProductImages kayıtları siliniyor...';
    
    DELETE FROM "ProductImages" pi
    WHERE pi."Id" IN (SELECT "Id" FROM duplicate_productimages);
    
    GET DIAGNOSTICS v_images_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % mükerrer ProductImage silindi (% saniye)', 
        v_images_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 3. Özet
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ PRODUCTIMAGES TEMİZLEME İŞLEMİ TAMAMLANDI';
    RAISE NOTICE '   Silinen ProductImage: %', v_images_to_delete;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', 
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
END $$;

-- İstatistikleri göster
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
UNION ALL
SELECT 
    'ProductImages' AS "Tablo",
    COUNT(*) AS "Toplam Kayıt"
FROM "ProductImages"
UNION ALL
SELECT 
    'Unique ProductImages (ProductId+FileName)' AS "Tablo",
    COUNT(DISTINCT ("ProductId", COALESCE("FileName", ''))) AS "Toplam Kayıt"
FROM "ProductImages"
ORDER BY "Tablo";
