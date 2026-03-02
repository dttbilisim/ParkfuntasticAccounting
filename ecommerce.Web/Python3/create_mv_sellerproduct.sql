-- Materialized View: Önceden hesaplanmış SellerProduct (denormalized)
-- Bu view gece refresh edilir, Python script buradan okur (HIZLI!)

DROP MATERIALIZED VIEW IF EXISTS mv_sellerproduct_full CASCADE;

CREATE MATERIALIZED VIEW mv_sellerproduct_full AS
WITH SellerProductBase AS (
    SELECT 
        SI."Id" AS "SellerItemId",
        SI."SellerId",
        SI."Stock",
        SI."CostPrice",
        SI."SalePrice",
        SI."Commision",
        SI."Currency",
        SI."Unit",
        SI."Status" AS "SellerStatus",
        SI."ModifiedDate" AS "SellerModifiedDate",
        SI."CreatedDate" AS "SellerCreatedDate",
        SI."ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."Barcode" AS "ProductBarcode",
        P."Status" AS "ProductStatus",
        P."BrandId",
        P."TaxId"
    FROM "SellerItems" SI
    JOIN "Product" P ON P."Id" = SI."ProductId"
    WHERE SI."Stock" > 0
),
FirstGroupCode AS (
    SELECT DISTINCT ON (pgc."ProductId")
        pgc."ProductId",
        unnest(string_to_array(pgc."GroupCode", '|')) AS "PartNumber"
    FROM "ProductGroupCodes" pgc
)
SELECT 
    SPB.*,
    -- Brand (denormalized)
    jsonb_build_object('Id', B."Id", 'Name', B."Name") AS "Brand",
    -- Tax (denormalized)
    jsonb_build_object('Id', T."Id", 'Name', T."Name") AS "Tax",
    -- Categories (denormalized - FULL array)
    COALESCE(
        (SELECT json_agg(jsonb_build_object('Id', c."Id", 'Name', c."Name"))
         FROM "ProductCategories" pc
         JOIN "Category" c ON c."Id" = pc."CategoryId"
         WHERE pc."ProductId" = SPB."ProductId"),
        '[]'::json
    ) AS "Categories",
    -- Images (denormalized - FULL array)
    COALESCE(
        (SELECT json_agg(jsonb_build_object('Id', i."Id", 'FileName', i."FileName", 'FileGuid', i."FileGuid"))
         FROM "ProductImages" i
         WHERE i."ProductId" = SPB."ProductId"),
        '[]'::json
    ) AS "Images",
    -- GroupCodes (denormalized - FULL array)
    COALESCE(
        (SELECT json_agg(jsonb_build_object('Id', gc."Id", 'GroupCode', gc."GroupCode"))
         FROM "ProductGroupCodes" gc
         WHERE gc."ProductId" = SPB."ProductId"),
        '[]'::json
    ) AS "GroupCodes",
    -- DotParts (LEFT JOIN - NULL olabilir)
    FGC."PartNumber",
    DP."Name" AS "DotPartName",
    DP."ManufacturerName",
    DP."VehicleTypeName",
    DP."Description" AS "DotPartDescription",
    DP."BaseModelName",
    DP."NetPrice",
    DP."PriceDate",
    DD."VehicleTypeKey",
    DD."ManufactureKey",
    DD."BaseModelKey"
FROM SellerProductBase SPB
LEFT JOIN "Brand" B ON B."Id" = SPB."BrandId"
LEFT JOIN "Tax" T ON T."Id" = SPB."TaxId"
LEFT JOIN FirstGroupCode FGC ON FGC."ProductId" = SPB."ProductId"
LEFT JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = FGC."PartNumber"
LEFT JOIN "DatDatas" DD
    ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
    AND DP."ManufactureKey" = DD."ManufactureKey"
    AND DP."BaseModelKey" = DD."BaseModelKey";

-- Index'ler (hızlı okuma için)
CREATE INDEX idx_mv_sellerproduct_selleritemid ON mv_sellerproduct_full("SellerItemId");
CREATE INDEX idx_mv_sellerproduct_productid ON mv_sellerproduct_full("ProductId");
CREATE INDEX idx_mv_sellerproduct_stock ON mv_sellerproduct_full("Stock");

-- ANALYZE
ANALYZE mv_sellerproduct_full;

-- Refresh komutu (cron job ile kullanılır)
-- REFRESH MATERIALIZED VIEW CONCURRENTLY mv_sellerproduct_full;
-- NOT: CONCURRENTLY için unique index gerekli:
-- CREATE UNIQUE INDEX idx_mv_sellerproduct_unique ON mv_sellerproduct_full("SellerItemId");

