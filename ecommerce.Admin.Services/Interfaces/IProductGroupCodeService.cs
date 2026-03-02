using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IProductGroupCodeService{
    public Task<IActionResult<List<ProductGroupCodeListDto>>> GetProductGroupCodes(int productId);
    Task<IActionResult<Empty>> DeleteProductGroupCode(AuditWrapDto<ProductGroupCodeDeleteDto> model);
    Task<IActionResult<Empty>> UpsertProductGroupCde(AuditWrapDto<ProductGroupCodeUpsertDto> model);
}
