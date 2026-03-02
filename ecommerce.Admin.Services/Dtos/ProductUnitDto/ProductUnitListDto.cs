namespace ecommerce.Admin.Domain.Dtos.ProductUnitDto
{
    public class ProductUnitListDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public string? Barcode { get; set; }
        public decimal UnitValue { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
