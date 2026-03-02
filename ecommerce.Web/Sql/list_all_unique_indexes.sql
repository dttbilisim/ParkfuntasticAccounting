-- List ALL unique indexes on SellerItems
SELECT 
    i.indexname,
    i.indexdef
FROM pg_indexes i
WHERE i.tablename = 'SellerItems'
  AND i.indexdef LIKE '%UNIQUE%'
ORDER BY i.indexname;
