namespace ecommerce.Admin.Domain.Dtos.PriceListDto
{
    public class PriceListListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
