using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Warehouse
{
    public class ProductStock : AuditableEntity<int>
    {
        public int ProductId { get; set; }
        
        public int WarehouseShelfId { get; set; }

        public decimal Quantity { get; set; }


        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        [ForeignKey(nameof(WarehouseShelfId))]
        public virtual WarehouseShelf Shelf { get; set; }
    }
}
