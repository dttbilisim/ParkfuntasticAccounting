using ecommerce.Core.BackgroundJobs;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using Microsoft.Extensions.Logging;
using Otokoc.Abstract;
using Otokoc.Dto;

using EFCore.BulkExtensions;
using Product = Otokoc.Dto.Product;
namespace Otokoc.BackgroundServices;

public class OtokocProductBackgoundService : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IOtokocService _apiClient;
    private readonly ILogger<OtokocProductBackgoundService> _logger;

    public OtokocProductBackgoundService(
        IUnitOfWork<ApplicationDbContext> context,
        IOtokocService apiClient,
        ILogger<OtokocProductBackgoundService> logger)
    {
        _context = context;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var message1 = $"OtokocProductBackgoundService started at {DateTime.Now}";
        _logger.LogInformation(message1);
        Console.WriteLine(message1);

        var allProducts = new List<Product>();
        int pageIndex = 0;
        const int pageSize = 1000;
        bool hasMoreData;

        do
        {
            _logger.LogInformation("Fetching page {PageIndex}", pageIndex);
            Console.WriteLine($"Fetching page {pageIndex}");

            var chunk = await _apiClient.GetAllProductsByPageAsync(pageIndex, pageSize); 
            if (chunk == null || chunk.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                allProducts.AddRange(chunk);
                hasMoreData = chunk.Count == pageSize;
                pageIndex++;
            }

        } while (hasMoreData);

        if (allProducts.Count == 0)
        {
            var message2 = "No products found from Otokoc API.";
            _logger.LogInformation(message2);
            Console.WriteLine(message2);
            return;
        }

        var entityList = allProducts.Select(p => new ProductOtokoc
        {
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            BrandCode = p.BrandCode,
            BrandName = p.BrandName,
            Price = p.Price,
            Currency = p.Currency,
            StockQuantity = p.StockQuantity,
            TaxRate = p.TaxRate,
            Barcode = p.Barcode,
            OEM = p.OEM,
            GroupCode = p.OEM, // OEM field from Otokoc DTO
            GroupName = p.GroupName,
            ImagePath = p.ImageUrl,
            CreatedDate = DateTime.Now,
            Status = 1,
            CreatedId = 1
        }).ToList();

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            UpdateByProperties = new List<string> { "ProductCode" }
        };

        int barLength = 50;
        await _context.DbContext.BulkInsertOrUpdateAsync(entityList, bulkConfig, progress: processed =>
        {
            double percentage = (double)processed / entityList.Count;
            int filledLength = (int)(percentage * barLength);
            string bar = new string('█', filledLength).PadRight(barLength, '-');
            Console.Write($"\r[{bar}] {processed}/{entityList.Count} completed ({percentage:P0})");
            _logger.LogInformation("[{Bar}] {Processed}/{Total} completed ({Percentage})", bar, processed, entityList.Count, percentage * 100);
        });

        await _context.SaveChangesAsync();

        var message3 = $"{entityList.Count} products saved to the database.";
        _logger.LogInformation(message3);
        Console.WriteLine(message3);
    }
}
