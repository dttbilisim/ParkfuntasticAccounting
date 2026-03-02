using AutoMapper;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.WarehouseDto
{
    [AutoMap(typeof(Warehouse))]
    public class WarehouseListDto
    {
        public int Id { get; set; }
        public int CorporationId { get; set; }
        public string CorporationName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? TownId { get; set; }
        public string? TownName { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public int Status { get; set; } // 1: Active, 0: Passive
    }
}
