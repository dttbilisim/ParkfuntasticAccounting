using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICityService
    {
        public Task<IActionResult<List<CityListDto>>> GetCities();

    }
}
