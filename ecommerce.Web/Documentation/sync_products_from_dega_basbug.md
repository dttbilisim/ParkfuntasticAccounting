# Dega & Basbug Sync Procedures (`sync_dega.sql`, `sync_basbug.sql`)

## 📌 Genel Amaç
Dega ve Başbuğ Otomotiv tedarikçilerinden gelen stok ve fiyat listelerini sisteme entegre eder. Bu prosedürler de standart senkronizasyon şablonunu takip eder.

## ⚙️ Ortak İşleyiş
1.  **Kaynak Tablolar:**
    *   `ProductDegas`: Dega verisi.
    *   `ProductBasbugs`: Başbuğ verisi.
2.  **Komisyon Hesaplama:**
    *   Her tedarikçi (Seller) için tanımlı bir komisyon oranı vardır (`Sellers` tablosundan alınır).
    *   Maliyet fiyatının üzerine bu komisyon eklenerek `SalePrice` hesaplanır.
3.  **Stok Kontrolü:**
    *   Bu tedarikçilerden gelen veriler genellikle sadece stok ve fiyat güncellemelerini içerir.
    *   Eğer ürün sistemde yoksa (OEM kodu eşleşmiyorsa) yeni kart açılır.
4.  **Optimizasyon:**
    *   Tüm prosedürlerde "Değişiklik Yoksa Güncelleme Yapma" (`IS DISTINCT FROM`) mantığı kullanılarak sistem performansı korunur.

## 📁 Dosyalar
*   `sync_products_from_dega.sql`: Dega entegrasyonu.
*   `sync_products_from_basbugs.sql`: Başbuğ entegrasyonu.
