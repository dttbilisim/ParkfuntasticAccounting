import psycopg2
import time
import subprocess

def run_global_sync():
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
        
        # 1. Apply Basbug Fix
        print("📖 Reading sync_products_from_basbugs.sql...")
        with open("ecommerce.Web/Sql/sync_products_from_basbugs.sql", "r") as f:
            sql_content = f.read()

        print("🛠 Applying Basbug Stored Procedure...")
        cursor.execute(sql_content)
        conn.commit()
        print("✅ Basbug Procedure applied.")

        # 2. Execute All Sync Procedures
        procedures = [
            "sync_products_from_remars",
            "sync_products_from_dega",
            "sync_products_from_basbugs",
            "sync_products_from_otoismails" # Run again to be sure
        ]

        for proc in procedures:
            print(f"⚙️ Executing {proc}()...")
            start_time = time.time()
            cursor.execute(f"CALL {proc}();")
            conn.commit()
            print(f"   ✅ Completed in {time.time() - start_time:.2f}s")
            
        conn.close()

        # 3. Re-index Everything (using product.py or similar logic)
        # Since product.py orchestrates everything, we could just run it?
        # But product.py ALSO calls the sync procedures.
        # So essentially, running product.py would do Step 2 and 3.
        # However, I want to control the execution to be sure procedures are updated first.
        # Now that procedures are updated (Step 1), I can just run product.py?
        # OR I can run the indexing part separately.
        # Start product.py might take long and repeat work.
        # I'll rely on product.py for indexing if possible, but let's check if we can just trigger indexing.
        # Looking at product.py, it seems coupled.
        # Instead, I will assume the user wanted me to "review" product.py.
        # I'll create a simple re-indexer script here that behaves like product.py's indexing block.
        
        print("\n🔄 Starting Global Re-indexing...")
        # Running product.py is the safest bet to ensure environment consistency
        # But might duplicate the syncs I just did.
        # That's fine, it ensures correctness.
        # "python3 ecommerce.Web/Python3/product.py"
        
        subprocess.run(["python3", "ecommerce.Web/Python3/product.py"], check=True)
        print("✅ Global Sync & Indexing Logic Complete.")

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    run_global_sync()
