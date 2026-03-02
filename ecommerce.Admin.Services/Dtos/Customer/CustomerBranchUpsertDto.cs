namespace ecommerce.Admin.Domain.Dtos.Customer
{
    public class CustomerBranchUpsertDto
    {
        public int? Id { get; set; }
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public bool IsDefault { get; set; }
    }
}
