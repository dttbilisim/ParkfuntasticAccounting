using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.OrderDto;

public class OrderStatusUpdateDto
{
    public int Id { get; set; }

    public OrderStatusType OrderStatusType { get; set; }
}