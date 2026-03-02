# Kuryem Olur musun? – Modül Task Listesi

Mobil uygulamada kurye başvurusu, admin onayı, hizmet bölgeleri (il/ilçe/mahalle), sepet aşamasında kurye seçeneği, kurye sipariş akışı ve canlı konum takibi.

---

## Faz 1: Core – Entity & Enum

### Task K1 – Courier, CourierApplication, CourierServiceArea + Enum’lar

- **ecommerce.Core**
  - **Enum’lar** (`Enums.cs`):
    - `CourierApplicationStatus`: Pending, Approved, Rejected
    - `CourierDeliveryStatus`: PendingAssignment, Assigned, Accepted, PickedUp, OnTheWay, Delivered, Cancelled
    - `DeliveryOptionType`: Cargo = 0, Courier = 1 (sepet/checkout’ta kargo vs kurye ayrımı için)
  - **Entity: Courier** (`Entities/Courier.cs`)
    - Id, ApplicationUserId (FK → ApplicationUser), Status (Active/Passive), CreatedAt, UpdatedAt
    - Onaylanan başvuru sonrası bir kullanıcı “kurye” olur.
  - **Entity: CourierApplication** (`Entities/CourierApplication.cs`)
    - Id, ApplicationUserId (başvuran kullanıcı), Status (Pending/Approved/Rejected), AppliedAt, ReviewedAt, ReviewedByUserId, RejectReason (nullable), Phone, IdentityNumber (opsiyonel), Note
  - **Entity: CourierServiceArea** (`Entities/CourierServiceArea.cs`)
    - Id, CourierId (FK), CityId, TownId, NeighboorId (nullable – mahalle opsiyonel)
    - Unique: (CourierId, CityId, TownId, NeighboorId) – aynı bölge tekrar eklenmesin

### Task K2 – Order ilişkisi + CourierLocation

- **Orders** (mevcut `Orders.cs`):
  - `CourierId` (int?, FK → Courier) – kurye atanmış siparişler için
  - `CourierDeliveryStatus` (CourierDeliveryStatus?) – kurye teslimat durumu
  - `DeliveryOptionType` (DeliveryOptionType?) veya mevcut CargoId yanında “kurye mi kargo mu” bilgisi (tercihe göre Cargo enum’a Courier eklenebilir veya ayrı alan)
  - `EstimatedCourierDeliveryMinutes` (int?) – tahmini teslimat süresi (dakika)
- **Entity: CourierLocation** (`Entities/CourierLocation.cs`)
  - Id, CourierId (FK), OrderId (int?, nullable – genel konum için null), Latitude, Longitude, Accuracy (optional), RecordedAt (UTC)
  - Canlı takip: kurye konum atar, müşteri son konumu/geçmişi çeker

---

## Faz 2: EFCore – DbContext & Migration

### Task K3

- **ApplicationDbContext**
  - DbSet\<Courier\>, DbSet\<CourierApplication\>, DbSet\<CourierServiceArea\>, DbSet\<CourierLocation\>
  - İlişkiler: Courier → ApplicationUser, CourierApplication → ApplicationUser, CourierServiceArea → Courier, City, Town, Neighboor; CourierLocation → Courier, Order (optional)
  - Orders için CourierId, CourierDeliveryStatus (ve varsa DeliveryOptionType/EstimatedCourierDeliveryMinutes) konfigürasyonu
- **Migration**: AddCourierModule veya benzeri isimle oluşturulup uygulanacak.

---

## Faz 3: Admin.Services – DTO & Servis

### Task K4 – DTO’lar ve servisler (ecommerce.Admin.Domain)

- **CourierApplication**
  - DTO: CourierApplicationListDto, CourierApplicationUpsertDto (başvuru için – mobile’dan gelecek), CourierApplicationReviewDto (onay/red + sebep)
  - Interface: ICourierApplicationService – Create (mobile başvuru), GetPaged (admin liste), Approve, Reject
  - Servis: CourierApplicationService
- **Courier**
  - DTO: CourierListDto, CourierDetailDto
  - Interface: ICourierService – GetById, GetPaged, GetServiceAreas, SaveServiceAreas (il/ilçe/mahalle listesi)
  - Servis: CourierService (onay sonrası Courier kaydı oluşturma burada veya CourierApplicationService.Approve içinde)
- **CourierServiceArea**
  - DTO: CourierServiceAreaListDto, CourierServiceAreaUpsertDto (CityId, TownId, NeighboorId?)
  - CRUD: CourierService içinde veya ayrı ICourierServiceAreaService
- **CourierDelivery (sipariş–kurye)**
  - DTO: CourierDeliveryOptionDto (sepet için: kurye uygun mu, tahmini süre, ücret vb.), AssignCourierToOrderDto
  - Interface: ICourierDeliveryService – GetDeliveryOptionsForAddress(cityId, townId, [neighboorId]), AssignCourierToOrder(orderId, courierId), UpdateDeliveryStatus(orderId, status)
  - Servis: CourierDeliveryService (ecommerce.EP veya Admin.Services’ta olabilir; EP’den çağrılacak)
- **CourierLocation**
  - DTO: CourierLocationDto (Lat, Lng, RecordedAt, OrderId?)
  - Interface: ICourierLocationService – RecordLocation(courierId, orderId?, lat, lng), GetLatestForOrder(orderId), GetLatestForCourier(courierId)
  - Servis: CourierLocationService

Tüm interface ve implementasyonlar **ecommerce.Admin.Services** (ecommerce.Admin.Domain.csproj) içinde; EP sadece bu servisleri kullanacak.

---

## Faz 4: Admin – Sayfalar

### Task K5 – Admin UI (ecommerce.Admin)

- **Kurye Başvuruları** (`/courier-applications` veya mevcut menü yapısına göre)
  - Liste: Başvuru tarihi, kullanıcı adı/email/telefon, durum (Beklemede/Onaylandı/Reddedildi)
  - Onaylama: Onayla / Reddet butonu, red için sebep alanı
  - Onaylanan başvuruda otomatik Courier kaydı oluşturulacak (ApplicationUserId ile)
- **Kurye Hizmet Bölgeleri** (kurye detayında veya ayrı sayfa)
  - Kurye seçimi (dropdown veya kurye detay sayfasından)
  - İl → İlçe → Mahalle (opsiyonel) seçerek “hizmet bölgesi” ekleme/çıkarma
  - Liste: İl, İlçe, Mahalle kolonları

Menü: Örn. “Kurye Yönetimi” altında “Başvurular” ve “Kuryeler” (kurye listesi + hizmet bölgeleri).

**Yapılan (K5):**
- **Kurye Başvuruları** sayfası: `/courier-applications` — liste (durum filtresi), Onayla/Reddet; red için modal ile `RejectReason`.
- **Kuryeler** sayfası: `/couriers` — kurye listesi, satırda “Hizmet bölgeleri” ile il/ilçe CRUD modalı (mahalle opsiyonel, şu an sadece il+ilçe).
- Modallar: `ReviewCourierApplicationModal`, `CourierServiceAreasModal`.
- **Menü:** Sidebar menüsü `MenuService.GetMenusForCurrentUser()` ile veritabanından gelir. Bu sayfaları göstermek için Menus tablosuna (veya rol-menü eşlemesine) “Kurye Başvuruları” (URL: `/courier-applications`) ve “Kuryeler” (URL: `/couriers`) kayıtları eklenmeli; isteğe göre üst menü “Kurye Yönetimi” altında gruplanabilir.

---

## Faz 5: ecommerce.EP – Mobile API

### Task K6 – Başvuru, sepet kurye seçeneği, atama/kabul

- **CourierApplicationController** (veya Auth/User altında)
  - `POST /api/courier-application` – Mobil kullanıcı kurye başvurusu (CourierApplicationUpsertDto); ApplicationUserId token’dan alınır.
- **CourierDeliveryController** (veya Checkout/Cart ile ilişkili controller)
  - `GET /api/delivery-options?cityId=&townId=&neighboorId=` – Teslimat seçenekleri: kargo + (eğer bu il/ilçe/mahalle bir kurye hizmet bölgesindeyse) kurye seçeneği (teslimat süresi, hızlı teslimat vb.). Customer B2B/B2C il–ilçe bilgisi sepet/adres ile gelir; bu endpoint’e cityId/townId gönderilir.
  - Sipariş oluşturulurken deliveryOption: Cargo | Courier ve courierId (kurye seçildiyse) gönderilir; Order’a CourierId, CourierDeliveryStatus = Assigned (veya PendingAssignment), EstimatedCourierDeliveryMinutes kaydedilir.
- **CourierOrderController** (kurye rolü)
  - `GET /api/courier/orders` – Kuryenin kendine atanmış / atanabilir siparişler (duruma göre filtre)
  - `POST /api/courier/orders/{orderId}/accept` – Siparişi kabul et (CourierDeliveryStatus → Accepted)
  - `POST /api/courier/orders/{orderId}/status` – Durum güncelle: PickedUp, OnTheWay, Delivered, Cancelled (body: { status })
  - Push/notification: Yeni sipariş kurye uygulamasına düştüğünde bildirim (mevcut ExpoPushService ile entegre)

Tüm bu endpoint’ler **ecommerce.EP** içinde; servis katmanı **Admin.Services** (ICourierApplicationService, ICourierDeliveryService, ICourierService vb.) kullanılacak.

---

## Faz 6: EP – Konum ve canlı takip

### Task K7 – Konum API

- **CourierLocationController**
  - `POST /api/courier/location` – Kurye konum gönderir (courierId token’dan, body: orderId?, latitude, longitude, accuracy?). CourierLocationService.RecordLocation.
  - `GET /api/courier/orders/{orderId}/track` – Müşteri (veya kurye) siparişin son konumunu / konum geçmişini alır (canlı takip için polling veya SignalR sonra eklenebilir).
- **Policies / Authorization**: Kurye rolü veya “Courier” claim’i ile `[Authorize]` ve rol kontrolü.

---

## Faz 7: Mobile (React Native / Expo)

### Task K8 – Mobil ekranlar

- **Kurye başvuru ekranı**
  - Form: Telefon, TC (opsiyonel), not. Gönder → `POST /api/courier-application`.
- **Checkout / sepet**
  - Adres seçildiğinde (veya varsayılan adres) cityId, townId ile `GET /api/delivery-options` çağrılır.
  - Dönen listede “Kurye – Hızlı teslimat” gibi seçenek varsa radio/checkbox ile seçilir; sipariş oluşturulurken deliveryOption: Courier gönderilir.
- **Kurye paneli** (sadece kurye rolü)
  - Sipariş listesi (atanan / atanabilir), “Kabul et”, “Satıcıdan aldım”, “Yola çıktım”, “Teslim ettim” gibi durum butonları.
  - Arka planda konum gönderimi (periyodik veya “teslimatta” iken sürekli) → `POST /api/courier/location`.
- **Müşteri – sipariş detay / canlı takip**
  - Sipariş kurye ile ise “Kuryeyi takip et” butonu; ekranda harita + kurye son konumu. `GET /api/courier/orders/{orderId}/track` ile periyodik güncelleme (polling). İsteğe bağlı: SignalR/WebSocket ile anlık güncelleme.

---

## Özet checklist

| # | Task | Proje |
|---|------|--------|
| K1 | Courier, CourierApplication, CourierServiceArea entity + Enums | ecommerce.Core |
| K2 | Order alanları (CourierId, CourierDeliveryStatus) + CourierLocation | ecommerce.Core |
| K3 | DbSet, migration, ilişkiler | ecommerce.EFCore |
| K4 | DTO’lar + ICourierApplicationService, ICourierService, ICourierDeliveryService, ICourierLocationService | ecommerce.Admin.Services |
| K5 | Admin: Başvuru listesi + onay/red, Kurye hizmet bölgeleri CRUD | ecommerce.Admin |
| K6 | EP: Başvuru POST, delivery-options GET, kurye sipariş atama/kabul/status | ecommerce.EP |
| K7 | EP: Konum kaydet + sipariş canlı takip GET | ecommerce.EP |
| K8 | Mobile: Başvuru formu, checkout kurye seçeneği, kurye paneli, müşteri canlı takip | ecommercemobile |

Bu sırayla ilerlenebilir; K1→K2→K3 birbirine bağlı, K4 K3’ten sonra, K5–K6–K7 K4’ten sonra, K8 en son (API’ler hazır olunca).

---

## Mobile: Tema ve modal kuralları

- **5 tema:** purple, ocean, forest, sunset, lavender — renkler için `useColors()` / `useCreateStyles(createStyles, c)` kullanımı zorunlu.
- **Modal donma önleme:** Özel modallar `BaseModal` ile sarmalanmalı; GlobalModal kapanmada 320ms gecikmeli.
- **Kurye ekranları:** Mümkünse tam ekran sayfa (route), modal değil.
- Detay: ecommercemobile repo `b2bmobile/docs/COURIER_MODULE_DESIGN.md`.
