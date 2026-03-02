# SellerProduct Cache Strategy

## 📦 **Elasticsearch Index Yapısı**

```json
{
  "ProductId": 78187,
  "ProductName": "Alternator Gergi Rulmani...",
  "BrandId": 211541,
  "BrandName": "ABA",              // ✅ Denormalized (her zaman lazım)
  "TaxId": 5,
  "TaxName": "KDV %20",            // ✅ Denormalized (her zaman lazım)
  "TaxRate": 20.0,
  "CategoryIds": [1, 5, 10],       // ❗ Sadece ID'ler
  "ImageIds": [100, 101, 102],     // ❗ Sadece ID'ler
  "GroupCodeIds": [50, 51],        // ❗ Sadece ID'ler
  "Stock": 10,
  "SalePrice": 157.67
}
```

---

## 🚀 **C# Implementation (Blazor)**

### **1. Service Layer**

```csharp
public class SellerProductService
{
    private readonly IElasticClient _elasticClient;
    private readonly IDistributedCache _cache; // Redis
    private readonly ApplicationDbContext _dbContext;

    public async Task<List<SellerProductViewModel>> GetSellerProducts(
        int page = 1, 
        int pageSize = 20)
    {
        // 1. Elasticsearch'ten ürünleri çek (HIZLI - nested yok!)
        var esResponse = await _elasticClient.SearchAsync<SellerProductEs>(s => s
            .Index("sellerproduct_index")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.MatchAll())
        );

        var products = esResponse.Documents.ToList();
        
        // 2. Tüm CategoryIds'leri topla
        var allCategoryIds = products
            .SelectMany(p => p.CategoryIds ?? new int[0])
            .Distinct()
            .ToList();

        // 3. Redis'ten Categories çek (cache)
        var categories = await GetCategoriesFromCache(allCategoryIds);

        // 4. Tüm ImageIds'leri topla
        var allImageIds = products
            .SelectMany(p => p.ImageIds ?? new int[0])
            .Distinct()
            .ToList();

        // 5. Redis'ten Images çek (cache)
        var images = await GetImagesFromCache(allImageIds);

        // 6. Memory'de birleştir
        var viewModels = products.Select(p => new SellerProductViewModel
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            BrandName = p.BrandName,        // ✅ Zaten var
            TaxName = p.TaxName,            // ✅ Zaten var
            SalePrice = p.SalePrice,
            Stock = p.Stock,
            
            // Cache'den gelen
            Categories = categories
                .Where(c => p.CategoryIds?.Contains(c.Id) == true)
                .ToList(),
            Images = images
                .Where(i => p.ImageIds?.Contains(i.Id) == true)
                .ToList()
        }).ToList();

        return viewModels;
    }

    private async Task<List<Category>> GetCategoriesFromCache(List<int> categoryIds)
    {
        if (!categoryIds.Any()) return new List<Category>();

        var cacheKey = $"Categories:{string.Join(",", categoryIds.OrderBy(x => x))}";
        
        // Redis'ten çek
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<Category>>(cached);
        }

        // Cache'de yok, DB'den çek
        var categories = await _dbContext.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();

        // Redis'e kaydet (1 saat)
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(categories),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }
        );

        return categories;
    }

    private async Task<List<ProductImage>> GetImagesFromCache(List<int> imageIds)
    {
        if (!imageIds.Any()) return new List<ProductImage>();

        var cacheKey = $"Images:{string.Join(",", imageIds.OrderBy(x => x))}";
        
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<ProductImage>>(cached);
        }

        var images = await _dbContext.ProductImages
            .Where(i => imageIds.Contains(i.Id))
            .ToListAsync();

        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(images),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }
        );

        return images;
    }
}
```

---

### **2. Models**

```csharp
// Elasticsearch'ten gelen (basit)
public class SellerProductEs
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int BrandId { get; set; }
    public string BrandName { get; set; }  // Denormalized
    public int TaxId { get; set; }
    public string TaxName { get; set; }    // Denormalized
    public double TaxRate { get; set; }    // Denormalized
    public int Stock { get; set; }
    public double SalePrice { get; set; }
    
    public int[] CategoryIds { get; set; }  // Sadece ID'ler
    public int[] ImageIds { get; set; }     // Sadece ID'ler
    public int[] GroupCodeIds { get; set; } // Sadece ID'ler
}

// UI'ya giden (zengin)
public class SellerProductViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string BrandName { get; set; }
    public string TaxName { get; set; }
    public double SalePrice { get; set; }
    public int Stock { get; set; }
    
    public List<Category> Categories { get; set; }        // Cache'den
    public List<ProductImage> Images { get; set; }        // Cache'den
    public List<ProductGroupCode> GroupCodes { get; set; } // Cache'den
}
```

---

## 📊 **Performans Karşılaştırması**

### **Eski Yaklaşım (Nested)**
```
Indexleme: 2 saat
Query: 1 × Elasticsearch (nested query) = 50-100ms
TOPLAM: 50-100ms
```

### **Yeni Yaklaşım (Hybrid + Cache)**
```
Indexleme: 15-20 dakika
İlk Query: 
  - 1 × Elasticsearch (basit) = 10-20ms
  - 1 × Redis (categories) = 1-2ms
  - 1 × Redis (images) = 1-2ms
  TOPLAM: ~15-25ms

2. Query (cache'de): 
  - 1 × Elasticsearch = 10-20ms
  - Redis hit (categories) = 1ms
  - Redis hit (images) = 1ms
  TOPLAM: ~12ms ⚡️
```

---

## 🎯 **Avantajlar**

1. ✅ **Indexleme 8x daha hızlı** (2 saat → 15 dakika)
2. ✅ **Query daha hızlı** (nested query yok)
3. ✅ **Redis cache kullanımı** (zaten var)
4. ✅ **Esnek** (sadece gerekli data çekilir)
5. ✅ **Scalable** (Categories değişirse sadece cache temizle)

---

## 🔧 **Redis Configuration**

**appsettings.json:**
```json
{
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "MarketPlace:"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});
```

---

## 📝 **Kullanım (Blazor Page)**

```razor
@page "/products"
@inject SellerProductService ProductService

<h3>Ürünler</h3>

@if (_products == null)
{
    <p>Yükleniyor...</p>
}
else
{
    @foreach (var product in _products)
    {
        <div class="product-card">
            <h4>@product.ProductName</h4>
            <p>Marka: @product.BrandName</p>
            <p>Fiyat: @product.SalePrice @product.Currency</p>
            <p>Stok: @product.Stock</p>
            
            @* Categories (cache'den) *@
            <div>
                Kategoriler: 
                @string.Join(", ", product.Categories.Select(c => c.Name))
            </div>
            
            @* Images (cache'den) *@
            @if (product.Images?.Any() == true)
            {
                <img src="@product.Images.First().FileName" alt="@product.ProductName" />
            }
        </div>
    }
}

@code {
    private List<SellerProductViewModel> _products;

    protected override async Task OnInitializedAsync()
    {
        _products = await ProductService.GetSellerProducts(page: 1, pageSize: 20);
    }
}
```

---

## 🚀 **SON ADIMLAR**

1. ✅ Python script'i çalıştır (15-20 dakika):
   ```bash
   python3 sellerproduct_optimized_v2.py --skip-refresh
   ```

2. ✅ C# Service'i implement et (yukarıdaki kod)

3. ✅ Redis cache kullan (zaten var)

4. ✅ Test et ve rahatla! 🎉

