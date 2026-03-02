-- ====================================================================
-- OEMS BAZLI MÜKERRER PRODUCT KAYITLARINI SİLME SCRIPT'İ (ULTRA FAST)
-- ====================================================================
-- Bu script, aynı Oems + SellerId kombinasyonuna sahip mükerrer 
-- Product kayıtlarını HIZLICA siler (sadece Product silme)
-- ====================================================================

DO $$
DECLARE
    v_duplicate_count INTEGER;
    v_products_to_delete INTEGER;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
    v_drop_sql TEXT;
    v_add_sql TEXT;
    v_batch_size INTEGER := 50000;
    v_processed INTEGER := 0;
    v_batch_count INTEGER;
    v_current_batch INTEGER;
    v_batch_deleted INTEGER;
    v_percent INTEGER;
BEGIN
    v_start_time := clock_timestamp();
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🗑️ OEMS BAZLI MÜKERRER PRODUCT SİLME İŞLEMİ BAŞLATILDI';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Mükerrer kayıtları tespit et (Oems + SellerId bazlı)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔍 Oems + SellerId bazlı mükerrer kayıtlar tespit ediliyor...';
    
    CREATE TEMP TABLE duplicate_products_by_oems ON COMMIT DROP AS
    WITH product_oems AS (
        SELECT 
            p."Id" AS "ProductId",
            p."Oems",
            COALESCE(p."SellerId", 0) AS "SellerId",
            p."CreatedDate",
            ROW_NUMBER() OVER (
                PARTITION BY p."Oems", COALESCE(p."SellerId", 0)
                ORDER BY p."CreatedDate" DESC, p."Id" DESC
            ) AS rn
        FROM "Product" p
        WHERE p."Oems" IS NOT NULL 
          AND p."Oems" != ''
          AND p."Oems" != 'UNIVERSAL'  -- Genel Oems'leri atla
    )
    SELECT "ProductId", "Oems", "SellerId"
    FROM product_oems
    WHERE rn > 1;  -- Sadece mükerrer olanlar (rn=1 olanlar kalacak)
    
    SELECT COUNT(*) INTO v_duplicate_count FROM duplicate_products_by_oems;
    RAISE NOTICE '✅ % mükerrer Product kaydı tespit edildi (% saniye)', 
        v_duplicate_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    IF v_duplicate_count = 0 THEN
        RAISE NOTICE 'ℹ️ Mükerrer kayıt bulunamadı. İşlem sonlandırılıyor.';
        RETURN;
    END IF;
    
    -- 2. Foreign key constraint'lerini geçici olarak drop et (ULTRA FAST silme için)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔧 Foreign key constraint''leri geçici olarak drop ediliyor...';
    
    CREATE TEMP TABLE fk_constraints_to_drop ON COMMIT DROP AS
    SELECT 
        tc.table_name,
        tc.constraint_name,
        'ALTER TABLE "' || tc.table_schema || '"."' || tc.table_name || '" DROP CONSTRAINT IF EXISTS "' || tc.constraint_name || '";' AS drop_sql,
        'ALTER TABLE "' || tc.table_schema || '"."' || tc.table_name || '" ADD CONSTRAINT "' || tc.constraint_name || '" ' ||
        'FOREIGN KEY ("' || kcu.column_name || '") REFERENCES "' || ccu.table_schema || '"."' || ccu.table_name || '"("' || ccu.column_name || '");' AS add_sql
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu 
        ON tc.constraint_schema = kcu.constraint_schema 
        AND tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage ccu 
        ON ccu.constraint_schema = tc.constraint_schema 
        AND ccu.constraint_name = tc.constraint_name
    WHERE tc.constraint_type = 'FOREIGN KEY'
      AND ccu.table_name = 'Product'
      AND ccu.column_name = 'Id'
      AND tc.table_schema = 'public';
    
    -- Constraint'leri drop et
    FOR v_drop_sql IN SELECT drop_sql FROM fk_constraints_to_drop LOOP
        BEGIN
            EXECUTE v_drop_sql;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE '   ⚠️ Constraint drop edilemedi: %', SQLERRM;
        END;
    END LOOP;
    
    RAISE NOTICE '✅ % foreign key constraint geçici olarak drop edildi (% saniye)', 
        (SELECT COUNT(*) FROM fk_constraints_to_drop),
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 3. Batch'ler halinde mükerrer Product'ları sil (PROGRESS ile)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🗑️ Mükerrer Product kayıtları siliniyor (batch processing: % kayıt/batch)...', v_batch_size;
    
    -- Batch processing için ProductId'leri sırala
    CREATE TEMP TABLE duplicate_productids_ordered ON COMMIT DROP AS
    SELECT "ProductId", ROW_NUMBER() OVER (ORDER BY "ProductId") AS rn
    FROM duplicate_products_by_oems;
    
    CREATE INDEX idx_dpo_rn ON duplicate_productids_ordered (rn);
    ANALYZE duplicate_productids_ordered;
    
    SELECT CEIL(COUNT(*)::numeric / v_batch_size) INTO v_batch_count FROM duplicate_productids_ordered;
    RAISE NOTICE '   → Toplam % batch işlenecek', v_batch_count;
    
    v_processed := 0;
    v_current_batch := 0;
    v_products_to_delete := 0;
    
    WHILE v_processed < v_duplicate_count LOOP
        v_current_batch := v_current_batch + 1;
        v_percent := ROUND((v_processed::numeric / v_duplicate_count * 100)::numeric);
        
        -- Her batch'te progress göster
        RAISE NOTICE '   → Batch %/% işleniyor... (%/% kayıt, %%% tamamlandı)', 
            v_current_batch, v_batch_count, v_processed, v_duplicate_count, v_percent;
        
        DELETE FROM "Product" p
        WHERE p."Id" IN (
            SELECT dpo."ProductId" 
            FROM duplicate_productids_ordered dpo
            WHERE dpo.rn > v_processed AND dpo.rn <= v_processed + v_batch_size
        );
        
        GET DIAGNOSTICS v_batch_deleted = ROW_COUNT;
        v_products_to_delete := v_products_to_delete + v_batch_deleted;
        v_processed := v_processed + v_batch_size;
        
        -- Her 5 batch'te bir detaylı log
        IF v_current_batch % 5 = 0 OR v_processed >= v_duplicate_count THEN
            v_percent := ROUND((v_processed::numeric / v_duplicate_count * 100)::numeric);
            RAISE NOTICE '   ✓ Batch % tamamlandı: % Product silindi (Toplam: %/% kayıt, %%% tamamlandı)', 
                v_current_batch, v_batch_deleted, v_processed, v_duplicate_count, v_percent;
        END IF;
    END LOOP;
    
    RAISE NOTICE '✅ % mükerrer Product silindi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 4. Foreign key constraint'lerini tekrar create et
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔧 Foreign key constraint''leri yeniden oluşturuluyor...';
    
    FOR v_add_sql IN SELECT add_sql FROM fk_constraints_to_drop LOOP
        BEGIN
            EXECUTE v_add_sql;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE '   ⚠️ Constraint yeniden oluşturulamadı: %', SQLERRM;
        END;
    END LOOP;
    
    RAISE NOTICE '✅ Foreign key constraint''leri yeniden oluşturuldu (% saniye)', 
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 5. Özet
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ OEMS BAZLI SİLME İŞLEMİ TAMAMLANDI';
    RAISE NOTICE '   Silinen Product: %', v_products_to_delete;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', 
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
END $$;
