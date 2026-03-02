namespace ecommerce.Cargo.Mng.Models;
public class CreateDetailOrder{
    
    public Order Order { get; set; } 



    public Recipient Recipient { get; set; }
    public Shipper Shipper{get;set;} 
    public List<OrderPiece> OrderPieceList { get; set; }

}
