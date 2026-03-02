import time
import json
import psycopg2
from elasticsearch import Elasticsearch, helpers
from elasticsearch.helpers import BulkIndexError

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

INDEX_NAME = "manufacturer_index"
NESTED_LIMIT = 100000

# Delete existing index and create with new mapping
log_with_time("🚀 Manufacturer index kuruluyor...")
if es.indices.exists(index=INDEX_NAME):
    es.indices.delete(index=INDEX_NAME)

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
                    "ImageUrl": {"type": "keyword"},
                    "SubModels": {
                        "type": "nested",
                        "properties": {
                            "Id": {"type": "integer"},
                            "Name": {
                                "type": "text",
                                "fields": {
                                    "keyword": {"type": "keyword"}
                                }
                            },
                            "SubModelKey": {"type": "keyword"},
                            "ImageUrl": {"type": "keyword"}
                        }
                    }
                }
            }
        }
    }
}

es.indices.create(index=INDEX_NAME, body=mapping)
log_with_time("✅ Manufacturer index oluşturuldu")

# Set nested limit immediately after creation
es.indices.put_settings(
    index=INDEX_NAME,
    body={
        "index": {
            "mapping.nested_objects.limit": NESTED_LIMIT
        }
    }
)
log_with_time(f"🔧 Nested limit {NESTED_LIMIT} olarak ayarlandı")

# Fetch manufacturers with models + submodels + images
log_with_time("📦 Üretici + model + alt model verileri çekiliyor...")
start = time.time()

sql = '''
      SELECT
          m."Id",
          m."DatKey",
          m."Name",
          m."VehicleType",
          m."LogoUrl",
          m."Order",
          jsonb_agg(
                  jsonb_build_object(
                          'Id', bm."Id",
                          'Name', bm."Name",
                          'VehicleType', bm."VehicleType",
                          'ManufacturerKey', bm."ManufacturerKey",
                          'BaseModelKey', bm."DatKey",
                          'ImageUrl', bm."ImageUrl",
                          'SubModels', COALESCE(sm_data.sub_models, '[]'::jsonb)
                  )
                      ORDER BY bm."Name"
          ) FILTER (WHERE bm."Id" IS NOT NULL) AS "Models"
      FROM "DotManufacturers" m
               LEFT JOIN LATERAL (
          SELECT DISTINCT ON (b."Id")
          b."Id",
        b."DatKey",
        b."Name",
        b."VehicleType",
        b."ManufacturerKey",
        base_img."Url" AS "ImageUrl"
      FROM "DotBaseModels" b
          LEFT JOIN LATERAL (
          SELECT vi."Url"
          FROM "DotCompiledCodes" cc
          INNER JOIN "DotVehicleImages" vi ON vi."DatECode" = cc."DatECode"
          WHERE cc."ManufacturerKey" = m."DatKey"
          AND cc."BaseModelKey" = b."DatKey"
          AND cc."IsActive" = TRUE
          AND vi."IsActive" = TRUE
          AND vi."Url" IS NOT NULL
          AND vi."Url" <> ''
          AND vi."Aspect" IN ('SIDEVIEW', 'ANGULARFRONT')
          ORDER BY CASE WHEN vi."Aspect" = 'SIDEVIEW' THEN 1 ELSE 2 END
          LIMIT 1
          ) base_img ON TRUE
      WHERE b."IsActive" = TRUE
        AND b."ManufacturerKey" = m."DatKey"
          ) bm ON TRUE
          LEFT JOIN LATERAL (
          SELECT jsonb_agg(
          jsonb_build_object(
          'Id', sm."Id",
          'Name', sm."Name",
          'SubModelKey', sm."DatKey",
          'ImageUrl', sm_img."Url"
          )
          ORDER BY sm."Name"
          ) AS sub_models
          FROM "DotSubModels" sm
          LEFT JOIN LATERAL (
          SELECT vi."Url"
          FROM "DotCompiledCodes" cc
          INNER JOIN "DotVehicleImages" vi ON vi."DatECode" = cc."DatECode"
          WHERE cc."ManufacturerKey" = m."DatKey"
          AND cc."BaseModelKey" = bm."DatKey"
          AND cc."SubModelKey" = sm."DatKey"
          AND cc."IsActive" = TRUE
          AND vi."IsActive" = TRUE
          AND vi."Url" IS NOT NULL
          AND vi."Url" <> ''
          AND vi."Aspect" IN ('SIDEVIEW', 'ANGULARFRONT')
          ORDER BY CASE WHEN vi."Aspect" = 'SIDEVIEW' THEN 1 ELSE 2 END
          LIMIT 1
          ) sm_img ON TRUE
          WHERE sm."IsActive" = TRUE
          AND sm."ManufacturerKey" = m."DatKey"
          AND sm."BaseModelKey" = bm."DatKey"
          ) sm_data ON TRUE
      WHERE m."IsActive" = TRUE
      GROUP BY m."Id", m."DatKey", m."Name", m."VehicleType", m."LogoUrl", m."Order"
      ORDER BY m."Order", m."Name"; \
      '''

pg_cursor.execute(sql)
columns = [desc[0] for desc in pg_cursor.description]
rows = pg_cursor.fetchall()

log_with_time(f"✅ {len(rows)} üretici çekildi", start)

# Prepare bulk actions
actions = []
for row in rows:
    manufacturer = dict(zip(columns, row))

    models = manufacturer.get("Models") or []
    clean_models = []

    for model in models:
        if not model:
            continue

        submodels = model.get("SubModels") or []
        clean_submodels = [
            {
                "Id": sm.get("Id"),
                "Name": sm.get("Name"),
                "SubModelKey": sm.get("SubModelKey"),
                "ImageUrl": sm.get("ImageUrl")
            }
            for sm in submodels if sm and sm.get("Id")
        ]

        clean_models.append({
            "Id": model.get("Id"),
            "Name": model.get("Name"),
            "VehicleType": model.get("VehicleType"),
            "ManufacturerKey": model.get("ManufacturerKey"),
            "BaseModelKey": model.get("BaseModelKey"),
            "ImageUrl": model.get("ImageUrl"),
            "SubModels": clean_submodels
        })

    manufacturer["Models"] = clean_models
    manufacturer["ModelCount"] = len(clean_models)

    actions.append({
        "_index": INDEX_NAME,
        "_id": manufacturer["Id"],
        "_source": manufacturer
    })

# Bulk index
try:
    start = time.time()
    success, errors = helpers.bulk(es, actions, raise_on_error=False)
    log_with_time(f"✅ {success} üretici Elasticsearch'e yazıldı", start)

    if errors:
        print(f"⚠️ {len(errors)} hata oluştu, örnek ilk 3:")
        for err in errors[:3]:
            print(err)
except BulkIndexError as e:
    print(f"❌ Bulk index hatası: {len(e.errors)} doküman yazılamadı")
    for err in e.errors[:3]:
        print(err)

# Close connections
pg_cursor.close()
pg_conn.close()
log_with_time("🎯 Manufacturer indexing tamamlandı (modeller + alt modeller)!") 