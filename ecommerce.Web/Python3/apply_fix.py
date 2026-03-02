import psycopg2
import os

def apply_sql():
    try:
        # Read the SQL file
        sql_path = "/Users/sezgin/Repos/ecommerce/ecommerce.Web/Sql/sync_products_from_otoismails.sql"
        with open(sql_path, "r") as f:
            sql_content = f.read()

        # Connect to DB
        conn = psycopg2.connect(
            host="localhost",
            port=5454,
            database="MarketPlace",
            user="myinsurer",
            password="Posmdh0738"
        )
        cursor = conn.cursor()

        # Execute
        print("Executing SQL update...")
        cursor.execute(sql_content)
        conn.commit()
        print("✅ Procedure updated successfully.")

        cursor.close()
        conn.close()
    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    apply_sql()
