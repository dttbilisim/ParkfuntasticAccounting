-- ====================================================================
-- OEMS BAZLI MÜKERRER PRODUCT KAYITLARINI TEMİZLEME SCRIPT'İ
-- ====================================================================
-- Bu script, aynı Oems'e sahip mükerrer Product kayıtlarını temizler
-- Her Oems için sadece 1 Product kalacak (en son oluşturulan veya en yüksek ID'li)
-- ====================================================================

DO $$
DECLARE
    v_duplicate_count INTEGER;
    v_products_to_delete INTEGER;
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
    v_batch_size INTEGER := 30000;
    v_processed INTEGER := 0;
    v_batch_count INTEGER;
    v_current_batch INTEGER;
    v_batch_deleted INTEGER;
    v_percent INTEGER;
    v_count INTEGER;
    v_drop_sql TEXT;
    v_add_sql TEXT;
BEGIN
    v_start_time := clock_timestamp();
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🧹 OEMS BAZLI MÜKERRER PRODUCT TEMİZLEME İŞLEMİ BAŞLATILDI';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. Mükerrer kayıtları tespit et (Oems bazlı)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔍 Oems bazlı mükerrer kayıtlar tespit ediliyor...';
    
    CREATE TEMP TABLE duplicate_products_by_oems ON COMMIT DROP AS
    WITH product_oems AS (
        SELECT 
            p."Id" AS "ProductId",
            p."Oems",
            p."SellerId",
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
    
    -- 2. Her Oems için kalacak Product'ı belirle
    CREATE TEMP TABLE products_to_keep_by_oems ON COMMIT DROP AS
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
          AND p."Oems" != 'UNIVERSAL'
    )
    SELECT "ProductId", "Oems", "SellerId"
    FROM product_oems
    WHERE rn = 1;  -- Her Oems için sadece 1 Product kalacak
    
    -- 3. SellerItems'ları güncelle
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 SellerItems güncelleniyor...';
    
    -- Strateji: Aynı SellerId + ProductId kombinasyonuna sahip SellerItems'ları birleştir
    -- Önce silinecek Product'ların SellerItems'larını geçici tabloya al
    CREATE TEMP TABLE selleritems_to_move_raw ON COMMIT DROP AS
    SELECT 
        si."Id",
        si."SellerId",
        dp."ProductId" AS "OldProductId",
        ptk."ProductId" AS "NewProductId",
        si."Stock",
        si."CostPrice",
        si."SalePrice",
        si."Commision",
        si."Currency",
        si."Unit",
        si."Status",
        si."SourceId",
        si."Step",
        si."MinSaleAmount",
        si."MaxSaleAmount",
        si."CreatedId",
        si."CreatedDate"
    FROM "SellerItems" si
    JOIN duplicate_products_by_oems dp ON dp."ProductId" = si."ProductId"
    JOIN products_to_keep_by_oems ptk ON ptk."Oems" = dp."Oems" AND ptk."SellerId" = dp."SellerId";
    
    -- Aynı SellerId + NewProductId kombinasyonuna sahip kayıtları filtrele (sadece ilk kayıt kalacak)
    CREATE TEMP TABLE selleritems_to_move ON COMMIT DROP AS
    SELECT DISTINCT ON (stm."SellerId", stm."NewProductId")
        stm."Id",
        stm."SellerId",
        stm."OldProductId",
        stm."NewProductId",
        stm."Stock",
        stm."CostPrice",
        stm."SalePrice",
        stm."Commision",
        stm."Currency",
        stm."Unit",
        stm."Status",
        stm."SourceId",
        stm."Step",
        stm."MinSaleAmount",
        stm."MaxSaleAmount",
        stm."CreatedId",
        stm."CreatedDate"
    FROM selleritems_to_move_raw stm
    ORDER BY stm."SellerId", stm."NewProductId", stm."CreatedDate" DESC, stm."Id" DESC;
    
    -- Çakışan SellerItems'ları sil (hedef Product'ta zaten SellerItem varsa)
    DELETE FROM "SellerItems" si
    WHERE si."Id" IN (
        SELECT stm."Id"
        FROM selleritems_to_move stm
        WHERE EXISTS (
            SELECT 1 FROM "SellerItems" si2 
            WHERE si2."SellerId" = stm."SellerId" 
              AND si2."ProductId" = stm."NewProductId"
        )
    );
    
    -- Kalan SellerItems'ları yeni Product'a taşı (artık çakışma olmayacak)
    UPDATE "SellerItems" si
    SET "ProductId" = stm."NewProductId",
        "ModifiedDate" = NOW()
    FROM selleritems_to_move stm
    WHERE si."Id" = stm."Id";
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '✅ % SellerItem güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 4. ProductImages'ları güncelle (ULTRA FAST: Pre-computed mapping table)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductImages güncelleniyor (ultra-fast pre-computed mode)...';
    
    -- Önce tüm ProductImages mapping'ini hazırla (çok daha hızlı)
    CREATE TEMP TABLE productimages_mapping_raw ON COMMIT DROP AS
    SELECT 
        pi."Id",
        pi."ProductId" AS "OldProductId",
        ptk."ProductId" AS "NewProductId",
        pi."FileName"
    FROM "ProductImages" pi
    JOIN duplicate_products_by_oems dp ON dp."ProductId" = pi."ProductId"
    JOIN products_to_keep_by_oems ptk ON ptk."Oems" = dp."Oems" AND ptk."SellerId" = dp."SellerId";
    
    -- Aynı NewProductId + FileName kombinasyonuna sahip kayıtları filtrele (sadece ilk kayıt kalacak)
    CREATE TEMP TABLE productimages_mapping ON COMMIT DROP AS
    SELECT DISTINCT ON (pim."NewProductId", pim."FileName")
        pim."Id",
        pim."OldProductId",
        pim."NewProductId",
        pim."FileName"
    FROM productimages_mapping_raw pim
    ORDER BY pim."NewProductId", pim."FileName", pim."Id" DESC;
    
    CREATE INDEX idx_pim_id ON productimages_mapping ("Id");
    CREATE INDEX idx_pim_newproduct_filename ON productimages_mapping ("NewProductId", "FileName");
    ANALYZE productimages_mapping;
    
    RAISE NOTICE '   → % ProductImage mapping hazırlandı (duplicate''ler filtrelendi)', (SELECT COUNT(*) FROM productimages_mapping);
    
    -- Çakışmaları sil (hedef Product'ta aynı FileName varsa)
    DELETE FROM "ProductImages" pi
    WHERE pi."Id" IN (
        SELECT pim."Id"
        FROM productimages_mapping pim
        WHERE EXISTS (
            SELECT 1 FROM "ProductImages" pi2 
            WHERE pi2."ProductId" = pim."NewProductId" 
              AND pi2."FileName" = pim."FileName"
        )
    );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '   → % çakışan ProductImage silindi', v_products_to_delete;
    
    -- Kalan ProductImages'ları BATCH'ler halinde güncelle (çok daha hızlı)
    SELECT COUNT(*) INTO v_count FROM productimages_mapping;
    SELECT CEIL(v_count::numeric / v_batch_size) INTO v_batch_count;
    RAISE NOTICE '   → % ProductImage batch''ler halinde güncellenecek (% batch)', v_count, v_batch_count;
    
    v_processed := 0;
    v_current_batch := 0;
    v_products_to_delete := 0;
    
    CREATE TEMP TABLE pim_ordered ON COMMIT DROP AS
    SELECT "Id", "NewProductId", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM productimages_mapping;
    
    CREATE INDEX idx_pim_ordered_rn ON pim_ordered (rn);
    ANALYZE pim_ordered;
    
    WHILE v_processed < v_count LOOP
        v_current_batch := v_current_batch + 1;
        IF v_current_batch % 10 = 0 OR v_processed = 0 THEN
            v_percent := ROUND((v_processed::numeric / v_count * 100)::numeric);
            RAISE NOTICE '   → ProductImage batch %/% işleniyor... (%/% kayıt, %%%)', 
                v_current_batch, v_batch_count, v_processed, v_count, v_percent;
        END IF;
        
        UPDATE "ProductImages" pi
        SET "ProductId" = pim."NewProductId"
        FROM productimages_mapping pim
        JOIN pim_ordered pimo ON pimo."Id" = pim."Id"
        WHERE pi."Id" = pim."Id"
          AND pimo.rn > v_processed AND pimo.rn <= v_processed + v_batch_size;
        
        GET DIAGNOSTICS v_batch_deleted = ROW_COUNT;
        v_products_to_delete := v_products_to_delete + v_batch_deleted;
        v_processed := v_processed + v_batch_size;
    END LOOP;
    
    RAISE NOTICE '✅ % ProductImage güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 5. ProductCategories'ları güncelle (ULTRA FAST: Pre-computed mapping)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductCategories güncelleniyor (ultra-fast pre-computed mode)...';
    
    -- Önce tüm ProductCategories mapping'ini hazırla
    CREATE TEMP TABLE productcategories_mapping_raw ON COMMIT DROP AS
    SELECT 
        pc."Id",
        pc."ProductId" AS "OldProductId",
        ptk."ProductId" AS "NewProductId",
        pc."CategoryId"
    FROM "ProductCategories" pc
    JOIN duplicate_products_by_oems dp ON dp."ProductId" = pc."ProductId"
    JOIN products_to_keep_by_oems ptk ON ptk."Oems" = dp."Oems" AND ptk."SellerId" = dp."SellerId";
    
    -- Aynı NewProductId + CategoryId kombinasyonuna sahip kayıtları filtrele
    CREATE TEMP TABLE productcategories_mapping ON COMMIT DROP AS
    SELECT DISTINCT ON (pcm."NewProductId", pcm."CategoryId")
        pcm."Id",
        pcm."OldProductId",
        pcm."NewProductId",
        pcm."CategoryId"
    FROM productcategories_mapping_raw pcm
    ORDER BY pcm."NewProductId", pcm."CategoryId", pcm."Id" DESC;
    
    CREATE INDEX idx_pcm_id ON productcategories_mapping ("Id");
    CREATE INDEX idx_pcm_newproduct_category ON productcategories_mapping ("NewProductId", "CategoryId");
    ANALYZE productcategories_mapping;
    
    RAISE NOTICE '   → % ProductCategory mapping hazırlandı (duplicate''ler filtrelendi)', (SELECT COUNT(*) FROM productcategories_mapping);
    
    -- Çakışmaları sil
    DELETE FROM "ProductCategories" pc
    WHERE pc."Id" IN (
        SELECT pcm."Id"
        FROM productcategories_mapping pcm
        WHERE EXISTS (
            SELECT 1 FROM "ProductCategories" pc2 
            WHERE pc2."ProductId" = pcm."NewProductId" 
              AND pc2."CategoryId" = pcm."CategoryId"
        )
    );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '   → % çakışan ProductCategory silindi', v_products_to_delete;
    
    -- Kalan ProductCategories'ları BATCH'ler halinde güncelle
    SELECT COUNT(*) INTO v_count FROM productcategories_mapping;
    SELECT CEIL(v_count::numeric / v_batch_size) INTO v_batch_count;
    RAISE NOTICE '   → % ProductCategory batch''ler halinde güncellenecek (% batch)', v_count, v_batch_count;
    
    v_processed := 0;
    v_current_batch := 0;
    v_products_to_delete := 0;
    
    CREATE TEMP TABLE pcm_ordered ON COMMIT DROP AS
    SELECT "Id", "NewProductId", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM productcategories_mapping;
    
    CREATE INDEX idx_pcm_ordered_rn ON pcm_ordered (rn);
    ANALYZE pcm_ordered;
    
    WHILE v_processed < v_count LOOP
        v_current_batch := v_current_batch + 1;
        IF v_current_batch % 10 = 0 OR v_processed = 0 THEN
            v_percent := ROUND((v_processed::numeric / v_count * 100)::numeric);
            RAISE NOTICE '   → ProductCategory batch %/% işleniyor... (%/% kayıt, %%%)', 
                v_current_batch, v_batch_count, v_processed, v_count, v_percent;
        END IF;
        
        UPDATE "ProductCategories" pc
        SET "ProductId" = pcm."NewProductId"
        FROM productcategories_mapping pcm
        JOIN pcm_ordered pcmo ON pcmo."Id" = pcm."Id"
        WHERE pc."Id" = pcm."Id"
          AND pcmo.rn > v_processed AND pcmo.rn <= v_processed + v_batch_size;
        
        GET DIAGNOSTICS v_batch_deleted = ROW_COUNT;
        v_products_to_delete := v_products_to_delete + v_batch_deleted;
        v_processed := v_processed + v_batch_size;
    END LOOP;
    
    RAISE NOTICE '✅ % ProductCategory güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 6. ProductGroupCodes'ları güncelle (ULTRA FAST: Pre-computed mapping)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 ProductGroupCodes güncelleniyor (ultra-fast pre-computed mode)...';
    
    -- Önce tüm ProductGroupCodes mapping'ini hazırla
    CREATE TEMP TABLE productgroupcodes_mapping_raw ON COMMIT DROP AS
    SELECT 
        pg."Id",
        pg."ProductId" AS "OldProductId",
        ptk."ProductId" AS "NewProductId",
        pg."GroupCode"
    FROM "ProductGroupCodes" pg
    JOIN duplicate_products_by_oems dp ON dp."ProductId" = pg."ProductId"
    JOIN products_to_keep_by_oems ptk ON ptk."Oems" = dp."Oems" AND ptk."SellerId" = dp."SellerId";
    
    -- Aynı NewProductId + GroupCode kombinasyonuna sahip kayıtları filtrele
    CREATE TEMP TABLE productgroupcodes_mapping ON COMMIT DROP AS
    SELECT DISTINCT ON (pgcm."NewProductId", pgcm."GroupCode")
        pgcm."Id",
        pgcm."OldProductId",
        pgcm."NewProductId",
        pgcm."GroupCode"
    FROM productgroupcodes_mapping_raw pgcm
    ORDER BY pgcm."NewProductId", pgcm."GroupCode", pgcm."Id" DESC;
    
    CREATE INDEX idx_pgcm_id ON productgroupcodes_mapping ("Id");
    CREATE INDEX idx_pgcm_newproduct_groupcode ON productgroupcodes_mapping ("NewProductId", "GroupCode");
    ANALYZE productgroupcodes_mapping;
    
    RAISE NOTICE '   → % ProductGroupCode mapping hazırlandı (duplicate''ler filtrelendi)', (SELECT COUNT(*) FROM productgroupcodes_mapping);
    
    -- Çakışmaları sil
    DELETE FROM "ProductGroupCodes" pg
    WHERE pg."Id" IN (
        SELECT pgcm."Id"
        FROM productgroupcodes_mapping pgcm
        WHERE EXISTS (
            SELECT 1 FROM "ProductGroupCodes" pg2 
            WHERE pg2."ProductId" = pgcm."NewProductId" 
              AND pg2."GroupCode" = pgcm."GroupCode"
        )
    );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '   → % çakışan ProductGroupCode silindi', v_products_to_delete;
    
    -- Kalan ProductGroupCodes'ları BATCH'ler halinde güncelle
    SELECT COUNT(*) INTO v_count FROM productgroupcodes_mapping;
    SELECT CEIL(v_count::numeric / v_batch_size) INTO v_batch_count;
    RAISE NOTICE '   → % ProductGroupCode batch''ler halinde güncellenecek (% batch)', v_count, v_batch_count;
    
    v_processed := 0;
    v_current_batch := 0;
    v_products_to_delete := 0;
    
    CREATE TEMP TABLE pgcm_ordered ON COMMIT DROP AS
    SELECT "Id", "NewProductId", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM productgroupcodes_mapping;
    
    CREATE INDEX idx_pgcm_ordered_rn ON pgcm_ordered (rn);
    ANALYZE pgcm_ordered;
    
    WHILE v_processed < v_count LOOP
        v_current_batch := v_current_batch + 1;
        IF v_current_batch % 10 = 0 OR v_processed = 0 THEN
            v_percent := ROUND((v_processed::numeric / v_count * 100)::numeric);
            RAISE NOTICE '   → ProductGroupCode batch %/% işleniyor... (%/% kayıt, %%%)', 
                v_current_batch, v_batch_count, v_processed, v_count, v_percent;
        END IF;
        
        UPDATE "ProductGroupCodes" pg
        SET "ProductId" = pgcm."NewProductId"
        FROM productgroupcodes_mapping pgcm
        JOIN pgcm_ordered pgcmo ON pgcmo."Id" = pgcm."Id"
        WHERE pg."Id" = pgcm."Id"
          AND pgcmo.rn > v_processed AND pgcmo.rn <= v_processed + v_batch_size;
        
        GET DIAGNOSTICS v_batch_deleted = ROW_COUNT;
        v_products_to_delete := v_products_to_delete + v_batch_deleted;
        v_processed := v_processed + v_batch_size;
    END LOOP;
    
    RAISE NOTICE '✅ % ProductGroupCode güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 7. PriceListItems'ları güncelle (ULTRA FAST: Pre-computed mapping)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔄 PriceListItems güncelleniyor (ultra-fast pre-computed mode)...';
    
    -- Önce tüm PriceListItems mapping'ini hazırla
    CREATE TEMP TABLE pricelistitems_mapping_raw ON COMMIT DROP AS
    SELECT 
        pli."Id",
        pli."ProductId" AS "OldProductId",
        ptk."ProductId" AS "NewProductId",
        pli."PriceListId"
    FROM "PriceListItems" pli
    JOIN duplicate_products_by_oems dp ON dp."ProductId" = pli."ProductId"
    JOIN products_to_keep_by_oems ptk ON ptk."Oems" = dp."Oems" AND ptk."SellerId" = dp."SellerId"
    WHERE pli."ProductId" IS NOT NULL;
    
    -- Aynı NewProductId + PriceListId kombinasyonuna sahip kayıtları filtrele
    CREATE TEMP TABLE pricelistitems_mapping ON COMMIT DROP AS
    SELECT DISTINCT ON (plim."NewProductId", plim."PriceListId")
        plim."Id",
        plim."OldProductId",
        plim."NewProductId",
        plim."PriceListId"
    FROM pricelistitems_mapping_raw plim
    ORDER BY plim."NewProductId", plim."PriceListId", plim."Id" DESC;
    
    CREATE INDEX idx_plim_id ON pricelistitems_mapping ("Id");
    ANALYZE pricelistitems_mapping;
    
    RAISE NOTICE '   → % PriceListItem mapping hazırlandı (duplicate''ler filtrelendi)', (SELECT COUNT(*) FROM pricelistitems_mapping);
    
    -- Çakışmaları sil (hedef Product'ta aynı PriceListId varsa)
    DELETE FROM "PriceListItems" pli
    WHERE pli."Id" IN (
        SELECT plim."Id"
        FROM pricelistitems_mapping plim
        WHERE EXISTS (
            SELECT 1 FROM "PriceListItems" pli2 
            WHERE pli2."ProductId" = plim."NewProductId" 
              AND pli2."PriceListId" = plim."PriceListId"
        )
    );
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    RAISE NOTICE '   → % çakışan PriceListItem silindi', v_products_to_delete;
    
    -- Kalan PriceListItems'ları BATCH'ler halinde güncelle
    SELECT COUNT(*) INTO v_count FROM pricelistitems_mapping;
    SELECT CEIL(v_count::numeric / v_batch_size) INTO v_batch_count;
    RAISE NOTICE '   → % PriceListItem batch''ler halinde güncellenecek (% batch)', v_count, v_batch_count;
    
    v_processed := 0;
    v_current_batch := 0;
    v_products_to_delete := 0;
    
    CREATE TEMP TABLE plim_ordered ON COMMIT DROP AS
    SELECT "Id", "NewProductId", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM pricelistitems_mapping;
    
    CREATE INDEX idx_plim_ordered_rn ON plim_ordered (rn);
    ANALYZE plim_ordered;
    
    WHILE v_processed < v_count LOOP
        v_current_batch := v_current_batch + 1;
        IF v_current_batch % 10 = 0 OR v_processed = 0 THEN
            v_percent := ROUND((v_processed::numeric / v_count * 100)::numeric);
            RAISE NOTICE '   → PriceListItem batch %/% işleniyor... (%/% kayıt, %%%)', 
                v_current_batch, v_batch_count, v_processed, v_count, v_percent;
        END IF;
        
        UPDATE "PriceListItems" pli
        SET "ProductId" = plim."NewProductId",
            "ModifiedDate" = NOW()
        FROM pricelistitems_mapping plim
        JOIN plim_ordered plimo ON plimo."Id" = plim."Id"
        WHERE pli."Id" = plim."Id"
          AND plimo.rn > v_processed AND plimo.rn <= v_processed + v_batch_size;
        
        GET DIAGNOSTICS v_batch_deleted = ROW_COUNT;
        v_products_to_delete := v_products_to_delete + v_batch_deleted;
        v_processed := v_processed + v_batch_size;
    END LOOP;
    
    RAISE NOTICE '✅ % PriceListItem güncellendi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 8. Son olarak mükerrer Product'ları sil (ULTRA FAST: Foreign key constraint'leri dinamik olarak drop)
    v_step_start := clock_timestamp();
    RAISE NOTICE '🗑️ Mükerrer Product kayıtları siliniyor (ultra-fast mode: FK constraint''leri dinamik olarak drop)...';
    
    -- Foreign key constraint'lerini dinamik olarak bul ve geçici olarak drop et
    -- NOT: Tüm ilişkili kayıtları zaten güncelledik, bu yüzden güvenli
    CREATE TEMP TABLE fk_constraints_to_drop ON COMMIT DROP AS
    SELECT 
        tc.table_name,
        tc.constraint_name,
        'ALTER TABLE "' || tc.table_name || '" DROP CONSTRAINT IF EXISTS "' || tc.constraint_name || '";' AS drop_sql,
        'ALTER TABLE "' || tc.table_name || '" ADD CONSTRAINT "' || tc.constraint_name || '" ' ||
        'FOREIGN KEY ("' || kcu.column_name || '") REFERENCES "' || ccu.table_name || '"("' || ccu.column_name || '");' AS add_sql
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name = tc.constraint_name
    WHERE tc.constraint_type = 'FOREIGN KEY'
      AND ccu.table_name = 'Product'
      AND ccu.column_name = 'Id'
      AND tc.table_name NOT IN ('SellerItems', 'ProductImages', 'ProductCategories', 'ProductGroupCodes', 'PriceListItems');
    
    -- Constraint'leri drop et
    FOR v_drop_sql IN SELECT drop_sql FROM fk_constraints_to_drop LOOP
        EXECUTE v_drop_sql;
    END LOOP;
    
    RAISE NOTICE '   → % foreign key constraint geçici olarak drop edildi', (SELECT COUNT(*) FROM fk_constraints_to_drop);
    
    -- Tüm mükerrer Product'ları tek seferde sil (çok daha hızlı)
    DELETE FROM "Product" p
    WHERE p."Id" IN (SELECT "ProductId" FROM duplicate_products_by_oems);
    
    GET DIAGNOSTICS v_products_to_delete = ROW_COUNT;
    
    -- Foreign key constraint'lerini tekrar create et
    FOR v_add_sql IN SELECT add_sql FROM fk_constraints_to_drop LOOP
        BEGIN
            EXECUTE v_add_sql;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE '   ⚠️ Constraint yeniden oluşturulamadı: %', SQLERRM;
        END;
    END LOOP;
    
    RAISE NOTICE '✅ % mükerrer Product silindi (% saniye)', 
        v_products_to_delete, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 9. Özet
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ OEMS BAZLI TEMİZLEME İŞLEMİ TAMAMLANDI';
    RAISE NOTICE '   Silinen Product: %', v_products_to_delete;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', 
        ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
END $$;
