namespace ecommerce.Admin.Domain.Dtos.ProductUnitDto
{
    public class ProductUnitUpsertDto
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public int UnitId { get; set; }
        public string? Barcode { get; set; }
        public decimal UnitValue { get; set; } = 1;
    }
}
