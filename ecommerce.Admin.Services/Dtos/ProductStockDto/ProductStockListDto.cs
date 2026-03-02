using AutoMapper;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.ProductStockDto
{
    [AutoMap(typeof(ProductStock))]
    public class ProductStockListDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } 
        public int WarehouseShelfId { get; set; }
        public int ShelfWarehouseId { get; set; }
        public string ShelfCode { get; set; } 
        public string ShelfWarehouseName { get; set; }
        public int Quantity { get; set; }
        public int Status { get; set; }
    }
}
