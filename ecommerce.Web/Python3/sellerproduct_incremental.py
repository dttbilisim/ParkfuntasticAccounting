import time
from datetime import datetime, timedelta
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
from psycopg2.extras import RealDictCursor
from decimal import Decimal
import sys
import os

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

# 2. Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# 3. Check if index exists (first run or incremental)
index_exists = es.indices.exists(index="sellerproduct_index")
is_full_index = "--full" in sys.argv or not index_exists

if is_full_index:
    log_with_time("🆕 FULL INDEX MODE - Creating index from scratch...")
    
    # Refresh materialized view
    skip_refresh = "--skip-refresh" in sys.argv
    if not skip_refresh:
        log_with_time("🔄 Refreshing materialized view...")
        start = time.time()
        pg_cursor.execute('REFRESH MATERIALIZED VIEW "mv_dotparts_joined";')
        pg_conn.commit()
        log_with_time("✅ Materialized view refreshed", start)
    
    # Delete and recreate index
    if index_exists:
        es.indices.delete(index="sellerproduct_index")
    
    mapping = {
        "settings": {
            "number_of_shards": 2,
            "number_of_replicas": 0,
            "refresh_interval": "-1"
        },
        "mappings": {
            "properties": {
                "PartNumber": {"type": "keyword"},
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
                "ProductId": {"type": "integer"},
                "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
                "ProductDescription": {"type": "text"},
                "BrandId": {"type": "integer"},
                "BrandName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
                "Status": {"type": "integer"},
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
                "DatProcessNumber": {"type": "keyword"}
            }
        }
    }
    es.indices.create(index="sellerproduct_index", body=mapping)
    log_with_time("📦 Index created")
    
    # WHERE clause for full indexing
    where_clause = 'SI."Stock" > 0'
    
else:
    log_with_time("⚡️ INCREMENTAL MODE - Updating changed records only...")
    
    # Index son 2 saat içinde değişen kayıtlar (margin of safety)
    time_window_hours = 2
    where_clause = f'''SI."Stock" > 0 
        AND (SI."ModifiedDate" >= NOW() - INTERVAL '{time_window_hours} hours' 
             OR P."ModifiedDate" >= NOW() - INTERVAL '{time_window_hours} hours')'''
    
    log_with_time(f"📅 Indexing records modified in last {time_window_hours} hours...")

# 4. Get total count
count_sql = f'''
    SELECT COUNT(DISTINCT SI."Id")
    FROM "ProductGroupCodes" pgc
    CROSS JOIN LATERAL unnest(string_to_array(pgc."GroupCode", '|')) AS g
    JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = g
    JOIN "Product" P ON P."Id" = pgc."ProductId"
    JOIN "SellerItems" SI ON SI."ProductId" = pgc."ProductId"
    JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    WHERE {where_clause}
'''
pg_cursor.execute(count_sql)
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Records to process: {total_count:,}")

if total_count == 0:
    log_with_time("✅ No records to update. Exiting.")
    pg_cursor.close()
    pg_conn.close()
    exit(0)

# 5. Data transfer
batch_size = 5000
last_id = 0
total_indexed = 0
total_updated = 0
total_deleted = 0
log_with_time("🚀 Starting data transfer...")

while True:
    sql = f'''
    SELECT DISTINCT ON (SI."Id")
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
        SI."Status" AS "SellerStatus",
        SI."ModifiedDate" AS "SellerModifiedDate",
        (
            SELECT array_agg(DISTINCT pod."DatProcessNumber")
            FROM "ProductOemDetail" pod
            WHERE pod."ProductId" = P."Id"
        ) AS "DatProcessNumber"
    FROM "ProductGroupCodes" pgc
    CROSS JOIN LATERAL unnest(string_to_array(pgc."GroupCode", '|')) AS g
    JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = g
    JOIN "Product" P ON P."Id" = pgc."ProductId"
    JOIN "SellerItems" SI ON SI."ProductId" = pgc."ProductId"
    JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    LEFT JOIN "Brand" B ON B."Id" = P."BrandId"
    WHERE {where_clause}
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
        log_with_time("⚡️ All records processed!")
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
        
        # Stock 0 ise sil, değilse upsert
        if product.get('Stock', 0) <= 0:
            actions.append({
                "_op_type": "delete",
                "_index": "sellerproduct_index",
                "_id": doc_id
            })
        else:
            actions.append({
                "_op_type": "index",  # index = upsert (insert or update)
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
            raise_on_error=False,
            stats_only=False
        )
        
        # Count updates vs inserts
        total_indexed += len([a for a in actions if a.get('_op_type') == 'index'])
        total_deleted += len([a for a in actions if a.get('_op_type') == 'delete'])
        
        progress_pct = (total_indexed / total_count * 100) if total_count > 0 else 0
        log_with_time(
            f"✅ Batch: {len(actions)} docs | Indexed: {total_indexed:,} | Deleted: {total_deleted:,} | Progress: {progress_pct:.1f}%", 
            start
        )
        
        if errors:
            print(f"⚠️  {len(errors)} documents had errors")
            
    except Exception as e:
        print(f"❌ Error: {e}")

# 6. Cleanup - Remove stale records (stock = 0 for more than 24 hours)
if not is_full_index:
    log_with_time("🧹 Cleaning up stale records (stock = 0)...")
    delete_sql = '''
        SELECT DISTINCT 
               SI."Id" AS "SellerItemId", 
               P."Id" AS "ProductId", 
               g AS "PartNumber"
        FROM "SellerItems" SI
        JOIN "Product" P ON P."Id" = SI."ProductId"
        JOIN "ProductGroupCodes" pgc ON pgc."ProductId" = P."Id"
        CROSS JOIN LATERAL unnest(string_to_array(pgc."GroupCode", '|')) AS g
        WHERE SI."Stock" = 0 
          AND SI."ModifiedDate" >= NOW() - INTERVAL '24 hours'
        LIMIT 10000
    '''
    dict_cursor = pg_conn.cursor(cursor_factory=RealDictCursor)
    dict_cursor.execute(delete_sql)
    stale_rows = dict_cursor.fetchall()
    dict_cursor.close()
    
    if stale_rows:
        delete_actions = []
        for row in stale_rows:
            doc_id = f"{row['ProductId']}_{row['SellerItemId']}_{row['PartNumber']}"
            delete_actions.append({
                "_op_type": "delete",
                "_index": "sellerproduct_index",
                "_id": doc_id
            })
        
        try:
            helpers.bulk(es, delete_actions, raise_on_error=False)
            log_with_time(f"🗑️  Deleted {len(delete_actions)} stale records")
        except:
            pass

# 7. Re-enable settings if full index
if is_full_index:
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

# Always refresh
es.indices.refresh(index="sellerproduct_index")
log_with_time("✅ Index refreshed")

# 8. Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 Completed! Indexed: {total_indexed:,} | Deleted: {total_deleted:,}")

