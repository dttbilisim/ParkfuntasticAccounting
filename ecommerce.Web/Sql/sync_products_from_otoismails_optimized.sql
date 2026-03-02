CREATE OR REPLACE PROCEDURE "public"."sync_products_from_otoismails"()
AS $BODY$
DECLARE
    seller_commission INTEGER;
BEGIN
    -- 0. Komisyon bilgisi
    SELECT "Commission" INTO seller_commission FROM "Sellers" WHERE "Id" = 1;
    seller_commission := COALESCE(seller_commission, 0);

    -- 1. Döviz kurları
    CREATE TEMP TABLE tmp_currency ON COMMIT DROP AS
    SELECT "CurrencyCode", "ForexSelling" AS "Rate"
    FROM "Currencies"
    WHERE "CurrencyCode" IN ('USD', 'EUR', 'TRY');

    -- 2. Normalize edilmiş veri
    CREATE TEMP TABLE tmp_incoming ON COMMIT DROP AS
    SELECT
        INITCAP(TRIM(pd."Ad")) AS "Ad",
        TRIM(pd."Marka") AS "CleanBrand",
        ROUND(COALESCE(NULLIF(pd."Fiyat3", 0), pd."Fiyat1")::numeric, 2) AS "RawPrice",
        TRIM('TL') AS "Currency",
        pd."CreatedId", pd."CreatedDate",
        pd."Kod", pd."OrjinalKod", pd."Oem",
        COALESCE(
            GREATEST(
                COALESCE(pd."Gebze", 0),
                COALESCE(pd."Ankara", 0),
                COALESCE(pd."Ikitelli", 0),
                COALESCE(pd."Izmir", 0),
                COALESCE(pd."Samsun", 0),
                COALESCE(pd."Depo1030", 0),
                COALESCE(pd."Depo13", 0)
            ),
            0
        ) AS "AdvertCount",
        pd."ImageUrl" AS "DocumentUrl"
    FROM "ProductOtoIsmails" pd
    WHERE TRIM(pd."Oem") IS NOT NULL AND TRIM(pd."Oem") <> '';

    CREATE INDEX idx_tmp_incoming_brand ON tmp_incoming ("CleanBrand");
    CREATE INDEX idx_tmp_incoming_kod ON tmp_incoming ("Kod");
    ANALYZE tmp_incoming;

    -- 3. Döviz çevirisi
    CREATE TEMP TABLE tmp_converted ON COMMIT DROP AS
    SELECT t.*, ROUND(t."RawPrice" * COALESCE(c."Rate", 1), 2) AS "Price"
    FROM tmp_incoming t
    LEFT JOIN tmp_currency c ON c."CurrencyCode" = t."Currency";

    -- 4. Marka ekleme (toplu)
    INSERT INTO "Brand" ("Name", "Status", "CreatedId", "CreatedDate", "BranchId")
    SELECT DISTINCT ON (LOWER(TRIM(t."CleanBrand")))
        t."CleanBrand", 1, t."CreatedId", t."CreatedDate", 1
    FROM tmp_converted t
    LEFT JOIN "Brand" b ON LOWER(TRIM(b."Name")) = LOWER(TRIM(t."CleanBrand")) AND (b."BranchId" = 1 OR b."BranchId" IS NULL)
    WHERE b."Id" IS NULL;

    -- 5. matched_incoming
    CREATE TEMP TABLE matched_incoming ON COMMIT DROP AS
    SELECT
        t."Ad", t."Price", t."Price" AS "CostPrice", t."Price" AS "RetailPrice",
        b."Id" AS "BrandId", t."CreatedId", t."CreatedDate", t."AdvertCount",
        t."DocumentUrl", t."Currency",
        COALESCE(NULLIF(TRIM(t."Kod"), ''), '') || '|' ||
        COALESCE(NULLIF(TRIM(t."OrjinalKod"), ''), '') || '|' ||
        COALESCE(NULLIF(TRIM(t."Oem"), ''), '') AS "GroupCode",
        t."Kod" AS "Oems"
    FROM tmp_converted t
    JOIN "Brand" b ON LOWER(TRIM(b."Name")) = LOWER(TRIM(t."CleanBrand"));

    CREATE INDEX idx_matched_groupcode ON matched_incoming ("GroupCode");
    ANALYZE matched_incoming;

    -- 6. YENİ ÜRÜNLERİ TOPLU EKLE
    WITH new_products AS (
        SELECT DISTINCT ON (mi."GroupCode") mi.*
        FROM matched_incoming mi
        LEFT JOIN "ProductGroupCodes" pg ON pg."GroupCode" = mi."GroupCode"
        WHERE pg."ProductId" IS NULL
        ORDER BY mi."GroupCode", mi."CreatedDate" DESC
    ),
    inserted_rows AS (
        INSERT INTO "Product" (
            "Name", "Oems", "Barcode", "BrandId", "TaxId", "Price", "CostPrice", "RetailPrice",
            "Status", "CreatedId", "CreatedDate", "CargoDesi", "IsNewsProduct",
            "DocumentUrl", "IsCustomerCreated", "IsGift", "SellerId", "AdvertCount",
            "CartMinValue", "Weight", "Width", "Length", "Height"
        )
        SELECT 
            "Ad", "Oems", NULL, "BrandId", 6, "Price", "CostPrice", "RetailPrice",
            1, "CreatedId", "CreatedDate", 1, FALSE,
            "DocumentUrl", FALSE, FALSE, 1, "AdvertCount",
            1, 1, 1, 1, 1
        FROM new_products
        RETURNING "Id", "Oems"
    )
    INSERT INTO "ProductGroupCodes" ("ProductId", "GroupCode", "Status", "CreatedId", "CreatedDate")
    SELECT i."Id", i."Oems", 1, 1, NOW()
    FROM inserted_rows i
    WHERE NOT EXISTS (
        SELECT 1 FROM "ProductGroupCodes" pg 
        WHERE pg."GroupCode" = i."Oems"
    );

    -- 7. SELLERITEMS TOPLU UPSERT (DISTINCT to avoid duplicates)
    INSERT INTO "SellerItems" (
        "SellerId", "ProductId", "Stock", "CostPrice", "SalePrice", "Commision",
        "Currency", "Unit", "Status", "CreatedId", "CreatedDate"
    )
    SELECT DISTINCT ON (pg."ProductId")
        1,
        pg."ProductId",
        mi."AdvertCount",
        mi."CostPrice",
        ROUND(mi."CostPrice" * (1 + seller_commission / 100.0), 2),
        seller_commission,
        mi."Currency",
        'Adet',
        1,
        mi."CreatedId",
        mi."CreatedDate"
    FROM matched_incoming mi
    JOIN "ProductGroupCodes" pg ON pg."GroupCode" = mi."GroupCode"
    ORDER BY pg."ProductId", mi."CreatedDate" DESC
    ON CONFLICT ("SellerId", "ProductId") DO UPDATE SET
        "Stock" = EXCLUDED."Stock",
        "CostPrice" = EXCLUDED."CostPrice",
        "SalePrice" = EXCLUDED."SalePrice",
        "Commision" = EXCLUDED."Commision",
        "ModifiedDate" = NOW(),
        "ModifiedId" = EXCLUDED."CreatedId";

    -- 8. PRODUCT TOPLU GÜNCELLEME
    UPDATE "Product" p
    SET
        "Price" = mi."Price",
        "RetailPrice" = mi."RetailPrice",  
        "DocumentUrl" = mi."DocumentUrl",
        "BrandId" = mi."BrandId",
        "Oems" = mi."Oems",
        "AdvertCount" = mi."AdvertCount",
        "ModifiedDate" = NOW(),
        "Status" = 1
    FROM (
        SELECT DISTINCT ON ("GroupCode") *
        FROM matched_incoming
        ORDER BY "GroupCode", "CreatedDate" DESC
    ) mi
    JOIN "ProductGroupCodes" pg ON pg."GroupCode" = mi."GroupCode"
    WHERE p."Id" = pg."ProductId"
    AND (
        p."Price" IS DISTINCT FROM mi."Price" OR
        p."RetailPrice" IS DISTINCT FROM mi."RetailPrice" OR
        p."DocumentUrl" IS DISTINCT FROM mi."DocumentUrl" OR
        p."AdvertCount" IS DISTINCT FROM mi."AdvertCount"
    );

    RAISE NOTICE 'OtoIsmail senkronizasyonu tamamlandı (optimized)!';
END;
$BODY$
LANGUAGE plpgsql;
