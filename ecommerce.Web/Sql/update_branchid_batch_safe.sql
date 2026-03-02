-- ================================================================
-- SAFE BATCH UPDATE SCRIPT FOR LARGE TABLES
-- Purpose: Update BranchId = 1 without table locking
-- Method: Iterates by Primary Key ID ranges (avoiding Seq Scans)
-- ================================================================

-- 1. UPDATE ProductCategories
DO $$
DECLARE
    min_id INT;
    max_id INT;
    batch_size INT := 50000; -- Adjustable batch size
    curr_id INT;
    rows_affected INT;
    total_affected INT := 0;
    start_time TIMESTAMP;
BEGIN
    start_time := clock_timestamp();
    
    -- Get ID range
    SELECT MIN("Id"), MAX("Id") INTO min_id, max_id FROM "ProductCategories";
    
    -- Handle empty table case
    IF min_id IS NULL THEN 
        RAISE NOTICE 'Table ProductCategories is empty. Skipping.';
        RETURN;
    END IF;

    curr_id := min_id;
    RAISE NOTICE 'Starting update on ProductCategories (Range: % to %)', min_id, max_id;

    WHILE curr_id <= max_id LOOP
        -- Update a chunk of IDs
        UPDATE "ProductCategories"
        SET "BranchId" = 1
        WHERE "Id" >= curr_id 
          AND "Id" < (curr_id + batch_size)
          AND "BranchId" IS DISTINCT FROM 1; -- Only update if needed

        GET DIAGNOSTICS rows_affected = ROW_COUNT;
        total_affected := total_affected + rows_affected;
        
        -- Commit transaction to release row locks and keep WAL size managed
        COMMIT;

        -- Optional: Progress log every batch or so
        RAISE NOTICE 'Processed range % - %. Updated: %', curr_id, (curr_id + batch_size), rows_affected;
        
        -- Move to next batch
        curr_id := curr_id + batch_size;
        
        -- Optional: Small sleep to be very gentle on DB CPU/IO
        -- PERFORM pg_sleep(0.01);
    END LOOP;

    RAISE NOTICE 'ProductCategories Update Complete. Total Updated: %. Time: %', 
                 total_affected, (clock_timestamp() - start_time);
END $$;

-- 2. UPDATE ProductUnits
DO $$
DECLARE
    min_id INT;
    max_id INT;
    batch_size INT := 50000;
    curr_id INT;
    rows_affected INT;
    total_affected INT := 0;
    start_time TIMESTAMP;
BEGIN
    start_time := clock_timestamp();

    SELECT MIN("Id"), MAX("Id") INTO min_id, max_id FROM "ProductUnits";

    IF min_id IS NULL THEN 
        RAISE NOTICE 'Table ProductUnits is empty. Skipping.';
        RETURN;
    END IF;

    curr_id := min_id;
    RAISE NOTICE 'Starting update on ProductUnits (Range: % to %)', min_id, max_id;

    WHILE curr_id <= max_id LOOP
        UPDATE "ProductUnits"
        SET "BranchId" = 1
        WHERE "Id" >= curr_id 
          AND "Id" < (curr_id + batch_size)
          AND "BranchId" IS DISTINCT FROM 1;

        GET DIAGNOSTICS rows_affected = ROW_COUNT;
        total_affected := total_affected + rows_affected;
        
        COMMIT;

        RAISE NOTICE 'Processed range % - %. Updated: %', curr_id, (curr_id + batch_size), rows_affected;
        
        curr_id := curr_id + batch_size;
    END LOOP;

    RAISE NOTICE 'ProductUnits Update Complete. Total Updated: %. Time: %', 
                 total_affected, (clock_timestamp() - start_time);
END $$;
