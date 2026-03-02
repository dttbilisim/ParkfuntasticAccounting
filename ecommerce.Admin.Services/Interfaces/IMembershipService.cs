using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
	public interface IMembershipService
	{
        Task<IActionResult<Paging<IQueryable<MembershipListDto>>>> GetMembership(PageSetting pager);
        Task<IActionResult<List<MembershipListDto>>> GetMembership();
        Task<IActionResult<Empty>> UpsertMembership(AuditWrapDto<MembershipUpsertDto> model);
        Task<IActionResult<Empty>> DeleteMembership(AuditWrapDto<MembershipDeleteDto> model);
        Task<IActionResult<MembershipUpsertDto>> GetMembershipById(int Id);
        Task<IActionResult<List<CityListDto>>> GetCityList();
        Task<IActionResult<List<TownListDto>>> GetTownListGetById(int Id);
        Task<IActionResult<Empty>> UpsertAspnetUser(ApplicationUser model);
        Task<IActionResult<MembershipActivation>> GetUserToken(int membershipId);
    }
}

