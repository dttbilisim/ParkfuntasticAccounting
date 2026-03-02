using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Accounting
{
    public class Invoice : AuditableEntity<int>, ITenantEntity
    {
        public int BranchId { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNo { get; set; } = null!;

        public int InvoiceTypeId { get; set; }
        [ForeignKey(nameof(InvoiceTypeId))]
        public InvoiceTypeDefinition InvoiceType { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; }

        [MaxLength(100)]
        public string CustomerName { get; set; }

        [MaxLength(50)]
        public string InvoiceSerialNo { get; set; }

        [MaxLength(100)]
        public string? Warehouse { get; set; }

        public int? WarehouseId { get; set; }

        [MaxLength(50)]
        public string? DocumentType { get; set; }

        public int? PaymentTypeId { get; set; } // Ödeme Tipi
        [ForeignKey(nameof(PaymentTypeId))]
        public virtual PaymentType? PaymentType { get; set; }

        public int? CashRegisterId { get; set; } // Kasa
        [ForeignKey(nameof(CashRegisterId))]
        public virtual CashRegister? CashRegister { get; set; }

        public int? PcPosDefinitionId { get; set; } // POS
        [ForeignKey(nameof(PcPosDefinitionId))]
        public virtual PcPosDefinition? PcPosDefinition { get; set; }

        public int? SalesPersonId { get; set; } // Plasiyer
        [ForeignKey(nameof(SalesPersonId))]
        public SalesPerson? SalesPerson { get; set; }

        public bool IsVatIncluded { get; set; } = true; // KDV Dahil

        public bool IsActive { get; set; } = true; // Aktif Pasif

        public bool IsEInvoice { get; set; } // e-Fatura

        public bool IsEArchive { get; set; } // e-Arşiv

        public bool IsCashSale { get; set; } // Nakit Satış


        public bool UseCustomerLastInvoiceAddress { get; set; } // Carinin Son Fatura Adresini Kullan

        public decimal RiskLimit { get; set; }

        [MaxLength(1000)]
        public string? RiskLimitText { get; set; } // Risk Limiti text area

        public decimal CurrentBalance { get; set; }

        public decimal LastServiceTotal { get; set; }

        public int AverageMaturity { get; set; } // Ortalama Vade

        public decimal ExchangeRate { get; set; } = 1; // Döviz Kuru

        public decimal Discount1 { get; set; }
        public decimal Discount2 { get; set; }
        public decimal Discount3 { get; set; }
        public decimal Discount4 { get; set; }
        public decimal Discount5 { get; set; }

        
        public decimal TotalAmountCurrency { get; set; } = 0;
        public decimal DiscountTotalCurrency { get; set; } = 0;
        public decimal VatTotalCurrency { get; set; } = 0;
        public decimal GeneralTotalCurrency { get; set; } = 0;
        
        public decimal TotalAmount { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal GeneralTotal { get; set; }
        
        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }
        public int CurrencyId { get; set; } // Varsayılan TL (1)
        

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Odaksoft ETTN (Elektronik Tablo Takip Numarası)
        /// </summary>
        [MaxLength(50)]
        public string? Ettn { get; set; }

        /// <summary>
        /// e-Fatura gönderim durumu (Taslak, Gönderildi, Onaylandı, Reddedildi)
        /// </summary>
        [MaxLength(50)]
        public string? EInvoiceStatus { get; set; }

        /// <summary>
        /// Sipariş ID (hangi siparişten oluştuğunu tutmak için)
        /// </summary>
        public int? OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Orders? Order { get; set; }

        // Navigation property for invoice items
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}
