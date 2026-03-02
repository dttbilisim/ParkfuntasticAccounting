using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.SalesPersonDto
{
    [AutoMap(typeof(SalesPerson))]
    public class SalesPersonListDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? MobilePhone { get; set; }
        public string? Email { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? TownId { get; set; }
        public string? TownName { get; set; }
        public string? Address { get; set; }
        public bool SmsPermission { get; set; }
        public EntityStatus Status { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}