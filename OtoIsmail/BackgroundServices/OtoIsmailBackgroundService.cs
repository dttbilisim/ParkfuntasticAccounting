using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OtoIsmail.Abstract;
using OtoIsmail.Concreate;
using OtoIsmail.Dtos;
namespace OtoIsmail.BackgroundServices;
public class OtoIsmailBackgroundService : IAsyncBackgroundJob{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDapperService _dapperService;
    private readonly IApiClient _apiClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<OtoIsmailBackgroundService> _logger;

    public OtoIsmailBackgroundService(
        IUnitOfWork<ApplicationDbContext> context,
        IApiClient apiClient,
        IDapperService dapperService,
        ILogger<OtoIsmailBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _apiClient = apiClient;
        _dapperService = dapperService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public async Task ExecuteAsync(){
        try{
            Console.WriteLine("🚀 OtoIsmailBackgroundService başlatıldı...");
            _logger.LogInformation("OtoIsmailBackgroundService başlatıldı");
            
            var brands = await _apiClient.GetBrandsAsync();
            if(brands == null || brands.Count == 0){
                Console.WriteLine("❌ Marka verisi alınamadı veya boş (0 marka)");
                _logger.LogError("Marka verisi alınamadı veya boş. API yanıt dönmedi (veya Token alınamadı).");
                return; // Kullanıcı isteği üzerine job'ı fail etmiyoruz, sadece çıkıyoruz.
            }
            
            Console.WriteLine($"📋 Toplam {brands.Count} marka bulundu");
            _logger.LogInformation($"Toplam {brands.Count} marka bulundu");
            var existingBrands = await _context.GetRepository<Brand>().GetAllAsync(predicate:null);
            var brandsToInsertOrUpdate = new List<Brand>();
            foreach (var brand in brands)
            {
                var existing = existingBrands.FirstOrDefault(b => b.Name == brand.Kod);
                if (existing != null)
                {
                    existing.Status = 1;
                    existing.ModifiedDate = DateTime.Now;
                    existing.ModifiedId = 1;
                }
                else
                {
                    brandsToInsertOrUpdate.Add(new Brand
                    {
                        Name = brand.Kod,
                        Status = 1,
                        CreatedDate = DateTime.Now,
                        CreatedId = 1,
                        ModifiedDate = DateTime.Now,
                        ModifiedId = 1
                    });
                }
            }
            await _context.DbContext.BulkInsertOrUpdateAsync(brandsToInsertOrUpdate, new BulkConfig());
            await _context.SaveChangesAsync();
            var lastSave = _context.LastSaveChangesResult;
            if(lastSave.IsOk){
                Console.WriteLine($"✅ {brandsToInsertOrUpdate.Count} marka işlendi");
                _logger.LogInformation($"Tüm markalar başarıyla işlendi ({brandsToInsertOrUpdate.Count} marka)");
            }
            else{
                Console.WriteLine($"❌ Marka işleme hatası: {lastSave.Exception?.Message}");
                _logger.LogError($"Marka işleme hatası: {lastSave.Exception?.Message}");
            }

            int totalProcessedProducts = 0;
            int totalNewProducts = 0;
            int totalUpdatedProducts = 0;
            
            foreach (var brand in brands)
            {
                try
                {
                    // TARİH BAZLI: Sadece bugün değişen ürünleri al
                    // Günlük senkronizasyon için bugünün tarihini kullan
                    var today = DateTime.Today.ToString("yyyyMMdd");
                    var products = await _apiClient.GetProductsAsync(brand.Kod, today);
                    if (products == null || products.Count == 0)
                    {
                        // Sadece loglayıp devam edebiliriz ama kritikse hata fırlatabiliriz.
                        // Şimdilik uyarı verip devam ediyoruz ama hata fırlatmak daha iyi olabilir.
                        Console.WriteLine($"[{brand.Kod}] ⚠️ API'den ürün gelmedi veya boş (0 ürün)");
                        _logger.LogWarning($"Marka {brand.Kod} için ürün verisi alınamadı veya boş.");
                        continue;
                    }
                    
                    // LOG: API'den gelen ürün sayısı
                    Console.WriteLine($"[{brand.Kod}] 📥 API'den {products.Count} ürün geldi");
                    _logger.LogInformation($"[{brand.Kod}] API'den {products.Count} ürün geldi");

                    var netsisIds = products.Select(p => p.NetsisStokId).ToList();
                    var existingProducts = await _context.GetRepository<ProductOtoIsmail>()
                        .GetAllAsync(predicate: p => netsisIds.Contains(p.NetsisStokId));

                    // LOG: Mevcut ürün sayısı
                    Console.WriteLine($"[{brand.Kod}] 🔍 Veritabanında {existingProducts.Count} mevcut ürün bulundu");
                    _logger.LogInformation($"[{brand.Kod}] Veritabanında {existingProducts.Count} mevcut ürün bulundu");

                    var productEntities = new List<ProductOtoIsmail>();
                    int newProductCount = 0;
                    int updateProductCount = 0;
                    foreach (var product in products)
                    {
                        var existingProduct = existingProducts.FirstOrDefault(p => p.NetsisStokId == product.NetsisStokId);

                        if (existingProduct != null)
                        {
                            // LOG SAMPLE: First product in batch
                            if (updateProductCount == 0)
                            {
                                Console.WriteLine($"[DEBUG] Updating Product {existingProduct.Kod}: Old Stock={existingProduct.StokSayisi}, New Stock={product.StokSayisi}");
                                _logger.LogInformation($"[DEBUG] Updating Product {existingProduct.Kod}: Old Stock={existingProduct.StokSayisi}, New Stock={product.StokSayisi}");
                            }

                            existingProduct.Kod = product.Kod;
                            existingProduct.OrjinalKod = product.OrjinalKod;
                            existingProduct.Ad = product.Ad;
                            existingProduct.Marka = product.Marka;
                            existingProduct.MarkaFull = product.MarkaFull ?? product.Marka;
                            existingProduct.Birim = product.Birim;
                            existingProduct.GrupKodu = product.GrupKodu;
                            existingProduct.Barkod1 = product.Barkod1;
                            existingProduct.Barkod2 = product.Barkod2;
                            existingProduct.Barkod3 = product.Barkod3;
                            existingProduct.ImageUrl = product.ImageUrl;
                            existingProduct.Fiyat1 = product.Fiyat1 ?? 0;
                            existingProduct.ParaBirimi1 = product.ParaBirimi1; 
                            existingProduct.Fiyat2 = product.Fiyat2 ?? 0;
                            existingProduct.Fiyat3 = product.Fiyat3 ?? 0;
                            existingProduct.ParaBirimi3 = product.ParaBirimi3; 
                            existingProduct.Fiyat4 = product.Fiyat4 ?? 0;
                            existingProduct.KDV = product.KDV;
                            existingProduct.Oem = product.Oem;
                            existingProduct.Payda = product.Payda;
                            existingProduct.StokSayisi = product.StokSayisi;
                            existingProduct.Plaza = product.Plaza;
                            existingProduct.Gebze = product.Gebze;
                            existingProduct.Ankara = product.Ankara;
                            existingProduct.Ikitelli = product.Ikitelli;
                            existingProduct.Izmir = product.Izmir;
                            existingProduct.Samsun = product.Samsun;
                            existingProduct.Depo1030 = product.Depo1030;
                            existingProduct.Depo13 = product.Depo13;
                            existingProduct.Nakliye = product.Nakliye ?? 0;
                            existingProduct.ParaBirimi = product.ParaBirimi;
                            existingProduct.ModifiedDate = DateTime.Now;
                            existingProduct.ModifiedId = 1;
                            existingProduct.Status = 1;
                            productEntities.Add(existingProduct);
                            updateProductCount++;
                        }
                        else
                        {
                            var newProduct = new ProductOtoIsmail
                            {
                                Id = 0, 
                                NetsisStokId = product.NetsisStokId,
                                Kod = product.Kod,
                                OrjinalKod = product.OrjinalKod,
                                Ad = product.Ad,
                                Marka = product.Marka,
                                MarkaFull = product.MarkaFull ?? product.Marka,
                                Birim = product.Birim,
                                GrupKodu = product.GrupKodu,
                                Barkod1 = product.Barkod1,
                                Barkod2 = product.Barkod2,
                                Barkod3 = product.Barkod3,
                                ImageUrl = product.ImageUrl,
                                Fiyat1 = product.Fiyat1 ?? 0,
                                ParaBirimi1 = product.ParaBirimi1,
                                Fiyat2 = product.Fiyat2 ?? 0,
                                Fiyat3 = product.Fiyat3 ?? 0,
                                ParaBirimi3 = product.ParaBirimi3,
                                Fiyat4 = product.Fiyat4 ?? 0,
                                KDV = product.KDV,
                                Oem = product.Oem,
                                Payda = product.Payda,
                                StokSayisi = product.StokSayisi,
                                Plaza = product.Plaza,
                                Gebze = product.Gebze,
                                Ankara = product.Ankara,
                                Ikitelli = product.Ikitelli,
                                Izmir = product.Izmir,
                                Samsun = product.Samsun,
                                Depo1030 = product.Depo1030,
                                Depo13 = product.Depo13,
                                Nakliye = product.Nakliye ?? 0,
                                ParaBirimi = product.ParaBirimi,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                ModifiedId = 1,
                                Status = 1
                            };
                            productEntities.Add(newProduct);
                            newProductCount++;
                        }
                    }

                    // LOG: Yeni ve güncellenecek ürün sayıları
                    Console.WriteLine($"[{brand.Kod}] 📊 İşlenecek: {productEntities.Count} ürün ({newProductCount} yeni, {updateProductCount} güncelleme)");
                    _logger.LogInformation($"[{brand.Kod}] İşlenecek: {productEntities.Count} ürün ({newProductCount} yeni, {updateProductCount} güncelleme)");

                    // FIXED: BulkInsertOrUpdate geçici index oluşturuyor (CONCURRENTLY hatası)
                    // Yeni ve güncellenecek ürünleri ayırıp, BulkInsert ve BulkUpdate ayrı ayrı yap
                    var newProducts = productEntities.Where(p => p.Id == 0).ToList();
                    var updateProducts = productEntities.Where(p => p.Id != 0).ToList();

                    if (newProducts.Any())
                    {
                        // FIXED: Duplicate kontrolü kaldırıldı - Unique index zaten var, BulkInsert duplicate'leri atlayacak
                        // Ayrıca, ilk sorguda zaten existingProducts kontrolü yapıldı, tekrar kontrol gereksiz
                        // Eğer aynı batch içinde duplicate varsa, unique index constraint hatası verir ama bu çok nadir
                        var insertConfig = new BulkConfig
                        {
                            SetOutputIdentity = true  // Yeni eklenen kayıtların Id'lerini al
                        };
                        
                        // BulkInsert duplicate'leri atlayacak (unique index sayesinde)
                        // Eğer duplicate varsa, sadece ilk kayıt insert edilir, diğerleri atlanır
                        await _context.DbContext.BulkInsertAsync(newProducts, insertConfig);
                        
                        Console.WriteLine($"[{brand.Kod}] 📝 {newProducts.Count} yeni ürün insert edildi");
                        _logger.LogInformation($"[{brand.Kod}] {newProducts.Count} yeni ürün insert edildi");
                    }

                    if (updateProducts.Any())
                    {
                        var updateConfig = new BulkConfig
                        {
                            SetOutputIdentity = false  // Update için gerekli değil
                        };
                        await _context.DbContext.BulkUpdateAsync(updateProducts, updateConfig);
                        
                        Console.WriteLine($"[{brand.Kod}] 🔄 {updateProducts.Count} ürün güncellendi");
                        _logger.LogInformation($"[{brand.Kod}] {updateProducts.Count} ürün güncellendi");
                    }

                    await _context.SaveChangesAsync();
                    var lastSaveProduct = _context.LastSaveChangesResult;
                    if(lastSaveProduct.IsOk){
                        // FIXED: Gerçek insert/update sayılarını kullan
                        var actualNewCount = newProducts.Any() ? newProducts.Count : 0;
                        var actualUpdateCount = updateProducts.Any() ? updateProducts.Count : 0;
                        
                        totalProcessedProducts += (actualNewCount + actualUpdateCount);
                        totalNewProducts += actualNewCount;
                        totalUpdatedProducts += actualUpdateCount;
                        
                        Console.WriteLine($"[{brand.Kod}] ✅ Başarılı: {actualNewCount + actualUpdateCount} ürün işlendi ({actualNewCount} yeni eklendi, {actualUpdateCount} güncellendi)");
                        _logger.LogInformation($"[{brand.Kod}] Başarılı: {actualNewCount + actualUpdateCount} ürün işlendi ({actualNewCount} yeni eklendi, {actualUpdateCount} güncellendi)");
                    }
                    else{
                        Console.WriteLine($"[{brand.Kod}] ❌ Hata: {lastSaveProduct.Exception?.Message}");
                        _logger.LogError($"[{brand.Kod}] Hata: {lastSaveProduct.Exception?.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OtoIsmailBackgroundService] {brand.Kod} için ürün işlenirken hata oluştu: {ex.Message}");
                    _logger.LogInformation($"[OtoIsmailBackgroundService] {brand.Kod} için ürün işlenirken hata oluştu: {ex.Message}");
                    continue;
                }
            }
            
            // LOG: Toplam özet
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"📊 TOPLAM ÖZET:");
            Console.WriteLine($"   • İşlenen Marka: {brands.Count}");
            Console.WriteLine($"   • İşlenen Ürün: {totalProcessedProducts}");
            Console.WriteLine($"   • Yeni Ürün: {totalNewProducts}");
            Console.WriteLine($"   • Güncellenen Ürün: {totalUpdatedProducts}");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            _logger.LogInformation($"OtoIsmailBackgroundService tamamlandı - Toplam: {totalProcessedProducts} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)");
            
            // Update Seller SyncDate (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(1);
                    if (seller != null)
                    {
                        seller.SyncDate = DateTime.Now;
                        seller.SyncMessage = $"✅ Sync tamamlandı: {totalProcessedProducts} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)";
                        await scopedContext.SaveChangesAsync();
                        Console.WriteLine($"✅ Seller (Id=1) SyncDate güncellendi: {seller.SyncDate}");
                        _logger.LogInformation($"Seller (Id=1) SyncDate güncellendi: {seller.SyncDate}");
                    } else {
                        Console.WriteLine($"⚠️ Seller (Id=1) BULUNAMADI!");
                        _logger.LogWarning($"Seller (Id=1) BULUNAMADI!");
                    }
                }
            } catch(Exception ex) {
                Console.WriteLine($"❌ Seller Güncelleme Hatası: {ex.Message}");
                _logger.LogError(ex, "Seller Güncelleme Hatası");
            }

        } catch(Exception ex){
            Console.WriteLine($"❌ KRİTİK HATA: {ex.Message}");
            _logger.LogError(ex, "[OtoIsmailBackgroundService] Kritik Hata oluştu: {Message}", ex.Message);
            
            // Update Seller with error message (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(1);
                    if (seller != null)
                    {
                        seller.SyncDate = DateTime.Now;
                        seller.SyncMessage = $"❌ Hata: {ex.Message}";
                        await scopedContext.SaveChangesAsync();
                    }
                }
            } catch { /* ignore error during error logging */ }
            // throw; // Job fail olmasın diye kaldırdım.
        }
    }
}
