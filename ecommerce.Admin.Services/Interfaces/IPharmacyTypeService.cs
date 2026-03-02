using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IPharmacyTypeService
    {
        public Task<IActionResult<List<PharmacyTypeListDto>>> GetPharmacyTypes();
    }
}
