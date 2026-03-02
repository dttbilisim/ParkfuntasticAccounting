using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces {
    public interface ICategoryService {
        public Task<IActionResult<Paging<IQueryable<CategoryListDto>>>> GetCategories(PageSetting pager);
        public Task<IActionResult<List<CategoryListDto>>> GetCategories();
        public Task<IActionResult<List<CategoryListDto>>> GetTreeCategories();
        Task<IActionResult<Empty>> UpsertCategory(AuditWrapDto<CategoryUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCategory(AuditWrapDto<CategoryDeleteDto> model);
        Task<IActionResult<CategoryUpsertDto>> GetCategoryById(int categoryId);
        Task<IActionResult<List<ecommerce.Core.Entities.Category>>> GetCategoryHierarchy();
        Task<IActionResult<Empty>> UpdateCategoryLocation(int id, int? parentId, int order);
    }
}
