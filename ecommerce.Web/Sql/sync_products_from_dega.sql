CREATE OR REPLACE PROCEDURE "public"."sync_products_from_dega"()
AS $BODY$
DECLARE
    seller_id INTEGER := 3; -- Dega SellerId
    seller_commission INTEGER;
    v_start_time TIMESTAMP := clock_timestamp();
    v_step_start TIMESTAMP;
    v_new_products INTEGER := 0;
    v_inserted_oems INTEGER := 0;
    v_seller_items INTEGER := 0;
BEGIN
    -- 0. TERA-NITRO TUNING
    SET LOCAL lock_timeout = '30s';
    SET LOCAL work_mem = '512MB';
    SET LOCAL synchronous_commit = OFF;
    SET LOCAL maintenance_work_mem = '512MB';
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 TERA-NITRO SYNC: Dega (Bulletproof)';
    RAISE NOTICE '   Start Time: %', v_start_time;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    
    -- 1. DISABLE TRIGGERS
    BEGIN
        ALTER TABLE "Product" DISABLE TRIGGER ALL;
        ALTER TABLE "ProductGroupCodes" DISABLE TRIGGER ALL;
        ALTER TABLE "SellerItems" DISABLE TRIGGER ALL;
        
        DROP INDEX IF EXISTS "idx_product_name_trgm";
        DROP INDEX IF EXISTS "idx_productgroupcodes_groupcode_gin";
        DROP INDEX IF EXISTS "idx_pg_groupcode_norm";
        DROP INDEX IF EXISTS "idx_productgroupcodes_code";
        DROP INDEX IF EXISTS "idx_pgc_lookup";
    EXCEPTION WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Warning: Optimization steps failed.';
    END;

    -- 2. Commission
    SELECT "Commission" INTO seller_commission FROM "Sellers" WHERE "Id" = seller_id;

    -- 3. Raw Data
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_incoming ON COMMIT DROP AS
    SELECT
        pd."Id"::TEXT AS "SourceId",
        INITCAP(TRIM(pd."Name")) AS "ProductName",
        TRIM(pd."Manufacturer") AS "BrandName",
        UPPER(TRIM(CONCAT_WS(' ', pd."Code", pd."OrjinalKod", pd."SpecialField9"))) AS "NormalizedOem", 
        pd."SalePriceContact" AS "Price",
        CASE
            WHEN UPPER(TRIM(pd."SalePriceContactCurrency")) IN ('₺', 'TL', 'TRY') THEN 'TRY'
            WHEN UPPER(TRIM(pd."SalePriceContactCurrency")) IN ('$', 'USD') THEN 'USD'
            WHEN UPPER(TRIM(pd."SalePriceContactCurrency")) IN ('€', 'EUR') THEN 'EUR'
            ELSE 'TRY'
        END AS "Currency",
        (
            CASE WHEN pd."Depo_1" ~ '^[0-9]+$' THEN pd."Depo_1"::int WHEN LOWER(TRIM(pd."Depo_1")) = 'var' THEN 5 ELSE 0 END +
            CASE WHEN pd."Depo_2" ~ '^[0-9]+$' THEN pd."Depo_2"::int WHEN LOWER(TRIM(pd."Depo_2")) = 'var' THEN 5 ELSE 0 END +
            CASE WHEN pd."Depo_3" ~ '^[0-9]+$' THEN pd."Depo_3"::int WHEN LOWER(TRIM(pd."Depo_3")) = 'var' THEN 5 ELSE 0 END +
            CASE WHEN pd."Depo_4" ~ '^[0-9]+$' THEN pd."Depo_4"::int WHEN LOWER(TRIM(pd."Depo_4")) = 'var' THEN 5 ELSE 0 END +
            CASE WHEN pd."Depo_5" ~ '^[0-9]+$' THEN pd."Depo_5"::int WHEN LOWER(TRIM(pd."Depo_5")) = 'var' THEN 5 ELSE 0 END +
            CASE WHEN pd."Depo_6" ~ '^[0-9]+$' THEN pd."Depo_6"::int WHEN LOWER(TRIM(pd."Depo_6")) = 'var' THEN 5 ELSE 0 END
        ) AS "Stock",
        1 as "CreatedId", 
        NOW() as "CreatedDate"
    FROM "ProductDegas" pd
    WHERE pd."Name" IS NOT NULL AND pd."SalePriceContact" IS NOT NULL;

    GET DIAGNOSTICS v_seller_items = ROW_COUNT;
    RAISE NOTICE '   - 📥 Ham veri alındı: % kayıt. [%]', v_seller_items, clock_timestamp() - v_step_start;

    -- 4. Brands
    INSERT INTO "Brand" ("Name", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT DISTINCT ON (TRIM("BrandName")) TRIM("BrandName"), 1, 1, NOW(), 1
    FROM tmp_incoming WHERE "BrandName" IS NOT NULL AND "BrandName" <> ''
    ON CONFLICT ("BranchId", "Name") DO NOTHING;

    -- 4. STEP: MAPPING & MATCHING
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_mapping ON COMMIT DROP AS
    SELECT 
        src."SourceId" AS source_id,
        src."ProductName" AS product_name,
        src."BrandName" AS brand_raw,
        src."NormalizedOem" AS normalized_oem,
        src."Price" AS raw_price,
        src."Currency" AS currency,
        src."Stock" AS stock,
        1 AS min_amount,
        b."Id" as brand_id,
        si."ProductId" as product_id,
        FALSE as is_new -- Marker for Fast Path
    FROM tmp_incoming src
    LEFT JOIN "Brand" b ON b."Name" = src."BrandName"
    LEFT JOIN "SellerItems" si ON si."SellerId" = seller_id AND si."SourceId" = src."SourceId";

    RAISE NOTICE '🔍 Adım 4: Haritalama bitti. (Mevcut bağlantılar kontrol edildi) [%]', clock_timestamp() - v_step_start;

    -- 5. STEP: INSERT PRODUCTS
    v_step_start := clock_timestamp();
    
    INSERT INTO "Product" (
        "Name", "Status", "CreatedId", "CreatedDate", "ProductTypeId", "TaxId", "BrandId",
        "CartMinValue", "Weight", "Width", "Length", "Height", "CargoDesi", "RetailPrice", 
        "IsNewsProduct", "IsCustomerCreated", "IsGift", "Barcode", "BranchId"
    )
    SELECT 
        product_name, 1, 1, NOW(), 173561, 6, COALESCE(brand_id, 1),
        1, 1, 1, 1, 1, 1, 0, 
        FALSE, FALSE, FALSE, 'TMP-DEGA-' || source_id, 1
    FROM (
        SELECT DISTINCT ON (source_id) source_id, product_name, brand_id
        FROM tmp_mapping WHERE product_id IS NULL
    ) distinct_new;

    GET DIAGNOSTICS v_new_products = ROW_COUNT;

    -- Resolve the new Product IDs back to mapping table
    UPDATE tmp_mapping tm
    SET product_id = p."Id",
        is_new = TRUE
    FROM "Product" p
    WHERE p."Barcode" = 'TMP-DEGA-' || tm.source_id 
      AND tm.product_id IS NULL;

    -- Clear Temporary Barcodes
    UPDATE "Product" SET "Barcode" = NULL WHERE "Barcode" LIKE 'TMP-DEGA-%';
    
    -- Insert ProductUnits for new products (UnitId=2 is 'Adet', UnitValue=1)
    INSERT INTO "ProductUnits" ("ProductId", "UnitId", "UnitValue", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT product_id, 2, 1, 1, 1, NOW(), 1
    FROM tmp_mapping
    WHERE product_id IS NOT NULL AND is_new = TRUE
    ON CONFLICT ("ProductId", "UnitId") DO NOTHING;

    RAISE NOTICE '📦 Adım 5: Yeni ürünler kataloğa eklendi. [+% ürün, %]', v_new_products, clock_timestamp() - v_step_start;

    -- 6. STEP: SELLER SYNC
    v_step_start := clock_timestamp();
    RAISE NOTICE '📝 Adım 6: Satıcı stok/fiyat bilgileri hazırlanıyor...';

    CREATE TEMP TABLE tmp_verified_links ON COMMIT DROP AS
    SELECT 
        src.source_id, src.brand_raw, src.product_id,
        src.stock, src.raw_price, fx."ForexSelling" as rate,
        src.currency, seller_commission as commission, src.min_amount -- Corrected v_commission to seller_commission
    FROM tmp_mapping src
    LEFT JOIN (
        SELECT DISTINCT ON ("CurrencyCode") "CurrencyCode", "ForexSelling" 
        FROM "Currencies" ORDER BY "CurrencyCode", "CreatedDate" DESC
    ) fx ON fx."CurrencyCode" = src.currency
    WHERE src.product_id IS NOT NULL;

    -- Clean up identities (OEM codes) - We insert them for the NEW products too
    -- 7. OEM Mapping
    v_step_start := clock_timestamp();
    RAISE NOTICE '📝 Adım 6.1: OEM kodları işleniyor (Batch Mode: 10k)...';

    -- First, create a temp table for distinct OEM codes from incoming data
    CREATE TEMP TABLE tmp_distinct_oems ON COMMIT DROP AS
    SELECT DISTINCT product_id, normalized_oem AS oem_code, is_new
    FROM tmp_mapping
    WHERE product_id IS NOT NULL AND normalized_oem IS NOT NULL AND normalized_oem <> '';

    -- Add an ID to easily batch processing
    ALTER TABLE tmp_distinct_oems ADD COLUMN _id SERIAL PRIMARY KEY;
    
    DECLARE 
        _min_id INT;
        _max_id INT;
        _batch_size INT := 10000;
        _current_id INT;
        _next_id INT;
        _total_inserted INT := 0;
        _batch_inserted INT;
    BEGIN
        SELECT MIN(_id), MAX(_id) INTO _min_id, _max_id FROM tmp_distinct_oems;
        _current_id := _min_id;

        WHILE _current_id <= _max_id LOOP
            _next_id := _current_id + _batch_size;

            -- A) FAST PATH: New Products
            INSERT INTO "ProductGroupCodes" ("ProductId", "OemCode", "SourceType", "Status", "CreatedId", "CreatedDate")
            SELECT tdo.product_id, tdo.oem_code, 'OEM', 1, 1, NOW()
            FROM tmp_distinct_oems tdo
            WHERE tdo._id >= _current_id AND tdo._id < _next_id
              AND tdo.is_new = TRUE;
            
            GET DIAGNOSTICS _batch_inserted = ROW_COUNT;
            _total_inserted := _total_inserted + _batch_inserted;

            -- B) SLOW PATH: Existing Products
            INSERT INTO "ProductGroupCodes" ("ProductId", "OemCode", "SourceType", "Status", "CreatedId", "CreatedDate")
            SELECT tdo.product_id, tdo.oem_code, 'OEM', 1, 1, NOW()
            FROM tmp_distinct_oems tdo
            LEFT JOIN "ProductGroupCodes" existing 
                ON existing."ProductId" = tdo.product_id 
                AND existing."OemCode" = tdo.oem_code
            WHERE tdo._id >= _current_id AND tdo._id < _next_id
              AND tdo.is_new = FALSE 
              AND existing."Id" IS NULL;

            GET DIAGNOSTICS _batch_inserted = ROW_COUNT;
            _total_inserted := _total_inserted + _batch_inserted;

            RAISE NOTICE '   - 📦 Batch İlerlemesi: % kayıt işlendi...', _total_inserted;
            
            _current_id := _next_id;
        END LOOP;
        
        -- Export to outer scope variable
        v_inserted_oems := _total_inserted;
    END;
    
    RAISE NOTICE '   - OEM Bağlantıları: %', v_inserted_oems;

    INSERT INTO "SellerItems" (
        "SellerId", "ProductId", "SourceId", "ManufacturerName",
        "Stock", "CostPrice", "SalePrice", "Commision", "Currency", "Unit", 
        "Status", "CreatedId", "CreatedDate", "Step", "MinSaleAmount", "MaxSaleAmount"
    )
    SELECT DISTINCT ON (seller_id, product_id, source_id)
           seller_id, product_id, source_id, brand_raw,
           stock, ROUND(CASE WHEN currency IN ('TL', 'TRY') THEN raw_price ELSE raw_price * COALESCE(NULLIF(rate, 0), 1) END, 2),
           ROUND((CASE WHEN currency IN ('TL', 'TRY') THEN raw_price ELSE raw_price * COALESCE(NULLIF(rate, 0), 1) END) * (1 + commission/100.0), 2),
           commission, 'TRY', 'Adet', 1, 1, NOW(), 1, min_amount, 0
    FROM tmp_verified_links
    ORDER BY seller_id, product_id, source_id, stock DESC
    ON CONFLICT ("SellerId", "ProductId", "SourceId") DO UPDATE SET
        "Stock" = EXCLUDED."Stock", "CostPrice" = EXCLUDED."CostPrice", "SalePrice" = EXCLUDED."SalePrice", "ModifiedDate" = NOW(), "Status" = 1;
    GET DIAGNOSTICS v_seller_items = ROW_COUNT;
    RAISE NOTICE '💰 Adım 6: Satıcı fiyat ve stokları güncellendi. [% kayıt, %]', v_seller_items, clock_timestamp() - v_step_start;

    -- 8. RESTORE
    ALTER TABLE "Product" ENABLE TRIGGER ALL;
    ALTER TABLE "ProductGroupCodes" ENABLE TRIGGER ALL;
    ALTER TABLE "SellerItems" ENABLE TRIGGER ALL;

    CREATE INDEX IF NOT EXISTS "idx_product_name_trgm" ON public."Product" USING gin ("Name" gin_trgm_ops);
    CREATE INDEX IF NOT EXISTS "idx_productgroupcodes_code" ON public."ProductGroupCodes" ("OemCode");
    CREATE INDEX IF NOT EXISTS "idx_pgc_lookup" ON public."ProductGroupCodes" ("OemCode");
    CREATE INDEX IF NOT EXISTS "idx_productgroupcodes_groupcode_gin" ON public."ProductGroupCodes" USING gin (string_to_array("OemCode", '|'::text));
    CREATE INDEX IF NOT EXISTS "idx_pg_groupcode_norm" ON public."ProductGroupCodes" (regexp_replace(upper("OemCode"), '[^A-Z0-9]', '', 'g'));

    RAISE NOTICE '🏁 TERA-NITRO DEGA COMPLETED. Products: %, OEMs: %, Items: %, Time: %s', 
                 v_new_products, v_inserted_oems, v_seller_items, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
END;
$BODY$
LANGUAGE plpgsql;
