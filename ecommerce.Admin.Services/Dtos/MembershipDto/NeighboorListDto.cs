using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(Neighboor))]
    public class NeighboorListDto
    {
        public int Id { get; set; }
        public int TownId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}
