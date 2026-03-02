namespace ecommerce.Admin.Domain.Dtos.InvoiceDto
{
    public class InvoiceUpsertDto
    {
        public int? Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Fatura numarası zorunludur")]
        public string InvoiceNo { get; set; } = null!;
        public int? InvoiceTypeId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public int? CustomerId { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Cari adı boş olamaz")]
        public string? CustomerName { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Seri no zorunludur")]
        public string? InvoiceSerialNo { get; set; }
        public string? Warehouse { get; set; }
        public int? WarehouseId { get; set; }
        public string? DocumentType { get; set; }
        public int? PaymentTypeId { get; set; } // Ödeme Tipi
        public int? CashRegisterId { get; set; } // Kasa
        public int? PcPosDefinitionId { get; set; } // POS
        public int? SalesPersonId { get; set; } // Plasiyer
        public string? CurrencyCode { get; set; } // Döviz Tipi string (opsiyonel/gösterimlik)
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Döviz tipi zorunludur")]
        public int? CurrencyId { get; set; } // Döviz ID (Birincil referans)
        public bool IsVatIncluded { get; set; } = true; // KDV Dahil
        public bool IsActive { get; set; } = true; // Aktif Pasif
        public bool IsEInvoice { get; set; }
        public bool IsEArchive { get; set; }
        public bool IsCashSale { get; set; }
        public bool UseCustomerLastInvoiceAddress { get; set; }
        public decimal RiskLimit { get; set; }
        public string? RiskLimitText { get; set; } // Risk Limiti text area
        public decimal CurrentBalance { get; set; }
        public decimal LastServiceTotal { get; set; }
        public int AverageMaturity { get; set; } // Ortalama Vade
        public decimal ExchangeRate { get; set; } // Döviz Kuru
        public decimal Discount1 { get; set; }
        public decimal Discount2 { get; set; }
        public decimal Discount3 { get; set; }
        public decimal Discount4 { get; set; }
        public decimal Discount5 { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal GeneralTotal { get; set; }
        public decimal TotalAmountCurrency { get; set; }
        public decimal DiscountTotalCurrency { get; set; }
        public decimal VatTotalCurrency { get; set; }
        public decimal GeneralTotalCurrency { get; set; }
        public string? Description { get; set; }
        
        /// <summary>
        /// Odaksoft ETTN (Elektronik Tablo Takip Numarası)
        /// </summary>
        public string? Ettn { get; set; }
        
        /// <summary>
        /// e-Fatura gönderim durumu (Taslak, Gönderildi, Onaylandı, Reddedildi, İptal)
        /// </summary>
        public string? EInvoiceStatus { get; set; }
        
        public int? OrderId { get; set; } // Hangi siparişten oluştuğunu tutmak için
        public List<int>? OrderIds { get; set; } // Çoklu siparişten oluştuysa listesi
        public string? OrderNumber { get; set; } // Sipariş numarası (Görüntüleme için)
        public string? InvoiceTypeName { get; set; } // Fatura Tipi (Görüntüleme için)
        public int CorporationId { get; set; }
        public int BranchId { get; set; }
        public List<InvoiceItemUpsertDto> Items { get; set; } = new();
    }
}
