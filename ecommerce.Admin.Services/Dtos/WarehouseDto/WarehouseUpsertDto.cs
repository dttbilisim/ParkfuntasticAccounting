using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.WarehouseDto
{
    [AutoMap(typeof(Warehouse), ReverseMap = true)]
    public class WarehouseUpsertDto
    {
        public int? Id { get; set; }
        public int? CorporationId { get; set; }
        public int? BranchId { get; set; }
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public int Status { get; set; } = 1;
        [Ignore]
        public bool StatusBool { get; set; }
    }
}
