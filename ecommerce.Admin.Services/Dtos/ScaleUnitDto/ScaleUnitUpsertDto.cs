using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ScaleUnitDto
{
    [AutoMap(typeof(ScaleUnit), ReverseMap = true)]
    public class ScaleUnitUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
