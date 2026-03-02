using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.RegionDto;

namespace ecommerce.Admin.Domain.Dtos.Customer
{
    public class CustomerSalesPersonDto
    {
        public int Id { get; set; }             // CustomerPlasiyer Id
        public int CustomerId { get; set; }
        public int SalesPersonId { get; set; }
        public int RegionId { get; set; }

        public string SalesPersonName { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}


