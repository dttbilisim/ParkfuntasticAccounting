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

DOWNLOAD_DIR = "/Users/sezgin/Downloads/otoismail_images"
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
existing_product_ids = set()  # ProductId'leri ProductImages'da olanlar
db_pool = None
pending_inserts = deque()     # Thread-safe queue for batch inserts
pending_inserts_lock = threading.Lock()

def init_db_pool():
    """Initialize PostgreSQL connection pool"""
    global db_pool
    db_pool = psycopg2.pool.ThreadedConnectionPool(
        minconn=2,
        maxconn=10,  # Azaltıldı çünkü batch insert yapacağız
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

def load_existing_product_ids():
    """🚀 MEGA OPTIMIZATION: Tüm mevcut ProductId'leri tek seferde RAM'e yükle"""
    global existing_product_ids
    print("📥 Loading existing ProductIds into memory...")
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        cur.execute('SELECT DISTINCT "ProductId" FROM "ProductImages"')
        rows = cur.fetchall()
        existing_product_ids = {row[0] for row in rows}
        cur.close()
        print(f"✅ Loaded {len(existing_product_ids)} existing ProductIds into cache")
    except Exception as e:
        print(f"❌ Failed to load ProductIds: {e}")
        existing_product_ids = set()
    finally:
        if conn:
            db_release(conn)

def get_products_to_sync():
    """Fetch target products with optimized query"""
    print("🔍 Fetching target products...")
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        # 🎯 NetsisStokId ekledik
        sql = f"""
            SELECT DISTINCT ON (si."ProductId") 
                si."Id", 
                si."ProductId", 
                si."SourceId",
                poi."ImageUrl",
                poi."NetsisStokId"
            FROM "SellerItems" si
            INNER JOIN "ProductOtoIsmails" poi ON si."SourceId"::integer = poi."Id"
            WHERE si."SellerId" = {SELLER_ID} 
              AND si."Status" = 1
              AND poi."ImageUrl" IS NOT NULL
              AND poi."ImageUrl" NOT IN ('None', 'null', 'NULL', 'nan', 'NaN', '')
            ORDER BY si."ProductId", si."Id"
        """
        if LIMIT_QUERY:
            sql += f' LIMIT {LIMIT_QUERY}'
        
        cur.execute(sql)
        items = cur.fetchall()
        cur.close()
        
        # 🎯 RAM'deki cache'e göre filtrele
        filtered_items = [
            item for item in items 
            if item[1] not in existing_product_ids  # ProductId check
        ]
        
        print(f"✅ Found {len(items)} total products")
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
        "User-Agent": "Mozilla/5.0 (Mac) SeniorImageDownloader/3.0"
    })
    return session

def flush_pending_inserts(force=False):
    """🚀 Batch insert to database"""
    global pending_inserts
    
    with pending_inserts_lock:
        if len(pending_inserts) < DB_BATCH_SIZE and not force:
            return  # Wait for more records
        
        if not pending_inserts:
            return
        
        # Convert deque to list for processing
        batch = list(pending_inserts)
        pending_inserts.clear()
    
    # Perform batch insert
    conn = None
    try:
        conn = db_connect()
        cur = conn.cursor()
        
        sql = """
            INSERT INTO "ProductImages" (
                "ProductId", "FileName", "FileGuid", "Root", 
                "Order", "Status", "CreatedDate", "CreatedId"
            ) VALUES (%s, %s, %s, %s, %s, %s, NOW(), %s)
            ON CONFLICT DO NOTHING
        """
        
        execute_batch(cur, sql, batch, page_size=100)
        conn.commit()
        cur.close()
        
        print(f"💾 DB BATCH INSERT: {len(batch)} records written")
        
        # Update global cache
        with stats_lock:
            for item in batch:
                existing_product_ids.add(item[0])  # ProductId
        
    except Exception as e:
        print(f"❌ DB BATCH INSERT FAILED: {e}")
    finally:
        if conn:
            db_release(conn)

def process_item(item):
    """Process single download item"""
    # 🎯 NetsisStokId'yi de aldık
    seller_item_id, product_id, source_id, image_url, netsis_stok_id = item
    
    # Rate limiting
    time.sleep(RATE_LIMIT_DELAY)
    
    # 0. URL Validation
    if not image_url or image_url.lower() in ('none', 'null', 'nan', ''):
        print(f"⏩ SKP [{product_id}] - Invalid URL")
        with stats_lock: stats['skipped'] += 1
        return None
    
    # 1. File Extension & Base Name
    ext = os.path.splitext(image_url)[1] or ".jpg"
    if '?' in ext: 
        ext = ext.split('?')[0]
    
    # 🎯 NetsisStokId varsa onu, yoksa ProductId'yi kullan
    base_name = str(netsis_stok_id).strip() if netsis_stok_id else str(product_id)
    filename = f"{base_name}{ext}"
    filepath = os.path.join(DOWNLOAD_DIR, filename)
    
    # 2. Check if already on disk
    if os.path.exists(filepath):
        # Add to pending inserts (will be batch inserted)
        with pending_inserts_lock:
            pending_inserts.append((
                product_id, filename, filename, CDN_ROOT_PATH, 1, 1, 1
            ))
        print(f"📦 DIS [{product_id}] - Local file exists ({filename}), queued for DB")
        with stats_lock: stats['on_disk'] += 1
        return (product_id, filename)
    
    # 3. Download
    try:
        session = create_session()
        response = session.get(image_url, timeout=8)
        
        if response.status_code == 200:
            # Write to disk
            with open(filepath, 'wb') as f:
                f.write(response.content)
            
            # Queue for batch insert
            with pending_inserts_lock:
                pending_inserts.append((
                    product_id, filename, filename, CDN_ROOT_PATH, 1, 1, 1
                ))
            
            print(f"✅ SYN [{product_id}] - Downloaded as {filename} & queued")
            with stats_lock: stats['synced'] += 1
            return (product_id, filename)
            
        elif response.status_code == 429:
            print(f"🛑 429 [{product_id}] - RATE LIMITED")
            with stats_lock: stats['rate_limit'] += 1
        elif response.status_code == 404:
            print(f"🚫 404 [{product_id}] - Not Found")
            with stats_lock: stats['not_found'] += 1
        else:
            print(f"❌ {response.status_code} [{product_id}]")
            with stats_lock: stats['error'] += 1
            
    except requests.exceptions.Timeout:
        print(f"⚠️  TMO [{product_id}] - Timeout")
        with stats_lock: stats['timeout'] += 1
    except Exception as e:
        print(f"❌ ERR [{product_id}] - {str(e)[:50]}")
        with stats_lock: stats['error'] += 1
    
    return None

def main():
    print("\n" + "="*60)
    print("🚀 OtoIsmail Image Sync v3.0 - NETSIS EDITION")
    print("="*60 + "\n")
    
    # Initialize
    init_db_pool()
    create_directory(DOWNLOAD_DIR)
    
    # 🎯 STEP 1: Load existing ProductIds into RAM
    load_existing_product_ids()
    
    # 🎯 STEP 2: Get products to sync (already filtered by RAM cache)
    items = get_products_to_sync()
    if not items:
        print("✅ System in sync. No products to process.")
        if db_pool:
            db_pool.closeall()
        return
    
    total_count = len(items)
    stats['total'] = total_count
    stats['start_time'] = time.time()
    
    print(f"\n📊 CONFIGURATION")
    print(f"├─ Total Targets: {total_count}")
    print(f"├─ Download Workers: {DOWNLOAD_WORKERS}")
    print(f"├─ DB Batch Size: {DB_BATCH_SIZE}")
    print(f"├─ Download Batch: {DOWNLOAD_BATCH_SIZE}")
    print(f"└─ Rate Limit Delay: {RATE_LIMIT_DELAY}s\n")
    
    # 🎯 STEP 3: Process downloads in batches
    for i in range(0, total_count, DOWNLOAD_BATCH_SIZE):
        batch = items[i:i + DOWNLOAD_BATCH_SIZE]
        batch_num = (i // DOWNLOAD_BATCH_SIZE) + 1
        total_batches = (total_count + DOWNLOAD_BATCH_SIZE - 1) // DOWNLOAD_BATCH_SIZE
        
        # Download batch concurrently
        with concurrent.futures.ThreadPoolExecutor(max_workers=DOWNLOAD_WORKERS) as executor:
            results = list(executor.map(process_item, batch))
        
        # Flush DB inserts for this batch
        flush_pending_inserts(force=True)
        
        # Progress Report
        processed = min(i + DOWNLOAD_BATCH_SIZE, total_count)
        remaining = total_count - processed
        elapsed = time.time() - stats['start_time']
        
        if processed > 0:
            avg_speed = processed / elapsed
            eta_sec = remaining / avg_speed if avg_speed > 0 else 0
            eta_min = eta_sec / 60
        else:
            eta_min = 0
        
        print("\n" + "─"*60)
        print(f"📦 BATCH {batch_num}/{total_batches} COMPLETED")
        print(f"├─ Progress: {(processed/total_count)*100:.1f}% ({processed}/{total_count})")
        print(f"├─ Remaining: {remaining} | ETA: {eta_min:.1f}m")
        print(f"├─ Speed: {avg_speed:.1f} items/sec")
        with stats_lock:
            print(f"├─ ✅ Synced: {stats['synced']} | 📦 Disk: {stats['on_disk']}")
            print(f"├─ 🚫 404: {stats['not_found']} | 🛑 429: {stats['rate_limit']}")
            print(f"└─ ⚠️  TMO: {stats['timeout']} | ⏩ Skip: {stats['skipped']} | ❌ Err: {stats['error']}")
        print("─"*60 + "\n")
    
    # 🎯 STEP 4: Final flush (safety)
    flush_pending_inserts(force=True)
    
    # Final Report
    total_time = (time.time() - stats['start_time']) / 60
    total_success = stats['synced'] + stats['on_disk']
    
    print("\n" + "="*60)
    print(f"🏁 SYNC COMPLETED - {total_time:.1f} minutes")
    print("="*60)
    print(f"✅ Successfully Synced:  {stats['synced']}")
    print(f"📦 Already on Disk:      {stats['on_disk']}")
    print(f"🎯 Total Success:        {total_success} / {total_count}")
    print(f"🚫 Not Found (404):      {stats['not_found']}")
    print(f"🛑 Rate Limited (429):   {stats['rate_limit']}")
    print(f"⚠️  Timeouts:             {stats['timeout']}")
    print(f"⏩ Skipped (Invalid URL): {stats['skipped']}")
    print(f"❌ Errors:               {stats['error']}")
    print(f"⚡ Avg Speed:            {total_count/((time.time() - stats['start_time']) or 1):.1f} items/sec")
    print("="*60 + "\n")
    
    # Cleanup
    if db_pool:
        db_pool.closeall()
        print("🔌 Database connection pool closed.\n")

if __name__ == "__main__":
    main()
