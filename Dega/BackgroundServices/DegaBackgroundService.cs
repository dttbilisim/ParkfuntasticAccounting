using Dega.Abstract;
using Dega.Dtos;
using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
namespace Dega.BackgroundServices;
public class DegaBackgroundService : IAsyncBackgroundJob{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDapperService _dapperService;
    private readonly IDegaService _apiClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<DegaBackgroundService> _logger;

    public DegaBackgroundService(IUnitOfWork<ApplicationDbContext> context, IDegaService apiClient, IDapperService dapperService, ILogger<DegaBackgroundService> logger, IServiceProvider serviceProvider){
        _context = context;
        _apiClient = apiClient;
        _dapperService = dapperService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public async Task ExecuteAsync(){
        try {
            Console.WriteLine("🚀 DegaBackgroundService başlatıldı...");
            _logger.LogInformation($"DegaProductBackgroundService başlatıldı: {DateTime.Now}");
        
        var token = await _apiClient.GetTokenAsync();
        if(string.IsNullOrWhiteSpace(token)){
            Console.WriteLine("❌ Dega oturum açma başarısız. Token boş.");
            _logger.LogError("Dega oturum açma başarısız. Token boş.");
            return;
        }
        
        Console.WriteLine("✅ Token alındı");
        _logger.LogInformation("Token alma başarılı");
        
        var pageNo = 1;
        const int pageLen = 5000;
        var allProducts = new List<ProductResponse>();
        int totalPagesProcessed = 0;
        int totalProductsFromApi = 0;
        int totalNewProducts = 0;
        int totalUpdatedProducts = 0;
        while (true)
        {
            Console.WriteLine($"📥 Sayfa {pageNo} alınıyor...");
            _logger.LogInformation($"Ürünleri alınıyor... Sayfa: {pageNo}");

            var request = new ProductListRequestDto { SearchText = "", ProductCode = "", PageNo = pageNo, PageLen = pageLen };
            var pageProducts = await _apiClient.GetProductsAsync(token, request);

            if (pageProducts == null || pageProducts.Count == 0)
            {
                Console.WriteLine($"⚠️ Sayfa {pageNo}: Ürün bulunamadı (0 ürün)");
                _logger.LogInformation("Daha fazla ürün bulunamadı.");
                break;
            }

            Console.WriteLine($"✅ Sayfa {pageNo}: {pageProducts.Count} ürün geldi");
            _logger.LogInformation($"Sayfa {pageNo}: {pageProducts.Count} ürün geldi");
            
            allProducts.AddRange(pageProducts);
            totalProductsFromApi += pageProducts.Count;
            totalPagesProcessed++;

            if (allProducts.Count >= 5000)
            {
                Console.WriteLine($"📦 Batch işleniyor: {allProducts.Count} ürün veritabanına aktarılıyor...");
                _logger.LogInformation($"5000 ürün birikti. Veritabanına aktarılıyor...");
            
                // Mevcut ürünleri getir
                var codes = allProducts.Select(p => p.Code).ToList();
                var existingProducts = await _context.GetRepository<ProductDega>()
                    .GetAllAsync(predicate: pd => codes.Contains(pd.Code));
                
                var existingMap = existingProducts.ToDictionary(p => p.Code, p => p);
                
                var toInsert = new List<ProductDega>();
                var toUpdate = new List<ProductDega>();
                
                foreach (var p in allProducts)
                {
                    // Fallback logic for Depo fields (Prioritize "Var")
                    var d1 = (string.Equals(p.Depo_1, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo1, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_1) ? p.Depo_1 : p.Depo1);
                    var d2 = (string.Equals(p.Depo_2, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo2, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_2) ? p.Depo_2 : p.Depo2);
                    var d3 = (string.Equals(p.Depo_3, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo3, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_3) ? p.Depo_3 : p.Depo3);
                    var d4 = (string.Equals(p.Depo_4, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo4, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_4) ? p.Depo_4 : p.Depo4);
                    var d5 = (string.Equals(p.Depo_5, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo5, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_5) ? p.Depo_5 : p.Depo5);
                    var d6 = (string.Equals(p.Depo_6, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo6, "Var", StringComparison.OrdinalIgnoreCase)) ? "Var" : (!string.IsNullOrEmpty(p.Depo_6) ? p.Depo_6 : p.Depo6);

                    if (existingMap.TryGetValue(p.Code, out var existing))
                    {
                        // Update
                        existing.Name = p.Name;
                        existing.Manufacturer = p.Manufacturer;
                        existing.Unit = p.Unit;
                        existing.SalePriceContact = p.SalePriceContact;
                        existing.SalePriceContactCurrency = p.SalePriceContactCurrency;
                        existing.ModifiedDate = DateTime.Now;
                        existing.Status = 1; // Ensure status is active
                        existing.Depo_1 = d1;
                        existing.Depo_2 = d2;
                        existing.Depo_3 = d3;
                        existing.Depo_4 = d4;
                        existing.Depo_5 = d5;
                        existing.Depo_6 = d6;
                        existing.Depo1 = d1;
                        existing.Depo2 = d2;
                        existing.Depo3 = d3;
                        existing.Depo4 = d4;
                        existing.Depo5 = d5;
                        existing.Depo6 = d6;
                        existing.OrjinalKod = p.OrjinalKod;
                        existing.SpecialField9 = p.SpecialField9;
                        
                        toUpdate.Add(existing);
                    }
                    else
                    {
                        // Insert
                        var newEntity = new ProductDega
                        {
                            Code = p.Code,
                            Name = p.Name,
                            Manufacturer = p.Manufacturer,
                            Unit = p.Unit,
                            SalePriceContact = p.SalePriceContact,
                            SalePriceContactCurrency = p.SalePriceContactCurrency,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            Status = 1,
                            CreatedId = 1,
                            Depo_1 = d1,
                            Depo_2 = d2,
                            Depo_3 = d3,
                            Depo_4 = d4,
                            Depo_5 = d5,
                            Depo_6 = d6,
                            Depo1 = d1,
                            Depo2 = d2,
                            Depo3 = d3,
                            Depo4 = d4,
                            Depo5 = d5,
                            Depo6 = d6,
                            OrjinalKod = p.OrjinalKod,
                            SpecialField9 = p.SpecialField9,
                        };
                        toInsert.Add(newEntity);
                    }
                }
                
                Console.WriteLine($"   • {toInsert.Count} yeni ürün");
                Console.WriteLine($"   • {toUpdate.Count} güncelleme");
                _logger.LogInformation($"Batch: {toInsert.Count} yeni, {toUpdate.Count} güncelleme");

                if (toInsert.Any())
                {
                    await _context.DbContext.BulkInsertAsync(toInsert, new BulkConfig { SetOutputIdentity = true });
                }
                
                if (toUpdate.Any())
                {
                    await _context.DbContext.BulkUpdateAsync(toUpdate);
                }

                // Log Success
                totalNewProducts += toInsert.Count;
                totalUpdatedProducts += toUpdate.Count;
                Console.WriteLine($"✅ Batch tamamlandı: {allProducts.Count} ürün işlendi");
                _logger.LogInformation($"Batch tamamlandı: {allProducts.Count} ürün işlendi");

                allProducts.Clear();
            }

            if (pageProducts.Count < pageLen)
            {
               // Process any remaining products after pagination loop
               if (allProducts.Any())
               {
                   Console.WriteLine($"📦 Son batch işleniyor: {allProducts.Count} ürün veritabanına aktarılıyor...");
                   _logger.LogInformation($"{allProducts.Count} ürün kaldı. Veritabanına aktarılıyor...");

                   var codes = allProducts.Select(p => p.Code).ToList();
                   var existingProducts = await _context.GetRepository<ProductDega>()
                       .GetAllAsync(predicate: pd => codes.Contains(pd.Code));
                   
                   var existingMap = existingProducts.ToDictionary(p => p.Code, p => p);
                   
                   var toInsert = new List<ProductDega>();
                   var toUpdate = new List<ProductDega>();
                   
                   foreach (var p in allProducts)
                   {
                       if (existingMap.TryGetValue(p.Code, out var existing))
                       {
                            // Update
                            existing.Name = p.Name;
                            existing.Manufacturer = p.Manufacturer;
                            existing.Unit = p.Unit;
                            existing.SalePriceContact = p.SalePriceContact;
                            existing.SalePriceContactCurrency = p.SalePriceContactCurrency;
                            existing.ModifiedDate = DateTime.Now;
                            existing.Status = 1;
                            existing.Depo_1 = p.Depo_1;
                            existing.Depo_2 = p.Depo_2;
                            existing.Depo_3 = p.Depo_3;
                            existing.Depo_4 = p.Depo_4;
                            existing.Depo_5 = p.Depo_5;
                            existing.Depo_6 = p.Depo_6;
                            existing.Depo1 = p.Depo1;
                            existing.Depo2 = p.Depo2;
                            existing.Depo3 = p.Depo3;
                            existing.Depo4 = p.Depo4;
                            existing.Depo5 = p.Depo5;
                            existing.Depo6 = p.Depo6;
                            existing.OrjinalKod = p.OrjinalKod;
                            existing.SpecialField9 = p.SpecialField9;
                            
                            toUpdate.Add(existing);
                       }
                       else
                       {
                            // Insert
                            var newEntity = new ProductDega
                            {
                                Code = p.Code,
                                Name = p.Name,
                                Manufacturer = p.Manufacturer,
                                Unit = p.Unit,
                                SalePriceContact = p.SalePriceContact,
                                SalePriceContactCurrency = p.SalePriceContactCurrency,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                Status = 1,
                                CreatedId = 1,
                                Depo_1 = p.Depo_1,
                                Depo_2 = p.Depo_2,
                                Depo_3 = p.Depo_3,
                                Depo_4 = p.Depo_4,
                                Depo_5 = p.Depo_5,
                                Depo_6 = p.Depo_6,
                                Depo1 = p.Depo1,
                                Depo2 = p.Depo2,
                                Depo3 = p.Depo3,
                                Depo4 = p.Depo4,
                                Depo5 = p.Depo5,
                                Depo6 = p.Depo6,
                                OrjinalKod = p.OrjinalKod,
                                SpecialField9 = p.SpecialField9,
                            };
                            toInsert.Add(newEntity);
                       }
                   }
                   
                   Console.WriteLine($"   • {toInsert.Count} yeni ürün");
                   Console.WriteLine($"   • {toUpdate.Count} güncelleme");
                   _logger.LogInformation($"Son batch: {toInsert.Count} yeni, {toUpdate.Count} güncelleme");

                   if (toInsert.Any())
                   {
                        await _context.DbContext.BulkInsertAsync(toInsert, new BulkConfig { SetOutputIdentity = true });
                   }
                   
                   if (toUpdate.Any())
                   {
                        await _context.DbContext.BulkUpdateAsync(toUpdate);
                   }

                   await _context.SaveChangesAsync();
                   var lastSave = _context.LastSaveChangesResult;
                   if (lastSave.IsOk)
                   {
                       totalNewProducts += toInsert.Count;
                       totalUpdatedProducts += toUpdate.Count;
                       Console.WriteLine($"✅ Son batch tamamlandı: {allProducts.Count} ürün işlendi");
                       _logger.LogInformation($"Son batch tamamlandı: {allProducts.Count} ürün işlendi");
                   }
                   else
                   {
                       Console.WriteLine($"❌ Son batch hatası: {lastSave.Exception?.Message}");
                       _logger.LogError($"Son batch hatası: {lastSave.Exception?.Message}");
                   }

                   allProducts.Clear();
               }

                Console.WriteLine("🏁 Son sayfaya ulaşıldı");
                _logger.LogInformation("Son sayfaya ulaşıldı.");
                break;
            }

            pageNo++;
        }
        
        // Toplam özet
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"📊 DEGA TOPLAM ÖZET:");
        Console.WriteLine($"   • İşlenen Sayfa: {totalPagesProcessed}");
        Console.WriteLine($"   • API'den Gelen Ürün: {totalProductsFromApi}");
        Console.WriteLine($"   • Yeni Ürün: {totalNewProducts}");
        Console.WriteLine($"   • Güncellenen Ürün: {totalUpdatedProducts}");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        _logger.LogInformation($"DegaBackgroundService tamamlandı - Toplam: {totalProductsFromApi} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)");
        
        Console.WriteLine("⏳ Seller (Id=3) güncellemesi yapılıyor (Yeni Scope)...");
        _logger.LogInformation("Seller (Id=3) güncellemesi yapılıyor (Yeni Scope)...");
        
        try {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var seller = await scopedContext.GetRepository<Seller>().FindAsync(3);
                if (seller != null)
                {
                    seller.SyncDate = DateTime.Now;
                    seller.SyncMessage = $"✅ Başarılı: {totalProductsFromApi} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)";
                    await scopedContext.SaveChangesAsync();
                    Console.WriteLine($"✅ Seller (Id=3) SyncDate güncellendi: {seller.SyncDate}");
                    _logger.LogInformation($"Seller (Id=3) SyncDate güncellendi: {seller.SyncDate}");
                } else {
                     Console.WriteLine($"⚠️ Seller (Id=3) BULUNAMADI!");
                     _logger.LogWarning($"Seller (Id=3) BULUNAMADI!");
                }
            }
        } catch(Exception ex) {
            Console.WriteLine($"❌ Seller Güncelleme Hatası: {ex.Message}");
            _logger.LogError(ex, "Seller Güncelleme Hatası");
        }

        } catch(Exception ex){
            Console.WriteLine($"❌ KRİTİK HATA: {ex.Message}");
            _logger.LogError(ex, "DegaBackgroundService kritik hata: {Message}", ex.Message);
            
            // Update Seller with error message (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(3);
                    if (seller != null)
                    {
                        seller.SyncDate = DateTime.Now;
                        seller.SyncMessage = $"❌ Hata: {ex.Message}";
                        await scopedContext.SaveChangesAsync();
                    }
                }
            } catch { /* ignore error during error logging */ }
        }
    }
}
