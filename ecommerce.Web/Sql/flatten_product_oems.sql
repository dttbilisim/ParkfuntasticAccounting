-- ====================================================================
-- FLATTEN STRATEGY FOR HIGH-PERFORMANCE PRODUCT MATCHING
-- ====================================================================
-- Purpose:
-- The 'Product.Oems' column contains delimited strings (e.g., "OEM1, OEM2; OEM3").
-- Searching this with LIKE '%...%' is extremely slow on large datasets.
-- Even pg_trgm has limitations.
--
-- This script:
-- 1. Creates a temporary table `tmp_product_oems_expanded`.
-- 2. Splits the `Oems` column into individual rows (one per OEM code).
-- 3. Cleans and normalizes the OEM codes (remove spaces, uppercase).
-- 4. Indexes the result for O(1) equality lookups.
-- ====================================================================

-- 1. Create Temporary Table
-- We use a TEMP table so it is session-specific and automatically dropped.
-- If this needs to be a persistent materialized view, remove 'TEMP'.
DROP TABLE IF EXISTS tmp_product_oems_expanded;

CREATE TEMP TABLE tmp_product_oems_expanded AS
SELECT
    p."Id" AS "ProductId",
    p."SellerId",
    TRIM(UPPER(REGEXP_REPLACE(token, '\s+', '', 'g'))) AS "CleanOem"
FROM "Product" p,
     -- Split by common delimiters: comma, semicolon, pipe, slash, backslash
     unnest(string_to_array(regexp_replace(p."Oems", '[,;|/\\\\]+', ',', 'g'), ',')) AS token
WHERE p."Oems" IS NOT NULL
  AND TRIM(token) <> '';

-- 2. Create Index for Blazing Fast Lookups
-- B-Tree index is perfect for equality checks (=).
CREATE INDEX idx_tmp_product_oems_expanded_cleanoem 
ON tmp_product_oems_expanded ("CleanOem");

CREATE INDEX idx_tmp_product_oems_expanded_productid 
ON tmp_product_oems_expanded ("ProductId");

-- 3. Analyze for Query Planner Statistics
ANALYZE tmp_product_oems_expanded;

-- ====================================================================
-- USAGE EXAMPLE
-- ====================================================================
-- To find products matching a specific OEM code '12345':
/*
SELECT p.*
FROM "Product" p
JOIN tmp_product_oems_expanded e ON e."ProductId" = p."Id"
WHERE e."CleanOem" = '12345';
*/

-- To join with another table (e.g., IncomingItems) that has an OEM column:
/*
SELECT i.*, p."Id" as "MatchedProductId"
FROM "IncomingItems" i
JOIN tmp_product_oems_expanded e ON e."CleanOem" = i."OemClean"
JOIN "Product" p ON p."Id" = e."ProductId"
WHERE i."IsProcessed" = false;
*/

RAISE NOTICE '✅ Expanded OEM table created with % rows.', (SELECT COUNT(*) FROM tmp_product_oems_expanded);
