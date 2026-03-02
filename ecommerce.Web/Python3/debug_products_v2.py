import psycopg2

def analyze_data():
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
        
        oem_query = '%7701477028%'
        
        print(f"🔍 Analyzing ProductOtoIsmails for OEM LIKE '{oem_query}'")
        
        cursor.execute("""
            SELECT 
                "Id", 
                "Kod", 
                "OrjinalKod", 
                "Oem", 
                "Marka",
                COALESCE(GREATEST("Gebze", "Ankara", "Ikitelli", "Izmir", "Samsun", "Depo1030", "Depo13"), 0) as CalcStock,
                "Fiyat1",
                "Fiyat3"
            FROM "ProductOtoIsmails" 
            WHERE "Oem" LIKE %s
        """, (oem_query,))
        
        rows = cursor.fetchall()
        print(f"📊 Found {len(rows)} records in ProductOtoIsmails:")
        
        sample_group_codes = []
        
        for row in rows:
            # Construct GroupCode logic from SQL: 
            # COALESCE(NULLIF(TRIM(Kod), ''), '') || '|' || COALESCE(NULLIF(TRIM(OrjinalKod), ''), '') || '|' || COALESCE(NULLIF(TRIM(Oem), ''), '')
            kod = (row[1] or '').strip() or ''
            orj_kod = (row[2] or '').strip() or ''
            oem = (row[3] or '').strip() or ''
            
            # Mimic SQL logic roughly
            group_code = f"{kod}|{orj_kod}|{oem}"
            
            print(f" - ID: {row[0]}, Marka: {row[4]}, Stock: {row[5]}, GroupCode(Calc): {group_code}")
            sample_group_codes.append(group_code)

        print(f"\n🔍 Checking Product/SellerItems table for matches:")
        
        # Check by OEM match in Product table
        print("\n--- Listing ALL Products matching OEM in Product Table ---")
        cursor.execute('SELECT "Id", "Name", "Oems", "SellerId" FROM "Product" WHERE "Oems" LIKE %s', (oem_query,))
        prod_rows = cursor.fetchall()
        print(f"Found {len(prod_rows)} products in Product table:")
        for row in prod_rows:
            print(f" - ProdID: {row[0]}, SellerId: {row[3]}, Oems: {row[2]}, Name: {row[1]}")
            
            # Check SellerItems
            cursor.execute('SELECT "SellerId", "Stock", "SalePrice" FROM "SellerItems" WHERE "ProductId" = %s', (row[0],))
            sis = cursor.fetchall()
            for si in sis:
                print(f"   -> SellerItem: SellerId={si[0]}, Stock={si[1]}, Price={si[2]}")

        # Check specifically if GroupCodes exist in ProductGroupCodes
        print("\n--- Checking ProductGroupCodes for sample derived codes ---")
        for gc in sample_group_codes[:5]: # Check first 5
            cursor.execute('SELECT "ProductId", "GroupCode" FROM "ProductGroupCodes" WHERE "GroupCode" = %s', (gc,))
            pgc_rows = cursor.fetchall()
            if pgc_rows:
                for pgc in pgc_rows:
                    print(f" ✅ Found GroupCode '{gc}' -> ProductId: {pgc[0]}")
            else:
                 print(f" ❌ GroupCode '{gc}' NOT FOUND in ProductGroupCodes")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    analyze_data()
