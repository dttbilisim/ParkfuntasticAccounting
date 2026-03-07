using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    /// <summary>PcPos transfer: tSaleOptions</summary>
    public class SaleOptions : AuditableEntity<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;
    }
}
