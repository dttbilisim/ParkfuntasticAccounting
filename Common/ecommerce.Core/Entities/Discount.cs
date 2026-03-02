using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class Discount : AuditableEntity<int>
    {
        public DiscountType DiscountType { get; set; }
        public int? BranchId { get; set; }

        public bool RequiresCouponCode { get; set; }

        public string? CouponCode { get; set; }

        public string Name { get; set; } = null!;

        //urun
        public List<int>? AssignedEntityIds { get; set; }
        public List<int> ? AssignedSellerIds{get;set;}

        public bool UsePercentage { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public decimal? MaximumDiscountAmount { get; set; }

        public bool IsCumulative { get; set; }

        public string? Description { get; set; }

        public string? CouponDescription { get; set; }

        public string? ImagePath { get; set; }

        public string? CampaignLink { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DiscountLimitationType DiscountLimitation { get; set; }

        public int LimitationTimes { get; set; }

        public bool HasGiftProducts { get; set; }

        public bool UseSingleGiftProduct { get; set; }

        public int? GiftProductSellerId { get; set; }

        public List<int>? GiftProductIds { get; set; }

        public Rule? Rule { get; set; }

        public ICollection<OrderAppliedDiscount> UsedOrders { get; set; } = new List<OrderAppliedDiscount>();

        [JsonIgnore]
        public ICollection<DiscountCompanyCoupon> CompanyCoupons { get; set; } = new List<DiscountCompanyCoupon>();

        [NotMapped]
        public List<int>? AssignedSellerItemIds { get; set; }
    }
}