import os
import re
import psycopg2
import unicodedata
import numpy as np
from tqdm import tqdm
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.cluster import MiniBatchKMeans
from collections import Counter

# === DB Ayarları ===
DB_PARAMS = dict(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738"
)

# === Ayarlar ===
NUM_CLUSTERS = 300     # Hedeflenen kategori sayısı (Yaklaşık)
BATCH_SIZE = 5000      # ML işlem batch boyutu
MIN_CLUSTER_SIZE = 10  # Bu sayıdan az ürünü olan kümeler 'Diğer' olur

def normalize_text(text):
    if not text: return ""
    tr_map = str.maketrans("çğıöşüÇĞİÖŞÜ", "cgiosuCGIOSU")
    text = text.translate(tr_map).lower()
    text = re.sub(r'[^a-zA-Z0-9\s]', ' ', text) # Sadece harf rakam
    return re.sub(r'\s+', ' ', text).strip()

def get_stop_words():
    # Kategori isminde geçmemesi gereken anlamsız kelimeler
    return [
        've', 'ile', 'icin', 'uyumlu', 'model', 'orjinal', 'yan', 'on', 'arka', 
        'sag', 'sol', 'takim', 'adet', 'set', 'kople', 'komple', 'yeni', 'eski', 'tip'
    ]

def generate_category_name(product_names):
    """Kümedeki en sık geçen kelimelerden kategori ismi türetir"""
    words = []
    stop_words = get_stop_words()
    
    for name in product_names:
        # 2'li kelime grupları (Bigrams) daha anlamlı kategori ismi çıkarır
        # Örn: "fren balatasi", "yag filtresi"
        tokens = [t for t in normalize_text(name).split() if t not in stop_words and len(t) > 2]
        
        # Tek kelimeler
        words.extend(tokens)
        
        # İkili gruplar (Bigrams)
        if len(tokens) >= 2:
            for i in range(len(tokens)-1):
                words.append(f"{tokens[i]} {tokens[i+1]}")
    
    if not words:
        return "Genel Parcalar"
        
    # En sık geçen kelime/kelime grubu
    most_common = Counter(words).most_common(1)
    if most_common:
        cat_name = most_common[0][0]
        return cat_name.title() # "Fren Balatasi" yapar
    return "Diger"

def main():
    conn = psycopg2.connect(**DB_PARAMS)
    cursor = conn.cursor()
    
    print("⏳ Ürün verileri çekiliyor (Product + Brand)...")
    # Sadece ismi olan ürünleri çek, Brand ismini de al
    sql = """
        SELECT p."Id", p."Name", COALESCE(b."Name", '') 
        FROM "Product" p
        LEFT JOIN "Brand" b ON p."BrandId" = b."Id"
        WHERE p."Name" IS NOT NULL AND p."Status" != 99
    """
    cursor.execute(sql)
    products = cursor.fetchall() # [(Id, Name, BrandName), ...]
    
    print(f"✅ {len(products)} ürün yüklendi.")
    
    if not products:
        print("❌ İşlenecek ürün yok.")
        return

    # ML için metin hazırla: "Marka ÜrünAdı"
    corpus = []
    pids = []
    raw_names = []
    
    print("⏳ Metinler normalize ediliyor...")
    for pid, name, brand in tqdm(products):
        # Marka ismini de ekliyoruz ki "BOSCH Fren" ile "VALEO Fren" benzer kümelensin
        text = f"{brand} {name}" 
        corpus.append(normalize_text(text))
        pids.append(pid)
        raw_names.append(name)
        
    # 1. VEKTÖRİZASYON (TF-IDF)
    print("🤖 TF-IDF Vektörlemesi yapılıyor...")
    vectorizer = TfidfVectorizer(max_features=5000, stop_words=None)
    X = vectorizer.fit_transform(corpus)
    
    # 2. KÜMELEME (Clustering)
    print(f"🤖 {NUM_CLUSTERS} kategoriye ayrıştırılıyor (K-Means)...")
    kmeans = MiniBatchKMeans(
        n_clusters=NUM_CLUSTERS,
        batch_size=BATCH_SIZE,
        random_state=42,
        n_init=3
    )
    labels = kmeans.fit_predict(X)
    
    # 3. KATEGORİ İSİMLENDİRME
    print("🏷️ Kategori isimleri üretiliyor...")
    
    # Cluster ID -> [Product Names]
    cluster_products = {}
    for i, label in enumerate(labels):
        if label not in cluster_products:
            cluster_products[label] = []
        cluster_products[label].append(raw_names[i])
        
    # Cluster ID -> Category Name
    cluster_names = {}
    cluster_ids_map = {} # ClusterID -> DB CategoryId
    
    # Kategorileri DB'ye Ekle
    print("💾 Kategoriler veritabanına yazılıyor...")
    inserted_cats = 0
    
    for label, names in cluster_products.items():
        if len(names) < MIN_CLUSTER_SIZE:
            cat_name = "Diger Yedek Parcalar"
        else:
            cat_name = generate_category_name(names)
            
        # Aynı isimde kategori varsa ID'sini al, yoksa ekle
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
            inserted_cats += 1
            
        cluster_ids_map[label] = cat_id
        conn.commit()

    print(f"✅ {inserted_cats} yeni kategori oluşturuldu.")
    
    # 4. ÜRÜN - KATEGORİ EŞLEŞTİRME
    print("🔗 Ürünler kategorilere bağlanıyor...")
    
    batch_args = []
    for i, pid in enumerate(pids):
        cluster_label = labels[i]
        cat_id = cluster_ids_map[cluster_label]
        batch_args.append((pid, cat_id))
        
    # Batch Insert (Performans için)
    query = 'INSERT INTO "ProductCategories" ("ProductId", "CategoryId") VALUES (%s, %s) ON CONFLICT DO NOTHING'
    
    # 1000'erli paketler halinde at
    chunk_size = 1000
    for i in tqdm(range(0, len(batch_args), chunk_size)):
        chunk = batch_args[i:i+chunk_size]
        cursor.executemany(query, chunk)
        conn.commit()
        
    print("✅ Tüm işlemler tamamlandı.")
    cursor.close()
    conn.close()

if __name__ == "__main__":
    main()
