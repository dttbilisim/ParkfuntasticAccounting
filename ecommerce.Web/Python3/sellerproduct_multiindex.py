import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
import psycopg2
from psycopg2.extras import RealDictCursor
from decimal import Decimal
import sys

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

# Refresh materialized view (optional)
skip_refresh = "--skip-refresh" in sys.argv
if not skip_refresh:
    log_with_time("🔄 Refreshing materialized view...")
    start = time.time()
    pg_cursor.execute('REFRESH MATERIALIZED VIEW "mv_dotparts_joined";')
    pg_conn.commit()
    log_with_time("✅ Materialized view refreshed", start)
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

# Delete and create sellerproduct_index (SADECE ID'ler, nested YOK!)
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
            # SellerItems
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
            
            # Product
            "ProductId": {"type": "integer"},
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "ProductBarcode": {"type": "keyword"},
            "ProductStatus": {"type": "integer"},
            "BrandId": {"type": "integer"},
            "TaxId": {"type": "integer"},
            
            # DotParts (LEFT JOIN - NULL olabilir)
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
log_with_time("📦 sellerproduct_index created (multi-index strategy)")

# Get total count
pg_cursor.execute('SELECT COUNT(*) FROM "SellerItems" WHERE "Stock" > 0')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total records (Stock > 0): {total_count:,}")

# Data transfer - BASIT QUERY (nested yok!)
batch_size = 10000
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting indexing...")

while True:
    sql = f'''
    WITH FirstGroupCode AS (
        SELECT DISTINCT ON (pgc."ProductId")
            pgc."ProductId",
            unnest(string_to_array(pgc."GroupCode", '|')) AS "PartNumber"
        FROM "ProductGroupCodes" pgc
    )
    SELECT 
        SI."Id" AS "SellerItemId",
        SI."SellerId",
        SI."Stock",
        SI."CostPrice",
        SI."SalePrice",
        SI."Commision",
        SI."Currency",
        SI."Unit",
        SI."Status" AS "SellerStatus",
        SI."ModifiedDate" AS "SellerModifiedDate",
        P."Id" AS "ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."Barcode" AS "ProductBarcode",
        P."Status" AS "ProductStatus",
        P."BrandId",
        P."TaxId",
        FGC."PartNumber",
        DP."Name" AS "DotPartName",
        DP."ManufacturerName",
        DP."VehicleTypeName",
        DP."Description" AS "DotPartDescription",
        DP."BaseModelName",
        DP."NetPrice",
        DP."PriceDate",
        DD."VehicleTypeKey",
        DD."ManufactureKey",
        DD."BaseModelKey"
    FROM "SellerItems" SI
    JOIN "Product" P ON P."Id" = SI."ProductId"
    LEFT JOIN FirstGroupCode FGC ON FGC."ProductId" = P."Id"
    LEFT JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = FGC."PartNumber"
    LEFT JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    WHERE SI."Stock" > 0
      AND SI."Id" > {last_id}
    ORDER BY SI."Id"
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
    index="sellerproduct_index",
    body={
        "index": {
            "refresh_interval": "1s",
            "number_of_replicas": 1
        }
    }
)
es.indices.refresh(index="sellerproduct_index")

# Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 Completed! Total: {total_indexed:,}")

print("\n" + "="*70)
print("🏆 MULTI-INDEX STRATEGY:")
print("="*70)
print("✅ sellerproduct_index → Ürün bilgileri (BrandId, TaxId, ProductId)")
print("✅ brand_index → Brand detayları (zaten var)")
print("✅ category_index → Category detayları (zaten var)")
print("✅ image_index → Image detayları (yeni oluşturulacak)")
print("✅ C# tarafında Elasticsearch multi-index query (application-side join)")
print("="*70)

