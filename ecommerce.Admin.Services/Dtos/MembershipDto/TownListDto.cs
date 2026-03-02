using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(Town))]
    public class TownListDto
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}

