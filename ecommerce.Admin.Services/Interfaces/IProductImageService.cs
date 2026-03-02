using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductImageService
    {
        public Task<IActionResult<List<ProductImageListDto>>> GetProductImages(int productId);
        public Task<IActionResult<ProductImageUpsertDto>> GetProductImage(int Id);
        public Task<IActionResult<int>> GetProductImageMaxOrderNumber(int ProductId);
        public Task<IActionResult<Empty>> UpsertProductImage(AuditWrapDto<ProductImageUpsertDto> model);
        public Task<IActionResult<Empty>> DeleteProductImage(AuditWrapDto<ProductImageDeleteDto> model);
    }
}
