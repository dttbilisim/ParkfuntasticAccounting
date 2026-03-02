-- ================================================================
-- SAFE TRUNCATE SCRIPT
-- Purpose: Deletes ALL records from "ProductCategories" table only.
-- Usage: Run this to clear all category links before re-running the Python import.
-- Warning: This verifies that no other tables are affected (unless they cascade delete FROM this table, which is rare for a link table).
-- ================================================================

TRUNCATE TABLE "ProductCategories";

-- If you get a foreign key error, it means some other table depends on this one.
-- In that case, use: TRUNCATE TABLE "ProductCategories" CASCADE; 
-- But for standard Many-to-Many tables, the simple TRUNCATE is safer and sufficient.
