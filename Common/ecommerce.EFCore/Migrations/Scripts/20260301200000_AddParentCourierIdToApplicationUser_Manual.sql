-- ParentCourierId: AspNetUsers tablosuna üst kurye (ana kurye) ID kolonu.
-- Bu script'i veritabanında bir kez çalıştırın (psql veya pgAdmin).
-- Çalıştırdıktan sonra EF migration'ı "uygulandı" sayması için altta INSERT var.

BEGIN;

-- Kolon yoksa ekle (idempotent)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'AspNetUsers' AND column_name = 'ParentCourierId'
  ) THEN
    ALTER TABLE "AspNetUsers" ADD COLUMN "ParentCourierId" integer NULL;
  END IF;
END $$;

-- Index yoksa ekle
CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_ParentCourierId" ON "AspNetUsers" ("ParentCourierId");

-- FK yoksa ekle (PostgreSQL'de constraint adı benzersiz olmalı)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.table_constraints
    WHERE constraint_name = 'FK_AspNetUsers_AspNetUsers_ParentCourierId'
  ) THEN
    ALTER TABLE "AspNetUsers"
    ADD CONSTRAINT "FK_AspNetUsers_AspNetUsers_ParentCourierId"
    FOREIGN KEY ("ParentCourierId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL;
  END IF;
END $$;

-- EF Core'un bu migration'ı uygulandı sayması için
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260301200000_AddParentCourierIdToApplicationUser', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
