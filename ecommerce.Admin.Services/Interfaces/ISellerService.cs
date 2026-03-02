using ecommerce.Admin.Domain.Dtos.SellerDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ISellerService
    {
        Task<IActionResult<Paging<List<SellerListDto>>>> GetSellers(PageSetting pager);
        Task<IActionResult<SellerUpsertDto>> GetSellerById(int id);
        Task<IActionResult<int>> UpsertSeller(AuditWrapDto<SellerUpsertDto> model);
        Task<IActionResult<Empty>> DeleteSeller(AuditWrapDto<SellerDeleteDto> model);
        Task<List<int>> GetAllSellerIds();
    }
}

