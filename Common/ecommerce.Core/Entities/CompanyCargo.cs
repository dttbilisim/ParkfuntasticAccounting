using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class CompanyCargo : AuditableEntity<int>
    {
        [ForeignKey(nameof(SellerId))]
        public Seller Seller { get; set; }
        public int SellerId { get; set; }
        
        [ForeignKey(nameof(CargoId))]
        public Cargo Cargo { get; set; }
        public int CargoId { get; set; }

        public decimal MinBasketAmount { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }
}

