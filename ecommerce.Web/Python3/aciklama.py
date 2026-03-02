import psycopg2
from psycopg2.extras import execute_batch
import re

# Parça açıklamaları
PART_DESCRIPTION = {
    "helezon": "Araç süspansiyon sisteminin önemli bir parçasıdır. Sarsıntıları emer, sürüş konforunu artırır.",
    "fren": "Aracın güvenliğini sağlayan fren sisteminde kritik bir rol oynar, yüksek performans ve dayanıklılık sunar.",
    "motor": "Motorun verimli çalışması için üretilmiş dayanıklı bir parçadır, uzun ömürlüdür.",
    "şanzıman": "Güç aktarımında görev alan önemli bir parçadır, aracınızın performansını etkiler.",
    "akü": "Aracın elektrik sistemine enerji sağlar, güvenilir ve uzun ömürlüdür.",
    "filtre": "Motorunuzu ve yakıt sisteminizi kirletici maddelerden korur, bakım gerektirir.",
    "kaporta": "Aracın dış görünüşünü tamamlayan estetik ve koruyucu parçadır.",
    "yağ": "Motor ve sistemlerin sağlıklı çalışması için gereklidir, düzenli değişim önerilir.",
    "egzoz": "Egzoz gazlarının tahliyesini sağlar, çevre dostudur.",
    "süspansiyon": "Aracın yol tutuşunu ve sürüş konforunu sağlar.",
    "aydınlatma": "Aracın görünürlüğünü artırır, gece sürüşlerinde güvenlik sağlar.",
    "bakım": "Aracınızın performansını artıran bakım ve temizlik ürünleridir.",
    "aksesuar": "Aracınıza şıklık ve fonksiyonellik katan ekstra parçalardır.",
}

# Marka notları
BRAND_NOTES = {
    "gunsan": "Türkiye'nin önde gelen kaliteli yedek parça üreticisidir.",
    "bosch": "Dünya çapında güvenilir otomotiv parçaları sunar.",
    "valeo": "Teknolojik yenilikleriyle öncü markalardan biridir.",
}

# Araç modelleri
CAR_MODELS = [
    "tempra", "ducato", "boxer", "partner", "focus", "clio", "c4", "p306", "p307",
    "grand scenic", "fiorino", "bipper", "twingo", "avensis", "m131", "punto", "berlingo",
    "r12", "sportage", "tucson", "kartal", "lodgy", "dokker", "transit", "getz", "tipo",
    "p207", "c-elysee", "accent", "fiat", "peugeot", "citroen", "renault", "ford", "opel",
    "volkswagen", "bmw", "audi", "mercedes", "toyota", "honda", "hyundai", "nissan",
]

def simplify_oem(oem_raw):
    if not oem_raw:
        return "OEM kodu mevcut değil."
    # Hem | hem de boşlukları (space, tab, newline) ayraç olarak kabul ederek böl
    parts = re.split(r'[|\s]+', oem_raw.strip())
    # Boş olmayanları temizle ve sadece ilk 5 tanesini al
    parts = [part.strip() for part in parts if part.strip()][:5]
    return ", ".join(parts)

def find_part_type(text):
    text = text.lower()
    for key in PART_DESCRIPTION:
        if key in text:
            return key
    return None

def find_car_models(text):
    text = text.lower()
    found = {model.title() for model in CAR_MODELS if re.search(r'\b' + re.escape(model) + r'\b', text)}
    return ", ".join(sorted(found)) if found else None

def generate_description(name, brand, oem):
    brand_display = brand.title() if brand else "Bilinmeyen Marka"
    oem_display = simplify_oem(oem)
    text_combined = f"{name} {brand} {oem}".lower()

    part_type = find_part_type(text_combined)
    car_models = find_car_models(name)

    base_desc = f"{brand_display} markasına ait {name} ürünüdür."
    if car_models:
        base_desc += f" Bu ürün, {car_models} gibi araç modelleriyle uyumludur."
    oem_desc = f" OEM kodları: {oem_display}."
    part_desc = PART_DESCRIPTION.get(part_type, "Araçlarınız için yüksek kaliteli ve dayanıklı bir yedek parçadır.")
    brand_note = BRAND_NOTES.get(brand.lower(), "")

    return " ".join(f"{base_desc} {oem_desc} {part_desc} {brand_note}".split())

# DB bağlantısı
conn = psycopg2.connect(
    host="92.204.172.6", port=5454, database="MarketPlace", user="myinsurer", password="Posmdh0738"
)
cursor = conn.cursor()

def fetch_products_to_fix(limit=None):
    sql = """
    SELECT p."Id", p."Name", COALESCE(b."Name", '') AS BrandName,
           COALESCE(string_agg(DISTINCT pgc."OemCode", ' '), '') AS OEM_Codes
    FROM "Product" p
    LEFT JOIN "Brand" b ON p."BrandId" = b."Id"
    LEFT JOIN "ProductGroupCodes" pgc ON pgc."ProductId" = p."Id"
    WHERE p."Description" IS NULL OR p."Description" LIKE '%markasına ait%ürünüdür%'
    GROUP BY p."Id", p."Name", b."Name"
    """
    if limit:
        sql += f" LIMIT {limit}"
    cursor.execute(sql)
    return cursor.fetchall()

def update_descriptions(products, batch_size=30000):
    total = len(products)
    print(f"{total} ürün açıklaması güncellenecek...")
    batch = []

    for i, (pid, name, brand, oem) in enumerate(products, start=1):
        desc = generate_description(name, brand, oem)
        batch.append((desc, pid))

        if i % batch_size == 0:
            execute_batch(cursor,
                          'UPDATE "Product" SET "Description" = %s WHERE "Id" = %s',
                          batch)
            conn.commit()
            print(f"{i} ürün güncellendi...")
            batch.clear()

    if batch:
        execute_batch(cursor,
                      'UPDATE "Product" SET "Description" = %s WHERE "Id" = %s',
                      batch)
        conn.commit()
        print(f"{len(products)} ürün güncellendi.")

def main():
    print("Düzenlenecek (boş veya hatalı) ürünler alınıyor...")
    products = fetch_products_to_fix()
    if products:
        update_descriptions(products)
    else:
        print("Güncellenecek ürün bulunamadı.")

if __name__ == "__main__":
    main()
