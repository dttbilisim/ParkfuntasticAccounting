using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.SupportLine;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface  ICommonManager
{
    Task<IActionResult<List<CityListDto>>> GetCategoryList();
    Task<IActionResult<List<TownListDto>>> GetTownList();
    Task<IActionResult<List<BannerItem>>> GetBannerListAsync();
    Task<IActionResult<bool>> BannerCount(BannerCountDto model);


    Task<IActionResult<bool>> SubmitSupportLineAsync(SupportLine dto);
    Task<IActionResult<List<FrequentlyAskedQuestion>>> GetFrequentlyAskedQuestions();
    Task<IActionResult<List<CarBrand>>> GetCarBrandsAsync();
    Task<IActionResult<List<CarModel>>> GetCarModelsByBrandIdAsync(int brandId);
    Task<IActionResult<List<CarEngine>>> GetCarEnginesByModelIdAsync(int modelId);
    Task<IActionResult<List<CarFuelType>>> GetCarFuelTypesAsync();
    Task<IActionResult<List<CarYear>>> GetCarYearsAsync();
}