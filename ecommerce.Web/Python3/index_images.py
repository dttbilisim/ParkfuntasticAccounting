import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
import psycopg2
from psycopg2.extras import RealDictCursor
from decimal import Decimal

def log_with_time(message, start=None):
    if start:
        elapsed = time.time() - start
        print(f"{message} ⏱️ {elapsed:.2f}s")
    else:
        print(message)

# PostgreSQL connection
pg_conn = psycopg2.connect(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)
pg_cursor = pg_conn.cursor()

# Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# Delete and create image_index
if es.indices.exists(index="image_index"):
    es.indices.delete(index="image_index")

mapping = {
    "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "refresh_interval": "-1"
    },
    "mappings": {
        "properties": {
            "Id": {"type": "integer"},
            "ProductId": {"type": "integer"},  # JOIN key
            "FileName": {"type": "keyword"},
            "FileGuid": {"type": "keyword"},
            "CreatedDate": {"type": "date"},
            "ModifiedDate": {"type": "date"}
        }
    }
}
es.indices.create(index="image_index", body=mapping)
log_with_time("📦 image_index created")

# Get total count
pg_cursor.execute('SELECT COUNT(*) FROM "ProductImages"')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total images: {total_count:,}")

# Data transfer
batch_size = 10000
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting image indexing...")

while True:
    sql = f'''
    SELECT 
        "Id",
        "ProductId",
        "FileName",
        "FileGuid",
        "CreatedDate",
        "ModifiedDate"
    FROM "ProductImages"
    WHERE "Id" > {last_id}
    ORDER BY "Id"
    LIMIT {batch_size};
    '''

    start = time.time()
    dict_cursor = pg_conn.cursor(cursor_factory=RealDictCursor)
    dict_cursor.execute(sql)
    rows = dict_cursor.fetchall()
    dict_cursor.close()
    
    if not rows:
        log_with_time("⚡️ All images indexed!")
        break

    last_id = rows[-1]['Id']

    # Transform to ES documents
    actions = []
    for row in rows:
        image = dict(row)
        
        # Convert types
        for k, v in image.items():
            if isinstance(v, datetime):
                image[k] = v.isoformat()
        
        actions.append({
            "_index": "image_index",
            "_id": image['Id'],
            "_source": image
        })

    try:
        success, _ = helpers.bulk(
            es, 
            actions,
            chunk_size=5000,
            request_timeout=60,
            raise_on_error=False
        )
        total_indexed += success
        
        progress_pct = (total_indexed / total_count * 100) if total_count > 0 else 0
        log_with_time(
            f"✅ Batch: {success} | Total: {total_indexed:,}/{total_count:,} ({progress_pct:.1f}%)", 
            start
        )
        
    except Exception as e:
        print(f"❌ Error: {e}")

# Re-enable settings
log_with_time("🔄 Enabling refresh and replicas...")
es.indices.put_settings(
    index="image_index",
    body={
        "index": {
            "refresh_interval": "1s",
            "number_of_replicas": 1
        }
    }
)
es.indices.refresh(index="image_index")

# Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 Completed! Total images: {total_indexed:,}")

