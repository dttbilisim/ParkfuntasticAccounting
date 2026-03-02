using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IBranchService
    {
        Task<IActionResult<Paging<IQueryable<BranchListDto>>>> GetBranches(PageSetting pager);
        Task<IActionResult<List<BranchListDto>>> GetBranchesByCorporationId(int corporationId);
        Task<IActionResult<BranchUpsertDto>> GetBranchById(int id);
        Task<IActionResult<List<BranchListDto>>> GetAllActiveBranches();
        Task<IActionResult<Empty>> UpsertBranch(AuditWrapDto<BranchUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBranch(int id, int userId);
    }
}
