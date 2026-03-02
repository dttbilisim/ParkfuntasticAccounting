import psycopg2
import json

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
                -- AdvertCount removed
                COALESCE(GREATEST("Gebze", "Ankara", "Ikitelli", "Izmir", "Samsun", "Depo1030", "Depo13"), 0) as CalcStock,
                "Fiyat1",
                "Fiyat3"
            FROM "ProductOtoIsmails" 
            WHERE "Oem" LIKE %s
        """, (oem_query,))
        
        rows = cursor.fetchall()
        print(f"📊 Found {len(rows)} records in ProductOtoIsmails:")
        for row in rows:
            print(f" - ID: {row[0]}, Marka: {row[4]}, Kod: {row[1]}, Oem: {row[3]}, Stock: {row[6]}")

        # Check Product table
        print(f"\n🔍 Checking Product table for matches:")
        cursor.execute('SELECT "Id", "Name", "Oems", "SellerId" FROM "Product" WHERE "Oems" LIKE %s', (oem_query,))
        prod_rows = cursor.fetchall()
        for row in prod_rows:
            print(f" - ProdID: {row[0]}, SellerId: {row[3]}, Oems: {row[2]}, Name: {row[1]}")
            
            # Check SellerItems for this Product
            cursor.execute('SELECT "SellerId", "Stock", "SellerName" FROM "SellerItems" WHERE "ProductId" = %s', (row[0],))
            seller_items = cursor.fetchall()
            for si in seller_items:
                print(f"   -> SellerItem: SellerId={si[0]}, Stock={si[1]}")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    analyze_data()
