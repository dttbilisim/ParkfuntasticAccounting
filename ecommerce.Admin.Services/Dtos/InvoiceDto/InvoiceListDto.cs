namespace ecommerce.Admin.Domain.Dtos.InvoiceDto
{
    public class InvoiceListDto
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = null!;
        public string? InvoiceSerialNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceTypeName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VatTotal { get; set; }
        public decimal GeneralTotal { get; set; }
        public decimal TotalAmountCurrency { get; set; }
        public decimal VatTotalCurrency { get; set; }
        public decimal GeneralTotalCurrency { get; set; }
        public bool IsEInvoice { get; set; }
        public bool IsEArchive { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? OrderId { get; set; } // Hangi siparişten oluştuğunu tutmak için
        public string? OrderNumber { get; set; } // Sipariş numarası (navigation property'den)
        
        /// <summary>
        /// Faturaya bağlı tüm siparişler (birden fazla sipariş aynı faturaya bağlanabilir)
        /// </summary>
        public List<InvoiceOrderLinkDto> LinkedOrders { get; set; } = new();
        
        public string? Ettn { get; set; } // Odaksoft ETTN
        public string? EInvoiceStatus { get; set; } // e-Fatura gönderim durumu
    }

    /// <summary>
    /// Faturaya bağlı sipariş bilgisi (link için)
    /// </summary>
    public class InvoiceOrderLinkDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
    }
}
