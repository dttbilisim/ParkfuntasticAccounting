using AutoMapper;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto
{
    [AutoMap(typeof(Discount), ReverseMap = true)]
    public class DiscountUpsertDto
    {
        public int? Id { get; set; }

        public DiscountType? DiscountType { get; set; }

        public bool RequiresCouponCode { get; set; }

        public string? CouponCode { get; set; }

        public string Name { get; set; } = null!;

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

        public bool HasGiftProducts { get; set; }

        public bool UseSingleGiftProduct { get; set; }

        public int? GiftProductSellerId { get; set; }

        public List<int>? GiftProductIds { get; set; }

        public DiscountLimitationType DiscountLimitation { get; set; }

        public int LimitationTimes { get; set; }

        public int Status { get; set; }

        public RuleUpsertDto? Rule { get; set; }

        public List<DiscountCompanyCouponUpsertDto> CompanyCoupons { get; set; } = new();
    }
}