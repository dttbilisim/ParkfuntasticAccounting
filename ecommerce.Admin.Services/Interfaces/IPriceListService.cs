using ecommerce.Admin.Domain.Dtos.PriceListDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IPriceListService
{
    Task<IActionResult<Paging<IQueryable<PriceListListDto>>>> GetPriceLists(PageSetting pager);
    Task<IActionResult<PriceListUpsertDto>> GetPriceListById(int id);
    Task<IActionResult<Empty>> UpsertPriceList(AuditWrapDto<PriceListUpsertDto> model);
    Task<IActionResult<Empty>> DeletePriceList(AuditWrapDto<PriceListDeleteDto> model);
}



