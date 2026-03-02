-- CourierApplications tablosuna CityId, TownId ve CriminalRecordPath kolonlarını ekler.
-- Bu script migration'lar uygulanmadan önce manuel çalıştırılabilir.

-- Kolonlar (yoksa ekle)
ALTER TABLE "CourierApplications" ADD COLUMN IF NOT EXISTS "CityId" integer NULL;
ALTER TABLE "CourierApplications" ADD COLUMN IF NOT EXISTS "TownId" integer NULL;
ALTER TABLE "CourierApplications" ADD COLUMN IF NOT EXISTS "CriminalRecordPath" text NULL;

-- İndeksler (yoksa oluştur)
CREATE INDEX IF NOT EXISTS "IX_CourierApplications_CityId" ON "CourierApplications" ("CityId");
CREATE INDEX IF NOT EXISTS "IX_CourierApplications_TownId" ON "CourierApplications" ("TownId");

-- Foreign key'ler (PostgreSQL'de önce index gerekir; hata verirse tabloda veri yoksa atlayabilirsiniz)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_CourierApplications_City_CityId'
  ) THEN
    ALTER TABLE "CourierApplications"
    ADD CONSTRAINT "FK_CourierApplications_City_CityId"
    FOREIGN KEY ("CityId") REFERENCES "City" ("Id") ON DELETE RESTRICT;
  END IF;
END $$;
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_CourierApplications_Town_TownId'
  ) THEN
    ALTER TABLE "CourierApplications"
    ADD CONSTRAINT "FK_CourierApplications_Town_TownId"
    FOREIGN KEY ("TownId") REFERENCES "Town" ("Id") ON DELETE RESTRICT;
  END IF;
END $$;
