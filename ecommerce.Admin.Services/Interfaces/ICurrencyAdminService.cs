using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface ICurrencyAdminService
{
    Task<IActionResult<Paging<IQueryable<CurrencyListDto>>>> GetCurrencies(PageSetting pager);
    Task<IActionResult<List<CurrencyListDto>>> GetCurrencies();
    Task<IActionResult<CurrencyUpsertDto>> GetCurrencyById(int id);
    Task<IActionResult<Empty>> UpsertCurrency(AuditWrapDto<CurrencyUpsertDto> model);
    Task<IActionResult<Empty>> DeleteCurrency(AuditWrapDto<CurrencyDeleteDto> model);
    Task<IActionResult<Empty>> RefreshCurrenciesFromCurrencyData(int userId);
}



