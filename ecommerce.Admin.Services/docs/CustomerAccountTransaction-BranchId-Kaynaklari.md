# CustomerAccountTransaction — BranchId Boş Gelme Kaynakları

Bu dokümanda `CustomerAccountTransaction` kaydının **hangi servislerde / nerede** oluşturulduğu ve **BranchId**'nin nasıl atandığı listelenir. BranchId boş (NULL) kayıtların hangi akıştan geldiğini bulmak için kullanılır.

---

## 1. Doğrudan entity oluşturup Insert eden yerler

Bu yerlerde `new CustomerAccountTransaction { ... }` ile entity oluşturulup `Insert`/`InsertAsync` ile veritabanına yazılıyor. **BranchId mutlaka atanmalı.**

| # | Dosya | Metod | BranchId ataması | Not |
|---|-------|--------|------------------|-----|
| 1 | **CustomerAccountTransactionService.cs** | `CreateTransaction` | `BranchId = _tenantProvider.GetCurrentBranchId()` | Manuel cari hareket, plasiyer tahsilat, EP callback hepsi bu metodu kullanır. BranchId her zaman set ediliyor. |
| 2 | **InvoiceService.cs** | `ReverseCustomerAccountTransactions` (reverseTransaction) | `BranchId = transaction.BranchId ?? _tenantProvider.GetCurrentBranchId()` | İptal ters kayıt; orijinal hareketin BranchId’si yoksa mevcut şube kullanılıyor. |
| 3 | **InvoiceService.cs** | `CreateCustomerAccountTransactionForInvoice` | `BranchId = invoice.BranchId` | Fatura kesildiğinde borç/alacak; fatura hangi şubeyse o şube atanıyor. |
| 4 | **CheckService.cs** | `CreateCustomerAccountTransactionForCheckAsync` | `BranchId = check.BranchId` | Çek portföye alındığında Alacak; tahsil/red/iade/silme (iptal) durumunda Borç. CheckId ile ilişkilidir. |

Başka yerde `new CustomerAccountTransaction` + `Insert` yok; tüm yeni kayıtlar yukarıdaki dört noktadan gelir.

---

## 2. CreateTransaction çağıran servisler (BranchId CreateTransaction içinde set edilir)

Bu servisler **entity oluşturmuyor**; sadece DTO ile `ICustomerAccountTransactionService.CreateTransaction` çağırıyor. BranchId, **CustomerAccountTransactionService.CreateTransaction** içinde `_tenantProvider.GetCurrentBranchId()` ile atanıyor.

| Servis / Controller | Metod | Ne zaman |
|---------------------|--------|----------|
| **PaymentCollectionService** | `CollectCashPayment` | Plasiyer nakit tahsilat (siparişli veya serbest) |
| **ecommerce.EP – MobilePaymentCallbackController** | Plasiyer 3D Secure callback | Plasiyer kart tahsilatı tamamlandığında |

Bu akışlarda BranchId boş gelmemesi için: **CreateTransaction** içinde `BranchId = _tenantProvider.GetCurrentBranchId()` kullanıldığı sürece (şu an öyle) sorun yok. EP tarafında istek yapan kullanıcının context’inde `ActiveBranchId` / cookie doğru set edilmiş olmalı.

---

## 3. Özet: BranchId’nin boş kalabileceği tarihî kaynaklar

- **InvoiceService** içinde fatura keserken ve iptal ters kaydında BranchId daha önce atanmıyordu; bu iki yere BranchId ataması eklendi.
- **CustomerAccountTransactionService.CreateTransaction** zaten BranchId atıyordu; değişiklik yok.
- Veritabanında **zaten BranchId = NULL** olan kayıtlar büyük ihtimalle:
  - Eski **InvoiceService** akışından (fatura kesimi veya iptal), veya
  - Çok eski bir **CreateTransaction** sürümünden (eğer bir dönem BranchId atanmadıysa)  
  kalmış olabilir.

İleride boş kayıt görürseniz:  
1) Hangi işlemden oluştuğunu (fatura, iptal, plasiyer nakit, plasiyer kart) tespit edin.  
2) Yukarıdaki tabloya göre ilgili servis/metodu kontrol edin; BranchId ataması orada yapılıyor olmalı.

---

## 4. Yeni kayıtlarda BranchId’nin boş atanmaması

- **CustomerAccountTransactionService.CreateTransaction:** `BranchId = _tenantProvider.GetCurrentBranchId()` (int; null değil).
- **InvoiceService – reverse:** `BranchId = transaction.BranchId ?? _tenantProvider.GetCurrentBranchId()`.
- **InvoiceService – fatura kesimi:** `BranchId = invoice.BranchId`.
- **CheckService – çek cari hareketi:** `BranchId = check.BranchId`.

Bunlar dışında `CustomerAccountTransaction` insert eden başka bir yer yok; bu dört nokta kontrol altında olduğu sürece yeni kayıtlarda BranchId boş kalmamalı.
