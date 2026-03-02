using AutoMapper;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.WarehouseShelfDto
{
    [AutoMap(typeof(WarehouseShelf))]
    public class WarehouseShelfListDto
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } 
        public string Code { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }
}
