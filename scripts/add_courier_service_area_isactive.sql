-- CourierServiceAreas tablosuna IsActive sütunu ekler (yoksa).
-- Hata: Npgsql 42703: column c0.IsActive does not exist
-- Çalıştırma: psql -U postgres -d your_db -f add_courier_service_area_isactive.sql
-- veya pgAdmin / DBeaver ile çalıştırın.

ALTER TABLE "CourierServiceAreas" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;
