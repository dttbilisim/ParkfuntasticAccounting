-- CargoType=BicopsExpress için mesafe bazlı ücretlendirme kolonları.
-- Bu script'i veritabanında bir kez çalıştırın (örn. psql veya pgAdmin).
-- Çalıştırdıktan sonra EF migration'ı "zaten uygulandı" olarak işaretleyin (aşağıdaki INSERT).

BEGIN;

-- Kolonlar yoksa ekle (idempotent)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'Cargoes' AND column_name = 'CoveredKm'
  ) THEN
    ALTER TABLE "Cargoes" ADD COLUMN "CoveredKm" numeric NOT NULL DEFAULT 0;
  END IF;
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'Cargoes' AND column_name = 'PricePerExtraKm'
  ) THEN
    ALTER TABLE "Cargoes" ADD COLUMN "PricePerExtraKm" numeric NOT NULL DEFAULT 0;
  END IF;
END $$;

-- EF Core'un bu migration'ı uygulandı sayması için (dotnet ef database update tekrar çalıştırıldığında atlaması için)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260301190000_AddCargoDistancePricing', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
