using ecommerce.Admin.Domain.Dtos.SellerItemDto;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ISellerItemService
    {
        Task<IActionResult<Paging<List<SellerItemListDto>>>> GetSellerItems(PageSetting pager, int? sellerId = null);
        Task<IActionResult<SellerItemUpsertDto>> GetSellerItemById(int id);
        Task<IActionResult<SellerItem>> AddSellerItem(SellerItemUpsertDto model, int userId);
        Task<IActionResult<SellerItem>> UpdateSellerItem(SellerItemUpsertDto model, int userId);
    }
}
