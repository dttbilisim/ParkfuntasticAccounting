# 🎯 Multi-Index Strategy (Elasticsearch Native)

## 📋 **Index'ler**

```
Elasticsearch:
├─ sellerproduct_index → SellerItems + Product (BrandId, TaxId, ProductId)
├─ brand_index → Brand detayları (zaten var ✅)
├─ category_index → Category detayları (zaten var ✅)
└─ image_index → Image detayları (yeni oluştur)
```

---

## 🚀 **Kurulum**

### **1. image_index Oluştur** (~30 saniye)

```bash
python3 index_images.py
```

### **2. sellerproduct_index Oluştur** (~10-15 dakika)

```bash
python3 sellerproduct_multiindex.py --skip-refresh
```

---

## 💻 **C# Kullanımı (Application-Side Join)**

### **Service**

```csharp
public class SellerProductService
{
    private readonly IElasticClient _elasticClient;

    public async Task<List<SellerProductViewModel>> GetSellerProducts(
        int page = 1, 
        int pageSize = 20)
    {
        // 1. Ana ürünleri çek (sellerproduct_index)
        var productsResponse = await _elasticClient.SearchAsync<SellerProductEs>(s => s
            .Index("sellerproduct_index")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Range(r => r.Field(f => f.Stock).GreaterThan(0)))
            .Sort(ss => ss.Descending(p => p.SellerModifiedDate))
        );

        var products = productsResponse.Documents.ToList();
        
        if (!products.Any()) return new List<SellerProductViewModel>();

        // 2. Brand ID'leri topla
        var brandIds = products.Select(p => p.BrandId).Distinct().ToList();
        
        // 3. Product ID'leri topla
        var productIds = products.Select(p => p.ProductId).Distinct().ToList();
        
        // 4. Parallel queries (HIZLI!)
        var brandsTask = GetBrandsByIds(brandIds);
        var categoriesTask = GetCategoriesByProductIds(productIds);
        var imagesTask = GetImagesByProductIds(productIds);
        
        await Task.WhenAll(brandsTask, categoriesTask, imagesTask);
        
        var brands = brandsTask.Result;
        var categoriesDict = categoriesTask.Result;
        var imagesDict = imagesTask.Result;
        
        // 5. Memory'de birleştir
        var viewModels = products.Select(p => new SellerProductViewModel
        {
            SellerItemId = p.SellerItemId,
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Stock = p.Stock,
            SalePrice = p.SalePrice,
            Currency = p.Currency,
            
            // JOIN'ler (memory'de)
            Brand = brands.FirstOrDefault(b => b.Id == p.BrandId),
            Categories = categoriesDict.GetValueOrDefault(p.ProductId, new List<Category>()),
            Images = imagesDict.GetValueOrDefault(p.ProductId, new List<ProductImage>())
        }).ToList();

        return viewModels;
    }

    private async Task<List<Brand>> GetBrandsByIds(List<int> brandIds)
    {
        if (!brandIds.Any()) return new List<Brand>();
        
        var response = await _elasticClient.SearchAsync<Brand>(s => s
            .Index("brand_index")
            .Size(brandIds.Count)
            .Query(q => q.Terms(t => t.Field(f => f.Id).Terms(brandIds)))
        );
        
        return response.Documents.ToList();
    }

    private async Task<Dictionary<int, List<Category>>> GetCategoriesByProductIds(List<int> productIds)
    {
        if (!productIds.Any()) return new Dictionary<int, List<Category>>();
        
        // category_index'te ProductCategories ilişkisi olmalı
        // Eğer category_index'te ProductId yoksa, PostgreSQL'den çek
        
        // Option 1: ProductCategories mapping var (category_index'te)
        var response = await _elasticClient.SearchAsync<ProductCategoryMapping>(s => s
            .Index("category_index")  // veya "productcategories_index" 
            .Size(productIds.Count * 5)  // Max 5 kategori/ürün
            .Query(q => q.Terms(t => t.Field(f => f.ProductId).Terms(productIds)))
        );
        
        return response.Documents
            .GroupBy(pc => pc.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pc => new Category 
                { 
                    Id = pc.CategoryId, 
                    Name = pc.CategoryName 
                }).ToList()
            );
        
        // Option 2: category_index'te ProductId yok → DB'den çek
        // using var context = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // var mappings = await context.ProductCategories
        //     .Where(pc => productIds.Contains(pc.ProductId))
        //     .Include(pc => pc.Category)
        //     .ToListAsync();
        // 
        // return mappings.GroupBy(pc => pc.ProductId)
        //     .ToDictionary(g => g.Key, g => g.Select(pc => pc.Category).ToList());
    }

    private async Task<Dictionary<int, List<ProductImage>>> GetImagesByProductIds(List<int> productIds)
    {
        if (!productIds.Any()) return new Dictionary<int, List<ProductImage>>();
        
        var response = await _elasticClient.SearchAsync<ProductImage>(s => s
            .Index("image_index")
            .Size(productIds.Count * 3)  // Max 3 image/ürün
            .Query(q => q.Terms(t => t.Field(f => f.ProductId).Terms(productIds)))
        );
        
        return response.Documents
            .GroupBy(img => img.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
    }
}
```

---

### **Models**

```csharp
// Elasticsearch'ten gelen (sellerproduct_index)
public class SellerProductEs
{
    public int SellerItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductDescription { get; set; }
    public int Stock { get; set; }
    public double SalePrice { get; set; }
    public string Currency { get; set; }
    public DateTime? SellerModifiedDate { get; set; }
    
    // JOIN key'leri (sadece ID'ler)
    public int BrandId { get; set; }
    public int TaxId { get; set; }
}

// Elasticsearch'ten gelen (brand_index)
public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Elasticsearch'ten gelen (image_index)
public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }  // JOIN key
    public string FileName { get; set; }
    public string FileGuid { get; set; }
}

// Elasticsearch'ten gelen (category_index veya mapping)
public class ProductCategoryMapping
{
    public int ProductId { get; set; }  // JOIN key
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// UI'ya giden (zengin)
public class SellerProductViewModel
{
    public int SellerItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Stock { get; set; }
    public double SalePrice { get; set; }
    public string Currency { get; set; }
    
    // Joined data
    public Brand Brand { get; set; }
    public List<Category> Categories { get; set; }
    public List<ProductImage> Images { get; set; }
}
```

---

## 📊 **Performans**

### **Indexing**

| Index | Kayıt Sayısı | Süre |
|-------|--------------|------|
| **sellerproduct_index** | ~250K | ~10-15 dakika |
| **image_index** | ~100K | ~30 saniye |
| **brand_index** | ~10K | Zaten var ✅ |
| **category_index** | ~500 | Zaten var ✅ |

### **Runtime (20 ürün için)**

| Query | Süre |
|-------|------|
| sellerproduct_index | ~10-15ms |
| brand_index (parallel) | ~2-5ms |
| category_index (parallel) | ~2-5ms |
| image_index (parallel) | ~2-5ms |
| **TOPLAM** | **~15-30ms** ⚡️ |

---

## 🎯 **Avantajlar**

1. ✅ **Hızlı indexing** (nested yok, GROUP BY minimal)
2. ✅ **Parallel queries** (3-4 index aynı anda)
3. ✅ **Elasticsearch native** (DB'ye gitme!)
4. ✅ **Esnek** (index'ler bağımsız güncellenir)
5. ✅ **Scalable** (brand_index, category_index cache'lenebilir)

---

## 💡 **Pro Tips**

### **Option 1: ProductCategories Mapping Index'i Oluştur**

```python
# productcategories_mapping_index.py
# ProductCategories tablosunu index'le
SELECT 
    pc."ProductId",
    pc."CategoryId",
    c."Name" AS "CategoryName"
FROM "ProductCategories" pc
JOIN "Category" c ON c."Id" = pc."CategoryId"
```

Bu şekilde C# tarafında DB'ye hiç gitme!

### **Option 2: Brand/Category Cache (küçük tablolar)**

```csharp
// Startup'ta MemoryCache'e at
public void ConfigureBrandCache()
{
    var brands = await _elasticClient.SearchAsync<Brand>(s => s
        .Index("brand_index")
        .Size(10000)
        .MatchAll()
    );
    
    _memoryCache.Set("AllBrands", brands.Documents.ToList(), TimeSpan.FromHours(24));
}

// Kullanım
var brands = _memoryCache.Get<List<Brand>>("AllBrands");
var productBrand = brands.FirstOrDefault(b => b.Id == product.BrandId);
```

---

## 🚀 **Şimdi Başla**

```bash
# 1. Images index'le
python3 index_images.py

# 2. SellerProducts index'le
python3 sellerproduct_multiindex.py --skip-refresh

# 3. C# Service'i implement et (yukarıdaki kod)

# 4. Rahat et! 🎉
```

**TOPLAM SÜRE: ~15-20 dakika**

