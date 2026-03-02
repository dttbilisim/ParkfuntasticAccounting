-- CourierVehicles tablosuna şoför adı soyadı ve cep telefonu kolonları
ALTER TABLE "CourierVehicles"
ADD COLUMN IF NOT EXISTS "DriverName" character varying(200) NULL,
ADD COLUMN IF NOT EXISTS "DriverPhone" character varying(20) NULL;
