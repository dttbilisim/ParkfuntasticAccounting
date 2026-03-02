import subprocess
import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
from decimal import Decimal
import json
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

# 3. Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
)

# 4. Delete existing index and create new one with mapping
if es.indices.exists(index="sellerproduct_index"):
    es.indices.delete(index="sellerproduct_index")

mapping = {
    "mappings": {
        "properties": {
            "PartNumber": {"type": "text"},
            "ProductId": {"type": "integer"},
            "DotPartName": {"type": "text"},
            "ManufacturerName": {"type": "text"},
            "VehicleTypeName": {"type": "text"},
            "Description": {"type": "text"},
            "BaseModelName": {"type": "text"},
            "NetPrice": {"type": "double"},
            "PriceDate": {"type": "date"},
            "VehicleTypeKey": {"type": "integer"},
            "ManufactureKey": {"type": "integer"},
            "BaseModelKey": {"type": "integer"},
            # Product fields (P.*)
            "ProductName": {"type": "text"},
            "ProductDescription": {"type": "text"},
            "BrandId": {"type": "integer"},
            "TaxId": {"type": "integer"},
            "Status": {"type": "integer"},
            "CreatedDate": {"type": "date"},
            "ModifiedDate": {"type": "date"},
            "CreatedId": {"type": "integer"},
            "ModifiedId": {"type": "integer"},
            # SellerItems fields (SI.*)
            "SellerItemId": {"type": "integer"},
            "SellerId": {"type": "integer"},
            "Stock": {"type": "integer"},
            "CostPrice": {"type": "double"},
            "SalePrice": {"type": "double"},
            "Commision": {"type": "double"},
            "Currency": {"type": "keyword"},
            "Unit": {"type": "text"},
            "SellerStatus": {"type": "integer"},
            "SellerCreatedDate": {"type": "date"},
            "SellerModifiedDate": {"type": "date"},
            "SellerCreatedId": {"type": "integer"},
            "SellerModifiedId": {"type": "integer"},
            # Additional nested structures
            "Categories": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text"}
                }
            },
            "Images": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "FileName": {"type": "text"},
                    "FileGuid": {"type": "text"}
                }
            },
            "Brand": {
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text"}
                }
            }
        }
    }
}
es.indices.create(index="sellerproduct_index", body=mapping)
log_with_time("📦 New SellerProduct index created with mapping")

# 5. Data transfer from PostgreSQL to Elasticsearch
batch_size = 1000
offset = 0
total_indexed = 0
log_with_time("🚀 Starting Elasticsearch data transfer for SellerProduct...")

while True:
    sql = f'''
    WITH flat_codes AS (
        SELECT 
            "ProductId",
            unnest(string_to_array("GroupCode", '|')) AS g
        FROM "ProductGroupCodes"
    )
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
    FROM flat_codes fc
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
    LIMIT {batch_size} OFFSET {offset};
    '''

    start = time.time()
    pg_cursor.execute(sql)
    columns = [desc[0] for desc in pg_cursor.description]
    rows = pg_cursor.fetchall()
    if not rows:
        log_with_time("⚡️ All seller products have been indexed to Elasticsearch.")
        break

    actions = []
    for row in rows:
        product = dict(zip(columns, row))
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
        
        # Use SellerItemId as unique document ID
        doc_id = f"{product.get('ProductId')}_{product.get('SellerItemId')}_{product.get('PartNumber')}"
        
        actions.append({
            "_index": "sellerproduct_index",
            "_id": doc_id,
            "_source": product
        })

    try:
        success, _ = helpers.bulk(es, actions)
        total_indexed += success
        log_with_time(f"✅ Batch indexed: {success} seller products, Total: {total_indexed}", start)
    except BulkIndexError as e:
        print(f"❌ Error: {len(e.errors)} documents failed.")
        for err in e.errors[:3]:
            print(err)

    offset += batch_size

# 6. Maintenance
log_with_time("🧩 Running ANALYZE on related tables...")
maintenance_sqls = [
    'ANALYZE "ProductGroupCodes";',
    'ANALYZE "Product";',
    'ANALYZE "SellerItems";',
    'ANALYZE "DatDatas";',
    'ANALYZE "ProductCategories";',
    'ANALYZE "Category";',
    'ANALYZE "ProductImages";',
    'ANALYZE "Brand";'
]
for sql in maintenance_sqls:
    start = time.time()
    print(f"🔄 {sql.strip()}")
    pg_cursor.execute(sql)
    pg_conn.commit()
    log_with_time(f"✅ {sql.strip()} completed", start)

# 7. Close
pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 All operations completed. Total indexed seller products: {total_indexed}")

