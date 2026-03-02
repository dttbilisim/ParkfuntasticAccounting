-- ====================================================================
-- ULTRA FAST DATABASE CLEANUP SCRIPT (WITH CONNECTION KILLER)
-- ====================================================================
-- TRUNCATE işlemi normalde milisaniyeler sürer. Eğer bekliyorsa, 
-- veritabanında "Lock" (kilit) var demektir (örn: çalışan uygulama).
-- Bu script ÖNCE tüm bağlantıları keser, SONRA siler.
-- ====================================================================

DO $$
DECLARE
    v_start_time TIMESTAMP;
    v_db_name TEXT := current_database();
    v_killed_connections INTEGER;
BEGIN
    v_start_time := clock_timestamp();
    
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '🚀 ULTRA HIZLI TEMİZLİK MODU (Bağlantılar Kesilecek)';
    RAISE NOTICE '   Hedef Veritabanı: %', v_db_name;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';

    -- 1. ADIM: MEVCUT BAĞLANTILARI ZORLA KOPAR (KILL CONNECTIONS)
    -- TRUNCATE'in bekleme sebebi %99 çalışan uygulamanın tuttuğu kilitlerdir.
    RAISE NOTICE '🔌 Aktif bağlantılar sonlandırılıyor (Force Kill)...';
    
    SELECT count(*)
    INTO v_killed_connections
    FROM pg_stat_activity
    WHERE pid <> pg_backend_pid() -- Kendi bağlantımızı koparmayalım
      AND datname = v_db_name;
      
    -- Postgres'te DO bloğu içinde pg_terminate_backend kullanımı transaction state yüzünden bazen sorun olabilir
    -- ama genellikle çalışır. Eğer çalışmazsa ayrı bir komut olarak çalıştırmak gerekir.
    PERFORM pg_terminate_backend(pid)
    FROM pg_stat_activity
    WHERE pid <> pg_backend_pid()
      AND datname = v_db_name;
      
    RAISE NOTICE '✅ % adet bağlantı sonlandırıldı. Kilitler serbest bırakıldı.', v_killed_connections;
    
    -- 2. ADIM: TABLOLARI TRUNCATE ET
    -- Artık bekleyen kimse olmadığı için bu işlem ANINDA bitmeli.
    RAISE NOTICE '🗑️ Tablolar TRUNCATE ediliyor...';
    
    TRUNCATE TABLE 
        "OrderInvoices",
        "OrderAppliedDiscounts",
        "OrderItems",
        "Orders",
        "InvoiceItems",
        "Invoices",
        "CustomerAccountTransactions",
        "ProductImages",
        "ProductUnits",
        "ProductCategories",
        "ProductGroupCodes",
        "SellerItems",
        "PriceListItems",
        "Product",
        "Category",
        "Brand"
    RESTART IDENTITY CASCADE;

    -- 3. Sequence (Sayaç) Resetleme
    RAISE NOTICE '🔄 ID sayaçları sıfırlandı.';

    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE '✅ İŞLEM TAMAMLANDI';
    RAISE NOTICE '⏱️ Toplam Süre: % saniye', ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - v_start_time))::numeric, 4);
    RAISE NOTICE '═══════════════════════════════════════════════════════════';

EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE '❌ HATA: %', SQLERRM;
    -- Bağlantı kesme yetkisi yoksa uyarı ver
    RAISE NOTICE '   İpucu: Eğer izin hatası alırsanız, Superuser olarak çalıştırın veya uygulamayı manuel kapatın.';
END $$;
