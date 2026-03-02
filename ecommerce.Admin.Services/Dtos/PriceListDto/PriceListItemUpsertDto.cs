namespace ecommerce.Admin.Domain.Dtos.PriceListDto
{
    public class PriceListItemUpsertDto
    {
        public int? Id { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Order { get; set; }
    }
}
