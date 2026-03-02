using ecommerce.Domain.Shared.Abstract;

namespace ecommerce.Domain.Shared.Abstract
{
    public interface IRealTimeStockResolver
    {
        IRealTimeStockProvider? GetProvider(int sellerId);
    }
}
