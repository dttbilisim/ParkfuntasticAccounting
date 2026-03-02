# Şu anda bulunduğunuz dizinde çalıştırın (ecommerce.EP klasöründe)

# 1. ILM Policy oluştur
curl -X PUT "http://92.204.172.6:9200/_ilm/policy/ecommerce-logs-30d-retention" \
  -u "elastic:itO5M3EZrbc96K_42ah3" \
  -H 'Content-Type: application/json' \
  -d '{
  "policy": {
    "phases": {
      "hot": {
        "min_age": "0ms",
        "actions": {
          "rollover": {
            "max_age": "7d",
            "max_primary_shard_size": "50gb"
          },
          "set_priority": {
            "priority": 100
          }
        }
      },
      "delete": {
        "min_age": "30d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}'

# 2. Index Template oluştur
curl -X PUT "http://92.204.172.6:9200/_index_template/ecommerce-logs-template" \
  -u "elastic:itO5M3EZrbc96K_42ah3" \
  -H 'Content-Type: application/json' \
  -d '{
  "index_patterns": ["ecommerce-logs-*"],
  "template": {
    "settings": {
      "index.lifecycle.name": "ecommerce-logs-30d-retention",
      "number_of_shards": 2,
      "number_of_replicas": 1
    }
  },
  "priority": 500
}'

# 3. Mevcut index'lere policy uygula
curl -X PUT "http://92.204.172.6:9200/ecommerce-logs-*/_settings" \
  -u "elastic:itO5M3EZrbc96K_42ah3" \
  -H 'Content-Type: application/json' \
  -d '{
  "index.lifecycle.name": "ecommerce-logs-30d-retention"
}'
