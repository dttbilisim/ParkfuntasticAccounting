using ecommerce.Admin.Domain.Dtos.EducationDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IEducationService{
   
    
    //education
    public Task<IActionResult<List<EducationListDto>>> GetEducation();

    public Task<IActionResult<Paging<List<EducationListDto>>>> GetEducations(PageSetting pager);

    Task<IActionResult<Empty>> UpsertEducation(AuditWrapDto<EducationUpsertDto> model);

    Task<IActionResult<Empty>> DeleteEducation(EducationDeleteDto dto);

    Task<IActionResult<EducationUpsertDto>> GetEducationId(int Id);

    Task<int> GetEducationLastCount();
   
    
    //education category
    public Task<IActionResult<List<EducationCategoryListDto>>> GetTreeCategories();
    public Task<IActionResult<List<EducationCategoryListDto>>> GetCategoriesByEducationCategoryType(EducationCategoryType educationCategoryType);
    public Task<IActionResult<Paging<IQueryable<EducationCategoryListDto>>>> GetCategories(PageSetting pager);
    
   public Task<IActionResult<Empty>> UpsertCategory(AuditWrapDto<EducationCategoryUpsertDto> model);
   public Task<IActionResult<Empty>> DeleteCategory(AuditWrapDto<EducationCategoryDeleteDto> model);
   public Task<IActionResult<EducationCategoryUpsertDto>> GetCategoryById(int categoryId);



    // Edication Items

    public Task<IActionResult<List<EducationItemsListDto>>> GetEducationItem();

    public Task<IActionResult<Paging<List<EducationItemsListDto>>>> GetEducationItems(PageSetting pager);

    Task<IActionResult<Empty>> UpsertEducationItem(AuditWrapDto<EducationItemsUpsertDto> model);

    Task<IActionResult<Empty>> DeleteEducationItem(EducationItemsDeleteDto dto);

    Task<IActionResult<EducationItemsUpsertDto>> GetEducationItemId(int Id);
    
    Task<int> GetEducationItemLastCount();
    
    
    // EdicationItems Images
    
    public Task<IActionResult<List<EducationImagesListDto>>> GetEducationImage();

    public Task<IActionResult<Paging<List<EducationImagesListDto>>>> GetEducationImages(PageSetting pager);

    Task<IActionResult<Empty>> UpsertEducationItemImage(AuditWrapDto<EducationImagesUpsertDto> model, AuditWrapDto<EducationItemsUpsertDto> educationItemsUpsertDto);

    Task<IActionResult<Empty>> DeleteEducationItemImage(EducationImagesDeleteDto dto);

    Task<IActionResult<List<EducationImagesListDto>>> GetEducationItemImageId(int Id);
    Task<int> GetEducationItemImageLastCount();
    
    
    
    
    
    
    
}
