namespace ecommerce.Admin.Domain.Dtos.SellerDto
{
    public class SellerUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public decimal Commission { get; set; }
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public bool IsOrderUse { get; set; }
        public int Status { get; set; }
    }
}

