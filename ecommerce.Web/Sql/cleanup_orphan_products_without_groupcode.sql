-- ====================================================================
-- ORPHAN PRODUCT CLEANUP SCRIPT (ProductGroupCodes'da olmayan)
-- ====================================================================
-- ProductGroupCodes'da olmayan Product'ları temizler
-- NOT: Sadece SellerId=1 (OtoIsmail) için - diğer satıcılar için ayrı kontrol gerekir
-- ====================================================================

CREATE OR REPLACE PROCEDURE "public"."cleanup_orphan_products_without_groupcode"()
AS $BODY$
DECLARE
    v_count INTEGER;
    v_deleted INTEGER := 0;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
    v_total_orphans INTEGER := 0;
BEGIN
    v_start_time := clock_timestamp();
    
    -- Lock timeout ayarla
    SET LOCAL lock_timeout = '10min';
    SET LOCAL statement_timeout = '30min';
    SET LOCAL transaction_isolation = 'read committed';
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🧹 ORPHAN PRODUCT CLEANUP başlatıldı (ProductGroupCodes olmayan)';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Orphan Product'ları tespit et (ProductGroupCodes'da olmayan)
    v_step_start := clock_timestamp();
    RAISE NOTICE '📊 Orphan Productlar tespit ediliyor (ProductGroupCodes olmayan)...';
    
    CREATE TEMP TABLE tmp_orphan_products ON COMMIT DROP AS
    SELECT p."Id", p."SellerId"
    FROM "Product" p
    WHERE p."Status" = 1
      AND NOT EXISTS (
          SELECT 1 FROM "ProductGroupCodes" pg WHERE pg."ProductId" = p."Id"
      );
    
    CREATE INDEX idx_tmp_orphan_productid ON tmp_orphan_products ("Id");
    ANALYZE tmp_orphan_products;
    
    SELECT COUNT(*) INTO v_total_orphans FROM tmp_orphan_products;
    RAISE NOTICE '✅ % orphan Product tespit edildi (% saniye)', v_total_orphans, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    IF v_total_orphans = 0 THEN
        RAISE NOTICE 'ℹ️ Silinecek orphan Product yok!';
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RETURN;
    END IF;
    
    -- Satıcı bazlı özet
    RAISE NOTICE E'\n📊 Satıcı Bazlı Özet:';
    FOR v_count IN 
        SELECT DISTINCT "SellerId" FROM tmp_orphan_products ORDER BY "SellerId"
    LOOP
        SELECT COUNT(*) INTO v_deleted 
        FROM tmp_orphan_products 
        WHERE "SellerId" = v_count;
        RAISE NOTICE '   SellerId %: % orphan Product', v_count, v_deleted;
    END LOOP;
    
    -- 2. Foreign key'leri temizle
    v_step_start := clock_timestamp();
    RAISE NOTICE E'\n🧹 Foreign key kayıtları temizleniyor...';
    
    -- SellerItems sil
    DELETE FROM "SellerItems" si
    WHERE si."ProductId" IN (SELECT "Id" FROM tmp_orphan_products);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE '   🗑️ % SellerItems kaydı silindi', v_count;
    
    -- ProductImages sil (Identified orphans)
    DELETE FROM "ProductImages" pi
    WHERE pi."ProductId" IN (SELECT "Id" FROM tmp_orphan_products);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '   🗑️ % ProductImages kaydı silindi (Identified orphans)', v_count;
    END IF;

    -- ProductImages sil (General - Product tablosunda olmayanlar)
    DELETE FROM "ProductImages" pi
    WHERE pi."ProductId" IS NOT NULL 
      AND NOT EXISTS (SELECT 1 FROM "Product" p WHERE p."Id" = pi."ProductId");
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '   🗑️ % ProductImages kaydı silindi (General orphan images)', v_count;
    END IF;
    
    -- PriceListItems güncelle
    UPDATE "PriceListItems" pli
    SET "ProductId" = NULL
    WHERE pli."ProductId" IN (SELECT "Id" FROM tmp_orphan_products);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '   🗑️ % PriceListItems kaydı güncellendi (ProductId = NULL)', v_count;
    END IF;
    
    RAISE NOTICE '✅ Foreign key temizliği tamamlandı (% saniye)', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 3. Product'ları sil (BATCH MODE - 10,000'lik gruplar halinde)
    v_step_start := clock_timestamp();
    RAISE NOTICE E'\n🗑️ Orphan Productlar siliniyor...';
    
    v_deleted := 0;
    LOOP
        -- Her batch'te 10,000 kayıt sil ve temp tablodan da kaldır
        WITH batch_to_delete AS (
            DELETE FROM tmp_orphan_products
            WHERE "Id" IN (
                SELECT "Id" FROM tmp_orphan_products
                LIMIT 10000
            )
            RETURNING "Id"
        )
        DELETE FROM "Product" p
        WHERE p."Id" IN (SELECT "Id" FROM batch_to_delete);
        
        GET DIAGNOSTICS v_count = ROW_COUNT;
        v_deleted := v_deleted + v_count;
        
        IF v_count > 0 THEN
            RAISE NOTICE '   → % Product silindi (Toplam: % / %)', v_count, v_deleted, v_total_orphans;
        ELSE
            EXIT; -- Daha fazla silinecek kayıt yok
        END IF;
        
        -- Progress göstermek için (her 100K'da bir)
        IF v_deleted > 0 AND v_deleted % 100000 = 0 THEN
            RAISE NOTICE '   📊 İlerleme: % %% tamamlandı', ROUND((v_deleted::numeric / v_total_orphans::numeric * 100)::numeric, 2);
        END IF;
        
        -- Her batch'ten sonra kısa bir bekleme (lock'ları serbest bırakmak için)
        PERFORM pg_sleep(0.1);
    END LOOP;
    
    RAISE NOTICE '✅ % orphan Product silindi (% saniye)', v_deleted, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 4. Özet
    RAISE NOTICE E'\n═══════════════════════════════════════════════════════════';
    RAISE NOTICE '📊 CLEANUP ÖZET:';
    RAISE NOTICE '   • Tespit edilen orphan: %', v_total_orphans;
    RAISE NOTICE '   • Silinen Product: %', v_deleted;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '❌ HATA: %', SQLERRM;
        RAISE;
END;
$BODY$ LANGUAGE plpgsql;
