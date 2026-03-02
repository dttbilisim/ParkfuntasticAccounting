import psycopg2
from psycopg2 import pool
from psycopg2.extras import execute_batch
import requests
import os
import concurrent.futures
from urllib.parse import urlparse
from datetime import datetime
import time
import random
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry
import warnings
import threading
from collections import deque

# Suppress urllib3 SSL warnings
warnings.filterwarnings("ignore", category=requests.packages.urllib3.exceptions.NotOpenSSLWarning)
warnings.filterwarnings("ignore", message=".*urllib3 v2 only supports OpenSSL 1.1.1+.*")

# --- CONFIGURATION ---
DB_HOST = "92.204.172.6"
DB_PORT = 5454
DB_NAME = "MarketPlace"
DB_USER = "myinsurer"
DB_PASS = "Posmdh0738"

# Path to original image storage (on local machine)
DOWNLOAD_DIR = "/Users/sezgin/Downloads/otoismail_images_new"
# Path to CDN root (registered in DB)
CDN_ROOT_PATH = r"C:\inetpub\wwwroot\cdn.yedeksen.com\images\ProductImages"
SELLER_ID = 1  # Oto Ismail
LIMIT_QUERY = None

# 🚀 SENIOR OPTIMIZATIONS
DOWNLOAD_WORKERS = 30        # Download concurrency
DB_BATCH_SIZE = 100          # Tek seferde kaç DB insert
DOWNLOAD_BATCH_SIZE = 100    # Kaç download sonrası DB'ye yaz
RATE_LIMIT_DELAY = 0.02      # Download'lar arası delay (20ms)

# --- STATUS TRACKING ---
stats_lock = threading.Lock()
stats = {
    'total': 0,
    'synced': 0,
    'on_disk': 0,
    'not_found': 0,
    'rate_limit': 0,
    'timeout': 0,
    'skipped': 0,
    'error': 0,
    'start_time': None
}

# 🎯 GLOBAL CACHE - Başlangıçta yüklenecek
existing_file_names = set()  # FileName'leri ProductImages'da olanlar
source_id_to_product_id = {}  # SourceId -> ProductId mapping cache
db_pool = None
pending_inserts = deque()     # Thread-safe queue for batch inserts
pending_inserts_lock = threading.Lock()

def init_db_pool():
    """Initialize PostgreSQL connection pool"""
    global db_pool
    db_pool = psycopg2.pool.ThreadedConnectionPool(
        minconn=2,
        maxconn=10,
        host=DB_HOST,
        port=DB_PORT,
        database=DB_NAME,
        user=DB_USER,
        password=DB_PASS
    )
    print("🔌 Database connection pool initialized (2-10 connections)")

def db_connect():
    return db_pool.getconn()

def db_release(conn):
    db_pool.putconn(conn)

def load_existing_file_names():
    """🚀 ULTRA FAST: Veritabanındaki tüm dosya isimlerini (harf/rakam karışık) RAM'e yükle (NetsisId BAZLI)"""
    global existing_file_names
    print("� Loading existing FileNames into memory (optimized)...")
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        # FIX: Regex kısıtlaması kaldırıldı, tüm dosya isimleri çekiliyor (ASM-23PE... gibi harfli isimler dahil)
        cur.execute('''
            SELECT DISTINCT "FileName" 
            FROM "ProductImages" 
            WHERE "FileName" IS NOT NULL
        ''')
        rows = cur.fetchall()
        existing_file_names = {row[0] for row in rows}
        cur.close()
        print(f"✅ Loaded {len(existing_file_names)} existing FileNames into cache")
    except Exception as e:
        print(f"❌ Failed to load FileNames: {e}")
        existing_file_names = set()
    finally:
        if conn:
            db_release(conn)

def get_products_to_sync():
    """Fetch target products - ProductOtoIsmails JOIN SellerItems ile ProductId direkt al (ULTRA FAST)"""
    print("� Fetching target products from ProductOtoIsmails (with ProductId)...")
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        # ProductId'yi direkt JOIN ile al - SellerItems'da olanlar için
        sql = """
            SELECT 
                poi."NetsisStokId",
                poi."ImageUrl",
                poi."Id" as "SourceId",
                si."ProductId"
            FROM "ProductOtoIsmails" poi
            INNER JOIN "SellerItems" si ON si."SourceId"::integer = poi."Id" AND si."SellerId" = %s
            WHERE poi."ImageUrl" != ''
              AND poi."ImageUrl" IS NOT NULL
              AND poi."ImageUrl" NOT IN ('None', 'null', 'NULL', 'nan', 'NaN')
              AND poi."NetsisStokId" IS NOT NULL
              AND si."ProductId" IS NOT NULL
            ORDER BY poi."Id"
        """
        if LIMIT_QUERY:
            sql += f' LIMIT {LIMIT_QUERY}'
        
        cur.execute(sql, (SELLER_ID,))
        items = cur.fetchall()
        cur.close()
        
        # RAM'de filtrele (cache'te olanları çıkar)
        filtered_items = []
        for netsis_stok_id, image_url, source_id, product_id in items:
            # Expected filename hesapla (NetsisStokId bazlı)
            ext = os.path.splitext(image_url)[1] or ".jpg"
            if '?' in ext: 
                ext = ext.split('?')[0]
            expected_filename = f"{netsis_stok_id}{ext}"
            
            # Cache kontrolü
            if expected_filename not in existing_file_names:
                filtered_items.append((netsis_stok_id, image_url, source_id, product_id))
        
        print(f"✅ Found {len(items)} products with ImageUrl, NetsisStokId and ProductId")
        print(f"🔥 {len(filtered_items)} products need sync (after cache filter)")
        return filtered_items
    except Exception as e:
        print(f"❌ DB Fetch Error: {e}")
        return []
    finally:
        if conn:
            db_release(conn)

def create_directory(path):
    if not os.path.exists(path):
        os.makedirs(path, exist_ok=True)

def create_session():
    """Create optimized HTTP session"""
    session = requests.Session()
    retry = Retry(
        total=2, 
        backoff_factor=0.2, 
        status_forcelist=[500, 502, 503, 504],
        allowed_methods=["GET"]
    )
    session.mount("http://", HTTPAdapter(max_retries=retry, pool_maxsize=50))
    session.mount("https://", HTTPAdapter(max_retries=retry, pool_maxsize=50))
    session.headers.update({
        "User-Agent": "Mozilla/5.0 (Mac) SeniorImageDownloader/3.1"
    })
    return session

def flush_pending_inserts(force=False):
    """🚀 Batch insert to database"""
    global existing_file_names
    
    with pending_inserts_lock:
        if len(pending_inserts) < DB_BATCH_SIZE and not force:
            return  
        
        if not pending_inserts:
            return
        
        batch = list(pending_inserts)
        pending_inserts.clear()
    
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        
        sql = """
            INSERT INTO "ProductImages" (
                "ProductId", "FileName", "FileGuid", "Root", 
                "Status", "CreatedDate", "CreatedId", "Order"
            ) VALUES (%s, %s, %s, %s, %s, NOW(), %s, %s)
            ON CONFLICT DO NOTHING
        """
        
        execute_batch(cur, sql, batch, page_size=100)
        conn.commit()
        cur.close()
        
        print(f"💾 DB BATCH INSERT: {len(batch)} records written")
        
        # Update global cache
        with stats_lock:
            for item in batch:
                existing_file_names.add(item[1])  # FileName 
        
    except Exception as e:
        print(f"❌ DB BATCH INSERT FAILED: {e}")
    finally:
        if conn:
            db_release(conn)

def process_item(item):
    """Process single download item - NetsisStokId (NetsizId) ile filename"""
    netsis_stok_id, image_url, source_id, product_id = item
    
    time.sleep(RATE_LIMIT_DELAY)
    
    # 0. URL Validation & CDN Check
    if not image_url or image_url.lower() in ('none', 'null', 'nan', ''):
        with stats_lock: stats['skipped'] += 1
        return None
        
    # Eğer URL zaten bizim CDN'i gösteriyorsa indirmeye gerek yok
    if "cdn.yedeksen.com" in image_url.lower():
        print(f"⏩ CDN [{netsis_stok_id}] - Already on our CDN")
        with stats_lock: stats['skipped'] += 1
        return (netsis_stok_id, "already_on_cdn")
    
    # 1. Filename Mapping (NetsisStokId bazlı)
    ext = os.path.splitext(image_url)[1] or ".jpg"
    if '?' in ext: 
        ext = ext.split('?')[0]
    filename = f"{netsis_stok_id}{ext}"
    filepath = os.path.join(DOWNLOAD_DIR, filename)
    
    # 2. Check if already on disk (but not in DB cache - cache check already done in get_products_to_sync)
    if os.path.exists(filepath):
        if product_id:
            with pending_inserts_lock:
                pending_inserts.append((
                    product_id, filename, filename, CDN_ROOT_PATH, 1, 1, 1
                ))
            print(f"📦 DIS [{netsis_stok_id}] - Local file exists, queued for DB")
            with stats_lock: stats['on_disk'] += 1
            return (netsis_stok_id, filename)
    
    # 3. Download
    try:
        session = create_session()
        response = session.get(image_url, timeout=10)
        
        if response.status_code == 200:
            with open(filepath, 'wb') as f:
                f.write(response.content)
            
            if product_id:
                with pending_inserts_lock:
                    pending_inserts.append((
                        product_id, filename, filename, CDN_ROOT_PATH, 1, 1, 1
                    ))
                
                print(f"✅ SYN [{netsis_stok_id}] - Downloaded & queued")
                with stats_lock: stats['synced'] += 1
                return (netsis_stok_id, filename)
            
        elif response.status_code == 429:
            print(f"🛑 429 [{netsis_stok_id}] - RATE LIMITED")
            with stats_lock: stats['rate_limit'] += 1
        elif response.status_code == 404:
            print(f"� 404 [{netsis_stok_id}] - Not Found")
            with stats_lock: stats['not_found'] += 1
        else:
            with stats_lock: stats['error'] += 1
            
    except Exception as e:
        with stats_lock: stats['error'] += 1
    
    return None

def main():
    print("\n" + "="*60)
    print("🚀 OtoIsmail Image Sync v3.1 - NetsisId Edition")
    print("="*60 + "\n")
    
    init_db_pool()
    create_directory(DOWNLOAD_DIR)
    
    # 🎯 STEP 1: Load ALL existing FileNames into RAM (NetsisId support)
    load_existing_file_names()
    
    # 🎯 STEP 2: Get products needing sync
    items = get_products_to_sync()
    if not items:
        print("✅ System in sync. No products to process.")
        if db_pool: db_pool.closeall()
        return
    
    total_count = len(items)
    stats['total'] = total_count
    stats['start_time'] = time.time()
    
    # 🎯 STEP 3: Process batches
    for i in range(0, total_count, DOWNLOAD_BATCH_SIZE):
        batch = items[i:i + DOWNLOAD_BATCH_SIZE]
        
        with concurrent.futures.ThreadPoolExecutor(max_workers=DOWNLOAD_WORKERS) as executor:
            list(executor.map(process_item, batch))
        
        flush_pending_inserts(force=True)
        
        processed = min(i + DOWNLOAD_BATCH_SIZE, total_count)
        elapsed = time.time() - stats['start_time']
        avg_speed = processed / elapsed if elapsed > 0 else 0
        
        print(f"📊 Progress: {(processed/total_count)*100:.1f}% ({processed}/{total_count}) - {avg_speed:.1f} items/sec")
    
    flush_pending_inserts(force=True)
    
    print("\n" + "="*60)
    print(f"🏁 SYNC COMPLETED in {(time.time() - stats['start_time'])/60:.1f} minutes")
    print(f"✅ Synced: {stats['synced']} | 📦 Disk: {stats['on_disk']} | 🚫 404: {stats['not_found']}")
    print("="*60 + "\n")
    
    if db_pool: db_pool.closeall()

if __name__ == "__main__":
    main()
