using ecommerce.Admin.Domain.Dtos.PopupDto;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface IPopupService
{
    Task<IActionResult<Paging<List<PopupListDto>>>> GetPopups(PageSetting pager);

    Task<IActionResult<List<PopupListDto>>> GetPopups();

    Task<IActionResult<PopupUpsertDto>> GetPopupById(int Id);

    Task<IActionResult<Empty>> UpsertPopup(PopupUpsertDto dto);

    Task<IActionResult<Empty>> DeletePopup(PopupDeleteDto dto);
}