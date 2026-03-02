
namespace ecommerce.Admin.Domain.Dtos.ProductStockDto
{
    public class ProductStockTransferDto
    {
        public int ProductId { get; set; }
        public int SourceShelfId { get; set; }
        public int TargetShelfId { get; set; }
        public decimal Quantity { get; set; }
        public Guid BatchId { get; set; }
    }
}
