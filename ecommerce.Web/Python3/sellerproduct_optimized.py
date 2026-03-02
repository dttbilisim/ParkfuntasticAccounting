import subprocess
import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
from psycopg2.extras import RealDictCursor
from decimal import Decimal
import json
from concurrent.futures import ThreadPoolExecutor
import threading
import sys

def log_with_time(message, start=None):
    if start:
        elapsed = time.time() - start
        print(f"{message} ⏱️ {elapsed:.2f}s")
    else:
        print(message)

# 1. PostgreSQL connection pool configuration
def get_pg_connection():
    return psycopg2.connect(
        host="localhost",
        port=5454,
        database="MarketPlace",
        user="myinsurer",
        password="Posmdh0738"
    )

pg_conn = get_pg_connection()
pg_cursor = pg_conn.cursor()

# 2. Refresh materialized view (optional - --skip-refresh parametresi ile atlanabilir)
skip_refresh = "--skip-refresh" in sys.argv
if skip_refresh:
    log_with_time("⏭️  Materialized view refresh SKIPPED (--skip-refresh)")
else:
    log_with_time("🔄 Refreshing materialized view...")
    start = time.time()
    pg_cursor.execute('REFRESH MATERIALIZED VIEW "mv_dotparts_joined";')
    pg_conn.commit()
    log_with_time("✅ Materialized view refreshed", start)

# 3. Create flat_codes as temporary table (MAJOR PERFORMANCE BOOST)
log_with_time("🚀 Creating flat_codes temporary table...")
start = time.time()
pg_cursor.execute('''
    DROP TABLE IF EXISTS temp_flat_codes;
    
    CREATE TEMP TABLE temp_flat_codes AS
    SELECT 
        "ProductId",
        unnest(string_to_array("GroupCode", '|')) AS g
    FROM "ProductGroupCodes";
    
    CREATE INDEX idx_temp_flat_codes_g ON temp_flat_codes(g);
    CREATE INDEX idx_temp_flat_codes_productid ON temp_flat_codes("ProductId");
    
    ANALYZE temp_flat_codes;
''')
pg_conn.commit()
log_with_time("✅ Temporary table created with indexes", start)

# 4. Check if necessary indexes exist (recommendations)
log_with_time("📋 Checking indexes...")
index_recommendations = '''
-- Önerilen index'ler (yoksa oluştur):
-- CREATE INDEX IF NOT EXISTS idx_selleritems_productid_stock ON "SellerItems"("ProductId", "Stock") WHERE "Stock" > 0;
-- CREATE INDEX IF NOT EXISTS idx_mv_dotparts_partnumber ON "mv_dotparts_joined"("PartNumber");
-- CREATE INDEX IF NOT EXISTS idx_datdatas_composite ON "DatDatas"("VehicleTypeKey", "ManufactureKey", "BaseModelKey");
-- CREATE INDEX IF NOT EXISTS idx_productcategories_productid ON "ProductCategories"("ProductId");
-- CREATE INDEX IF NOT EXISTS idx_productimages_productid ON "ProductImages"("ProductId");
'''
print(index_recommendations)

# 5. Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# 6. Delete existing index and create new one with optimized settings
if es.indices.exists(index="sellerproduct_index"):
    es.indices.delete(index="sellerproduct_index")

mapping = {
    "settings": {
        "number_of_shards": 2,
        "number_of_replicas": 0,  # Indexing sırasında replica 0 olmalı
        "refresh_interval": "-1",  # Indexing sırasında refresh'i kapat
        "index": {
            "max_result_window": 100000
        }
    },
    "mappings": {
        "properties": {
            "PartNumber": {"type": "keyword"},  # text yerine keyword (exact match için)
            "ProductId": {"type": "integer"},
            "DotPartName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ManufacturerName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "VehicleTypeName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "Description": {"type": "text"},
            "BaseModelName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "NetPrice": {"type": "double"},
            "PriceDate": {"type": "date"},
            "VehicleTypeKey": {"type": "integer"},
            "ManufactureKey": {"type": "integer"},
            "BaseModelKey": {"type": "integer"},
            # Product fields
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "BrandId": {"type": "integer"},
            "TaxId": {"type": "integer"},
            "Status": {"type": "integer"},
            "CreatedDate": {"type": "date"},
            "ModifiedDate": {"type": "date"},
            "CreatedId": {"type": "integer"},
            "ModifiedId": {"type": "integer"},
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
            "SellerCreatedDate": {"type": "date"},
            "SellerModifiedDate": {"type": "date"},
            "SellerCreatedId": {"type": "integer"},
            "SellerModifiedId": {"type": "integer"},
            # Nested structures
            "Categories": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}}
                }
            },
            "Images": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "FileName": {"type": "keyword"},
                    "FileGuid": {"type": "keyword"}
                }
            },
            "Brand": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}}
                }
            }
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 Optimized SellerProduct index created")

# 7. Get total count for progress tracking
pg_cursor.execute('''
    SELECT COUNT(*) 
    FROM temp_flat_codes fc
    JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = fc.g
    JOIN "Product" P ON P."Id" = fc."ProductId"
    JOIN "SellerItems" SI ON SI."ProductId" = fc."ProductId"
    WHERE SI."Stock" > 0
''')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total records to index: {total_count:,}")

# 8. Data transfer with cursor-based pagination (NO OFFSET!)
batch_size = 1000  # Küçük batch size (ağır sorgu için)
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting Elasticsearch data transfer for SellerProduct...")

# Semaphore for throttling
semaphore = threading.Semaphore(3)  # Max 3 concurrent ES bulk operations (ağır sorgu için)

def transform_and_prepare_doc(row):
    """Transform database row to Elasticsearch document"""
    product = dict(row)
    
    # Convert datetime and decimal types
    for k, v in product.items():
        if isinstance(v, datetime):
            product[k] = v.isoformat()
        elif isinstance(v, Decimal):
            product[k] = float(v)
        elif isinstance(v, list):
            for i in range(len(v)):
                if isinstance(v[i], dict):
                    for kk, vv in v[i].items():
                        if isinstance(vv, datetime):
                            v[i][kk] = vv.isoformat()
                        elif isinstance(vv, Decimal):
                            v[i][kk] = float(vv)
    
    # Create unique document ID
    doc_id = f"{product.get('ProductId')}_{product.get('SellerItemId')}_{product.get('PartNumber')}"
    
    return {
        "_index": "sellerproduct_index",
        "_id": doc_id,
        "_source": product
    }

while True:
    # Cursor-based pagination using composite key (SellerItemId)
    sql = f'''
    SELECT 
        DP."PartNumber",
        fc."ProductId",
        DP."Name" AS "DotPartName",
        DP."ManufacturerName",
        DP."VehicleTypeName",
        DP."Description",
        DP."BaseModelName",
        DP."NetPrice",
        DP."PriceDate",
        DD."VehicleTypeKey",
        DD."ManufactureKey",
        DD."BaseModelKey",
        P."Id" AS "ProductIdFull",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."BrandId",
        P."TaxId",
        P."Status",
        P."CreatedDate",
        P."ModifiedDate",
        P."CreatedId",
        P."ModifiedId",
        SI."Id" AS "SellerItemId",
        SI."SellerId",
        SI."Stock",
        SI."CostPrice",
        SI."SalePrice",
        SI."Commision",
        SI."Currency",
        SI."Unit",
        SI."Status" AS "SellerStatus",
        SI."CreatedDate" AS "SellerCreatedDate",
        SI."ModifiedDate" AS "SellerModifiedDate",
        SI."CreatedId" AS "SellerCreatedId",
        SI."ModifiedId" AS "SellerModifiedId",
        json_agg(DISTINCT jsonb_build_object('Id', c."Id", 'Name', c."Name")) 
            FILTER (WHERE c."Id" IS NOT NULL) AS "Categories",
        json_agg(DISTINCT jsonb_build_object('Id', i."Id", 'FileName', i."FileName", 'FileGuid', i."FileGuid")) 
            FILTER (WHERE i."Id" IS NOT NULL) AS "Images",
        jsonb_build_object('Id', b."Id", 'Name', b."Name") AS "Brand"
    FROM temp_flat_codes fc
    JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = fc.g
    JOIN "Product" P ON P."Id" = fc."ProductId"
    JOIN "SellerItems" SI ON SI."ProductId" = fc."ProductId"
    JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    LEFT JOIN "ProductCategories" pc ON pc."ProductId" = P."Id"
    LEFT JOIN "Category" c ON c."Id" = pc."CategoryId"
    LEFT JOIN "ProductImages" i ON i."ProductId" = P."Id"
    LEFT JOIN "Brand" b ON b."Id" = P."BrandId"
    WHERE SI."Stock" > 0
      AND SI."Id" > {last_id}
    GROUP BY 
        DP."PartNumber", fc."ProductId", DP."Name", DP."ManufacturerName",
        DP."VehicleTypeName", DP."Description", DP."BaseModelName",
        DP."NetPrice", DP."PriceDate", DD."VehicleTypeKey",
        DD."ManufactureKey", DD."BaseModelKey",
        P."Id", P."Name", P."Description", P."BrandId", P."TaxId",
        P."Status", P."CreatedDate", P."ModifiedDate", P."CreatedId", P."ModifiedId",
        SI."Id", SI."SellerId", SI."Stock", SI."CostPrice", SI."SalePrice",
        SI."Commision", SI."Currency", SI."Unit", SI."Status",
        SI."CreatedDate", SI."ModifiedDate", SI."CreatedId", SI."ModifiedId",
        b."Id", b."Name"
    ORDER BY SI."Id"
    LIMIT {batch_size};
    '''

    start = time.time()
    dict_cursor = pg_conn.cursor(cursor_factory=RealDictCursor)
    dict_cursor.execute(sql)
    rows = dict_cursor.fetchall()
    dict_cursor.close()
    
    if not rows:
        log_with_time("⚡️ All seller products have been indexed to Elasticsearch.")
        break

    # Update last_id for next iteration
    last_id = rows[-1]['SellerItemId']

    # Transform rows to ES documents
    actions = [transform_and_prepare_doc(row) for row in rows]

    try:
        with semaphore:
            success, errors = helpers.bulk(
                es, 
                actions,
                chunk_size=500,  # ES bulk chunk size (küçültüldü)
                request_timeout=60,
                raise_on_error=False
            )
            total_indexed += success
            
            progress_pct = (total_indexed / total_count * 100) if total_count > 0 else 0
            log_with_time(
                f"✅ Batch indexed: {success} products, Total: {total_indexed:,}/{total_count:,} ({progress_pct:.1f}%)", 
                start
            )
            
            if errors:
                print(f"⚠️  {len(errors)} documents had errors")
                for err in errors[:3]:
                    print(err)
                    
    except BulkIndexError as e:
        print(f"❌ Error: {len(e.errors)} documents failed.")
        for err in e.errors[:3]:
            print(err)
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

# 9. Re-enable refresh and add replicas after indexing
log_with_time("🔄 Enabling refresh and adding replicas...")
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

# 10. Maintenance
log_with_time("🧩 Running ANALYZE on related tables...")
maintenance_sqls = [
    'ANALYZE "ProductGroupCodes";',
    'ANALYZE "Product";',
    'ANALYZE "SellerItems";',
    'ANALYZE "DatDatas";',
    'ANALYZE "ProductCategories";',
    'ANALYZE "Category";',
    'ANALYZE "ProductImages";',
    'ANALYZE "Brand";',
    'ANALYZE "mv_dotparts_joined";'
]
for sql in maintenance_sqls:
    start = time.time()
    print(f"🔄 {sql.strip()}")
    pg_cursor.execute(sql)
    pg_conn.commit()
    log_with_time(f"✅ {sql.strip()} completed", start)

# 11. Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 All operations completed. Total indexed seller products: {total_indexed:,}")

