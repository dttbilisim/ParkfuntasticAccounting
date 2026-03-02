#!/usr/bin/env python3
"""
Uzak kaynaktan banka ve şube listesini çeker, mevcut City (il) ve Town (ilçe) tablolarıyla
eşleştirip Bank ve BankBranches tablolarına yazar.

Kullanım:
  python3 seed_bank_branches.py
    -> appsettings'ten okur (scripts/../ecommerce.Admin/appsettings.json veya APP_SETTINGS_PATH)
  CONNECTION_STRING="..." python3 seed_bank_branches.py
  python3 seed_bank_branches.py "Host=...;Database=...;..."
  Uzak kaynağa erişilemiyorsa (DNS ağı): XML'i indirip yerel yol verin:
  TCMB_XML_PATH=./bankaSubeTumListe.xml python3 seed_bank_branches.py

Gereksinim: pip install psycopg2-binary
"""

import json
import os
import re
import sys
import builtins
import xml.etree.ElementTree as ET
from urllib.request import urlopen, Request
from urllib.error import URLError
from datetime import datetime

try:
    import psycopg2
except ImportError:
    print("psycopg2-binary gerekli: pip install psycopg2-binary")
    sys.exit(1)

# Banka/şube listesi kaynağı
BANK_BRANCHES_XML_URL = "https://mobilteg.com.tr/downloads/Bankalar/bankaSubeTumListe.xml"
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
LOG_FILE_PATH = os.environ.get("SEED_BANK_BRANCHES_LOG_PATH") or os.path.join(SCRIPT_DIR, "seed_bank_branches.log")

def print(*args, **kwargs):
    """
    Script loglarını hem terminale hem dosyaya yazar.
    Özel yol için: SEED_BANK_BRANCHES_LOG_PATH=/tmp/seed.log
    """
    sep = kwargs.get("sep", " ")
    end = kwargs.get("end", "\n")
    message = sep.join(str(a) for a in args)
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] {message}"

    builtins.print(line, end=end, flush=True)
    try:
        with open(LOG_FILE_PATH, "a", encoding="utf-8") as f:
            f.write(line + end)
    except Exception:
        # Log dosyasına yazılamasa da script çalışmaya devam etsin.
        pass

# Türkçe karakter normalizasyonu (il/ilçe eşlemesi için)
TR_NORMALIZE = str.maketrans(
    "İIıiĞğÜüŞşÖöÇç",
    "iiiiGgUuSsOoCc"
)

def tr_lower(s):
    if not s:
        return ""
    return s.replace("İ", "i").replace("I", "ı").lower()

def tr_title_case(s):
    if not s:
        return ""
    s = re.sub(r"\s+", " ", s.strip())
    parts = []
    for word in s.split(" "):
        lw = tr_lower(word)
        if not lw:
            continue
        first = lw[0]
        first_up = "İ" if first == "i" else ("I" if first == "ı" else first.upper())
        parts.append(first_up + lw[1:])
    return " ".join(parts)

def normalize_name(s):
    if not s:
        return ""
    s = s.strip().upper().translate(TR_NORMALIZE)
    return re.sub(r"\s+", " ", s)

def parse_connection_string(cs):
    """Host=...;Database=...;User ID=...;Password=... -> dict"""
    d = {}
    for part in cs.split(";"):
        part = part.strip()
        if not part:
            continue
        if "=" in part:
            k, v = part.split("=", 1)
            d[k.strip()] = v.strip()
    return d

def load_connection_string_from_appsettings():
    """appsettings.json'dan ApplicationDbContext connection string'i oku."""
    path = os.environ.get("APP_SETTINGS_PATH")
    if not path:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        path = os.path.join(script_dir, "..", "ecommerce.Admin", "appsettings.json")
    path = os.path.normpath(path)
    if not os.path.isfile(path):
        return None
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
        conn = (data.get("ConnectionStrings") or {}).get("ApplicationDbContext")
        return conn if isinstance(conn, str) and conn.strip() else None
    except Exception:
        return None

def conn_from_env_or_arg():
    cs = os.environ.get("CONNECTION_STRING") or (sys.argv[1] if len(sys.argv) > 1 else None)
    if not cs:
        cs = load_connection_string_from_appsettings()
        if cs:
            print("Bağlantı appsettings.json'dan alındı (ecommerce.Admin).")
    if not cs:
        print("Bağlantı gerekli: CONNECTION_STRING ortam değişkeni, ilk argüman veya appsettings.json (ApplicationDbContext).")
        sys.exit(1)
    params = parse_connection_string(cs)
    host = params.get("Host", params.get("Server", "localhost"))
    if ":" in host:
        host, port = host.rsplit(":", 1)
    else:
        port = params.get("Port", "5432")
    return {
        "host": host,
        "port": int(port) if str(port).isdigit() else 5432,
        "dbname": params.get("Database", ""),
        "user": params.get("User ID", params.get("Username", params.get("User", ""))),
        "password": params.get("Password", ""),
    }

def fetch_tcmb_xml():
    # Yerel dosya: kaynağa erişilemiyorsa XML'i indirip TCMB_XML_PATH ile verin
    local_path = os.environ.get("TCMB_XML_PATH", "").strip()
    if local_path:
        if not os.path.isfile(local_path):
            print("Hata: TCMB_XML_PATH ile verilen dosya bulunamadı:", local_path)
            print("curl bu ağdan kaynağa ulaşamadığı için dosya oluşmamış olabilir.")
            print("XML'i tarayıcıda veya Türkiye VPN ile indirip bu yola kaydedin.")
            print("Test için örnek dosya: TCMB_XML_PATH=./scripts/bankaSubeTumListe.sample.xml")
            sys.exit(1)
        raw = None
        with open(local_path, "rb") as f:
            raw = f.read()
        if not raw or not raw.strip():
            print("Hata: Dosya boş:", local_path)
            sys.exit(1)
        print("Banka/şube listesi yerel dosyadan okunuyor:", local_path)
        return raw
    print("Banka/şube listesi indiriliyor:", BANK_BRANCHES_XML_URL)
    req = Request(BANK_BRANCHES_XML_URL, headers={"User-Agent": "Mozilla/5.0"})
    try:
        with urlopen(req, timeout=30) as r:
            return r.read()
    except URLError:
        pass
    print("Banka/şube listesi indirilemedi:", BANK_BRANCHES_XML_URL)
    print("Yapmanız gereken: XML dosyasını erişebilen bir yerden indirin (Türkiye VPN veya başka bilgisayar),")
    print("proje klasörüne koyun ve şu komutla çalıştırın:")
    print("  TCMB_XML_PATH=./bankaSubeTumListe.xml python3 scripts/seed_bank_branches.py")
    sys.exit(1)

def parse_tcmb_banks_and_branches(xml_bytes):
    """XML'den banka ve şube listesi çıkar."""
    root = ET.fromstring(xml_bytes)
    banks = []
    branches = []

    def first_text(el, tags=(), attrs=()):
        if el is None:
            return ""
        for tag in tags:
            node = el.find(f"./{{*}}{tag}")
            if node is not None and node.text and node.text.strip():
                return node.text.strip()
        for attr in attrs:
            val = el.get(attr)
            if val and str(val).strip():
                return str(val).strip()
        return ""

    # Yeni şema (mobilteg): bankaSubeTumListe -> bankaSubeleri -> banka/sube
    # Tag örnekleri: bKd,bAd,sKd,sAd,sIlAd,sIlcAd,adr
    bank_groups = root.findall(".//{*}bankaSubeleri")
    if bank_groups:
        city_name_by_code = {}
        town_name_by_code = {}
        for group in bank_groups:
            for sube in group.findall("./{*}sube"):
                city_code = first_text(sube, tags=("sIlKd",))
                city_name = first_text(sube, tags=("sIlAd",))
                town_code = first_text(sube, tags=("sIlcKd",))
                town_name = first_text(sube, tags=("sIlcAd",))
                if city_code and city_name:
                    city_name_by_code[city_code] = city_name
                if city_code and town_code and town_name:
                    town_name_by_code[(city_code, town_code)] = town_name

        seen_bank_codes = set()
        for group in bank_groups:
            bank_el = group.find("./{*}banka")
            bank_code_raw = first_text(bank_el, tags=("bKd", "BankaKodu"), attrs=("Kod", "BankaKodu"))
            bank_name = first_text(bank_el, tags=("bAd", "BankaAdi"), attrs=("Ad", "BankaAdi"))
            try:
                bank_code = int(bank_code_raw) if bank_code_raw and bank_code_raw.isdigit() else (hash(bank_code_raw or bank_name) % 100000)
            except Exception:
                bank_code = hash(bank_code_raw or bank_name) % 100000

            if bank_code not in seen_bank_codes:
                banks.append((bank_code, bank_name or bank_code_raw or "Banka"))
                seen_bank_codes.add(bank_code)

            for sube in group.findall("./{*}sube"):
                sc = first_text(sube, tags=("sKd", "SubeKodu"), attrs=("SubeKodu",))
                sn = first_text(sube, tags=("sAd", "SubeAdi"), attrs=("SubeAdi",))
                city_code = first_text(sube, tags=("sIlKd",))
                town_code = first_text(sube, tags=("sIlcKd",))
                il = first_text(sube, tags=("sIlAd", "IlAdi"), attrs=("IlAdi",)) or city_name_by_code.get(city_code, "")
                ilce = first_text(sube, tags=("sIlcAd", "IlceAdi"), attrs=("IlceAdi",)) or town_name_by_code.get((city_code, town_code), "")
                adres = first_text(sube, tags=("adr", "Adres"), attrs=("Adres",))
                branches.append((bank_code, sc, sn or sc or "Şube", il, ilce, adres[:500]))

        if banks:
            return banks, branches

    # TCMB yapısı: ListeBankaSube -> Banka (BankaKodu, BankaAdi) -> SubeListesi -> Sube (SubeKodu, SubeAdi, IlAdi, IlceAdi, Adres)
    for bank_el in root.findall(".//{*}Banka"):
        code_el = bank_el.find(".//{*}BankaKodu")
        name_el = bank_el.find(".//{*}BankaAdi")
        code = (code_el.text or "").strip() if code_el is not None else ""
        name = (name_el.text or "").strip() if name_el is not None else ""
        if not code and not name:
            code = bank_el.get("Kod") or bank_el.get("BankaKodu") or ""
            name = bank_el.get("Ad") or bank_el.get("BankaAdi") or ""
        try:
            bank_code = int(code) if code and code.isdigit() else (hash(code or name) % 100000)
        except Exception:
            bank_code = hash(code or name) % 100000
        banks.append((bank_code, name or code or "Banka"))

        for sube in bank_el.findall(".//{*}Sube"):
            sc = sube.find(".//{*}SubeKodu")
            sn = sube.find(".//{*}SubeAdi")
            il = sube.find(".//{*}IlAdi")
            ilce = sube.find(".//{*}IlceAdi")
            adres = sube.find(".//{*}Adres")
            sc = (sc.text or "").strip() if sc is not None else (sube.get("SubeKodu") or "")
            sn = (sn.text or "").strip() if sn is not None else (sube.get("SubeAdi") or "")
            il = (il.text or "").strip() if il is not None else (sube.get("IlAdi") or "")
            ilce = (ilce.text or "").strip() if ilce is not None else (sube.get("IlceAdi") or "")
            adres = (adres.text or "").strip() if adres is not None else (sube.get("Adres") or "")
            branches.append((bank_code, sc, sn or sc or "Şube", il, ilce, adres[:500]))

    # Alternatif tag isimleri (namespace olmadan)
    if not banks:
        for bank_el in root.iter():
            tag = (bank_el.tag or "").split("}")[-1]
            if tag != "Banka":
                continue
            code_el = bank_el.find(".//*[local-name()='BankaKodu']")
            name_el = bank_el.find(".//*[local-name()='BankaAdi']")
            code = (code_el.text or "").strip() if code_el is not None else (bank_el.get("Kod") or "")
            name = (name_el.text or "").strip() if name_el is not None else (bank_el.get("Ad") or "")
            try:
                bank_code = int(code) if code and code.isdigit() else (hash(code or name) % 100000)
            except Exception:
                bank_code = hash(code or name) % 100000
            banks.append((bank_code, name or code or "Banka"))
            for sube in bank_el.findall(".//*[local-name()='Sube']"):
                def text(e, attr):
                    if e is not None and e.text:
                        return e.text.strip()
                    return sube.get(attr, "")
                sc = text(sube.find(".//*[local-name()='SubeKodu']"), "SubeKodu")
                sn = text(sube.find(".//*[local-name()='SubeAdi']"), "SubeAdi")
                il = text(sube.find(".//*[local-name()='IlAdi']"), "IlAdi")
                ilce = text(sube.find(".//*[local-name()='IlceAdi']"), "IlceAdi")
                adres = text(sube.find(".//*[local-name()='Adres']"), "Adres")
                branches.append((bank_code, sc, sn or sc or "Şube", il, ilce, adres[:500]))
            break

    return banks, branches

def load_cities_towns(conn):
    cur = conn.cursor()
    cur.execute('SELECT "Id", "Name" FROM "City"')
    cities = [(r[0], normalize_name(r[1])) for r in cur.fetchall()]
    cur.execute('SELECT "Id", "CityId", "Name" FROM "Town"')
    towns = [(r[0], r[1], normalize_name(r[2])) for r in cur.fetchall()]
    cur.close()
    return cities, towns

def find_city_id(cities, il_adi):
    n = normalize_name(il_adi)
    if not n:
        return None
    for cid, cname in cities:
        if cname == n or n in cname or cname in n:
            return cid
    return None

def find_town_id(towns, city_id, ilce_adi):
    if not city_id or not ilce_adi:
        return None
    n = normalize_name(ilce_adi)
    if not n:
        return None
    for tid, cid, tname in towns:
        if cid == city_id and (tname == n or n in tname or tname in n):
            return tid
    return None

def main():
    pg = conn_from_env_or_arg()
    print("Veritabanına bağlanılıyor:", pg["host"], pg["port"], pg["dbname"])
    conn = psycopg2.connect(**pg)
    conn.autocommit = False

    xml_bytes = fetch_tcmb_xml()
    banks, branches = parse_tcmb_banks_and_branches(xml_bytes)

    if not banks:
        print("XML'den banka bulunamadı. TCMB formatı farklı olabilir.")
        conn.close()
        sys.exit(1)

    print("Banka sayısı:", len(banks), "Şube sayısı:", len(branches))
    print("City ve Town yükleniyor...")
    cities, towns = load_cities_towns(conn)
    print("  City:", len(cities), "Town:", len(towns))

    cur = conn.cursor()

    # Mevcut bankaları kod ve isim ile yükle
    cur.execute('SELECT "Id", "Name", "BankCode" FROM "Banks"')
    existing_by_name = {}
    existing_by_code = {}
    for r in cur.fetchall():
        bank_id, bank_name, bank_code_existing = r[0], r[1], r[2]
        if bank_code_existing is not None and bank_code_existing not in existing_by_code:
            existing_by_code[bank_code_existing] = (bank_id, bank_name)
        n = normalize_name(bank_name or "")
        if n and n not in existing_by_name:
            existing_by_name[n] = (bank_id, bank_name)

    bank_id_by_code = {}
    for bank_code, bank_name in banks:
        raw_name = (bank_name or str(bank_code)).strip() or "Banka"
        display_name = tr_title_case(raw_name)
        system_name = display_name.replace(" ", "_")[:100]
        n = normalize_name(display_name)
        if bank_code in existing_by_code:
            bank_id = existing_by_code[bank_code][0]
            cur.execute(
                """UPDATE "Banks" SET "BankCode"=%s, "Name"=%s, "SystemName"=%s, "UpdateDate"=NOW() WHERE "Id"=%s""",
                (bank_code, display_name, system_name, bank_id)
            )
            bank_id_by_code[bank_code] = bank_id
            if n:
                existing_by_name[n] = (bank_id, display_name)
            print("  Banka güncellendi:", display_name, "Kod:", bank_code)
        elif n and n in existing_by_name:
            bank_id = existing_by_name[n][0]
            cur.execute(
                """UPDATE "Banks" SET "BankCode"=%s, "Name"=%s, "SystemName"=%s, "UpdateDate"=NOW() WHERE "Id"=%s""",
                (bank_code, display_name, system_name, bank_id)
            )
            bank_id_by_code[bank_code] = bank_id
            existing_by_code[bank_code] = (bank_id, display_name)
            print("  Banka güncellendi:", display_name, "Kod:", bank_code)
        else:
            cur.execute(
                """INSERT INTO "Banks" ("Name", "SystemName", "BankCode", "LogoPath", "UseCommonPaymentPage", "DefaultBank", "Active", "CreateDate", "UpdateDate")
                   VALUES (%s, %s, %s, %s, false, false, true, NOW(), NOW())
                   RETURNING "Id" """,
                (display_name, system_name, bank_code, "")
            )
            row = cur.fetchone()
            if row:
                bank_id = row[0]
                bank_id_by_code[bank_code] = bank_id
                if n:
                    existing_by_name[n] = (bank_id, display_name)
                existing_by_code[bank_code] = (bank_id, display_name)
                print("  Banka eklendi:", display_name, "Kod:", bank_code)
            else:
                cur.execute('SELECT "Id" FROM "Banks" WHERE "Name" = %s', (display_name,))
                r = cur.fetchone()
                if r:
                    bank_id_by_code[bank_code] = r[0]

    conn.commit()
    print("Banka değişiklikleri veritabanına yazıldı (commit).")

    cur.execute('SELECT "BankId", "Code", "CityId" FROM "BankBranches"')
    existing_branches = {(r[0], (r[1] or "").strip(), r[2]) for r in cur.fetchall()}

    inserted = 0
    skipped_no_city = 0
    COMMIT_EVERY = 500
    for bank_code, sube_code, sube_name, il_adi, ilce_adi, adres in branches:
        bank_id = bank_id_by_code.get(bank_code)
        if not bank_id:
            continue
        city_id = find_city_id(cities, il_adi)
        if not city_id:
            skipped_no_city += 1
            continue
        town_id = find_town_id(towns, city_id, ilce_adi)
        key = (bank_id, sube_code or sube_name, city_id)
        if key in existing_branches:
            continue
        branch_name = tr_title_case(sube_name or sube_code or "Şube")
        cur.execute(
            """INSERT INTO "BankBranches" ("BankId", "CityId", "TownId", "Name", "Code", "Address", "Phone", "Active")
               VALUES (%s, %s, %s, %s, %s, %s, %s, true)""",
            (bank_id, city_id, town_id, branch_name[:200], (sube_code[:50] if sube_code else None), adres[:500] if adres else None, None)
        )
        existing_branches.add(key)
        inserted += 1
        if inserted % COMMIT_EVERY == 0:
            conn.commit()
            print("  Şube:", inserted, "(ara commit yapıldı)")

    conn.commit()
    print("Tüm değişiklikler veritabanına yazıldı (commit).")
    print("Toplam şube eklendi:", inserted)
    if skipped_no_city:
        print("(İl eşleşmediği için atlanan şube:", skipped_no_city, ")")
    cur.close()
    conn.close()
    print("Bitti.")

if __name__ == "__main__":
    main()
