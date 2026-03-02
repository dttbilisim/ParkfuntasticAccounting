using ecommerce.Admin.Domain.Dtos.EditorialContentDto;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface IEditorialContentService
{
    Task<IActionResult<Paging<List<EditorialContentListDto>>>> GetEditorialContents(PageSetting pager);

    Task<IActionResult<List<EditorialContentListDto>>> GetEditorialContents();

    Task<IActionResult<EditorialContentUpsertDto>> GetEditorialContentById(int id);

    Task<IActionResult<Empty>> UpsertEditorialContent(EditorialContentUpsertDto dto);

    Task<IActionResult<Empty>> DeleteEditorialContent(EditorialContentDeleteDto dto);
}