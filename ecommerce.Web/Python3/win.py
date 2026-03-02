#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import re
import logging
from typing import Optional, Tuple, List, Dict

import psycopg2
import psycopg2.extras

# ================== DB AYAR ==================
PG_CFG = dict(
    host=os.environ.get("PG_HOST", "localhost"),
    port=int(os.environ.get("PG_PORT", "5454")),
    database=os.environ.get("PG_DB", "MarketPlace"),
    user=os.environ.get("PG_USER", "myinsurer"),
    password=os.environ.get("PG_PASS", "Posmdh0738"),
)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)

# ================== YARDIMCILAR ==================
WSPLIT = re.compile(r"[;,/\|]+")

def clean(s: Optional[str]) -> str:
    return re.sub(r"\s+", " ", (s or "").strip())

def split_multi(s: str) -> List[str]:
    if not s:
        return []
    parts = [p.strip() for p in WSPLIT.split(s) if p and p.strip()]
    return parts

def parse_year_range(txt: str) -> Tuple[Optional[int], Optional[int]]:
    """
    '2005-2010', '2005 / 2010', '2005–2010' -> (2005, 2010)
    '2007' -> (2007, None)
    else -> (None, None)
    """
    if not txt:
        return (None, None)
    t = clean(txt)
    # range
    m = re.search(r"\b(19\d{2}|20\d{2})\s*[-/–]\s*(19\d{2}|20\d{2})\b", t)
    if m:
        y1, y2 = int(m.group(1)), int(m.group(2))
        if y2 < y1:
            y1, y2 = y2, y1
        return (y1, y2)
    # single
    m = re.search(r"\b(19\d{2}|20\d{2})\b", t)
    if m:
        return (int(m.group(1)), None)
    return (None, None)

def safe_int(s: Optional[str]) -> Optional[int]:
    try:
        return int(s) if s is not None else None
    except:
        return None

# ================== DB YARDIM FONKS. ==================
class Db:
    def __init__(self, cfg: dict):
        self.conn = psycopg2.connect(**cfg)
        self.conn.autocommit = False

    def execute_returning_id(self, sql: str, params: tuple = ()) -> int:
        with self.conn.cursor() as cur:
            cur.execute(sql, params)
            rid = cur.fetchone()
            return rid[0] if rid else None

    def close(self):
        try:
            self.conn.close()
        except:
            pass

    def fetchone(self, sql: str, params: tuple) -> Optional[dict]:
        with self.conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
            cur.execute(sql, params)
            return cur.fetchone()

    def fetchall(self, sql: str, params: tuple = ()) -> List[dict]:
        with self.conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
            cur.execute(sql, params)
            return list(cur.fetchall())

    def execute(self, sql: str, params: tuple) -> int:
        with self.conn.cursor() as cur:
            cur.execute(sql, params)
            return cur.rowcount


# ---------- GET-OR-CREATE YARDIMCILARI ----------
def get_or_create_brand(db: Db, name: str) -> int:
    name = clean(name)
    row = db.fetchone('SELECT "Id" FROM "CarBrands" WHERE "Name" = %s;', (name,))
    if row:
        db.execute('UPDATE "CarBrands" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarBrands" ("Name","CreatedDate","CreatedId","Status")
           VALUES (%s,NOW(),1,1)
           RETURNING "Id";''',
        (name,)
    )

def get_or_create_model(db: Db, brand_id: int, name: str) -> int:
    name = clean(name)
    row = db.fetchone('SELECT "Id" FROM "CarModels" WHERE "CarBrandId"=%s AND "Name"=%s;', (brand_id, name))
    if row:
        db.execute('UPDATE "CarModels" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarModels" ("CarBrandId","Name","CreatedDate","CreatedId","Status")
           VALUES (%s,%s,NOW(),1,1)
           RETURNING "Id";''',
        (brand_id, name)
    )

def get_or_create_engine(db: Db, name: str, model_id: int) -> Optional[int]:
    """
    Engine kaydı UNIQUE("Name") veya UNIQUE("CarModelId","Name") durumlarında
    güvenle çalışsın. Önce (model,name), sonra sadece (name) kontrol eder.
    Gerekirse mevcut kaydı günceller, yoksa ekler.
    """
    name = clean(name)
    if not name:
        return None

    # 1) Model + Name ile var mı?
    row = db.fetchone(
        'SELECT "Id" FROM "CarEngines" WHERE "CarModelId"=%s AND "Name"=%s;',
        (model_id, name)
    )
    if row:
        # Mevcut
        db.execute('UPDATE "CarEngines" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]

    # 2) Sadece Name ile var mı? (DB'de UNIQUE("Name") olabilir)
    row = db.fetchone(
        'SELECT "Id","CarModelId" FROM "CarEngines" WHERE "Name"=%s;',
        (name,)
    )
    if row:
        # Kaydı kullan; eğer model boş/NULL ise ilişkilendir
        if not row.get("CarModelId") and model_id:
            db.execute(
                'UPDATE "CarEngines" SET "CarModelId"=%s, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;',
                (model_id, row["Id"])
            )
        return row["Id"]

    # 3) Yoksa ekle — UNIQUE("Name") varsa ON CONFLICT ile yumuşak davran
    return db.execute_returning_id(
        '''
        INSERT INTO "CarEngines" ("CarModelId","Name","Status","CreatedId","CreatedDate")
        VALUES (%s,%s,1,1,NOW())
        ON CONFLICT ("Name") DO UPDATE
            SET "ModifiedDate"=NOW(), "ModifiedId"=1
        RETURNING "Id";
        ''',
        (model_id, name)
    )
def get_or_create_fuel(db: Db, name: str) -> Optional[int]:
    name = clean(name)
    if not name:
        return None
    row = db.fetchone('SELECT "Id" FROM "CarFuelTypes" WHERE "Name"=%s;', (name,))
    if row:
        db.execute('UPDATE "CarFuelTypes" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarFuelTypes" ("Name","Status","CreatedDate","CreatedId")
           VALUES (%s, 1, NOW(), 1)
           RETURNING "Id";''',
        (name,)
    )

def get_or_create_gearbox(db: Db, name: str) -> Optional[int]:
    name = clean(name)
    if not name:
        return None
    row = db.fetchone('SELECT "Id" FROM "CarGearboxes" WHERE "Name"=%s;', (name,))
    if row:
        db.execute('UPDATE "CarGearboxes" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarGearboxes" ("Name","Status","CreatedId","CreatedDate")
           VALUES (%s,1,1,NOW())
           RETURNING "Id";''',
        (name,)
    )
def parse_year_text(year_txt):
    """
    Yıl metnini parçalayıp (start, end, raw_text) döndürür.
    Örnekler:
      '2012-2016' -> (2012, 2016, '2012-2016')
      '2015->'    -> (2015, None, '2015->')
      '->2018'    -> (None, 2018, '->2018')
      '2012; 2009'-> (2009, 2012, '2012; 2009')
      '2015'      -> (2015, None, '2015')
    Hiç yıl yoksa (None, None, raw_text) döner ve çağıran taraf default değer koyar.
    """
    raw_text = (year_txt or "").strip()
    if not raw_text:
        return None, None, ""

    t = raw_text

    # Arrow formatları için ipuçları
    has_right_arrow = '->' in t and not t.startswith('->')
    has_left_arrow  = t.startswith('->')

    # Metinden tüm 4 haneli yıl değerlerini topla
    years = re.findall(r'(19\d{2}|20\d{2})', t)
    years = [int(y) for y in years]

    if len(years) >= 2:
        y1, y2 = min(years[0], years[1]), max(years[0], years[1])
        return y1, y2, raw_text
    elif len(years) == 1:
        y = years[0]
        if has_right_arrow:
            # 2015->
            return y, None, raw_text
        if has_left_arrow:
            # ->2018
            return None, y, raw_text
        # Tek yıl
        return y, None, raw_text
    else:
        # Yıl bulunamadı (ör: tamamen serbest metin)
        return None, None, raw_text
def get_or_create_year(db, year_txt):
    y_start, y_end, raw_text = parse_year_text(year_txt)

    # DB tarafında NOT NULL ihlali olmaması için boşları 0'a sabitle (0 = bilinmiyor)
    norm_start = y_start if y_start is not None else 0
    norm_end   = y_end   if y_end   is not None else 0
    norm_raw   = raw_text or ''

    # Var mı?
    row = db.fetchone(
        'SELECT "Id" FROM "CarYears" '
        'WHERE COALESCE("YearStart", 0) = %s '
        'AND   COALESCE("YearEnd", 0)   = %s '
        'AND   COALESCE("RawText", \'\') = %s;',
        (norm_start, norm_end, norm_raw)
    )
    if row:
        db.execute('UPDATE "CarYears" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row['Id'],))
        return row['Id']

    # Yoksa ekle
    return db.execute_returning_id(
        '''INSERT INTO "CarYears" ("YearStart","YearEnd","RawText","Status","CreatedDate","CreatedId")
           VALUES (%s,%s,%s,1,NOW(),1)
           RETURNING "Id";''',
        (norm_start, norm_end, norm_raw)
    )

def get_or_create_vin(db: Db, code: str) -> Optional[int]:
    code = clean(code)
    if not code:
        return None
    row = db.fetchone('SELECT "Id" FROM "CarVins" WHERE "Code"=%s;', (code,))
    if row:
        db.execute('UPDATE "CarVins" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarVins" ("Code","Status","CreatedId","CreatedDate")
           VALUES (%s,1,1,NOW())
           RETURNING "Id";''',
        (code,)
    )

def get_or_create_original_number(db: Db, num: str) -> Optional[int]:
    n = clean(num)
    if not n:
        return None
    # 4 karakter altını çoğu zaman atlıyoruz; istersen kaldır
    if len(re.sub(r"[^A-Za-z0-9]", "", n)) < 4:
        return None
    row = db.fetchone('SELECT "Id" FROM "CarOriginalNumbers" WHERE "Number"=%s;', (n,))
    if row:
        db.execute('UPDATE "CarOriginalNumbers" SET "Status"=1, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;', (row["Id"],))
        return row["Id"]
    return db.execute_returning_id(
        '''INSERT INTO "CarOriginalNumbers" ("Number","Status","CreatedId","CreatedDate")
           VALUES (%s,1,1,NOW())
           RETURNING "Id";''',
        (n,)
    )

def upsert_spec(db: Db, payload: Dict) -> int:
    """
    CarSpecs kaydı: (BrandId, ModelId, EngineId?, FuelId?, GearboxId?, YearId?, VinId?, Oem, SourceUrl)
    Aynı kombinasyon varsa günceller, yoksa ekler.
    """
    # Önce var mı?
    row = db.fetchone("""
        SELECT "Id" FROM "CarSpecs"
        WHERE "OEM"=%s
          AND "CarBrandId"=%s
          AND "ModelId"=%s
          AND COALESCE("CarEngineId",-1)=COALESCE(%s,-1)
          AND COALESCE("CarFuelId",-1)=COALESCE(%s,-1)
          AND COALESCE("CarGearboxId",-1)=COALESCE(%s,-1)
          AND COALESCE("CarYearId",-1)=COALESCE(%s,-1);
    """, (
        payload["OEM"], payload["BrandId"], payload["ModelId"],
        payload.get("EngineId"), payload.get("FuelId"),
        payload.get("GearboxId"), payload.get("YearId"),
    ))
    if row:
        # küçük bir update örneği (SourceUrl değişebilir)
        db.execute(
            'UPDATE "CarSpecs" SET "SourceUrl"=%s, "ModifiedDate"=NOW(), "ModifiedId"=1 WHERE "Id"=%s;',
            (payload.get("SourceUrl"), row["Id"])
        )
        return row["Id"]

    # yoksa insert with ON CONFLICT
    return db.execute_returning_id(
        """
        INSERT INTO "CarSpecs"
        ("CarBrandId","ModelId","CarEngineId","CarFuelId","CarGearboxId","CarYearId","CarVinId","OEM","SourceUrl",
         "Status","CreatedId","CreatedDate")
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s, 1,1,NOW())
        ON CONFLICT ("OEM", "CarBrandId", "ModelId", "CarEngineId", "CarFuelId", "CarGearboxId", "CarYearId") DO UPDATE SET
            "SourceUrl" = EXCLUDED."SourceUrl",
            "Status" = EXCLUDED."Status",
            "ModifiedId" = 1,
            "ModifiedDate" = NOW()
        RETURNING "Id";
        """,
        (
            payload["BrandId"], payload["ModelId"],
            payload.get("EngineId"), payload.get("FuelId"),
            payload.get("GearboxId"), payload.get("YearId"),
            payload.get("VinId"),
            payload["OEM"], payload.get("SourceUrl")
        )
    )

def link_spec_original_number(db: Db, spec_id: int, on_id: int):
    # köprüde varsa tekrar ekleme
    row = db.fetchone(
        'SELECT 1 FROM "CarSpecOriginalNumbers" WHERE "CarSpecId"=%s AND "OriginalNumberId"=%s;',
        (spec_id, on_id)
    )
    if row:
        return
    db.execute(
        'INSERT INTO "CarSpecOriginalNumbers" ("CarSpecId","OriginalNumberId") VALUES (%s,%s);',
        (spec_id, on_id)
    )

# ================== MAIN ==================
def main():
    db = Db(PG_CFG)
    total = 0
    try:
        logging.info("OemToCars -> Car* tablolarına aktarım başlıyor…")

        # OemToCars kaynağı: DISTINCT ile gereksiz tekrar azalır
        rows = db.fetchall("""
            SELECT DISTINCT
                COALESCE(NULLIF("Brand",''),'')   AS "Brand",
                COALESCE(NULLIF("Model",''),'')   AS "Model",
                COALESCE(NULLIF("Engine",''),'')  AS "Engine",
                COALESCE(NULLIF("Fuel",''),'')    AS "Fuel",
                COALESCE(NULLIF("Gearbox",''),'') AS "Gearbox",
                COALESCE(NULLIF("Years",''),'')   AS "Years",
                COALESCE(NULLIF("VIN",''),'')     AS "VIN",
                COALESCE(NULLIF("OriginalNumbers",''),'') AS "OriginalNumbers",
                COALESCE(NULLIF("OEM",''),'')     AS "OEM",
                COALESCE(NULLIF("ImageUrl",''),'') AS "ImageUrl"
            FROM "OemToCars"
            WHERE COALESCE(NULLIF("Brand",''),'') <> ''
              AND COALESCE(NULLIF("Model",''),'') <> ''
              AND COALESCE(NULLIF("OEM",''),'')   <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM "CarSpecs" cs
                  WHERE cs."OEM" = "OemToCars"."OEM"
              )
        """)
        logging.info("Kaynak satır: %d", len(rows))

        for r in rows:
            brand = clean(r["Brand"])
            model = clean(r["Model"])
            engine = clean(r["Engine"])
            if engine and (engine.upper() == brand.upper() or engine.upper() == model.upper()):
                engine = ""
            fuel = clean(r["Fuel"])
            gearbox = clean(r["Gearbox"])
            years_txt = clean(r["Years"])
            vin_txt = clean(r["VIN"])
            oem = clean(r["OEM"])
            img = clean(r["ImageUrl"])
            orig_numbers = split_multi(r["OriginalNumbers"])

            # Brand / Model
            brand_id = get_or_create_brand(db, brand)
            model_id = get_or_create_model(db, brand_id, model)

            # Opsiyonel alanlar
            engine_id = get_or_create_engine(db, engine, model_id) if engine else None
            fuel_id = get_or_create_fuel(db, fuel) if fuel else None
            gearbox_id = get_or_create_gearbox(db, gearbox) if gearbox else None
            year_id = get_or_create_year(db, years_txt) if years_txt else None
            vin_id = get_or_create_vin(db, vin_txt) if vin_txt else None

            spec_id = upsert_spec(db, dict(
                BrandId=brand_id,
                ModelId=model_id,
                EngineId=engine_id,
                FuelId=fuel_id,
                GearboxId=gearbox_id,
                YearId=year_id,
                VinId=vin_id,
                OEM=oem,
                SourceUrl=img or None
            ))

            # OriginalNumbers
            for num in orig_numbers:
                on_id = get_or_create_original_number(db, num)
                if on_id is not None:
                    # köprü tablo varsa bağla
                    try:
                        link_spec_original_number(db, spec_id, on_id)
                    except Exception as e:
                        # Eğer köprü tablo yoksa bu kısmı sessiz geçmek için yorum satırına alabilirsin
                        logging.debug("Köprü ekleme atlandı: %s", e)

            total += 1
            if total % 1000 == 0:
                db.conn.commit()
                logging.info("Ara commit (%d kayıt)…", total)

        db.conn.commit()
        logging.info("Bitti. Toplam işlenen satır: %d", total)

    except Exception as ex:
        db.conn.rollback()
        logging.error("Hata! Tüm değişiklikler geri alındı: %s", ex)
        raise
    finally:
        db.close()

if __name__ == "__main__":
    main()