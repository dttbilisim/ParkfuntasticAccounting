namespace ecommerce.Admin.Domain.Dtos.ProductStockDto;

public class StockTransferBatchDto
{
    public Guid BatchId { get; set; }
    public DateTime TransferDate { get; set; }
    public string SourceWarehouseName { get; set; }
    public string TargetWarehouseName { get; set; }
    public string SourceShelfCodes { get; set; } // Comma-separated if multiple
    public string TargetShelfCodes { get; set; } // Comma-separated if multiple
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public string TransferredByUserName { get; set; }
    public List<StockTransferLogListDto> Items { get; set; } = new();
}
