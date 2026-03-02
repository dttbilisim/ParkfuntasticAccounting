using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.BackgroundServices;
public class IntegrationProductBackgroundService(IUnitOfWork<ApplicationDbContext> context,IDapperService dapperService, ILogger<IntegrationProductBackgroundService> logger): IAsyncBackgroundJob{

    public async Task ExecuteAsync(){
        
        try
        {
          
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<Product>> OtoIsmail()
    {
        var rs = await context.DbContext.ProductOtoIsmails.Where(x => x.StokSayisi > 0).ToListAsync();
        var result = new List<Product>();

        foreach (var item in rs)
        {
            var product = new Product
            {
                Name = item.Ad,
                Barcode = string.Join(",", new[] { item.Barkod1, item.Barkod2, item.Barkod3 }
                                      .Where(b => !string.IsNullOrWhiteSpace(b))),
                Description = item.Ad,
                WebKeyword = string.Join(",", new[] { item.Ad, item.Kod, item.OrjinalKod, item.Marka, item.GrupKodu, item.Oem }
                                      .Where(b => !string.IsNullOrWhiteSpace(b))),
                Height = 0,
                Length = 0,
                Price = (decimal)item.Fiyat1,
                // Removed: SellerId (moved to SellerItems), AdvertCount, AvgPrice
                Width = 0,
                CargoDesi = 0
            };

            result.Add(product);
        }

        return result;
    }
}
