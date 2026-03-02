import os
import re
import json
import time
import joblib
import psycopg2
import unicodedata
import numpy as np
from tqdm import tqdm
from multiprocessing import Pool, cpu_count
from sklearn.linear_model import SGDClassifier
from sklearn.feature_extraction.text import TfidfVectorizer

# === PostgreSQL bağlantısı ===
DB_PARAMS = dict(
    host="localhost", port=5454, database="MarketPlace", user="myinsurer", password="Posmdh0738"
)

# === Normalize ===
def normalize_text(text):
    tr_map = str.maketrans("çğıöşü", "cgiosu")
    text = text.lower().translate(tr_map).strip()
    return unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode("utf-8")

# === Resume ===
RESUME_FILE = "resume.json"
last_processed_id = 0
if os.path.exists(RESUME_FILE):
    with open(RESUME_FILE) as f:
        data = json.load(f)
        last_processed_id = data.get("last_processed_id", 0)
        if last_processed_id:
            print(f"⏩ Kaldığı yerden devam ediyor (ProductId > {last_processed_id})")

# === Kategorileri çek ===
conn = psycopg2.connect(**DB_PARAMS)
cursor = conn.cursor()
cursor.execute('SELECT "Id", "Name" FROM "Category" WHERE "Status"=1')
categories = cursor.fetchall()
texts = [normalize_text(name) for _, name in categories]
labels = [cid for cid, _ in categories]

# === MiniBatch TF-IDF + SGDClassifier ===
print(f"🤖 MiniBatch ML eğitimi başlıyor... ({len(texts)} örnek)")
vectorizer = TfidfVectorizer(max_features=5000)
X = vectorizer.fit_transform(texts)
clf = SGDClassifier(loss="log_loss", max_iter=5, random_state=42)

batch_size = max(10000, len(texts)//80)
for i in tqdm(range(0, len(texts), batch_size), desc="📊 Eğitim ilerliyor"):
    X_batch = X[i:i+batch_size]
    y_batch = labels[i:i+batch_size]
    clf.partial_fit(X_batch, y_batch, classes=np.unique(labels))

joblib.dump((vectorizer, clf), "category_model_minibatch.joblib")
print("✅ MiniBatch model kaydedildi: category_model_minibatch.joblib")

# === Ürünleri getir ===
cursor.execute('SELECT "Id","Name" FROM "Product" WHERE "Id" > %s ORDER BY "Id" ASC', (last_processed_id,))
products = cursor.fetchall()
total_products = len(products)
print(f"🔍 İşlenecek ürün sayısı: {total_products}")
cursor.close(); conn.close()

# === Tahmin fonksiyonu ===
vectorizer, clf = joblib.load("category_model_minibatch.joblib")

def predict_batch(batch):
    conn = psycopg2.connect(**DB_PARAMS)
    cursor = conn.cursor()
    inserted = 0
    for pid, name in batch:
        norm_name = normalize_text(name)
        X_test = vectorizer.transform([norm_name])
        cat_pred = int(clf.predict(X_test)[0])
        cursor.execute("""
            INSERT INTO "ProductCategories" ("ProductId","CategoryId")
            VALUES (%s,%s)
            ON CONFLICT DO NOTHING
        """, (int(pid), cat_pred))
        inserted += 1
    conn.commit()
    cursor.close()
    conn.close()
    return inserted, batch[-1][0]  # (eklenen, son ProductId)

# === Paralel işlem ===
batch_size = 10000
batches = [products[i:i+batch_size] for i in range(0, total_products, batch_size)]

total_inserted = 0
start_time = time.time()

with Pool(processes=max(1, cpu_count() - 1)) as pool:
    for idx, result in enumerate(tqdm(pool.imap(predict_batch, batches), total=len(batches), desc="⚙️ Paralel işleniyor")):
        inserted, last_id = result
        total_inserted += inserted
        if idx % 1 == 0:
            with open(RESUME_FILE, "w") as f:
                json.dump({"last_processed_id": last_id}, f)
            print(f"💾 {total_inserted}/{total_products} ürün işlendi (%{(total_inserted/total_products)*100:.1f})")

end_time = time.time()
print(f"\n🎯 Toplam eklenen: {total_inserted}")
print(f"⏱️ Süre: {(end_time - start_time)/60:.1f} dakika")
print("💾 Tüm işlemler tamamlandı — resume, batch ve multiprocessing aktif.")