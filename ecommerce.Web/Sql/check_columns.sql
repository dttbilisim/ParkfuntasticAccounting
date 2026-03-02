-- Check columns in ProductRemars and ProductOtoIsmails
SELECT table_name, column_name, data_type 
FROM information_schema.columns 
WHERE table_name IN ('ProductRemars', 'ProductOtoIsmails', 'SellerItems', 'Product')
  AND (column_name ILIKE '%Dat%' OR column_name ILIKE '%Process%')
ORDER BY table_name, column_name;

-- Also check if DatProcessNumber exists in DotParts
SELECT table_name, column_name 
FROM information_schema.columns 
WHERE table_name = 'DotParts' 
  AND column_name ILIKE '%Dat%';
