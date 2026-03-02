import psycopg2

def inspect_otoismail_full():
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
        
        # 1. Get Columns
        cursor.execute('SELECT * FROM "ProductOtoIsmails" LIMIT 0')
        colnames = [desc[0] for desc in cursor.description]
        print(f"Columns: {colnames}")
        
        # 2. Dump all rows for the OEM
        print("\n--- Rows for %7701477028% ---")
        cursor.execute('SELECT * FROM "ProductOtoIsmails" WHERE "Oem" LIKE \'%7701477028%\'')
        rows = cursor.fetchall()
        print(f"Total Rows: {len(rows)}")
        
        for row in rows:
            # Create a dict for easier view
            row_dict = dict(zip(colnames, row))
            
            # Print only non-zero numeric fields or relevant text
            print(f"\nID: {row_dict.get('Id')}")
            print(f"  Kod: {row_dict.get('Kod')}")
            print(f"  Brand: {row_dict.get('Marka')}")
            print(f"  Oem: {row_dict.get('Oem')}")
            
            # Print potential stock fields
            stock_candidates = []
            for k, v in row_dict.items():
                if isinstance(v, (int, float,  type(None))): # primitive check
                    try:
                        val = float(v) if v is not None else 0
                        if val > 0 and k not in ['Id', 'Fiyat1', 'Fiyat2', 'Fiyat3', 'Fiyat4', 'Kdv', 'CreatedId', 'ModifiedId', 'Payda']:
                            stock_candidates.append(f"{k}={val}")
                    except:
                        pass
            print(f"  Positive Numeric (Candidates): {', '.join(stock_candidates)}")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    inspect_otoismail_full()
