using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    /// <summary>PcPos transfer: tProductSaleItems - Paket ürün bileşenleri</summary>
    public class ProductSaleItems
    {
        public int Id { get; set; }
        /// <summary>Paket ürün ID</summary>
        public int RefProductId { get; set; }
        [ForeignKey(nameof(RefProductId))]
        public virtual Product RefProduct { get; set; } = null!;
        /// <summary>Alt ürün ID</summary>
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;
        public int? CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public virtual Currency? Currency { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Price { get; set; }
    }
}
