-- Drop duplicate UNIQUE constraints on SellerItems
-- Keep only: UK_SellerItems_SellerId_ProductId_SourceId

-- Drop old constraint (without ProductId)
DROP INDEX IF EXISTS "UK_SellerItems_SellerId_SourceId";

-- Drop duplicate (same as above, different name)
DROP INDEX IF EXISTS "idx_selleritems_seller_source_unique";

-- Verify - should only have the new constraint
SELECT indexname FROM pg_indexes 
WHERE tablename = 'SellerItems' 
  AND indexname LIKE '%unique%' OR indexname LIKE '%UK_%'
ORDER BY indexname;

-- Expected result: Only UK_SellerItems_SellerId_ProductId_SourceId
