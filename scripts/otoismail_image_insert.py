import os
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

# Image Directory for OtoIsmail (Seller 1)
# Assuming a standard path or specific folder. User can adjust.
BASE_ROOT = r"C:\inetpub\wwwroot\cdn.yedeksen.com\images\ProductImages\otoismail"
IMAGE_EXTENSIONS = {".jpg", ".png", ".jpeg", ".webp", ".bmp"}

def scan_images():
    """
    Scans the directory and returns a map of {NetsisStokId (str): FileName}
    Assumes filenames are like '12345.jpg' where 12345 is the NetsisStokId.
    """
    image_map = {}
    physical_files = set()
    
    if not os.path.exists(BASE_ROOT):
        print(f"⚠️ Warning: Directory not found: {BASE_ROOT}")
        return image_map, physical_files

    print("📂 Scanning disk for images...")
    files = os.listdir(BASE_ROOT)
    for f in files:
        name, ext = os.path.splitext(f)
        if ext.lower() in IMAGE_EXTENSIONS:
            # We assume the filename (without extension) IS the NetsisStokId
            # We strip whitespace just in case
            key = name.strip()
            image_map[key] = f
            physical_files.add(f)
            
    return image_map, physical_files

def main():
    print("🚀 Starting OtoIsmail Image Insert Script")
    
    image_map, physical_files = scan_images()
    print(f"🖼 Found {len(physical_files)} physical images on disk.")
    
    if not image_map:
        print("❌ No images found. Exiting.")
        return

    conn = psycopg2.connect(**DB_CFG)
    cur = conn.cursor()
    
    try:
        # 1. Fetch Mapping: ProductId <-> NetsisStokId
        # We join Product -> SellerItems -> ProductOtoIsmails
        # SellerId = 1 for OtoIsmail
        print("📦 Fetching Product <-> NetsisStokId mapping from DB...")
        
        query_mapping = """
            SELECT 
                p."Id" as "ProductId",
                CAST(poi."NetsisStokId" AS TEXT) as "NetsisStokId"
            FROM "Product" p
            JOIN "SellerItems" si ON si."ProductId" = p."Id"
            JOIN "ProductOtoIsmails" poi ON si."SourceId" = CAST(poi."Id" AS TEXT)
            WHERE p."SellerId" = 1
              AND si."SellerId" = 1
              AND poi."NetsisStokId" IS NOT NULL
        """
        cur.execute(query_mapping)
        products = cur.fetchall()
        print(f"✅ Loaded {len(products)} products with NetsisStokId.")

        # 2. Fetch Existing Images to avoid duplicates
        # We only care about images for these products that we might insert
        # To be safe, we can fetch all images for Seller 1 products
        print("📑 Fetching existing ProductImages for Seller 1...")
        query_existing = """
            SELECT pi."ProductId", pi."FileName"
            FROM "ProductImages" pi
            JOIN "Product" p ON p."Id" = pi."ProductId"
            WHERE p."SellerId" = 1
        """
        cur.execute(query_existing)
        existing_images = set()
        for pid, fname in cur.fetchall():
            existing_images.add((pid, fname))
            
        print(f"✅ Found {len(existing_images)} existing image records.")

        # 3. Match and Prepare Inserts
        insert_batch = []
        
        print("🔄 Matching images...")
        for product_id, netsis_stok_id in products:
            if netsis_stok_id in image_map:
                file_name = image_map[netsis_stok_id]
                
                # Check duplicate
                if (product_id, file_name) in existing_images:
                    continue
                
                insert_batch.append((
                    product_id,
                    file_name, # FileGuid (using filename for now)
                    file_name, # FileName
                    1,         # Status
                    1,         # CreatedId
                    datetime.now(),
                    BASE_ROOT, # Root
                    1          # Order
                ))

        if insert_batch:
            print(f"🟢 Inserting {len(insert_batch)} new images...")
            insert_query = """
                INSERT INTO "ProductImages"
                ("ProductId","FileGuid","FileName","Status",
                 "CreatedId","CreatedDate","Root","Order")
                VALUES (%s,%s,%s,%s,%s,%s,%s,%s)
            """
            execute_batch(cur, insert_query, insert_batch)
            conn.commit()
            print("✅ Insert completed.")
        else:
            print("ℹ️ No new images to insert.")

    except Exception as e:
        print(f"❌ Error: {e}")
        conn.rollback()
    finally:
        cur.close()
        conn.close()
        print("🏁 Script finished.")

if __name__ == "__main__":
    main()
