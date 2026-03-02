using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Warehouse;

public class StockTransferLog : AuditableEntity<int>
{
    public int ProductId { get; set; }
    public int SourceWarehouseId { get; set; }
    public int TargetWarehouseId { get; set; }
    public int SourceShelfId { get; set; }
    public int TargetShelfId { get; set; }
    public decimal Quantity { get; set; }
    public int TransferredByUserId { get; set; }
    public DateTime TransferDate { get; set; }
    public Guid BatchId { get; set; } // Groups related transfers together
    
    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; }
    
    [ForeignKey(nameof(SourceWarehouseId))]
    public virtual Warehouse SourceWarehouse { get; set; }
    
    [ForeignKey(nameof(TargetWarehouseId))]
    public virtual Warehouse TargetWarehouse { get; set; }
    
    [ForeignKey(nameof(SourceShelfId))]
    public virtual WarehouseShelf SourceShelf { get; set; }
    
    [ForeignKey(nameof(TargetShelfId))]
    public virtual WarehouseShelf TargetShelf { get; set; }
}
