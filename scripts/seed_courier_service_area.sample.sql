-- Örnek: İlçeye göre kurye testi için hizmet bölgesi ekleme
-- Kullanım: CourierId, CityId, TownId değerlerini kendi veritabanındaki geçerli ID'lerle değiştir.

-- 1) Mevcut aktif kurye ve il/ilçe ID'lerini görmek için:
/*
SELECT c."Id" AS CourierId, c."ApplicationUserId", c."Status"
FROM "Couriers" c
WHERE c."Status" = 1;

SELECT "Id", "Name" FROM "Cities" ORDER BY "Name" LIMIT 20;
SELECT t."Id", t."Name", t."CityId" FROM "Towns" t WHERE t."CityId" = 34 ORDER BY t."Name" LIMIT 30;
*/

-- 2) Hizmet bölgesi ekle (aynı CourierId+CityId+TownId zaten varsa tekrar ekleme):
-- CourierId: Kuryeler tablosundan bir Id
-- CityId: Cities tablosundan (örn. 34 = İstanbul)
-- TownId: Towns tablosundan (örn. Kadıköy ilçe Id'si)
INSERT INTO "CourierServiceAreas" ("CourierId", "CityId", "TownId", "NeighboorId")
VALUES (
  1,   -- CourierId: kendi kurye Id'n
  34,  -- CityId: örn. İstanbul
  1234, -- TownId: örn. Kadıköy ilçe Id'si (Towns tablosundan al)
  NULL -- Mahalle opsiyonel
);

-- 3) Kontrol: Kurye hizmet bölgeleri
-- SELECT csa."Id", csa."CourierId", csa."CityId", csa."TownId", c."Name" AS CityName, t."Name" AS TownName
-- FROM "CourierServiceAreas" csa
-- JOIN "Cities" c ON c."Id" = csa."CityId"
-- JOIN "Towns" t ON t."Id" = csa."TownId";
