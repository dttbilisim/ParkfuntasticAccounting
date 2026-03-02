namespace ecommerce.Admin.Domain.Dtos.SellerItemDto
{
    public class SellerItemListDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Barcode { get; set; } = null!;
        public string? Oem { get; set; }
        public decimal Stock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
