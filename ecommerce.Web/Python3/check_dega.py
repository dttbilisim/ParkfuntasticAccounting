import psycopg2

def check_dega_depos():
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
        
        print("Checking Dega Depo columns...")
        # Check if Depo1 (no underscore) has distinct values from Depo_1
        cursor.execute('''
            SELECT "Depo1", "Depo_1", "Depo2", "Depo_2" 
            FROM "ProductDegas" 
            WHERE "Depo1" IS NOT NULL OR "Depo_1" IS NOT NULL
            LIMIT 10
        ''')
        rows = cursor.fetchall()
        for r in rows:
            print(f"Depo1: {r[0]}, Depo_1: {r[1]}, Depo2: {r[2]}, Depo_2: {r[3]}")
            
    except Exception as e:
        print(f"Error: {e}")
    finally:
        conn.close()

if __name__ == "__main__":
    check_dega_depos()
