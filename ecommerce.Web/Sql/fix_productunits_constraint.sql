-- 1. Deduplicate ProductUnits table (keeping the one with the lowest Id)
DELETE FROM "ProductUnits"
WHERE "Id" IN (
    SELECT "Id"
    FROM (
        SELECT 
            "Id", 
            ROW_NUMBER() OVER (PARTITION BY "ProductId", "UnitId" ORDER BY "Id") as r_num 
        FROM "ProductUnits"
    ) t
    WHERE t.r_num > 1
);

-- 2. Force Drop existing index if any
DROP INDEX IF EXISTS "IX_ProductUnits_ProductId_UnitId";

-- 3. Create the missing Unique Index required for ON CONFLICT ("ProductId", "UnitId")
CREATE UNIQUE INDEX "IX_ProductUnits_ProductId_UnitId" ON "ProductUnits" ("ProductId", "UnitId");
