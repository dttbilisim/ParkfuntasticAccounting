using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IProductUnitService
{
    Task<IActionResult<Paging<IQueryable<ProductUnitListDto>>>> GetProductUnits(PageSetting pager);
    Task<IActionResult<List<ProductUnitListDto>>> GetProductUnitsByProductId(int productId);
    Task<IActionResult<List<ProductUnitListDto>>> GetProductUnitsByProductIds(List<int> productIds);
    Task<IActionResult<ProductUnitUpsertDto>> GetProductUnitById(int id);
    Task<IActionResult<Empty>> UpsertProductUnit(AuditWrapDto<ProductUnitUpsertDto> model);
    Task<IActionResult<Empty>> DeleteProductUnit(AuditWrapDto<ProductUnitDeleteDto> model);
}
