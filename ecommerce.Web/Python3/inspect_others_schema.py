import psycopg2

def inspect_others():
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
        
        tables = ["ProductRemars", "ProductDegas", "ProductBasbugs"]
        
        for table in tables:
            print(f"\n--- Checking {table} ---")
            cursor.execute(f'SELECT * FROM "{table}" LIMIT 0')
            colnames = [desc[0] for desc in cursor.description]
            print(f"Columns: {colnames}")
            
            # Check for OEM-like columns
            oem_cols = [c for c in colnames if 'oem' in c.lower() or 'kod' in c.lower() or 'no' in c.lower()]
            print(f"Potential ID/OEM Cols: {oem_cols}")
            
            # Check for Stock-like columns
            stock_cols = [c for c in colnames if 'stok' in c.lower() or 'depo' in c.lower() or 'adet' in c.lower() or 'quantity' in c.lower()]
            print(f"Potential Stock Cols: {stock_cols}")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    inspect_others()
