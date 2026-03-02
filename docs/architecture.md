# Mimari Özeti

Bu doküman ParPazar pazar yeri uygulamasının yüksek seviyeli mimarisini, çok dilli web arayüzünü ve Redis/Elasticsearch entegrasyonlarını özetler.

## Uygulama Katmanları
- Web (Blazor Server): `ecommerce.Web`
  - Giriş: `Program.cs`
  - App: `Components/App.razor`, yönlendirme: `Components/Routes.razor`
  - Sayfalar: `Components/Pages/*` (`Home.razor`, `CategoryLeftSideBar.razor` → `/product-search`, `CategorySidebarPage.razor` → `/search`)
  - Layout: `Components/Layout/*` (`HeaderMenu.razor`)
- Domain Servisleri: `ecommerce.Web.Domain/Services/*`
  - Örnek: `ManufacturerElasticService.cs`
- Ortak Katman: `Common/ecommerce.Core/*`, `Common/ecommerce.Domain.Shared/*`
- Veri Katmanı: `Common/ecommerce.EFCore/*`
  - Context: `Context/ApplicationDbContext.cs`
  - UnitOfWork: `UnitOfWork/*`
  - Migrationlar: `Migrations/*`

## Pazar Yeri Özellikleri
- Çok satıcılı alışveriş sepeti, satıcı başlıkları ve toplamlar (Header sepet dropdown)
- Ürün listeleme ve detay akışı `ProductComponent` üzerinden
- Gelişmiş filtreler (kategori, marka, DOT, üretici, model/alt model)

## Çok Dilli (Localization)
- Dil seçimi Header’da; sayfalarda `@lang["..."]` kullanımı
- Kaynaklar ve dil listesi `lang` servisi ile sunuluyor

## Elasticsearch Entegrasyonu
- Servis: `ecommerce.Web.Domain/Services/ManufacturerElasticService.cs`
  - Üretici listesi ve detay sorguları
  - Sıralama ve EF fallback
- Kullanım alanları: Header marka slider/flyout, arama filtreleri

## Redis Entegrasyonu
- Uygulama genelinde cache/state yönetimi için Redis kullanılır
- DI ile servislerde yapılandırma (örn. cache servisleri ve kısa süreli veri saklama)

## Statik Assetler
- CSS: `wwwroot/assets/css/custom.css` (header, flyout, mega menü, mobil)
- JS: `wwwroot/assets/js/manufacturer-slider.js`, `infinite-scroll.js`, `car-brand-logos.js`

## Önemli Akışlar
- Header arama: canlı sonuçlar, ürün/marka sekmeleri (`HeaderMenu.razor`)
- Marka flyout: logo + arama + model/alt model (`HeaderMenu.razor`)
- Ürün arama ve sonsuz kaydırma: `CategoryLeftSideBar.razor` ve `wwwroot/assets/js/infinite-scroll.js`

