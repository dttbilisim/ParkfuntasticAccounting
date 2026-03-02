import os
import re
import psycopg2
from psycopg2.extras import execute_batch
from datetime import datetime

# Configuration
DB_CFG = {
    "host": "92.204.172.6",
    "port": 5454,
    "database": "MarketPlace",
    "user": "myinsurer",
    "password": "Posmdh0738"
}

# General ProductImages path (same as manage_images.py)
BASE_ROOT = r"C:\inetpub\wwwroot\cdn.yedeksen.com\images\ProductImages"
IMAGE_EXTENSIONS = {".jpg", ".png", ".jpeg", ".webp", ".bmp"}

def normalize_token(token):
    """
    Cleaner normalization:
    - Uppercase
    - Remove non-alphanumeric characters (A-Z, 0-9)
    - This handles 2.7-3.0 -> 2730, but mostly we want to match exact codes like '4F0145806E'
    """
    if not token:
        return ""
    # Remove all non-alphanumeric characters
    return re.sub(r'[^A-Z0-9]', '', str(token).upper())

def split_complex_oems(text):
    """
    Splits Oems string by |, -, ;, ,, /, \, space, and newlines.
    """
    if not text:
        return []
    # Split by common delimiters including dash (-)
    tokens = re.split(r"[|;\-,/\\\s]+", str(text))
    
    # Filter out empty or too short tokens (e.g. '04', 'A6' might be dangerously short, but let's keep it safe)
    # 3 characters seems like a safe minimum for a distinct part number to avoid false positives with versions like 'V1'
    return [t for t in tokens if len(t.strip()) >= 3]

def scan_images():
    """
    Returns a map of { NormalizedName: FileName }
    """
    image_map = {}
    physical_files = set()
    
    if not os.path.exists(BASE_ROOT):
        print(f"⚠️ Warning: Directory not found: {BASE_ROOT}")
        return image_map, physical_files

    print("📂 Scanning disk for images...")
    # Walk is not needed if all images are in the root of BASE_ROOT
    files = os.listdir(BASE_ROOT)
    for f in files:
        name, ext = os.path.splitext(f)
        if ext.lower() in IMAGE_EXTENSIONS:
            norm = normalize_token(name)
            if norm:
                image_map[norm] = f
                physical_files.add(f)
            
    return image_map, physical_files

def main():
    print("🚀 Starting GENERAL Image Insert Script (all_image_insert.py)")
    
    image_map, physical_files = scan_images()
    print(f"🖼 Found {len(physical_files)} physical images on disk.")
    
    if not image_map:
        print("❌ No images found on disk. Exiting.")
        return

    conn = psycopg2.connect(**DB_CFG)
    cur = conn.cursor()
    
    try:
        # 1. Fetch ALL Products with Oems
        print("📦 Fetching ALL products with Oems from DB...")
        cur.execute("""
            SELECT "Id", "Oems"
            FROM "Product"
            WHERE "Oems" IS NOT NULL AND "Oems" != ''
        """)
        products = cur.fetchall()
        print(f"✅ Loaded {len(products)} products.")

        # 2. Fetch Existing ProductImages (to avoid duplicates)
        # We need to check duplicates globally or per product
        # Ideally, we load all existing matches to skip them
        print("📑 Fetching existing ProductImages...")
        cur.execute('SELECT "ProductId", "FileName" FROM "ProductImages"')
        existing_images = set()
        for pid, fname in cur.fetchall():
            existing_images.add((pid, fname))
        print(f"✅ Found {len(existing_images)} existing image records.")

        # 3. Match Logic
        insert_batch = []
        matched_count = 0
        
        print("🔄 Matching products to images...")
        
        for product_id, oems_raw in products:
            tokens = split_complex_oems(oems_raw)
            
            # Find all matching files for this product
            matches_for_product = set()
            
            for token in tokens:
                norm = normalize_token(token)
                if norm in image_map:
                    filename = image_map[norm]
                    matches_for_product.add(filename)
            
            # Prepare inserts
            for fname in matches_for_product:
                if (product_id, fname) not in existing_images:
                    insert_batch.append((
                        product_id,
                        fname, # FileGuid (using filename)
                        fname, # FileName
                        1,     # Status
                        1,     # CreatedId
                        datetime.now(),
                        BASE_ROOT, # Root
                        1      # Order
                    ))
                    # Add to existing to prevent duplicate valid matches in same run?
                    # The set handles unique filenames for this product already.
        
        if insert_batch:
            print(f"🟢 Inserting {len(insert_batch)} new images...")
            
            insert_query = """
                INSERT INTO "ProductImages"
                ("ProductId","FileGuid","FileName","Status",
                 "CreatedId","CreatedDate","Root","Order")
                VALUES (%s,%s,%s,%s,%s,%s,%s,%s)
            """
            
            # Execute in chunks to avoid memory issues if huge
            batch_size = 5000
            total_inserted = 0
            
            for i in range(0, len(insert_batch), batch_size):
                chunk = insert_batch[i:i + batch_size]
                execute_batch(cur, insert_query, chunk)
                conn.commit()
                total_inserted += len(chunk)
                print(f"   ... Inserted {total_inserted}/{len(insert_batch)}")
                
            print("✅ All inserts completed.")
        else:
            print("ℹ️ No new matches found to insert.")

    except Exception as e:
        print(f"❌ Error: {e}")
        conn.rollback()
    finally:
        cur.close()
        conn.close()
        print("🏁 Script finished.")

if __name__ == "__main__":
    main()
