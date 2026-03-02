namespace ecommerce.Admin.Domain.Dtos.ProductStockDto;

public class StockTransferLogListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public DateTime TransferDate { get; set; }
    public string ProductName { get; set; }
    public int SourceWarehouseId { get; set; }
    public string SourceWarehouseName { get; set; }
    public int TargetWarehouseId { get; set; }
    public string TargetWarehouseName { get; set; }
    public int SourceShelfId { get; set; }
    public string SourceShelfCode { get; set; }
    public int TargetShelfId { get; set; }
    public string TargetShelfCode { get; set; }
    public decimal Quantity { get; set; }
    public string TransferredByUserName { get; set; }
}
