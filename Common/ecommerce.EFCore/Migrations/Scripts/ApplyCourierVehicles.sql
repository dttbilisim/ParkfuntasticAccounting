-- CourierVehicles tablosu ve CourierServiceAreas.CourierVehicleId kolonu
-- Migration: 20260227200000_AddCourierVehiclesAndVehicleToServiceArea
-- Veritabanında "relation CourierVehicles does not exist" hatası alıyorsanız bu script'i çalıştırın.

-- 1) CourierVehicles tablosu (yoksa oluştur)
CREATE TABLE IF NOT EXISTS "CourierVehicles" (
    "Id" serial PRIMARY KEY,
    "CourierId" integer NOT NULL,
    "VehicleType" smallint NOT NULL,
    "LicensePlate" character varying(50) NOT NULL,
    CONSTRAINT "FK_CourierVehicles_Couriers_CourierId" FOREIGN KEY ("CourierId") REFERENCES "Couriers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_CourierVehicles_CourierId" ON "CourierVehicles" ("CourierId");

-- 2) CourierServiceAreas'a CourierVehicleId kolonu (yoksa ekle)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'CourierServiceAreas' AND column_name = 'CourierVehicleId'
    ) THEN
        ALTER TABLE "CourierServiceAreas" ADD COLUMN "CourierVehicleId" integer NULL;
    END IF;
END $$;

-- 3) Eski index'leri kaldır (varsa)
DROP INDEX IF EXISTS "IX_CourierServiceAreas_CourierId_CityId_TownId_NeighboorId";
DROP INDEX IF EXISTS "IX_CourierServiceAreas_CourierId_CityId_TownId";

-- 4) Yeni index'ler (varsa atla için önce drop)
DROP INDEX IF EXISTS "IX_CourierServiceAreas_CourierVehicleId";
DROP INDEX IF EXISTS "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId";
DROP INDEX IF EXISTS "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId";

CREATE INDEX "IX_CourierServiceAreas_CourierVehicleId" ON "CourierServiceAreas" ("CourierVehicleId");

CREATE UNIQUE INDEX "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId"
ON "CourierServiceAreas" ("CourierId", "CourierVehicleId", "CityId", "TownId", "NeighboorId")
WHERE "NeighboorId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId"
ON "CourierServiceAreas" ("CourierId", "CourierVehicleId", "CityId", "TownId")
WHERE "NeighboorId" IS NULL;

-- 5) FK (yoksa ekle)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_CourierServiceAreas_CourierVehicles_CourierVehicleId'
          AND table_name = 'CourierServiceAreas'
    ) THEN
        ALTER TABLE "CourierServiceAreas"
        ADD CONSTRAINT "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId"
        FOREIGN KEY ("CourierVehicleId") REFERENCES "CourierVehicles" ("Id") ON DELETE SET NULL;
    END IF;
END $$;

-- 6) EF migration history'ye ekle (bu migration'ı uygulandı olarak işaretler)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260227200000_AddCourierVehiclesAndVehicleToServiceArea', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
