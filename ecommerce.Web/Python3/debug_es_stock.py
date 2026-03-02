import requests
import json
import base64

# Config
ES_URL = "http://92.204.172.6:9200"
ES_USER = "elastic"
ES_PASS = "itO5M3EZrbc96K_42ah3"
INDEX = "sellerproduct_index"

# Query: replicate the C# query logic roughly
# Must have OemCode=85008011 AND Stock > 0 AND SalePrice > 0 AND SourceId exists
query = {
    "size": 50,
    "query": {
        "bool": {
            "must": [
                {
                    "bool": {
                        "should": [
                           {"match": {"OemCode": "85008011"}},
                           {"match": {"PartNumber": "85008011"}}
                        ],
                        "minimum_should_match": 1
                    }
                }
            ],
            "filter": [
                {"range": {"SalePrice": {"gt": 0}}},
                {"exists": {"field": "SourceId"}},
                {"range": {"Stock": {"gt": 0}}}
            ]
        }
    },
    "_source": ["SellerItemId", "ProductId", "PartNumber", "OemCode", "Stock", "SellerId", "SellerName", "SalePrice", "SourceId"]
}

def check_stock():
    url = f"{ES_URL}/{INDEX}/_search"
    auth = (ES_USER, ES_PASS)
    
    print(f"Connecting to {url}...")
    try:
        resp = requests.post(url, json=query, auth=auth, timeout=10)
        resp.raise_for_status()
        data = resp.json()
        
        hits = data.get("hits", {}).get("hits", [])
        total = data.get("hits", {}).get("total", {}).get("value", 0)
        
        print(f"Total Hits: {total}")
        print("-" * 60)
        print(f"{'SellerName':<20} {'Stock':<10} {'Price':<10} {'PartNumber':<15} {'OemCode'}")
        print("-" * 60)
        
        for h in hits:
            src = h["_source"]
            oem = src.get("OemCode", [])
            if isinstance(oem, list): oem = ",".join(oem[:1])
            
            s_name = str(src.get('SellerName', 'MISSING'))
            stock = src.get('Stock', 0)
            price = src.get('SalePrice', 0)
            p_num = str(src.get('PartNumber', 'MISSING'))
            
            print(f"{s_name:<20} {stock:<10} {price:<10} {p_num:<15} {oem}")
            
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    check_stock()
