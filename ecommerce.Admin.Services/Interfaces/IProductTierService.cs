using ecommerce.Admin.Domain.Dtos.ProductTierDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductTierService
    {
        public Task<IActionResult<Paging<IQueryable<ProductTierListDto>>>> GetProductTiers(PageSetting pager);
        public Task<IActionResult<List<ProductTierListDto>>> GetProductTiers();
        public Task<IActionResult<List<ProductTierListDto>>> GetProductTiers(int productId);
        Task<IActionResult<Empty>> UpsertProductTier(AuditWrapDto<ProductTierUpsertDto> model);
        Task<IActionResult<Empty>> DeleteProductTier(AuditWrapDto<ProductTierDeleteDto> model);
        Task<IActionResult<ProductTierUpsertDto>> GetProductTierById(int Id);
    }
}
