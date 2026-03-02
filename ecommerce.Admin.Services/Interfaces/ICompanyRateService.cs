using ecommerce.Admin.Domain.Dtos.CompanyRateDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICompanyRateService
    {
        public Task<IActionResult<List<CompanyRateListDto>>> GetCompanyRateByCompanyId(int companyId);
        public Task<IActionResult<List<CompanyRateListDto>>> GetCompanyRateByProductId(int productId);
        Task<IActionResult<Empty>> UpsertCompanyRate(AuditWrapDto<CompanyRateUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCompanyRate(AuditWrapDto<CompanyRateDeleteDto> model);
        Task<IActionResult<CompanyRateUpsertDto>> GetCompanyRateById(int Id);
    }
}
