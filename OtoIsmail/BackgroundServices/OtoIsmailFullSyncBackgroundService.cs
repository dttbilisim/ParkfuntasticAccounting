using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using OtoIsmail.Abstract;
using OtoIsmail.Concreate;
using OtoIsmail.Dtos;
namespace OtoIsmail.BackgroundServices;
public class OtoIsmailFullSyncBackgroundService : IAsyncBackgroundJob{
    private readonly IDapperService _dapperService;
    private readonly IApiClient _apiClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<OtoIsmailFullSyncBackgroundService> _logger;
    public OtoIsmailFullSyncBackgroundService(
        IUnitOfWork<ApplicationDbContext> context,
        IApiClient apiClient,
        IDapperService dapperService,
        ILogger<OtoIsmailFullSyncBackgroundService> logger)
    {
        _context = context;
        _apiClient = apiClient;
        _dapperService = dapperService;
        _logger = logger;
    }
    public async Task ExecuteAsync(){
        try{
            Console.WriteLine("🚀 OtoIsmailFullSyncBackgroundService başlatıldı (TAM SENKRONİZASYON - Tüm ürünler)...");
            _logger.LogInformation("OtoIsmailFullSyncBackgroundService başlatıldı (TAM SENKRONİZASYON)");
            
            var brands = await _apiClient.GetBrandsAsync();
            if(brands == null || brands.Count == 0){
                Console.WriteLine("❌ Marka verisi alınamadı veya boş (0 marka)");
                _logger.LogError("Marka verisi alınamadı veya boş. API yanıt dönmedi (veya Token alınamadı).");
                return;
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
            int totalSkippedProducts = 0;
            
            foreach (var brand in brands)
            {
                try
                {
                    // TAM SENKRONİZASYON: Tüm ürünleri almak için çok eski bir tarih gönder
                    // API dokümantasyonuna göre: "tarih parametresinde belirlenen tarihten sonra değişen stok kartlarını listeler"
                    // Tüm ürünler için çok eski bir tarih (19000101) gönderiyoruz
                    // Haftada bir çalışacak, tüm ürünleri senkronize edecek
                    var products = await _apiClient.GetProductsAsync(brand.Kod, "19000101");
                    if (products == null || products.Count == 0)
                    {
                        Console.WriteLine($"[{brand.Kod}] ⚠️ API'den ürün gelmedi veya boş (0 ürün)");
                        _logger.LogWarning($"Marka {brand.Kod} için ürün verisi alınamadı veya boş.");
                        continue;
                    }
                    
                    // LOG: API'den gelen ürün sayısı
                    Console.WriteLine($"[{brand.Kod}] 📥 API'den {products.Count} ürün geldi (TAM SENKRONİZASYON)");
                    _logger.LogInformation($"[{brand.Kod}] API'den {products.Count} ürün geldi (TAM SENKRONİZASYON)");

                    var netsisIds = products.Select(p => p.NetsisStokId).Distinct().ToList();
                    Console.WriteLine($"[{brand.Kod}] 🔍 {netsisIds.Count} unique NetsisStokId kontrol ediliyor...");
                    _logger.LogInformation($"[{brand.Kod}] {netsisIds.Count} unique NetsisStokId kontrol ediliyor");
                    
                    var existingProducts = await _context.GetRepository<ProductOtoIsmail>()
                        .GetAllAsync(predicate: p => netsisIds.Contains(p.NetsisStokId));

                    // LOG: Mevcut ürün sayısı
                    Console.WriteLine($"[{brand.Kod}] 🔍 Veritabanında {existingProducts.Count} mevcut ürün bulundu (Toplam: {products.Count} ürün)");
                    _logger.LogInformation($"[{brand.Kod}] Veritabanında {existingProducts.Count} mevcut ürün bulundu (Toplam: {products.Count} ürün)");

                    // FIXED: Sadece yeni ürünleri ekle, mevcut ürünlere dokunma
                    // Hem veritabanındaki mevcut kayıtları hem de aynı batch içindeki duplicate'leri filtrele
                    var existingNetsisIds = existingProducts.Select(p => p.NetsisStokId).ToHashSet();
                    var processedNetsisIds = new HashSet<int>(); // Aynı batch içindeki duplicate'leri takip et
                    var newProducts = new List<ProductOtoIsmail>();
                    int newProductCount = 0;
                    int skippedCount = 0;
                    int duplicateInBatchCount = 0;
                    
                    foreach (var product in products)
                    {
                        // NetsisStokId 0 veya negatif olamaz
                        if (product.NetsisStokId <= 0)
                        {
                            skippedCount++;
                            continue;
                        }
                        
                        // Veritabanında mevcut mu kontrol et
                        if (existingNetsisIds.Contains(product.NetsisStokId))
                        {
                            skippedCount++;
                            continue;
                        }
                        
                        // Aynı batch içinde daha önce işlendi mi kontrol et (duplicate)
                        if (processedNetsisIds.Contains(product.NetsisStokId))
                        {
                            duplicateInBatchCount++;
                            skippedCount++;
                            continue;
                        }
                        
                        // Yeni ürün - ekle
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
                        newProducts.Add(newProduct);
                        processedNetsisIds.Add(product.NetsisStokId); // İşlenen olarak işaretle
                        newProductCount++;
                    }
                    
                    totalSkippedProducts += skippedCount;
                    
                    // LOG: Detaylı özet
                    Console.WriteLine($"[{brand.Kod}] 📊 DETAYLI ANALİZ:");
                    Console.WriteLine($"[{brand.Kod}]   ┌─────────────────────────────────────────┐");
                    Console.WriteLine($"[{brand.Kod}]   │ API'den Gelen:        {products.Count,10} ürün │");
                    Console.WriteLine($"[{brand.Kod}]   │ Veritabanında Mevcut: {existingProducts.Count,10} ürün │");
                    Console.WriteLine($"[{brand.Kod}]   │ Batch İçi Duplicate:  {duplicateInBatchCount,10} ürün │");
                    Console.WriteLine($"[{brand.Kod}]   │ Yeni Eklenecek:       {newProductCount,10} ürün │");
                    Console.WriteLine($"[{brand.Kod}]   │ Toplam Atlanan:       {skippedCount,10} ürün │");
                    Console.WriteLine($"[{brand.Kod}]   └─────────────────────────────────────────┘");
                    _logger.LogInformation($"[{brand.Kod}] DETAYLI ANALİZ - API: {products.Count}, Mevcut: {existingProducts.Count}, Batch Duplicate: {duplicateInBatchCount}, Yeni: {newProductCount}, Atlanan: {skippedCount}");

                    // FIXED: Sadece yeni ürünleri ekle, mevcut ürünlere dokunma
                    if (newProducts.Any())
                    {
                        Console.WriteLine($"[{brand.Kod}] 📝 {newProducts.Count} yeni ürün insert ediliyor...");
                        _logger.LogInformation($"[{brand.Kod}] {newProducts.Count} yeni ürün insert ediliyor");
                        
                        var insertConfig = new BulkConfig
                        {
                            SetOutputIdentity = true  // Yeni eklenen kayıtların Id'lerini al
                        };
                        
                        try
                        {
                            // BulkInsert - Duplicate kontrolü zaten yapıldı (existingNetsisIds + processedNetsisIds)
                            // Unique index sayesinde ekstra koruma var, ama duplicate'ler zaten filtrelendi
                            await _context.DbContext.BulkInsertAsync(newProducts, insertConfig);
                            await _context.SaveChangesAsync();
                            
                            var lastSaveProduct = _context.LastSaveChangesResult;
                            if(lastSaveProduct.IsOk){
                                totalProcessedProducts += newProducts.Count;
                                totalNewProducts += newProducts.Count;
                                
                                Console.WriteLine($"[{brand.Kod}] ✅ BAŞARILI: {newProducts.Count} yeni ürün veritabanına eklendi");
                                _logger.LogInformation($"[{brand.Kod}] ✅ BAŞARILI: {newProducts.Count} yeni ürün veritabanına eklendi");
                            }
                            else{
                                Console.WriteLine($"[{brand.Kod}] ❌ SaveChanges HATASI: {lastSaveProduct.Exception?.Message}");
                                if (lastSaveProduct.Exception?.InnerException != null)
                                {
                                    Console.WriteLine($"[{brand.Kod}] ❌ InnerException: {lastSaveProduct.Exception.InnerException.Message}");
                                }
                                _logger.LogError($"[{brand.Kod}] SaveChanges hatası: {lastSaveProduct.Exception?.Message}");
                            }
                        }
                        catch (Exception insertEx)
                        {
                            Console.WriteLine($"[{brand.Kod}] ❌ Insert hatası: {insertEx.Message}");
                            Console.WriteLine($"[{brand.Kod}] ❌ StackTrace: {insertEx.StackTrace}");
                            _logger.LogError(insertEx, $"[{brand.Kod}] Insert hatası: {insertEx.Message}");
                            throw; // Hata varsa devam etmesin
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{brand.Kod}] ℹ️ Yeni ürün yok - Tüm ürünler zaten veritabanında mevcut");
                        _logger.LogInformation($"[{brand.Kod}] Yeni ürün yok - Tüm ürünler zaten veritabanında mevcut");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OtoIsmailFullSyncBackgroundService] {brand.Kod} için ürün işlenirken hata oluştu: {ex.Message}");
                    _logger.LogInformation($"[OtoIsmailFullSyncBackgroundService] {brand.Kod} için ürün işlenirken hata oluştu: {ex.Message}");
                    continue;
                }
            }
            
            // LOG: Toplam özet
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"📊 TAM SENKRONİZASYON - TOPLAM ÖZET:");
            Console.WriteLine($"   ┌─────────────────────────────────────────────────────┐");
            Console.WriteLine($"   │ İşlenen Marka:              {brands.Count,10} marka │");
            Console.WriteLine($"   │ Yeni Eklenen Ürün:          {totalNewProducts,10} ürün │");
            Console.WriteLine($"   │ Mevcut Ürünler (Atlanan):   {totalSkippedProducts,10} ürün │");
            Console.WriteLine($"   │ Toplam İşlenen:             {totalProcessedProducts,10} ürün │");
            Console.WriteLine($"   └─────────────────────────────────────────────────────┘");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            _logger.LogInformation($"OtoIsmailFullSyncBackgroundService tamamlandı - Toplam: {totalNewProducts} yeni ürün eklendi, {totalSkippedProducts} mevcut ürün atlandı");
        } catch(Exception ex){
            Console.WriteLine($"❌ KRİTİK HATA: {ex.Message}");
            _logger.LogError(ex, "[OtoIsmailFullSyncBackgroundService] Kritik Hata oluştu: {Message}", ex.Message);
        }
    }
}
