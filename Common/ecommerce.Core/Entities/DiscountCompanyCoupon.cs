using System.Text.Json.Serialization;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class DiscountCompanyCoupon : Entity<int>
{
    public int DiscountId { get; set; }

    public int CompanyId { get; set; }

    public string CouponCode { get; set; } = null!;

    public bool IsUsed { get; set; }

    public DateTime CreatedDate { get; set; }

    public Discount Discount { get; set; } = null!;

    public Company Company { get; set; } = null!;
    [JsonIgnore]
    public ICollection<OrderAppliedDiscount> AppliedOrders { get; set; } = new List<OrderAppliedDiscount>();
}