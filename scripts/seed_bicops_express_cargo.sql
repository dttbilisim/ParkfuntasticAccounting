-- Hızlı Kargo Bicops Express — kurye teslimatı için kargo kaydı
-- Migration (AddCargoTypeToCargo) bu kaydı otomatik ekler; manuel çalıştırmak için:
-- CargoType: 0 = Standard, 1 = BicopsExpress

INSERT INTO "Cargoes" ("Name", "Amount", "CargoOverloadPrice", "Message", "IsLocalStorage", "CargoType", "Status", "CreatedDate", "CreatedId")
SELECT 'Hızlı Kargo Bicops Express', 0, 0, 'Kurye teslimatı', false, 1, 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM "Cargoes" c WHERE c."CargoType" = 1);
