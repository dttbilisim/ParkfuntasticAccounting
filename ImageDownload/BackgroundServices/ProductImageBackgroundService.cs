using System.Data.Entity;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ImageDownload.Abstract;
using Microsoft.Extensions.Logging;
namespace ImageDownload.BackgroundServices;
public class ProductImageBackgroundService : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IGoogleImageFetcher _imageFetcher;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<ProductImageBackgroundService> _logger;

    public ProductImageBackgroundService(
        IUnitOfWork<ApplicationDbContext> context,
        IGoogleImageFetcher imageFetcher,
        IImageStorageService imageStorageService,
        ILogger<ProductImageBackgroundService> logger)
    {
        _context = context;
        _imageFetcher = imageFetcher;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            var productsWithoutImage = await _context.DbContext.ProductBasbugs
                .Where(p => !_context.DbContext.ProductImages.Any(pi => pi.ProductId == p.Id))
                .Take(50)
                .ToListAsync();

            foreach (var product in productsWithoutImage)
            {
                try
                {
                    var query = product.OemKod ?? product.Id.ToString() ?? product.Id.ToString();
                    var imageUrl = await _imageFetcher.GetFirstImageUrlAsync(query);
                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        _logger.LogInformation("No image found for product {ProductId}", product.Id);
                        continue;
                    }

                    var fileName = $"product_{product.Id}";
                    var savedPath = await _imageStorageService.SaveImageAsync(imageUrl, fileName);

                    if (savedPath is null)
                    {
                        _logger.LogWarning("Failed to save image for product {ProductId}", product.Id);
                        continue;
                    }

                    _context.DbContext.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Root = savedPath,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        ModifiedId = 1,
                        Status = 1
                    });

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Image saved for product {ProductId} => {Path}", product.Id, savedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product {ProductId}", product.Id);
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProductImageBackgroundService] Critical failure during execution.");
        }
    }
}
