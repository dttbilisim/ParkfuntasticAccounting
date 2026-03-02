import psycopg2

def trace_dayco():
    conn_params = {
        "host": "92.204.172.6",
        "port": 5454,
        "database": "MarketPlace",
        "user": "myinsurer",
        "password": "Posmdh0738"
    }

    try:
        conn = psycopg2.connect(**conn_params)
        cursor = conn.cursor()
        
        # DAYCO Item from previous analysis
        # ID: 106864
        # Kod: DAYCO KTB532
        # Oem: 7701477028-7701476571-1680600QA8
        
        # Reconstruct GroupCode exactly as SQL does
        # COALESCE(NULLIF(TRIM(t."Kod"), ''), '') || '|' || COALESCE(NULLIF(TRIM(t."OrjinalKod"), ''), '') || '|' || COALESCE(NULLIF(TRIM(t."Oem"), ''), '')
        # OriginalCode is null/empty for this item in previous analysis?
        # inspect_otoismail.py didn't print OrjinalKod but printed non-zero fields.
        # Let's query it exactly.
        
        print("🔍 Fetching DAYCO item 106864 source data...")
        cursor.execute('SELECT "Kod", "OrjinalKod", "Oem", "StokSayisi" FROM "ProductOtoIsmails" WHERE "Id" = 106864')
        row = cursor.fetchone()
        if not row:
            print("❌ DAYCO Item 106864 NOT FOUND in source!")
            return

        kod, orj, oem, stok = row
        print(f"   Source: Kod='{kod}', Orj='{orj}', Oem='{oem}', StokSayisi={stok}")
        
        def clean(val): return (val or '').strip() or ''
        group_code = f"{clean(kod)}|{clean(orj)}|{clean(oem)}"
        print(f"   Generated GroupCode: '{group_code}'")
        
        print(f"\n🔍 checking ProductGroupCodes for '{group_code}'...")
        cursor.execute('SELECT "ProductId" FROM "ProductGroupCodes" WHERE "GroupCode" = %s', (group_code,))
        pg_row = cursor.fetchone()
        
        if pg_row:
            pid = pg_row[0]
            print(f"   ✅ Mapped to ProductId: {pid}")
            
            # Check Product
            cursor.execute('SELECT "Name", "Oems", "Status" FROM "Product" WHERE "Id" = %s', (pid,))
            prod = cursor.fetchone()
            print(f"   Product: Name='{prod[0]}', Status={prod[2]}")
            print(f"   Product Oems: '{prod[1]}'")
            
            if "7701477028" in (prod[1] or ""):
                print("   ✅ OEM match in Product!")
            else:
                 print("   ❌ OEM NOT FOUND in Product Oems!")
                 
            # Check SellerItems
            cursor.execute('SELECT "Stock", "Status" FROM "SellerItems" WHERE "ProductId" = %s AND "SellerId" = 1', (pid,))
            si = cursor.fetchone()
            if si:
                print(f"   SellerItem: Stock={si[0]}, Status={si[1]}")
            else:
                print("   ❌ SellerItem NOT FOUND!")
                
        else:
            print("   ❌ GroupCode NOT FOUND in ProductGroupCodes!")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    trace_dayco()
