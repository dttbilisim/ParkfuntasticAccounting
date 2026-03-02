using ecommerce.Domain.Shared.Abstract;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IRealTimeStockResolver
    {
        IRealTimeStockProvider? GetProvider(int sellerId);
    }
}
