using BasbugOto.Abstract;
using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
namespace BasbugOto.BackgroundServices;
public class BasbugOtoBackgroundService : IAsyncBackgroundJob{
    private readonly IDapperService _dapperService;
    private readonly IBasbugApiService _apiClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<BasbugOtoBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BasbugOtoBackgroundService(IUnitOfWork<ApplicationDbContext> context, IBasbugApiService apiClient, IDapperService dapperService, ILogger<BasbugOtoBackgroundService> logger, IServiceProvider serviceProvider){
        _context = context;
        _apiClient = apiClient;
        _logger = logger;
        _dapperService = dapperService;
        _serviceProvider = serviceProvider;
    }
    public async Task ExecuteAsync(){
        try{
            _logger.LogInformation("Basbug ürün aktarımı başladı...");
            Console.WriteLine("Basbug ürün aktarımı başladı...");

            var groups = await _apiClient.GetGroupsAsync();
            _logger.LogInformation("Toplam {Count} grup bulundu.", groups.Count);
            Console.WriteLine($"Toplam {groups.Count} grup bulundu.");

            foreach (var group in groups)
            {
                var products = await _apiClient.GetProductsByGroupAsync(group.Kod, "MRK");
                var stockList = await _apiClient.GetStockByGroupAsync(group.Kod, "MRK");

                _logger.LogInformation("{Grup} grubu için {Count} ürün bulundu.", group.Kod, products.Count);
                Console.WriteLine($"{group.Kod} grubu için {products.Count} ürün bulundu.");

                // Mevcut ürünleri getir
                var productNos = products.Select(p => p.No).ToList();
                var existingProducts = await _context.GetRepository<ProductBasbug>()
                    .GetAllAsync(predicate: pb => productNos.Contains(pb.No));
                
                var existingMap = existingProducts.ToDictionary(p => p.No, p => p);
                
                var toInsert = new List<ProductBasbug>();
                var toUpdate = new List<ProductBasbug>();
                var now = DateTime.UtcNow;

                foreach (var p in products)
                {
                    var stockVal = stockList.FirstOrDefault(s => s.No == p.No)?.Stok ?? 0;
                    
                    if (existingMap.TryGetValue(p.No, out var existing))
                    {
                        // Update
                        existing.Aciklama1 = p.Aciklama1;
                        existing.Aciklama2 = p.Aciklama2;
                        existing.MarkaKod = p.MarkaKod;
                        existing.OemKod = p.OemKod;
                        existing.Uretici = p.Uretici;
                        existing.GrupKod = p.GrupKod;
                        existing.Model = p.Model;
                        existing.Motor = p.Motor;
                        existing.Yil = p.Yil;
                        existing.Birim = p.Birim;
                        existing.ParaBirimi = p.ParaBirimi;
                        existing.Fiyat = p.Fiyat;
                        existing.Stok = stockVal;
                        existing.ModifiedDate = now;
                        
                        toUpdate.Add(existing);
                    }
                    else
                    {
                        // Insert
                        var newEntity = new ProductBasbug
                        {
                            No = p.No,
                            Aciklama1 = p.Aciklama1,
                            Aciklama2 = p.Aciklama2,
                            MarkaKod = p.MarkaKod,
                            OemKod = p.OemKod,
                            Uretici = p.Uretici,
                            GrupKod = p.GrupKod,
                            Model = p.Model,
                            Motor = p.Motor,
                            Yil = p.Yil,
                            Birim = p.Birim,
                            ParaBirimi = p.ParaBirimi,
                            Fiyat = p.Fiyat,
                            Stok = stockVal,
                            CreatedDate = now,
                            ModifiedDate = now
                        };
                        toInsert.Add(newEntity);
                    }
                }

                _logger.LogInformation("{Grup} grubu için {Insert} yeni, {Update} güncellenecek.", group.Kod, toInsert.Count, toUpdate.Count);

                if (toInsert.Any())
                {
                    await _context.DbContext.BulkInsertAsync(toInsert, new BulkConfig { SetOutputIdentity = true });
                }
                
                if (toUpdate.Any())
                {
                    await _context.DbContext.BulkUpdateAsync(toUpdate);
                }
                
                // SaveChanges is strictly not needed for Bulk ops but good for consistency with other services if they rely on it?
                // But Basbug didn't have it inside the loop before, only at end? No, inside loop.
                // Basbug code: await _context.DbContext.BulkInsertOrUpdateAsync(entities, bulkConfig);
                // It did NOT call SaveChangesAsync inside the loop.
                // But BulkUpdate executes immediately. So it's fine.
            }

            _logger.LogInformation("Stok güncelleme tamamlandı.");
            Console.WriteLine("Stok güncelleme tamamlandı.");
            
            // Update Seller SyncDate and SyncMessage
            // Update Seller SyncDate and SyncMessage (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(2);
                    if (seller != null)
                    {
                        seller.SyncDate = DateTime.Now;
                        seller.SyncMessage = "✅ Başarılı: Stok güncelleme tamamlandı";
                        await scopedContext.SaveChangesAsync();
                        Console.WriteLine($"✅ Seller (Id=2) SyncDate güncellendi: {seller.SyncDate}");
                        _logger.LogInformation($"Seller (Id=2) SyncDate güncellendi: {seller.SyncDate}");
                    } else {
                        Console.WriteLine($"⚠️ Seller (Id=2) BULUNAMADI!");
                        _logger.LogWarning($"Seller (Id=2) BULUNAMADI!");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"❌ Seller Güncelleme Hatası: {ex.Message}");
                _logger.LogError(ex, "Seller Güncelleme Hatası");
            }
        } catch(Exception e){
            _logger.LogError(e, "Basbug ürün aktarımı sırasında hata oluştu.");
            Console.WriteLine($"Hata: {e.Message}");
            
            // Update Seller with error message
            // Update Seller with error message (Fresh Scope)
            try {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var seller = await scopedContext.GetRepository<Seller>().FindAsync(2);
                    if (seller != null)
                    {
                        seller.SyncDate = DateTime.Now;
                        seller.SyncMessage = $"❌ Hata: {e.Message}";
                        await scopedContext.SaveChangesAsync();
                    }
                }
            } catch { /* ignore error during error logging */ }
            throw;
        }
    }

}
