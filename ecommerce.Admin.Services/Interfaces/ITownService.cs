using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ITownService
    {
        public Task<IActionResult<List<TownListDto>>> GetTownsByCityId(int CityId);

    }
}
