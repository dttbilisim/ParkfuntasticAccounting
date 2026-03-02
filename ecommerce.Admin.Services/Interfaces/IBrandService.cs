using ecommerce.Admin.Domain.Dtos.BrandDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IBrandService
    {
        public Task<IActionResult<Paging<List<BrandListDto>>>> GetBrands(PageSetting pager);
        public Task<IActionResult<List<BrandListDto>>> GetBrands();

        Task<IActionResult<Empty>> UpsertBrand(AuditWrapDto<BrandUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBrand(AuditWrapDto<BrandDeleteDto> model);
        Task<IActionResult<BrandUpsertDto>> GetBrandById(int Id);
    }
}
