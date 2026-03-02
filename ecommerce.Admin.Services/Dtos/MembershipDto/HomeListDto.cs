using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(Home))]
    public class HomeListDto
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}
