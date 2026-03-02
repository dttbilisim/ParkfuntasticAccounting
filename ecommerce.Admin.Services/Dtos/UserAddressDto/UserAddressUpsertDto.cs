namespace ecommerce.Admin.Domain.Dtos.UserAddressDto
{
    public class UserAddressUpsertDto
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public int? ApplicationUserId { get; set; }
        public string AddressName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public string? IdentityNumber { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSameAsDeliveryAddress { get; set; } = true;
        public int? InvoiceCityId { get; set; }
        public int? InvoiceTownId { get; set; }
        public string? InvoiceAddress { get; set; }
    }
}
