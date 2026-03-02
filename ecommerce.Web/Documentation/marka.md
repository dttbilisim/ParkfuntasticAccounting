# Manufacturer Sync Script (`marka.py` / `manufacturer.py`)

## 📌 Amaç
Araç üreticisi (Manufacturer) verilerini ve logolarını `DotManufacturers` tablosunda Elasticsearch'e `manufacturer_index` adıyla aktarmaktır.

## 🔄 İşleyiş Adımları
1.  **İndeks Hazırlığı:**
    *   Mevcut `manufacturer_index` varsa siler ve yeniden oluşturur (Full Reindex). Bu veri seti küçük olduğu için silip yeniden oluşturmak daha temizdir.
2.  **Veri Çekme:**
    *   `DotManufacturers` tablosundan aktif üreticileri çeker.
    *   Her üretici için `DotBaseModels` tablosundan ilişkili Araç Modellerini (Örn: Audi -> A3, A4) toplar.
    *   **Logo Görselleri:** `DotVehicleImages` tablosundan üreticiye ait araç görsellerini (Önden veya Yandan görünüm) bulur ve ekler.
3.  **İndeksleme:**
    *   Çekilen hiyerarşik veriyi (Üretici -> Modeller -> Görseller) JSON formatında Elasticsearch'e kaydeder.

## 🔑 Kullanım Alanı
*   Web ve Admin panelindeki **"Araç Seç" (Vehicle Match Modal)** ekranlarında görünen Marka/Model listesi bu indeks üzerinden beslenir.
