-- 1. Updates Products with BranchId = 1 (HQ) to BranchId = 0 (Global)
UPDATE "Product" SET "BranchId" = 0 WHERE "BranchId" = 1;

-- 2. Updates Brands with BranchId = 1 to BranchId = 0
UPDATE "Brand" SET "BranchId" = 0 WHERE "BranchId" = 1;

-- 3. Updates ProductUnits with BranchId = 1 to BranchId = 0
UPDATE "ProductUnits" SET "BranchId" = 0 WHERE "BranchId" = 1;

-- 4. Updates Categories (if any inserted by sync)
UPDATE "Category" SET "BranchId" = 0 WHERE "BranchId" = 1;

-- 5. Updates ProductCategories
UPDATE "ProductCategories" SET "BranchId" = 0 WHERE "BranchId" = 1;

-- Note: ProductGroupCodes usually do not have BranchId (checked Entity), so no update needed there.
