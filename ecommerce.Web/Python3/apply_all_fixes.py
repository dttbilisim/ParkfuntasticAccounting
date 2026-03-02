import psycopg2
import os

def apply_sql_files():
    sql_files = [
        "sync_products_from_otoismails.sql",
        "sync_products_from_remars.sql",
        "sync_products_from_dega.sql",
        "sync_products_from_basbugs.sql"
    ]
    
    base_path = "/Users/sezgin/Repos/ecommerce/ecommerce.Web/Sql/"
    
    try:
        # Connect to DB
        conn = psycopg2.connect(
            host="localhost",
            port=5454,
            database="MarketPlace",
            user="myinsurer",
            password="Posmdh0738"
        )
        cursor = conn.cursor()
        print("🔌 Connected to database.")

        for sql_file in sql_files:
            file_path = os.path.join(base_path, sql_file)
            print(f"📖 Reading {sql_file}...")
            
            with open(file_path, "r") as f:
                sql_content = f.read()

            print(f"⚙️  Executing {sql_file}...")
            cursor.execute(sql_content)
            conn.commit()
            print(f"✅ {sql_file} applied successfully.")

        cursor.close()
        conn.close()
        print("\n🎉 All procedures updated successfully!")
        
    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    apply_sql_files()
