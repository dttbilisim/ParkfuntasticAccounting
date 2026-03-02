using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
    [AutoMap(typeof(Street))]
    public class StreetListDto
    {
        public int Id { get; set; }
        public int NeighboorId { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
    }
}
