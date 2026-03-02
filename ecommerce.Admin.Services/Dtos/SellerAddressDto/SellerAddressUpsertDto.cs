namespace ecommerce.Admin.Domain.Dtos.SellerAddressDto
{
    public class SellerAddressUpsertDto
    {
        public int? Id { get; set; }
        public int SellerId { get; set; }
        public int CityId { get; set; }
        public int TownId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Title { get; set; }
        public string? StockWhereIs { get; set; }
        public bool IsDefault { get; set; }
    }
}
