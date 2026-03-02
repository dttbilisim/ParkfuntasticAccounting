CREATE OR REPLACE PROCEDURE "public"."sync_products_from_remars"()
AS $BODY$
DECLARE
    seller_id INTEGER := 4; -- Remars SellerId
    seller_commission INTEGER;
    v_new_products INTEGER := 0;
    v_inserted_oems INTEGER := 0;
    v_seller_items INTEGER := 0;
    v_start_time TIMESTAMP := clock_timestamp();
    v_step_start TIMESTAMP;
BEGIN
    -- 0. TERA-NITRO TUNING
    SET LOCAL lock_timeout = '30s';
    SET LOCAL work_mem = '512MB';
    SET LOCAL synchronous_commit = OFF;
    SET LOCAL maintenance_work_mem = '512MB';
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 TERA-NITRO SYNC: Remars (Bulletproof)';
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
        r."Id"::TEXT AS "SourceId",
        INITCAP(LOWER(TRIM(r."Name"))) AS "ProductName",
        TRIM(LOWER(r."Manufacturer")) AS "NormalizedManufacturer",
        UPPER(TRIM(CONCAT_WS(' ', r."Code", r."Oem_No"))) AS "NormalizedOem",  
        r."SalePriceContact" AS "Price",
        CASE 
            WHEN UPPER(TRIM(r."SalePriceContactCurrency")) IN ('₺', 'TL', 'TRY') THEN 'TRY'
            WHEN UPPER(TRIM(r."SalePriceContactCurrency")) IN ('$', 'USD') THEN 'USD'
            WHEN UPPER(TRIM(r."SalePriceContactCurrency")) IN ('€', 'EUR') THEN 'EUR'
            ELSE 'TRY'
        END AS "Currency",
        r."MinOrderQuantity" AS "MinSaleAmount",
        (CASE WHEN r."Depo_1" ~ '^[0-9]+$' THEN r."Depo_1"::int WHEN LOWER(TRIM(r."Depo_1"))='var' THEN 5 ELSE 0 END +
         CASE WHEN r."Depo_2" ~ '^[0-9]+$' THEN r."Depo_2"::int WHEN LOWER(TRIM(r."Depo_2"))='var' THEN 5 ELSE 0 END) AS "Stock",
        1 as "CreatedId", 
        NOW() as "CreatedDate"
    FROM "ProductRemars" r
    WHERE TRIM(r."Oem_No") IS NOT NULL;

    GET DIAGNOSTICS v_seller_items = ROW_COUNT;
    RAISE NOTICE '   - 📥 Ham veri alındı: % kayıt. [%]', v_seller_items, clock_timestamp() - v_step_start;

    -- 4. Brands
    INSERT INTO "Brand" ("Name", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT DISTINCT ON (TRIM("NormalizedManufacturer"))
        INITCAP(TRIM("NormalizedManufacturer")), 1, 1, NOW(), 1
    FROM tmp_incoming WHERE "NormalizedManufacturer" IS NOT NULL
    ON CONFLICT ("BranchId", "Name") DO NOTHING;

    RAISE NOTICE '   - 🏭 Markalar işlendi. [%]', clock_timestamp() - v_step_start;

    -- 5. STEP: MAPPING & MATCHING
    v_step_start := clock_timestamp();
    CREATE TEMP TABLE tmp_mapping ON COMMIT DROP AS
    SELECT 
        src."SourceId" AS source_id,
        src."ProductName" AS product_name,
        src."NormalizedManufacturer" AS brand_raw, -- Corrected from BrandName
        src."NormalizedOem" AS normalized_oem,
        src."Price" AS raw_price,
        src."Currency" AS currency,
        src."Stock" AS stock,
        src."MinSaleAmount" AS min_amount,
        b."Id" as brand_id,
        si."ProductId" as product_id,
        FALSE as is_new -- Marker for Fast Path
    FROM tmp_incoming src
    LEFT JOIN "Brand" b ON b."Name" = src."NormalizedManufacturer"
    LEFT JOIN "SellerItems" si ON si."SellerId" = seller_id AND si."SourceId" = src."SourceId";

    RAISE NOTICE '🔍 Adım 5: Haritalama bitti. [%]', clock_timestamp() - v_step_start;

    -- 6. STEP: INSERT PRODUCTS
    v_step_start := clock_timestamp();
    
    INSERT INTO "Product" (
        "Name", "Barcode", "CartMinValue", "Weight", "Width", "Length", "Height",
        "BrandId", "TaxId", "ProductTypeId", "CostPrice", "Status", "CreatedId", "CreatedDate",
        "CargoDesi", "RetailPrice", "IsNewsProduct", "IsCustomerCreated", "IsGift", "BranchId"
    )
    SELECT 
        product_name, 'TMP-REMARS-' || source_id, min_amount, 1, 1, 1, 1,
        COALESCE(brand_id, 1), 6, 173561, 0, 1, 1, NOW(), 1, 0, FALSE, FALSE, FALSE, 1
    FROM (
        SELECT DISTINCT ON (source_id) source_id, product_name, brand_id, min_amount
        FROM tmp_mapping WHERE product_id IS NULL
    ) distinct_new;
    
    GET DIAGNOSTICS v_new_products = ROW_COUNT;

    -- Resolve IDs & MARK AS NEW
    UPDATE tmp_mapping tm
    SET product_id = p."Id",
        is_new = TRUE
    FROM "Product" p
    WHERE p."Barcode" = 'TMP-REMARS-' || tm.source_id 
      AND tm.product_id IS NULL;

    -- Clear Temporary Barcodes
    UPDATE "Product" SET "Barcode" = NULL WHERE "Barcode" LIKE 'TMP-REMARS-%';
    
    -- Insert ProductUnits for new products (UnitId=2 is 'Adet', UnitValue=1)
    INSERT INTO "ProductUnits" ("ProductId", "UnitId", "UnitValue", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT product_id, 2, 1, 1, 1, NOW(), 1
    FROM tmp_mapping
    WHERE product_id IS NOT NULL AND is_new = TRUE
    ON CONFLICT ("ProductId", "UnitId") DO NOTHING;

    RAISE NOTICE '📦 Adım 6: Yeni ürünler kataloğa eklendi. [+% ürün, %]', v_new_products, clock_timestamp() - v_step_start;

    -- 7. OEM Mapping
    v_step_start := clock_timestamp();
    RAISE NOTICE '📝 Adım 7: OEM kodları işleniyor (Batch Mode: 100k)...';
    
    CREATE TEMP TABLE tmp_distinct_oems ON COMMIT DROP AS
    SELECT DISTINCT product_id, normalized_oem AS oem_code, is_new
    FROM tmp_mapping
    WHERE product_id IS NOT NULL AND normalized_oem IS NOT NULL AND normalized_oem <> '';

    -- Add an ID to easily batch processing
    ALTER TABLE tmp_distinct_oems ADD COLUMN _id SERIAL PRIMARY KEY;
    
    DECLARE 
        _min_id INT;
        _max_id INT;
        _batch_size INT := 100000;
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

    -- 8. SellerItems
    v_step_start := clock_timestamp();
    RAISE NOTICE '📝 Adım 8: Satıcı stok/fiyat bilgileri güncelleniyor...';
    
    CREATE TEMP TABLE tmp_verified_links ON COMMIT DROP AS
    SELECT 
        src.source_id, src.brand_raw, src.product_id,
        src.stock, src.raw_price, fx."ForexSelling" as rate,
        src.currency, seller_commission as commission, src.min_amount
    FROM tmp_mapping src
    LEFT JOIN (
        SELECT DISTINCT ON ("CurrencyCode") "CurrencyCode", "ForexSelling" 
        FROM "Currencies" ORDER BY "CurrencyCode", "CreatedDate" DESC
    ) fx ON fx."CurrencyCode" = src.currency
    WHERE src.product_id IS NOT NULL;
    
    INSERT INTO "SellerItems" (
        "SellerId", "ProductId", "SourceId", "ManufacturerName", "Stock", "CostPrice", "SalePrice", 
        "Commision", "Currency", "Unit", "Status", "CreatedId", "CreatedDate", "Step", "MinSaleAmount", "MaxSaleAmount"
    )
    SELECT 
        seller_id, product_id, source_id, brand_raw, stock,
        ROUND(CASE WHEN currency IN ('TL', 'TRY') THEN raw_price ELSE raw_price * COALESCE(NULLIF(rate, 0), 1) END, 2),
        ROUND((CASE WHEN currency IN ('TL', 'TRY') THEN raw_price ELSE raw_price * COALESCE(NULLIF(rate, 0), 1) END) * (1 + commission/100.0), 2),
        commission, 'TRY', 'Adet', 1, 1, NOW(), 1, min_amount, 0
    FROM tmp_verified_links
    ON CONFLICT ("SellerId", "ProductId", "SourceId") DO UPDATE SET
        "Stock" = EXCLUDED."Stock", "CostPrice" = EXCLUDED."CostPrice", "SalePrice" = EXCLUDED."SalePrice", "ModifiedDate" = NOW(), "Status" = 1;

    GET DIAGNOSTICS v_seller_items = ROW_COUNT;

    -- 8. RESTORE
    ALTER TABLE "Product" ENABLE TRIGGER ALL;
    ALTER TABLE "ProductGroupCodes" ENABLE TRIGGER ALL;
    ALTER TABLE "SellerItems" ENABLE TRIGGER ALL;

    CREATE INDEX IF NOT EXISTS "idx_product_name_trgm" ON public."Product" USING gin ("Name" gin_trgm_ops);
    CREATE INDEX IF NOT EXISTS "idx_productgroupcodes_code" ON public."ProductGroupCodes" ("OemCode");
    CREATE INDEX IF NOT EXISTS "idx_pgc_lookup" ON public."ProductGroupCodes" ("OemCode");
    CREATE INDEX IF NOT EXISTS "idx_productgroupcodes_groupcode_gin" ON public."ProductGroupCodes" USING gin (string_to_array("OemCode", '|'::text));
    CREATE INDEX IF NOT EXISTS "idx_pg_groupcode_norm" ON public."ProductGroupCodes" (regexp_replace(upper("OemCode"), '[^A-Z0-9]', '', 'g'));

    RAISE NOTICE '🏁 TERA-NITRO REMARS COMPLETED. Products: %, OEMs: %, Items: %, Time: %s', 
                 v_new_products, v_inserted_oems, v_seller_items, ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
END;
$BODY$
LANGUAGE plpgsql;
