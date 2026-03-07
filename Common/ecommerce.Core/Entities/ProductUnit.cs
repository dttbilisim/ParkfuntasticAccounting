using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    public class ProductUnit : AuditableEntity<int>
    {
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        public int UnitId { get; set; }
        [ForeignKey(nameof(UnitId))]
        public Unit Unit { get; set; } = null!;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public decimal UnitValue { get; set; } = 1; // Birim dönüşüm değeri (örn: 1 koli = 12 adet)
        public bool IsDefault { get; set; }
        public int BranchId { get; set; }
        /// <summary>PcPos transfer: Şirket kodu</summary>
        [MaxLength(50)]
        public string? CompanyCode { get; set; }
    }
}
