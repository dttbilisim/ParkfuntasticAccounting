-- Find the remaining old index
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'SellerItems'
  AND (indexname LIKE '%seller%source%' OR indexname LIKE '%UK%SourceId%' OR indexname LIKE '%SellerId%SourceId%')
ORDER BY indexname;
