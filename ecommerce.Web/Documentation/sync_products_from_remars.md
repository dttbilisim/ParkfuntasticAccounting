# Remar Product Sync Procedure (`sync_products_from_remars.sql`)

## 📌 Amaç
Remar Otomotiv tedarikçisinden gelen verileri (`ProductRemars`) ana sisteme entegre etmektir. Otoİsmail prosedürü ile benzer mantıkta çalışır ancak tedarikçiye özel veri temizleme kuralları içerir.

## ⚙️ İşlem Adımları
1.  **Döviz ve Veri Hazırlığı:**
    *   Remar'dan gelen verilerdeki fiyatları ve stok durumlarını normalize eder.
2.  **Marka Yönetimi:**
    *   Remar verisindeki markaları (`CleanBrand`) sistemdeki `Brand` tablosu ile eşleştirir, yoksa oluşturur.
3.  **Ürün Eşleştirme:**
    *   Ürünleri `Kod` (Parça Kodu) ve `Ref` (OEM) alanlarına göre gruplar.
4.  **Veri Aktarımı (Upsert):**
    *   **Yeni Ürünler:** `Product` tablosuna eklenir.
    *   **Mevcut Ürünler:** Stok ve Fiyat bilgileri güncellenir.
    *   **SellerId:** Bu tedarikçi için belirlenen SellerId (Genellikle 2 veya farklı bir ID) ile `SellerItems` tablosuna kayıt atılır.

## 💡 Farklılıklar
*   Remar verisinde bazen parça numaraları farklı formatlarda gelebilir, bu yüzden `TRIM` ve `REPLACE` işlemleri daha yoğundur.
