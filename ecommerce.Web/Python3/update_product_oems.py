import psycopg2

def fix_product_oems():
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
        
        pid = 16293
        search_term = "7701477028"
        
        print(f"🛠 Updating Product {pid} Oems...")
        
        cursor.execute('SELECT "Oems" FROM "Product" WHERE "Id" = %s', (pid,))
        current_oems = cursor.fetchone()[0] or ""
        
        if search_term not in current_oems:
            new_oems = current_oems + "|" + search_term
            cursor.execute('UPDATE "Product" SET "Oems" = %s WHERE "Id" = %s', (new_oems, pid))
            conn.commit()
            print(f"✅ Updated Oems. New length: {len(new_oems)}")
        else:
            print("ℹ️ OEM already present.")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    fix_product_oems()
