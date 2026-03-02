using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Accounting
{
    public class InvoiceItem : AuditableEntity<int>
    {
        public int InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;

        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [MaxLength(50)]
        public string? ProductCode { get; set; } // Stok Kodu

        [MaxLength(200)]
        public string ProductName { get; set; } = null!;

        [MaxLength(50)]
        public string? Unit { get; set; }

        public int? ProductUnitId { get; set; }
        [ForeignKey(nameof(ProductUnitId))]
        public ProductUnit? ProductUnit { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal PriceCurrency { get; set; } // Döviz bazlı fiyat
        public decimal TotalCurrency { get; set; } // Döviz bazlı toplam

        public decimal VatRate { get; set; }

        public decimal Discount1 { get; set; }
        public decimal Discount2 { get; set; }
        public decimal Discount3 { get; set; }
        public decimal Discount4 { get; set; }
        public decimal Discount5 { get; set; }

        public decimal Total { get; set; }

        public int Order { get; set; }
    }
}
