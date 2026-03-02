using ecommerce.Admin.Domain.Dtos.ProductTypeDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductTypeService
    {
        Task<IActionResult<Paging<IQueryable<ProductTypeListDto>>>> GetProductTypes(PageSetting pager);
        Task<IActionResult<List<ProductTypeListDto>>> GetProductTypes();
        Task<IActionResult<Empty>> UpsertProductType(AuditWrapDto<ProductTypeUpsertDto> model);
        Task<IActionResult<Empty>> DeleteProductType(AuditWrapDto<ProductTypeDeleteDto> model);
        Task<IActionResult<ProductTypeUpsertDto>> GetProductTypeById(int Id);
    }
}
