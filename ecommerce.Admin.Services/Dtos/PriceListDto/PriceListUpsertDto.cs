namespace ecommerce.Admin.Domain.Dtos.PriceListDto
{
    public class PriceListUpsertDto
    {
        public int? Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int? CorporationId { get; set; }
        public int? BranchId { get; set; }
        public int? CustomerId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CurrencyId { get; set; }
        public List<PriceListItemUpsertDto> Items { get; set; } = new();
    }
}
