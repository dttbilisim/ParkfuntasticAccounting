-- =====================================================================
-- Migration: Global Product Architecture with OEM-Based Matching
-- Date: 2026-01-25
-- Description: 
--   - Remove SellerId and Oems from Product table (global products)
--   - Update ProductGroupCodes columns (GroupCode → OemCode, add SourceType)
--   - Update SellerItems UNIQUE constraint
-- =====================================================================

-- =====================================================================
-- STEP 1: Cleanup - Delete All Existing Data
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '🗑️  Step 1: Cleaning up existing data...';
END $$;

-- Delete in correct order (FK dependencies)
DELETE FROM "SellerItems";
DELETE FROM "ProductUnits";
DELETE FROM "ProductGroupCodes";
DELETE FROM "PriceListItems";
DELETE FROM "Product";

DO $$ BEGIN
    RAISE NOTICE '✅ Data cleanup completed';
END $$;

-- =====================================================================
-- STEP 2: Product Table - Remove SellerId and Oems
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '🔧 Step 2: Modifying Product table...';
END $$;

-- Drop SellerId column
ALTER TABLE "Product" DROP COLUMN IF EXISTS "SellerId";

-- Drop Oems column
ALTER TABLE "Product" DROP COLUMN IF EXISTS "Oems";

DO $$ BEGIN
    RAISE NOTICE '✅ Product table modified (SellerId and Oems removed)';
END $$;

-- =====================================================================
-- STEP 3: ProductGroupCodes - Update Columns (Keep Table Name)
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '🔧 Step 3: Updating ProductGroupCodes columns...';
END $$;

-- Rename GroupCode to OemCode
ALTER TABLE "ProductGroupCodes" RENAME COLUMN "GroupCode" TO "OemCode";

-- Add SourceType column (default: OEM)
ALTER TABLE "ProductGroupCodes" ADD COLUMN IF NOT EXISTS "SourceType" VARCHAR(20) DEFAULT 'OEM';

-- Add UNIQUE constraint (ProductId + OemCode must be unique)
ALTER TABLE "ProductGroupCodes" DROP CONSTRAINT IF EXISTS "UK_ProductGroupCodes_ProductId_OemCode";
ALTER TABLE "ProductGroupCodes" ADD CONSTRAINT "UK_ProductGroupCodes_ProductId_OemCode" 
    UNIQUE ("ProductId", "OemCode");

DO $$ BEGIN
    RAISE NOTICE '✅ ProductGroupCodes columns updated (GroupCode → OemCode, SourceType added)';
END $$;

-- =====================================================================
-- STEP 4: SellerItems - Update UNIQUE Constraint
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '🔧 Step 4: Updating SellerItems constraints...';
END $$;

-- Drop old constraint
ALTER TABLE "SellerItems" DROP CONSTRAINT IF EXISTS "UK_SellerItems_SellerId_SourceId";

-- Add new constraint (allow multiple SourceIds per Product per Seller)
ALTER TABLE "SellerItems" DROP CONSTRAINT IF EXISTS "UK_SellerItems_SellerId_ProductId_SourceId";
ALTER TABLE "SellerItems" ADD CONSTRAINT "UK_SellerItems_SellerId_ProductId_SourceId" 
    UNIQUE ("SellerId", "ProductId", "SourceId");

DO $$ BEGIN
    RAISE NOTICE '✅ SellerItems constraint updated';
END $$;

-- =====================================================================
-- STEP 5: Create Indexes for Performance
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '🔧 Step 5: Creating performance indexes...';
END $$;

-- Index on ProductGroupCodes.OemCode for fast OEM lookup
CREATE INDEX IF NOT EXISTS "IX_ProductGroupCodes_OemCode" ON "ProductGroupCodes" ("OemCode");

-- Index on ProductGroupCodes.ProductId for joins
CREATE INDEX IF NOT EXISTS "IX_ProductGroupCodes_ProductId" ON "ProductGroupCodes" ("ProductId");

-- Index on SellerItems for performance
CREATE INDEX IF NOT EXISTS "IX_SellerItems_ProductId" ON "SellerItems" ("ProductId");
CREATE INDEX IF NOT EXISTS "IX_SellerItems_SellerId" ON "SellerItems" ("SellerId");

DO $$ BEGIN
    RAISE NOTICE '✅ Performance indexes created';
END $$;

-- =====================================================================
-- STEP 6: Verification Queries
-- =====================================================================
DO $$ 
DECLARE
    has_seller_id BOOLEAN;
    has_oems BOOLEAN;
    table_exists BOOLEAN;
    has_oem_code BOOLEAN;
    has_source_type BOOLEAN;
BEGIN
    RAISE NOTICE '📊 Step 6: Running verification queries...';
    
    -- Check Product schema
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Product' AND column_name = 'SellerId'
    ) INTO has_seller_id;
    
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Product' AND column_name = 'Oems'
    ) INTO has_oems;
    
    IF has_seller_id OR has_oems THEN
        RAISE EXCEPTION '❌ Product table still has SellerId or Oems columns!';
    ELSE
        RAISE NOTICE '✅ Product table schema verified';
    END IF;
    
    -- Check ProductGroupCodes table
    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'ProductGroupCodes'
    ) INTO table_exists;
    
    IF NOT table_exists THEN
        RAISE EXCEPTION '❌ ProductGroupCodes table does not exist!';
    END IF;
    
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'ProductGroupCodes' AND column_name = 'OemCode'
    ) INTO has_oem_code;
    
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'ProductGroupCodes' AND column_name = 'SourceType'
    ) INTO has_source_type;
    
    IF NOT has_oem_code OR NOT has_source_type THEN
        RAISE EXCEPTION '❌ ProductGroupCodes table schema is invalid!';
    ELSE
        RAISE NOTICE '✅ ProductGroupCodes table schema verified';
    END IF;
END $$;

-- =====================================================================
-- SUMMARY
-- =====================================================================
DO $$ BEGIN
    RAISE NOTICE '═════════════════════════════════════════════════════════';
    RAISE NOTICE '🎉 Migration Completed Successfully!';
    RAISE NOTICE '   • Product: SellerId and Oems removed';
    RAISE NOTICE '   • ProductGroupCodes: GroupCode → OemCode';
    RAISE NOTICE '   • ProductGroupCodes: SourceType column added';
    RAISE NOTICE '   • SellerItems constraint updated';
    RAISE NOTICE '   • Performance indexes created';
    RAISE NOTICE '═════════════════════════════════════════════════════════';
END $$;
