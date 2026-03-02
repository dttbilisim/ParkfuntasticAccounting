using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.OrderItemDto
{
    public class OrderItemListDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int BrandId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal OldPrice { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public int? CommissionRateId { get; set; } = 0;
        public int? CommissionRatePercent { get; set; } = 0;
        public decimal? CommissionTotal { get; set; } = 0;
        public decimal? DiscountAmount { get; set; } = 0;
        public DateTime ExprationDate { get; set; }
        public decimal MerhantCommision { get; set; } = 0;
        public decimal SubmerhantCommision { get; set; } = 0;
        public string PaymentTransactionId { get; set; }
        public decimal Width { get; set; } = 0;
        public decimal Length { get; set; } = 0;
        public decimal Height { get; set; } = 0;
        public decimal CargoDesi { get; set; } = 0;
        public virtual List<ProductImage> ProductImages { get; set; }

        public Product Product { get; set; }
    }
}
