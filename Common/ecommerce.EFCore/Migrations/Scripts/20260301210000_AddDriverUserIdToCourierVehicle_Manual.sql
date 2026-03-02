-- CourierVehicles tablosuna DriverUserId (şoför/alt kullanıcı) kolonu ekler.
-- Migration: 20260301210000_AddDriverUserIdToCourierVehicle

ALTER TABLE "CourierVehicles"
ADD COLUMN IF NOT EXISTS "DriverUserId" integer NULL;

CREATE INDEX IF NOT EXISTS "IX_CourierVehicles_DriverUserId"
ON "CourierVehicles" ("DriverUserId");

ALTER TABLE "CourierVehicles"
ADD CONSTRAINT "FK_CourierVehicles_AspNetUsers_DriverUserId"
FOREIGN KEY ("DriverUserId") REFERENCES "AspNetUsers" ("Id")
ON DELETE SET NULL;
