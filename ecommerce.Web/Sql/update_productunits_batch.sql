-- ================================================================
-- SAFE BATCH UPDATE FOR ProductUnits
-- Purpose: Update BranchId = 1 in chunks to avoid table locks.
-- ================================================================

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

    -- Get ID range
    SELECT MIN("Id"), MAX("Id") INTO min_id, max_id FROM "ProductUnits";

    IF min_id IS NULL THEN 
        RAISE NOTICE 'Table ProductUnits is empty. Skipping.';
        RETURN;
    END IF;

    curr_id := min_id;
    RAISE NOTICE 'Starting update on ProductUnits (Range: % to %)', min_id, max_id;

    WHILE curr_id <= max_id LOOP
        -- Update chunk
        UPDATE "ProductUnits"
        SET "BranchId" = 1
        WHERE "Id" >= curr_id 
          AND "Id" < (curr_id + batch_size)
          AND "BranchId" IS DISTINCT FROM 1;

        GET DIAGNOSTICS rows_affected = ROW_COUNT;
        total_affected := total_affected + rows_affected;
        
        COMMIT; -- Release locks
        
        RAISE NOTICE 'Processed range % - %. Updated: %', curr_id, (curr_id + batch_size), rows_affected;
        
        curr_id := curr_id + batch_size;
    END LOOP;

    RAISE NOTICE 'ProductUnits Update Complete. Total Updated: %. Time: %', 
                 total_affected, (clock_timestamp() - start_time);
END $$;
