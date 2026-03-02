# SellerProduct Multi-Index Integration Kılavuzu

## 📋 **Özet**

Yeni multi-index stratejisi ile:
- ❌ `product_index` (eski - nested yapılar, yavaş) → KULLANMA
- ✅ `sellerproduct_index` + `image_index` + `brand_index` + `category_index` → KULLAN

---

## 🎯 **Ne Değişti?**

### **ÖNCE (Eski Yapı):**
```csharp
// ProductElasticDto (product_index) - NESTED yapılar
public class ProductElasticDto
{
    public int Id { get; set; }
    public List<CategoryDto> Categories { get; set; } // ❌ Nested
    public List<ProductImageDto> Images { get; set; } // ❌ Nested
    public BrandDto Brand { get; set; } // ❌ Object
    // ...
}

// Kullanım
var result = await _productService.GetByFilterPagingAsync(filter);
// -> product_index'ten tek query, ama YAVAŞ indexing (2+ saat)
```

### **SONRA (Yeni Yapı):**
```csharp
// SellerProductElasticDto (sellerproduct_index) - SADECE ID'ler
public class SellerProductElasticDto
{
    public int ProductId { get; set; }
    public int BrandId { get; set; } // ✅ Sadece ID
    public int TaxId { get; set; }   // ✅ Sadece ID
    // Categories, Images YOK - ayrı index'lerde
}

// SellerProductViewModel (Joined - C# tarafında)
public class SellerProductViewModel
{
    public int ProductId { get; set; }
    public BrandDto Brand { get; set; }        // ✅ brand_index'ten JOIN
    public List<ProductImageDto> Images { get; set; } // ✅ image_index'ten JOIN
    // ...
}

// Kullanım
var result = await _sellerProductService.GetByFilterPagingAsync(filter);
// -> 3 parallel query (sellerproduct + brand + image), ama HIZLI indexing (~15 dakika)
```

---

## 🚀 **Kullanım**

### **1. Service Injection**

```csharp
@inject ISellerProductService SellerProductService
```

### **2. Ürün Listesi Çekme**

```csharp
// Basit liste (paging ile)
var result = await SellerProductService.GetAllAsync(page: 1, pageSize: 20);

if (result.Succeeded)
{
    var products = result.Result; // List<SellerProductViewModel>
    
    foreach (var product in products)
    {
        Console.WriteLine($"{product.ProductName} - {product.Stock} adet");
        Console.WriteLine($"Marka: {product.Brand?.Name}");
        Console.WriteLine($"İlk Resim: {product.Images?.FirstOrDefault()?.FileName}");
    }
}
```

### **3. Filtreleme**

```csharp
var filter = new SearchFilterReguestDto
{
    Page = 1,
    PageSize = 20,
    Search = "fren",
    BrandIds = new List<int> { 1, 2, 3 },
    Sort = ProductFilter.ByPriceAsc
};

var result = await SellerProductService.GetByFilterPagingAsync(filter);

if (result.Succeeded)
{
    var paging = result.Result; // Paging<List<SellerProductViewModel>>
    var products = paging.Data;
    var totalCount = paging.DataCount;
    
    Console.WriteLine($"Toplam {totalCount} ürün bulundu, {products.Count} gösteriliyor");
}
```

---

## 📝 **Razor Component Örneği**

```razor
@page "/urunler"
@inject ISellerProductService SellerProductService

<h3>Ürünler</h3>

@if (_loading)
{
    <GlobalLoadingComponent />
}
else if (_products?.Any() == true)
{
    <div class="products-grid">
        @foreach (var product in _products)
        {
            <div class="product-card">
                @* Image (image_index'ten geldi) *@
                @if (product.Images?.Any() == true)
                {
                    <img src="@product.Images.First().FileName" alt="@product.ProductName" />
                }
                
                <h4>@product.ProductName</h4>
                
                @* Brand (brand_index'ten geldi) *@
                <p class="brand">@product.Brand?.Name</p>
                
                @* Price *@
                <p class="price">@product.SalePrice @product.Currency</p>
                
                @* Stock *@
                <p class="stock">Stok: @product.Stock</p>
                
                <button @onclick="() => AddToCart(product)">Sepete Ekle</button>
            </div>
        }
    </div>
    
    <Pagination CurrentPage="@_currentPage" 
                TotalPages="@_totalPages" 
                OnPageChanged="LoadProducts" />
}

@code {
    private List<SellerProductViewModel> _products = new();
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
            var filter = new SearchFilterReguestDto
            {
                Page = page,
                PageSize = PageSize,
                Sort = ProductFilter.ByStockDesc
            };
            
            var result = await SellerProductService.GetByFilterPagingAsync(filter);
            
            if (result.Succeeded)
            {
                _products = result.Result.Data;
                _currentPage = page;
                _totalPages = (int)Math.Ceiling(result.Result.DataCount / (double)PageSize);
            }
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

## 🔄 **Migration Path (Eski → Yeni)**

### **Adım 1: Service Değiştir**

```csharp
// ÖNCE (ESKİ)
@inject IProductService ProductService

// SONRA (YENİ)
@inject ISellerProductService SellerProductService
```

### **Adım 2: DTO Değiştir**

```csharp
// ÖNCE (ESKİ)
List<ProductElasticDto> products;

// SONRA (YENİ)
List<SellerProductViewModel> products;
```

### **Adım 3: Property Adları Güncelle**

```csharp
// ÖNCE (ESKİ)
product.Name          // ProductElasticDto
product.Price
product.Status

// SONRA (YENİ)
product.ProductName   // SellerProductViewModel
product.SalePrice
product.ProductStatus
```

---

## 📊 **Performans Karşılaştırması**

| Metrik | ProductService (ESKİ) | SellerProductService (YENİ) |
|--------|----------------------|---------------------------|
| **Indexing Süresi** | ~2+ saat | ~15-20 dakika ⚡️ |
| **Query Süresi** | 50-100ms (nested query) | 15-30ms (parallel queries) ⚡️ |
| **Index Boyutu** | Büyük (nested data) | Küçük (sadece ID'ler) ✅ |
| **Esneklik** | Düşük (nested update zor) | Yüksek (independent indices) ✅ |

---

## ⚠️ **Önemli Notlar**

1. **ProductService hala kullanılabilir** (backward compatibility için), ama YENİ kod'da `SellerProductService` kullan
2. **Brand, Category, Image** artık ayrı index'lerde → Redis cache kullanabilirsin
3. **Stock > 0** filtresi otomatik uygulanıyor (SellerProductService)
4. **Nested query yok** → Daha hızlı, daha basit

---

## 🎯 **Özet**

✅ **YENİ kod:** `SellerProductService` + `SellerProductViewModel`  
❌ **ESKİ kod:** `ProductService` + `ProductElasticDto` (kullanma)

**İlk indexing bittiğinde:**
```bash
python3 product.py
# -> sellerproduct_index + image_index oluşturulur (~20-30 dakika)
```

**Sonra C# tarafında:**
```csharp
var result = await SellerProductService.GetByFilterPagingAsync(filter);
// -> Multi-index join, HIZLI! ⚡️
```

---

**Başarılar! 🚀**

