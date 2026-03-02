using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.RegionDto
{
    [AutoMap(typeof(Region), ReverseMap = true)]
    public class RegionUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}

