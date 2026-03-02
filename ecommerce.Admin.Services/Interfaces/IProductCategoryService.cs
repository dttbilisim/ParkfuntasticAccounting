using ecommerce.Admin.Domain.Dtos.ProductCategory;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductCategoryService
    {
        public Task<IActionResult<List<ProductCategoryListDto>>> GetProductCategories(int productId);
        Task<IActionResult<Empty>> UpsertProductCategory(AuditWrapDto<ProductCategoryUpsertDto> model);
        Task<IActionResult<Empty>> DeleteProductCategory(AuditWrapDto<ProductCategoryDeleteDto> model);
        Task<IActionResult<ProductCategoryUpsertDto>> GetProductCategoryById(int Id);
    }
}
