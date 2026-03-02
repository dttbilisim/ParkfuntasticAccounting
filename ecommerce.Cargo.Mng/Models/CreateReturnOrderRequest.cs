namespace ecommerce.Cargo.Mng.Models;

public class CreateReturnOrderRequest
{
    public Order Order { get; set; } = null!;

    public List<OrderPiece> OrderPieceList { get; set; } = null!;

    public Customer Shipper { get; set; } = null!;
}