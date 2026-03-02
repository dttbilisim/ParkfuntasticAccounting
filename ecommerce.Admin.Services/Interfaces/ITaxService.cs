using ecommerce.Admin.Domain.Dtos.TaxDto;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ITaxService
    {
        public Task<IActionResult<List<TaxListDto>>> GetTaxes();

    }
}
