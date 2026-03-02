-- Clean up temporary tables from previous test runs
-- Run this before executing the test script again

DO $$
DECLARE
    tmp_tables TEXT[] := ARRAY[
        'tmp_incoming',
        'tmp_products_ready',
        'tmp_seller_items_ready',
        'tmp_oem_codes',
        'tmp_mapped_products'
    ];
    tbl TEXT;
BEGIN
    FOREACH tbl IN ARRAY tmp_tables
    LOOP
        EXECUTE format('DROP TABLE IF EXISTS %I CASCADE', tbl);
        RAISE NOTICE 'Dropped table: %', tbl;
    END LOOP;
    
    RAISE NOTICE '✅ All temporary tables cleaned up';
END $$;
