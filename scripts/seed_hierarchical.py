import json
import psycopg2
import unicodedata
import os
from tqdm import tqdm
from collections import Counter
import re

# === DB Ayarları ===
DB_PARAMS = dict(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)

# === Ayarlar ===
LEARNING_THRESHOLD_RATIO = 0.05  # Kategori ürünlerinin %5'inde geçen yeni kelimeyi öğren
MIN_OCCURRENCE = 5               # En az 5 üründe geçmeli
STOP_WORDS = {
    've', 'ile', 'veya', 'takim', 'adet', 'set', 'komple', 'sag', 'sol', 'on', 'arka',
    'ust', 'alt', 'yan', 'orta', 'tip', 'yeni', 'eski', 'model', 'kisa', 'uzun',
    'ic', 'dis', 'siyah', 'beyaz', 'gri', 'kirmizi', 'mavi', 'sari', 'yesil',
    'orjinal', 'ithal', 'yerli', 'marka', 'uyumlu', 'icin', 'kapi', 'motor', 'fren'
}

def normalize_text(text):
    if not text: return ""
    tr_map = str.maketrans("çğıöşüÇĞİÖŞÜ", "cgiosuCGIOSU")
    text = text.translate(tr_map).lower()
    return re.sub(r'[^a-z0-9\s]', '', text)

def insert_category(cursor, name, parent_id=None, order=0):
    if parent_id:
        cursor.execute('SELECT "Id" FROM "Category" WHERE "Name" = %s AND "ParentId" = %s', (name, parent_id))
    else:
        cursor.execute('SELECT "Id" FROM "Category" WHERE "Name" = %s AND "ParentId" IS NULL', (name,))
        
    res = cursor.fetchone()
    if res:
        return res[0]
    
    cursor.execute("""
        INSERT INTO "Category" 
        ("Name", "ParentId", "Status", "CreatedDate", "CreatedId", "IsMainPage", "IsMainSlider", "Order", "SubCategoryCount", "Height")
        VALUES (%s, %s, 1, NOW(), 1, FALSE, FALSE, %s, 0, 0)
        RETURNING "Id"
    """, (name, parent_id, order))
    return cursor.fetchone()[0]

def extract_frequent_words(product_names, existing_keywords):
    """Bir ürün listesinden sık geçen YENİ kelimeleri bulur."""
    all_words = []
    
    # Mevcut keywordleri set'e çevir (hızlı kontrol)
    existing_set = set(normalize_text(k) for k in existing_keywords)
    
    for name in product_names:
        norm = normalize_text(name)
        words = [w for w in norm.split() if w not in STOP_WORDS and len(w) > 2] # 3 harften uzunlar
        
        # Bigrams (2'li kelime grupları) da ekle (Örn: "camurluk davlumbazi")
        if len(words) >= 2:
            for i in range(len(words)-1):
                all_words.append(f"{words[i]} {words[i+1]}")
                
        all_words.extend(words)

    if not all_words: 
        return []

    count = len(product_names)
    threshold = max(MIN_OCCURRENCE, int(count * LEARNING_THRESHOLD_RATIO))
    
    counter = Counter(all_words)
    new_candidates = []
    
    for word, freq in counter.most_common(10): # En sık geçen 10 aday
        if freq >= threshold and word not in existing_set:
            new_candidates.append(word)
            
    return new_candidates

def main():
    print("⏳ [Self-Learning Modu] Ayarlar yükleniyor...")
    
    json_path = "scripts/hierarchical_keywords.json"
    if not os.path.exists(json_path):
        json_path = "hierarchical_keywords.json"

    try:
        with open(json_path, "r", encoding="utf-8") as f:
            tree_data = json.load(f)
    except FileNotFoundError:
        print(f"❌ '{json_path}' bulunamadı.")
        return

    conn = psycopg2.connect(**DB_PARAMS)
    cursor = conn.cursor()

    print("💾 Kategori Ağacı oluşturuluyor ve kurallar yükleniyor...")
    
    # Eşleştirme kuralları: (category_id, normalized_keyword, [path_to_json_node])
    match_rules = []
    
    # JSON Node'una referans tutmak zor olduğu için, basit bir harita kullanacağız
    # Ancak JSON'u güncellemek için hiyerarşik yapıya ihtiyacımız var.
    # Bu yüzden match_rules içine "leaf_category_name" de ekleyelim.
    
    # === 1. AĞAÇ OLUŞTURMA ===
    l1_order = 0
    for l1_name, l2_dict in tree_data.items():
        l1_order += 10
        l1_id = insert_category(cursor, l1_name, None, l1_order)
        
        l2_order = 0
        for l2_name, l3_dict in l2_dict.items():
            l2_order += 10
            l2_id = insert_category(cursor, l2_name, l1_id, l2_order)
            
            l3_order = 0
            for l3_name, keywords in l3_dict.items():
                l3_order += 10
                l3_id = insert_category(cursor, l3_name, l2_id, l3_order)
                
                # Kuralları ekle
                for kw in keywords:
                    match_rules.append((l3_id, normalize_text(kw), l1_name, l2_name, l3_name))
                    
    conn.commit()
    print(f"✅ Kategori ağacı aktif. {len(match_rules)} kural yüklendi.")

    # === 2. ÜRÜN EŞLEŞTİRME & ANALİZ ===
    # Uzun kelimeleri önce kontrol et (Precision)
    match_rules.sort(key=lambda x: len(x[1]), reverse=True)
    
    print("⏳ Tüm ürünler analiz ediliyor (Learning Mode)...")
    # "bütün ürünlere baksın" dendiği için filtre yok.
    cursor.execute('SELECT "Id", "Name" FROM "Product" WHERE "Status" != 99 LIMIT 1000000') 
    products = cursor.fetchall()
    
    print(f"🔍 {len(products)} ürün taranıyor...")
    
    batch_args = []
    matched_products_by_category = {} # { (l1,l2,l3): ["Product Name 1", ...] }
    
    for pid, name in tqdm(products, desc="Taranıyor"):
        norm_name = normalize_text(name)
        matched_rule = None
        
        for rule in match_rules:
            cat_id, keyword, l1, l2, l3 = rule
            if keyword in norm_name:
                matched_rule = rule
                break 
        
        if matched_rule:
            cat_id, keyword, l1, l2, l3 = matched_rule
            # Eşleştirmeyi kaydet (DB için)
            batch_args.append((pid, cat_id))
            
            # Öğrenme için kaydet (JSON için)
            key = (l1, l2, l3)
            if key not in matched_products_by_category:
                matched_products_by_category[key] = []
            matched_products_by_category[key].append(name)
            
    # Batch Insert (DB Update)
    if batch_args:
        print(f"💾 {len(batch_args)} eşleşme veritabanına yazılıyor...")
        query = 'INSERT INTO "ProductCategories" ("ProductId", "CategoryId") VALUES (%s, %s) ON CONFLICT DO NOTHING'
        chunk_size = 5000
        for i in tqdm(range(0, len(batch_args), chunk_size), desc="Kaydediliyor"):
            chunk = batch_args[i:i+chunk_size]
            cursor.executemany(query, chunk)
            conn.commit()
            
    # === 3. ÖĞRENME & GÜNCELLEME ===
    print("🧠 Öğrenme Modu: Yeni anahtar kelimeler aranıyor...")
    new_keywords_count = 0
    
    for key, product_names in tqdm(matched_products_by_category.items(), desc="Kategori Analizi"):
        l1, l2, l3 = key
        current_keywords = tree_data[l1][l2][l3]
        
        # Yeni kelime adaylarını bul
        new_words = extract_frequent_words(product_names, current_keywords)
        
        if new_words:
            print(f"   ✨ [{l3}] Yeni Kelimeler: {new_words}")
            tree_data[l1][l2][l3].extend(new_words)
            new_keywords_count += len(new_words)
            
    if new_keywords_count > 0:
        print(f"💾 {new_keywords_count} yeni kelime JSON dosyasına kaydediliyor...")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(tree_data, f, ensure_ascii=False, indent=2)
        print("✅ JSON Güncellendi.")
    else:
        print("ℹ️ Yeni öğrenilecek kelime bulunamadı.")

    print("✅ İŞLEM TAMAMLANDI.")
    cursor.close()
    conn.close()

if __name__ == "__main__":
    main()
