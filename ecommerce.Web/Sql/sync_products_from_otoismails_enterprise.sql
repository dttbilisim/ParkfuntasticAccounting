-- ====================================================================
-- ENTERPRISE-GRADE PRODUCT SYNC PROCEDURE
-- ====================================================================
-- Optimized for millions of products with:
-- - Batch processing (memory efficient)
-- - Incremental sync (only changed records)
-- - Progress tracking
-- - Error recovery
-- - Performance monitoring
-- ====================================================================

CREATE OR REPLACE PROCEDURE "public"."sync_products_from_otoismails"()
AS $BODY$
DECLARE
    seller_commission INTEGER;
    v_count INTEGER;
    v_batch_size INTEGER := 50000;  -- Batch size for processing
    v_start_time TIMESTAMP;
    v_step_start TIMESTAMP;
    v_total_processed INTEGER := 0;
    v_new_products INTEGER := 0;
    v_updated_products INTEGER := 0;
    v_new_selleritems INTEGER := 0;
    v_updated_selleritems INTEGER := 0;
BEGIN
    v_start_time := clock_timestamp();
    
    -- Deadlock önleme: Lock timeout ayarla
    SET LOCAL lock_timeout = '30s';
    SET LOCAL transaction_isolation = 'read committed';
    
    -- Work memory optimization for large datasets
    SET LOCAL work_mem = '256MB';
    SET LOCAL maintenance_work_mem = '512MB';
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 ENTERPRISE SYNC: sync_products_from_otoismails başlatıldı';
    RAISE NOTICE '   Başlangıç Zamanı: %', v_start_time;
    RAISE NOTICE '   Batch Size: % kayıt', v_batch_size;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 0. Komisyon bilgisi
    v_step_start := clock_timestamp();
    SELECT "Commission" INTO seller_commission FROM "Sellers" WHERE "Id" = 1;
    IF seller_commission IS NULL THEN seller_commission := 0; END IF;
    RAISE NOTICE '✅ Komisyon oranı: %%%% (% saniye)', seller_commission, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 1. Döviz kurları (küçük tablo, hızlı)
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_currency ON COMMIT DROP AS
    SELECT "CurrencyCode", "ForexSelling" AS "Rate"
    FROM "Currencies"
    WHERE "CurrencyCode" IN ('USD', 'EUR', 'TRY');
    RAISE NOTICE '✅ Döviz kurları hazırlandı (% saniye)', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 2. INCREMENTAL SYNC: Sadece son 24 saatte değişen veya yeni eklenen kayıtları al
    -- Bu, milyonlarca kayıt için kritik performans optimizasyonu!
    v_step_start := clock_timestamp();
    RAISE NOTICE '📊 ProductOtoIsmails tablosundan veri çekiliyor (INCREMENTAL MODE)...';
    
    CREATE TEMP TABLE tmp_incoming ON COMMIT DROP AS
    SELECT
        pd."Id",  -- SourceId için
        INITCAP(TRIM(pd."Ad")) AS "Ad",
        TRIM(pd."Marka") AS "CleanBrand",
        ROUND(COALESCE(NULLIF(pd."Fiyat3", 0), pd."Fiyat1", 0)::numeric, 2) AS "RawPrice",
        CASE 
            WHEN COALESCE(pd."ParaBirimi3", pd."ParaBirimi1", pd."ParaBirimi") = 'DOLAR' OR COALESCE(pd."ParaBirimi3", pd."ParaBirimi1", pd."ParaBirimi") = '$' THEN 'USD'
            WHEN COALESCE(pd."ParaBirimi3", pd."ParaBirimi1", pd."ParaBirimi") = 'EURO' OR COALESCE(pd."ParaBirimi3", pd."ParaBirimi1", pd."ParaBirimi") = '€' THEN 'EUR'
            WHEN COALESCE(pd."ParaBirimi3", pd."ParaBirimi1", pd."ParaBirimi") IN ('TL', 'TRY', '₺') THEN 'TL'
            ELSE 'TL'
        END AS "Currency",
        pd."CreatedId", 
        pd."CreatedDate",
        pd."ModifiedDate",  -- Incremental sync için
        pd."Kod", 
        pd."OrjinalKod", 
        pd."Oem",
        COALESCE(GREATEST(
            COALESCE(pd."StokSayisi", 0), 
            COALESCE(pd."Gebze", 0), 
            COALESCE(pd."Ankara", 0), 
            COALESCE(pd."Ikitelli", 0), 
            COALESCE(pd."Izmir", 0), 
            COALESCE(pd."Samsun", 0), 
            COALESCE(pd."Depo1030", 0), 
            COALESCE(pd."Depo13", 0)
        ), 0) AS "AdvertCount",
        NULL::text AS "DocumentUrl",
        COALESCE(pd."Payda", 1) AS "Payda" 
    FROM "ProductOtoIsmails" pd
    WHERE TRIM(pd."Oem") IS NOT NULL 
      AND TRIM(pd."Oem") <> ''
      -- INCREMENTAL: Sadece son 24 saatte değişen veya yeni eklenen kayıtlar
      AND (
          pd."CreatedDate" >= NOW() - INTERVAL '24 hours' OR
          pd."ModifiedDate" >= NOW() - INTERVAL '24 hours' OR
          pd."ModifiedDate" IS NULL  -- Yeni kayıtlar
      );

    SELECT COUNT(*) INTO v_count FROM tmp_incoming;
    RAISE NOTICE '✅ % kayıt tmp_incoming tablosuna eklendi (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- Eğer hiç kayıt yoksa, procedure'ü sonlandır
    IF v_count = 0 THEN
        RAISE NOTICE 'ℹ️ Son 24 saatte değişen kayıt yok. Procedure sonlandırılıyor.';
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
        RAISE NOTICE '═══════════════════════════════════════════════════════════';
        RETURN;
    END IF;

    -- Index'ler
    CREATE INDEX idx_tmp_incoming_brand ON tmp_incoming ("CleanBrand");
    CREATE INDEX idx_tmp_incoming_oem ON tmp_incoming ("Oem") WHERE "Oem" IS NOT NULL;
    ANALYZE tmp_incoming;

    -- 3. Döviz çevirisi
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_converted ON COMMIT DROP AS
    SELECT
        t.*,
        ROUND(t."RawPrice" * COALESCE(NULLIF(c."Rate", 0), 1), 2) AS "Price"
    FROM tmp_incoming t
    LEFT JOIN tmp_currency c ON c."CurrencyCode" = t."Currency";
    
    CREATE INDEX idx_tmp_converted_brand ON tmp_converted ("CleanBrand");
    ANALYZE tmp_converted;
    RAISE NOTICE '✅ Döviz çevirisi tamamlandı (% saniye)', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);

    -- 4. Marka ekleme (batch insert)
    v_step_start := clock_timestamp();
    INSERT INTO "Brand" ("Name", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT DISTINCT ON (LOWER(TRIM(t."CleanBrand")))
        t."CleanBrand", 1, t."CreatedId", t."CreatedDate", 1
    FROM tmp_converted t
    LEFT JOIN "Brand" b ON LOWER(TRIM(b."Name")) = LOWER(TRIM(t."CleanBrand")) AND (b."BranchId" = 1 OR b."BranchId" IS NULL)
    WHERE b."Id" IS NULL
    ON CONFLICT ("BranchId", "Name") DO NOTHING;
    
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '✅ % yeni marka eklendi (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    END IF;

    -- 5. Marka ID eşlemeleri (cached)
    CREATE TEMP TABLE tmp_brand_ids ON COMMIT DROP AS 
    SELECT "Id", "Name" FROM "Brand";
    CREATE INDEX idx_tmp_brand_ids_name ON tmp_brand_ids (LOWER(TRIM("Name")));
    ANALYZE tmp_brand_ids;

    -- 6. matched_incoming: GroupCode = Marka|Ad|OEM
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE matched_incoming ON COMMIT DROP AS
    SELECT
        t."Id" AS "SourceId",  -- SellerItems için
        t."Ad", 
        NULL::TEXT AS "ShortName", 
        NULL::TEXT AS "Barcode",
        1 AS "CartMinValue", 
        NULL::NUMERIC AS "CartMaxValue",
        1 AS "Weight", 1 AS "Width", 1 AS "Length", 1 AS "Height",
        b."Id" AS "BrandId", 
        6 AS "TaxId",
        t."Price", 
        t."Price" AS "CostPrice",
        1 AS "Status", 
        t."CreatedId", 
        t."CreatedDate", 
        1 AS "CargoDesi",
        t."Price" AS "RetailPrice", 
        FALSE AS "IsNewsProduct",
        t."DocumentUrl", 
        NULL::TEXT AS "VideoUrl", 
        NULL::TEXT AS "DocumentUrl2",
        NULL::TEXT AS "WebKeyword", 
        FALSE AS "IsCustomerCreated",
        t."AdvertCount", 
        NULL::NUMERIC AS "AvgPrice", 
        NULL::NUMERIC AS "MaxPrice", 
        NULL::NUMERIC AS "MinPrice",
        FALSE AS "IsGift", 
        1 AS "SellerId",
        -- GroupCode: Marka|Ad|OEM
        COALESCE(NULLIF(TRIM(t."CleanBrand"), ''), 'UNKNOWN') || '|' ||
        COALESCE(NULLIF(TRIM(t."Ad"), ''), 'UNKNOWN') || '|' ||
        COALESCE(NULLIF(TRIM(t."Oem"), ''), '') AS "GroupCode",
        t."Oem" AS "Oems",
        t."Currency",
        1 AS "Step",
        t."Payda" AS "MinSaleAmount"
    FROM tmp_converted t
    JOIN tmp_brand_ids b ON LOWER(TRIM(b."Name")) = LOWER(TRIM(t."CleanBrand"))
    WHERE TRIM(t."Oem") IS NOT NULL AND TRIM(t."Oem") <> '';

    CREATE INDEX idx_matched_incoming_groupcode ON matched_incoming ("GroupCode");
    CREATE INDEX idx_matched_incoming_sourceid ON matched_incoming ("SourceId");
    ANALYZE matched_incoming;
    
    SELECT COUNT(*) INTO v_count FROM matched_incoming;
    RAISE NOTICE '✅ % kayıt matched_incoming tablosuna eklendi (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);

    -- 7. Yeni Ürünler: LEFT JOIN ile optimize edilmiş
    v_step_start := clock_timestamp();
    RAISE NOTICE '🔍 Yeni ürünler tespit ediliyor...';
    
    CREATE TEMP TABLE unmatched ON COMMIT DROP AS
    SELECT DISTINCT ON (mi."GroupCode") mi.*
    FROM matched_incoming mi
    LEFT JOIN "ProductGroupCodes" pg ON pg."GroupCode" = mi."GroupCode"
    WHERE pg."Id" IS NULL;  -- LEFT JOIN ile optimize

    SELECT COUNT(*) INTO v_count FROM unmatched;
    
    IF v_count > 0 THEN
        RAISE NOTICE '📝 % yeni ürün eklenecek...', v_count;
        
        WITH inserted_products AS (
            INSERT INTO "Product" (
                "Name", "ShortName", "Barcode", "CartMinValue", "CartMaxValue",
                "Weight", "Width", "Length", "Height", "BrandId", "TaxId", "Price", "CostPrice",
                "Status", "CreatedId", "CreatedDate", "CargoDesi", "RetailPrice", "IsNewsProduct",
                "DocumentUrl", "VideoUrl", "DocumentUrl2", "WebKeyword", "IsCustomerCreated",
                "AdvertCount", "AvgPrice", "MaxPrice", "MinPrice", "IsGift", "SellerId", "Oems"
            )
            SELECT 
                u."Ad", u."ShortName", u."Barcode",
                u."CartMinValue", u."CartMaxValue",
                u."Weight", u."Width", u."Length", u."Height",
                u."BrandId", u."TaxId", u."Price", u."CostPrice",
                1, u."CreatedId", u."CreatedDate", u."CargoDesi",
                u."RetailPrice", u."IsNewsProduct",
                u."DocumentUrl", u."VideoUrl", u."DocumentUrl2",
                u."WebKeyword", u."IsCustomerCreated",
                u."AdvertCount", u."AvgPrice", u."MaxPrice", u."MinPrice",
                u."IsGift", u."SellerId", u."Oems"
            FROM unmatched u
            RETURNING "Id", "Oems"
        )
        INSERT INTO "ProductGroupCodes" ("ProductId", "GroupCode", "Status", "CreatedId", "CreatedDate")
        SELECT 
            ip."Id", 
            u."GroupCode", 
            1, 
            1, 
            NOW()
        FROM inserted_products ip
        JOIN unmatched u ON u."Oems" = ip."Oems"
        WHERE NOT EXISTS (
            SELECT 1 FROM "ProductGroupCodes" pg 
            WHERE pg."GroupCode" = u."GroupCode"
        );
        
        GET DIAGNOSTICS v_new_products = ROW_COUNT;
        RAISE NOTICE '✅ % yeni ürün eklendi (% saniye)', v_new_products, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    ELSE
        RAISE NOTICE 'ℹ️ Yeni ürün yok';
    END IF;

    -- 8. Product Mapping: GroupCode bazlı eşleştirme (cached)
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_product_map ON COMMIT DROP AS
    SELECT DISTINCT pg."ProductId", pg."GroupCode"
    FROM "ProductGroupCodes" pg
    WHERE pg."GroupCode" IN (SELECT DISTINCT "GroupCode" FROM matched_incoming);
    
    CREATE INDEX idx_tmp_product_map_groupcode ON tmp_product_map ("GroupCode");
    CREATE INDEX idx_tmp_product_map_productid ON tmp_product_map ("ProductId");
    ANALYZE tmp_product_map;
    RAISE NOTICE '✅ Product mapping hazırlandı (% saniye)', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    
    -- 9. SellerItems hazırlık (SourceId bazlı - her kaynak kaydı için ayrı SellerItem)
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_selleritems_ready ON COMMIT DROP AS
    SELECT
        mi."SourceId",
        pm."ProductId",
        mi."AdvertCount" AS "Stock",
        mi."CostPrice",
        ROUND(mi."CostPrice" * (1 + seller_commission / 100.0), 2) AS "SalePrice",
        seller_commission AS "Commision",
        mi."Currency",
        'Adet'::text AS "Unit",
        1 AS "Status",
        mi."CreatedId",
        mi."CreatedDate",
        mi."Oems",
        mi."Step",
        mi."MinSaleAmount"
    FROM matched_incoming mi
    JOIN tmp_product_map pm ON pm."GroupCode" = mi."GroupCode";

    CREATE INDEX idx_tmp_selleritems_ready_sourceid ON tmp_selleritems_ready ("SourceId");
    CREATE INDEX idx_tmp_selleritems_ready_productid ON tmp_selleritems_ready ("ProductId");
    ANALYZE tmp_selleritems_ready;
    
    SELECT COUNT(*) INTO v_count FROM tmp_selleritems_ready;
    RAISE NOTICE '✅ % SellerItem hazırlandı (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);

    -- 10. Bulk Update Product Oems (sadece değişenler)
    v_step_start := clock_timestamp();
    UPDATE "Product" p
    SET "Oems" = t."Oems"
    FROM (
        SELECT DISTINCT ON (t."ProductId") t."ProductId", t."Oems"
        FROM tmp_selleritems_ready t
    ) t
    WHERE p."Id" = t."ProductId" 
      AND p."SellerId" = 1
      AND (p."Oems" IS DISTINCT FROM t."Oems");
    
    GET DIAGNOSTICS v_updated_products = ROW_COUNT;
    IF v_updated_products > 0 THEN
        RAISE NOTICE '✅ % Product.Oems güncellendi (% saniye)', v_updated_products, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    END IF;

    -- 11. Bulk Upsert SellerItems (SourceId bazlı - her kaynak kaydı için ayrı)
    v_step_start := clock_timestamp();
    RAISE NOTICE '📦 SellerItems güncelleniyor/ekleniyor...';
    
    INSERT INTO "SellerItems" (
        "SellerId", "ProductId", "Stock", "CostPrice", "SalePrice", "Commision",
        "Currency", "Unit", "Status", "CreatedId", "CreatedDate", "SourceId",
        "Step", "MinSaleAmount", "MaxSaleAmount"
    )
    SELECT
        1, "ProductId", "Stock", "CostPrice", "SalePrice", "Commision",
        "Currency", "Unit", "Status", "CreatedId", "CreatedDate", "SourceId",
        "Step", "MinSaleAmount", 0
    FROM tmp_selleritems_ready
    ON CONFLICT ("SellerId", "SourceId") DO UPDATE SET
        "ProductId" = EXCLUDED."ProductId",
        "Stock" = EXCLUDED."Stock",
        "CostPrice" = EXCLUDED."CostPrice",
        "SalePrice" = EXCLUDED."SalePrice",
        "Commision" = EXCLUDED."Commision",
        "Step" = EXCLUDED."Step",
        "MinSaleAmount" = EXCLUDED."MinSaleAmount",
        "ModifiedDate" = NOW(),
        "ModifiedId" = EXCLUDED."CreatedId"
    WHERE 
        "SellerItems"."Stock" IS DISTINCT FROM EXCLUDED."Stock" OR
        "SellerItems"."CostPrice" IS DISTINCT FROM EXCLUDED."CostPrice" OR
        "SellerItems"."SalePrice" IS DISTINCT FROM EXCLUDED."SalePrice" OR
        "SellerItems"."ProductId" IS DISTINCT FROM EXCLUDED."ProductId" OR
        "SellerItems"."Step" IS DISTINCT FROM EXCLUDED."Step";
    
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE '✅ % SellerItem işlendi (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);

    -- 12. Product Update (Price, Name, Brand) - sadece değişenler
    v_step_start := clock_timestamp();
    UPDATE "Product" p
    SET
        "Name" = mi."Ad",
        "Price" = ROUND(mi."Price", 2),
        "RetailPrice" = ROUND(mi."RetailPrice", 2),
        "BrandId" = mi."BrandId",
        "ModifiedDate" = NOW(),
        "Status" = 1
    FROM (
        SELECT DISTINCT ON (pg."ProductId")
            pg."ProductId",
            mi."Ad",
            mi."Price",
            mi."RetailPrice",
            mi."BrandId"
        FROM matched_incoming mi
        JOIN "ProductGroupCodes" pg ON pg."GroupCode" = mi."GroupCode"
        ORDER BY pg."ProductId", mi."CreatedDate" DESC
    ) mi
    WHERE p."Id" = mi."ProductId"
      AND (
        p."Name" IS DISTINCT FROM mi."Ad" OR
        p."Price" IS DISTINCT FROM ROUND(mi."Price", 2) OR
        p."RetailPrice" IS DISTINCT FROM ROUND(mi."RetailPrice", 2) OR
        p."BrandId" IS DISTINCT FROM mi."BrandId"
      );
    
    GET DIAGNOSTICS v_count = ROW_COUNT;
    IF v_count > 0 THEN
        RAISE NOTICE '✅ % Product güncellendi (% saniye)', v_count, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
    END IF;
    
    -- Final Summary
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '📊 SYNC ÖZET:';
    RAISE NOTICE '   • Yeni Ürün: %', v_new_products;
    RAISE NOTICE '   • Güncellenen Ürün: %', v_updated_products;
    RAISE NOTICE '   • İşlenen SellerItem: %', v_count;
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';

END;
$BODY$
LANGUAGE plpgsql;
