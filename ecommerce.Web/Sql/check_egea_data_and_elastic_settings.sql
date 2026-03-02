-- 1. Check if there are any products in SellerItems that have 'Egea' in their SourceId or ManufacturerName (approximate check)
-- Note: SellerItems table does NOT have BaseModelName column. We check ManufacturerName and SourceId.

SELECT count(*) as total_potential_egea
FROM "SellerItems"
WHERE "ManufacturerName" ILIKE '%Fiat%' 
  AND (
      "ManufacturerName" ILIKE '%Egea%' 
      OR "SourceId" ILIKE '%Egea%'
      OR "SourceId" ILIKE '%356%' -- Fiat Egea chassis code often used
  );

-- 2. Check a sample of potential Egea products to see their data
SELECT "SellerId", "ProductId", "SourceId", "ManufacturerName", "Stock", "SalePrice", "Currency"
FROM "SellerItems" 
WHERE "ManufacturerName" ILIKE '%Fiat%' 
  AND (
      "ManufacturerName" ILIKE '%Egea%' 
      OR "SourceId" ILIKE '%Egea%'
      OR "SourceId" ILIKE '%356%'
  )
LIMIT 20;

-- 3. Check if there are any products linked to DotVehicleData with 'Egea' model
-- This requires joining Product -> DotPart -> DotVehicleData (if the link exists in your schema)
-- Assuming Product table has a link or we can check Product directly. 
-- If Product table has Name, check there too.

SELECT "Id", "Name", "ProductBrandName"
FROM "Product"
WHERE "Name" ILIKE '%Egea%'
LIMIT 20;

-- 4. Check DotVehicleData directly to ensure "Fiat Egea" exists as a vehicle definition
SELECT "Id", "ManufacturerName", "BaseModelName", "SubModelName", "DatECode"
FROM "DotVehicleData"
WHERE "ManufacturerName" ILIKE 'Fiat' AND "BaseModelName" ILIKE '%Egea%'
LIMIT 20;
