using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.SalesPersonDto
{
    [AutoMap(typeof(SalesPerson), ReverseMap = true)]
    public class SalesPersonUpsertDto
    {
        public int? Id { get; set; }
        public int? BranchId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? MobilePhone { get; set; }
        public string? Email { get; set; }
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public string? Address { get; set; }
        public bool SmsPermission { get; set; }
        public int Status { get; set; }


        [Ignore]
        public bool StatusBool { get; set; }

        public List<SalesPersonBranchUpsertDto> Branches { get; set; } = new();
    }
}