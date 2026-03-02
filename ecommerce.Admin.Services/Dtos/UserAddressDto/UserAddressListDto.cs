namespace ecommerce.Admin.Domain.Dtos.UserAddressDto
{
    public class UserAddressListDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ApplicationUserId { get; set; }
        public string AddressName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? TownId { get; set; }
        public string? TownName { get; set; }
        public string? IdentityNumber { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSameAsDeliveryAddress { get; set; }
        public int? InvoiceCityId { get; set; }
        public string? InvoiceCityName { get; set; }
        public int? InvoiceTownId { get; set; }
        public string? InvoiceTownName { get; set; }
        public string? InvoiceAddress { get; set; }
        public int Status { get; set; }
        public string StatusStr => Status == 1 ? "Aktif" : "Pasif";
        
        // Display property for dropdown
        public string AddressDisplayName => $"{AddressName} - {FullName}";
    }
}
