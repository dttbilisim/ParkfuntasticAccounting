using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.Customer
{
    public class CustomerListDto
    {
        public int Id { get; set; }
        public int CorporationId { get; set; }
        public string CorporationName { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public CustomerType Type { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CityName { get; set; }
        public string? TownName { get; set; }
        public string? RegionName { get; set; }
        public bool IsActive { get; set; }
        public decimal RiskLimit { get; set; }
        public CustomerWorkingTypeEnum CustomerWorkingType { get; set; }
        public int PaymentDue { get; set; }
        public string DisplayName => $"[{Code}] {Name}";
    }
}
