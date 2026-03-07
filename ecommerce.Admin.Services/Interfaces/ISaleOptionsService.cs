using ecommerce.Admin.Domain.Dtos.SaleOptionsDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface ISaleOptionsService
{
    Task<IActionResult<Paging<IQueryable<SaleOptionsListDto>>>> GetSaleOptions(PageSetting pager);
    Task<IActionResult<SaleOptionsUpsertDto>> GetSaleOptionsById(int id);
    Task<IActionResult<Empty>> UpsertSaleOptions(AuditWrapDto<SaleOptionsUpsertDto> model);
    Task<IActionResult<Empty>> DeleteSaleOptions(AuditWrapDto<SaleOptionsDeleteDto> model);
}
