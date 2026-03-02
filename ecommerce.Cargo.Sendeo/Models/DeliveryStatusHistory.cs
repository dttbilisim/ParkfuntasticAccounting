namespace ecommerce.Cargo.Sendeo.Models;

public class DeliveryStatusHistory
{
    public DateTime StatusDate { get; set; }
        
    public int StatusId { get; set; }
        
    public string Status { get; set; } = null!;
        
    public string Description { get; set; } = null!;
}