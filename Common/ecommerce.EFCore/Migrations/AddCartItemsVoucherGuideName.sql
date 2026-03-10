-- CartItems tablosuna Voucher ve GuideName kolonlarını ekle
-- Migration uygulanmadığında manuel çalıştırılabilir
-- Kullanım: psql -h HOST -p PORT -U USER -d DB -f AddCartItemsVoucherGuideName.sql

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CartItems' AND column_name = 'Voucher') THEN
        ALTER TABLE "CartItems" ADD COLUMN "Voucher" text NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CartItems' AND column_name = 'GuideName') THEN
        ALTER TABLE "CartItems" ADD COLUMN "GuideName" text NULL;
    END IF;
END $$;

-- EF migration history'ye ekle (tekrar uygulanmasın diye)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309100000_AddVoucherAndGuideNameToCartItems', '9.0.6'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309100000_AddVoucherAndGuideNameToCartItems');
