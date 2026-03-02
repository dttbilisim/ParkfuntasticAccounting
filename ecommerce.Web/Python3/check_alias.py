from elasticsearch import Elasticsearch

es = Elasticsearch(
    ["http://92.204.172.6:9200"],
    basic_auth=("elastic", "itO5M3EZrbc96K_42ah3")
)

alias_name = "sellerproduct_index"

if es.indices.exists_alias(name=alias_name):
    indices = es.indices.get_alias(name=alias_name).keys()
    print(f"Alias '{alias_name}' points to indices: {list(indices)}")
    
    if len(indices) > 1:
        print("⚠️  WARNING: Alias points to MULTIPLE indices! This causes duplicate search results.")
    else:
        print("✅ Alias is healthy (points to exactly one index).")
        
        # Check doc count
        index_name = list(indices)[0]
        count = es.count(index=index_name)['count']
        print(f"📊 Document count in {index_name}: {count}")

else:
    print(f"❌ Alias '{alias_name}' does NOT existing!")
    # Check if concrete index exists
    if es.indices.exists(index=alias_name):
         print(f"⚠️  But a concrete index named '{alias_name}' EXISTS.")
