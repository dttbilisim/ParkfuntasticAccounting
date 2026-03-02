namespace ecommerce.Cargo.Mng.Models;

public class CreateOrderRequest
{
    public Order Order { get; set; } = null!;

    public List<OrderPiece> OrderPieceList { get; set; } = null!;

    public Customer Recipient { get; set; } = null!;
}