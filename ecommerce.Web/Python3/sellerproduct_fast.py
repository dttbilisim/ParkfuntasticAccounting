import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
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

# 1. PostgreSQL connection
pg_conn = psycopg2.connect(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)
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

# 3. Create flat_codes as temporary table
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

# 4. Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# 5. Delete existing index and create new one (SADECE TEMEL ALANLAR)
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
            # DotParts fields
            "PartNumber": {"type": "keyword"},
            "DotPartName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ManufacturerName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "VehicleTypeName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "Description": {"type": "text"},
            "BaseModelName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "NetPrice": {"type": "double"},
            "PriceDate": {"type": "date"},
            # DatDatas fields
            "VehicleTypeKey": {"type": "integer"},
            "ManufactureKey": {"type": "integer"},
            "BaseModelKey": {"type": "integer"},
            # Product fields (basit)
            "ProductId": {"type": "integer"},
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "BrandId": {"type": "integer"},
            "BrandName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "Status": {"type": "integer"},
            # SellerItems fields
            "SellerItemId": {"type": "integer"},
            "SellerId": {"type": "integer"},
            "Stock": {"type": "integer"},
            "CostPrice": {"type": "double"},
            "SalePrice": {"type": "double"},
            "Commision": {"type": "double"},
            "Currency": {"type": "keyword"},
            "Unit": {"type": "keyword"},
            "SellerStatus": {"type": "integer"}
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 Fast SellerProduct index created")

# 6. Get total count
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

# 7. Data transfer (HIZLI SORGU - NO GROUP BY, NO JSON_AGG)
batch_size = 5000
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting Elasticsearch data transfer...")

while True:
    # BASİT SORGU - Sadece JOIN'ler, GROUP BY yok!
    sql = f'''
    SELECT 
        DP."PartNumber",
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
        P."Id" AS "ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."BrandId",
        P."Status",
        B."Name" AS "BrandName",
        SI."Id" AS "SellerItemId",
        SI."SellerId",
        SI."Stock",
        SI."CostPrice",
        SI."SalePrice",
        SI."Commision",
        SI."Currency",
        SI."Unit",
        SI."Status" AS "SellerStatus"
    FROM temp_flat_codes fc
    JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = fc.g
    JOIN "Product" P ON P."Id" = fc."ProductId"
    JOIN "SellerItems" SI ON SI."ProductId" = fc."ProductId"
    JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    LEFT JOIN "Brand" B ON B."Id" = P."BrandId"
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
        log_with_time("⚡️ All seller products indexed!")
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
        
        doc_id = f"{product['ProductId']}_{product['SellerItemId']}_{product['PartNumber']}"
        
        actions.append({
            "_index": "sellerproduct_index",
            "_id": doc_id,
            "_source": product
        })

    try:
        success, errors = helpers.bulk(
            es, 
            actions,
            chunk_size=1000,
            request_timeout=60,
            raise_on_error=False
        )
        total_indexed += success
        
        progress_pct = (total_indexed / total_count * 100) if total_count > 0 else 0
        log_with_time(
            f"✅ Batch: {success} products | Total: {total_indexed:,}/{total_count:,} ({progress_pct:.1f}%)", 
            start
        )
        
        if errors:
            print(f"⚠️  {len(errors)} documents had errors")
            
    except Exception as e:
        print(f"❌ Error: {e}")

# 8. Re-enable refresh and replicas
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

# 9. Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 Completed! Total indexed: {total_indexed:,}")

