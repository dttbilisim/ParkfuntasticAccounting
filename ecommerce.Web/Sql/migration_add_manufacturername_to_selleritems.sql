-- Migration: Add ManufacturerName column to SellerItems table
-- This allows each seller to display their own original brand name
-- even when sharing the same Product (same OEM code)

ALTER TABLE "SellerItems" 
ADD COLUMN IF NOT EXISTS "ManufacturerName" TEXT NULL;

-- Add index for performance (used in Python indexer and potentially in queries)
CREATE INDEX IF NOT EXISTS "IX_SellerItems_ManufacturerName" 
ON "SellerItems" ("ManufacturerName");

COMMENT ON COLUMN "SellerItems"."ManufacturerName" IS 'Seller-specific brand name from source table (e.g., OtoIsmail.Marka, Remar.Manufacturer). Used to display correct brand per seller in search results.';
