-- Orders tablosuna CourierVehicleId (siparişe atanan araç/şoför) kolonu ekler.
-- Kargo takip listesinde doğru kurye adı ve plaka gösterimi için.
-- Migration: 20260301220000_AddCourierVehicleIdToOrders

ALTER TABLE "Orders"
ADD COLUMN IF NOT EXISTS "CourierVehicleId" integer NULL;

CREATE INDEX IF NOT EXISTS "IX_Orders_CourierVehicleId"
ON "Orders" ("CourierVehicleId");

ALTER TABLE "Orders"
ADD CONSTRAINT "FK_Orders_CourierVehicles_CourierVehicleId"
FOREIGN KEY ("CourierVehicleId") REFERENCES "CourierVehicles" ("Id")
ON DELETE SET NULL;

-- EF __EFMigrationsHistory'ye bu migration'ı ekle (zaten varsa atla)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260301220000_AddCourierVehicleIdToOrders', '9.0.6'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301220000_AddCourierVehicleIdToOrders');
