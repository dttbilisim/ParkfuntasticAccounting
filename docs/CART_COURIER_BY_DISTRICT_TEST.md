# Sepet + İlçeye Göre Kurye Test Rehberi

Bu rehber, kullanıcı adresi (il/ilçe) seçildiğinde **sadece o ilçede hizmet veren kuryelerin** teslimat seçeneklerinde görünmesini test etmek için adımları açıklar.

## Akış Özeti

1. **Backend**: `CourierServiceAreas` tablosunda her kurye için il (CityId) + ilçe (TownId) tanımlıdır.
2. **Checkout**: Kullanıcı adres seçince mobil uygulama `GET /api/DeliveryOptions?cityId=...&townId=...` çağırır.
3. **API**: Sadece seçilen il/ilçe ile eşleşen hizmet bölgesi olan **aktif** kuryeler döner; yanıtta "Kargo" + (varsa) "Kurye – Hızlı teslimat" seçenekleri gelir.
4. Kullanıcı kurye seçip siparişi tamamlarsa siparişe o kurye atanır.

## Test İçin Gerekenler

- En az **bir onaylı ve aktif kurye** (Admin’de Kurye Başvuruları’ndan onaylanmış).
- Bu kurye için **en az bir hizmet bölgesi** (il + ilçe) tanımlı olmalı.
- Test kullanıcısının (Cari veya B2C) **en az bir adresi** aynı **il + ilçe** ile kayıtlı olmalı (CityId, TownId dolu).

---

## Adım 1: Kurye ve Hizmet Bölgesi (Admin)

1. **Admin** → **Kurye Yönetimi** → **Kuryeler**.
2. Onaylı bir kurye seç (veya önce **Başvurular**’dan bir başvuruyu onayla).
3. Kurye satırında **Hizmet bölgeleri** (veya benzeri) butonuna tıkla.
4. **İl** ve **İlçe** seçip ekle (ör: İstanbul, Kadıköy). İsteğe bağlı mahalle (NeighboorId) boş bırakılabilir.
5. Kaydet.

Böylece `CourierServiceAreas` tablosuna bu kurye için il/ilçe kaydı düşer.

---

## Adım 2: Kullanıcı Adresi (Aynı İl/İlçe)

Teslimat seçeneklerinde kurye çıkması için, checkout’ta seçilen adresin **CityId** ve **TownId** değerleri, kuryenin hizmet bölgesiyle **aynı** olmalı.

- **Mobil**: Giriş yap → Profil/Adresler’den yeni adres ekle veya düzenle; **İl** ve **İlçe** alanlarını, kurye hizmet bölgesiyle aynı seç (örn. İstanbul, Kadıköy). Kaydet.
- **Veritabanı**: Adresler `UserAddress` (veya Cart API’nin döndüğü adres kaynağı) tablosunda tutuluyor. Test kullanıcısının bir adresinde `CityId` ve `TownId` değerlerini, Adım 1’de seçtiğin il/ilçe ID’leriyle eşleştir.

Örnek kontrol (kendi veritabanına göre tablo/ad alanlarını uyarla):

```sql
-- Örnek: Kullanıcı adreslerinde il/ilçe
SELECT ua."Id", ua."AddressName", ua."CityId", ua."TownId", c."Name" AS CityName, t."Name" AS TownName
FROM "UserAddresses" ua
LEFT JOIN "Cities" c ON c."Id" = ua."CityId"
LEFT JOIN "Towns" t ON t."Id" = ua."TownId"
WHERE ua."ApplicationUserId" = <TEST_USER_ID>;
```

---

## Adım 3: Sepet + Checkout (Mobil)

1. **Giriş**: Test kullanıcısıyla (Cari veya B2C) mobil uygulamaya giriş yap.
2. **Sepet**: Sepete en az bir ürün ekle.
3. **Checkout**: Sepetten **Siparişi Tamamla** (veya Ödeme/Checkout) butonuna tıkla.
4. **Adres**: Teslimat adresi olarak **Adım 2’de il/ilçesi eşleşen adresi** seç → **Devam**.
5. **Teslimat seçeneği**: Bu adımda **“Kurye – Hızlı teslimat”** seçeneği görünmeli (il/ilçe eşleştiği için). Görünmüyorsa:
   - Adresin gerçekten aynı CityId/TownId ile kayıtlı olduğunu kontrol et.
   - `GET /api/DeliveryOptions?cityId=X&townId=Y` çağrısını tarayıcı veya Postman ile dene; yanıtta `type: 1` (Kurye) olan kayıt var mı bak.
6. **Kurye seçimi**: “Kurye – Hızlı teslimat”ı seçip ödeme/adım akışını tamamla.
7. **Sipariş**: Sipariş oluştuktan sonra Admin veya kurye panelinde bu siparişe ilgili kuryenin atandığını doğrula.

---

## İlçe Değişince Kurye Düşmesi

- **Farklı ilçe**: Kullanıcı checkout’ta **başka bir il/ilçeye** sahip adres seçerse (ör. İstanbul, Üsküdar), `DeliveryOptions` sadece o il/ilçe için sorgulanır. O ilçede hizmet bölgesi tanımlı kurye yoksa yanıtta sadece **Kargo** gelir; kurye seçeneği **görünmez**.
- Böylece “ilçeye göre kurye düşürme” davranışı test edilmiş olur: sadece seçilen ilçede hizmet veren kuryeler listelenir.

---

## API Özeti

| Amaç | Metot | Endpoint |
|------|--------|----------|
| Teslimat seçenekleri (il/ilçeye göre kurye) | GET | `/api/DeliveryOptions?cityId={id}&townId={id}` |
| Adresler (cityId/townId dahil) | GET | `/api/Cart/addresses` (veya seçili müşteriye göre) |

---

## Hızlı Veri Kontrolü (SQL)

Kurye ve hizmet bölgesi kayıtları:

```sql
-- Aktif kuryeler
SELECT "Id", "ApplicationUserId", "Status" FROM "Couriers" WHERE "Status" = 1;

-- Kurye hizmet bölgeleri (il/ilçe)
SELECT csa."Id", csa."CourierId", csa."CityId", csa."TownId", c."Name" AS CityName, t."Name" AS TownName
FROM "CourierServiceAreas" csa
JOIN "Cities" c ON c."Id" = csa."CityId"
JOIN "Towns" t ON t."Id" = csa."TownId";
```

Örnek hizmet bölgesi ekleme (ID’leri kendi veritabanına göre değiştir):

```sql
-- Sadece örnek: CourierId, CityId, TownId kendi veritabanındaki geçerli ID'lerle değiştir
INSERT INTO "CourierServiceAreas" ("CourierId", "CityId", "TownId", "NeighboorId")
VALUES (1, 34, 1234, NULL);   -- ör: İstanbul=34, Kadıköy ilçe ID'si
```

Tablo/sütun adları projede farklıysa (örn. `UserAddress` tek tablo, Pascal case) migration veya DbContext’e bakarak güncelle.

Bu adımlarla sepet tarafını kullanıcı ve ilçeye göre kurye düşecek şekilde test edebilirsin.
