using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(City))]
    public class CityListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}

