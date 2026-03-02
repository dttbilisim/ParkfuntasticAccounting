using ecommerce.Core.Entities;

namespace ecommerce.Web.Domain.Dtos;

public class DiscountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CouponDescription { get; set; }
    public string? ImagePath { get; set; }
    public string? CampaignLink { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool RequiresCouponCode { get; set; }
    public string? CouponCode { get; set; }
    public bool UsePercentage { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool IsNotStarted => StartDate.HasValue && StartDate.Value > DateTime.UtcNow;
    public bool IsCurrentlyActive => IsActive && !IsExpired && !IsNotStarted;
}
