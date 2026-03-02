-- Cargoes tablosuna CargoType sütunu ekler (Standard=0, BicopsExpress=1).
-- Hata: "column c0.CargoType does not exist" için çalıştırın.
-- Migration (AddCargoTypeToCargo) uygulanmamışsa bu script ile aynı değişikliği yapabilirsiniz.

-- 1) Sütunu ekle (zaten varsa hata verir; o zaman sadece 2. adımı çalıştırın)
ALTER TABLE "Cargoes" ADD COLUMN IF NOT EXISTS "CargoType" smallint NOT NULL DEFAULT 0;

-- 2) Hızlı Kargo Bicops Express kaydı (yoksa ekle)
INSERT INTO "Cargoes" ("Name", "Amount", "CargoOverloadPrice", "Message", "IsLocalStorage", "CargoType", "Status", "CreatedDate", "CreatedId")
SELECT 'Hızlı Kargo Bicops Express', 0, 0, 'Kurye teslimatı', false, 1, 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM "Cargoes" c WHERE c."CargoType" = 1);
