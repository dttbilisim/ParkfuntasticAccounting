# OtoIsmail Product Sync Procedure (`sync_products_from_otoismails.sql`)

## 📌 Amaç
Otoİsmail tedarikçisinden gelen ham ürün verilerini (`ProductOtoIsmails`), sistemin ana ürün tabloları olan `Product` ve `SellerItems` tablolarına senkronize etmektir.

## ⚙️  İşlem Adımları
1.  **Hazırlık:**
    *   Güncel döviz kurlarını (`Currencies`) geçici tabloya alır.
    *   Tedarikçi tablosundaki verileri temizler (TRIM, INITCAP) ve fiyatları sayısal formata çevirir.
2.  **Fiyat Hesaplama:**
    *   Dövizli fiyatları güncel kurlarla TL'ye çevirir.
3.  **Marka Eşleştirme:**
    *   Gelen markaları sistemdeki `Brand` tablosuyla eşleştirir. Yeni marka varsa otomatik olarak ekler.
4.  **Ürün Oluşturma (Insert):**
    *   Sistemde henüz var olmayan ürünleri (OEM koduna göre kontrol ederek) `Product` tablosuna ekler.
    *   Bu ürünler için `ProductGroupCodes` tablosuna grup kaydı atar.
5.  **Satıcı Ürünü Güncelleme (Upsert):**
    *   Ürün verilerini `SellerItems` tablosuna basar (`SellerId = 1` olarak varsayılır).
    *   Eğer kayıt varsa Stok ve Fiyat bilgilerini günceller.
    *   **Optimizasyon:** Sadece stok veya fiyatı değişen ürünleri güncelleyerek gereksiz veritabanı yükünü ve bildirimleri engeller.
6.  **Ana Ürün Fiyat Güncellemesi:**
    *   Satıcıdan gelen en güncel fiyatı, ana `Product` tablosundaki referans fiyat alanlarına da yazar.

## ⚠️ Kritik Nokta
*   Prosedür, `DISTINCT ON ("GroupCode")` mantığıyla çalışır. Yani aynı OEM grubuna (örneğin aynı parçanın farklı markaları) ait ürünleri tek bir `Product` kartı altında toplamaz, her biri için ayrı `SellerItem` oluşturur ancak ana ürün kartı (`Product`) her OEM grubu için tekil tutulmaya çalışılır.
