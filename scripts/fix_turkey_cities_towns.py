#!/usr/bin/env python3
"""
Türkiye il (City) ve ilçe (Town) tablolarındaki mükerrer kayıtları temizler,
eksik illeri ve ilçeleri resmi/güncel listeye göre ekler.
Id'ler başka tablolarda kullanılabileceği için duplicate birleştirmede
en küçük Id korunur, diğer tablolardaki FK'lar bu Id'ye güncellenir.

Önce --dry-run ile deneyin. Canlıda çalıştırmadan önce veritabanı yedeği alın.

Kullanım:
  pip install -r scripts/requirements.txt
  python scripts/fix_turkey_cities_towns.py --dry-run
  python scripts/fix_turkey_cities_towns.py
  python scripts/fix_turkey_cities_towns.py --appsettings ecommerce.Admin/appsettings.json
  python scripts/fix_turkey_cities_towns.py --connection "Host=localhost;Database=MarketPlace;..."
"""

import argparse
import json
import os
import re
import sys
import urllib.request
from collections import defaultdict
from typing import List, Tuple

try:
    import psycopg2
    from psycopg2 import sql
except ImportError:
    print("psycopg2 gerekli: pip install psycopg2-binary")
    sys.exit(1)


# Referans: 81 il ve ilçeler (internetten güncel JSON - BuNickTamYirmiHarfli/turkey-cities-districts-json)
REFERENCE_JSON_URL = "https://raw.githubusercontent.com/BuNickTamYirmiHarfli/turkey-cities-districts-json/main/cities.json"


def normalize_name(name: str) -> str:
    """Karşılaştırma için isim normalize (boşluk trim, Türkçe uyumlu küçük harf)."""
    if not name:
        return ""
    s = name.strip()
    # Türkçe İ -> i, I -> ı için basit dönüşüm
    s = s.replace("İ", "i").replace("I", "ı")
    return s.lower()


def tr_title_case(name: str) -> str:
    """Baş harf büyük, diğerleri küçük (Türkçe: i->İ, ı->I, I->ı, İ->i)."""
    if not name or not name.strip():
        return name.strip() if name else ""
    s = name.strip()
    # İlk harf büyük (Türkçe)
    if s[0] == "i":
        first = "İ"
    elif s[0] == "ı":
        first = "I"
    else:
        first = s[0].upper()
    # Diğerleri küçük (Türkçe)
    rest = []
    for c in s[1:]:
        if c == "I":
            rest.append("ı")
        elif c == "İ":
            rest.append("i")
        else:
            rest.append(c.lower())
    return first + "".join(rest)


def load_reference_data():
    """İnternetten güncel il/ilçe listesini yükler."""
    try:
        with urllib.request.urlopen(REFERENCE_JSON_URL, timeout=15) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except Exception as e:
        print(f"Referans JSON yüklenemedi ({REFERENCE_JSON_URL}): {e}")
        print("Embedded fallback kullanılıyor (81 il, ilçe sayısı sınırlı olabilir).")
        data = _embedded_reference()
    return data


def _embedded_reference():
    """İnternet yoksa kullanılacak minimal 81 il listesi (ilçeler script çalışınca URL'den denenir)."""
    # Sadece 81 il adı; ilçeler için yine URL gerekir veya sadece il dedupe/eksik yapılır
    cities_81 = [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya",
        "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik",
        "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum",
        "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
        "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
        "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kırıkkale",
        "Kırklareli", "Kırşehir", "Kilis", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
        "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize",
        "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Sivas", "Şırnak", "Tekirdağ",
        "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
    ]
    return [{"id": i + 1, "name": n, "towns": []} for i, n in enumerate(cities_81)]


def parse_connection_string(conn_str: str) -> dict:
    """Npgsql tarzı connection string'i psycopg2 parametrelerine çevirir."""
    params = {}
    for part in conn_str.split(";"):
        part = part.strip()
        if not part:
            continue
        if "=" in part:
            k, v = part.split("=", 1)
            k, v = k.strip(), v.strip()
            if k == "Host":
                if ":" in v:
                    params["host"], params["port"] = v.rsplit(":", 1)
                    params["port"] = int(params["port"])
                else:
                    params["host"] = v
            elif k == "User ID":
                params["user"] = v
            elif k == "Password":
                params["password"] = v
            elif k == "Database":
                params["dbname"] = v
    return params


def get_connection(appsettings_path, connection_string):
    """appsettings.json veya doğrudan connection string ile bağlantı bilgisi döner."""
    if connection_string:
        return parse_connection_string(connection_string)
    path = appsettings_path or os.path.join(
        os.path.dirname(__file__), "..", "ecommerce.Admin", "appsettings.json"
    )
    path = os.path.abspath(path)
    if not os.path.isfile(path):
        raise FileNotFoundError(f"appsettings bulunamadı: {path}")
    with open(path, "r", encoding="utf-8") as f:
        cfg = json.load(f)
    conn_str = (cfg.get("ConnectionStrings") or {}).get("ApplicationDbContext")
    if not conn_str:
        raise ValueError("ConnectionStrings:ApplicationDbContext bulunamadı.")
    return parse_connection_string(conn_str)


def discover_fk_columns(conn) -> Tuple[List[Tuple[str, str]], List[Tuple[str, str]]]:
    """CityId / TownId (ve InvoiceCityId, InvoiceTownId) içeren tablo.kolon listesi."""
    with conn.cursor() as cur:
        cur.execute("""
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND column_name IN ('CityId', 'TownId', 'InvoiceCityId', 'InvoiceTownId')
            ORDER BY table_name, column_name
        """)
        rows = cur.fetchall()
    city_cols = [(t, c) for t, c in rows if c in ("CityId", "InvoiceCityId")]
    town_cols = [(t, c) for t, c in rows if c in ("TownId", "InvoiceTownId")]
    return city_cols, town_cols


def dedupe_cities(conn, city_fk_columns: List[Tuple[str, str]], dry_run: bool) -> dict:
    """Aynı isimdeki illeri birleştirir; (eski_id -> korunan_id) map döner."""
    with conn.cursor() as cur:
        cur.execute("SELECT \"Id\", \"Name\" FROM \"City\"")
        cities = cur.fetchall()
    by_key = defaultdict(list)  # normalize(name) -> [(id, name), ...]
    for id_, name in cities:
        by_key[normalize_name(name)].append((id_, name))

    id_remap = {}  # duplicate_id -> keep_id
    for key, group in by_key.items():
        if len(group) <= 1:
            continue
        group.sort(key=lambda x: x[0])
        keep_id = group[0][0]
        for dup_id, _ in group[1:]:
            id_remap[dup_id] = keep_id

    if not id_remap:
        return id_remap

    if not dry_run:
        for table_name, column_name in city_fk_columns:
            col = sql.Identifier(column_name)
            tbl = sql.Identifier(table_name)
            for old_id, new_id in id_remap.items():
                with conn.cursor() as cur:
                    cur.execute(
                        sql.SQL("UPDATE {} SET {} = %s WHERE {} = %s").format(tbl, col, col),
                        (new_id, old_id),
                    )
        with conn.cursor() as cur:
            for dup_id in id_remap:
                cur.execute('DELETE FROM "City" WHERE "Id" = %s', (dup_id,))
        conn.commit()
    return id_remap


def dedupe_towns(conn, town_fk_columns: List[Tuple[str, str]], dry_run: bool) -> dict:
    """Aynı (CityId, Name) ilçeleri birleştirir; (eski_id -> korunan_id) map."""
    with conn.cursor() as cur:
        cur.execute('SELECT "Id", "CityId", "Name" FROM "Town"')
        towns = cur.fetchall()
    by_key = defaultdict(list)  # (city_id, normalize(name)) -> [(id, ...), ...]
    for id_, city_id, name in towns:
        by_key[(city_id, normalize_name(name))].append((id_, city_id, name))

    id_remap = {}
    for key, group in by_key.items():
        if len(group) <= 1:
            continue
        group.sort(key=lambda x: x[0])
        keep_id = group[0][0]
        for dup_id, _, _ in group[1:]:
            id_remap[dup_id] = keep_id

    if not id_remap:
        return id_remap

    if not dry_run:
        for table_name, column_name in town_fk_columns:
            col = sql.Identifier(column_name)
            tbl = sql.Identifier(table_name)
            for old_id, new_id in id_remap.items():
                with conn.cursor() as cur:
                    cur.execute(
                        sql.SQL("UPDATE {} SET {} = %s WHERE {} = %s").format(tbl, col, col),
                        (new_id, old_id),
                    )
        with conn.cursor() as cur:
            for dup_id in id_remap:
                cur.execute('DELETE FROM "Town" WHERE "Id" = %s', (dup_id,))
        conn.commit()
    return id_remap


def add_missing_cities(conn, reference: List, dry_run: bool) -> int:
    """Referans listede olup DB'de olmayan illeri ekler."""
    with conn.cursor() as cur:
        cur.execute('SELECT "Name" FROM "City"')
        existing = {normalize_name(r[0]) for r in cur.fetchall()}
    added = 0
    for city in reference:
        name = (city.get("name") or "").strip()
        if not name or normalize_name(name) in existing:
            continue
        if not dry_run:
            with conn.cursor() as cur:
                cur.execute('INSERT INTO "City" ("Name") VALUES (%s)', (tr_title_case(name),))
                conn.commit()
        existing.add(normalize_name(name))
        added += 1
    return added


def add_missing_towns(conn, reference: List, dry_run: bool) -> int:
    """Referans listede olup DB'de olmayan ilçeleri ekler (il adına göre CityId bulunur)."""
    with conn.cursor() as cur:
        cur.execute('SELECT "Id", "Name" FROM "City"')
        city_id_by_name = {normalize_name(n): id_ for id_, n in cur.fetchall()}
    with conn.cursor() as cur:
        cur.execute('SELECT "CityId", "Name" FROM "Town"')
        existing_towns = {(cid, normalize_name(n)) for cid, n in cur.fetchall()}

    added = 0
    for city in reference:
        city_name = city.get("name")
        if not city_name:
            continue
        city_id = city_id_by_name.get(normalize_name(city_name))
        if not city_id:
            continue
        for town in city.get("towns") or []:
            town_name = (town.get("name") or "").strip()
            if not town_name:
                continue
            if (city_id, normalize_name(town_name)) in existing_towns:
                continue
            if not dry_run:
                with conn.cursor() as cur:
                    cur.execute(
                        'INSERT INTO "Town" ("CityId", "Name") VALUES (%s, %s)',
                        (city_id, tr_title_case(town_name)),
                    )
                    conn.commit()
            existing_towns.add((city_id, normalize_name(town_name)))
            added += 1
    return added


def normalize_name_cases(conn, dry_run: bool) -> Tuple[int, int]:
    """Tüm il ve ilçe isimlerini 'baş harf büyük diğerleri küçük' (Türkçe) yapar."""
    updated_cities = 0
    updated_towns = 0
    with conn.cursor() as cur:
        cur.execute('SELECT "Id", "Name" FROM "City"')
        to_update_city = []
        for row in cur.fetchall():
            id_, name = row[0], row[1]
            new_name = tr_title_case(name)
            if new_name != name:
                updated_cities += 1
                to_update_city.append((new_name, id_))
        if to_update_city and not dry_run:
            for new_name, id_ in to_update_city:
                cur.execute('UPDATE "City" SET "Name" = %s WHERE "Id" = %s', (new_name, id_))
            conn.commit()
    with conn.cursor() as cur:
        cur.execute('SELECT "Id", "Name" FROM "Town"')
        to_update_town = []
        for row in cur.fetchall():
            id_, name = row[0], row[1]
            new_name = tr_title_case(name)
            if new_name != name:
                updated_towns += 1
                to_update_town.append((new_name, id_))
        if to_update_town and not dry_run:
            for new_name, id_ in to_update_town:
                cur.execute('UPDATE "Town" SET "Name" = %s WHERE "Id" = %s', (new_name, id_))
            conn.commit()
    return updated_cities, updated_towns


def main():
    parser = argparse.ArgumentParser(description="Türkiye City/Town mükerrer ve eksik kayıt düzeltme")
    parser.add_argument(
        "--appsettings",
        default=None,
        help="appsettings.json dosya yolu (varsayılan: ../ecommerce.Admin/appsettings.json)",
    )
    parser.add_argument(
        "--connection",
        default=None,
        help="Doğrudan connection string (appsettings yerine)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Değişiklik yapmadan raporla",
    )
    args = parser.parse_args()

    try:
        conn_params = get_connection(args.appsettings, args.connection)
    except Exception as e:
        print(f"Bağlantı ayarı hatası: {e}")
        sys.exit(1)

    print("Referans il/ilçe listesi yükleniyor...")
    reference = load_reference_data()
    if not reference:
        print("Referans veri yok, çıkılıyor.")
        sys.exit(1)
    print(f"  {len(reference)} il, toplam ilçe: {sum(len(c.get('towns') or []) for c in reference)}")

    print("Veritabanına bağlanılıyor...")
    try:
        conn = psycopg2.connect(**conn_params)
    except Exception as e:
        print(f"Bağlantı hatası: {e}")
        sys.exit(1)

    try:
        city_fk, town_fk = discover_fk_columns(conn)
        print(f"  CityId/TownId referansları: {len(city_fk)} kolon (City), {len(town_fk)} kolon (Town)")

        print("Mükerrer iller temizleniyor...")
        city_remap = dedupe_cities(conn, city_fk, args.dry_run)
        print(f"  Birleştirilen: {len(city_remap)} duplicate il (korunan Id'lere yönlendirildi)")

        print("Mükerrer ilçeler temizleniyor...")
        town_remap = dedupe_towns(conn, town_fk, args.dry_run)
        print(f"  Birleştirilen: {len(town_remap)} duplicate ilçe")

        print("Eksik iller ekleniyor...")
        added_cities = add_missing_cities(conn, reference, args.dry_run)
        print(f"  Eklenen il: {added_cities}")

        print("Eksik ilçeler ekleniyor...")
        added_towns = add_missing_towns(conn, reference, args.dry_run)
        print(f"  Eklenen ilçe: {added_towns}")

        print("İl ve ilçe isimleri düzeltiliyor (baş harf büyük, diğerleri küçük)...")
        upd_cities, upd_towns = normalize_name_cases(conn, args.dry_run)
        print(f"  Güncellenen il: {upd_cities}, ilçe: {upd_towns}")

        if args.dry_run:
            print("\n[DRY-RUN] Hiçbir değişiklik yapılmadı.")
    finally:
        conn.close()

    print("Tamamlandı.")


if __name__ == "__main__":
    main()
