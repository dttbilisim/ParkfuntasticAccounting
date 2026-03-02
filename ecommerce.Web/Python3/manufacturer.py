import time
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError
import psycopg2
import json

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
            "Name": {
                "type": "text",
                "fields": {
                    "keyword": {"type": "keyword"}
                }
            },
            "VehicleType": {"type": "integer"},
            "LogoUrl": {"type": "keyword"},
            "Order": {"type": "integer"},
            "ModelCount": {"type": "integer"},
            "Models": {
                "type": "nested",
                "properties": {
                    "Id": {"type": "integer"},
                    "Name": {
                        "type": "text",
                        "fields": {
                            "keyword": {"type": "keyword"}
                        }
                    },
                    "VehicleType": {"type": "integer"},
                    "ManufacturerKey": {"type": "keyword"},
                    "BaseModelKey": {"type": "keyword"},
                    "ImageUrl": {"type": "keyword"}
                }
            }
        }
    }
}
es.indices.create(index="manufacturer_index", body=mapping)
log_with_time("✅ Manufacturer index created")

# Fetch manufacturers with their models AND IMAGES
log_with_time("📦 Fetching manufacturers with models and images...")
start = time.time()

sql = '''
SELECT 
    m."Id",
    m."DatKey",
    m."Name",
    m."VehicleType",
    m."LogoUrl",
    m."Order",
    json_agg(DISTINCT
        jsonb_build_object(
            'Id', bm."Id",
            'Name', bm."Name",
            'VehicleType', bm."VehicleType",
            'ManufacturerKey', bm."ManufacturerKey",
            'BaseModelKey', bm."DatKey",
            'ImageUrl', vi."Url"
        ) ORDER BY jsonb_build_object(
            'Id', bm."Id",
            'Name', bm."Name",
            'VehicleType', bm."VehicleType",
            'ManufacturerKey', bm."ManufacturerKey",
            'BaseModelKey', bm."DatKey",
            'ImageUrl', vi."Url"
        )
    ) FILTER (WHERE bm."Id" IS NOT NULL) AS "Models"
FROM "DotManufacturers" m
LEFT JOIN (
    SELECT DISTINCT ON ("Id") "Id", "DatKey", "Name", "VehicleType", "ManufacturerKey"
    FROM "DotBaseModels"
    WHERE "IsActive" = true
) bm ON bm."ManufacturerKey" = m."DatKey"
LEFT JOIN LATERAL (
    SELECT vi2."Url"
    FROM "DotCompiledCodes" cc
    INNER JOIN "DotVehicleImages" vi2 ON vi2."DatECode" = cc."DatECode"
    WHERE cc."ManufacturerKey" = m."DatKey" 
        AND cc."BaseModelKey" = bm."DatKey"
        AND cc."IsActive" = true
        AND vi2."IsActive" = true
        AND vi2."Url" IS NOT NULL
        AND vi2."Url" != ''
        AND vi2."Aspect" IN ('SIDEVIEW', 'ANGULARFRONT')
    ORDER BY 
        CASE 
            WHEN vi2."Aspect" = 'SIDEVIEW' THEN 1 
            WHEN vi2."Aspect" = 'ANGULARFRONT' THEN 2 
            ELSE 3 
        END
    LIMIT 1
) vi ON true
WHERE m."IsActive" = true
GROUP BY m."Id", m."DatKey", m."Name", m."VehicleType", m."LogoUrl", m."Order"
ORDER BY m."Order", m."Name";
'''

pg_cursor.execute(sql)
columns = [desc[0] for desc in pg_cursor.description]
rows = pg_cursor.fetchall()

log_with_time(f"✅ Fetched {len(rows)} manufacturers with images", start)

# Index to Elasticsearch
actions = []
for row in rows:
    manufacturer = dict(zip(columns, row))
    
    # Calculate model count
    models = manufacturer.get("Models") or []
    manufacturer["ModelCount"] = len(models) if models and models != [None] else 0
    
    # Clean up None models
    if models == [None]:
        manufacturer["Models"] = []
    
    actions.append({
        "_index": "manufacturer_index",
        "_id": manufacturer["Id"],
        "_source": manufacturer
    })

try:
    start = time.time()
    success, errors = helpers.bulk(es, actions, raise_on_error=False)
    log_with_time(f"✅ Indexed {success} manufacturers to Elasticsearch", start)
    
    if errors:
        print(f"⚠️ {len(errors)} errors occurred during indexing")
        for err in errors[:3]:
            print(err)
except BulkIndexError as e:
    print(f"❌ Bulk index error: {len(e.errors)} documents failed")
    for err in e.errors[:3]:
        print(err)

# Close connections
pg_cursor.close()
pg_conn.close()
log_with_time("🎯 Manufacturer indexing completed with vehicle images!")
