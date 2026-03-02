using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using Remar.Abstract;
using Remar.Dtos;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider extensions

namespace Remar.BackgroundServices;
public class RemarProductBackgroundService:IAsyncBackgroundJob{
   
    private readonly IDapperService _dapperService;
    private readonly IServiceProvider _serviceProvider; // Added
    private readonly IRemarApiService _apiClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<RemarProductBackgroundService> _logger;

    public RemarProductBackgroundService(IUnitOfWork<ApplicationDbContext> context, IRemarApiService apiClient, ILogger<RemarProductBackgroundService> logger, IServiceProvider serviceProvider) // Modified
    {
        _context = context;
        _apiClient = apiClient;
        _logger = logger;
        _serviceProvider = serviceProvider; // Added
    }
    public async Task ExecuteAsync()
    {
        try {
            Console.WriteLine("🚀 RemarProductBackgroundService başlatıldı...");
            _logger.LogInformation($"RemarProductBackgroundService başlatıldı: {DateTime.Now}");

        var token = await _apiClient.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("❌ Remar oturum açma başarısız. Token boş.");
            _logger.LogError("Remar oturum açma başarısız. Token boş.");
            return;
        }
        
        Console.WriteLine("✅ Token alındı");
        _logger.LogInformation("Token alma başarılı");

        var pageNo = 1;
        const int pageLen = 5000;
        var allProducts = new List<RemarProductDto>();
        int totalPagesProcessed = 0;
        int totalProductsFromApi = 0;
        int totalNewProducts = 0;
        int totalUpdatedProducts = 0;

        while (true)
        {
            Console.WriteLine($"📥 Sayfa {pageNo} alınıyor...");
            _logger.LogInformation($"Ürünleri alınıyor... Sayfa: {pageNo}");
            
            var request = new RemarProductListRequestDto
            {
                SearchText = "",
                ProductCode = "",
                PageNo = pageNo,
                PageLen = pageLen
            };

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
                var existingProducts = await _context.GetRepository<ProductRemar>()
                    .GetAllAsync(predicate: pr => codes.Contains(pr.Code));
                
                var existingMap = existingProducts.ToDictionary(p => p.Code, p => p);
                
                var toInsert = new List<ProductRemar>();
                var toUpdate = new List<ProductRemar>();

                foreach (var p in allProducts)
                {
                    // Check fallback for Depo fields (Prioritize "Var" if either has it)
                    var depo1Val = (string.Equals(p.Depo_1, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo1, "Var", StringComparison.OrdinalIgnoreCase)) 
                                   ? "Var" 
                                   : (!string.IsNullOrEmpty(p.Depo_1) ? p.Depo_1 : p.Depo1);

                    var depo2Val = (string.Equals(p.Depo_2, "Var", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Depo2, "Var", StringComparison.OrdinalIgnoreCase)) 
                                   ? "Var" 
                                   : (!string.IsNullOrEmpty(p.Depo_2) ? p.Depo_2 : p.Depo2);

                    if (existingMap.TryGetValue(p.Code, out var existing))
                    {
                        // Update
                        existing.Name = p.Name;
                        existing.Manufacturer = p.Manufacturer;
                        existing.Unit = p.Unit;
                        existing.Oem_No = p.Oem_No;
                        existing.SalePriceContact = p.SalePriceContact;
                        existing.SalePriceContactCurrency = p.SalePriceContactCurrency;
                        existing.MinOrderQuantity = p.MinOrderQuantity;
                        existing.Status = 1;
                        existing.ModifiedDate = DateTime.Now;
                        existing.ModifiedId = 1;
                        existing.Cross_Referans = p.Cross_Referans;
                        
                        existing.Depo_1 = depo1Val;
                        existing.Depo_2 = depo2Val;
                        
                        existing.PackageUsage = p.PackageUsage;
                        existing.SpecialField_1 = p.SpecialField_1;
                        existing.SpecialField_2 = p.SpecialField_2;
                        
                        toUpdate.Add(existing);
                    }
                    else
                    {
                        // Insert
                        var newEntity = new ProductRemar
                        {
                            Code = p.Code,
                            Name = p.Name,
                            Manufacturer = p.Manufacturer,
                            Unit = p.Unit,
                            Oem_No = p.Oem_No,
                            SalePriceContact = p.SalePriceContact,
                            SalePriceContactCurrency = p.SalePriceContactCurrency,
                            MinOrderQuantity = p.MinOrderQuantity,
                            CreatedDate = DateTime.Now,
                            Status = 1,
                            ModifiedDate = DateTime.Now,
                            ModifiedId = 1,
                            CreatedId = 1,
                            Cross_Referans = p.Cross_Referans,
                            
                            Depo_1 = depo1Val,
                            Depo_2 = depo2Val,
                            
                            PackageUsage = p.PackageUsage,
                            SpecialField_1 = p.SpecialField_1,
                            SpecialField_2 = p.SpecialField_2
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
                if (allProducts.Any())
                {
                    Console.WriteLine($"📦 Son batch işleniyor: {allProducts.Count} ürün veritabanına aktarılıyor...");
                    _logger.LogInformation($"{allProducts.Count} ürün kaldı. Veritabanına aktarılıyor...");

                    var codes = allProducts.Select(p => p.Code).ToList();
                    var existingProducts = await _context.GetRepository<ProductRemar>()
                        .GetAllAsync(predicate: pr => codes.Contains(pr.Code));
                    
                    var existingMap = existingProducts.ToDictionary(p => p.Code, p => p);
                    
                    var toInsert = new List<ProductRemar>();
                    var toUpdate = new List<ProductRemar>();

                    foreach (var p in allProducts)
                    {
                        if (existingMap.TryGetValue(p.Code, out var existing))
                        {
                            // Update
                            existing.Name = p.Name;
                            existing.Manufacturer = p.Manufacturer;
                            existing.Unit = p.Unit;
                            existing.Oem_No = p.Oem_No;
                            existing.SalePriceContact = p.SalePriceContact;
                            existing.SalePriceContactCurrency = p.SalePriceContactCurrency;
                            existing.MinOrderQuantity = p.MinOrderQuantity;
                            existing.Status = 1;
                            existing.ModifiedDate = DateTime.Now;
                            existing.ModifiedId = 1;
                            existing.Cross_Referans = p.Cross_Referans;
                            existing.Depo_1 = p.Depo_1;
                            existing.Depo_2 = p.Depo_2;
                            existing.PackageUsage = p.PackageUsage;
                            existing.SpecialField_1 = p.SpecialField_1;
                            existing.SpecialField_2 = p.SpecialField_2;
                            
                            toUpdate.Add(existing);
                        }
                        else
                        {
                            // Insert
                            var newEntity = new ProductRemar
                            {
                                Code = p.Code,
                                Name = p.Name,
                                Manufacturer = p.Manufacturer,
                                Unit = p.Unit,
                                Oem_No = p.Oem_No,
                                SalePriceContact = p.SalePriceContact,
                                SalePriceContactCurrency = p.SalePriceContactCurrency,
                                MinOrderQuantity = p.MinOrderQuantity,
                                CreatedDate = DateTime.Now,
                                Status = 1,
                                ModifiedDate = DateTime.Now,
                                ModifiedId = 1,
                                CreatedId = 1,
                                Cross_Referans = p.Cross_Referans,
                                Depo_1 = p.Depo_1,
                                Depo_2 = p.Depo_2,
                                PackageUsage = p.PackageUsage,
                                SpecialField_1 = p.SpecialField_1,
                                SpecialField_2 = p.SpecialField_2
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
        
        // Process any remaining products after pagination loop
        if (allProducts.Any())
        {
            Console.WriteLine($"📦 Son batch işleniyor: {allProducts.Count} ürün veritabanına aktarılıyor...");
            _logger.LogInformation($"{allProducts.Count} ürün kaldı. Veritabanına aktarılıyor...");

            var codes = allProducts.Select(p => p.Code).ToList();
            var existingProducts = await _context.GetRepository<ProductRemar>()
                .GetAllAsync(predicate: pr => codes.Contains(pr.Code));
            
            var existingMap = existingProducts.ToDictionary(p => p.Code, p => p);
            
            var toInsert = new List<ProductRemar>();
            var toUpdate = new List<ProductRemar>();

            foreach (var p in allProducts)
            {
                if (existingMap.TryGetValue(p.Code, out var existing))
                {
                    // Update
                    existing.Name = p.Name;
                    existing.Manufacturer = p.Manufacturer;
                    existing.Unit = p.Unit;
                    existing.Oem_No = p.Oem_No;
                    existing.SalePriceContact = p.SalePriceContact;
                    existing.SalePriceContactCurrency = p.SalePriceContactCurrency;
                    existing.MinOrderQuantity = p.MinOrderQuantity;
                    existing.Status = 1;
                    existing.ModifiedDate = DateTime.Now;
                    existing.ModifiedId = 1;
                    existing.Cross_Referans = p.Cross_Referans;
                    existing.Depo_1 = p.Depo_1;
                    existing.Depo_2 = p.Depo_2;
                    existing.PackageUsage = p.PackageUsage;
                    existing.SpecialField_1 = p.SpecialField_1;
                    existing.SpecialField_2 = p.SpecialField_2;
                    
                    toUpdate.Add(existing);
                }
                else
                {
                    // Insert
                    var newEntity = new ProductRemar
                    {
                        Code = p.Code,
                        Name = p.Name,
                        Manufacturer = p.Manufacturer,
                        Unit = p.Unit,
                        Oem_No = p.Oem_No,
                        SalePriceContact = p.SalePriceContact,
                        SalePriceContactCurrency = p.SalePriceContactCurrency,
                        MinOrderQuantity = p.MinOrderQuantity,
                        CreatedDate = DateTime.Now,
                        Status = 1,
                        ModifiedDate = DateTime.Now,
                        ModifiedId = 1,
                        CreatedId = 1,
                        Cross_Referans = p.Cross_Referans,
                        Depo_1 = p.Depo_1,
                        Depo_2 = p.Depo_2,
                        PackageUsage = p.PackageUsage,
                        SpecialField_1 = p.SpecialField_1,
                        SpecialField_2 = p.SpecialField_2
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

            // Log Success
            totalNewProducts += toInsert.Count;
            totalUpdatedProducts += toUpdate.Count;
            Console.WriteLine($"✅ Son batch tamamlandı: {allProducts.Count} ürün işlendi");
            _logger.LogInformation($"Son batch tamamlandı: {allProducts.Count} ürün işlendi");

            allProducts.Clear();
        }

        // Toplam özet
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"📊 REMAR TOPLAM ÖZET:");
        Console.WriteLine($"   • İşlenen Sayfa: {totalPagesProcessed}");
        Console.WriteLine($"   • API'den Gelen Ürün: {totalProductsFromApi}");
        Console.WriteLine($"   • Yeni Ürün: {totalNewProducts}");
        Console.WriteLine($"   • Güncellenen Ürün: {totalUpdatedProducts}");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        _logger.LogInformation($"RemarProductBackgroundService tamamlandı - Toplam: {totalProductsFromApi} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)");
        
        Console.WriteLine("⏳ Seller (Id=4) güncellemesi yapılıyor (Yeni Scope)...");
        _logger.LogInformation("Seller (Id=4) güncellemesi yapılıyor (Yeni Scope)...");
        
        try {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var seller = await scopedContext.GetRepository<Seller>().FindAsync(4);
                if (seller != null)
                {
                    seller.SyncDate = DateTime.Now;
                    seller.SyncMessage = $"✅ Başarılı: {totalProductsFromApi} ürün ({totalNewProducts} yeni, {totalUpdatedProducts} güncelleme)";
                    
                    await scopedContext.SaveChangesAsync();
                    Console.WriteLine($"✅ Seller (Id=4) SyncDate güncellendi: {seller.SyncDate}");
                    _logger.LogInformation($"Seller (Id=4) SyncDate güncellendi: {seller.SyncDate}");
                } else {
                    Console.WriteLine($"⚠️ Seller (Id=4) BULUNAMADI!");
                    _logger.LogWarning($"Seller (Id=4) BULUNAMADI!");
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"❌ Seller Güncelleme Hatası: {ex.Message}");
            _logger.LogError(ex, "Seller Güncelleme Hatası");
        }

        } catch(Exception ex){
            Console.WriteLine($"❌ KRİTİK HATA: {ex.Message}");
            _logger.LogError(ex, "RemarProductBackgroundService kritik hata: {Message}", ex.Message);
            
            // Update Seller with error message (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(4);
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
