import psycopg2
from elasticsearch import Elasticsearch
from datetime import datetime
import time

def log_with_time(message, start=None):
    if start:
        elapsed = time.time() - start
        print(f"{message} ⏱️ {elapsed:.2f}s")
    else:
        print(message)

start_total = time.time()

# PostgreSQL bağlantısı
pg_conn = psycopg2.connect(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)
pg_cursor = pg_conn.cursor()

# Elasticsearch bağlantısı
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    request_timeout=30,
    retry_on_timeout=True,
    max_retries=3
)

index_name = "brand_index"

# 1. Eğer index varsa sil
start = time.time()
if es.indices.exists(index=index_name):
    es.indices.delete(index=index_name)
    log_with_time(f"🗑️ {index_name} indeksi silindi", start)
else:
    log_with_time(f"ℹ️ {index_name} zaten yok", start)

# 2. Yeni boş index oluştur
start = time.time()
es.indices.create(index=index_name)
log_with_time(f"📦 {index_name} indeksi oluşturuldu", start)

# 3. Brand tablosundan tüm aktif markaları çek
start = time.time()
# SELECT * kullandığımız için yeni eklenen ImageUrl alanı otomatik olarak dahil edilecektir.
pg_cursor.execute('SELECT * FROM "Brand" WHERE "Status" = 1')
columns = [desc[0] for desc in pg_cursor.description]
rows = pg_cursor.fetchall()
log_with_time(f"📥 {len(rows)} aktif marka aktarılacak. Elasticsearch'e gönderiliyor...", start)

# 5. Her kaydı Elasticsearch’e indexle
start = time.time()
for row in rows:
    doc = dict(zip(columns, row))

    # datetime'ları ISO string'e çevir
    for key, value in doc.items():
        if isinstance(value, datetime):
            doc[key] = value.isoformat()

    # Elasticsearch'e yaz
    es.index(index=index_name, id=doc["Id"], document=doc)

log_with_time("✅ Brand verileri (ImageUrl dahil) başarıyla Elasticsearch'e aktarıldı", start)

# Bağlantıları kapat
pg_cursor.close()
pg_conn.close()
log_with_time("🔚 Bağlantılar kapatıldı.", time.time())
log_with_time("🎯 Tüm işlem tamamlandı.", start_total)
