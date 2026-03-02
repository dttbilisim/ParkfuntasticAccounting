-- =========================================================
-- SENIOR LEVEL BATCH UPDATE SCRIPT
-- Purpose: Safely update millions of rows without locking the table
-- Method: Updates in batches of 50,000
-- =========================================================

DO $$
DECLARE
    r_count INTEGER;
    batch_size INTEGER := 50000;
    total_updated_product INTEGER := 0;
    total_updated_brand INTEGER := 0;
    total_updated_units INTEGER := 0;
    start_time TIMESTAMP;
BEGIN
    RAISE NOTICE '🚀 Starting Batch Update process...';

    -- 1. PRODUCT TABLE UPDATE
    start_time := clock_timestamp();
    LOOP
        UPDATE "Product"
        SET "BranchId" = 0
        WHERE "Id" IN (
            SELECT "Id"
            FROM "Product"
            WHERE "BranchId" = 1
            LIMIT batch_size
        );
        
        GET DIAGNOSTICS r_count = ROW_COUNT;
        total_updated_product := total_updated_product + r_count;
        
        RAISE NOTICE '   -> Product Batch Updated: % rows (Total: %)', r_count, total_updated_product;
        
        -- If no rows were updated, we are done with this table
        EXIT WHEN r_count < batch_size;
        
        -- Optional: Sleep to let other transactions breathe (e.g., 0.1s)
        -- PERFORM pg_sleep(0.1); 
    END LOOP;
    RAISE NOTICE '✅ Product Table Finished. Total: % (%s)', total_updated_product, clock_timestamp() - start_time;

    -- 2. BRAND TABLE UPDATE
    start_time := clock_timestamp();
    LOOP
        UPDATE "Brand"
        SET "BranchId" = 0
        WHERE "Id" IN (
            SELECT "Id"
            FROM "Brand"
            WHERE "BranchId" = 1
            LIMIT batch_size
        );
        
        GET DIAGNOSTICS r_count = ROW_COUNT;
        total_updated_brand := total_updated_brand + r_count;
        
        RAISE NOTICE '   -> Brand Batch Updated: % rows (Total: %)', r_count, total_updated_brand;
        
        EXIT WHEN r_count < batch_size;
    END LOOP;
    RAISE NOTICE '✅ Brand Table Finished. Total: % (%s)', total_updated_brand, clock_timestamp() - start_time;

    -- 3. PRODUCT UNITS TABLE UPDATE
    start_time := clock_timestamp();
    LOOP
        UPDATE "ProductUnits"
        SET "BranchId" = 0
        WHERE "Id" IN (
            SELECT "Id"
            FROM "ProductUnits"
            WHERE "BranchId" = 1
            LIMIT batch_size
        );
        
        GET DIAGNOSTICS r_count = ROW_COUNT;
        total_updated_units := total_updated_units + r_count;
        
        RAISE NOTICE '   -> ProductUnits Batch Updated: % rows (Total: %)', r_count, total_updated_units;
        
        EXIT WHEN r_count < batch_size;
    END LOOP;
    RAISE NOTICE '✅ ProductUnits Table Finished. Total: % (%s)', total_updated_units, clock_timestamp() - start_time;

    -- 4. CATEGORY (Smaller tables, single update is usually fine but kept safe)
    UPDATE "Category" SET "BranchId" = 0 WHERE "BranchId" = 1;
    UPDATE "ProductCategories" SET "BranchId" = 0 WHERE "BranchId" = 1;

    RAISE NOTICE '🎉 ALL UPDATES COMPLETED SUCCESSFULLY.';
END $$;
