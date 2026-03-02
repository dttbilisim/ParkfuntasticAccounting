using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.ProductStockDto
{
    [AutoMap(typeof(ProductStock), ReverseMap = true)]
    public class ProductStockUpsertDto
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public int WarehouseShelfId { get; set; }
        public int Quantity { get; set; }
        public int Status { get; set; } = 1;
        [Ignore]
        public bool StatusBool { get; set; }
    }
}
