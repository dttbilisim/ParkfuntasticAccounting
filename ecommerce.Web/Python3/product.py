import subprocess
import gc
import time
import sys
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

# Command line argument parsing
skip_procedures = "--skip-procedures" in sys.argv or "--no-sync" in sys.argv
recreate_index = "--recreate" in sys.argv or "--force-recreate" in sys.argv

if skip_procedures:
    log_with_time("⚡️ SKIP PROCEDURES MODE: Stored procedures will be skipped (fast test mode)")

if recreate_index:
    log_with_time("🔥 RECREATE MODE: Existing indices will be deleted and recreated.")

# 1. Run brand.py, category.py and manufacturer.py
log_with_time("🚀 Running brand.py, category.py and marka.py...")
for script in ["brand.py", "category.py", "marka.py"]:
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

# 3. Stored procedures (Enterprise-Grade with Progress Tracking)
if not skip_procedures:
    log_with_time("🔄 Running stored procedures (ENTERPRISE MODE)...")
    procedure_calls = [
        ("CALL sync_products_from_otoismails();", "OtoIsmail"),
        ("CALL sync_products_from_remars();", "Remars"),
        ("CALL sync_products_from_dega();", "Dega"),
        ("CALL sync_products_from_basbugs();", "Basbugs")
    ]

    # PostgreSQL NOTICE'ları yakalamak için cursor'u ayarla
    pg_conn.set_session(autocommit=False)
    pg_cursor.execute("SET client_min_messages TO NOTICE")

    for proc_sql, proc_name in procedure_calls:
        start = time.time()
        print(f"\n{'='*70}")
        print(f"⚙️  Executing: {proc_name}")
        print(f"{'='*70}")
        sys.stdout.flush()
        
        try:
            # Procedure'ü çalıştır
            pg_cursor.execute(proc_sql)
            
            # NOTICE'ları ekrana bas (procedure tamamlandığında birikmiş olacaklar)
            if pg_conn.notices:
                for notice in pg_conn.notices:
                    print(notice.strip())
                    sys.stdout.flush()
                pg_conn.notices.clear()
            
            pg_conn.commit()
            
            elapsed = time.time() - start
            print(f"✅ {proc_name} completed in {elapsed:.2f}s")
            log_with_time(f"✅ Completed: {proc_name}", start)
            
        except Exception as e:
            pg_conn.rollback()
            print(f"❌ ERROR in {proc_name}: {str(e)}")
            log_with_time(f"❌ ERROR in {proc_name}: {str(e)}", start)
            # Hata olsa bile diğer procedure'leri çalıştırmaya devam et
            continue
else:
    log_with_time("⏭️  Skipping stored procedures (--skip-procedures mode)")

# 4. Run createcategory.py
#start = time.time()
#log_with_time("🚀 Running createcategory.py...")
#subprocess.run(["python3", "createcategory.py"])
#log_with_time("✅ createcategory.py completed", start)

# 5. Elasticsearch setup (Optimized for Senior-Level Performance)
es = Elasticsearch(
    ["http://92.204.172.6:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
    max_retries=5,
    retry_on_timeout=True,
    request_timeout=300,
    connections_per_node=50 # Allow more parallel connections
)

# 5.1. Create image_index
log_with_time("📦 Creating image_index...")
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

if es.indices.exists(index="image_index") or es.indices.exists_alias(name="image_index"):
    log_with_time("ℹ️ 'image_index' exists (Index or Alias). Updating in-place (Upsert mode)...")
else:
    es.indices.create(index="image_index", body=image_mapping)
    log_with_time("✅ image_index created")

# 5.2. Elasticsearch Index Setup (In-Place Optimization Strategy)
# USER CONSTRAINT: Disk is full, cannot use Blue/Green (Copy/Write).
# STRATEGY: Detect duplicate indices, clean them up, then update in-place.
alias_name = "sellerproduct_index"
current_session_id = int(time.time())
log_with_time(f"🕒 Current Sync Session ID: {current_session_id}")

sellerproduct_mapping = {
    "settings": {
        "index": {
            "number_of_shards": 1,
            "number_of_replicas": 0,
            "codec": "best_compression"
        }
    },
    "mappings": {
        "properties": {
            "SellerItemId": {"type": "integer"},
            "SellerId": {"type": "integer"},
            "BranchId": {"type": "integer"},
            "SellerName": {"type": "keyword"},
            "Stock": {"type": "double"},
            "CostPrice": {"type": "double"},
            "SalePrice": {"type": "double"},
            "Commision": {"type": "double"},
            "Currency": {"type": "keyword"},
            "Unit": {"type": "keyword"},
            "SellerStatus": {"type": "integer"},
            "SellerModifiedDate": {"type": "date"},
            "SourceId": {"type": "keyword"},
            "Step": {"type": "double"},
            "MinSaleAmount": {"type": "double"},
            "MaxSaleAmount": {"type": "double"},
            "ProductId": {"type": "integer"},
            "ProductName": {
                "type": "text",
                "fields": {"keyword": {"type": "keyword"}}
            },
            "ProductDescription": {"type": "text", "index": False},
            "ProductBarcode": {"type": "keyword"},
            "ProductStatus": {"type": "integer"},
            "DocumentUrl": {"type": "keyword"},
            "MainImageUrl": {"type": "keyword"}, 
            "BrandId": {"type": "integer"},
            "CategoryId": {"type": "integer"},
            "TaxId": {"type": "integer"},
            "PartNumber": {"type": "keyword"},
            "DotPartName": {"type": "text"},
            "ManufacturerName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "ProductBrandName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "VehicleTypeName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "DotPartDescription": {"type": "text"},
            "BaseModelName": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "NetPrice": {"type": "double"},
            "PriceDate": {"type": "date"},
            "DatProcessNumber": {"type": "keyword"}, # Keyword supports arrays natively
            "VehicleType": {"type": "integer"},
            "ManufacturerKey": {"type": "keyword"},
            "BaseModelKey": {"type": "keyword"},
            "OemCode": {
                "type": "text", 
                "fields": {
                    "keyword": {"type": "keyword"}
                }
            },
            "SubModelsJson": {
                "type": "nested",
                "properties": {
                    "Name": {"type": "text"}
                }
            },
            "SyncSessionId": {"type": "long"}
        }
    }
}

# ⚠️ CRITICAL SPACE SAVING: Check for multiple indices on the Alias ⚠️
target_index = alias_name

if recreate_index:
    if es.indices.exists(index=alias_name):
        log_with_time(f"🗑️ Deleting index {alias_name} (--recreate flag set)")
        es.indices.delete(index=alias_name)
    if es.indices.exists_alias(name=alias_name):
        indices = es.indices.get_alias(name=alias_name)
        for idx in indices.keys():
            log_with_time(f"🗑️ Deleting index {idx} in alias {alias_name} (--recreate flag set)")
            es.indices.delete(index=idx)

if es.indices.exists_alias(name=alias_name):
    indices = es.indices.get_alias(name=alias_name)
    if len(indices) > 1:
        log_with_time(f"⚠️ DETECTED {len(indices)} INDICES ON ALIAS! CLEANING UP TO SAVE DISK SPACE...")
        # Sort by creation date (descending) -> Keep simplest name or newest
        sorted_indices = sorted(indices.keys(), reverse=True)
        target_index = sorted_indices[0] # Keep the "latest" 
        
        for idx in sorted_indices[1:]:
             log_with_time(f"🗑️ Deleting DUP index: {idx}")
             es.indices.delete(index=idx)
        
        log_with_time(f"✅ Kept index: {target_index}")
    else:
        target_index = list(indices.keys())[0]
        log_with_time(f"ℹ️ Found single index backing alias: {target_index}")
elif es.indices.exists(index=alias_name):
    target_index = alias_name
    log_with_time(f"ℹ️ Found concrete index: {target_index}")
else:
    # Does not exist, create it from scratch with full settings
    es.indices.create(index=alias_name, body=sellerproduct_mapping)
    log_with_time(f"✅ Created new index: {alias_name}")
    target_index = alias_name

# UPDATE SETTINGS & MAPPING
log_with_time(f"⚙️ Checking/Updating settings for {target_index}...")
try:
    # Prepare settings update
    analysis_settings = sellerproduct_mapping["settings"].get("analysis")
    
    # Dynamic settings that can be updated on open indices
    dynamic_settings = {
        "index": {
            "refresh_interval": "-1" 
        }
    }
    
    if analysis_settings:
        # Analyzers require closing the index
        es.indices.close(index=target_index)
        
        # Static/Complex settings update
        full_settings = {
            "index": {
                "analysis": analysis_settings,
                "codec": "best_compression"
            }
        }
        es.indices.put_settings(index=target_index, body=full_settings)
        es.indices.open(index=target_index)
    
    # Update dynamic settings (can be done while open or just after opening)
    es.indices.put_settings(index=target_index, body=dynamic_settings)
    
    # Put mapping
    es.indices.put_mapping(index=target_index, body=sellerproduct_mapping["mappings"])
    log_with_time(f"✅ Mapping verified for: {target_index}")
    
except Exception as e:
    log_with_time(f"⚠️ Settings/Mapping Update Error: {str(e)}")
    if "cannot be changed from type" in str(e):
        log_with_time("❌ CRITICAL: Structural mapping conflict detected!")
        log_with_time("💡 TIP: Use '--recreate' flag to delete and recreate the index with the new mapping.")
    # Ensure index is open even if update failed
    try: es.indices.open(index=target_index)
    except: pass

# 6.2. Index sellerproducts (SENIOR-LEVEL OPTIMIZATION - DISK/RAM FRIENDLY)
log_with_time("🚀 Starting optimized sellerproduct_index transfer...")

# 6.2.1. Create temporary tables for complex matching (Still needed but kept minimal)
log_with_time("🛠️ Creating minimal temporary tables...")
try:
    pg_conn.rollback() # Ensure clean state
    # Set work_mem for this session to balance RAM vs Disk
    pg_cursor.execute("SET work_mem = '64MB'") 
    
    pg_cursor.execute("DROP TABLE IF EXISTS tmp_dotparts")
    pg_cursor.execute('CREATE TEMPORARY TABLE tmp_dotparts AS SELECT "Id", "PartNumber", "DatProcessNumber", REGEXP_REPLACE("PartNumber", \'[^a-zA-Z0-9]\', \'\', \'g\') as "CleanPartNumber", "ManufacturerKey", "BaseModelKey" FROM "DotParts"')
    pg_cursor.execute('CREATE INDEX idx_tmp_dotparts_clean ON tmp_dotparts ("CleanPartNumber")')

    pg_cursor.execute("DROP TABLE IF EXISTS tmp_product_matched_codes")
    pg_cursor.execute("""
        CREATE TEMPORARY TABLE tmp_product_matched_codes AS
        SELECT DISTINCT ON (pg."ProductId")
            pg."ProductId", tdp."Id" as "DotPartId", tdp."PartNumber"
        FROM "ProductGroupCodes" pg
        CROSS JOIN LATERAL unnest(string_to_array(pg."OemCode", '|')) AS g
        JOIN tmp_dotparts tdp ON tdp."CleanPartNumber" = REGEXP_REPLACE(g, '[^a-zA-Z0-9]', '', 'g')
        WHERE pg."OemCode" IS NOT NULL AND pg."OemCode" != '' AND length(g) > 2
        ORDER BY pg."ProductId", (tdp."ManufacturerKey" IS NOT NULL AND tdp."ManufacturerKey" != '' AND tdp."BaseModelKey" IS NOT NULL AND tdp."BaseModelKey" != '') DESC
    """)
    pg_cursor.execute('CREATE INDEX idx_tmp_pmc_productid ON tmp_product_matched_codes ("ProductId")')



    # NEW: Aggregate ALL DatProcessNumbers for lookup (Union inferred + direct mappings)
    pg_cursor.execute("DROP TABLE IF EXISTS tmp_product_dat_numbers")
    pg_cursor.execute("""
        CREATE TEMPORARY TABLE tmp_product_dat_numbers AS
        WITH CombinedDPNs AS (
            -- From inferred matching
            SELECT 
                pg."ProductId", 
                tdp."DatProcessNumber"
            FROM "ProductGroupCodes" pg
            CROSS JOIN LATERAL unnest(string_to_array(pg."OemCode", '|')) AS g
            JOIN tmp_dotparts tdp ON tdp."CleanPartNumber" = REGEXP_REPLACE(g, '[^a-zA-Z0-9]', '', 'g')
            WHERE pg."OemCode" IS NOT NULL AND pg."OemCode" != '' AND length(g) > 2
            
            UNION
            
            -- From direct mappings (ProductOemDetail table)
            SELECT "ProductId", "DatProcessNumber"
            FROM "ProductOemDetail"
        )
        SELECT "ProductId", array_agg(DISTINCT "DatProcessNumber") AS "AllDatProcessNumbers"
        FROM CombinedDPNs
        GROUP BY "ProductId"
    """)
    pg_cursor.execute('CREATE INDEX idx_tmp_pdn_productid ON tmp_product_dat_numbers ("ProductId")')

    pg_cursor.execute("DROP TABLE IF EXISTS tmp_product_groupcodes_all")
    pg_cursor.execute('CREATE TEMPORARY TABLE tmp_product_groupcodes_all AS SELECT pg."ProductId", array_agg(DISTINCT pg."OemCode") AS "GroupCode" FROM "ProductGroupCodes" pg WHERE pg."OemCode" IS NOT NULL AND pg."OemCode" <> \'\' GROUP BY pg."ProductId"')
    pg_cursor.execute('CREATE INDEX idx_tmp_pgc_all_productid ON tmp_product_groupcodes_all ("ProductId")')
    
    # 6.2.2 Fetch lookups
    log_with_time("📥 Fetching lookups (Images,Categories,Brands) into memory...")
    
    pg_cursor.execute('SELECT "ProductId", "FileName" FROM "ProductImages" ORDER BY "Id" DESC')
    image_lookup = dict(pg_cursor.fetchall()) 
    
    pg_cursor.execute('SELECT "ProductId", "CategoryId" FROM "ProductCategories"')
    category_lookup = dict(pg_cursor.fetchall())
    
    pg_cursor.execute('SELECT "Id", "Name" FROM "Brand"')
    brand_lookup = dict(pg_cursor.fetchall())
    
    pg_conn.commit()
    log_with_time(f"✅ Lookups ready (Images: {len(image_lookup):,}, Categories: {len(category_lookup):,}, Brands: {len(brand_lookup):,})")

except Exception as e:
    pg_conn.rollback()
    print(f"🚨 Setup Error: {str(e)}")
    sys.exit(1)

# 6.2.5 High-Performance Data Transformation Generator
def document_generator(cursor, columns, index_name, image_map, cat_map, brand_map, session_id):
    processed = 0
    start_time = time.time()
    int_fields = {'SellerItemId', 'SellerId', 'ProductId', 'SellerStatus', 'ProductStatus', 'BrandId', 'CategoryId', 'TaxId', 'VehicleType', 'BranchId'}
    
    while True:
        rows = cursor.fetchmany(2000)
        if not rows: break
        for row in rows:
            product = dict(zip(columns, row))
            p_id = product['ProductId']
            
            # Enrich from memory lookups (FAST & Disk-Saving)
            product['MainImageUrl'] = image_map.get(p_id)
            product['CategoryId'] = cat_map.get(p_id)
            
            # Brand Fallback logic
            seller_brand = product.get('ManufacturerName')
            product_brand_id = product.get('BrandId')
            product_brand_name = brand_map.get(product_brand_id) if product_brand_id else None
            dotpart_brand = product.get('DotPartManufacturerName')

            final_brand = seller_brand or product_brand_name
            product['ProductBrandName'] = final_brand
            product['ManufacturerName'] = dotpart_brand

            # Optimized transformation
            for k, v in list(product.items()):
                if isinstance(v, datetime): product[k] = v.isoformat()
                elif isinstance(v, Decimal): product[k] = int(v) if k in int_fields else float(v)
                elif k == 'SubModelsJson' and isinstance(v, str):
                    try: product[k] = json.loads(v)
                    except: product[k] = []
            
            product['SyncSessionId'] = session_id
            
            # Unique ID: SellerItemId
            # Upsert logic: Same ID overwrites old data
            yield {"_index": index_name, "_id": product['SellerItemId'], "_source": product}
            processed += 1
            if processed % 100000 == 0:
                elapsed = time.time() - start_time
                print(f"📈 Progress: {processed:,} items | Speed: {processed/elapsed:.0f} items/sec")
                gc.collect()

# 6.2.6 Prepare Connection for High-Speed Indexing
pg_conn.commit() 
pg_conn.autocommit = False 

ss_cursor_name = f"ss_cursor_{int(time.time())}"
ss_cursor = pg_conn.cursor(name=ss_cursor_name)
ss_cursor.itersize = 2000

# SIMPLIFIED QUERY
sql = '''
    SELECT 
        SI."Id" AS "SellerItemId", SI."SellerId", S."BranchId", S."Name" AS "SellerName", SI."Stock", SI."CostPrice", SI."SalePrice", SI."Commision", SI."Currency", SI."Unit",
        SI."Status" AS "SellerStatus", SI."ModifiedDate" AS "SellerModifiedDate", SI."SourceId", COALESCE(SI."Step", 1) AS "Step", COALESCE(SI."MinSaleAmount", 1) AS "MinSaleAmount",
        COALESCE(SI."MaxSaleAmount", 0) AS "MaxSaleAmount", P."Id" AS "ProductId", P."Name" AS "ProductName", P."Description" AS "ProductDescription", P."Barcode" AS "ProductBarcode",
        P."Status" AS "ProductStatus", P."DocumentUrl", P."BrandId", P."TaxId", PFG."PartNumber", PGA."GroupCode" AS "OemCode",
        DP."Name" AS "DotPartName", SI."ManufacturerName", DP."ManufacturerName" as "DotPartManufacturerName", DP."VehicleTypeName",
        DP."Description" AS "DotPartDescription", DP."BaseModelName", DP."NetPrice", DP."PriceDate", 
        PDN."AllDatProcessNumbers" AS "DatProcessNumber", -- Use Aggregated List
        DP."VehicleType", DP."SubModelsJson",
        DP."ManufacturerKey", DP."BaseModelKey"
    FROM "SellerItems" SI
    JOIN "Product" P ON P."Id" = SI."ProductId"
    JOIN "Sellers" S ON S."Id" = SI."SellerId"
    LEFT JOIN tmp_product_matched_codes PFG ON PFG."ProductId" = P."Id"
    LEFT JOIN tmp_product_groupcodes_all PGA ON PGA."ProductId" = P."Id"
    LEFT JOIN tmp_product_dat_numbers PDN ON PDN."ProductId" = P."Id" -- Join Aggregated DPNs
    LEFT JOIN "DotParts" DP ON DP."Id" = PFG."DotPartId"
    WHERE SI."SourceId" IS NOT NULL AND SI."Status" = 1 AND P."Status" = 1
'''

log_with_time("🚀 Executing main query with Parallel Bulk Indexing...")
ss_cursor.execute(sql)

first_batch = ss_cursor.fetchmany(1)
if not first_batch:
    log_with_time("⚠️ No active products found to index.")
    ss_cursor.close()
else:
    columns = [desc[0] for desc in ss_cursor.description]
    
    def combined_generator_fixed(first_row, cursor, columns, index_name, image_map, cat_map, brand_map, session_id):
        # Process first row manually
        product = dict(zip(columns, first_row[0]))
        p_id = product['ProductId']
        product['MainImageUrl'] = image_map.get(p_id)
        product['CategoryId'] = cat_map.get(p_id)
        
        seller_brand = product.get('ManufacturerName')
        product_brand_id = product.get('BrandId')
        product_brand_name = brand_map.get(product_brand_id) if product_brand_id else None
        dotpart_brand = product.get('DotPartManufacturerName')
        
        final_brand = seller_brand or product_brand_name
        product['ProductBrandName'] = final_brand
        product['ManufacturerName'] = dotpart_brand

        for k, v in list(product.items()):
            if isinstance(v, datetime): product[k] = v.isoformat()
            elif isinstance(v, Decimal): product[k] = int(v) if k in {'SellerItemId', 'SellerId', 'ProductId', 'SellerStatus', 'ProductStatus', 'BrandId', 'CategoryId', 'TaxId', 'VehicleType', 'BranchId'} else float(v)
            elif k == 'SubModelsJson' and isinstance(v, str):
                try: product[k] = json.loads(v)
                except: product[k] = []
        
        product['SyncSessionId'] = session_id
        yield {"_index": index_name, "_id": product['SellerItemId'], "_source": product}
        yield from document_generator(cursor, columns, index_name, image_map, cat_map, brand_map, session_id)

    total_success = 0
    start_time = time.time()
    try:
        for success, info in helpers.parallel_bulk(
            es, combined_generator_fixed(first_batch, ss_cursor, columns, target_index, image_lookup, category_lookup, brand_lookup, current_session_id), 
            thread_count=4, chunk_size=500, request_timeout=360, queue_size=2
        ):
            if success: total_success += 1
            else: print(f"❌ Failed: {info}")
            
        log_with_time(f"✅ Parallel indexing completed! Total: {total_success:,}")
        
    except Exception as e: 
        print(f"🚨 Indexing Error: {str(e)}")

ss_cursor.close()

# 6.3. Cleanup Stale Records (In-Place Optimization)
log_with_time("🧹 Cleaning up stale records (not updated in this session)...")
try:
    delete_query = {
        "query": {
            "bool": {
                "must_not": [
                    {"term": {"SyncSessionId": current_session_id}}
                ]
            }
        }
    }
    # Using delete_by_query to remove orphans
    res = es.delete_by_query(index=target_index, body=delete_query, conflicts="proceed", wait_for_completion=True)
    log_with_time(f"🗑️ Deleted {res.get('deleted', 0)} orphan records")
except Exception as e: print(f"⚠️ Cleanup Error: {str(e)}")


# 6.4. Index images (Sequential)
log_with_time("🚀 Starting image_index transfer (Upsert Mode)...")
try:
    pg_conn.rollback() # Fix "InFailedSqlTransaction"
    pg_cursor.execute('SELECT "Id", "ProductId", "FileName", "FileGuid", "CreatedDate", "ModifiedDate" FROM "ProductImages" ORDER BY "Id"')
    total_images_indexed = 0
    img_cols = [desc[0] for desc in pg_cursor.description]
    
    while True:
        rows = pg_cursor.fetchmany(5000)
        if not rows: break
        img_actions = []
        for row in rows:
            image = dict(zip(img_cols, row))
            for k, v in image.items():
                if isinstance(v, datetime): image[k] = v.isoformat()
            img_actions.append({"_index": "image_index", "_id": image["Id"], "_source": image})
        success, _ = helpers.bulk(es, img_actions)
        total_images_indexed += success
    log_with_time(f"✅ image_index completed! Total: {total_images_indexed:,}")
except Exception as e: print(f"⚠️ Image Indexing Error: {str(e)}")

# 7. Finalize
log_with_time("🔄 Final settings and cleanup...")
try:
    for index_name in ["image_index", target_index]:
        if es.indices.exists(index=index_name):
            es.indices.put_settings(index=index_name, body={"index": {"refresh_interval": "1s", "number_of_replicas": 1}})
            es.indices.refresh(index=index_name)
except Exception as e: print(f"⚠️ Finalize Error: {str(e)}")

pg_cursor.close()
pg_conn.close()
print(f"\n{'='*70}\n🎯 ALL OPERATIONS COMPLETED!\n{'='*70}")