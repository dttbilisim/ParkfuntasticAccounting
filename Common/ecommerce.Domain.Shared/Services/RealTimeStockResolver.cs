using ecommerce.Domain.Shared.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace ecommerce.Domain.Shared.Services
{
    public class RealTimeStockResolver(IEnumerable<IRealTimeStockProvider> providers) : IRealTimeStockResolver
    {
        public IRealTimeStockProvider? GetProvider(int sellerId)
        {
            return providers.FirstOrDefault(x => x.SellerId == sellerId);
        }
    }
}
