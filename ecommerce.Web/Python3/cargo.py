import os
import re
import json
import time
import math
import joblib
import argparse
import unicodedata
import psycopg2
import numpy as np
import pandas as pd
from tqdm import tqdm
from multiprocessing import Pool, cpu_count
from sklearn.feature_extraction.text import TfidfVectorizer

# XGBoost requirement
try:
    from xgboost import XGBRegressor
except ImportError:
    print("⚠️ XGBoost bulunamadı. Lütfen kurun: pip3 install xgboost")
    from sklearn.linear_model import Ridge as XGBRegressor # Fallback

# === Config ===
DB_PARAMS = dict(
    host="localhost", port=5454, database="MarketPlace", user="myinsurer", password="Posmdh0738"
)
MODEL_FILE = "desi_model_v11.joblib"
RESUME_FILE = "desi_resume.json"
AUDIT_LOG_FILE = "desi_log.csv"
DESI_DIVISOR = 3000
MAX_DIM_SAFE = 150 
GLOBAL_MAX_DESI = 20
ML_SAFETY_BUFFER = 1.05

# === Auto-Parts Specialist Rules (Business Wisdom) ===
KEYWORD_CAPS = {
    'conta': 3,
    'kece': 1,
    'rulman': 2,
    'balata': 4,
    'fren ayna': 10,
    'disk': 10,
    'subap': 1,
    'civata': 1,
    'somun': 1,
    'segman': 1,
    'kayis': 2,
    'hortum': 5,
    'tapa': 1,
    'filtre': 5,
    'buji': 1,
    'kapak': 8,
    'sensor': 1,
    'balat': 4,
    'kaput':8
}

# === Global Translation Map (Fast) ===
TR_MAP = str.maketrans("çğıöşü", "cgiosu")

def normalize_text(text):
    if not text: return ""
    # Hızlı çeviri ve temizlik
    text = text.lower().translate(TR_MAP).strip()
    # Unicode temizliği (Halı, Şemsiye vb. gibi karakterler için)
    return unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode("utf-8")

# === Improved Regex (V10: Mm Aware) ===
DIM_PATTERN = re.compile(r'(\d+(?:[.,]\d+)?)\s*[xX*]\s*(\d+(?:[.,]\d+)?)\s*[xX*]\s*(\d+(?:[.,]\d+)?)')

def extract_dimensions_pro(text):
    if not text: return None
    is_mm = re.search(r'(\d+)\s*(?:mm|Mm)', text) is not None
    clean_text = text.replace(',', '.')
    matches = DIM_PATTERN.findall(clean_text)
    if matches:
        try:
            results = []
            for m in matches:
                d = [float(val) for val in m]
                # Milimetreyi Santimetreye çevir (V10 logic)
                if is_mm or (any(v > 50 for v in d) and any(v < 30 for v in d)):
                    d = [v/10.0 for v in d]
                if all(v <= MAX_DIM_SAFE for v in d) and all(v > 0.1 for v in d):
                    results.append(tuple(d))
            if results:
                return max(results, key=lambda x: x[0]*x[1]*x[2])
        except: return None
    return None

# === Worker (Single Process - Bulk Enabled) ===
def process_batch_bulk(batch, model_data):
    vectorizer, model, cat_stats = model_data
    update_data = [] # List of tuples for bulk update
    audit_records = []
    
    for pid, name, desc, cid, cur_w, cur_l, cur_h, cur_weight, cur_desi in batch:
        norm_name = normalize_text(name)
        hard_cap = GLOBAL_MAX_DESI
        for kw, limit in KEYWORD_CAPS.items():
            if kw in norm_name: hard_cap = min(hard_cap, limit)

        dims = extract_dimensions_pro(name) or extract_dimensions_pro(desc)
        if dims:
            w, l, h = dims
            desi = int(min(hard_cap, math.ceil((w * l * h) / DESI_DIVISOR)))
            # (Width, Length, Height, CargoDesi, Weight, Id)
            update_data.append((float(w), float(l), float(h), int(desi), int(desi), int(pid)))
        elif model:
            X = vectorizer.transform([normalize_text(f"{name} {desc if desc else ''}")])
            desi = int(math.ceil(max(1.0, float(model.predict(X)[0]) * ML_SAFETY_BUFFER)))
            status = "XGB_OK"
            desi = min(hard_cap, desi)
            if cat_stats and str(cid) in cat_stats:
                avg = cat_stats[str(cid)]['avg']
                if desi > (avg * 2.5):
                    desi = int(math.ceil(avg * 1.5))
                    status = "XGB_CLAMPED"
            
            is_corrupted = (cur_w > MAX_DIM_SAFE or cur_l > MAX_DIM_SAFE or cur_h > MAX_DIM_SAFE or 
                            cur_weight > MAX_DIM_SAFE or cur_desi > GLOBAL_MAX_DESI)
            
            if is_corrupted:
                update_data.append((1.0, 1.0, 1.0, int(desi), int(desi), int(pid)))
                status += "_CLEANED"
            else:
                # Sadece Desi ve Weight güncelle (En Boy koru)
                update_data.append((float(cur_w), float(cur_l), float(cur_h), int(desi), int(desi), int(pid)))
            
            audit_records.append({"Id": pid, "Name": name[:50], "Desi": desi, "Method": status})
            
    return update_data, audit_records

class DesiPredictor:
    def __init__(self):
        self.vectorizer, self.model, self.cat_stats = None, None, {}

    def load_model(self):
        if os.path.exists(MODEL_FILE):
            try:
                self.vectorizer, self.model, self.cat_stats = joblib.load(MODEL_FILE)
                return True
            except: return False
        return False

    def train_from_db(self):
        print("🧠 V13: Bulk-Speed Model Eğitimi...")
        conn = psycopg2.connect(**DB_PARAMS)
        query = f'SELECT p."Name", p."Description", p."CargoDesi", pc."CategoryId" FROM "Product" p JOIN "ProductCategories" pc ON p."Id" = pc."ProductId" WHERE p."CargoDesi" BETWEEN 1 AND 20 AND p."Width" < {MAX_DIM_SAFE} LIMIT 300000'
        df = pd.read_sql_query(query, conn)
        conn.close()
        if len(df) < 500:
            print("❌ Eğitim için yeterli (500+) temiz veri yok.")
            return
        stats = df.groupby('CategoryId')['CargoDesi'].mean().to_dict()
        self.cat_stats = {str(k): {'avg': v} for k, v in stats.items()}
        print("📝 Metinler normalize ediliyor...")
        tqdm.pandas(desc="Normalization")
        df['text'] = (df['Name'] + " " + df['Description'].fillna("")).progress_apply(normalize_text)
        self.vectorizer = TfidfVectorizer(max_features=25000, ngram_range=(1,2))
        X = self.vectorizer.fit_transform(df['text'])
        print("🚀 XGBoost Model Fitting...")
        self.model = XGBRegressor(n_estimators=200, max_depth=6, learning_rate=0.1, objective='reg:squarederror', n_jobs=-1)
        self.model.fit(X, df['CargoDesi'])
        joblib.dump((self.vectorizer, self.model, self.cat_stats), MODEL_FILE)
        print(f"✅ V13 Model Hazır.")

    def run(self, reset=False):
        last_id = 0
        if not reset and os.path.exists(RESUME_FILE):
            with open(RESUME_FILE) as f: last_id = json.load(f).get("last_processed_id", 0)
        
        if not self.load_model():
            print("❌ Model bulunamadı. Lütfen önce --train yapın.")
            return

        print(f"🔍 Veriler veritabanından listeleniyor (ID > {last_id})...")
        conn = psycopg2.connect(**DB_PARAMS)
        read_cursor = conn.cursor() # Okuma imleci
        write_cursor = conn.cursor() # Yazma imleci
        
        query = """
            SELECT p."Id", p."Name", p."Description", MAX(pc."CategoryId"), p."Width", p."Length", p."Height", p."Weight", p."CargoDesi"
            FROM "Product" p
            LEFT JOIN "ProductCategories" pc ON p."Id" = pc."ProductId"
            WHERE (p."Id" > %s OR p."Width" > 150 OR p."Length" > 150 OR p."CargoDesi" > 20)
            GROUP BY p."Id", p."Name", p."Description", p."Width", p."Length", p."Height", p."Weight", p."CargoDesi"
            ORDER BY p."Id" ASC
        """
        read_cursor.execute(query, (last_id,))
        
        model_data = (self.vectorizer, self.model, self.cat_stats)
        print(f"🚀 V13.1 BULK-STABLE: İşlem başlıyor...")
        
        total_updated = 0
        pbar = tqdm(desc="İşleniyor", unit=" ürün")
        
        while True:
            rows = read_cursor.fetchmany(2000) # Her seferinde 2 bin satır çek
            if not rows: break
            
            # 1. Tahminleri Hesapla
            up_data, audit = process_batch_bulk(rows, model_data)
            
            # 2. Veritabanını TOPLU (Bulk) Güncelle - Yazma imleci kullanıyoruz
            if up_data:
                from psycopg2.extras import execute_values
                update_query = """
                    UPDATE "Product" AS p SET
                        "Width" = v.w, "Length" = v.l, "Height" = v.h,
                        "CargoDesi" = v.desi, "Weight" = v.weight
                    FROM (VALUES %s) AS v(w, l, h, desi, weight, id)
                    WHERE p."Id" = v.id
                """
                execute_values(write_cursor, update_query, up_data)
                conn.commit()
                
                total_updated += len(rows)
                pbar.update(len(rows))
                
                # Log ve Resume
                if audit:
                    with open(AUDIT_LOG_FILE, 'a') as f:
                        for r in audit: f.write(f"{r['Id']},\"{r['Name']}\",{r['Desi']},{r['Method']}\n")
                
                with open(RESUME_FILE, "w") as f: json.dump({"last_processed_id": rows[-1][0]}, f)

        pbar.close()
        print(f"✅ Bitti. Toplam {total_updated} ürün güncellendi.")
        read_cursor.close(); write_cursor.close(); conn.close()

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--run", action="store_true")
    parser.add_argument("--train", action="store_true")
    parser.add_argument("--reset", action="store_true")
    args = parser.parse_args()
    p = DesiPredictor()
    if args.train: p.train_from_db()
    elif args.run: p.run(reset=args.reset)
