-- CashRegisterMovements tablosuna TransCode ve ProcessType kolonlarını ekler
-- (20260303130000_AddProcessTypeTransCodeSalesPersonIdToCashRegisterMovement migration'ının manuel uygulanması)
-- PcPos transfer ProcessPayments hatasını gidermek için.

-- TransCode kolonu zaten varsa hata vermez
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'TransCode'
    ) THEN
        ALTER TABLE "CashRegisterMovements" ADD COLUMN "TransCode" character varying(50) NULL;
    END IF;
END $$;

-- ProcessType kolonu zaten varsa hata vermez
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'ProcessType'
    ) THEN
        ALTER TABLE "CashRegisterMovements" ADD COLUMN "ProcessType" smallint NOT NULL DEFAULT 1;
    END IF;
END $$;

-- SalesPersonId kolonu zaten varsa hata vermez (opsiyonel, migration'da var)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'SalesPersonId'
    ) THEN
        ALTER TABLE "CashRegisterMovements" ADD COLUMN "SalesPersonId" integer NULL;
    END IF;
END $$;

-- Index (SalesPersonId için - migration'da var)
CREATE INDEX IF NOT EXISTS "IX_CashRegisterMovements_SalesPersonId" ON "CashRegisterMovements" ("SalesPersonId");
