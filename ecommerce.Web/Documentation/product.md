# Product Sync Orchestrator (`product.py`)

Bu script, e-ticaret sisteminin ana ürün senkronizasyon ve indeksleme motorudur. Veritabanındaki ürünleri, tedarikçi verilerini ve resimleri işleyerek Elasticsearch'e aktarır.

## 🚀 Temel Görevleri
1.  **Yardımcı Scriptleri Çalıştırma:**
    *   Marka (`brand.py`), Kategori (`category.py`) ve Üretici (`marka.py`) verilerini güncelleyen alt scriptleri tetikler.
2.  **Veri Tabanı Senkronizasyonu:**
    *   PostgreSQL üzerindeki Stored Procedure'leri (`sync_products_from_*`) çağırarak tedarikçi tablolarından (`ProductOtoIsmails`, `ProductRemars` vb.) gelen verileri ana `Product` ve `SellerItems` tablolarına aktarır.
3.  **Elasticsearch İndeks Yönetimi:**
    *   **Kesintisiz (Zero Downtime) Mantığı:** `image_index` ve `sellerproduct_index` indekslerini kontrol eder. Eğer varsa silmez, sadece günceller (Upsert). Yoksa sıfırdan oluşturur.
    *   **NGram Analizi:** Ürün isimleri ve OEM kodları için parçalı arama (NGram) yeteneği kazandırır.
4.  **Resimlerin İndekslenmesi:**
    *   `ProductImages` tablosundaki milyonlarca resmi parça parça okuyup `image_index` içine basar.
5.  **Ürünlerin İndekslenmesi:**
    *   **Veri Zenginleştirme:** Her ürün için; Stok, Maliyet, Satış Fiyatı, Marka, Araç Uyumluluk Listesi (SubModelsJson) ve OEM kodlarını birleştirir.
    *   **Performans:** Geçici tablolar (`tmp_dotparts`, `tmp_product_matched_codes`) kullanarak milyonlarca satırlık veriyi hızlıca işler.
6.  **Sistem Bakımı:**
    *   İşlem sonunda veritabanında `ANALYZE` ve `CHECKPOINT` komutlarını çalıştırarak performansı optimize eder.

## 🔗 Bağımlılıklar
*   **Python Kütüphaneleri:** `elasticsearch`, `psycopg2`
*   **Veritabanı:** PostgreSQL (`Product`, `SellerItems`, `ProductImages`, `DotParts` tabloları)
*   **Arama Motoru:** Elasticsearch (Port 9200)

## ⚠️ Önemli Notlar
*   **Upsert Modu:** Script, mevcut indeksleri silmeden güncelleme yapacak şekilde tasarlanmıştır. Bu sayede canlı sistemde arama kesintisi yaşanmaz.
*   **Bellek Yönetimi:** Büyük verileri (Resimler, Ürünler) 10.000'lik paketler halinde (Batch Processing) işler.
