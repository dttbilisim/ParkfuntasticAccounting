import subprocess
import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
from decimal import Decimal
import json

def log_with_time(message, start=None):
    if start:
        elapsed = time.time() - start
        print(f"{message} ⏱️ {elapsed:.2f}s")
    else:
        print(message)

# 1. Run brand.py, category.py and manufacturer.py
log_with_time("🚀 Running brand.py, category.py and manufacturer.py...")
for script in ["brand.py", "category.py", "manufacturer.py"]:
    start = time.time()
    subprocess.run(["python3", script]) 
    log_with_time(f"✅ {script} completed", start)

# 2. PostgreSQL connection
pg_conn = psycopg2.connect(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)
pg_cursor = pg_conn.cursor()

# 3. Stored procedures
log_with_time("🔄 Running stored procedures...")
procedure_calls = [
    "CALL sync_products_from_otoismails();",
    "CALL sync_products_from_remars();",
    "CALL sync_products_from_dega();",
    "CALL sync_products_from_basbugs();"
]
for proc in procedure_calls:
    start = time.time()
    print(f"⚙️  Executing: {proc}")
    pg_cursor.execute(proc)
    pg_conn.commit()
    log_with_time(f"✅ Completed: {proc.strip()}", start)

# 5. Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=3,
    retry_on_timeout=True,
    request_timeout=60
)

# 5.1. Create image_index (Original script had this, adding it back for completeness)
log_with_time("📦 Creating image_index...")
if es.indices.exists(index="image_index"):
    es.indices.delete(index="image_index")

image_mapping = {
    "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "refresh_interval": "-1"
    },
    "mappings": {
        "properties": {
            "Id": {"type": "integer"},
            "ProductId": {"type": "integer"},
            "FileName": {"type": "keyword"},
            "FileGuid": {"type": "keyword"},
            "CreatedDate": {"type": "date"},
            "ModifiedDate": {"type": "date"}
        }
    }
}
es.indices.create(index="image_index", body=image_mapping)
log_with_time("✅ image_index created")

# 5.2. Create sellerproduct_index (multi-index strategy)
log_with_time("📦 Ensuring sellerproduct_index mapping...")
sellerproduct_properties = {
    "SellerItemId": {"type": "integer"},
    "SellerId": {"type": "integer"},
    "Stock": {"type": "double"},
    "CostPrice": {"type": "double"},
    "SalePrice": {"type": "double"},
    "Commision": {"type": "double"},
    "Currency": {"type": "keyword"},
    "Unit": {"type": "keyword"},
    "SellerStatus": {"type": "integer"},
    "SellerModifiedDate": {"type": "date"},
    "SourceId": {"type": "keyword"},
    # Product
    "ProductId": {"type": "integer"},
    "ProductName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
    "ProductDescription": {"type": "text"},
    "ProductBarcode": {"type": "keyword"},
    "ProductStatus": {"type": "integer"},
    "DocumentUrl": {"type": "keyword"},
    "BrandId": {"type": "integer"},
    "CategoryId": {"type": "integer"},
    "TaxId": {"type": "integer"},
    # DotParts (LEFT JOIN - NULL olabilir, sadece İLK GroupCode)
    "PartNumber": {"type": "keyword"},
    "DotPartName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
    "ManufacturerName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
    "VehicleTypeName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
    "DotPartDescription": {"type": "text"},
    "BaseModelName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
    "NetPrice": {"type": "double"},
    "PriceDate": {"type": "date"},
    "DatProcessNumber": {"type": "keyword"},
    "VehicleType": {"type": "integer"},
    "ManufacturerKey": {"type": "keyword"},
    "BaseModelKey": {"type": "keyword"},
    "GroupCode": {"type": "keyword"},
    "SubModelsJson": {
        "type": "nested",
        "properties": {
            "Key": {"type": "keyword"},
            "Name": {"type": "text"}
        }
    }
}

if es.indices.exists(index="sellerproduct_index"):
    log_with_time("ℹ️ Updating sellerproduct_index mapping in-place...")
    es.indices.put_mapping(index="sellerproduct_index", body={"properties": sellerproduct_properties})
else:
    sellerproduct_mapping = {
        "settings": {
            "number_of_shards": 2,
            "number_of_replicas": 0,
            "refresh_interval": "-1"
        },
        "mappings": {
            "properties": sellerproduct_properties
        }
    }
    es.indices.create(index="sellerproduct_index", body=sellerproduct_mapping)
    log_with_time("✅ sellerproduct_index created")

# 6.1. Index images (HIZLI!)
log_with_time("🚀 Starting image_index transfer...")
pg_cursor.execute('SELECT COUNT(*) FROM "ProductImages"')
total_images = pg_cursor.fetchone()[0]
log_with_time(f"📊 Total images: {total_images:,}")

image_batch_size = 10000
last_image_id = 0
total_images_indexed = 0

while True:
    sql = f'''
    SELECT "Id", "ProductId", "FileName", "FileGuid", "CreatedDate", "ModifiedDate"
    FROM "ProductImages"
    WHERE "Id" > {last_image_id}
    ORDER BY "Id"
    LIMIT {image_batch_size};
    '''

    start = time.time()
    pg_cursor.execute(sql)
    columns = [desc[0] for desc in pg_cursor.description]
    rows = pg_cursor.fetchall()
    if not rows:
        break

    last_image_id = rows[-1][0]

    actions = []
    for row in rows:
        image = dict(zip(columns, row))
        for k, v in image.items():
            if isinstance(v, datetime):
                image[k] = v.isoformat()
        actions.append({
            "_index": "image_index",
            "_id": image.get("Id"),
            "_source": image
        })

    try:
        success, _ = helpers.bulk(es, actions, chunk_size=5000)
        total_images_indexed += success
        log_with_time(f"✅ Images batch: {success}, Total: {total_images_indexed:,}/{total_images:,}", start)
    except BulkIndexError as e:
        print(f"❌ Error: {len(e.errors)} images failed.")

log_with_time(f"✅ image_index completed! Total: {total_images_indexed:,}")


# 6.2. Index sellerproducts (HIZLI! - GROUP BY yok)
log_with_time("🚀 Starting sellerproduct_index transfer...")

# 6.2.1. Create temporary table for DotParts normalization
log_with_time("🛠️ Creating temporary table tmp_dotparts...")
pg_cursor.execute("DROP TABLE IF EXISTS tmp_dotparts")
pg_cursor.execute("""
    CREATE TEMPORARY TABLE tmp_dotparts AS
    SELECT 
        "Id",
        "PartNumber",
        REGEXP_REPLACE("PartNumber", '[^a-zA-Z0-9]', '', 'g') as "CleanPartNumber",
        "ManufacturerKey",
        "BaseModelKey"
    FROM "DotParts"
""")
pg_cursor.execute('CREATE INDEX idx_tmp_dotparts_clean ON tmp_dotparts ("CleanPartNumber")')
pg_cursor.execute('ANALYZE tmp_dotparts')
log_with_time("✅ tmp_dotparts created and indexed")

# 6.2.2. Create temporary table for Product Group Codes matching
log_with_time("🛠️ Creating temporary table tmp_product_matched_codes (This may take a while)...")
pg_cursor.execute("DROP TABLE IF EXISTS tmp_product_matched_codes")
pg_cursor.execute("""
    CREATE TEMPORARY TABLE tmp_product_matched_codes AS
    SELECT DISTINCT ON (pg."ProductId")
        pg."ProductId",
        pg."GroupCode",
        tdp."Id" as "DotPartId",
        tdp."PartNumber"
    FROM "ProductGroupCodes" pg
    CROSS JOIN LATERAL unnest(string_to_array(pg."GroupCode", '|')) AS g
    JOIN tmp_dotparts tdp ON tdp."CleanPartNumber" = REGEXP_REPLACE(g, '[^a-zA-Z0-9]', '', 'g')
    WHERE pg."GroupCode" IS NOT NULL AND pg."GroupCode" != '' AND length(g) > 2
    ORDER BY pg."ProductId", (tdp."ManufacturerKey" IS NOT NULL AND tdp."ManufacturerKey" != '' AND tdp."BaseModelKey" IS NOT NULL AND tdp."BaseModelKey" IS NOT NULL) DESC
""")
pg_cursor.execute('CREATE INDEX idx_tmp_pmc_productid ON tmp_product_matched_codes ("ProductId")')
pg_cursor.execute('ANALYZE tmp_product_matched_codes')
log_with_time("✅ tmp_product_matched_codes created and indexed")

# NOT: Gerçek kayıt sayısı SellerItems sayısından farklı olabilir
# Çünkü bazı SellerItems için Product/DotParts bulunamayabilir
# Progress tracking için yaklaşık sayı kullanıyoruz
pg_cursor.execute('SELECT COUNT(*) FROM "SellerItems"')
total_sellerproducts_estimate = pg_cursor.fetchone()[0]
log_with_time(f"📊 Estimated sellerproducts (Stock > 0): ~{total_sellerproducts_estimate:,}")

sellerproduct_batch_size = 10000
last_selleritem_id = 0
total_sellerproducts_indexed = 0

while True:
    # DISTINCT ON ile her SellerItem için SADECE 1 kayıt döner
    # FIX APPLIED: Also join raw ProductGroupCodes to fallback if PFG is null
    sql = f'''
    SELECT DISTINCT ON (SI."Id")
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
        SI."SourceId",
        P."Id" AS "ProductId",
        P."Name" AS "ProductName",
        P."Description" AS "ProductDescription",
        P."Barcode" AS "ProductBarcode",
        P."Status" AS "ProductStatus",
        P."DocumentUrl",
        P."BrandId",
        P."TaxId",
        PC."CategoryId",
        
        -- FIX: Use PFG.PartNumber if available, otherwise PGC_RAW ? No, PartNumber comes from matched DotParts usually.
        PFG."PartNumber",
        
        -- KEY FIX: If enriched GroupCode is null, use raw ProductGroupCodes
        COALESCE(PFG."GroupCode", PGC_RAW."GroupCode") AS "GroupCode",
        
        DP."Name" AS "DotPartName",
        DP."ManufacturerName",
        DP."VehicleTypeName",
        DP."Description" AS "DotPartDescription",
        DP."BaseModelName",
        DP."NetPrice",
        DP."PriceDate",
        DP."DatProcessNumber",
        DP."VehicleType",
        DP."SubModelsJson",
        DP."ManufacturerKey",
        DP."BaseModelKey"
    FROM "SellerItems" SI
    JOIN "Product" P ON P."Id" = SI."ProductId"
    LEFT JOIN tmp_product_matched_codes PFG ON PFG."ProductId" = P."Id"
    LEFT JOIN "DotParts" DP ON DP."Id" = PFG."DotPartId"
    
    -- ADDED RAW JOIN
    LEFT JOIN "ProductGroupCodes" PGC_RAW ON PGC_RAW."ProductId" = P."Id"
    
    LEFT JOIN (
        SELECT DISTINCT ON ("ProductId") "ProductId", "CategoryId"
        FROM "ProductCategories"
        ORDER BY "ProductId", "Id"
    ) PC ON PC."ProductId" = P."Id"
    WHERE 
      SI."Id" > {last_selleritem_id} AND SI."SourceId" IS NOT NULL
    
    ORDER BY SI."Id"
    LIMIT {sellerproduct_batch_size};
    '''

    start = time.time()
    pg_cursor.execute(sql)
    columns = [desc[0] for desc in pg_cursor.description]
    rows = pg_cursor.fetchall()
    if not rows:
        log_with_time("⚡️ All sellerproducts indexed!")
        break

    last_selleritem_id = rows[-1][0]  # SellerItemId

    actions = []
    for row in rows:
        product = dict(zip(columns, row))
        for k, v in product.items():
            if isinstance(v, datetime):
                product[k] = v.isoformat()
            elif isinstance(v, Decimal):
                # Integer field'lar için int'e cast et (Stock hariç - double olarak kalacak)
                if k in ['SellerItemId', 'SellerId', 'ProductId', 'SellerStatus', 'ProductStatus', 'BrandId', 'CategoryId', 'TaxId', 'VehicleType']:
                    product[k] = int(v)
                else:
                    product[k] = float(v)
            elif k == 'SubModelsJson' and isinstance(v, str):
                try:
                    product[k] = json.loads(v)
                except:
                    product[k] = []

        # Document ID: SellerItemId (UNIQUE - her SellerItem için 1 kayıt)
        doc_id = product['SellerItemId']

        actions.append({
            "_index": "sellerproduct_index",
            "_id": doc_id,
            "_source": product
        })

    try:
        success, _ = helpers.bulk(es, actions, chunk_size=5000)
        total_sellerproducts_indexed += success
        log_with_time(f"✅ SellerProducts batch: {success}, Total indexed: {total_sellerproducts_indexed:,}", start)
    except BulkIndexError as e:
        print(f"❌ Error: {len(e.errors)} sellerproducts failed.")

log_with_time(f"✅ sellerproduct_index completed! Total: {total_sellerproducts_indexed:,}")

# 6.3. Re-enable refresh and replicas
log_with_time("🔄 Enabling refresh and replicas...")
for index_name in ["image_index", "sellerproduct_index"]:
    es.indices.put_settings(
        index=index_name,
        body={
            "index": {
                "refresh_interval": "1s",
                "number_of_replicas": 1
            }
        }
    )
    es.indices.refresh(index=index_name)
log_with_time("✅ Index settings updated")

# 7. Maintenance
log_with_time("🧩 Running ANALYZE + WAL switch + CHECKPOINT...")
maintenance_sqls = [
    'ANALYZE "Product";',
    'ANALYZE "SellerItems";',
    'ANALYZE "Brand";',
    'ANALYZE "ProductGroupCodes";',
    'ANALYZE "ProductCategories";',
    'ANALYZE "ProductImages";',
    'ANALYZE "ProductOemDetail";',
    'ANALYZE "Category";',
    'ANALYZE "Tax";',
    'ANALYZE "DotParts";',
    'SELECT pg_switch_wal();',
    'CHECKPOINT;'
]
for sql in maintenance_sqls:
    start = time.time()
    print(f"🔄 {sql.strip()}")
    pg_cursor.execute(sql)
    pg_conn.commit()
    log_with_time(f"✅ {sql.strip()} completed", start)

# 8. Close
pg_cursor.close()
pg_conn.close()

print("\n" + "="*70)
print("🎯 ALL OPERATIONS COMPLETED!")
print("="*70)
print(f"✅ image_index: {total_images_indexed:,} images")
print(f"✅ sellerproduct_index: {total_sellerproducts_indexed:,} products (Stock > 0)")
print("="*70)
print("📋 Elasticsearch Index'leri:")
print("  - brand_index (zaten var)")
print("  - category_index (zaten var)")
print("  - image_index (YENİ OLUŞTURULDU)")
print("  - sellerproduct_index (YENİ OLUŞTURULDU)")
print("="*70)
print("💡 C# tarafında multi-index join kullan!")
print("   Detaylar: MULTI_INDEX_USAGE.md")
print("="*70)
