-- __EFMigrationsHistory ile DB senkron değilse bu script'i çalıştır.
-- Neighboors vb. tablolar zaten varsa, bu migration'ları "uygulanmış" olarak işaretle.
-- Bu script pgAdmin, DBeaver veya psql ile çalıştırılmalı.

-- Önce sadece hata veren 2 migration'ı ekle:
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
  ('20251003090222_clean', '9.0.6'),
  ('20251003092414_city', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;
