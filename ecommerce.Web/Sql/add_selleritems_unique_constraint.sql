-- Add unique constraint for SellerItems (SellerId, ProductId)
-- This is required for ON CONFLICT clause in sync_products_from_otoismails procedure

-- First, check if there are any duplicate records
DO $$
BEGIN
    -- If there are duplicates, keep only the latest one (by ModifiedDate or CreatedDate)
    WITH duplicates AS (
        SELECT 
            "Id",
            ROW_NUMBER() OVER (
                PARTITION BY "SellerId", "ProductId" 
                ORDER BY COALESCE("ModifiedDate", "CreatedDate") DESC
            ) as rn
        FROM "SellerItems"
    )
    DELETE FROM "SellerItems"
    WHERE "Id" IN (
        SELECT "Id" FROM duplicates WHERE rn > 1
    );
END $$;

-- Now add the unique constraint
ALTER TABLE "SellerItems" 
ADD CONSTRAINT "UK_SellerItems_SellerId_ProductId" 
UNIQUE ("SellerId", "ProductId");
