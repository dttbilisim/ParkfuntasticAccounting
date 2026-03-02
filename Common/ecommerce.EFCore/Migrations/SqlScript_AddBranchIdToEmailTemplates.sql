-- EmailTemplates tablosuna BranchId kolonu ekler.
-- Migration boş uygulandığı için kolon eksik kalan veritabanlarında bu script'i çalıştırın.
-- Navicat veya psql ile MarketPlace veritabanında çalıştırın.

ALTER TABLE "EmailTemplates"
ADD COLUMN IF NOT EXISTS "BranchId" integer NULL;
