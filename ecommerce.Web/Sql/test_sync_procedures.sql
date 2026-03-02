-- =====================================================================
-- Test Script: Run All Sync Procedures and Verify Results
-- Date: 2026-01-25
-- Description: Execute all 4 sync procedures in sequence and verify OEM-based matching
-- =====================================================================

-- =====================================================================
-- STEP 1: Run OtoIsmail Sync
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 STEP 1: Running OtoIsmail Sync...';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

CALL sync_products_from_otoismails();

-- Quick stats
DO $$
DECLARE
    product_count INTEGER;
    oem_count INTEGER;
    seller_item_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO product_count FROM "Product";
    SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 1;
    
    RAISE NOTICE '📊 OtoIsmail Stats:';
    RAISE NOTICE '   • Products: %', product_count;
    RAISE NOTICE '   • OEM Codes: %', oem_count;
    RAISE NOTICE '   • Seller Items: %', seller_item_count;
END $$;

-- =====================================================================
-- STEP 2: Run Dega Sync
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 STEP 2: Running Dega Sync...';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

CALL sync_products_from_dega();

-- Quick stats
DO $$
DECLARE
    product_count INTEGER;
    oem_count INTEGER;
    seller_item_count INTEGER;
    shared_oems INTEGER;
BEGIN
    SELECT COUNT(*) INTO product_count FROM "Product";
    SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 3;
    
    -- Check shared OEMs (same OEM from different sellers)
    SELECT COUNT(*) INTO shared_oems
    FROM "ProductGroupCodes" pgc
    WHERE EXISTS (
        SELECT 1 FROM "SellerItems" si1 WHERE si1."ProductId" = pgc."ProductId" AND si1."SellerId" = 1
    ) AND EXISTS (
        SELECT 1 FROM "SellerItems" si2 WHERE si2."ProductId" = pgc."ProductId" AND si2."SellerId" = 3
    );
    
    RAISE NOTICE '📊 Dega Stats:';
    RAISE NOTICE '   • Total Products: %', product_count;
    RAISE NOTICE '   • Total OEM Codes: %', oem_count;
    RAISE NOTICE '   • Dega Items: %', seller_item_count;
    RAISE NOTICE '   • Shared OEMs (OtoIsmail + Dega): %', shared_oems;
END $$;

-- =====================================================================
-- STEP 3: Run Remars Sync
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 STEP 3: Running Remars Sync...';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

CALL sync_products_from_remars();

-- Quick stats
DO $$
DECLARE
    product_count INTEGER;
    oem_count INTEGER;
    seller_item_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO product_count FROM "Product";
    SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 4;
    
    RAISE NOTICE '📊 Remars Stats:';
    RAISE NOTICE '   • Total Products: %', product_count;
    RAISE NOTICE '   • Total OEM Codes: %', oem_count;
    RAISE NOTICE '   • Remars Items: %', seller_item_count;
END $$;

-- =====================================================================
-- STEP 4: Run Basbugs Sync
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 STEP 4: Running Basbugs Sync...';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

CALL sync_products_from_basbugs();

-- Final stats
DO $$
DECLARE
    product_count INTEGER;
    oem_count INTEGER;
    seller_item_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO product_count FROM "Product";
    SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 2;
    
    RAISE NOTICE '📊 Basbugs Stats:';
    RAISE NOTICE '   • Total Products: %', product_count;
    RAISE NOTICE '   • Total OEM Codes: %', oem_count;
    RAISE NOTICE '   • Basbugs Items: %', seller_item_count;
END $$;

-- =====================================================================
-- STEP 5: Final Verification
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '📊 FINAL VERIFICATION';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

-- Overall statistics
DO $$
DECLARE
    total_products INTEGER;
    total_oems INTEGER;
    total_seller_items INTEGER;
    products_with_multiple_sellers INTEGER;
    products_with_multiple_oems INTEGER;
    duplicate_oems INTEGER;
BEGIN
    -- Total counts
    SELECT COUNT(*) INTO total_products FROM "Product";
    SELECT COUNT(*) INTO total_oems FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO total_seller_items FROM "SellerItems";
    
    -- Products with multiple sellers (GOOD - means OEM matching worked)
    SELECT COUNT(DISTINCT "ProductId") INTO products_with_multiple_sellers
    FROM (
        SELECT "ProductId", COUNT(DISTINCT "SellerId") as seller_count
        FROM "SellerItems"
        GROUP BY "ProductId"
        HAVING COUNT(DISTINCT "SellerId") > 1
    ) sub;
    
    -- Products with multiple OEMs (GOOD - means a product has variants)
    SELECT COUNT(DISTINCT "ProductId") INTO products_with_multiple_oems
    FROM (
        SELECT "ProductId", COUNT(*) as oem_count
        FROM "ProductGroupCodes"
        GROUP BY "ProductId"
        HAVING COUNT(*) > 1
    ) sub;
    
    -- Duplicate OEMs (BAD - should be 0 due to UNIQUE constraint)
    SELECT COUNT(*) INTO duplicate_oems
    FROM (
        SELECT "OemCode", COUNT(DISTINCT "ProductId") as product_count
        FROM "ProductGroupCodes"
        GROUP BY "OemCode"
        HAVING COUNT(DISTINCT "ProductId") > 1
    ) sub;
    
    RAISE NOTICE '📈 Overall Statistics:';
    RAISE NOTICE '   • Total Products: %', total_products;
    RAISE NOTICE '   • Total OEM Codes: %', total_oems;
    RAISE NOTICE '   • Total Seller Items: %', total_seller_items;
    RAISE NOTICE '';
    RAISE NOTICE '✅ Quality Metrics:';
    RAISE NOTICE '   • Products with Multiple Sellers: % (GOOD)', products_with_multiple_sellers;
    RAISE NOTICE '   • Products with Multiple OEMs: % (GOOD)', products_with_multiple_oems;
    RAISE NOTICE '   • Duplicate OEMs across Products: % (should be 0)', duplicate_oems;
    
    IF duplicate_oems > 0 THEN
        RAISE WARNING '⚠️  WARNING: Found % OEM codes mapped to multiple Products!', duplicate_oems;
    ELSE
        RAISE NOTICE '✅ Perfect! No duplicate OEMs across different Products.';
    END IF;
END $$;

-- =====================================================================
-- STEP 6: Sample Data Verification
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🔍 SAMPLE DATA (Products with Multiple Sellers)';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;

-- Show example products that are sold by multiple sellers
SELECT 
    p."Id" as "ProductId",
    p."Name" as "ProductName",
    COUNT(DISTINCT si."SellerId") as "SellerCount",
    STRING_AGG(DISTINCT s."Name", ', ' ORDER BY s."Name") as "Sellers",
    STRING_AGG(DISTINCT pgc."OemCode", ', ' ORDER BY pgc."OemCode") as "OemCodes"
FROM "Product" p
JOIN "SellerItems" si ON si."ProductId" = p."Id"
JOIN "Sellers" s ON s."Id" = si."SellerId"
JOIN "ProductGroupCodes" pgc ON pgc."ProductId" = p."Id"
GROUP BY p."Id", p."Name"
HAVING COUNT(DISTINCT si."SellerId") > 1
ORDER BY COUNT(DISTINCT si."SellerId") DESC
LIMIT 10;

DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🎉 ALL SYNC PROCEDURES COMPLETED!';
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;
