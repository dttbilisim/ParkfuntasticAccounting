using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IUserBranchService
    {
        Task<IActionResult<List<UserBranchListDto>>> GetUserBranches(int userId);
        Task<IActionResult<Empty>> UpsertUserBranches(int userId, List<UserBranchUpsertDto> branches);
        Task<IActionResult<Empty>> DeleteUserBranch(int id);
    }
}
