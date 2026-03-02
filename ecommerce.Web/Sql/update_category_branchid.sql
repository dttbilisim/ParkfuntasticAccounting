-- Update all existing categories to have BranchId = 1
-- Run this after the migration has been applied

UPDATE "Category" 
SET "BranchId" = 1 
WHERE "BranchId" IS NULL OR "BranchId" = 0;

-- Verify the update
SELECT COUNT(*) as total_categories, 
       COUNT(CASE WHEN "BranchId" = 1 THEN 1 END) as categories_with_branch_1
FROM "Category"
WHERE "Status" != 2; -- Exclude deleted categories
