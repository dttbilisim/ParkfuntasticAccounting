-- ====================================================================
-- FIX SCHEMA CONSTRAINTS FOR MULTI-LISTING
-- ====================================================================
-- Sorun: "UK_SellerItems_SellerId_ProductId" isimli kısıtlama (constraint),
-- bir satıcının aynı ürün için birden fazla ilan (SellerItem) açmasını engelliyor.
--
-- Çözüm: Bu kısıtlamayı kaldırıp, yerine "SourceId" (Kaynak ID) bazlı 
-- tekillik/unique yapısını garanti altına alacağız.
-- ====================================================================

DO $$
BEGIN
    RAISE NOTICE '🔧 Schema Constraint Düzeltmesi Başlatılıyor...';

    -- 1. Engelleyen Constraint'i Kaldır (SellerId + ProductId Unique OLMAMALI)
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'UK_SellerItems_SellerId_ProductId') THEN
        ALTER TABLE "SellerItems" DROP CONSTRAINT "UK_SellerItems_SellerId_ProductId";
        RAISE NOTICE '✅ UK_SellerItems_SellerId_ProductId kısıtlaması kaldırıldı.';
    ELSE
        RAISE NOTICE 'ℹ️ UK_SellerItems_SellerId_ProductId zaten yok.';
    END IF;

    -- Index varsa onu da uçur (bazen constraint index olarak görünebilir)
    DROP INDEX IF EXISTS "UK_SellerItems_SellerId_ProductId";

    -- 2. SourceId Bazlı Unique Index Oluştur (Eğer yoksa)
    -- Scriptlerimiz ON CONFLICT ("SellerId", "SourceId") kullanıyor, bu yüzden bu INDEX ŞART.
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'SellerItems' AND indexname = 'UK_SellerItems_SellerId_SourceId'
    ) THEN
        CREATE UNIQUE INDEX "UK_SellerItems_SellerId_SourceId" ON "SellerItems" ("SellerId", "SourceId");
        RAISE NOTICE '✅ UK_SellerItems_SellerId_SourceId indexi oluşturuldu.';
    ELSE
        RAISE NOTICE 'ℹ️ UK_SellerItems_SellerId_SourceId zaten var.';
    END IF;

    RAISE NOTICE '✅ ŞEMA DÜZELTME TAMAMLANDI.';
END $$;
