#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Otoparcasan.com — OEM -> OemToCars (PostgreSQL) yazıcı (INSERT + UPDATE)
- ProductGroupCodes'tan GroupCode OEM’lerini üretir (batch).
- WooCommerce arama ile ürün listelerini bulur, ürün detaylarını parse eder.
- OEM/OriginalNumbers, temel araç alanları (mümkün olduğunca) ve görseli çıkarır.
- UPSERT ve ProductGroupCodes.Status=99 güncellemesi yapar.
"""

import asyncio, os, re, json, logging, random, time
from typing import List, Dict, Any, Optional, Iterable, Set
from urllib.parse import urlencode, urljoin

import psycopg2
from bs4 import BeautifulSoup
from playwright.async_api import async_playwright, TimeoutError as PwTimeout

# ================== AYARLAR ==================
BASE = "https://otoparcasan.com"
HEADLESS = True
HEADLESS_RUNTIME = HEADLESS or not os.environ.get("DISPLAY")
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
DB_INSERT_BATCH   = 200
CREATED_ID_DEFAULT = 1

TEXT_FIELDS = [
    "OEM","Title","Brand","Model","Engine","EngineCode","Fuel","Gearbox","Body","Chassis",
    "VIN","Position","Years","ArticleNumber","OriginalNumbers","Currency",
    "ImageUrl","SourceUrl","Source","Description"
]

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s | %(message)s",
    handlers=[logging.StreamHandler(),
              logging.FileHandler("oem2cars_otoparcasan.log", encoding="utf-8")]
)

UA_POOL = [
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
]

# --- OEM filtreleme stopwords ve doğrulayıcılar ---
STOPWORDS = {
    "INVIEW","TAKSIT","SECENEKLERI","ACCORDION","MOBIL","BILGI","STOKTA","UYUMLU","NUMARALI","YEDEK",
    "YILINDA","OTOMOBIL","SONRASINDA","GENERAL","GROUP","STELLANTIS","FAZLA","ADEDIN","IPTAL","HAKKINI",
    "SAKLI","BELIRLENEN","LIMIT","KURUMSAL","OLMAYIP","FARKLI","LIMITLER","SILECEKSU","YIKAMALI",
    "INSIGNIA","HAKKINDA","DETAYLI","SORULARINIZ","OTOPARCASAN.COM","SITESINI","NUMARADAN",
    "FAQSSTATUS","FALSE","ALTERNATIVEBRANDSSTATUS","SUBCATEGORYSLUG","SUBCATEGORYNAME","ALTKATEGORI",
    "MONTAJYONU","SELECTEDCARDATA","SELECTEDCARDETAIL","SELECTEDCARDETAILURL","MEDYADA","SHARE",
    "FRANSA","KALITE","MOTOR","ELEKTRIK","MEKANIK","AKSAMDA","DAYANIKLI","ORJINAL","ORIJINAL",
    "API.WHATSAPP.COM","WWW.W3.ORG","WWW.LINKEDIN.COM","WWW.FACEBOOK.COM","SHARER","TWITTER.COM"
}

def _looks_like_oem(token: str) -> bool:
    """
    Kurallar:
    - Tamamen rakam ve 5–15 uzunluk: OK (örn. 34116766871)
    - Harf+rakam karışık 4–24 uzunluk ve sadece [A-Z0-9_/-]: OK
    - Noktalı ondalık sayı gibi (283.2, 97.1C339 vb.) -> RED
    - Sadece harf ya da çok kısa -> RED
    """
    t = clean(token).upper()
    if not t or t in STOPWORDS:
        return False
    # Ondalık benzeri değerleri çıkar
    if re.fullmatch(r"[0-9]+[.][0-9A-Z]+", t):
        return False
    # Sadece rakam, 5-15 haneyse kabul
    if re.fullmatch(r"\d{5,15}", t):
        return True
    # Harf + rakam karışık ve izinli karakterler
    if not re.fullmatch(r"[A-Z0-9/_-]{4,24}", t):
        return False
    # En az bir harf ve bir rakam içersin
    if re.search(r"[A-Z]", t) and re.search(r"\d", t):
        return True
    return False

def _sanitize_oem(token: str) -> str:
    t = clean(token).upper()
    t = re.sub(r"[^A-Z0-9/_-]", "", t)
    return t

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

# ---------- NORMALİZE: Brand / Model / Engine (kaba) ----------
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

def extract_brand_model_from_title(title: str) -> (str, str):
    t = clean(title)
    if not t: return "", ""
    parts = [p.strip() for p in re.split(r"\s*-\s*", t) if p.strip()]
    if len(parts) >= 2:
        brand = normalize_brand_str(parts[0])
        model = normalize_model_str(parts[1])
        return brand, model
    return "", ""

# ------------- SAYISAL PARSE (NULL güvenli) -------------
def parse_year_range(text: str) -> Optional[str]:
    if not text: return None
    text = clean(text)
    m = re.search(r"\b(19\d{2}|20\d{2})\s*[-/–]\s*(19\d{2}|20\d{2})\b", text)
    return m.group(0) if m else None

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

def parse_int_null(s: str) -> Optional[int]:
    if not s: return None
    m = re.search(r"\d{1,3}(?:[ .]\d{3})+|\d+", s)
    if not m: return None
    num = m.group(0).replace(" ", "").replace(".", "")
    try:
        return int(num)
    except Exception:
        return None

# --------- DB: GroupCode -> OEM (1000’er batch) ----
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

# -------------- Cookie banner --------------
async def accept_everything(page):
    for sel in [
        "#onetrust-accept-btn-handler",
        "button#onetrust-accept-btn-handler",
        "button:has-text('Kabul')",
        "button:has-text('Kabul Et')",
        "button:has-text('Accept')",
        "button:has-text('Accept all')",
    ]:
        try:
            el = page.locator(sel)
            if await el.count():
                await el.first.click(timeout=1500)
                await page.wait_for_timeout(200)
        except Exception:
            pass

# ---------------- Liste -> Link çıkar (Otoparcasan özel + WooCommerce fallback) --------------
async def bypass_firewall(page):
    """Try a few gentle tricks to get past the 'Otoparçasan Güvenlik Duvarı' page."""
    try:
        title = (await page.title()) or ""
    except Exception:
        title = ""
    if "Güvenlik" not in title and "Guvenlik" not in title:
        return
    for attempt in range(6):
        try:
            # random human-like actions
            await page.mouse.move(100 + attempt*20, 200 + attempt*10)
            await page.wait_for_timeout(400 + attempt*150)
            # sometimes there is a simple continue button; try to click anything actionable
            for sel in ["button:has-text('Devam')", "button:has-text('İlerle')", "a:has-text('Devam')", "a:has-text('İlerle')"]:
                el = page.locator(sel)
                if await el.count():
                    await el.first.click(timeout=800)
                    await page.wait_for_timeout(800)
                    break
            await page.reload(wait_until="domcontentloaded")
            await page.wait_for_timeout(800)
            t = (await page.title()) or ""
            if "Güvenlik" not in t and "Guvenlik" not in t:
                return
        except Exception:
            pass
    # last resort small wait (let their challenge finish)
    await page.wait_for_timeout(1500)

async def get_list_links(page, oem: str) -> List[str]:
    """
    Otoparcasan'da OEM araması:
      - /arama-sonucu?s=<oem>  (siteye özel)
      - WooCommerce klasik ?s= ve ?post_type=product fallback
    Ürün kartlarından başlık linklerini (h3.product-name a.name-link) ve
    olası "Detayları İncele/İncele" linklerini topla.
    """
    candidates = [
        f"{BASE}/arama-sonucu?{urlencode({'s': oem})}",                 # <- otoparcasan
        f"{BASE}/?{urlencode({'s': oem, 'post_type': 'product'})}",     # woo fallback
        f"{BASE}/?{urlencode({'s': oem})}",
    ]

    links: List[str] = []
    seen: Set[str] = set()

    async def _harvest_from_dom() -> None:
        html = await page.content()
        soup = BeautifulSoup(html, "lxml")

        anchors: List[Any] = []

        # 1) Otoparcasan kart başlık linki (en güvenilir)
        anchors += soup.select("h3.product-name a.name-link")

        # 2) Kart içi farklı olası bağlantılar
        anchors += soup.select(
            ".product-item a.name-link, "
            ".product-item a[href*='/urun/'], "
            ".product-item a[href*='/oto-yedek-parca/']"
        )

        # 3) Genel fallbacks
        anchors += soup.select(
            ".products li.product a.woocommerce-LoopProduct-link, "
            "ul.products li.product a, "
            "a[href*='/product/']"
        )

        # 4) Metne göre — “Detayları İncele / İncele”
        for a in soup.select("a"):
            t = (a.get_text(" ", strip=True) or "").lower()
            if ("detay" in t or "incele" in t) and a.get("href"):
                anchors.append(a)

        # Dedupe + filtre
        for a in anchors:
            href = a.get("href")
            if not href:
                continue
            u = href if href.startswith("http") else ensure_abs(href)
            if not u or u.endswith("#"):
                continue
            key = u.split("?")[0]
            if key not in seen:
                seen.add(key)
                links.append(key)

    for url in candidates:
        try:
            await page.goto(url, wait_until="networkidle", timeout=60000)
            await accept_everything(page)
            await bypass_firewall(page)
            # Kartlar veya “Detayları İncele” görünsün diye kısa bekleme
            try:
                await page.wait_for_selector(
                    "h3.product-name a.name-link, ul.products li.product, text=Detayları İncele, text=İncele",
                    timeout=4500
                )
            except Exception:
                pass
            await _harvest_from_dom()
            if links:
                break
        except Exception as e:
            logging.warning(f"[SEARCH] {url} hata: {e}")
        await page.wait_for_timeout(700)

    # Hiç link yoksa debug HTML bırak
    if not links and DEBUG:
        try:
            html = await page.content()
            with open(os.path.join(DEBUG_DIR, f"links_{oem}.html"), "w", encoding="utf-8") as f:
                f.write(html)
        except Exception:
            pass

    # fazlalıkları kırp
    return links[:20]
# ---- Baslıktan marka tahmini (küçük bir sözlük + tamamen büyük harf blokları) ----
KNOWN_BRANDS = {
    "BOSCH","SWAG","REINZ","V.REINZ","NGK","BREMBO","FEBI","SACHS","SKF","MEYLE","TRW","DELPHI",
    "TEXTAR","ATE","MAHLE","DAYCO","GATES","DENSO","MANN","VALEO","HELLA","VAICO","NISSENS",
    # producer/aftermarket labels we should never use as vehicle brand
    "DJ","DJPARTS","DJ PARTS","DJ AUTO","DJ AUTO GROUP"
}

def guess_brand_from_title(title: str) -> str:
    t = clean(title).replace("•", " ").replace("/", " ")
    # 1) Bilinen markalardan birini ara
    for b in KNOWN_BRANDS:
        if b in t.upper():
            return b
    # 2) Tamamen büyük harflerden oluşan 2–10 harf arası bir blok al
    m = re.search(r"\b([A-ZÇĞİÖŞÜ]{2,10})\b", t)
    return m.group(1) if m else ""

# --------------- OEM / alan çıkarımları -------------------
OEM_TOKEN = r"[A-Z0-9][A-Z0-9\.\-_/ ]{3,}[A-Z0-9]"
OEM_SPLIT = re.compile(r"[;,\|/\s]+")

def parse_oems_from_text(text: str) -> List[str]:
    if not text:
        return []
    parts = OEM_SPLIT.split(text)
    uniq, seen = [], set()
    for raw in parts:
        tok = clean(raw).upper()
        if not tok or len(tok) < 4:
            continue
        if _looks_like_oem(tok):
            tok = _sanitize_oem(tok)
            if tok and tok not in seen:
                uniq.append(tok); seen.add(tok)
    return uniq

# --- Helper: Clean and merge OEM tokens from multiple sources ---
def _clean_oem_token(tok: str) -> str:
    """Uppercase and strip non OEM chars commonly seen."""
    t = clean(tok).upper()
    return re.sub(r"[^A-Z0-9\.\-_/]", "", t)

def merge_oems(*texts: str, oem_hint: Optional[str] = None) -> List[str]:
    bag: List[str] = []
    for t in texts:
        for o in parse_oems_from_text(t or ""):
            bag.append(o)
    if oem_hint:
        oh = _sanitize_oem(oem_hint)
        if _looks_like_oem(oh):
            bag.append(oh)
    uniq, seen = [], set()
    for o in bag:
        if o and o not in seen:
            uniq.append(o); seen.add(o)
    return uniq

def extract_blocks_for_detail(soup: BeautifulSoup) -> List[str]:
    blocks = []
    for sel in [
        "h1.product_title", "h1.entry-title", "h1",
        ".woocommerce-product-details__short-description",
        ".entry-content", ".summary", ".product_meta",
        ".woocommerce-Tabs-panel", ".woocommerce-product-attributes",
        "table.woocommerce-product-attributes", "table.shop_attributes"
    ]:
        for el in soup.select(sel):
            t = clean(el.get_text(" ", strip=True))
            if t and t not in blocks:
                blocks.append(t)
    # ayrıca tüm tablolar
    for tr in soup.select("table tr"):
        t = clean(tr.get_text(" ", strip=True))
        if t and t not in blocks: blocks.append(t)
    return blocks

# --- Helper: Pick the best product image URL ---
def extract_image_url(soup: BeautifulSoup) -> Optional[str]:
    """
    Pick the best product image URL from the page.
    Priority:
      1) OpenGraph /link rel image
      2) Dedicated product image containers
      3) Any visible lazy-loaded product image inside product area
    Filters out placeholders like productloading.png or logo images.
    """
    def _norm(u: str) -> str:
        return ensure_abs((u or "").strip())
    def _is_bad(u: str) -> bool:
        if not u:
            return True
        low = u.lower()
        bad_parts = [
            "productloading.png",     # site placeholder
            "logo-kare.jpg",
            "/others/logo-kare.jpg",
            "data:image",             # base64
            "/svg", ".svg",
            "/icons/",
        ]
        return any(b in low for b in bad_parts)

    # 1) OG / image_src
    meta = soup.select_one('meta[property="og:image"][content], meta[name="og:image"][content]')
    if meta:
        u = meta.get("content", "")
        if u and not _is_bad(u):
            return _norm(u)
    link_img = soup.select_one('link[rel="image_src"][href]')
    if link_img:
        u = link_img.get("href", "")
        if u and not _is_bad(u):
            return _norm(u)

    # 2) Common product image containers
    img = soup.select_one(
        ".product-detail-image img, "
        ".product-images img, "
        ".woocommerce-product-gallery img, "
        ".woocommerce-product-gallery__image img, "
        ".product-gallery img, "
        "img[itemprop='image']"
    )
    if img:
        u = img.get("data-src") or img.get("src")
        if u and not _is_bad(u):
            return _norm(u)

    # 3) Fallback: any lazy image inside product card/summary/detail
    for sel in [
        ".product-item img[data-src]", ".product-item img", ".product img", ".summary img", ".entry-content img", "img.lazy", "img[data-src]"
    ]:
        for im in soup.select(sel):
            u = im.get("data-src") or im.get("src")
            if u and not _is_bad(u):
                return _norm(u)

    return None

# --- Helper: Parse Uyumlu Araçlar Modal (multi-block support) ---
def parse_compatible_modal(html: str) -> list:
    soup = BeautifulSoup(html, "lxml")
    uyumlu_araclar = []
    details = soup.select(".uyumlu-car-detail")
    if not details:
        # fallback: eski tekli modal için eski davranış
        out: Dict[str, str] = {}
        container = soup.select_one(
            ".uyumlu-car-detail, .uyumlu-car-modal, .compatible-dropdown-item.show, "
            ".dropdown-contents, .modal-content .uyumlu-car-detail, .modal-body .uyumlu-car-detail"
        )
        if not container:
            return []
        def _assign(k: str, v: str):
            k = clean(k).lower()
            v = clean(v)
            if not k or not v:
                return
            if k.startswith("marka"):
                out["brand"] = v
            elif k.startswith("model"):
                out["model"] = v
            elif "alt model" in k:
                out["engine"] = v
            elif "kasa" in k or "gövde" in k:
                out["body"] = v
            elif "yakıt" in k or "yakit" in k or "fuel" in k:
                out["fuel"] = v
            elif k == "yıl" or "yıl" in k or "yil" in k or "years" in k:
                out["years"] = v
            elif "motor kod" in k or "engine code" in k:
                out["engine_code"] = v
            elif "vites" in k or "şanzıman" in k or "sanziman" in k or "gearbox" in k:
                out["gearbox"] = v
            elif "vin" in k:
                out["vin"] = v
        for it in container.select(".item"):
            texts = [clean(x.get_text()) for x in it.find_all(["p", "span", "div"]) if clean(x.get_text())]
            key = val = ""
            if len(texts) >= 2:
                key, val = texts[0], texts[-1]
            else:
                txt = clean(it.get_text(" ", strip=True))
                if ":" in txt:
                    parts = [p.strip() for p in txt.split(":", 1)]
                    if len(parts) == 2:
                        key, val = parts
            if key and val:
                _assign(key, val)
        lone_ps = [clean(p.get_text()) for p in container.select("p") if clean(p.get_text())]
        if lone_ps:
            if "brand" not in out:
                for t in lone_ps:
                    m = re.fullmatch(r"[A-ZÇĞİÖŞÜ]{2,12}", t)
                    if m:
                        out["brand"] = t
                        break
            if "fuel" not in out:
                for t in lone_ps:
                    if t.lower() in ("benzin", "dizel", "lpg", "hybrid", "elektrik"):
                        out["fuel"] = t
                        break
        return [out] if out else []
    # Çoklu uyumlu-car-detail blokları için
    for detail in details:
        arac = {}
        for item in detail.select(".item"):
            parts = item.find_all("p")
            if len(parts) >= 2:
                key = parts[0].get_text(strip=True)
                val = parts[1].get_text(strip=True)
                k = clean(key).lower()
                v = clean(val)
                if not k or not v:
                    continue
                if k.startswith("marka"):
                    arac["brand"] = v
                elif k.startswith("model"):
                    arac["model"] = v
                elif "alt model" in k:
                    arac["engine"] = v
                elif "kasa" in k or "gövde" in k:
                    arac["body"] = v
                elif "yakıt" in k or "yakit" in k or "fuel" in k:
                    arac["fuel"] = v
                elif k == "yıl" or "yıl" in k or "yil" in k or "years" in k:
                    arac["years"] = v
                elif "motor kod" in k or "engine code" in k:
                    arac["engine_code"] = v
                elif "vites" in k or "şanzıman" in k or "sanziman" in k or "gearbox" in k:
                    arac["gearbox"] = v
                elif "vin" in k:
                    arac["vin"] = v
        # lone <p> blocks as hints
        lone_ps = [clean(p.get_text()) for p in detail.select("p") if clean(p.get_text())]
        if lone_ps:
            if "brand" not in arac:
                for t in lone_ps:
                    m = re.fullmatch(r"[A-ZÇĞİÖŞÜ]{2,12}", t)
                    if m:
                        arac["brand"] = t
                        break
            if "fuel" not in arac:
                for t in lone_ps:
                    if t.lower() in ("benzin", "dizel", "lpg", "hybrid", "elektrik"):
                        arac["fuel"] = t
                        break
        if arac:
            uyumlu_araclar.append(arac)
    return uyumlu_araclar

# --- Helper: Get vehicle brand hint from page even if list parse fails ---
def get_vehicle_brand_hint(html: str) -> str:
    soup = BeautifulSoup(html, "lxml")
    # Prefer the active brand button text (e.g., JAGUAR)
    for sel in [
        ".compatible-navs .uyumluSelectButtons.active h4",
        ".navs-list .uyumluSelectButtons.active h4",
        ".compatible-navs .item .uyumluSelectButtons.active h4",
        # NEW: list/search page brand selectors
        ".compatible-item.active .brand-name",
        ".compatible-item.active [alt]",
    ]:
        h4 = soup.select_one(sel)
        if h4 and clean(h4.get_text()):
            return clean(h4.get_text()).upper()
    # Additional: take brand from active button image alt attribute
    img_alt = soup.select_one(".navs-list .uyumluSelectButtons.active img[alt], .compatible-navs .uyumluSelectButtons.active img[alt], .compatible-item.active img[alt]")
    if img_alt and clean(img_alt.get("alt", "")):
        return clean(img_alt.get("alt", "")).upper()
    # Fallbacks: sometimes there is no `.active`; take any visible brand name in the carousel
    if not img_alt:
        any_brand = soup.select_one(".compatible-item .brand-name, .compatible-navs .brand-name, .compatible-item [alt]")
        if any_brand:
            txt = any_brand.get_text(strip=True) if hasattr(any_brand, 'get_text') else any_brand.get("alt", "")
            if clean(txt):
                return clean(txt).upper()
    # Hidden JSON blocks sometimes exist without visible container
    for div in soup.select("div[id^='uyumlu_car_']"):
        raw = clean(div.get_text(" ", strip=True))
        if not raw:
            continue
        try:
            data = json.loads(raw)
        except Exception:
            continue
        manu = clean(data.get("manuName", ""))
        if manu:
            return manu.upper()
    return ""

# --- Helper: Parse on-page compatible list (no modal needed) ---
def _fmt_year_mm(val: str) -> str:
    # "200204" -> "04.2002"
    val = clean(val)
    if re.fullmatch(r"\d{6}", val):
        return f"{val[4:6]}.{val[0:4]}"
    if re.fullmatch(r"\d{4}", val):
        return val
    return val

def parse_compatible_list(html: str) -> list:
    soup = BeautifulSoup(html, "lxml")
    out, seen = [], set()

    # 1) Prefer hidden JSON payloads: div#uyumlu_car_* containing JSON (works even if container is missing)
    for div in soup.select("div[id^='uyumlu_car_']"):
        raw = clean(div.get_text(" ", strip=True))
        if not raw:
            continue
        try:
            data = json.loads(raw)
        except Exception:
            continue
        brand = clean(data.get("manuName", ""))
        model = clean(data.get("modelName", ""))
        engine = clean(data.get("typeName", ""))
        body = clean(data.get("constructionType", ""))
        fuel = clean(data.get("fuelType", ""))
        years = ""
        yf, yt = clean(str(data.get("yearOfConstrFrom", ""))), clean(str(data.get("yearOfConstrTo", "")))
        if yf or yt:
            years = f"{_fmt_year_mm(yf)} - {_fmt_year_mm(yt)}".strip(" - ")
        key = (brand, model, engine, years)
        if brand and model and key not in seen:
            out.append({
                "brand": brand,
                "model": model,
                "engine": engine,
                "body": body,
                "fuel": fuel,
                "years": years,
            })
            seen.add(key)

    if out:
        return out

    # NEW: Handle search/list page layout (dropdown items under a selected brand)
    brand_hint = get_vehicle_brand_hint(html)
    if brand_hint:
        brand_hint = brand_hint.upper()
        if brand_hint in KNOWN_BRANDS:
            brand_hint = ""
    for dd in soup.select(".compatible-dropdown-item"):
        model_name = clean((dd.select_one(".dropdown-title") or {}).get_text(" ", strip=True) if dd.select_one(".dropdown-title") else "")
        # each dropdown-body may contain one or multiple compatible-group blocks
        for grp in dd.select(".dropdown-body .compatible-group"):
            item = {"brand": brand_hint, "model": model_name}
            if item["brand"] and item["brand"].upper() in KNOWN_BRANDS:
                item["brand"] = ""
            pairs = [clean(p.get_text()) for p in grp.select(".item p") if clean(p.get_text())]
            for i in range(0, len(pairs) - 1, 2):
                k = pairs[i].lower(); v = pairs[i+1]
                if k.startswith("hp") or k.startswith("kw"):  # skip power row
                    continue
                if k.startswith("alt model") or "alt model" in k:
                    item["engine"] = v
                elif k.startswith("kasa") or "gövde" in k or "govde" in k or "body" in k:
                    item["body"] = v
                elif ("yakıt" in k) or ("yakit" in k) or ("fuel" in k):
                    item["fuel"] = v
                elif ("yıl" in k) or ("yil" in k) or ("years" in k):
                    item["years"] = v
                elif ("vites" in k) or ("şanzıman" in k) or ("sanziman" in k) or ("gearbox" in k):
                    item["gearbox"] = v
            key = (item.get("brand",""), item.get("model",""), item.get("engine",""), item.get("years",""))
            if item.get("brand") and item.get("model") and key not in seen:
                out.append(item); seen.add(key)
    if out:
        return out

    # 2) Fallback: read visible list when JSON blocks are not present
    container = soup.select_one(".compatible-container, .uyumlu-wrapper, .compatible-navs")
    if not container:
        return []

    # 3) Fallback: read visible lines
    brand = get_vehicle_brand_hint(html)

    for model_block in soup.select(".uyumlu-wrapper .modelContainer"):
        model_name = clean((model_block.select_one(".modelLine p") or {}).get_text(" ", strip=True) if model_block.select_one(".modelLine p") else "")
        for line in model_block.select(".allLines .line"):
            item = {"brand": brand, "model": model_name}
            pairs = [clean(p.get_text()) for p in line.select(".item p") if clean(p.get_text())]
            # Expect alternating key/value
            for i in range(0, len(pairs) - 1, 2):
                k = pairs[i].strip().lower()
                v = pairs[i + 1].strip()
                if k.startswith("alt model") or "alt model" in k:
                    item["engine"] = v
                elif k.startswith("kasa") or "gövde" in k or "govde" in k or "body" in k:
                    item["body"] = v
                elif ("yakıt" in k) or ("yakit" in k) or ("fuel" in k):
                    item["fuel"] = v
                elif ("yıl" in k) or ("yil" in k) or ("years" in k):
                    item["years"] = v
                elif ("vites" in k) or ("şanzıman" in k) or ("sanziman" in k) or ("gearbox" in k):
                    item["gearbox"] = v
            key = (item.get("brand",""), item.get("model",""), item.get("engine",""), item.get("years",""))
            if item.get("brand") and item.get("model") and key not in seen:
                out.append(item)
                seen.add(key)

    return out

def parse_detail_otoparcasan(html: str) -> Dict[str, Any]:
    soup = BeautifulSoup(html, "lxml")
    parsed = {
        "Title": "",
        "PriceText": "",
        "Currency": "",
        "OriginalNumbers": [],
        "ArticleNumber": "",
        "Fields": {},
        "ImageUrl": ""
    }

    # Başlık (detail sayfası) + fallback (liste içi)
    h1 = soup.select_one("h1.product_title, h1.entry-title, h1")
    if h1 and clean(h1.get_text()):
        parsed["Title"] = clean(h1.get_text())
    if not parsed["Title"]:
        a = soup.select_one("h3.product-name a.name-link")
        if a and clean(a.get_text()):
            parsed["Title"] = clean(a.get_text())

    # Fiyat / para birimi
    price_el = soup.select_one(".summary .price, .price .amount, .woocommerce-Price-amount, .product-price .amount")
    if price_el:
        parsed["PriceText"] = clean(price_el.get_text())
        m = re.search(r"[€£₺$]", price_el.get_text())
        if m:
            parsed["Currency"] = m.group(0)

    # Özellikler — Woo tablosu
    fields: Dict[str, str] = {}
    def _merge(k, v):
        k = clean(k)
        v = clean(v)
        if not k or not v:
            return
        key = k.lower()
        cur = fields.get(key)
        fields[key] = (cur + "; " + v) if cur and cur != v else v

    for tr in soup.select("table.woocommerce-product-attributes tr, table.shop_attributes tr, table tr"):
        th = tr.select_one("th")
        td = tr.select_one("td")
        if th and td:
            _merge(th.get_text(), td.get_text())

    # Özellikler — Otoparcasan kart içi “product-info” blokları (Stok Kodu vb.)
    for info in soup.select(".product-info, .product-item .product-info"):
        for p in info.select("p"):
            txt = clean(p.get_text(" ", strip=True))
            m = re.match(r"(.{2,40}?)[\s:]+(.+)$", txt)
            if m:
                _merge(m.group(1), m.group(2))

    parsed["Fields"] = fields

    # Article number yakalama (Stok Kodu / Ürün Kodu vb.)
    art = ""
    for k in fields.keys():
        if any(w in k for w in ["stok kodu", "ürün kodu", "urun kodu", "article", "parça", "parca", "sku", "model kodu"]):
            art = clean(fields[k])
            break
    if not art:
        # düz metinden ara
        full = clean(soup.get_text(" ", strip=True))
        m = re.search(r"(Stok\s*Kodu|SKU|Ürün\s*Kodu)\s*[:\-]\s*([A-Z0-9\.\-_/ ]+)", full, flags=re.I)
        if m:
            art = clean(m.group(2))
    # Article number'ı sadeleştir (ilk uygun OEM benzeri parça)
    art_clean = ""
    for cand in OEM_SPLIT.split(art):
        cand = clean(cand)
        if _looks_like_oem(cand):
            art_clean = _sanitize_oem(cand)
            break
    parsed["ArticleNumber"] = art_clean or art[:64]

    # OriginalNumbers (OEM kodları) — birincil kaynak: sayfadaki OEM listeleri
    # Bilinen bazı kapsayıcı sınıflar + başlığı "OEM" geçen blokların metinleri
    oem_raw_texts: List[str] = []
    for sel in [
        ".product-detail-oem, .oem, .oem-list, .oem-codes, .oemCode",
        "#oem, [id*='oem'], [class*='oem']"
    ]:
        for el in soup.select(sel):
            t = clean(el.get_text(" ", strip=True))
            if t:
                oem_raw_texts.append(t)

    # Başlığında OEM geçen serbest bloklar (BeautifulSoup'ta :contains yok, text arayacağız)
    for node in soup.find_all(string=re.compile(r"OEM\s*Kod|OEM", re.I)):
        container = node.parent
        if container:
            t = clean(container.get_text(" ", strip=True))
            if t:
                oem_raw_texts.append(t)
            # bir sonraki kardeş bloğu da dene
            sib = container.find_next_sibling()
            if sib:
                t2 = clean(sib.get_text(" ", strip=True))
                if t2:
                    oem_raw_texts.append(t2)

    # Tüm bu metinlerden OEM adaylarını çıkar ve uniq liste oluştur
    parsed["OriginalNumbers"] = merge_oems(*oem_raw_texts)

    # Görsel: ana görseli al; lazy-loaded ve OG fallback'leri destekle
    img_url = extract_image_url(soup)
    parsed["ImageUrl"] = img_url

    # Ürün açıklaması (Description) — bilgi kutusu / Woo description panelleri
    desc_texts: List[str] = []
    for sel in [
        "#tab-description, .woocommerce-Tabs-panel--description, .woocommerce-product-details__short-description",
        ".tabs-body .product-info, .info-box, .product-info-box, .product-detail-page .info-box-body"
    ]:
        for el in soup.select(sel):
            t = clean(el.get_text(" ", strip=True))
            if t and t not in desc_texts:
                desc_texts.append(t)
    desc_joined = clean(" \n".join(desc_texts))
    # Metinden sık görülen tekrarlı blokları kırp
    desc_joined = re.sub(r"(Bu ürüne ait bazı OEM kodları[^.]+?\.)\s*(\1)+", r"\1", desc_joined, flags=re.I)
    parsed["Description"] = desc_joined[:2000]

    # Diğer eski OEM yakalama (legacy, fallback, not used for OrjinalNumbers now)
    # OEM / Orijinal / Eşdeğer / Üretici no yakalama
    oem_keys = [k for k in fields.keys() if any(w in k for w in ["oem", "orijinal", "orjinal", "eşdeğer", "esdeger", "üretici", "uretici", "referans"])]
    oems: Set[str] = set()
    for k in oem_keys:
        for o in parse_oems_from_text(fields.get(k, "")):
            oems.add(o)
    # bloklardan da tara
    blocks = extract_blocks_for_detail(soup)
    for b in blocks:
        for o in parse_oems_from_text(b):
            oems.add(o)
    # Not setting parsed["OriginalNumbers"] here, as OrjinalNumbers now comes from above

    # Status: eğer OriginalNumbers boş değilse işaretle (ürün bulundu)
    if parsed.get("OriginalNumbers"):
        parsed["Status"] = 99

    return parsed

# --------------- Tek OEM’i kazı -------------------
async def scrape_one_oem(page, oem: str) -> List[Dict[str, Any]]:
    rows: List[Dict[str, Any]] = []
    try:
        links = await get_list_links(page, oem)
    except Exception as e:
        logging.error(f"[SCRAPE] list fail oem={oem} err={e}")
        return rows

    # NEW: also parse the search result page itself as a fallback source of compatibility data
    try:
        await page.goto(f"{BASE}/arama-sonucu?{urlencode({'s': oem})}", wait_until="networkidle", timeout=60000)
        await accept_everything(page)
        await bypass_firewall(page)
        try:
            await page.wait_for_selector("div[id^='uyumlu_car_'], .compatible-dropdown-item, .compatible-container", timeout=5000)
        except Exception:
            pass
        search_html = await page.content()
        search_compats = parse_compatible_list(search_html)
    except Exception:
        search_compats = []

    search_brand_hint = ""
    try:
        search_brand_hint = get_vehicle_brand_hint(search_html)
    except Exception:
        search_brand_hint = ""

    seen = set()
    for href in links[:10]:
        try:
            await page.goto(href, wait_until="domcontentloaded", timeout=60000)
            await accept_everything(page)
            await bypass_firewall(page)
            await page.wait_for_timeout(500)
            html = await page.content()
            # Capture brand hint from the page before any modal/modal tab logic
            vehicle_brand_hint = get_vehicle_brand_hint(html)
            # Proactively click Uyumlu Araçlar tab to ensure compatible list is present
            try:
                # Click the on-page Compatible Vehicles tab (no modal)
                tab = page.locator(".tab-item[data-id='CompatibleVehicles']").first
                if not await tab.count():
                    tab = page.locator("div:has-text('Uyumlu Araçlar')").first
                if not await tab.count():
                    tab = page.locator("a:has-text('Uyumlu Araçlar'), button:has-text('Uyumlu Araçlar'), li:has-text('Uyumlu Araçlar')").first
                if await tab.count():
                    await tab.click(timeout=1500)
                    # Wait for the on-page compatible list or hidden JSON blocks to become available
                    try:
                        await page.wait_for_selector("div[id^='uyumlu_car_'], .uyumlu-wrapper, .compatible-container", timeout=6000)
                    except Exception:
                        pass
                    html = await page.content()
                    # refresh brand hint after tab click as well
                    vh = get_vehicle_brand_hint(html)
                    if vh:
                        vehicle_brand_hint = vh
            except Exception:
                pass
            # First try: parse on-page compatible list (fast path)
            compat_list = parse_compatible_list(html)
            parsed = parse_detail_otoparcasan(html)

            # If detail page compat list is missing or lacks brand/model, fall back to search-page parsed data
            def _list_missing_brand_or_model(lst):
                if not lst:
                    return True
                for _it in lst:
                    if not _it.get("brand") or not _it.get("model"):
                        return True
                return False

            if _list_missing_brand_or_model(compat_list) and search_compats:
                compat_list = search_compats

            # Ensure brand propagation from either detail-page hint or search-page hint
            if not vehicle_brand_hint and search_brand_hint:
                vehicle_brand_hint = search_brand_hint

            # Propagate any available brand hint into compat entries that missed it
            if vehicle_brand_hint and compat_list:
                for _c in compat_list:
                    if not _c.get("brand"):
                        _c["brand"] = vehicle_brand_hint

            title = parsed["Title"]
            fields = parsed["Fields"]

            # propagate brand hint if groups lack explicit brand
            if vehicle_brand_hint and compat_list:
                for _c in compat_list:
                    if not _c.get("brand"):
                        _c["brand"] = vehicle_brand_hint

            # Eğer uyumlu araçlar varsa her biri için ayrı satır, yoksa tek satır (eski davranış)
            compat_used = compat_list if compat_list else (search_compats if search_compats else ([{"brand": vehicle_brand_hint}] if vehicle_brand_hint else [{}]))
            for compat in compat_used:
                # --- Vehicle details (prefer modal values) ---
                # Brand MUST be vehicle brand (e.g., JAGUAR). Do NOT fall back to producer (BOSCH, DJ, ...).
                vehicle_brand = clean(compat.get("brand", "")) if compat else ""
                brand = ""
                if vehicle_brand:
                    brand = vehicle_brand.upper()
                elif vehicle_brand_hint:
                    brand = vehicle_brand_hint.upper()
                # guard against producer names (e.g., BOSCH, DJ PARTS) sneaking in
                if brand and brand.upper() in KNOWN_BRANDS:
                    brand = ""
                # Do not infer producer brand from title here; only vehicle brand is allowed.

                # Model: modal model first; else from title
                model = ""
                if compat.get("model"):
                    model = normalize_model_str(compat["model"])
                else:
                    _b, m_from_title = extract_brand_model_from_title(title)
                    model = normalize_model_str(m_from_title)

                # Engine / EngineCode
                # Preserve engine/type text exactly from compatibility (e.g., "R 4,2 V8", "4.2 V8")
                engine_guess = clean(compat.get("engine", "")) if compat.get("engine") else ""
                engine_code = ""
                for k in fields.keys():
                    if "motor kod" in k or "engine code" in k:
                        engine_code = clean(fields[k]); break
                if not engine_code and compat.get("engine_code"):
                    engine_code = clean(compat["engine_code"])

                # Fuel / Body / Gearbox / VIN / Years
                fuel = clean(compat.get("fuel", "")) if compat.get("fuel") else ""
                body = clean(compat.get("body", "")) if compat.get("body") else ""
                gearbox = clean(compat.get("gearbox", "")) if compat.get("gearbox") else ""
                vin_val = clean(compat.get("vin", "")) if compat.get("vin") else ""

                years_text = None
                # try to discover a year range from fields first
                for v in fields.values():
                    yrs = parse_year_range(v)
                    if yrs:
                        years_text = yrs; break
                # modal years fallback
                if not years_text and compat.get("years"):
                    years_text = compat.get("years")
                year_val = parse_year_null(years_text or title)

                # Stok Kodu (liste içinden yakalanmışsa)
                article_number = parsed["ArticleNumber"]

                price_val = parse_price_null(parsed["PriceText"])

                # Build a robust OriginalNumbers list:
                # - start with parsed ones (fields + blocks)
                # - add from title and article number
                # - always include the searched OEM hint
                oem_list = merge_oems(
                    " ".join(parsed.get("OriginalNumbers", []) or []),
                    title,
                    article_number,
                    " ".join(fields.values()) if isinstance(fields, dict) else "",
                    oem_hint=oem
                )

                row = {
                    "OEM": oem,
                    "Title": title,
                    "Brand": brand,
                    "Model": model,
                    "Engine": engine_guess,
                    "EngineCode": engine_code,
                    "Fuel": fuel,
                    "Gearbox": gearbox,
                    "Body": body,
                    "Chassis": "",
                    "VIN": vin_val,
                    "Position": "",
                    "MileageKm": None,
                    "Year": year_val,
                    "Years": years_text or "",
                    "ArticleNumber": article_number,
                    "OriginalNumbers": "; ".join(oem_list),
                    "Price": price_val,
                    "Currency": parsed["Currency"],
                    "ImageUrl": parsed["ImageUrl"],
                    "Description": parsed.get("Description", ""),
                    "SourceUrl": href,
                    "Source": "otoparcasan",
                    "Status": 1,
                }
                # --- Final fallbacks & fixes ---
                # Final brand guard: use any available hints
                if not row["Brand"]:
                    if vehicle_brand_hint and vehicle_brand_hint.upper() not in KNOWN_BRANDS:
                        row["Brand"] = vehicle_brand_hint.upper()
                    elif search_brand_hint and search_brand_hint.upper() not in KNOWN_BRANDS:
                        row["Brand"] = search_brand_hint.upper()

                # Derive Fuel/Gearbox from fields when compat is missing
                if not row["Fuel"]:
                    for k, v in fields.items():
                        lk = k.lower()
                        if ("yakıt" in lk) or ("yakit" in lk) or ("fuel" in lk):
                            row["Fuel"] = clean(v); break
                if not row["Gearbox"]:
                    for k, v in fields.items():
                        lk = k.lower()
                        if ("vites" in lk) or ("şanzıman" in lk) or ("sanziman" in lk) or ("gearbox" in lk):
                            row["Gearbox"] = clean(v); break

                # If Model still empty, try to extract from Title as last resort
                if not row["Model"]:
                    _bttl, mttl = extract_brand_model_from_title(title)
                    row["Model"] = normalize_model_str(mttl)

                # Guard: if by any chance Brand equals a producer brand, blank it
                if row["Brand"] and row["Brand"].upper() in KNOWN_BRANDS:
                    row["Brand"] = ""

                dedup_key = (row["OEM"], row["SourceUrl"], row["ArticleNumber"], row["Brand"], row["Model"], row["Engine"], row["EngineCode"])
                if dedup_key not in seen:
                    seen.add(dedup_key)
                    rows.append(row)
        except Exception as e:
            logging.error(f"[SCRAPE] detail fail oem={oem} url={href} err={e}")
        # nazik gecikme
        await page.wait_for_timeout(700)
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

def ensure_db_has_description():
    conn = psycopg2.connect(**PG_CFG)
    try:
        with conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema='public'
                      AND table_name='OemToCars'
                      AND column_name='Description'
                """)
                exists = cur.fetchone()
                if not exists:
                    cur.execute('ALTER TABLE "OemToCars" ADD COLUMN "Description" TEXT')
    finally:
        conn.close()

# --- Helper: Mark GroupCode rows as done when product found ---
def mark_groupcode_done(oem: str) -> None:
    """
    Ürün bulunduğunda (rows > 0) ilgili OEM'i içeren GroupCode satırlarını 99 yap.
    Ürün bulunmazsa çağrılmayacak.
    """
    conn = psycopg2.connect(**PG_CFG)
    try:
        with conn:
            with conn.cursor() as cur:
                cur.execute(
                    """
                    UPDATE "ProductGroupCodes"
                    SET "Status" = 99
                    WHERE regexp_replace(upper("GroupCode"), '[^0-9A-Z]', '', 'g')
                          LIKE '%%' || regexp_replace(upper(%s), '[^0-9A-Z]', '', 'g') || '%%'
                    """,
                    [oem]
                )
    finally:
        conn.close()

UPSERT_SQL = """
INSERT INTO "OemToCars" (
    "OEM","Title","Brand","Model","Engine","EngineCode","Fuel","Gearbox","Body","Chassis",
    "VIN","Position","MileageKm","Year","Years","ArticleNumber","OriginalNumbers",
    "Price","Currency","ImageUrl","Description","SourceUrl","Source","Status","CreatedId","CreatedDate"
) VALUES (
    %(OEM)s, %(Title)s, %(Brand)s, %(Model)s, %(Engine)s, %(EngineCode)s, %(Fuel)s, %(Gearbox)s, %(Body)s, %(Chassis)s,
    %(VIN)s, %(Position)s, %(MileageKm)s, %(Year)s, %(Years)s, %(ArticleNumber)s, %(OriginalNumbers)s,
    %(Price)s, %(Currency)s, %(ImageUrl)s, %(Description)s, %(SourceUrl)s, %(Source)s, %(Status)s, %(CreatedId)s, NOW()
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
    "Description"     = EXCLUDED."Description",
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
                        # removed per-row ProductGroupCodes status update
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
    logging.info("=== Otoparcasan OEM -> OemToCars başladı ===")
    ensure_unique_index()
    ensure_db_has_description()
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=HEADLESS_RUNTIME)
        # --- basic stealth to avoid the firewall page ---
        # Spoof webdriver and some commonly-detected properties
        stealth_js = """
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
        window.chrome = { runtime: {} };
        Object.defineProperty(navigator, 'languages', { get: () => ['tr-TR','tr','en-US','en'] });
        Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3,4,5] });
        """
        ctx = await browser.new_context(
            locale="tr-TR",
            viewport={"width": 1366, "height": 900},
            user_agent=random.choice(UA_POOL),
            extra_http_headers={
                "Accept-Language": "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7",
            }
        )
        await ctx.add_init_script(stealth_js)
        page = await ctx.new_page()

        # TEST MODE: only run for a single OEM to speed up iteration
        for oem in ["55177460"]:
            try:
                rows = await scrape_one_oem(page, oem)
                if rows:
                    cnt = db_upsert_rows(rows)
                    logging.info(f"[UPSERT] oem={oem} rows={len(rows)} upserted={cnt}")
                    # Ürün bulunduğu için bu OEM'i içeren GroupCode kayıtlarını 99 yap
                    try:
                        mark_groupcode_done(oem)
                    except Exception as ex:
                        logging.error(f"[PGC] Status=99 güncellemesi hata oem={oem}: {ex}")
                else:
                    logging.info(f"[UPSERT] oem={oem} sonuç yok (Status değişmeyecek)")
            except Exception as e:
                logging.error(f"[MAIN] oem={oem} genel hata: {e}")
            # genel rate limit
            await page.wait_for_timeout(800)

        await browser.close()
    logging.info("=== Bitti ===")

if __name__ == "__main__":
    asyncio.run(main())