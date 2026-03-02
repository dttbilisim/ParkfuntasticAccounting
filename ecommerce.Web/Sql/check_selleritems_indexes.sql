-- Check all indexes on SellerItems table (including unique indexes)
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'SellerItems'
ORDER BY indexname;
