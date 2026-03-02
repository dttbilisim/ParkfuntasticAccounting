using ecommerce.Domain.Shared.Abstract;
using Microsoft.Extensions.Logging;

namespace ecommerce.Domain.Shared.Services;

public class BasbugStockService : IRealTimeStockProvider
{
    private readonly ILogger<BasbugStockService> _logger;

    public int SellerId => 2;

    public BasbugStockService(ILogger<BasbugStockService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetStockAsync(string productCode, string? sourceId = null)
    {
        try
        {
            // Basbug API grup bazlı çalıştığı için, ürün kodunu kullanarak
            // doğrudan stok sorgulamak mümkün değil.
            // Bu yüzden OrderService'de lokal DB'den stok gösterilecek
            await Task.CompletedTask;
            return "DB Kullan";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BasbugStockService GetStockAsync error for code: {ProductCode}", productCode);
            return "Hata";
        }
    }
}
