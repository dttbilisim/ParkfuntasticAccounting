namespace ecommerce.Admin.Domain.Dtos.InvoiceDto
{
    public class InvoiceItemUpsertDto
    {
        public int? Id { get; set; }
        public int? ProductId { get; set; }
        public string? ProductCode { get; set; } // Stok Kodu
        public string ProductName { get; set; } = null!;
        public string? Unit { get; set; }
        public int? ProductUnitId { get; set; }
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
        public bool IsNew { get; set; }
        public bool HasError { get; set; }
    }
}
