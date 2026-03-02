import time
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
import json
from collections import defaultdict

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

# Elasticsearch setup
es = Elasticsearch(
    ["http://localhost:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3"),
)

# Delete existing index and create new one
log_with_time("🚀 Setting up manufacturer index...")
if es.indices.exists(index="manufacturer_index"):
    es.indices.delete(index="manufacturer_index")

mapping = {
    "mappings": {
        "properties": {
            "Id": {"type": "integer"},
            "DatKey": {"type": "keyword"},
            "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
            "VehicleType": {"type": "integer"},
            "LogoUrl": {"type": "keyword"},
            "Order": {"type": "integer"},
            "ModelCount": {"type": "integer"},
            "Models": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
                    "VehicleType": {"type": "integer"},
                    "ManufacturerKey": {"type": "keyword"},
                    "BaseModelKey": {"type": "keyword"},
                    "ImageBase64": {
                        "type": "binary",
                        "doc_values": False
                    }
                }
            }
        }
    }
}
es.indices.create(index="manufacturer_index", body=mapping)
log_with_time("✅ Manufacturer index created")

# Step 1: Get all manufacturers - SADECE POPÜLER OLANLAR
log_with_time("📦 Step 1: Loading popular manufacturers (Order < 100)...")
start = time.time()
pg_cursor.execute('SELECT "Id", "DatKey", "Name", "VehicleType", "LogoUrl", "Order" FROM "DotManufacturers" WHERE "IsActive" = true AND "Order" < 100 AND "LogoUrl" IS NOT NULL AND "LogoUrl" != \'\' ORDER BY "Order", "Name"')
manufacturers = {}
for row in pg_cursor.fetchall():
    # Her DatKey için sadece ilk olanı al (duplicate önleme)
    if row[1] not in manufacturers:
        manufacturers[row[1]] = {
            'Id': row[0], 
            'DatKey': row[1], 
            'Name': row[2], 
            'VehicleType': row[3], 
            'LogoUrl': row[4], 
            'Order': row[5], 
            'Models': []
        }
log_with_time(f"✅ Loaded {len(manufacturers)} popular manufacturers", start)

# Step 2: Get all models - ONE QUERY
log_with_time("📦 Step 2: Loading all models...")
start = time.time()
pg_cursor.execute('SELECT "Id", "DatKey", "Name", "VehicleType", "ManufacturerKey" FROM "DotBaseModels" WHERE "IsActive" = true')
models_by_manufacturer = defaultdict(list)
model_data = {}
for row in pg_cursor.fetchall():
    model_data[row[1]] = {'Id': row[0], 'DatKey': row[1], 'Name': row[2], 'VehicleType': row[3], 'ManufacturerKey': row[4]}
    models_by_manufacturer[row[4]].append(row[1])
log_with_time(f"✅ Loaded {len(model_data)} models", start)

# Step 3: Get images via DotCompiledCodes - BATCH QUERY
log_with_time("📦 Step 3: Loading vehicle images via DotCompiledCodes...")
start = time.time()
pg_cursor.execute('''
SELECT DISTINCT ON (cc."BaseModelKey")
    cc."BaseModelKey",
    vi."ImageBase64"
FROM "DotCompiledCodes" cc
INNER JOIN "DotVehicleImages" vi ON vi."DatECode" = cc."DatECode"
WHERE cc."IsActive" = true 
    AND vi."IsActive" = true 
    AND vi."Aspect" = 'SIDEVIEW'
ORDER BY cc."BaseModelKey", vi."Id"
''')
images_by_model = {row[0]: row[1] for row in pg_cursor.fetchall()}
log_with_time(f"✅ Loaded {len(images_by_model)} vehicle images", start)

# Step 4: Combine data
log_with_time("📦 Step 4: Combining data...")
for mfr_key, mfr_data in manufacturers.items():
    model_keys = models_by_manufacturer.get(mfr_key, [])
    for model_key in model_keys[:100]:  # Limit 100 per manufacturer
        if model_key in model_data:
            model = model_data[model_key]
            img_base64 = images_by_model.get(model['DatKey'])
            
            mfr_data['Models'].append({
                'Id': model['Id'],
                'Name': model['Name'],
                'VehicleType': model['VehicleType'],
                'ManufacturerKey': model['ManufacturerKey'],
                'BaseModelKey': model['DatKey'],
                'ImageBase64': img_base64
            })
    mfr_data['ModelCount'] = len(mfr_data['Models'])

# Step 5: Index to Elasticsearch
log_with_time("📦 Step 5: Indexing to Elasticsearch...")
actions = [{"_index": "manufacturer_index", "_id": mfr['Id'], "_source": mfr} for mfr in manufacturers.values()]

try:
    start = time.time()
    success, errors = helpers.bulk(es, actions, raise_on_error=False)
    log_with_time(f"✅ Indexed {success} manufacturers with images to Elasticsearch", start)
    
    if errors:
        print(f"⚠️ {len(errors)} errors occurred")
        for err in errors[:5]:
            print(f"  - Error: {err}")
except BulkIndexError as e:
    print(f"❌ Bulk index error: {len(e.errors)} failed")
    for err in e.errors[:5]:
        print(f"  - {err}")

pg_cursor.close()
pg_conn.close()
log_with_time(f"🎯 Completed! {len(images_by_model)} vehicle images indexed.")

