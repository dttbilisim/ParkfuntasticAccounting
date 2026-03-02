using ecommerce.Admin.Domain.Dtos.OrderItemDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IOrderItemService
    {
        public Task<IActionResult<List<OrderItemListDto>>> GetOrderItems();
        Task<IActionResult<Empty>> DeleteOrderItem(AuditWrapDto<OrderItemDeleteDto> model);
        Task<IActionResult<OrderItemUpsertDto>> GetOrderItemById(int Id);
    }
}
