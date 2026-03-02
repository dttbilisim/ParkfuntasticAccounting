namespace ecommerce.Admin.Domain.Dtos.ProductDto
{
    public class ProductSellerItemListDto
    {
        public int Id { get; set; }

        public string SellerName { get; set; } = null!;

        public string? SellerEmail { get; set; }

        public string? SellerCity { get; set; }

        public decimal Stock { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SalePrice { get; set; }

        public decimal Commission { get; set; }

        public string Currency { get; set; } = null!;

        public string Unit { get; set; } = null!;
    }
}



