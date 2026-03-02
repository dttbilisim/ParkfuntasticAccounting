-- 1. Deduplicate Brand table again to be safe
DELETE FROM "Brand"
WHERE "Id" IN (
    SELECT "Id"
    FROM (
        SELECT 
            "Id", 
            ROW_NUMBER() OVER (PARTITION BY "Name", "BranchId" ORDER BY "Id") as r_num 
        FROM "Brand"
    ) t
    WHERE t.r_num > 1
);

-- 2. FORCE DROP the index to ensure we are creating the correct one
DROP INDEX IF EXISTS "IX_Brand_BranchId_Name";

-- 3. Recreate the Unique Index strictly for (BranchId, Name)
CREATE UNIQUE INDEX "IX_Brand_BranchId_Name" ON "Brand" ("BranchId", "Name");
