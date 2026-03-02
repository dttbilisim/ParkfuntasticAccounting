import psycopg2
import requests
import json
import datetime

def reindex_product():
    # DB Params
    db_params = {
        "host": "92.204.172.6",
        "port": 5454,
        "database": "MarketPlace",
        "user": "myinsurer",
        "password": "Posmdh0738"
    }
    
    es_url = "http://92.204.172.6:9200/sellerproduct_index/_doc"
    es_auth = ("elastic", "itO5M3EZrbc96K_42ah3")

    try:
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()
        
        target_id = 16293
        print(f"🔄 Re-indexing Product {target_id}...")

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
            P."Barcode" AS "ProductBarcode",
            P."Status" AS "ProductStatus",
            P."DocumentUrl",
            -- P."MainImageUrl",
            
            P."BrandId",
            -- P."CategoryId",
            P."TaxId",
            
            -- PGC... removed
            
            PGC."GroupCode",
            
            P."Oems"
            
        FROM "SellerItems" SI
        JOIN "Product" P ON P."Id" = SI."ProductId"
        JOIN "Sellers" S ON S."Id" = SI."SellerId"
        LEFT JOIN (
            SELECT DISTINCT ON ("ProductId") *
            FROM "ProductGroupCodes"
            ORDER BY "ProductId", "Id" ASC
        ) PGC ON PGC."ProductId" = P."Id"
        
        WHERE P."Id" = {target_id}
        """
        
        cursor.execute(query)
        columns = [desc[0] for desc in cursor.description]
        rows = cursor.fetchall()
        
        if not rows:
            print("❌ No data found for this product in SellerItems view!")
            return

        print(f"📊 Found {len(rows)} SellerItems for Product {target_id}. Indexing...")
        
        for row in rows:
            doc = dict(zip(columns, row))
            
            from decimal import Decimal
            # Convert datetime and Decimal objects
            for k, v in doc.items():
                if isinstance(v, (datetime.date, datetime.datetime)):
                    doc[k] = v.isoformat()
                elif isinstance(v, Decimal):
                    doc[k] = float(v)
            
            doc_id = str(doc["SellerItemId"])
            url = f"{es_url}/{doc_id}"
            
            # Using PUT to create/update
            response = requests.put(url, json=doc, auth=es_auth)
            
            if response.status_code in [200, 201]:
                print(f"✅ Indexed SellerItemId {doc_id}: {response.json()['result']}")
            else:
                print(f"❌ Failed to index {doc_id}: {response.text}")

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    reindex_product()
