-- Tüm BicoJET / Hızlı Kargo (CargoType=1) kayıtlarının tek ücretini 250 TL yap.
-- Sepette hâlâ 120 TL görünüyorsa, satıcıya bağlı olan kargo farklı bir kayıt olabilir;
-- bu script hepsini 250 yaptığı için hangisi kullanılırsa 250 TL gelir.

UPDATE "Cargoes"
SET
  "Amount" = 250,
  "ModifiedDate" = NOW()
WHERE "CargoType" = 1
  AND "Status" != 99;

-- Kontrol: CargoType=1 olan tüm kayıtların Amount değeri 250 olmalı
-- SELECT "Id", "Name", "Amount", "CoveredKm", "PricePerExtraKm" FROM "Cargoes" WHERE "CargoType" = 1;
