using ecommerce.Admin.Domain.Dtos.SupportLineDto;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface ISupportLineService
{
    Task<IActionResult<Paging<List<SupportLineListDto>>>> GetSupportLines(PageSetting pager);

    Task<IActionResult<List<SupportLineListDto>>> GetSupportLines();

    Task<IActionResult<SupportLineUpsertDto>> GetSupportLineById(int Id);

    Task<IActionResult<Empty>> UpsertSupportLine(SupportLineUpsertDto dto);

    Task<IActionResult<Empty>> DeleteSupportLine(SupportLineDeleteDto dto);
}