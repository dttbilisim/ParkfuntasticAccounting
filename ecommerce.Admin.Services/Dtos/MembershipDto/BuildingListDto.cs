using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(Building))]
    public class BuildingListDto
    {
        public int Id { get; set; }
        public int StreetId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}
