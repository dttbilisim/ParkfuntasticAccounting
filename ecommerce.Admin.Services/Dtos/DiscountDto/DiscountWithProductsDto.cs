using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Dtos.Product;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto
{
    public class DiscountWithProductsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string? CampaignLink { get; set; }
        public DiscountType DiscountType { get; set; }
        public bool UsePercentage { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int>? AssignedEntityIds { get; set; }
        public List<SellerProductViewModel> Products { get; set; } = new();
    }
}
