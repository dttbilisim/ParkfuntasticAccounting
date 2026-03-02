using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICompanyCargoService
    {
        public Task<IActionResult<Paging<IQueryable<CompanyCargoListDto>>>> GetCompanyCargoes(PageSetting pager,int sellerId);
        public Task<IActionResult<List<CompanyCargoListDto>>> GetCompanyCargoes(int sellerId);
        Task<IActionResult<Empty>> UpsertCompanyCargo(AuditWrapDto<CompanyCargoUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCompanyCargo(AuditWrapDto<CompanyCargoDeleteDto> model);
      
        Task<IActionResult<CompanyCargoUpsertDto>> GetCompanyCargoById(int Id);
    }
}
