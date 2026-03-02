using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.WarehouseShelfDto
{
    [AutoMap(typeof(WarehouseShelf), ReverseMap = true)]
    public class WarehouseShelfUpsertDto
    {
        public int? Id { get; set; }
        public int WarehouseId { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; } = 1;

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
