-- =====================================================================
-- Test Script: Run All Sync Procedures with Transaction Safety
-- Date: 2026-01-25
-- Description: Execute all 4 sync procedures in a transaction with rollback on error
-- =====================================================================

BEGIN; -- Start transaction

DO $$
DECLARE
    v_start_time TIMESTAMP := clock_timestamp();
    v_step_start TIMESTAMP;
    v_step_duration NUMERIC;
    
    -- Counters
    product_count INTEGER;
    oem_count INTEGER;
    seller_item_count INTEGER;
    shared_oems INTEGER;
    
    -- Final stats
    total_products INTEGER;
    total_oems INTEGER;
    total_seller_items INTEGER;
    products_with_multiple_sellers INTEGER;
    products_with_multiple_oems INTEGER;
    duplicate_oems INTEGER;
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '╔═══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║  🚀 GLOBAL PRODUCT ARCHITECTURE - SYNC TEST              ║';
    RAISE NOTICE '║  Transaction: ENABLED (auto-rollback on error)           ║';
    RAISE NOTICE '╚═══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
    
    -- ================================================================
    -- STEP 1: OtoIsmail Sync
    -- ================================================================
    v_step_start := clock_timestamp();
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 📦 STEP 1/4: OtoIsmail Sync                              │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
    
    BEGIN
        CALL sync_products_from_otoismails();
        v_step_duration := ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
        
        SELECT COUNT(*) INTO product_count FROM "Product";
        SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
        SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 1;
        
        RAISE NOTICE '✅ OtoIsmail sync completed in % seconds', v_step_duration;
        RAISE NOTICE '   └─ Products: %', product_count;
        RAISE NOTICE '   └─ OEM Codes: %', oem_count;
        RAISE NOTICE '   └─ Seller Items: %', seller_item_count;
        RAISE NOTICE '';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION '❌ OtoIsmail sync FAILED: %', SQLERRM;
    END;
    
    -- ================================================================
    -- STEP 2: Dega Sync
    -- ================================================================
    v_step_start := clock_timestamp();
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 📦 STEP 2/4: Dega Sync                                   │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
    
    BEGIN
        CALL sync_products_from_dega();
        v_step_duration := ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
        
        SELECT COUNT(*) INTO product_count FROM "Product";
        SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
        SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 3;
        
        -- Check shared OEMs
        SELECT COUNT(*) INTO shared_oems
        FROM "ProductGroupCodes" pgc
        WHERE EXISTS (
            SELECT 1 FROM "SellerItems" si1 WHERE si1."ProductId" = pgc."ProductId" AND si1."SellerId" = 1
        ) AND EXISTS (
            SELECT 1 FROM "SellerItems" si2 WHERE si2."ProductId" = pgc."ProductId" AND si2."SellerId" = 3
        );
        
        RAISE NOTICE '✅ Dega sync completed in % seconds', v_step_duration;
        RAISE NOTICE '   └─ Total Products: %', product_count;
        RAISE NOTICE '   └─ Total OEM Codes: %', oem_count;
        RAISE NOTICE '   └─ Dega Items: %', seller_item_count;
        RAISE NOTICE '   └─ Shared OEMs (OtoIsmail + Dega): %', shared_oems;
        RAISE NOTICE '';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION '❌ Dega sync FAILED: %', SQLERRM;
    END;
    
    -- ================================================================
    -- STEP 3: Remars Sync
    -- ================================================================
    v_step_start := clock_timestamp();
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 📦 STEP 3/4: Remars Sync                                 │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
    
    BEGIN
        CALL sync_products_from_remars();
        v_step_duration := ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
        
        SELECT COUNT(*) INTO product_count FROM "Product";
        SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
        SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 4;
        
        RAISE NOTICE '✅ Remars sync completed in % seconds', v_step_duration;
        RAISE NOTICE '   └─ Total Products: %', product_count;
        RAISE NOTICE '   └─ Total OEM Codes: %', oem_count;
        RAISE NOTICE '   └─ Remars Items: %', seller_item_count;
        RAISE NOTICE '';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION '❌ Remars sync FAILED: %', SQLERRM;
    END;
    
    -- ================================================================
    -- STEP 4: Basbugs Sync
    -- ================================================================
    v_step_start := clock_timestamp();
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 📦 STEP 4/4: Basbugs Sync                                │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
    
    BEGIN
        CALL sync_products_from_basbugs();
        v_step_duration := ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_step_start))::numeric, 2);
        
        SELECT COUNT(*) INTO product_count FROM "Product";
        SELECT COUNT(*) INTO oem_count FROM "ProductGroupCodes";
        SELECT COUNT(*) INTO seller_item_count FROM "SellerItems" WHERE "SellerId" = 2;
        
        RAISE NOTICE '✅ Basbugs sync completed in % seconds', v_step_duration;
        RAISE NOTICE '   └─ Total Products: %', product_count;
        RAISE NOTICE '   └─ Total OEM Codes: %', oem_count;
        RAISE NOTICE '   └─ Basbugs Items: %', seller_item_count;
        RAISE NOTICE '';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION '❌ Basbugs sync FAILED: %', SQLERRM;
    END;
    
    -- ================================================================
    -- STEP 5: Final Verification & Quality Checks
    -- ================================================================
    RAISE NOTICE '';
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 📊 FINAL VERIFICATION & QUALITY METRICS                  │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
    
    -- Total counts
    SELECT COUNT(*) INTO total_products FROM "Product";
    SELECT COUNT(*) INTO total_oems FROM "ProductGroupCodes";
    SELECT COUNT(*) INTO total_seller_items FROM "SellerItems";
    
    -- Products with multiple sellers (GOOD - OEM matching worked)
    SELECT COUNT(DISTINCT "ProductId") INTO products_with_multiple_sellers
    FROM (
        SELECT "ProductId", COUNT(DISTINCT "SellerId") as seller_count
        FROM "SellerItems"
        GROUP BY "ProductId"
        HAVING COUNT(DISTINCT "SellerId") > 1
    ) sub;
    
    -- Products with multiple OEMs (GOOD - product has variants)
    SELECT COUNT(DISTINCT "ProductId") INTO products_with_multiple_oems
    FROM (
        SELECT "ProductId", COUNT(*) as oem_count
        FROM "ProductGroupCodes"
        GROUP BY "ProductId"
        HAVING COUNT(*) > 1
    ) sub;
    
    -- Duplicate OEMs (BAD - should be 0)
    SELECT COUNT(*) INTO duplicate_oems
    FROM (
        SELECT "OemCode", COUNT(DISTINCT "ProductId") as product_count
        FROM "ProductGroupCodes"
        GROUP BY "OemCode"
        HAVING COUNT(DISTINCT "ProductId") > 1
    ) sub;
    
    RAISE NOTICE '';
    RAISE NOTICE '📈 Overall Statistics:';
    RAISE NOTICE '   • Total Products: %', total_products;
    RAISE NOTICE '   • Total OEM Codes: %', total_oems;
    RAISE NOTICE '   • Total Seller Items: %', total_seller_items;
    RAISE NOTICE '';
    RAISE NOTICE '✅ Quality Metrics:';
    RAISE NOTICE '   • Products with Multiple Sellers: % %', 
        products_with_multiple_sellers,
        CASE 
            WHEN products_with_multiple_sellers > 0 THEN '(✓ GOOD - OEM matching working!)'
            ELSE '(⚠ WARNING - No cross-seller matches)'
        END;
    RAISE NOTICE '   • Products with Multiple OEMs: % %', 
        products_with_multiple_oems,
        CASE 
            WHEN products_with_multiple_oems > 0 THEN '(✓ GOOD - Variants detected)'
            ELSE '(ℹ INFO - No multi-OEM products)'
        END;
    RAISE NOTICE '   • Duplicate OEMs (diff Products): % %', 
        duplicate_oems,
        CASE 
            WHEN duplicate_oems = 0 THEN '(✓ PERFECT - No duplicates!)'
            ELSE '(❌ ERROR - Data integrity issue!)'
        END;
    
    -- Fatal error if duplicate OEMs found
    IF duplicate_oems > 0 THEN
        RAISE EXCEPTION '❌ CRITICAL: Found % OEM codes mapped to multiple Products! This violates data integrity.', duplicate_oems;
    END IF;
    
    RAISE NOTICE '';
    RAISE NOTICE '╔═══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║  ✅ ALL SYNC PROCEDURES COMPLETED SUCCESSFULLY           ║';
    RAISE NOTICE '║  Total Time: % seconds                                 ║', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 2);
    RAISE NOTICE '╚═══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
    
END $$;

-- =====================================================================
-- Sample Data: Products with Multiple Sellers
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '┌───────────────────────────────────────────────────────────┐';
    RAISE NOTICE '│ 🔍 SAMPLE: Top 10 Multi-Seller Products                  │';
    RAISE NOTICE '└───────────────────────────────────────────────────────────┘';
END $$;

SELECT 
    p."Id" as "ProductId",
    LEFT(p."Name", 40) as "ProductName",
    COUNT(DISTINCT si."SellerId") as "SellerCount",
    STRING_AGG(DISTINCT s."Name", ', ' ORDER BY s."Name") as "Sellers",
    STRING_AGG(DISTINCT pgc."OemCode", ', ' ORDER BY pgc."OemCode") as "OemCodes"
FROM "Product" p
JOIN "SellerItems" si ON si."ProductId" = p."Id"
JOIN "Sellers" s ON s."Id" = si."SellerId"
JOIN "ProductGroupCodes" pgc ON pgc."ProductId" = p."Id"
GROUP BY p."Id", p."Name"
HAVING COUNT(DISTINCT si."SellerId") > 1
ORDER BY COUNT(DISTINCT si."SellerId") DESC, p."Id"
LIMIT 10;

-- =====================================================================
-- Decision Point: COMMIT or ROLLBACK
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '╔═══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║  ⚠️  TRANSACTION READY TO COMMIT                         ║';
    RAISE NOTICE '║                                                           ║';
    RAISE NOTICE '║  Run COMMIT; to save changes                             ║';
    RAISE NOTICE '║  Run ROLLBACK; to undo everything                        ║';
    RAISE NOTICE '╚═══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
END $$;

-- Uncomment ONE of these:
-- COMMIT;    -- Accept all changes
-- ROLLBACK;  -- Undo all changes
