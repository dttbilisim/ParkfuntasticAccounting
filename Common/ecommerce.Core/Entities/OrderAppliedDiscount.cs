using System.Text.Json.Serialization;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities;

public class OrderAppliedDiscount : IEntity
{
    public int OrderId { get; set; }
 
    public int? OrderItemId { get; set; }
   
    public int DiscountId { get; set; }

    public string? CouponCode { get; set; }

    public int? CompanyCouponId { get; set; }

    public DateTime CreatedDate { get; set; }
   [JsonIgnore]
    public Orders Order { get; set; } = null!;
    [JsonIgnore]
    public OrderItems? OrderItem { get; set; }

    public Discount Discount { get; set; } = null!;

    public DiscountCompanyCoupon? CompanyCoupon { get; set; }
}