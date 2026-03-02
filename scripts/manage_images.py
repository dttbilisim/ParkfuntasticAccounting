import os
import re
import psycopg2
from psycopg2.extras import execute_batch
from datetime import datetime

DB_CFG = {
    "host": "92.204.172.6",
    "port": 5454,
    "database": "MarketPlace",
    "user": "myinsurer",
    "password": "Posmdh0738"
}

BASE_ROOT = r"C:\inetpub\wwwroot\cdn.yedeksen.com\images\ProductImages"
IMAGE_EXTENSIONS = {".jpg", ".png","jpeg","webp","bmp"}


def normalize_oem(oem):
    return str(oem).strip().replace(" ", "").upper()


def split_oems(text):
    return [
        normalize_oem(p)
        for p in re.split(r"[,;|/\\\s]+", str(text))
        if p.strip()
    ]


def scan_images():
    """
    OEM -> filename map
    """
    image_map = {}
    physical_files = set()

    for f in os.listdir(BASE_ROOT):
        name, ext = os.path.splitext(f)
        if ext.lower() in IMAGE_EXTENSIONS:
            norm = normalize_oem(name)
            image_map[norm] = f
            physical_files.add(f)

    return image_map, physical_files


def main():
    print("📂 Disk taranıyor...")
    image_map, physical_files = scan_images()
    print(f"🖼 Fiziksel resim: {len(physical_files)}")

    conn = psycopg2.connect(**DB_CFG)
    cur = conn.cursor()


    # -----------------------------
    # DB verilerini al
    # -----------------------------
    print("📦 Ürünler alınıyor...")
    cur.execute("""
        SELECT "Id","Oems"
        FROM "Product"
        WHERE "SellerId" = 3
          AND "Oems" IS NOT NULL
    """)
    products = cur.fetchall()

    print("📑 ProductImages alınıyor (Sadece Seller 3)...")
    # CRITICAL FIX: Sadece Seller 3'e ait resimleri çek!
    cur.execute("""
        SELECT pi."Id", pi."ProductId", pi."FileName"
        FROM "ProductImages" pi
        JOIN "Product" p ON p."Id" = pi."ProductId"
        WHERE p."SellerId" = 3
    """)
    product_images = cur.fetchall()

    existing_pairs = {(pid, fname) for _, pid, fname in product_images}

    # -----------------------------
    # 1️⃣ Disk VAR → DB YOK → INSERT
    # -----------------------------
    insert_batch = []

    for product_id, oems_raw in products:
        for oem in split_oems(oems_raw):
            if oem not in image_map:
                continue

            file_name = image_map[oem]

            if (product_id, file_name) in existing_pairs:
                break

            insert_batch.append((
                product_id,
                file_name,
                file_name,
                1,
                1,
                datetime.now(),
                BASE_ROOT,
                1
            ))
            break

    if insert_batch:
        print(f"🟢 INSERT: {len(insert_batch)}")
        execute_batch(cur, """
            INSERT INTO "ProductImages"
            ("ProductId","FileGuid","FileName","Status",
             "CreatedId","CreatedDate","Root","Order")
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s)
        """, insert_batch)

    # -----------------------------
    # 2️⃣ DB VAR → Disk YOK → DELETE
    # -----------------------------
    delete_ids = []

    for img_id, _, file_name in product_images:
        file_path = os.path.join(BASE_ROOT, file_name)
        if not os.path.isfile(file_path):
            delete_ids.append((img_id,))

    if delete_ids:
        print(f"🔴 DELETE: {len(delete_ids)}")
        execute_batch(cur, """
            DELETE FROM "ProductImages"
            WHERE "Id" = %s
        """, delete_ids)

    conn.commit()

    print("\n===== ÖZET =====")
    print(f"🟢 Eklenen kayıt : {len(insert_batch)}")
    print(f"🔴 Silinen kayıt : {len(delete_ids)}")

    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
