-- UserNotifications tablosu zaten varsa bu migration'ı "uygulanmış" say.
-- Böylece "dotnet ef database update" sadece sonraki migration'ları (AddCollectionReceiptTable dahil) uygular.
-- PostgreSQL: __EFMigrationsHistory tablosuna ekle.

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260219183651_AddUserNotificationTable', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;
