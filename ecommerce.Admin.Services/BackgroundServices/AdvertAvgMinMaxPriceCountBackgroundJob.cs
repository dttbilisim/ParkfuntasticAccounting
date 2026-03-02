using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Admin.Domain.BackgroundServices;
public class AdvertAvgMinMaxPriceCountBackgroundJob(IUnitOfWork<ApplicationDbContext> context, IReportService reportService) : IAsyncBackgroundJob{
    private readonly IReportService _reportService = reportService;
    private decimal avgPrice, minPrice, maxPrice;
    private int countSameProduct;
    public async Task ExecuteAsync(){
        try{
            var products = await context.DbContext.ProductSellerItems.AsTracking().Include(x=>x.Product).ToListAsync();
            foreach(var product in products){
                avgPrice = products.Where(x => x.ProductId == product.ProductId &&  x is{Status: 1, Price: > 0}).Select(x => x.Price).DefaultIfEmpty(0).Average();
                minPrice = products.Where(x => x.ProductId == product.ProductId && x is{Status: 1, Price: > 0}).Select(x => x.Price).DefaultIfEmpty(0).Min();
                maxPrice = products.Where(x => x.ProductId == product.ProductId && x is{Status: 1, Price: > 0}).Select(x => x.Price).DefaultIfEmpty(0).Max();
                countSameProduct = products.Count(x => x.ProductId == product.ProductId && x.ExprationDate==product.ExprationDate && x.Status==1);
                if(product.Price<=0 || product.Stock<=0 || product.Product.Status==0){
                    await context.DbContext.ProductSellerItems
                        .Where(x => x.ProductId == product.ProductId).ExecuteUpdateAsync(x => 
                            x.SetProperty(x => x.AvgSameProductPrice, avgPrice)
                                .SetProperty(x => x.MinSameProductPrice, minPrice)
                                .SetProperty(x => x.CountSameProduct, countSameProduct)
                                .SetProperty(x => x.MaxSameProductPrice, maxPrice)
                            .SetProperty(x => x.ModifiedDate, DateTime.Now)
                            .SetProperty(x => x.ModifiedId, 1).SetProperty(x=>x.Status,0));
                } else{
                    await context.DbContext.ProductSellerItems
                        .Where(x => x.ProductId == product.ProductId && (x.Status!=99 && x.Status!=100)).ExecuteUpdateAsync(x => 
                            x.SetProperty(x => x.AvgSameProductPrice, avgPrice)
                                .SetProperty(x => x.MinSameProductPrice, minPrice)
                                .SetProperty(x => x.CountSameProduct, countSameProduct)
                                .SetProperty(x => x.MaxSameProductPrice, maxPrice)
                            .SetProperty(x => x.ModifiedDate, DateTime.Now)
                            .SetProperty(x => x.ModifiedId, 1).SetProperty(x=>x.Status,1));
                }

                // Product aggregate stats removed - now calculated from SellerItems on-demand
                // await context.DbContext.Product.Where(x => x.Id == product.ProductId).ExecuteUpdateAsync(x => 
                //     x.SetProperty(x => x.MaxPrice, maxPrice)
                //         .SetProperty(x => x.MinPrice, minPrice)
                //         .SetProperty(x => x.AdvertCount, countSameProduct)
                //         .SetProperty(x => x.AvgPrice, avgPrice)
                //         .SetProperty(x=>x.ModifiedDate,DateTime.Now));
            }
            await context.SaveChangesAsync();
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
}
