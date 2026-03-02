using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Domain.Shared.Abstract;

namespace ecommerce.Admin.Domain.Concreate
{
    public class RealTimeStockResolver(IEnumerable<IRealTimeStockProvider> providers) : ecommerce.Admin.Domain.Interfaces.IRealTimeStockResolver
    {
        public IRealTimeStockProvider? GetProvider(int sellerId)
        {
            return providers.FirstOrDefault(x => x.SellerId == sellerId);
        }
    }
}
