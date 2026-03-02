#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Autoparts-24 OEM -> OemToCars (PostgreSQL) yazıcı (INSERT + UPDATE, tam sürüm)
- ProductGroupCodes tablosundan GroupCode'ları 1000'erlik batch'lerle çeker.
- OEM'leri uniq üretir.
- Her OEM için arama -> detay -> alanları çıkarır.
- Brand/Model/Engine: önce KV'den, yoksa Title'dan türet + temizlik (boş döndürmez).
- MileageKm/Year/Price gibi sayısallar sayı değilse NULL gönderilir.
- UPSERT + row-level rollback; CreatedDate/ModifiedDate otomatik set.
- Log: console + oem2cars.log
"""

import asyncio, os, re, json, logging, random
from typing import List, Dict, Any, Optional, Iterable, Set
from urllib.parse import urlencode, urljoin

import psycopg2
from bs4 import BeautifulSoup
from playwright.async_api import async_playwright, TimeoutError as PwTimeout

# ================== AYARLAR ==================
BASE = "https://www.autoparts-24.com"
HEADLESS = True
DEBUG = False
DEBUG_DIR = "/tmp/oem_debug"
os.makedirs(DEBUG_DIR, exist_ok=True)

PG_CFG = dict(
    host="localhost",
    port=5454,
    database="MarketPlace",
    user="myinsurer",
    password="Posmdh0738",
)

DB_GROUPCODE_BATCH = 1000
DB_INSERT_BATCH = 200
CREATED_ID_DEFAULT = 1

TEXT_FIELDS = [
    "OEM","Title","Brand","Model","Engine","EngineCode","Fuel","Gearbox","Body","Chassis",
    "VIN","Position","Years","ArticleNumber","OriginalNumbers","Currency",
    "ImageUrl","SourceUrl","Source"
]

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s | %(message)s",
    handlers=[logging.StreamHandler(),
              logging.FileHandler("oem2cars.log", encoding="utf-8")]
)

UA_POOL = [
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
]

# ---------------- yardımcılar ----------------
def clean(s: str) -> str:
    return re.sub(r"\s+", " ", (s or "").strip())

def ensure_abs(href: str) -> str:
    try:
        return urljoin(BASE, href or "")
    except Exception:
        return href or ""

def split_values(val: str) -> List[str]:
    if not val: return []
    parts = re.split(r"[;,/\|]", val)
    return [clean(p) for p in parts if clean(p)]

def norm_key(k: str) -> str:
    k0 = clean(k).lower()
    repl = {
        "article number": "article_number",
        "original number": "original_number",
        "original numbers": "original_number",
        "oem number": "original_number",
        "oem": "original_number",
        "brand": "brand",
        "model": "model",
        "engine code": "engine_code",
        "engine": "engine",
        "fuel": "fuel",
        "fuel type": "fuel",
        "gearbox": "gearbox",
        "transmission": "gearbox",
        "body": "body",
        "body type": "body",
        "chassis": "chassis",
        "chassis number": "chassis",
        "vin": "vin",
        "vin number": "vin",
        "year": "year",
        "years": "years",
        "position": "position",
        "mileage": "mileage_km",
        "mileage km": "mileage_km",
        "price": "price",
        "currency": "currency",
    }
    for key, val in repl.items():
        if key in k0:
            return val
    return k0.replace(" ", "_").replace(":", "")

# ---------- NORMALİZE: Brand / Model / Engine ----------
_word_re = re.compile(r"[A-Za-zÇĞİÖŞÜçğıöşü0-9\-\._]+", re.UNICODE)

def normalize_brand_str(s: str) -> str:
    s = clean(s)
    if not s: return ""
    s = re.split(r"[;,\|/]", s)[0]
    m = re.search(r"[A-Za-zÇĞİÖŞÜçğıöşü]+[A-Za-zÇĞİÖŞÜçğıöşü0-9\-]*", s)
    if not m: return ""
    token = m.group(0)
    return token.upper() if len(token) <= 3 else token.capitalize()

def normalize_model_str(s: str) -> str:
    s = clean(s)
    if not s: return ""
    s = re.split(r"[;,\|/]", s)[0]
    s = re.sub(r"\(.*?\)", "", s).strip()
    tokens = _word_re.findall(s)
    if not tokens: return ""
    keep = tokens[:2]
    out = " ".join(keep)
    return out.upper() if len(out) <= 8 else out

def normalize_engine_str(s: str) -> str:
    s = clean(s)
    if not s: return ""
    s = re.split(r"[;,\|/]", s)[0]
    m = re.search(r"\b[ABCEGHJKLMNPRSTVWXYZ][A-Z0-9\-\.]{2,}\b", s)
    if m:
        return m.group(0).upper()
    m = re.search(r"\b\d\.\d\s*[A-Za-z]{2,4}\b", s)
    if m:
        return m.group(0).upper()
    tokens = _word_re.findall(s)
    return tokens[0].upper() if tokens else ""

def normalize_brand(brand_raw: str) -> str:
    return normalize_brand_str(brand_raw)

def normalize_model(model_raw: str) -> str:
    return normalize_model_str(model_raw)

def normalize_engine(engine_raw: str) -> str:
    return normalize_engine_str(engine_raw)

# ---- Title fallback ----
def extract_brand_model_from_title(title: str) -> (str, str):
    t = clean(title)
    if not t: return "", ""
    parts = [p.strip() for p in re.split(r"\s*-\s*", t) if p.strip()]
    if len(parts) >= 2:
        brand = normalize_brand_str(parts[0])
        model = normalize_model_str(parts[1])
        return brand, model
    return "", ""

def extract_engine_from_text(text: str) -> str:
    return normalize_engine_str(text or "")

# ------------- SAYISAL PARSE (NULL güvenli) -------------
def parse_int_null(s: str) -> Optional[int]:
    if not s: return None
    m = re.search(r"\d{1,3}(?:[ .]\d{3})+|\d+", s)
    if not m: return None
    num = m.group(0).replace(" ", "").replace(".", "")
    try:
        return int(num)
    except Exception:
        return None

def parse_year_null(text: str) -> Optional[int]:
    if not text: return None
    text = clean(text)
    rng = re.search(r"\b(19\d{2}|20\d{2})\s*[-/–]\s*(19\d{2}|20\d{2})\b", text)
    if rng:
        return None
    m = re.search(r"\b(19\d{2}|20\d{2})\b", text)
    return int(m.group(1)) if m else None

def parse_price_null(text: str) -> Optional[float]:
    if not text: return None
    txt = clean(text)
    nums = re.findall(r"[\d\.,]+", txt)
    if not nums: return None
    raw = nums[0]
    if "." in raw and "," in raw:
        raw = raw.replace(".", "").replace(",", ".")
    else:
        if "," in raw and re.search(r",\d{1,3}$", raw):
            raw = raw.replace(",", ".")
        else:
            raw = raw.replace(",", "")
    try:
        return float(raw)
    except Exception:
        return None

# --------- DB: GroupCode -> OEM (1000'er batch) ----
def parse_groupcode(gc: str) -> List[str]:
    if not gc: return []
    parts = str(gc).split("|")
    right = parts[2] if len(parts) > 2 else ""
    oems: List[str] = []
    for token in (right or "").split("-"):
        t = token.strip()
        if t and t not in oems:
            oems.append(t)
    return oems

def get_groupcode_total(conn) -> int:
    with conn.cursor() as cur:
        cur.execute('SELECT COUNT(*) FROM "ProductGroupCodes" WHERE "Status" <> 99;')
        return int(cur.fetchone()[0])

def iter_oems_from_db_chunked(batch_size: int = DB_GROUPCODE_BATCH) -> Iterable[str]:
    conn = psycopg2.connect(**PG_CFG)
    conn.autocommit = True
    seen: Set[str] = set()
    try:
        total = get_groupcode_total(conn)
        logging.info(f"ProductGroupCodes toplam satır: {total:,}")
        for offset in range(0, total, batch_size):
            with conn.cursor() as cur:
                cur.execute(
                    'SELECT "GroupCode" FROM "ProductGroupCodes" WHERE "Status" <> 99 LIMIT %s OFFSET %s',
                    (batch_size, offset)
                )
                rows = cur.fetchall()
            batch_oems: List[str] = []
            for (gc,) in rows:
                for o in parse_groupcode(gc):
                    if o not in seen:
                        seen.add(o)
                        batch_oems.append(o)
            logging.info(f"Batch OEM üretildi: {len(batch_oems)} (offset={offset})")
            for o in batch_oems:
                yield o
    finally:
        conn.close()

# -------------- Cookie / Ülke kabulü --------------
async def accept_everything(page):
    for sel in [
        "#onetrust-accept-btn-handler", "button#onetrust-accept-btn-handler",
        "button:has-text('Accept all')", "button:has-text('Accept')",
        "button:has-text('I agree')", "button:has-text('Kabul Et')",
    ]:
        try:
            el = page.locator(sel)
            if await el.count():
                await el.first.click(timeout=1500)
                await page.wait_for_timeout(200)
        except Exception:
            pass

# ---------------- Liste -> Link çıkar --------------
async def get_list_links(page, oem: str) -> List[str]:
    url = f"{BASE}/search/1/?{urlencode({'q': oem, 'type': 'oemcode'})}"
    await page.goto(url, wait_until="networkidle", timeout=60000)
    await accept_everything(page)
    html = await page.content()
    s = BeautifulSoup(html, "lxml")
    candidates = [
        "ul.productList li.productList_item a.link---productList__title",
        "a.link---productList__title",
        "a[href*='/item/']",
    ]
    links: List[str] = []
    for css in candidates:
        for a in s.select(css):
            href = a.get("href")
            if href:
                links.append(ensure_abs(href))
    return list(dict.fromkeys(links))

# --------------- Detay parsleme -------------------

def _merge_kv(data: Dict[str, Any], k: str, v: str):
    nk = norm_key(k)
    if not nk: return
    prev = data.get(nk)
    v = clean(v)
    if not v: return
    data[nk] = f"{prev}; {v}" if prev and prev != v else v

def parse_key_value_blocks(soup: BeautifulSoup) -> Dict[str, Any]:
    data: Dict[str, Any] = {}
    data["_type_raw"] = ""

    # Only allow explicit, clean labels for certain fields
    ALLOWED_SYSTEM_LABELS = {"Fuel", "Gearbox"}  # Only these for system-like fields
    ALLOWED_TYPE_LABELS = {"Body type", "Fuel type"}  # Only these for type-like fields

    def should_exclude_key(k: str, v: str) -> bool:
        # Exclude keys like "system", "type", "number" (case-insensitive) except for specific allowed cases.
        kl = clean(k).lower()
        # Exclude "system" unless key is exactly "fuel" or "gearbox"
        if kl == "system":
            return True
        if "system" in kl and kl not in [x.lower() for x in ALLOWED_SYSTEM_LABELS]:
            return True
        # Exclude "type" unless key is exactly allowed
        if kl == "type":
            return True
        if "type" in kl and kl not in [x.lower() for x in ALLOWED_TYPE_LABELS]:
            return True
        # Exclude "number" unless key is exactly "vin" or "chassis number"
        if kl == "number":
            return True
        if kl not in ("vin", "chassis number") and "number" in kl:
            return True
        return False

    # TABLE
    for tr in soup.select("table tr"):
        key_el = tr.select_one("th, td:nth-of-type(1)")
        val_el = tr.select_one("td:nth-of-type(2)")
        if key_el and val_el:
            k, v = key_el.get_text(), val_el.get_text()
            klower = clean(k).lower()
            # Accumulate "type" keys into _type_raw
            if "type" in klower:
                prev = data.get("_type_raw", "")
                val = clean(v)
                if val:
                    data["_type_raw"] = (prev + "; " + val).strip("; ") if prev else val
                # do not merge this key into normalized fields; continue to next item
                continue
            # For VIN: only accept keys that are exactly "VIN" or "Chassis number" (case-insensitive)
            if klower in ("vin", "chassis number"):
                _merge_kv(data, k, v)
            elif not should_exclude_key(k, v):
                _merge_kv(data, k, v)

    # DL
    for dl in soup.select("dl"):
        dts = dl.select("dt")
        dds = dl.select("dd")
        for dt, dd in zip(dts, dds):
            k, v = dt.get_text(), dd.get_text()
            klower = clean(k).lower()
            if "type" in klower:
                prev = data.get("_type_raw", "")
                val = clean(v)
                if val:
                    data["_type_raw"] = (prev + "; " + val).strip("; ") if prev else val
                continue
            if klower in ("vin", "chassis number"):
                _merge_kv(data, k, v)
            elif not should_exclude_key(k, v):
                _merge_kv(data, k, v)

    # UL/LI "Key: Value" veya "Key - Value"
    for li in soup.select("ul li"):
        txt = clean(li.get_text(" ", strip=True))
        m = re.match(r"(.{2,40}?)[\s:\-–]+(.+)$", txt)
        if m:
            k, v = m.group(1), m.group(2)
            klower = clean(k).lower()
            if "type" in klower:
                prev = data.get("_type_raw", "")
                val = clean(v)
                if val:
                    data["_type_raw"] = (prev + "; " + val).strip("; ") if prev else val
                continue
            if klower in ("vin", "chassis number"):
                _merge_kv(data, k, v)
            elif not should_exclude_key(k, v):
                _merge_kv(data, k, v)

    # Fallback: düz metinden regex ile yakalama
    full_txt = clean(soup.get_text(" ", strip=True))
    patterns = {
        "engine_code": r"(?:Engine\s*Code|Motor\s*Code|Enginecode)\s*[:\-]\s*([A-Z0-9\-\.]{2,})",
        "fuel": r"(?:Fuel|Kraftstoff|Fuel\s*type)\s*[:\-]\s*([A-Za-zÇĞİÖŞÜçğıöşü/ ]{2,})",
        "gearbox": r"(?:Gearbox|Transmission|Getriebe)\s*[:\-]\s*([A-Za-z0-9/ \-]{2,})",
        "body": r"(?:Body|Body\s*type|Karosserie)\s*[:\-]\s*([A-Za-z0-9/ \-]{2,})",
        "chassis": r"(?:Chassis|Fahrgestell)\s*[:\-]\s*([A-Za-z0-9/ \-]{2,})",
        "vin": r"\b([A-HJ-NPR-Z0-9]{17})\b",
        "position": r"(front left|front right|rear left|rear right|left|right|front|rear)",
        "mileage_km": r"(\d{1,3}(?:[ .]\d{3})+|\d+)\s*km\b",
        "year": r"\b(19\d{2}|20\d{2})\b",
        "years": r"\b(19\d{2}|20\d{2})\s*[-/–]\s*(19\d{2}|20\d{2})\b",
        "article_number": r"(?:Article\s*Number|Artikelnummer)\s*[:\-]\s*([A-Za-z0-9_/ \-]+)",
        "original_number": r"(?:Original\s*Number(?:s)?|OEM)\s*[:\-]\s*([A-Za-z0-9_;,/ \-]+)",
    }
    for key, pat in patterns.items():
        if key in data and data[key]:
            continue
        m = re.search(pat, full_txt, flags=re.IGNORECASE)
        if m:
            # Only allow VIN for "vin" if the key is exactly "VIN" or "Chassis number"
            if key == "vin":
                # Only assign if "vin" or "chassis number"
                _merge_kv(data, "vin", m.group(1))
            elif key == "original_number":
                _merge_kv(data, key, m.group(1))
            else:
                _merge_kv(data, key, m.group(1))

    # Remove any "system" keys that slipped through
    for k in list(data.keys()):
        kl = k.lower()
        if kl == "system":
            del data[k]

    # Remove generic "number" (but keep "vin" and "article_number" and "chassis number")
    for k in list(data.keys()):
        kl = k.lower()
        if kl == "number":
            del data[k]
        elif "number" in kl and kl not in ("vin", "article_number", "chassis number", "original_number"):
            del data[k]

    # Normalize VIN value if present
    if "vin" in data and data["vin"]:
        m = re.search(r"\b([A-HJ-NPR-Z0-9]{17})\b", data["vin"], flags=re.I)
        data["vin"] = m.group(1).upper() if m else ""

    # If chassis_number present but chassis empty, move value to chassis
    if data.get("chassis_number") and not data.get("chassis"):
        data["chassis"] = data.get("chassis_number")

    return data
def is_valid_image(url: str) -> bool:
    return url.lower().endswith((".jpg", ".jpeg", ".png", ".webp"))

def parse_detail(html: str) -> Dict[str, Any]:
    soup = BeautifulSoup(html, "lxml")
    data = {
        "Fields": parse_key_value_blocks(soup),
        "OriginalNumbers": [],
        "Title": "",
        "PriceText": "",
        "YearText": "",
        "Currency": "",
        "ImageUrl": ""
    }

    # Title
    h1 = soup.select_one("h1")
    if h1:
        data["Title"] = clean(h1.get_text())

    # Fiyat
    price_el = soup.select_one(".item-detail__price")
    if price_el:
        data["PriceText"] = clean(price_el.get_text())
        m = re.search(r"[€£$]", price_el.get_text())
        if m:
            data["Currency"] = m.group(0)

    # Yıl
    year_el = soup.find(text=re.compile(r"(19\d{2}|20\d{2})"))
    if year_el:
        data["YearText"] = clean(year_el)

    # OriginalNumbers
    orig = data["Fields"].get("original_number") or ""
    if orig:
        data["OriginalNumbers"] = split_values(orig)

    # ImageUrl: extract from known image containers (high-res)
    image_url = ""
    # Prefer the big main image
    selectors = [
        "div.imageViewer__mainImage img",
        "section.imageViewer div.imageViewer__mainImage img",
        ".item-detail__gallery img",
        "img[itemprop='image']",
        "img[src*='cdn.autoparts24.eu/images/']",
        "meta[property='og:image']"
    ]
    for sel in selectors:
        el = soup.select_one(sel)
        if not el:
            continue
        # Try srcset first to pick the last (largest) candidate
        srcset = el.get("srcset")
        candidate = ""
        if srcset:
            parts = [p.strip().split(" ")[0] for p in srcset.split(",") if p.strip()]
            if parts:
                candidate = parts[-1]
        if not candidate:
            candidate = el.get("src") or el.get("data-src") or el.get("content") or ""
        candidate = clean(candidate)
        if candidate and is_valid_image(candidate):
            image_url = ensure_abs(candidate)
            break
    data["ImageUrl"] = image_url if image_url else ""

    return data

async def scrape_one_oem(page, oem: str) -> List[Dict[str, Any]]:
    rows: List[Dict[str, Any]] = []
    try:
        links = await get_list_links(page, oem)
    except Exception as e:
        logging.error(f"[SCRAPE] list fail oem={oem} err={e}")
        return rows

    seen_rows = set()
    for href in links[:8]:
        try:
            await page.goto(href, wait_until="networkidle", timeout=60000)
            parsed = parse_detail(await page.content())
            kv = parsed["Fields"]

            # --- Field cleanup: strip 'system/type/number' prefixes and normalize VIN ---
            def _strip_prefixes(val: str) -> str:
                if not val:
                    return ""
                val = clean(val)
                # remove leading labels like "system :", "type :", "number :"
                val = re.sub(r"^(system|type|number)\s*:\s*", "", val, flags=re.I)
                # split on semicolons and dedupe identical tokens
                tokens = [t.strip() for t in re.split(r"[;|]", val) if t.strip()]
                if not tokens:
                    return ""
                # prefer the longest unique token
                tokens = list(dict.fromkeys(tokens))
                return tokens[0]

            def _norm_vin(val: str) -> str:
                if not val:
                    return ""
                m = re.search(r"\b([A-HJ-NPR-Z0-9]{17})\b", val, flags=re.I)
                return (m.group(1).upper() if m else "").strip()

            for key in ("fuel", "gearbox"):
                if key in kv and kv[key]:
                    kv[key] = _strip_prefixes(kv.get(key, ""))

            if "vin" in kv and kv["vin"]:
                kv["vin"] = _norm_vin(kv["vin"])

            # Derive Body and Gearbox from any collected "type" strings
            type_raw = kv.pop("_type_raw", "")

            def _derive_body(val: str) -> str:
                txt = val.lower()
                body_keywords = [
                    "van", "hatchback", "sedan", "wagon", "estate", "coupe",
                    "convertible", "cabrio", "minivan", "minibus", "suv", "pickup",
                    "bus", "mpv"
                ]
                for w in body_keywords:
                    if re.search(r"\b" + re.escape(w) + r"\b", txt):
                        return w.capitalize()
                return ""

            def _derive_gearbox(val: str) -> str:
                t = val.lower()
                # Heuristics: prefer explicit words, else single-letter codes
                if re.search(r"\bautomatic\b|\bat\b", t):
                    return "Automatic"
                if re.search(r"\bmanual\b|\bmt\b", t):
                    return "Manual"
                # Single-letter hints (M/A) when separated by delimiters
                if re.search(r"(^|[; ,])a($|[; ,])", t):
                    return "Automatic"
                if re.search(r"(^|[; ,])m($|[; ,])", t):
                    return "Manual"
                return ""

            if type_raw:
                if not kv.get("body"):
                    body_guess = _derive_body(type_raw)
                    if body_guess:
                        kv["body"] = body_guess
                if not kv.get("gearbox"):
                    gb_guess = _derive_gearbox(type_raw)
                    if gb_guess:
                        kv["gearbox"] = gb_guess

            # If chassis is empty but chassis_number present, move it
            if not kv.get("chassis") and kv.get("chassis_number"):
                kv["chassis"] = clean(kv.get("chassis_number", ""))

            brand_raw = kv.get("brand", "")
            model_raw = kv.get("model", "")
            engine_raw = kv.get("engine", "")

            brand = normalize_brand(brand_raw)
            model = normalize_model(model_raw)
            engine = normalize_engine(engine_raw)

            # Gereksiz kayıt kelimeleri
            GENERIC_WORDS = {"brake", "caliper", "disc", "filter", "shock", "absorber", "pad", "front", "rear", "left", "right", "kit", "set"}

            # Marka boşsa ve başlıkta generic kelime yoksa başlıktan marka al
            if not brand:
                b_from_title, _ = extract_brand_model_from_title(parsed["Title"])
                if b_from_title and b_from_title.lower() not in GENERIC_WORDS:
                    brand = normalize_brand(b_from_title) or brand_raw

            # Model boşsa başlıktan model al
            if not model:
                _, m_from_title = extract_brand_model_from_title(parsed["Title"])
                model = normalize_model(m_from_title) or model_raw

            # Motor boşsa başlıktan motor al
            if not engine:
                engine = normalize_engine(
                    extract_engine_from_text(parsed["Title"]) or engine_raw
                )

            # VIN ek fallback
            if not kv.get("vin"):
                mvin = re.search(r"\b([A-HJ-NPR-Z0-9]{17})\b", parsed["Title"], re.I)
                if mvin:
                    kv["vin"] = mvin.group(1)

            # EngineCode ek fallback
            if not kv.get("engine_code") and engine:
                kv["engine_code"] = engine

            # Years ek fallback
            if not kv.get("years") and kv.get("year"):
                kv["years"] = kv.get("year")

            # Mileage ek fallback
            if not kv.get("mileage_km"):
                mm = re.search(r"(\d{1,3}(?:[ .]\d{3})+|\d+)\s*km\b", parsed["Title"], re.I)
                if mm:
                    kv["mileage_km"] = mm.group(1)

            # Yıl, fiyat, km parse
            year_text = parsed["YearText"] or kv.get("year", "")
            price_val = parse_price_null(parsed["PriceText"])
            year_val = parse_year_null(year_text)
            mileage_val = parse_int_null(kv.get("mileage_km", ""))

            # 🚫 Filtre: Boş veya generic marka/model içeren kayıtları atla
            if not brand or not model:
                continue
            if any(word in brand.lower() for word in GENERIC_WORDS):
                continue
            if any(word in model.lower() for word in GENERIC_WORDS):
                continue

            row = {
                "OEM": oem,
                "Title": parsed["Title"],
                "Brand": brand,
                "Model": model,
                "Engine": engine,
                "EngineCode": clean(kv.get("engine_code", "")),
                "Fuel": clean(kv.get("fuel", "")),
                "Gearbox": clean(kv.get("gearbox", "")),
                "Body": clean(kv.get("body", "")),
                "Chassis": clean(kv.get("chassis", "")),
                "VIN": clean(kv.get("vin", "")),
                "Position": clean(kv.get("position", "")),
                "MileageKm": mileage_val,
                "Year": year_val,
                "Years": clean(kv.get("years", "")),
                "ArticleNumber": clean(kv.get("article_number", "")),
                "OriginalNumbers": "; ".join(parsed["OriginalNumbers"]),
                "Price": price_val,
                "Currency": parsed["Currency"],
                "ImageUrl": parsed["ImageUrl"],
                "SourceUrl": href,
                "Source": "autoparts-24",
                "Status": 1,
            }
            # Deduplication: key over core business fields + URL
            dedup_key = (
                row["OEM"],
                row["SourceUrl"],
                row["ArticleNumber"] or "",
                row["VIN"] or "",
                row["Brand"] or "",
                row["Model"] or "",
                row["EngineCode"] or ""
            )
            # Prevent multiple inserts for the same item (deduplication)
            if dedup_key not in seen_rows:
                seen_rows.add(dedup_key)
                rows.append(row)
        except Exception as e:
            logging.error(f"[SCRAPE] detail fail oem={oem} url={href} err={e}")
    return rows


# ----------------- DB index + UPSERT --------------
def ensure_unique_index():
    sql = """
    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'ux_oem_src_art'
        ) THEN
            EXECUTE 'CREATE UNIQUE INDEX ux_oem_src_art
                     ON "OemToCars" ("OEM","SourceUrl","ArticleNumber")';
        END IF;
    END$$;
    """
    conn = psycopg2.connect(**PG_CFG)
    with conn:
        with conn.cursor() as cur:
            cur.execute(sql)
    conn.close()

UPSERT_SQL = """
INSERT INTO "OemToCars" (
    "OEM","Title","Brand","Model","Engine","EngineCode","Fuel","Gearbox","Body","Chassis",
    "VIN","Position","MileageKm","Year","Years","ArticleNumber","OriginalNumbers",
    "Price","Currency","ImageUrl","SourceUrl","Source","Status","CreatedId","CreatedDate"
) VALUES (
    %(OEM)s, %(Title)s, %(Brand)s, %(Model)s, %(Engine)s, %(EngineCode)s, %(Fuel)s, %(Gearbox)s, %(Body)s, %(Chassis)s,
    %(VIN)s, %(Position)s, %(MileageKm)s, %(Year)s, %(Years)s, %(ArticleNumber)s, %(OriginalNumbers)s,
    %(Price)s, %(Currency)s, %(ImageUrl)s, %(SourceUrl)s, %(Source)s, %(Status)s, %(CreatedId)s, NOW()
)
ON CONFLICT ("OEM","SourceUrl","ArticleNumber")
DO UPDATE SET
    "Title"           = EXCLUDED."Title",
    "Brand"           = EXCLUDED."Brand",
    "Model"           = EXCLUDED."Model",
    "Engine"          = EXCLUDED."Engine",
    "EngineCode"      = EXCLUDED."EngineCode",
    "Fuel"            = EXCLUDED."Fuel",
    "Gearbox"         = EXCLUDED."Gearbox",
    "Body"            = EXCLUDED."Body",
    "Chassis"         = EXCLUDED."Chassis",
    "VIN"             = EXCLUDED."VIN",
    "Position"        = EXCLUDED."Position",
    "MileageKm"       = EXCLUDED."MileageKm",
    "Year"            = EXCLUDED."Year",
    "Years"           = EXCLUDED."Years",
    "OriginalNumbers" = EXCLUDED."OriginalNumbers",
    "Price"           = EXCLUDED."Price",
    "Currency"        = EXCLUDED."Currency",
    "ImageUrl"        = EXCLUDED."ImageUrl",
    "Source"          = EXCLUDED."Source",
    "Status"          = EXCLUDED."Status",
    "ModifiedDate"    = NOW();
"""

def normalize_for_db(row: Dict[str, Any]) -> Dict[str, Any]:
    r = dict(row)
    for k in TEXT_FIELDS:
        if r.get(k) is None:
            r[k] = ''
    try:
        r["Status"] = int(r.get("Status", 1))
    except Exception:
        r["Status"] = 1
    r["CreatedId"] = CREATED_ID_DEFAULT
    return r

def db_upsert_rows(rows: List[Dict[str, Any]]) -> int:
    if not rows: return 0
    up = 0
    conn = psycopg2.connect(**PG_CFG)
    try:
        conn.autocommit = False
        with conn.cursor() as cur:
            for i in range(0, len(rows), DB_INSERT_BATCH):
                batch = rows[i:i+DB_INSERT_BATCH]
                for r in batch:
                    payload = normalize_for_db(r)
                    try:
                        cur.execute(UPSERT_SQL, payload)
                        up += 1

                        inserted_oem = (payload.get("OEM") or "").strip()
                        if inserted_oem:
                           cur.execute(
                               """
                               UPDATE "ProductGroupCodes"
                               SET "Status" = 99
                               WHERE regexp_replace(upper("GroupCode"), '[^0-9A-Z]', '', 'g')
                                     LIKE '%%' || regexp_replace(upper(%s), '[^0-9A-Z]', '', 'g') || '%%'
                               """,
                               [inserted_oem]
                           )
                    except Exception as e:
                        logging.error(f"[DB-ERR] upsert fail row={json.dumps(r, ensure_ascii=False)} err={e}")
                        conn.rollback()
                        continue
                conn.commit()
    finally:
        conn.close()
    return up

# ------------------- MAIN akış --------------------
async def main():
    logging.info("=== OEM to OemToCars başladı (UPSERT) ===")
    ensure_unique_index()
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=HEADLESS)
        ctx = await browser.new_context(
            locale="en-GB",
            viewport={"width": 1366, "height": 900},
            user_agent=random.choice(UA_POOL)
        )
        page = await ctx.new_page()

        for oem in iter_oems_from_db_chunked(DB_GROUPCODE_BATCH):
            try:
                rows = await scrape_one_oem(page, oem)
                if rows:
                    cnt = db_upsert_rows(rows)
                    logging.info(f"[UPSERT] oem={oem} rows={len(rows)} upserted={cnt}")
                else:
                    logging.info(f"[UPSERT] oem={oem} sonuç yok")
            except Exception as e:
                logging.error(f"[MAIN] oem={oem} genel hata: {e}")

        await browser.close()
    logging.info("=== OEM to OemToCars bitti ===")

if __name__ == "__main__":
    asyncio.run(main())