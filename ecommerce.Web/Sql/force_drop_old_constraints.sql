-- FORCE DROP all old unique constraints
-- This script will drop indexes even if they're in use

-- Method 1: Drop as constraint
ALTER TABLE "SellerItems" DROP CONSTRAINT IF EXISTS "UK_SellerItems_SellerId_SourceId";
ALTER TABLE "SellerItems" DROP CONSTRAINT IF EXISTS "idx_selleritems_seller_source_unique";

-- Method 2: Drop as index (in case they're standalone indexes)
DROP INDEX IF EXISTS "UK_SellerItems_SellerId_SourceId";
DROP INDEX IF EXISTS "idx_selleritems_seller_source_unique";

-- Verify - list remaining unique indexes
DO $$
DECLARE
    idx_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO idx_count
    FROM pg_indexes
    WHERE tablename = 'SellerItems'
      AND (indexname LIKE '%seller%source%' OR indexname LIKE '%UK%SourceId%');
    
    IF idx_count > 0 THEN
        RAISE WARNING 'Still have % old indexes!', idx_count;
    ELSE
        RAISE NOTICE '✅ All old indexes dropped successfully';
    END IF;
END $$;
