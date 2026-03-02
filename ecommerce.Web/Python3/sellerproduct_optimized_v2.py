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

# Delete and create index with OPTIMIZED mapping
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
            # DotParts fields (LEFT JOIN - NULL olabilir)
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
            
            # Product fields
            "ProductId": {"type": "integer"},
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "ProductBarcode": {"type": "keyword"},
            "Status": {"type": "integer"},
            "ProductCreatedDate": {"type": "date"},
            "ProductModifiedDate": {"type": "date"},
            
            # Brand (denormalized - sık kullanılıyor)
            "BrandId": {"type": "integer"},
            "BrandName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            
            # Tax (denormalized - sık kullanılıyor)
            "TaxId": {"type": "integer"},
            "TaxName": {"type": "text"},
            "TaxRate": {"type": "double"},
            
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
            
            # ID Arrays (cache için - nested DEĞİL!)
            "CategoryIds": {"type": "integer"},  # Array
            "ImageIds": {"type": "integer"},     # Array
            "GroupCodeIds": {"type": "integer"}  # Array
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 Optimized SellerProduct index created (NO nested structures)")

# Get total count
pg_cursor.execute('SELECT COUNT(*) FROM "SellerItems" WHERE "Stock" > 0')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total records (Stock > 0): {total_count:,}")

# Data transfer
batch_size = 5000  # BÜYÜK BATCH (çünkü GROUP BY yok!)
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting FAST indexing (no GROUP BY, no json_agg)...")

while True:
    # ULTRA FAST QUERY - NO GROUP BY, NO json_agg!
    sql = f'''
    WITH SellerProducts AS (
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
            SI."ProductId"
        FROM "SellerItems" SI
        WHERE SI."Stock" > 0
          AND SI."Id" > {last_id}
        ORDER BY SI."Id"
        LIMIT {batch_size}
    ),
    FirstGroupCode AS (
        SELECT DISTINCT ON (pgc."ProductId")
            pgc."ProductId",
            unnest(string_to_array(pgc."GroupCode", '|')) AS "PartNumber"
        FROM "ProductGroupCodes" pgc
        WHERE pgc."ProductId" IN (SELECT "ProductId" FROM SellerProducts)
    )
    SELECT 
        SP."SellerItemId",
        SP."SellerId",
        SP."Stock",
        SP."CostPrice",
        SP."SalePrice",
        SP."Commision",
        SP."Currency",
        SP."Unit",
        SP."SellerStatus",
        SP."SellerModifiedDate",
        P."Id" AS "ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."Barcode" AS "ProductBarcode",
        P."Status",
        P."CreatedDate" AS "ProductCreatedDate",
        P."ModifiedDate" AS "ProductModifiedDate",
        P."BrandId",
        P."TaxId",
        B."Name" AS "BrandName",
        T."Name" AS "TaxName",
        T."Rate" AS "TaxRate",
        FGC."PartNumber",
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
        -- ID Arrays (simple arrays - NO json_agg!)
        ARRAY(SELECT pc."CategoryId" FROM "ProductCategories" pc WHERE pc."ProductId" = P."Id") AS "CategoryIds",
        ARRAY(SELECT pi."Id" FROM "ProductImages" pi WHERE pi."ProductId" = P."Id") AS "ImageIds",
        ARRAY(SELECT pgc2."Id" FROM "ProductGroupCodes" pgc2 WHERE pgc2."ProductId" = P."Id") AS "GroupCodeIds"
    FROM SellerProducts SP
    JOIN "Product" P ON P."Id" = SP."ProductId"
    LEFT JOIN FirstGroupCode FGC ON FGC."ProductId" = P."Id"
    LEFT JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = FGC."PartNumber"
    LEFT JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    LEFT JOIN "Brand" B ON B."Id" = P."BrandId"
    LEFT JOIN "Tax" T ON T."Id" = P."TaxId"
    ORDER BY SP."SellerItemId";
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
log_with_time(f"🎯 Completed! Total: {total_indexed:,}")

print("\n" + "="*60)
print("📝 NEXT STEPS:")
print("="*60)
print("1. Categories/Images cache için Redis kullan")
print("2. C# tarafında CategoryIds array'i al")
print("3. Redis'ten Categories çek (cache)")
print("4. 20 ürün × minimal query = ÇOK HIZLI!")
print("="*60)

