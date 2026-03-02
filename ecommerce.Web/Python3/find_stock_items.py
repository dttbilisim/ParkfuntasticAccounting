import psycopg2

def find_missing_stock_items():
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
        
        print(f"🔍 Searching ALL in-stock items in ProductOtoIsmails for OEM LIKE '{oem_query}'")
        
        cursor.execute("""
            SELECT 
                "Id", 
                "Kod", 
                "OrjinalKod", 
                "Oem", 
                "Marka", 
                COALESCE(GREATEST("Gebze", "Ankara", "Ikitelli", "Izmir", "Samsun", "Depo1030", "Depo13"), 0) as CalcStock,
                "Fiyat1"
            FROM "ProductOtoIsmails" 
            WHERE "Oem" LIKE %s
        """, (oem_query,))
        
        rows = cursor.fetchall()
        
        stock_items = []
        for row in rows:
            stock = float(row[5])
            if stock > 0:
                stock_items.append(row)

        print(f"📊 Found {len(stock_items)} items with STOCK > 0 associated with this OEM:")
        
        for row in stock_items:
            # row: Id, Kod, Orj, Oem, Marka, Stock, Price
            raw_id = row[0]
            marka = row[4]
            stock = row[5]
            
            # Generate GroupCode
            def clean(val): return (val or '').strip() or ''
            kod = clean(row[1])
            orj = clean(row[2])
            oem = clean(row[3])
            group_code = f"{kod}|{orj}|{oem}"
            
            print(f"\n📦 Item ID: {raw_id} | Marka: {marka} | Stock: {stock}")
            
            # Map to Product
            cursor.execute('SELECT "ProductId" FROM "ProductGroupCodes" WHERE "GroupCode" = %s', (group_code,))
            pg_row = cursor.fetchone()
            
            if pg_row:
                pid = pg_row[0]
                print(f"   Refers to ProductId: {pid}")
                
                # Check Product Status
                cursor.execute('SELECT "Name", "Status" FROM "Product" WHERE "Id" = %s', (pid,))
                prod_row = cursor.fetchone()
                if prod_row:
                    p_name, p_status = prod_row
                    print(f"   Product Status: {p_status} ('{p_name}')")
                else:
                     print(f"   ❌ ProductId {pid} NOT FOUND in Product table!")
                
                # Check SellerItems
                cursor.execute('SELECT "Stock", "Status", "SalePrice" FROM "SellerItems" WHERE "ProductId" = %s AND "SellerId" = 1', (pid,))
                si_row = cursor.fetchone()
                if si_row:
                    si_stock, si_status, si_price = si_row
                    print(f"   SellerItem (OtoIsmail): Stock={si_stock}, Status={si_status}, Price={si_price}")
                else:
                    print(f"   ❌ NO SellerItem found for OtoIsmail!")
            else:
                 print(f"   ❌ GroupCode '{group_code}' NOT FOUND in ProductGroupCodes.")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    find_missing_stock_items()
