-- Check for duplicate keys in AppSettings
SELECT "Key", COUNT(*), array_agg("Id") as IDs
FROM "AppSettings"
WHERE "Key" IN ('Search_GeneralSettings', 'Search_BoostWeights')
GROUP BY "Key"
HAVING COUNT(*) > 1;

-- List all related settings
SELECT "Id", "Key", "Value", "Description"
FROM "AppSettings"
WHERE "Key" IN ('Search_GeneralSettings', 'Search_BoostWeights')
ORDER BY "Key", "Id";
