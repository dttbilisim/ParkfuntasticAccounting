# PcPos Transfer - Tablo Eşleme ve Eksik Alanlar

## 1. Tablo İsim Karşılıkları

| PcPos Tablosu | Bizdeki Tablo | Entity | Durum |
|---------------|---------------|--------|-------|
| tProduct | Product | Product | ✅ Var - eksik kolonlar var |
| tProductImage | ProductImages | ProductImage | ✅ Var - ImageUrl farklı (FileGuid/FileName/Root) |
| tMainGroup | Category | Category | ✅ Var |
| tProductBranch | **YOK** | - | ❌ Eksik - ProductId + BranchId |
| tProductUnit | ProductUnits | ProductUnit | ✅ Var - eksik: CompanyCode |
| tUnits | Units | Unit | ✅ Var |
| tUser | AspNetUsers | ApplicationUser | ✅ Var - eksik: CaseIds, IsEdit, CompanyCode, UserType |
| tCashType | PaymentTypes | PaymentType | ✅ Var - eksik: PaymentType, CurrencyId, IsPcPos |
| tCurrencyPrice | Currencies | Currency | ✅ Var - isim farklı (BuyPrice/SalesPrice) |
| tCustomer | Customers | Customer | ✅ Var - eksik: IsPcPos, IsCredit, IsVatExcluded, IsCurrentPricesUpdatable, IsStreetAgency, CompanyCode |
| tSaleOptions | **YOK** | - | ❌ Eksik tablo |
| tProductSaleItems | **YOK** | - | ❌ Eksik - Paket ürün bileşenleri |
| tTransactionMaster | **Kaldırıldı** | - | ❌ CustomerAccountTransaction kullanılacak (cari hareket) |
| tTransaction | **Kaldırıldı** | - | ❌ Satır detayları için Order/OrderItems veya Invoice kullanılabilir |

---

## 2. Kolon Bazlı Eşleme ve Eksikler

### tProduct → Product
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | Id | - |
| Name | Name | - |
| CatID | ProductCategories → CategoryId | - |
| VatSales | Tax.TaxRate (TaxId üzerinden) | - |
| StatusPcPos | **YOK** | ❌ Ekle |
| StatusSales | **YOK** | ❌ Ekle |
| IsActive | Status (farklı mantık) | - |
| **Filtre** | **Product.IsSoldOnPcPos = true** | ✅ DataTransferService ürün transferinde sadece IsSoldOnPcPos=true olanları aktarmalı |

### tProductImage → ProductImages
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ProductID | ProductId | - |
| ImageUrl | FileGuid/FileName/Root (birleşik URL) | İsim farklı - sen düzelteceksin |

### tMainGroup → Category
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | Id | - |
| Name | Name | - |

### tProductBranch → **YENİ TABLO**
| PcPos Kolon | Açıklama | Ekle |
|-------------|----------|------|
| StockId | ProductId FK | ✅ |
| BranchID | BranchId FK | ✅ |

### tProductUnit → ProductUnits
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| StockID | ProductId | - |
| Barcode | Barcode | - |
| Value | UnitValue | - |
| IsActive | Status | - |
| CompanyCode | **YOK** | ❌ Ekle |
| Name | UnitId FK | - |

### tUnits → Units
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | Id | - |
| Name | Name | - |

### tUser → AspNetUsers (ApplicationUser)
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| CompanyCode | **YOK** | ❌ Ekle |
| UserName | UserName | - |
| Pass | PasswordHash | - |
| Name | FirstName | - |
| SurName | LastName | - |
| CaseIds | PcPosDefinition Id'leri (virgülle ayrılmış) | ✅ Var |
| IsActive | (EmailConfirmed vb.) | - |
| IsEdit | **YOK** | ❌ Ekle |
| UserType | **YOK** | ❌ Ekle (2=POS kullanıcısı) |

### tCashType / Pos → **BankAccounts** (banka tanımları)
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | BankAccount.Id | - |
| Name | BankName + " - " + AccountName | - |
| PaymentType | BankAccount.PaymentType (1=Nakit, 2=KrediKarti, 3=HavaleEFT, 4=Cek) | - |
| CurrencyId | BankAccount.CurrencyId | - |
| CurrencyName | Currencies.CurrencyName/Code | - |
| IsActive | BankAccount.Active | - |

### tCurrencyPrice → Currencies
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | Id | - |
| Name | CurrencyCode/CurrencyName | İsim farklı |
| BuyPrice | ForexBuying | - |
| SalesPrice | ForexSelling | - |
| Created | CreatedDate | - |
| Updated | ModifiedDate | - |

### tCustomer → Customers
| PcPos Kolon | Bizdeki Karşılık | Eksik? |
|-------------|------------------|--------|
| ID | Id | - |
| Name | Name | - |
| CompanyCode | Code veya Corporation | İsim farklı |
| IsPcPos | **YOK** | ❌ Ekle |
| IsCredit | **YOK** | ❌ Ekle |
| IsVatExcluded | **YOK** | ❌ Ekle |
| IsCurrentPricesUpdatable | **YOK** | ❌ Ekle |
| IsStreetAgency | **YOK** | ❌ Ekle (Sokak Acentesi) |

### tSaleOptions → **YENİ TABLO**
| PcPos Kolon | Açıklama | Ekle |
|-------------|----------|------|
| ID | PK | ✅ |
| Name | nvarchar | ✅ |

### tProductSaleItems → **YENİ TABLO** (Paket ürün bileşenleri)
| PcPos Kolon | Açıklama | Ekle |
|-------------|----------|------|
| ID | PK | ✅ |
| RefProductId | Paket ürün ID | ✅ |
| ProductId | Alt ürün ID | ✅ |
| CurrencyId | nullable | ✅ |
| Price | decimal | ✅ |

---

## 3. Eklenecek Özet

### Entity'lere eklenecek kolonlar
- **Customer**: IsPcPos, IsCredit, IsVatExcluded, IsCurrentPricesUpdatable, IsStreetAgency
- **PaymentType**: CurrencyId, Type (PaymentType kodu), IsPcPos
- **ApplicationUser**: CaseIds, IsEdit, UserType, CompanyCode
- **Product**: StatusPcPos, StatusSales
- **ProductUnit**: CompanyCode

### Yeni tablolar
- **ProductBranch**: ProductId, BranchId
- **SaleOptions**: Id, Name
- **ProductSaleItems**: RefProductId, ProductId, CurrencyId, Price (paket ürün bileşenleri)

---

## 4. Yapılan Değişiklikler (Özet)

### Entity değişiklikleri
- **Customer**: IsPcPos, IsCredit, IsVatExcluded, IsCurrentPricesUpdatable, IsStreetAgency eklendi
- **PaymentType**: Type, CurrencyId, IsPcPos, CompanyCode eklendi
- **ApplicationUser**: CompanyCode, CaseIds, IsEdit, UserType eklendi
- **Product**: StatusPcPos, StatusSales eklendi
- **ProductUnit**: CompanyCode eklendi

### Yeni tablolar
- **ProductBranch**: ProductId, BranchId (ürün-şube eşlemesi)
- **SaleOptions**: Id, Name (tSaleOptions)
- **ProductSaleItems**: RefProductId, ProductId, CurrencyId, Price (paket ürün bileşenleri)

### UI güncellemeleri
- **Customer modal**: PcPos Transfer bölümü (IsPcPos, IsCredit, IsVatExcluded, IsCurrentPricesUpdatable, IsStreetAgency) eklendi
- **PaymentType modal**: Type, CurrencyId, IsPcPos, CompanyCode eklendi
- **ApplicationUser modal**: CompanyCode, CaseIds, IsEdit, UserType eklendi
- **SaleOptions**: Yeni sayfa `/sale-options` + CRUD

### Migration
- `20260304215752_AddPcPosTransferColumnsAndTables` oluşturuldu
- `dotnet ef database update` ile uygulanabilir

---

## 5. UI/CRUD Eklenecekler (Henüz yapılmamış)
- Customer modal: IsPcPos, IsCredit, IsVatExcluded, IsCurrentPricesUpdatable, IsStreetAgency (Sokak Acentesi vb.)
- ApplicationUser modal: CaseIds, IsEdit
- PaymentType modal: CurrencyId, Type, IsPcPos
- SaleOptions: Yeni sayfa + CRUD
- ProductSaleItems: Paket ürün yönetimi (ürün detay/modal içinde)
- ProductBranch: Ürün-şube atama

---

## 6. Transfer Sorgu Düzeltmeleri (2026-03)

### Product
- Tablo: `Product`, `ProductImages`, `Category`, `ProductCategories`, `Tax`, `PriceListItems`
- Fiyat: `COALESCE(PriceListItems.SalePrice, Product.Price, 0)` - PriceListItems yoksa Product.Price
- Filtre: Şube filtresi kaldırıldı, tüm ürünler çekiliyor
- **CategoryId:** `Product.CategoryId` null ise `ProductCategories` tablosundan alınır (LATERAL subquery)
- **Image:** `ProductImages` tablosundan LATERAL join ile her ürün için ilk resim (`Order` sırasına göre) seçilir. URL: `Root` + `/` (gerekirse) + `FileName` veya `FileGuid`

### Customer
- Tablo: `Customers`
- Filtre: `CompanyCode` boşsa tüm müşteriler, doluysa eşleşenler. `IsPcPos=true` zorunluluğu kaldırıldı

### ProductUnits (Barkod)
- `CompanyCode` boşsa veya eşleşiyorsa barkodlar çekiliyor (NULL dahil, case-insensitive)
- Boş barkodlar filtreleniyor: `Barcode IS NOT NULL AND TRIM(Barcode) != ''`
- Alias'lar tırnak içinde: `Type`, `Quantity`, `IsActive` (PostgreSQL küçük harf dönüşümü önlenir)

### Pos (Ödeme Tipleri) → BankAccounts
- **Kaynak:** `BankAccounts` (banka tanımları), PaymentTypes yerine
- **Filtre:** `Active=true`, `Status=1`, BranchId eşleşen veya 0 (şube paylaşımlı)
- **Name:** `BankName + " - " + AccountName`
- **PaymentType:** BankPaymentType enum (1=Nakit, 2=KrediKarti, 3=HavaleEFT, 4=Cek)

---

## 7. CustomerAccountTransaction (PcPos Cari Hareket)

**TransactionMaster/tTransaction kaldırıldı.** PcPos satış aktarımında cari hareket (borç/alacak) için **CustomerAccountTransaction** tablosu kullanılacak.

| CustomerAccountTransaction | Açıklama |
|---------------------------|----------|
| CustomerId | Müşteri ID |
| BranchId | Şube ID |
| OrderId | Sipariş ID (opsiyonel) |
| InvoiceId | Fatura ID (opsiyonel) |
| TransactionType | Debit (Borç) / Credit (Alacak) |
| Amount | Tutar |
| TransactionDate | İşlem tarihi |
| Description | Açıklama |
| PaymentTypeId | Ödeme tipi |
| CashRegisterId | Kasa ID (nakit için) |
| ReferenceNo | Referans no (TransCode vb.) |

**Admin'de nerede görülür:**
- **Cari Hareketler** sayfası: `/b2b/customer-account-transactions`
- Müşteri detayında **Cari Bakiye** modalı (PlasiyerCustomers vb.)
- **CustomerAccountReport** (müşteri bazlı rapor)

**Not:** CustomerAccountTransaction satır detayları (ürün, miktar, fiyat) tutmaz. Sadece tutar + borç/alacak. PcPos satış özeti (toplam tutar) bu tabloya yazılır.

---

## 8. ProcessPayments → CashRegisterMovements (Kasa Hareketleri)

ProcessPayments, nakit ve POS ödemelerini **CashRegisterMovements** tablosuna yazar (admin projesindeki kasa hareketleri tablosu).

| CashRegisterMovement | Açıklama |
|----------------------|----------|
| CashRegisterId | **PaymentTypeId'e göre PcPos kasası** — AŞAĞIDAKİ MANTIK ZORUNLU |
| MovementType | 1=Kasa Girişi (In), 2=Kasa Çıkışı (Out) |
| ProcessType | 4=Perakende Satış (PS) |
| TransCode | Benzersiz işlem kodu |
| **PaymentTypeId** | **tCashType/Payment kaynağından — BOŞ GÖNDERİLMEMELİ. Nakit=1, KrediKartı=2 vb. PaymentTypes tablosundan eşleştir.** |
| CurrencyId | Currencies tablosundan (TL, USD, EUR) |
| Amount | Tutar (çıkışta negatif) |
| TransactionDate | İşlem tarihi |
| Description | Açıklama |
| BranchId | Settings.BranchId |

**CashRegisterId (KRİTİK — YANLIŞ KASA SEÇİMİ ÖNLENMELİ):**

Nakit ve kredi kartı ödemeleri **veresiye kasasına** yazılmamalı. Her ödeme tipi için **o tipe ait PcPos kasası** seçilmelidir:

1. **PaymentTypeId'e göre kasa seç:** `CashRegisters WHERE BranchId = Settings.BranchId AND PaymentTypeId = [ödeme tipi]`
   - Nakit (PaymentTypeId=1) → `PaymentTypeId = 1` olan kasa (IsCash)
   - Kredi kartı (PaymentTypeId=2) → `PaymentTypeId = 2` olan kasa (IsCreditCard)
   - Veresiye ödemeler CashRegisterMovements'a yazılmaz (cari hesaba gider)

2. **Tercih sırası:** `PaymentType.IsPcPos = true` olan PaymentType'a bağlı kasalar öncelikli. Yoksa aynı PaymentTypeId'li ilk kasa.

3. **YANLIŞ:** `BranchId'ye göre ilk kasa` — Bu veresiye kasasını (Id=4 vb.) seçebilir; nakit/kart işlemleri yanlış kasaya yazılır.

4. **PcPosDefinitions.CashRegisterId** kolonu varsa, ilgili PcPos tanımına bağlı kasayı kullanabilirsin (opsiyonel).

**Örnek SQL (Transfer servisinde kullanılacak):**
```sql
-- Nakit (PaymentTypeId=1) için kasa
SELECT "Id" FROM "CashRegisters"
WHERE "BranchId" = @branchId AND "PaymentTypeId" = 1 AND "Status" <> 99
ORDER BY "IsDefault" DESC, "Id" LIMIT 1;

-- Kredi kartı (PaymentTypeId=2) için kasa
SELECT "Id" FROM "CashRegisters"
WHERE "BranchId" = @branchId AND "PaymentTypeId" = 2 AND "Status" <> 99
ORDER BY "IsDefault" DESC, "Id" LIMIT 1;
```

**Önemli:** ProcessPayments, ProcessSales'tan *sonra* çalışır. ProcessSales satışları `IsTrans=1` yapar. Bu yüzden Sale sorgusu `IsTrans=0` ile yapılmamalı; Payment'lardaki SaleId'lere göre satışlar çekilir (`Sale WHERE Id IN (...)`).

**Admin'de nerede görülür:**
- **Kasa Hareketlerim** sayfası: `/cash-register-movements`
- **Kasa Bakiyeleri** modalı (CashRegisterMovements sayfasından)
- **Tahsilat Listesi** (ProcessType=TH filtresi ile aynı sayfa)

---

## 9. PcPos Transfer Özeti – Admin'de Nerede Görülür?

| PcPos İşlemi | Tablo | Admin Sayfa | URL |
|--------------|-------|-------------|-----|
| Satış (toplam tutar) | CustomerAccountTransactions | Cari Hareketler | `/b2b/customer-account-transactions` |
| İade (alacak) | CustomerAccountTransactions | Cari Hareketler | `/b2b/customer-account-transactions` |
| Nakit tahsilat | CashRegisterMovements | Kasa Hareketlerim | `/cash-register-movements` |
| POS ödeme (kart vb.) | CashRegisterMovements | Kasa Hareketlerim | `/cash-register-movements` |
| Para üstü / İade | CashRegisterMovements | Kasa Hareketlerim | `/cash-register-movements` |

**Menü kurulumu:** Kasa sayfaları için `scripts/seed_kasa_islemleri_menu.sql` çalıştırılabilir. Cari Hareketler için menü yoksa:

```sql
-- Cari Hareketler menüsü (B2B veya Muhasebe altında)
INSERT INTO "Menus" ("ParentId", "Name", "Path", "Icon", "Tags", "Order")
SELECT COALESCE((SELECT "Id" FROM "Menus" WHERE "Name" = 'B2B' OR "Name" = 'Muhasebe' LIMIT 1), 0),
       'Cari Hareketler', '/b2b/customer-account-transactions', 'receipt_long', 'cari,borç,alacak', 10
WHERE NOT EXISTS (SELECT 1 FROM "Menus" WHERE "Path" = '/b2b/customer-account-transactions');
```
