#!/usr/bin/env python3
"""
ELASTICSEARCH VIN CACHE POPULATOR V3
Streaming approach: Process models one-by-one with real-time progress
"""
import sys
import psycopg2
import psycopg2.extras
import json
import time
from datetime import datetime
from elasticsearch import Elasticsearch, helpers

# Force unbuffered output
sys.stdout = open(sys.stdout.fileno(), 'w', buffering=1)
sys.stderr = open(sys.stderr.fileno(), 'w', buffering=1)

DB_CONFIG = {
    'host': '92.204.172.6',
    'port': 5454,
    'database': 'MarketPlace',
    'user': 'myinsurer',
    'password': 'Posmdh0738'
}

ES_CONFIG = {
    'hosts': ['http://92.204.172.6:9200'],
    'basic_auth': ('elastic', 'itO5M3EZrbc96K_42ah3'),
    'request_timeout': 60
}

INDEX_NAME = 'vin_index'

# COMPLETE WMI MAPPING
WMI_MAPPING = {
    'WAU': 'Audi', 'TRU': 'Audi', 'WBA': 'BMW', 'WBS': 'BMW', 'WBY': 'BMW',
    'WDB': 'Mercedes-Benz', 'WDC': 'Mercedes-Benz', 'WDD': 'Mercedes-Benz', 'WDF': 'Mercedes-Benz',
    'WP0': 'Porsche', 'WP1': 'Porsche', 'WVW': 'Volkswagen', 'WV1': 'Volkswagen', 'WV2': 'Volkswagen', '3VW': 'Volkswagen',
    'W0L': 'Opel', 'W0V': 'Opel', 'WME': 'Smart', 'WMW': 'MINI', 'WF0': 'Ford', 'W09': 'Abarth',
    'ZFF': 'Ferrari', 'ZAM': 'Maserati', 'ZLA': 'Lancia', 'ZCF': 'Iveco', 'WMA': 'MAN',
    'WKK': 'DAF', 'YS2': 'Scania', 'YS3': 'Saab', 'NMT': 'Neoplan', 'VSE': 'Kässbohrer/Setra',
    'VF1': 'Renault', 'VF3': 'Peugeot', 'VF7': 'Citroen', 'VF8': 'Alpine', 'VF9': 'DS',
    'UU1': 'Dacia', 'UU3': 'Dacia', 'ZFA': 'Fiat', 'NM0': 'Fiat', 'NM4': 'Fiat', 'ZAR': 'Alfa Romeo',
    'ZD4': 'Aprilia', 'ZD3': 'Aprilia', 'ZDM': 'Ducati', 'ZD0': 'Piaggio (Vespa)', 'ZD1': 'Piaggio (Vespa)',
    'ZD6': 'Gilera', 'ZD7': 'Cagiva', 'ZDC': 'Benelli', 'ZD9': 'MV Agusta', 'ZDL': 'Moto-Guzzi',
    'ZDN': 'Laverda', 'ZDP': 'Malaguti', 'ZDR': 'Italjet', 'ZDT': 'Moto-Morini', 'ZDU': 'Fantic',
    'ZDV': 'Derbi', 'ZHW': 'Lamborghini', 'JT2': 'Toyota', 'JTD': 'Toyota', 'JTE': 'Toyota',
    '2T1': 'Toyota', '4T1': 'Toyota', '5T': 'Toyota', 'JHM': 'Honda', '1HG': 'Honda', '2HG': 'Honda', 'SHH': 'Honda',
    'JN1': 'Nissan', 'JN3': 'Nissan', '1N4': 'Nissan', '5N1': 'Nissan', 'JM1': 'Mazda', 'JM3': 'Mazda', '4F2': 'Mazda',
    'JF1': 'Subaru', 'JF2': 'Subaru', '4S3': 'Subaru', 'JS1': 'Suzuki', 'JS2': 'Suzuki', 'JS3': 'Suzuki',
    'JA3': 'Mitsubishi', 'JA4': 'Mitsubishi', '4A3': 'Mitsubishi', 'JYA': 'Yamaha', 'JY4': 'Yamaha',
    'JKA': 'Kawasaki', 'JKB': 'Kawasaki', 'JTN': 'Lexus', '2T2': 'Lexus', 'JDA': 'Daihatsu',
    'JUB': 'Isuzu', '4S6': 'Isuzu', 'JN6': 'Infiniti', '5N3': 'Infiniti',
    'KMH': 'Hyundai', 'KM8': 'Hyundai', '5NP': 'Hyundai', 'KNA': 'Kia', 'KND': 'Kia', '5XX': 'Kia',
    'KPT': 'Ssangyong', 'KPA': 'Ssangyong', 'KL1': 'Daewoo', 'KLA': 'Daewoo', 'KMC': 'Hyosung',
    'KRF': 'SYM', 'KPW': 'KG Mobility', 'KL5': 'Genesis', 'KD1': 'Daelim', 'KD2': 'Daelim',
    '1FA': 'Ford', '1FB': 'Ford', '1FC': 'Ford', '1FD': 'Ford', '1FM': 'Ford',
    '1G1': 'Chevrolet', '1GC': 'Chevrolet', '1G2': 'Chevrolet', '1B3': 'Dodge', '1B4': 'Dodge', '1B7': 'Dodge',
    '1C3': 'Chrysler', '1C4': 'Chrysler', '1C6': 'Chrysler', '1J4': 'Jeep', '1J8': 'Jeep',
    '5YJ': 'Tesla', '7G2': 'Tesla', 'XP7': 'Tesla', '1G6': 'Cadillac', '1GY': 'Cadillac',
    '2G1': 'Pontiac', '2G2': 'Pontiac', '4US': 'Buick', '5GA': 'Buick', '1YV': 'Corvette',
    '4HD': 'Harley-Davidson', '1HD': 'Harley-Davidson', '5UM': 'Buell', '93H': 'Fisker',
    'SAL': 'Land Rover', 'SAH': 'Land Rover', 'SAJ': 'Jaguar', 'SCC': 'Lotus',
    'SAR': 'Rolls Royce', 'SCA': 'Rolls Royce', 'SAX': 'Bentley', 'SCF': 'Aston Martin',
    'SB1': 'Caterham', 'SBM': 'Westfield', 'SX1': 'LEVC', 'SCB': 'Triumph',
    'VSS': 'Seat', 'VS6': 'Seat', 'VR1': 'Cupra', 'VTL': 'Derbi', 'VTR': 'Sanglas',
    'TMB': 'Skoda', 'TMK': 'Skoda', 'TMA': 'Skoda', 'YV1': 'Volvo', 'YV2': 'Volvo', 'YV3': 'Volvo',
    'LP5': 'Polestar', 'LPS': 'Polestar', 'LVS': 'Aiways', 'LBV': 'BYD', 'LGB': 'BYD',
    'LGX': 'Great Wall Motors (GWM)', 'LGW': 'Great Wall Motors (GWM)', 'LNB': 'NIO',
    'LPL': 'Brilliance', 'LVV': 'Lynk & Co', 'LVG': 'Vinfast', 'LFV': 'Ora', 'LZZ': 'Lucid',
    'LKL': 'Micro', 'LVT': 'Silence', 'VBK': 'KTM', 'VB1': 'KTM', 'VBE': 'Puch',
    'VBM': 'Hercules', 'VBL': 'Kreidler', 'VBT': 'Tornax', 'LKM': 'Kymco', 'LKS': 'SYM', 'LKP': 'PGO',
    'PE1': 'Proton', 'MAT': 'Tata', 'XTA': 'Lada', 'MA3': 'Lada', 'XL9': 'DAF', 'VF6': 'Bova',
    'UU5': 'Trabant', 'UU7': 'Wartburg', 'UU9': 'Barkas', 'WMX': 'MZ', 'WVZ': 'Simson',
    'TMJ': 'Jawa', 'VX1': 'Zastava', 'ZD2': 'Sachs Bikes', 'ZD8': 'MBK', 'ZDK': 'Maico',
    'ZDS': 'Solo', 'SZM': 'INEOS',
}

def log(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}", flush=True)

def extract_vds(name):
    import re
    m = re.search(r'\(([A-Z0-9]{2,4})\)', name)
    return m.group(1) if m else None

def create_index(es):
    """Create Elasticsearch index"""
    log("📋 Creating Elasticsearch index...")
    
    mapping = {
        "settings": {
            "number_of_shards": 2, 
            "number_of_replicas": 1,
            "index.mapping.nested_objects.limit": 50000  # Increase nested limit
        },
        "mappings": {
            "properties": {
                "wmi": {"type": "keyword"},
                "manufacturer_key": {"type": "keyword"},
                "manufacturer_name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
                "base_model_key": {"type": "keyword"},
                "base_model_name": {"type": "text", "fields": {"keyword": {"type": "keyword"}}},
                "sub_model_key": {"type": "keyword"},
                "sub_model_name": {"type": "text"},
                "year_from": {"type": "integer"},
                "year_to": {"type": "integer"},
                "vds_code": {"type": "keyword"},
                "dot_ecode": {"type": "keyword"},
                "dat_process_number": {"type": "keyword"},
                "search_keywords": {"type": "text"},
                "oem_parts": {
                    "type": "object",  # Changed from nested to object (no limit)
                    "properties": {
                        "oem": {"type": "keyword"}, 
                        "name": {"type": "text"}
                    }
                },
                "created_date": {"type": "date"}
            }
        }
    }
    
    if es.indices.exists(index=INDEX_NAME):
        log(f"⚠️  Index '{INDEX_NAME}' exists, deleting...")
        es.indices.delete(index=INDEX_NAME)
    
    es.indices.create(index=INDEX_NAME, body=mapping)
    log(f"✅ Index '{INDEX_NAME}' created!")

def streaming_insert(es, conn):
    """REAL-TIME: Insert each model immediately to Elasticsearch"""
    log("📊 Fetching models list...")
    
    cur = conn.cursor()
    cur.execute("""
        SELECT bm."DatKey", bm."Name", bm."ManufacturerKey", dm."Name"
        FROM "DotBaseModels" bm
        JOIN "DotManufacturers" dm ON dm."DatKey" = bm."ManufacturerKey"
        WHERE bm."IsActive" = true AND dm."IsActive" = true 
          AND dm."Name" != '-> Üretici giriniz <-'
        ORDER BY bm."DatKey"
    """)
    models = cur.fetchall()
    total = len(models)
    log(f"✅ Got {total} models")
    log("⚡ Starting REAL-TIME insert (immediate ES write)...")
    
    inserted = 0
    skipped = 0
    start_time = time.time()
    
    for idx, (bkey, bname, mkey, mname) in enumerate(models, 1):
        try:
            wmis = [w for w, m in WMI_MAPPING.items() if m == mname]
            if not wmis:
                skipped += 1
                if idx % 100 == 0:
                    log(f"⚠️  Skipped {bname} - no WMI mapping")
                continue
            
            # Fetch OEM parts
            cur.execute("""
                SELECT "PartNumber", "Description"
                FROM "DotParts"
                WHERE "BaseModelKey" = %s AND "IsActive" = true AND "PartNumber" IS NOT NULL
            """, (bkey,))
            parts = cur.fetchall()
            oem_parts = [{"oem": p[0], "name": p[1]} for p in parts]
            
            vds = extract_vds(bname)
            kw = f"{bname} {mname}".lower()
            
            # Insert IMMEDIATELY for each WMI
            for wmi in wmis:
                doc = {
                    "wmi": wmi,
                    "manufacturer_key": mkey,
                    "manufacturer_name": mname,
                    "base_model_key": bkey,
                    "base_model_name": bname,
                    "sub_model_key": None,
                    "sub_model_name": None,
                    "year_from": 1980,
                    "year_to": 2026,
                    "vds_code": vds,
                    "dot_ecode": None,
                    "dat_process_number": None,
                    "search_keywords": kw,
                    "oem_parts": oem_parts,
                    "created_date": datetime.now().isoformat()
                }
                
                # IMMEDIATE INSERT
                es.index(index=INDEX_NAME, document=doc)
                inserted += 1
            
            # Real-time progress every 25 models
            if idx % 25 == 0:
                elapsed = time.time() - start_time
                rate = idx / elapsed
                eta = (total - idx) / rate if rate > 0 else 0
                pct = idx * 100 // total
                log(f"⚡ {idx}/{total} ({pct}%) | ES Docs: {inserted:,} | Skip: {skipped} | "
                    f"{rate:.1f} models/s | Parts: {len(oem_parts)} | ETA: {int(eta//60)}m{int(eta%60)}s")
                
        except Exception as e:
            log(f"❌ {bname}: {e}")
            conn.rollback()
            skipped += 1
            continue
    
    log(f"✅ Inserted {inserted:,} docs to ES, {skipped} skipped")
    cur.close()

def main():
    log("🚀 ELASTICSEARCH VIN POPULATOR V3 (STREAMING)")
    log("="*60)
    
    log("🔌 Connecting to Elasticsearch...")
    es = Elasticsearch(**ES_CONFIG)
    if not es.ping():
        log("❌ Cannot connect to Elasticsearch!")
        return
    log("✅ Connected to Elasticsearch")
    
    log("🔌 Connecting to PostgreSQL...")
    conn = psycopg2.connect(**DB_CONFIG)
    log("✅ Connected to PostgreSQL")
    
    try:
        create_index(es)
        streaming_insert(es, conn)
        
        log("="*60)
        es.indices.refresh(index=INDEX_NAME)
        count = es.count(index=INDEX_NAME)['count']
        log(f"📊 Final ES document count: {count:,}")
        log("="*60)
        
    finally:
        conn.close()
        log("✅ COMPLETED!")

if __name__ == '__main__':
    start = time.time()
    main()
    log(f"⏱️  Total: {time.time()-start:.1f}s")
