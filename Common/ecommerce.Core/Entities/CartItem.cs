using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
using Microsoft.AspNetCore.Identity;

namespace ecommerce.Core.Entities
{
    public class CartItem : AuditableEntity<int>
    {
        public int UserId { get; set; }
      
        public int ProductId { get; set; }

        public int ProductSellerItemId { get; set; }

        public int Quantity { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        // Web context için User (UserId FK ile)
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
        
        // Admin context için ApplicationUser (UserId FK ile)
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? ApplicationUser { get; set; }
        
        // Helper property: Context'e göre doğru user'ı döner
        [NotMapped]
        public IdentityUser<int>? CurrentUser => ApplicationUser ?? (IdentityUser<int>?)User;
        
        [ForeignKey(nameof(ProductSellerItemId))]
        public SellerItem ProductSellerItem { get; set; } = null!;

        [NotMapped]
        public int? CommissionRateId { get; set; }

        [NotMapped]
        public int? CommissionRatePercent { get; set; }

        [NotMapped]
        public decimal CommissionTotal { get; set; }

        [NotMapped]
        public int? SubmerhantCommisionRate { get; set; }

        [NotMapped]
        public decimal SubmerhantCommision { get; set; }

        [NotMapped]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal UnitPriceWithoutDiscount { get; set; }

        [NotMapped]
        public decimal Total { get; set; }

        [NotMapped]
        public decimal TotalWithoutDiscount { get; set; }

        [NotMapped]
        public decimal DiscountAmount { get; set; }

        [NotMapped]
        public decimal DisplaySavings { get; set; }

        [NotMapped]
        public decimal ProductDesi { get; set; }

        [NotMapped]
        public int Stock => (int)(ProductSellerItem?.Stock ?? 0);

        [NotMapped]
        public List<string> Warnings { get; set; } = new();

        [NotMapped]
        public List<Discount> AppliedDiscounts { get; set; } = new();

        [NotMapped]
        public bool IsReadonly { get; set; }

        [NotMapped]
        public bool IsGiftProduct { get; set; }

        [NotMapped]
        public bool CanGiftRemove { get; set; }

        [NotMapped]
        public string? PictureUrl { get; set; }

        [NotMapped]
        public bool IsPerfectCompatibility { get; set; }

        [NotMapped]
        public List<string> PerfectCompatibilitySummaries { get; set; } = new();
    }
}