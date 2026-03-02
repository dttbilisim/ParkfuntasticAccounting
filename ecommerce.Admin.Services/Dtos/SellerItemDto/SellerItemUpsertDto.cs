namespace ecommerce.Admin.Domain.Dtos.SellerItemDto
{
    public class SellerItemUpsertDto
    {
        public int? Id { get; set; }
        public int SellerId { get; set; }
        public int ProductId { get; set; }
        public decimal Stock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Commision { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Unit { get; set; } = "Adet";
        public int Status { get; set; }
    }
}
