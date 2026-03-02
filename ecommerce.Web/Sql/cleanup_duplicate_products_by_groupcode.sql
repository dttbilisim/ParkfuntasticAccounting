-- ====================================================================
-- DUPLICATE PRODUCT CLEANUP SCRIPT
-- ====================================================================
-- Tüm satıcılar için aynı GroupCode'a sahip duplicate Product'ları temizler
-- Strateji: Her GroupCode için en eski Product'ı tut, diğerlerini sil
-- ====================================================================

CREATE OR REPLACE PROCEDURE "public"."cleanup_duplicate_products_by_groupcode"()
AS $BODY$
DECLARE
    v_count INTEGER;
    v_deleted INTEGER := 0;
    v_batch_size INTEGER := 1000;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
    v_total_duplicates INTEGER := 0;
    v_seller_id INTEGER;
BEGIN
    v_start_time := clock_timestamp();
    
    -- Lock timeout ayarla
    SET LOCAL lock_timeout = '10min';
    SET LOCAL statement_timeout = '30min';
    SET LOCAL transaction_isolation = 'read committed';
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🧹 DUPLICATE PRODUCT CLEANUP başlatıldı';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '   Batch Size: % kayıt', v_batch_size;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Duplicate'leri tespit et (her GroupCode için en eski Product'ı tut)
    v_step_start := clock_timestamp();
    RAISE NOTICE '📊 Duplicate Productlar tespit ediliyor...';
    
    CREATE TEMP TABLE tmp_duplicate_products ON COMMIT DROP AS
    SELECT 
        pg."GroupCode",
        pg."ProductId",
        p."SellerId",
        p."CreatedDate",
        ROW_NUMBER() OVER (
            PARTITION BY pg."GroupCode", p."SellerId" 
            ORDER BY p."CreatedDate" ASC, p."Id" ASC
        ) AS rn
    FROM "ProductGroupCodes" pg
    JOIN "Product" p ON p."Id" = pg."ProductId"
    WHERE p."Status" = 1;
    
    CREATE INDEX idx_tmp_dup_productid ON tmp_duplicate_products ("ProductId");
    ANALYZE tmp_duplicate_products;
    
    -- Sadece duplicate olanları al (rn > 1)
    CREATE TEMP TABLE tmp_products_to_delete ON COMMIT DROP AS
    SELECT DISTINCT "ProductId", "SellerId", "GroupCode"
    FROM tmp_duplicate_products
    WHERE rn > 1;
    
    SELECT COUNT(*) INTO v_total_duplicates FROM tmp_products_to_delete;
    RAISE NOTICE '✅ % duplicate Product tespit edildi (% saniye)', v_total_duplicates, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    IF v_total_duplicates = 0 THEN
        RAISE NOTICE 'ℹ️ Silinecek duplicate Product yok!';
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RETURN;
    END IF;
    
    -- Satıcı bazlı özet
    RAISE NOTICE '\n📊 Satıcı Bazlı Özet:';
    FOR v_seller_id IN (SELECT DISTINCT "SellerId" FROM tmp_products_to_delete ORDER BY "SellerId")
    LOOP
        SELECT COUNT(*) INTO v_count 
        FROM tmp_products_to_delete 
        WHERE "SellerId" = v_seller_id;
        RAISE NOTICE '   SellerId %: % duplicate Product', v_seller_id, v_count;
    END LOOP;
    
    -- 2. Foreign key'leri temizle (SellerItems, ProductGroupCodes, vb.)
    v_step_start := clock_timestamp();
    RAISE NOTICE '\n🧹 Foreign key kayıtları temizleniyor...';
    
    -- SellerItems sil
    DELETE FROM "SellerItems" si
    WHERE si."ProductId" IN (SELECT "ProductId" FROM tmp_products_to_delete);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE '   🗑️ % SellerItems kaydı silindi', v_count;
    
    -- ProductGroupCodes sil
    DELETE FROM "ProductGroupCodes" pg
    WHERE pg."ProductId" IN (SELECT "ProductId" FROM tmp_products_to_delete);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE '   🗑️ % ProductGroupCodes kaydı silindi', v_count;
    
    -- PriceListItems güncelle (ProductId'yi NULL yap veya sil)
    UPDATE "PriceListItems" pli
    SET "ProductId" = NULL
    WHERE pli."ProductId" IN (SELECT "ProductId" FROM tmp_products_to_delete);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '   🗑️ % PriceListItems kaydı güncellendi (ProductId = NULL)', v_count;
    END IF;
    
    RAISE NOTICE '✅ Foreign key temizliği tamamlandı (% saniye)', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 3. Product'ları sil (tek seferde - foreign key'ler zaten temizlendi)
    v_step_start := clock_timestamp();
    RAISE NOTICE E'\n🗑️ Duplicate Productlar siliniyor...';
    
    DELETE FROM "Product" p
    WHERE p."Id" IN (SELECT "ProductId" FROM tmp_products_to_delete);
    
    GET DIAGNOSTICS v_deleted = ROW_COUNT;
    RAISE NOTICE '✅ % duplicate Product silindi (% saniye)', v_deleted, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 4. Özet
    RAISE NOTICE '\n═══════════════════════════════════════════════════════════';
    RAISE NOTICE '📊 CLEANUP ÖZET:';
    RAISE NOTICE '   • Tespit edilen duplicate: %', v_total_duplicates;
    RAISE NOTICE '   • Silinen Product: %', v_deleted;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '❌ HATA: %', SQLERRM;
        RAISE;
END;
$BODY$ LANGUAGE plpgsql;
