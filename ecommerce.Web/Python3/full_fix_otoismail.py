import psycopg2
import requests
import json
import time

def solve_issue():
    # DB Params
    db_params = {
        "host": "92.204.172.6",
        "port": 5454,
        "database": "MarketPlace",
        "user": "myinsurer",
        "password": "Posmdh0738"
    }

    try:
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()
        
        # 1. Read the updated SQL file
        print("📖 Reading updated sync_products_from_otoismails.sql...")
        with open("ecommerce.Web/Sql/sync_products_from_otoismails.sql", "r") as f:
            sql_content = f.read()

        # 2. Apply the Procedure (CREATE OR REPLACE)
        print("🛠 Applying updated Stored Procedure...")
        cursor.execute(sql_content)
        conn.commit()
        print("✅ Procedure applied successfully.")

        # 3. CALL the Procedure to sync data
        print("⚙️ Executing sync_products_from_otoismails()... This may take a moment.")
        start_time = time.time()
        cursor.execute("CALL sync_products_from_otoismails();")
        conn.commit()
        print(f"✅ Sync completed in {time.time() - start_time:.2f} seconds.")

        # 4. Identify Affected Products (matching the user's OEM) to Re-Index
        print("\n🔍 Identifying products to re-index (matching %7701477028%)...")
        oem_query = '%7701477028%'
        
        # Find ProductIds. Since sync is done, SellerItems should be updated.
        cursor.execute("""
            SELECT DISTINCT P."Id"
            FROM "Product" P
            JOIN "SellerItems" SI ON SI."ProductId" = P."Id"
            WHERE P."Oems" LIKE %s AND SI."SellerId" = 1 AND SI."Stock" > 0
        """, (oem_query,))
        
        product_ids = [row[0] for row in cursor.fetchall()]
        print(f"📊 Found {len(product_ids)} Products to re-index: {product_ids}")

        # 5. Re-Index these products
        es_url = "http://92.204.172.6:9200/sellerproduct_index/_doc"
        es_auth = ("elastic", "itO5M3EZrbc96K_42ah3")
        
        from decimal import Decimal
        import datetime

        for pid in product_ids:
            print(f"🔄 Re-indexing Product {pid}...")
            
            # Fetch Data (Simplified Query)
            query = f"""
            SELECT 
                SI."Id" AS "SellerItemId",
                SI."SellerId",
                S."Name" AS "SellerName",
                SI."Stock",
                SI."CostPrice",
                SI."SalePrice",
                SI."Commision",
                SI."Currency",
                SI."Unit",
                SI."Status" AS "SellerStatus",
                SI."ModifiedDate" AS "SellerModifiedDate",
                SI."SourceId",
                SI."Step",
                SI."MinSaleAmount",
                SI."MaxSaleAmount",
                P."Id" AS "ProductId",
                P."Name" AS "ProductName",
                P."Description" AS "ProductDescription",
                P."Oems",
                PGC."GroupCode"
            FROM "SellerItems" SI
            JOIN "Product" P ON P."Id" = SI."ProductId"
            JOIN "Sellers" S ON S."Id" = SI."SellerId"
             LEFT JOIN (
                SELECT DISTINCT ON ("ProductId") *
                FROM "ProductGroupCodes"
                ORDER BY "ProductId", "Id" ASC
            ) PGC ON PGC."ProductId" = P."Id"
            WHERE P."Id" = {pid}
            """
            cursor.execute(query)
            cols = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            
            for row in rows:
                doc = dict(zip(cols, row))
                # Serialization fix
                for k, v in doc.items():
                   if isinstance(v, (datetime.date, datetime.datetime)):
                       doc[k] = v.isoformat()
                   elif isinstance(v, Decimal):
                       doc[k] = float(v)

                url = f"{es_url}/{doc['SellerItemId']}"
                resp = requests.put(url, json=doc, auth=es_auth)
                if resp.status_code in [200, 201]:
                     print(f"   ✅ Indexed Item {doc['SellerItemId']}")
                else:
                     print(f"   ❌ Index Error {doc['SellerItemId']}: {resp.text}")

        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    solve_issue()
