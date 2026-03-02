using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ScaleUnitDto
{
    [AutoMap(typeof(ScaleUnit))]
    public class ScaleUnitListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public EntityStatus Status { get; set; }
    }
}
