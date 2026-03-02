import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
import psycopg2
from psycopg2.extras import RealDictCursor
from decimal import Decimal
import sys
import json

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

# Refresh materialized views (optional)
skip_refresh = "--skip-refresh" in sys.argv
if not skip_refresh:
    log_with_time("🔄 Refreshing materialized views...")
    start = time.time()
    
    # mv_dotparts_joined refresh
    pg_cursor.execute('REFRESH MATERIALIZED VIEW "mv_dotparts_joined";')
    pg_conn.commit()
    
    # mv_sellerproduct_full refresh (tüm hesaplama burada yapılır)
    pg_cursor.execute('REFRESH MATERIALIZED VIEW "mv_sellerproduct_full";')
    pg_conn.commit()
    
    log_with_time("✅ Materialized views refreshed", start)
else:
    log_with_time("⏭️  Materialized view refresh SKIPPED")

# Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# Delete and create index with FULL DENORMALIZED mapping
if es.indices.exists(index="sellerproduct_index"):
    es.indices.delete(index="sellerproduct_index")

mapping = {
    "settings": {
        "number_of_shards": 2,
        "number_of_replicas": 0,
        "refresh_interval": "-1"
    },
    "mappings": {
        "properties": {
            # SellerItems fields
            "SellerItemId": {"type": "integer"},
            "SellerId": {"type": "integer"},
            "Stock": {"type": "integer"},
            "CostPrice": {"type": "double"},
            "SalePrice": {"type": "double"},
            "Commision": {"type": "double"},
            "Currency": {"type": "keyword"},
            "Unit": {"type": "keyword"},
            "SellerStatus": {"type": "integer"},
            "SellerModifiedDate": {"type": "date"},
            "SellerCreatedDate": {"type": "date"},
            
            # Product fields
            "ProductId": {"type": "integer"},
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "ProductBarcode": {"type": "keyword"},
            "ProductStatus": {"type": "integer"},
            
            # Brand (denormalized object)
            "Brand": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}}
                }
            },
            
            # Tax (denormalized object)
            "Tax": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text"}
                }
            },
            
            # Categories (denormalized FULL nested array)
            "Categories": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}}
                }
            },
            
            # Images (denormalized FULL nested array)
            "Images": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "FileName": {"type": "keyword"},
                    "FileGuid": {"type": "keyword"}
                }
            },
            
            # GroupCodes (denormalized FULL nested array)
            "GroupCodes": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "GroupCode": {"type": "text"}
                }
            },
            
            # DotParts fields (LEFT JOIN - NULL olabilir)
            "PartNumber": {"type": "keyword"},
            "DotPartName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ManufacturerName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "VehicleTypeName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "DotPartDescription": {"type": "text"},
            "BaseModelName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "NetPrice": {"type": "double"},
            "PriceDate": {"type": "date"},
            "VehicleTypeKey": {"type": "integer"},
            "ManufactureKey": {"type": "integer"},
            "BaseModelKey": {"type": "integer"}
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 SENIOR index created (FULL DENORMALIZED - materialized view strategy)")

# Get total count
pg_cursor.execute('SELECT COUNT(*) FROM "mv_sellerproduct_full"')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total records (from materialized view): {total_count:,}")

# Data transfer - ULTRA SIMPLE! No GROUP BY, no json_agg, direkt materialized view!
batch_size = 10000  # BÜYÜK BATCH (çünkü sadece SELECT!)
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting ULTRA FAST indexing (pre-computed materialized view)...")

while True:
    # ULTRA BASIT QUERY - Materialized view'den direkt oku!
    sql = f'''
    SELECT *
    FROM "mv_sellerproduct_full"
    WHERE "SellerItemId" > {last_id}
    ORDER BY "SellerItemId"
    LIMIT {batch_size};
    '''

    start = time.time()
    dict_cursor = pg_conn.cursor(cursor_factory=RealDictCursor)
    dict_cursor.execute(sql)
    rows = dict_cursor.fetchall()
    dict_cursor.close()
    
    if not rows:
        log_with_time("⚡️ All records indexed!")
        break

    last_id = rows[-1]['SellerItemId']

    # Transform to ES documents
    actions = []
    for row in rows:
        product = dict(row)
        
        # Convert types
        for k, v in product.items():
            if isinstance(v, datetime):
                product[k] = v.isoformat()
            elif isinstance(v, Decimal):
                product[k] = float(v)
            # JSON fields zaten dict/list olarak geliyor (RealDictCursor sayesinde)
        
        # Document ID
        part_number = product.get('PartNumber') or 'NOPART'
        doc_id = f"{product['ProductId']}_{product['SellerItemId']}_{part_number}"
        
        actions.append({
            "_index": "sellerproduct_index",
            "_id": doc_id,
            "_source": product
        })

    try:
        success, _ = helpers.bulk(
            es, 
            actions,
            chunk_size=2000,
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
    index="sellerproduct_index",
    body={
        "index": {
            "refresh_interval": "1s",
            "number_of_replicas": 1
        }
    }
)
es.indices.refresh(index="sellerproduct_index")
log_with_time("✅ Index optimized")

# Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 COMPLETED! Total: {total_indexed:,}")

print("\n" + "="*70)
print("🏆 SENIOR STRATEGY:")
print("="*70)
print("✅ Materialized view → Önceden hesaplanmış (gece refresh)")
print("✅ Python → Sadece SELECT (GROUP BY YOK!)")
print("✅ Elasticsearch → FULL denormalized (Categories, Images, ALL!)")
print("✅ C# → Tek Elasticsearch query, DB'ye GİTME!")
print("✅ Performans → ⚡️ ⚡️ ⚡️ ULTRA FAST!")
print("="*70)

