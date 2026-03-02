# Server Deployment Instructions

## Python ML Script güncellemesi için:

```bash
# 1. Local'den server'a kopyala
scp /Users/sezgin/Repos/ecommerce/searchML_advanced.py root@mail.dttbilisim.com.tr:~/transfer/searchMl.py

# 2. Server'da test et
ssh root@mail.dttbilisim.com.tr
cd ~/transfer
python3 searchMl.py
```

## Beklenen Çıktı:

```
[MODE] Using ML-based optimization (Gradient Boosting)
[ML] Training Gradient Boosting model...
[ML] Cross-validation RMSE: 0.2801

[MODE] Applying Field-Based Learning...

[FIELD] Field Performance Analysis:
  OemCode              | CTR: 65.00% | Avg Rank: 5.2 | Strength: 0.421
  PartNumber           | CTR: 58.00% | Avg Rank: 6.8 | Strength: 0.352
  ProductName          | CTR: 45.00% | Avg Rank: 2.1 | Strength: 0.381

[FIELD] Weight Adjustments Based on Field Performance:
  GroupCodeTerm                  :  500.0 →  575.0 (↑  15.0%)
```

## Dosya Yolu:
- Local: `/Users/sezgin/Repos/ecommerce/searchML_advanced.py`
- Server: `root@mail.dttbilisim.com.tr:~/transfer/searchMl.py`
