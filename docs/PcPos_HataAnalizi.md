# PcPos Transfer Hata Analizi

## 1. Veritabanı Kolon Hataları

### 1.0a `column p.VatSales does not exist` ✅ DÜZELTİLDİ
- **Kaynak:** Product tablosu (TransferProductsAsync sorgusu)
- **Sebep:** Product tablosunda VatSales yok, vergi oranı Tax tablosundan (TaxRate) alınmalı
- **Çözüm:** PcPos DataTransferService'te `P."VatSales"` → `COALESCE(T."TaxRate", 0) AS "VatSales"` ve `LEFT JOIN "Tax" T ON T."Id" = P."TaxId"` eklendi

### 1.0 `column pi.ImageUrl does not exist` ✅ DÜZELTİLDİ
- **Kaynak:** ProductImages tablosu (TransferProductsAsync sorgusu)
- **Sebep:** ProductImages tablosunda ImageUrl yok, FileGuid/FileName/Root var
- **Çözüm:** PcPos DataTransferService'te `PI."ImageUrl"` kaldırıldı, sadece `CONCAT(PI."Root", PI."FileName")` kullanılıyor

### 1.1 `column p.CategoryId does not exist`
- **Kaynak:** Product tablosu
- **Sebep:** CategoryId migration'ı uygulanmamış veya tablo adı farklı (PostgreSQL'de "Product" vs "product")
- **Çözüm:** `dotnet ef database update` çalıştırın. `20260304230000_AddCategoryIdToProduct` migration'ının uygulandığından emin olun.

### 1.2 `column cp.Name does not exist` ✅ DÜZELTİLDİ
- **Kaynak:** Currencies tablosu (TransferPosAsync sorgusu)
- **Sebep:** Currencies tablosunda `Name` yok, `CurrencyName` ve `CurrencyCode` var
- **Çözüm:** PcPos DataTransferService'te `CP."Name"` → `CP."CurrencyName"` olarak değiştirildi

### 1.3 `column "IsActive" does not exist` (AspNetUsers) ✅ DÜZELTİLDİ
- **Kaynak:** AspNetUsers tablosu (TransferUsersAsync)
- **Sebep:** Identity tablosunda IsActive kolonu yok
- **Çözüm:** ApplicationUser'a IsActive eklendi, migration `20260305000000_AddPcPosCompatibilityColumns` oluşturuldu

---

## 2. Eksik Tablolar

### 2.1 `relation "VersionApp" does not exist` ✅ DÜZELTİLDİ
- **Kaynak:** AuthStorageService - uzak sürüm kontrolü
- **Sebep:** VersionApp tablosu ecommerce veritabanında yok
- **Çözüm:** VersionApp entity ve tablosu eklendi, migration ile oluşturuldu

### 2.2 `relation "TransactionMaster" does not exist` ✅ DÜZELTİLDİ
- **Kaynak:** TransferService - satış aktarımı, trigger enable/disable
- **Sebep:** TransactionMaster tablosu ecommerce'de yoktu
- **Çözüm:** TransactionMaster ve tTransaction entity/tabloları eklendi, SQL script ile oluşturuluyor

---

## 3. Veri / İş Mantığı Hataları

### 3.1 `TransferCurrencyPricesAsync: Geçersiz CurrencyId: 0` ✅ DÜZELTİLDİ
- **Sebep:** Dapper dynamic ile PostgreSQL'den gelen kolon adı (ID/Id/id) eşleşmiyordu
- **Çözüm:** PcPos DataTransferService'te item.ID, item.Id ve IDictionary fallback eklendi

### 3.2 `Product.IsActive` kullanımı ✅ DÜZELTİLDİ
- **Sebep:** Product entity'sinde IsActive yok, `Status` var (AuditableEntity)
- **Çözüm:** PcPos DataTransferService'te `P."Status"` kullanılarak IsActive hesaplanıyor

---

## 4. Harici Hatalar (Kod Dışı)

### 4.1 `Mailbox unavailable - message rate limit`
- **Sebep:** Email servisi rate limit aşıldı
- **Çözüm:** E-posta gönderim sıklığını azaltın veya SMTP limitlerini kontrol edin

### 4.2 `System.Formats.Nrbf` assembly uyarısı
- **Sebep:** .NET MAUI/Mac Catalyst runtime uyarısı
- **Çözüm:** Genelde görmezden gelinebilir

### 4.3 `UIBackgroundModes fetch` uyarısı
- **Sebep:** Info.plist'te "fetch" modu tanımlı değil
- **Çözüm:** Gerekirse Info.plist'e `UIBackgroundModes` → `fetch` ekleyin

---

## 5. Öncelik Sırası

| Öncelik | Hata | Yapılacak |
|---------|------|-----------|
| 1 | CategoryId | Migration uygula |
| 2 | Currencies.Name | ✅ PcPos'ta CurrencyName kullanıldı |
| 3 | AspNetUsers.IsActive | ✅ Entity + SQL script |
| 4 | Product.IsActive | ✅ PcPos sorgusunda Status kullanıldı |
| 5 | VersionApp | ✅ SQL script ile tablo |
| 6 | Customers.CompanyCode | ✅ Entity + SQL script |
| 7 | TransactionMaster | Şema eşlemesi / tablo oluşturma |
| 8 | CurrencyId: 0 | ✅ PcPos'ta ID mapping düzeltildi |

---

## 6. Migration'ları Uygulama (ÖNEMLİ)

EF migrations `20260304230000` ve `20260305000000` Designer dosyası olmadığı için otomatik tanınmıyor. **Manuel SQL script** kullanın:

```bash
# PcPos bağlandığı PostgreSQL veritabanında çalıştırın:
psql -h 92.204.172.6 -p 5454 -U postgres -d veritabani_adi -f Common/ecommerce.EFCore/Migrations/Scripts/20260305_PcPosCompatibility_Manual.sql
```

Veya pgAdmin / DBeaver ile script dosyasını açıp çalıştırın:
`Common/ecommerce.EFCore/Migrations/Scripts/20260305_PcPosCompatibility_Manual.sql`
