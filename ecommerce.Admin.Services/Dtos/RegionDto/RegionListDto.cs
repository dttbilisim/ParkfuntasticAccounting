using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.RegionDto
{
    [AutoMap(typeof(Region))]
    public class RegionListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public EntityStatus Status { get; set; }
    }
}

