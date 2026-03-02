namespace ecommerce.Domain.Shared.Abstract;

public interface IRealTimeStockProvider
{
    int SellerId { get; }
    Task<string> GetStockAsync(string productCode, string? sourceId = null);
}
