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

# Delete and create index with FULL mapping
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
            # Product fields (TÜM KOLONLAR)
            "ProductId": {"type": "integer"},
            "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductDescription": {"type": "text"},
            "BrandId": {"type": "integer"},
            "TaxId": {"type": "integer"},
            "Status": {"type": "integer"},
            "ProductCreatedDate": {"type": "date"},
            "ProductModifiedDate": {"type": "date"},
            "ProductCreatedId": {"type": "integer"},
            "ProductModifiedId": {"type": "integer"},
            "ProductBarcode": {"type": "keyword"},
            "ProductProductTypeId": {"type": "integer"},
            # SellerItems fields (TÜM KOLONLAR)
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
            # Nested structures (PRODUCT.PY GİBİ)
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
            "GroupCodes": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "GroupCode": {"type": "text"}
                }
            },
            "Brand": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}}
                }
            },
            "Tax": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text"}
                }
            }
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 Full SellerProduct index created")

# Get total count (SellerItems Stock > 0 HEPSI)
pg_cursor.execute('''
    SELECT COUNT(*)
    FROM "SellerItems" SI
    WHERE SI."Stock" > 0
''')
total_count = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total records (Stock > 0): {total_count:,}")

# Data transfer
batch_size = 2000  # CTE + LEFT JOIN ile optimize edildi
last_id = 0
total_indexed = 0
log_with_time("🚀 Starting indexing...")

while True:
    # PERFORMANSLI QUERY - SellerItems'dan başla (Stock > 0), DotParts opsiyonel LEFT JOIN
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
            SI."CreatedDate" AS "SellerCreatedDate",
            SI."ModifiedDate" AS "SellerModifiedDate",
            SI."CreatedId" AS "SellerCreatedId",
            SI."ModifiedId" AS "SellerModifiedId",
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
        -- SellerItems fields
        SP."SellerItemId",
        SP."SellerId",
        SP."Stock",
        SP."CostPrice",
        SP."SalePrice",
        SP."Commision",
        SP."Currency",
        SP."Unit",
        SP."SellerStatus",
        SP."SellerCreatedDate",
        SP."SellerModifiedDate",
        SP."SellerCreatedId",
        SP."SellerModifiedId",
        -- Product fields
        P."Id" AS "ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."BrandId",
        P."TaxId",
        P."Status",
        P."CreatedDate" AS "ProductCreatedDate",
        P."ModifiedDate" AS "ProductModifiedDate",
        P."CreatedId" AS "ProductCreatedId",
        P."ModifiedId" AS "ProductModifiedId",
        P."Barcode" AS "ProductBarcode",
        P."ProductTypeId" AS "ProductProductTypeId",
        -- DotParts fields (LEFT JOIN - NULL olabilir)
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
        -- Nested structures
        json_agg(DISTINCT jsonb_build_object('Id', c."Id", 'Name', c."Name")) 
            FILTER (WHERE c."Id" IS NOT NULL) AS "Categories",
        json_agg(DISTINCT jsonb_build_object('Id', i."Id", 'FileName', i."FileName", 'FileGuid', i."FileGuid")) 
            FILTER (WHERE i."Id" IS NOT NULL) AS "Images",
        json_agg(DISTINCT jsonb_build_object('Id', gc."Id", 'GroupCode', gc."GroupCode")) 
            FILTER (WHERE gc."Id" IS NOT NULL) AS "GroupCodes",
        jsonb_build_object('Id', b."Id", 'Name', b."Name") AS "Brand",
        jsonb_build_object('Id', t."Id", 'Name', t."Name") AS "Tax"
    FROM SellerProducts SP
    JOIN "Product" P ON P."Id" = SP."ProductId"
    LEFT JOIN FirstGroupCode FGC ON FGC."ProductId" = P."Id"
    LEFT JOIN "mv_dotparts_joined" DP ON DP."PartNumber" = FGC."PartNumber"
    LEFT JOIN "DatDatas" DD
        ON DP."VehicleTypeKey" = DD."VehicleTypeKey"
        AND DP."ManufactureKey" = DD."ManufactureKey"
        AND DP."BaseModelKey" = DD."BaseModelKey"
    LEFT JOIN "Brand" b ON b."Id" = P."BrandId"
    LEFT JOIN "Tax" t ON t."Id" = P."TaxId"
    LEFT JOIN "ProductCategories" pc ON pc."ProductId" = P."Id"
    LEFT JOIN "Category" c ON c."Id" = pc."CategoryId"
    LEFT JOIN "ProductImages" i ON i."ProductId" = P."Id"
    LEFT JOIN "ProductGroupCodes" gc ON gc."ProductId" = P."Id"
    GROUP BY 
        SP."SellerItemId", SP."SellerId", SP."Stock", SP."CostPrice", SP."SalePrice",
        SP."Commision", SP."Currency", SP."Unit", SP."SellerStatus",
        SP."SellerCreatedDate", SP."SellerModifiedDate", SP."SellerCreatedId", SP."SellerModifiedId",
        P."Id", P."Name", P."Description", P."BrandId", P."TaxId",
        P."Status", P."CreatedDate", P."ModifiedDate", P."CreatedId", P."ModifiedId",
        P."Barcode", P."ProductTypeId",
        FGC."PartNumber",
        DP."Name", DP."ManufacturerName", DP."VehicleTypeName", DP."Description",
        DP."BaseModelName", DP."NetPrice", DP."PriceDate",
        DD."VehicleTypeKey", DD."ManufactureKey", DD."BaseModelKey",
        b."Id", b."Name", t."Id", t."Name"
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
            elif isinstance(v, list):
                for i in range(len(v)):
                    if isinstance(v[i], dict):
                        for kk, vv in v[i].items():
                            if isinstance(vv, datetime):
                                v[i][kk] = vv.isoformat()
                            elif isinstance(vv, Decimal):
                                v[i][kk] = float(vv)
        
        # Document ID: PartNumber NULL olabilir (DotParts yoksa)
        part_number = product.get('PartNumber') or 'NOPART'
        doc_id = f"{product['ProductId']}_{product['SellerItemId']}_{part_number}"
        
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
            f"✅ Batch: {success} | Total: {total_indexed:,}/{total_count:,} ({progress_pct:.1f}%)", 
            start
        )
        
        if errors:
            print(f"⚠️  {len(errors)} errors")
            
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

