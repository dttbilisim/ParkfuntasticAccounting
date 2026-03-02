# 🏆 Senior Strategy: Materialized View + Full Denormalization

## 📋 **Strateji Özeti**

```
PostgreSQL (Gece - Cron Job):
├─ mv_dotparts_joined REFRESH → 2 dakika
└─ mv_sellerproduct_full REFRESH → 5-10 dakika (TÜM hesaplama burada!)

Python (Gece veya Saatlik):
├─ mv_sellerproduct_full'den SELECT → GROUP BY YOK! ⚡️
└─ Elasticsearch'e index → 10-15 dakika (10K batch!)

C# Blazor (Runtime):
└─ Tek Elasticsearch query → HER ŞEY hazır! (Categories, Images, ALL!)
```

---

## 🚀 **Kurulum**

### **1. Materialized View Oluştur** (Tek Seferlik)

```bash
psql -h localhost -p 5454 -U myinsurer -d MarketPlace -f create_mv_sellerproduct.sql
```

**Süre:** ~2-5 dakika (ilk oluşturma)

---

### **2. İlk Indexleme** (Tek Seferlik)

```bash
python3 sellerproduct_senior.py
```

**Beklenen:**
- ✅ mv_dotparts_joined refresh: ~2 dakika
- ✅ mv_sellerproduct_full refresh: ~5-10 dakika
- ✅ Elasticsearch indexing: ~10-15 dakika
- **TOPLAM: ~20-30 dakika**

**Her batch:**
- Batch size: 10,000 kayıt
- Süre: **~5-10 saniye** (sadece SELECT!)
- Query: `SELECT * FROM mv_sellerproduct_full` (GROUP BY YOK!)

---

### **3. Cron Job Kur** (Otomatik Güncelleme)

```bash
# Her gece saat 2'de materialized view refresh
0 2 * * * psql -h localhost -p 5454 -U myinsurer -d MarketPlace -c 'REFRESH MATERIALIZED VIEW "mv_dotparts_joined";' >> /var/log/mv_refresh.log 2>&1

# Her gece saat 2:10'da sellerproduct view refresh
10 2 * * * psql -h localhost -p 5454 -U myinsurer -d MarketPlace -c 'REFRESH MATERIALIZED VIEW "mv_sellerproduct_full";' >> /var/log/mv_sellerproduct.log 2>&1

# Her gece saat 3'te Elasticsearch indexing
0 3 * * * cd /root/transfer && python3 sellerproduct_senior.py --skip-refresh >> /var/log/sellerproduct_index.log 2>&1
```

---

## 💻 **C# Kullanımı (ULTRA BASİT!)**

### **Model**

```csharp
public class SellerProductEs
{
    public int SellerItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductDescription { get; set; }
    public int Stock { get; set; }
    public double SalePrice { get; set; }
    public string Currency { get; set; }
    
    // Denormalized objects (direkt Elasticsearch'ten gelir!)
    public Brand Brand { get; set; }
    public Tax Tax { get; set; }
    
    // Denormalized nested arrays (direkt Elasticsearch'ten gelir!)
    public List<Category> Categories { get; set; }
    public List<ProductImage> Images { get; set; }
    public List<GroupCode> GroupCodes { get; set; }
    
    // DotParts (null olabilir)
    public string PartNumber { get; set; }
    public string DotPartName { get; set; }
    public string ManufacturerName { get; set; }
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Tax
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ProductImage
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FileGuid { get; set; }
}

public class GroupCode
{
    public int Id { get; set; }
    public string GroupCode { get; set; }
}
```

---

### **Service**

```csharp
public class SellerProductService
{
    private readonly IElasticClient _elasticClient;

    public async Task<(List<SellerProductEs> Products, long Total)> GetSellerProducts(
        int page = 1, 
        int pageSize = 20,
        string search = null,
        int? categoryId = null)
    {
        var searchResponse = await _elasticClient.SearchAsync<SellerProductEs>(s => s
            .Index("sellerproduct_index")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => 
            {
                var queries = new List<Func<QueryContainerDescriptor<SellerProductEs>, QueryContainer>>();
                
                // Stock > 0 (her zaman)
                queries.Add(qq => qq.Range(r => r.Field(f => f.Stock).GreaterThan(0)));
                
                // Search (opsiyonel)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    queries.Add(qq => qq.MultiMatch(m => m
                        .Query(search)
                        .Fields(f => f
                            .Field(ff => ff.ProductName)
                            .Field(ff => ff.ProductDescription)
                            .Field(ff => ff.Brand.Name)
                        )
                    ));
                }
                
                // Category filter (nested query!)
                if (categoryId.HasValue)
                {
                    queries.Add(qq => qq.Nested(n => n
                        .Path(p => p.Categories)
                        .Query(nq => nq.Term(t => t
                            .Field("Categories.Id")
                            .Value(categoryId.Value)
                        ))
                    ));
                }
                
                return q.Bool(b => b.Must(queries.ToArray()));
            })
            .Sort(ss => ss.Descending(p => p.SellerModifiedDate))
        );

        return (searchResponse.Documents.ToList(), searchResponse.Total);
    }
}
```

---

### **Blazor Page (Razor)**

```razor
@page "/products"
@inject SellerProductService ProductService

<h3>Ürünler</h3>

@if (_loading)
{
    <GlobalLoadingComponent />
}
else
{
    <div class="products-grid">
        @foreach (var product in _products)
        {
            <div class="product-card">
                @* Image (direkt Elasticsearch'ten!) *@
                @if (product.Images?.Any() == true)
                {
                    <img src="@product.Images.First().FileName" alt="@product.ProductName" />
                }
                
                <h4>@product.ProductName</h4>
                
                @* Brand (direkt Elasticsearch'ten!) *@
                <p class="brand">@product.Brand?.Name</p>
                
                @* Price with Tax (direkt Elasticsearch'ten!) *@
                <p class="price">
                    @product.SalePrice @product.Currency
                    @if (product.Tax != null)
                    {
                        <span class="tax">(@product.Tax.Name)</span>
                    }
                </p>
                
                @* Stock *@
                <p class="stock">Stok: @product.Stock</p>
                
                @* Categories (direkt Elasticsearch'ten!) *@
                @if (product.Categories?.Any() == true)
                {
                    <div class="categories">
                        @foreach (var cat in product.Categories)
                        {
                            <span class="badge">@cat.Name</span>
                        }
                    </div>
                }
                
                <button @onclick="() => AddToCart(product)">Sepete Ekle</button>
            </div>
        }
    </div>
    
    <Pagination CurrentPage="@_currentPage" 
                TotalPages="@_totalPages" 
                OnPageChanged="LoadProducts" />
}

@code {
    private List<SellerProductEs> _products = new();
    private int _currentPage = 1;
    private int _totalPages = 1;
    private bool _loading = true;
    private const int PageSize = 20;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts(1);
    }

    private async Task LoadProducts(int page)
    {
        _loading = true;
        StateHasChanged();
        
        try
        {
            var (products, total) = await ProductService.GetSellerProducts(
                page: page, 
                pageSize: PageSize
            );
            
            _products = products;
            _currentPage = page;
            _totalPages = (int)Math.Ceiling(total / (double)PageSize);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
}
```

---

## 📊 **Performans Metrikleri**

### **Indexleme**

| Metrik | Değer |
|--------|-------|
| **Materialized view refresh** | ~10 dakika (gece yapılır) |
| **Python indexing** | ~10-15 dakika |
| **Batch size** | 10,000 kayıt |
| **Batch süresi** | ~5-10 saniye |
| **TOPLAM** | ~20-30 dakika (gece yapılır!) |

### **Runtime Query**

| Metrik | Değer |
|--------|-------|
| **Elasticsearch query** | 10-30ms |
| **DB query** | **0ms** (HİÇ YOK!) |
| **TOPLAM** | **10-30ms** ⚡️ |

---

## 🎯 **Avantajlar**

1. ✅ **ULTRA FAST Indexing** (10K batch, sadece SELECT)
2. ✅ **ULTRA FAST Query** (tek ES query, DB'ye gitme!)
3. ✅ **FULL Denormalized** (Categories, Images, ALL!)
4. ✅ **Materialized View** (ağır hesaplama gece yapılır)
5. ✅ **Nested Query Support** (kategori bazlı filtering)
6. ✅ **NO Redis Dependency** (optional - cache olarak kullanılabilir)
7. ✅ **Scalable** (view refresh paralel yapılabilir)

---

## 🔧 **Bakım**

### **Materialized View Güncelleme**

```bash
# Manuel refresh (gerekirse)
psql -h localhost -p 5454 -U myinsurer -d MarketPlace <<EOF
REFRESH MATERIALIZED VIEW "mv_dotparts_joined";
REFRESH MATERIALIZED VIEW "mv_sellerproduct_full";
EOF
```

### **İndex Yeniden Oluşturma**

```bash
# Full rebuild (her şey sıfırdan)
python3 sellerproduct_senior.py
```

### **İndex Boyutu Kontrol**

```bash
curl -X GET "http://localhost:9200/_cat/indices/sellerproduct_index?v&h=index,docs.count,store.size"
```

---

## 💡 **Senior Pro Tips**

1. **Materialized view refresh'i paralel yap**
   ```sql
   -- mv_dotparts_joined ve mv_sellerproduct_full paralel refresh edilebilir
   -- (farklı tablolara bağlılar)
   ```

2. **CONCURRENT refresh için unique index**
   ```sql
   CREATE UNIQUE INDEX idx_mv_sellerproduct_unique 
       ON mv_sellerproduct_full("SellerItemId");
   
   REFRESH MATERIALIZED VIEW CONCURRENTLY mv_sellerproduct_full;
   ```

3. **Partial indexing (sadece değişenler)**
   ```python
   # ModifiedDate > LAST_RUN tarihinden sonrakiler
   WHERE "SellerModifiedDate" > '2025-11-03'
   ```

4. **Multiple shards (büyük data için)**
   ```json
   {
     "settings": {
       "number_of_shards": 5,  // Arttır
       "number_of_replicas": 2  // HA için
     }
   }
   ```

---

## 🏆 **SONUÇ**

**Senior yaklaşımı:**
- ❌ DB'ye query at, cache'le, merge et (junior)
- ✅ Materialized view + Full denormalized (senior!)

**Sen haklıydın dostum!** 🎉

