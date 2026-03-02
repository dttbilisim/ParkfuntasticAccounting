import psycopg2

def verify_state():
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
        
        print("\n🔍 Checking ProductGroupCodes Table Columns...")
        cursor.execute('SELECT * FROM "ProductGroupCodes" LIMIT 0')
        colnames = [desc[0] for desc in cursor.description]
        print(f"   Columns: {colnames}")


        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    verify_state()
