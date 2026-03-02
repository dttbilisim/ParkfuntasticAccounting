# SellerProduct Elasticsearch Indexing

## 📋 Dosyalar

| Dosya | Amaç | Kullanım |
|-------|------|----------|
| `sellerproduct_incremental.py` | **ÖNERİLEN** - Akıllı indexleme (full/incremental) | Cron job için |
| `sellerproduct_fast.py` | Hızlı tam indexleme | Tek seferlik kullanım |
| `sellerproduct_optimized.py` | Optimized tam indexleme (nested yapılar) | Legacy |
| `sellerproduct.py` | Basit tam indexleme | Legacy |
| `setup_cron.sh` | Cron job kurulum scripti | Bir kez çalıştır |

---

## 🚀 Hızlı Başlangıç

### 1. İlk Kurulum (Tek Seferlik)

```bash
# İlk kez FULL INDEX yapın (539K kayıt - ~15-20 dakika)
python3 sellerproduct_incremental.py --full

# Materialized view refresh'i atlamak isterseniz:
python3 sellerproduct_incremental.py --full --skip-refresh
```

### 2. Cron Job Kurulumu (Otomatik Güncelleme)

```bash
# Cron job'u kur
chmod +x setup_cron.sh
./setup_cron.sh

# Kontrol et
crontab -l

# Log'ları izle
tail -f /var/log/sellerproduct/incremental.log
```

---

## ⚡️ Kullanım Senaryoları

### Senaryo 1: İlk Kurulum (Production'a İlk Deploy)

```bash
python3 sellerproduct_incremental.py --full
```

**Ne yapar?**
- Elasticsearch index'ini sıfırdan oluşturur
- Materialized view'i refresh eder
- Tüm 539K+ kaydı index'ler
- Süre: ~15-20 dakika

---

### Senaryo 2: Saatlik Otomatik Güncelleme (Cron Job)

```bash
# Crontab entry:
0 * * * * cd /path/to/scripts && python3 sellerproduct_incremental.py >> /var/log/sellerproduct/incremental.log 2>&1
```

**Ne yapar?**
- Sadece son 2 saat içinde değişen kayıtları index'ler
- Stock = 0 olan kayıtları siler
- Süre: ~10-30 saniye (sadece değişenler için)

---

### Senaryo 3: Manuel Güncelleme (Debug/Test)

```bash
# Incremental mode
python3 sellerproduct_incremental.py

# Full index (index'i sıfırdan oluştur)
python3 sellerproduct_incremental.py --full

# Full index + skip refresh
python3 sellerproduct_incremental.py --full --skip-refresh
```

---

### Senaryo 4: Acil Durum (Index Corrupt Olmuşsa)

```bash
# Index'i sil ve yeniden oluştur
curl -X DELETE "http://localhost:9200/sellerproduct_index"

# Full index çalıştır
python3 sellerproduct_incremental.py --full
```

---

## 📊 Performans Metrikleri

| Mod | Kayıt Sayısı | Süre | Kullanım |
|-----|--------------|------|----------|
| **Full Index** | 539,412 | ~15-20 dakika | İlk kurulum |
| **Incremental** | ~100-1000 | ~10-30 saniye | Saatlik cron |
| **Incremental (değişiklik yok)** | 0 | ~2-5 saniye | Saatlik cron |

---

## 🔍 Log İzleme

```bash
# Cron log'larını izle
tail -f /var/log/sellerproduct/incremental.log

# Son çalıştırmaları gör
tail -100 /var/log/sellerproduct/incremental.log

# Hata ara
grep "❌" /var/log/sellerproduct/incremental.log
```

---

## 🛠️ Troubleshooting

### Sorun: Script çok yavaş çalışıyor

**Çözüm 1:** PostgreSQL index'leri ekle

```sql
CREATE INDEX IF NOT EXISTS idx_selleritems_modifieddate 
    ON "SellerItems"("ModifiedDate") WHERE "Stock" > 0;

CREATE INDEX IF NOT EXISTS idx_product_modifieddate 
    ON "Product"("ModifiedDate");

CREATE INDEX IF NOT EXISTS idx_selleritems_productid_stock 
    ON "SellerItems"("ProductId", "Stock") WHERE "Stock" > 0;

CREATE INDEX IF NOT EXISTS idx_mv_dotparts_partnumber 
    ON "mv_dotparts_joined"("PartNumber");

CREATE INDEX IF NOT EXISTS idx_datdatas_composite 
    ON "DatDatas"("VehicleTypeKey", "ManufactureKey", "BaseModelKey");
```

**Çözüm 2:** Incremental time window'u azalt

Script'te bu satırı değiştir:
```python
time_window_hours = 2  # 1'e düşür
```

---

### Sorun: Materialized view refresh çok uzun sürüyor

**Çözüm:** `--skip-refresh` parametresi kullan

```bash
python3 sellerproduct_incremental.py --skip-refresh
```

**Not:** Materialized view'i ayrı bir cron job ile güncelle:

```bash
# Her gece saat 2'de
0 2 * * * psql -d MarketPlace -c 'REFRESH MATERIALIZED VIEW "mv_dotparts_joined";' >> /var/log/mv_refresh.log 2>&1
```

---

### Sorun: Elasticsearch connection timeout

**Çözüm:** Request timeout'u artır

Script'te bu değeri değiştir:
```python
request_timeout=60  # 120'ye çıkar
```

---

## 📈 Monitoring

### Elasticsearch Stats

```bash
# Index boyutu ve doküman sayısı
curl -X GET "http://localhost:9200/sellerproduct_index/_stats?pretty"

# Index health
curl -X GET "http://localhost:9200/_cat/indices/sellerproduct_index?v"

# Son indexlenen kayıtlar
curl -X GET "http://localhost:9200/sellerproduct_index/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{"query":{"match_all":{}},"sort":[{"SellerModifiedDate":{"order":"desc"}}],"size":10}'
```

---

## 🎯 Best Practices

1. **İlk kurulum:** `--full` ile başla
2. **Cron job:** Saatlik incremental mode
3. **Materialized view:** Gece refresh et (ayrı cron)
4. **Log rotation:** logrotate kur
5. **Monitoring:** Elasticsearch health check ekle
6. **Backup:** Index snapshot'larını düzenli al

---

## 📞 Önerilen Cron Schedule

```bash
# Her saat başı incremental update
0 * * * * cd /root/transfer && python3 sellerproduct_incremental.py >> /var/log/sellerproduct/incremental.log 2>&1

# Her gece saat 2'de materialized view refresh
0 2 * * * psql -h localhost -p 5454 -U myinsurer -d MarketPlace -c 'REFRESH MATERIALIZED VIEW "mv_dotparts_joined";' >> /var/log/mv_refresh.log 2>&1

# Her Pazar saat 3'te full reindex (temizlik)
0 3 * * 0 cd /root/transfer && python3 sellerproduct_incremental.py --full >> /var/log/sellerproduct/weekly_full.log 2>&1
```

---

## 🔒 Güvenlik

**Önemli:** Production'da credentials'ları environment variable'lara taşı!

```python
import os
pg_conn = psycopg2.connect(
    host=os.getenv("PG_HOST", "localhost"),
    port=os.getenv("PG_PORT", "5454"),
    database=os.getenv("PG_DB", "MarketPlace"),
    user=os.getenv("PG_USER", "myinsurer"),
    password=os.getenv("PG_PASSWORD")
)
```

---

## 📝 Version History

- **v3.0 (incremental):** Akıllı incremental update - Production ready
- **v2.0 (fast):** Hızlı full indexing, nested yapılar kaldırıldı
- **v1.0 (optimized):** Temp table + cursor-based pagination
- **v0.5 (basic):** İlk versiyon - OFFSET kullanımı

