import json
import re
import psycopg2
import unicodedata
import os
from tqdm import tqdm

# === DB Ayarları ===
DB_PARAMS = dict(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)

def normalize_text(text):
    if not text: return ""
    tr_map = str.maketrans("çğıöşüÇĞİÖŞÜ", "cgiosuCGIOSU")
    text = text.translate(tr_map).lower()
    return text

def main():
    print("⏳ Ayarlar yükleniyor...")
    
    # 1. Keywords JSON'ı yükle
    json_path = "spare_parts_keywords.json"
    if not os.path.exists(json_path):
        json_path = "scripts/spare_parts_keywords.json"

    try:
        with open(json_path, "r", encoding="utf-8") as f:
            category_map = json.load(f) # { "Fren Sistemi": ["fren", ...], ... }
    except FileNotFoundError:
        print(f"❌ '{json_path}' bulunamadı.")
        return

    conn = psycopg2.connect(**DB_PARAMS)
    cursor = conn.cursor()

    print("💾 Kategoriler veritabanına işleniyor (Seed)...")
    
    # Kategori ID'lerini saklamak için
    cat_id_map = {} # { "Fren Sistemi": 12, ... }

    # 2. Kategorileri Oluştur
    for cat_name, keywords in category_map.items():
        cursor.execute('SELECT "Id" FROM "Category" WHERE "Name" = %s', (cat_name,))
        res = cursor.fetchone()
        
        if res:
            cat_id = res[0]
        else:
            cursor.execute("""
                INSERT INTO "Category" 
                ("Name", "Status", "CreatedDate", "CreatedId", "IsMainPage", "IsMainSlider", "Order") 
                VALUES (%s, 1, NOW(), 1, FALSE, FALSE, 0) 
                RETURNING "Id"
            """, (cat_name,))
            cat_id = cursor.fetchone()[0]
            print(f"   ➕ Kategori oluşturuldu: {cat_name}")
            
        cat_id_map[cat_name] = cat_id
        
    conn.commit()
    print("✅ Kategori yapısı tamam.")

    # 3. Ürünleri Çek (Sadece kategorize edilmemişleri al)
    print("⏳ Ürünler analiz ediliyor (Sadece kategorisizler)...")
    cursor.execute("""
        SELECT p."Id", p."Name" 
        FROM "Product" p 
        WHERE p."Status" != 99 
          AND NOT EXISTS (
              SELECT 1 FROM "ProductCategories" pc 
              WHERE pc."ProductId" = p."Id"
          )
    """)
    products = cursor.fetchall()
    
    if not products:
        print("✅ Tüm ürünler zaten kategorize edilmiş.")
        return
    
    print(f"🔍 {len(products)} ürün taranacak...")
    
    # Performans için keywordleri normalize edip hafızaya alalım
    # [ (cat_id, "keyword"), (cat_id, "fren"), ... ] öncelikli sıraya göre
    match_rules = []
    for cat_name, keywords in category_map.items():
        cat_id = cat_id_map[cat_name]
        for kw in keywords:
            match_rules.append((cat_id, normalize_text(kw)))
            
    # Eşleştirme ve Kayıt
    batch_args = []
    matched_count = 0
    
    for pid, name in tqdm(products):
        norm_name = normalize_text(name)
        
        # Kelime eşleşmesi
        matched_cat_id = None
        
        # Basit "içinde geçiyor mu" kontrolü
        for cat_id, keyword in match_rules:
            # Tam kelime eşleşmesi için regex (örn: "dis" kelimesi "disk" içinde geçmesin diye)
            # Ama basit startswith/contains daha hızlı. Şimdilik " in " mantığı.
            if keyword in norm_name:
                matched_cat_id = cat_id
                break # İlk eşleşeni al (JSON sırası önemli - Genelden özele)
        
        if matched_cat_id:
            batch_args.append((pid, matched_cat_id))
            matched_count += 1
            
    # Batch Insert
    print(f"💾 {matched_count} eşleşme kaydediliyor...")
    query = 'INSERT INTO "ProductCategories" ("ProductId", "CategoryId") VALUES (%s, %s) ON CONFLICT DO NOTHING'
    
    chunk_size = 5000
    for i in tqdm(range(0, len(batch_args), chunk_size)):
        chunk = batch_args[i:i+chunk_size]
        cursor.executemany(query, chunk)
        conn.commit()
        
    print(f"✅ İŞLEM TAMAMLANDI. Toplam {matched_count} ürün kategorize edildi.")
    cursor.close()
    conn.close()

if __name__ == "__main__":
    main()
