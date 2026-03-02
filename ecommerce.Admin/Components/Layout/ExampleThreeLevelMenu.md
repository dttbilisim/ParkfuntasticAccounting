# Üç Seviyeli Menü Yapısı Örneği

Bu örnek, nasil üç seviyeli bir menü yapısı oluşturabileceğinizi gösterir:

## Veritabanı Yapısı

Menu tablosunda şu şekilde kayıtlar olmalı:

```sql
-- Ana Menü (1. seviye)
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (100, NULL, 'Raporlar Yönetimi', '', 'list');

-- Alt Menü (2. seviye) 
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (101, 100, 'Satış Raporları', '', 'bar_chart');
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (102, 100, 'Muhasebe Raporları', '', 'account_balance');

-- Alt-Alt Menü (3. seviye)
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (103, 101, 'Günlük Satış Raporu', 'daily-sales-report', 'today');
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (104, 101, 'Aylık Satış Raporu', 'monthly-sales-report', 'calendar_month');
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (105, 102, 'Gelir-Gider Raporu', 'income-expense-report', 'trending_up');
INSERT INTO "Menu" ("Id", "ParentId", "Name", "Path", "Icon") VALUES (106, 102, 'Bakiye Raporu', 'balance-report', 'account_balance_wallet');

-- Rol-Menü ilişkileri (Kullanıcının bu menüleri görebilmesi için)
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 100); -- Admin rolü için
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 101);
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 102);
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 103);
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 104);
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 105);
INSERT INTO "RoleMenu" ("RoleId", "MenuId") VALUES (1, 106);
```

## Sonuç

Bu yapı ile şu menü yapısı oluşur:

```
Raporlar Yönetimi (Ana Menü)
├── Satış Raporları (Alt Menü)
│   ├── Günlük Satış Raporu (Alt-Alt Menü)
│   └── Aylık Satış Raporu (Alt-Alt Menü)
└── Muhasebe Raporları (Alt Menü)
    ├── Gelir-Gider Raporu (Alt-Alt Menü)
    └── Bakiye Raporu (Alt-Alt Menü)
```

## Önemli Notlar

1. **ParentId**: Üst menünün Id'si ile eşleşmeli
2. **Path**: Sadece en alt seviye menülerde (3. seviye) route path verilmeli
3. **Icon**: Her seviyede farklı ikon kullanabilirsiniz
4. **RoleMenu**: Kullanıcının menñyü görebilmesi için gerekli rol-menu ilişkileri tanımlanmalı