# Tenant Yapısı ve BranchId (ecommerce.Admin)

**Önemli:** Admin projesi **her zaman tenant yapısını** dikkate alır. Veri **şube (BranchId)** bazlıdır. Yeni servis, entity veya API eklerken mutlaka BranchId ve yetki filtrelerini uygula.

---

## 1. Temel kavramlar

- **Tenant:** Şirket (Corporation) → Şube (Branch) hiyerarşisi. Kullanıcı bir veya birden fazla şubeye yetkili olabilir.
- **BranchId:** Entity’nin hangi şubeye ait olduğu. `null` veya `0` bazen “global/ortak” anlamında kullanılır (entity’ye göre değişir).
- **ITenantProvider:** Mevcut kullanıcının seçili şubesini ve yetkilerini verir.
  - `GetCurrentBranchId()`: Seçili şube ID.
  - `IsGlobalAdmin`: Merkez/global admin mi?
  - `IsMultiTenantEnabled`: Çok şubeli mod açık mı?
- **IRoleBasedFilterService (RoleFilter):** Sorgulara kullanıcının erişebildiği şubeleri filtreler.
  - `ApplyFilter(query, dbContext)`: Query’yi yetkili şubelere göre kısıtlar.
  - `CanAccessBranchAsync(branchId, dbContext)`: Kullanıcının o şubeye erişimi var mı?

---

## 2. Entity’de BranchId

Aşağıdaki (ve benzeri) entity’ler **BranchId** veya **CorporationId** içerir; ilgili servislerde **mutlaka** tenant filtresi veya ataması yapılmalıdır:

| Entity | BranchId tipi | Not |
|--------|----------------|-----|
| PriceList | int? | CorporationId + BranchId |
| EmailTemplates | int? | null = ortak şablon |
| Product | int? | |
| Invoice | int | |
| InvoiceTypeDefinition | int | |
| Customer | int? | |
| Warehouse | int | BranchId zorunlu |
| PaymentType, CashRegister, ExpenseDefinition | int | |
| CollectionReceipt | int? | |
| Order | int? | |
| Discount, Brand, Category, Banner, Tax, Unit, Tier | int? veya int | |
| SalesPerson, Seller, ProductUnit, PcPosDefinition, BankAccount, … | int? veya int | |

Yeni bir entity’ye BranchId eklediğinde ilgili tüm listeleme/get/insert/update/delete akışlarında **filtreleme ve atama** kurallarını uygula.

---

## 3. Servis kuralları (her zaman uygula)

### 3.1 Listeleme (GetAll, GetPaged, List)

- Sorguyu **önce** `_roleFilter.ApplyFilter(query, _context.DbContext)` ile sınırla; **veya**
- `GetCurrentBranchId()` alıp `query = query.Where(x => x.BranchId == currentBranchId || x.BranchId == null)` gibi açık BranchId filtresi uygula (politikaya göre).
- Global/admin kullanıcılar için `currentBranchId == 0` veya `IsGlobalAdmin` durumunda farklı mantık kullanılıyorsa dokümante et.

### 3.2 Tekil getirme (GetById, GetByX)

- Kaydı getirdikten sonra **yetki kontrolü** yap: `CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext)`. Yetkisi yoksa 403/uyarı dön.

### 3.3 Insert (Create)

- Yeni entity’de **BranchId** (ve gerekiyorsa CorporationId) mutlaka set et.
- Genelde: `entity.BranchId = model.Dto.BranchId ?? _tenantProvider.GetCurrentBranchId();`
- Formdan şube seçiliyorsa DTO’daki BranchId’yi kullan; yoksa mevcut şubeyi kullan.
- Duplicate / benzersizlik kontrollerini **şube bazında** yap (aynı isim aynı şubede tekrarlanmasın).

### 3.4 Update (Edit)

- Mevcut kaydın BranchId’si üzerinde yetki kontrolü yap (`CanAccessBranchAsync(current.BranchId ?? 0, ...)`).
- Güncellemede BranchId değişecekse DTO’dan al ve yetkili şubeler listesinde olduğunu doğrula.
- ExecuteUpdateAsync kullanıyorsan `SetProperty(c => c.BranchId, model.Dto.BranchId)` (ve gerekiyorsa CorporationId) ekle.

### 3.5 Delete

- Silmeden önce `CanAccessBranchAsync(entity.BranchId ?? 0, ...)` ile yetki kontrolü yap.

### 3.6 DTO’lar

- BranchId (ve CorporationId) alanları **varsayılan değer olarak 1 veya 0 atanmasın**; seçilmemiş için `null` kullan (formda “Seçiniz” için).

---

## 4. Örnek servis kalıbı

```csharp
// Constructor
private readonly ITenantProvider _tenantProvider;
private readonly IRoleBasedFilterService _roleFilter;

// List
var query = _context.DbContext.MyEntities.Where(x => x.Status != (int)EntityStatus.Deleted);
query = _roleFilter.ApplyFilter(query, _context.DbContext);
// veya: var branchId = _tenantProvider.GetCurrentBranchId();
//       if (branchId > 0) query = query.Where(x => x.BranchId == branchId || x.BranchId == null);

// GetById sonrası
if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext))
{
    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
    return rs;
}

// Insert
entity.BranchId = model.Dto.BranchId ?? _tenantProvider.GetCurrentBranchId();

// Update
.SetProperty(c => c.BranchId, model.Dto.BranchId)
```

---

## 5. Agent: Unutulan servisleri kontrol et

Yeni bir servis yazıldığında veya “tenant / BranchId eksik mi?” denetimi istendiğinde:

1. **Entity’yi kontrol et:** İlgili entity’de `BranchId` (veya `CorporationId`) var mı? Varsa aşağıdakiler zorunludur.
2. **Servisi kontrol et:**
   - Listeleme metodlarında sorgu `ApplyFilter` veya açık `BranchId` filtresi ile sınırlanıyor mu?
   - GetById (ve benzeri) sonrası `CanAccessBranchAsync` çağrılıyor mu?
   - Insert’te `BranchId` (ve gerekiyorsa `CorporationId`) set ediliyor mu?
   - Update’te BranchId güncelleniyor mu (ve yetki kontrolü yapılıyor mu)?
   - Delete’te şube yetkisi kontrol ediliyor mu?
3. **DTO’yu kontrol et:** BranchId/CorporationId varsayılanı yanlış (örn. `= 1`) atanmamalı; seçilmemiş için `null` kullan.

Eksik gördüğün servislerde bu kuralları uygula; değişiklikleri yaparken mevcut davranışı (global admin, çok şubeli mod vb.) bozmamaya dikkat et.

---

## 6. Referans servisler

- **PriceListService:** ApplyFilter, GetCurrentBranchId, insert/update’te BranchId/CorporationId, CanAccessBranchAsync.
- **InvoiceService:** BranchId filtreleri, allowedBranchIds, insert’te BranchId ataması.
- **EmailTemplateService:** GetCurrentBranchId ile liste/GetById/Upsert/Delete’te BranchId filtresi ve ataması.
- **InvoiceTypeService:** GetCurrentBranchId ile liste BranchId filtresi.
- **ProductService:** IsGlobalAdmin, GetCurrentBranchId, UserBranches, BranchId filtreleri.
- **CustomerService:** ApplyFilter, BranchId filtreleme.
- **WarehouseService:** ApplyFilter.

Detay için bu servislerin listeleme, get-by-id, insert ve update bloklarına bak.
