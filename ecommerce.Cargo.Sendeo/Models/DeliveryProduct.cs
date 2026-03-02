namespace ecommerce.Cargo.Sendeo.Models;

public class DeliveryProduct
{
    public int Count { get; set; }
        
    public int ProductCode { get; set; }
        
    public string Description { get; set; } = null!;
        
    public decimal Price { get; set; }
        
    public int StockCount { get; set; }
        
    public int MinStockAmount { get; set; }
}