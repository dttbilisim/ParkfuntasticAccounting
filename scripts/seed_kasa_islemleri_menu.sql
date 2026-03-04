-- Kasa İşlemleri üst menü ve alt menüler
-- Bu script "Kasa İşlemleri" ana menüsü altında ilgili sayfaları gruplar.
-- Çalıştırmadan önce mevcut menü yapınızı kontrol edin.

-- 1. Kasa İşlemleri üst menüsünü ekle (yoksa)
INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
SELECT NULL, 'Kasa İşlemleri', '#', 'account_balance_wallet', 'kasa,tahsilat,virman', 50
WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '#' AND "Name" = 'Kasa İşlemleri');

-- 2. Üst menü ID'sini al (yeni eklenen veya mevcut)
DO $$
DECLARE
    parent_menu_id INT;
BEGIN
    SELECT "Id" INTO parent_menu_id FROM "Menus" WHERE "Name" = 'Kasa İşlemleri' AND "Path" = '#' LIMIT 1;
    
    IF parent_menu_id IS NOT NULL THEN
        -- 3. Kasa Tanımları - ParentId güncelle (varsa)
        UPDATE "Menus" SET "ParentId" = parent_menu_id, "Order" = 0 
        WHERE "Path" = '/cash-registers';
        
        -- 4. Kasa Hareketleri - ParentId güncelle (varsa)
        UPDATE "Menus" SET "ParentId" = parent_menu_id, "Order" = 1 
        WHERE "Path" = '/cash-register-movements';
        
        -- 5. Tahsilat Listesi - yoksa ekle (Kasa Hareketleri sayfası TH filtresi ile)
        INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
        SELECT parent_menu_id, 'Tahsilat Listesi', '/cash-register-movements?processType=TH', 'receipt_long', 'tahsilat,makbuz', 2
        WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '/cash-register-movements?processType=TH');
        
        -- 6. Kasa Raporu - yoksa ekle
        INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
        SELECT parent_menu_id, 'Kasa Raporu', '/cash-register-report', 'assessment', 'kasa,rapor,bakiye', 3
        WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '/cash-register-report');
        
        -- 7. cash-registers ve cash-register-movements yoksa ekle
        INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
        SELECT parent_menu_id, 'Kasa Tanımları', '/cash-registers', 'account_balance', 'kasa,tanım', 0
        WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '/cash-registers');
        
        INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
        SELECT parent_menu_id, 'Kasa Hareketleri', '/cash-register-movements', 'list_alt', 'kasa,hareket', 1
        WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '/cash-register-movements');
    END IF;
END $$;

-- 8. Admin rolüne yeni menüleri ekle (RoleMenus - Admin role id genelde 1)
-- Not: Kendi projenizde Admin role id farklı olabilir, gerekirse düzenleyin.
INSERT INTO "RoleMenus" ("MenuId", "RoleId", "CanView", "CanCreate", "CanEdit", "CanDelete")
SELECT m."Id", 1, true, true, true, true
FROM "Menus" m
WHERE (m."Path" = '/cash-register-report' OR m."Path" = '/cash-register-movements?processType=TH')
  AND NOT EXISTS (
    SELECT 1 FROM "RoleMenus" rm 
    WHERE rm."MenuId" = m."Id" AND rm."RoleId" = 1
  );
