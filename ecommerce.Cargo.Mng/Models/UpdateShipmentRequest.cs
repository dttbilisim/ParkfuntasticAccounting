namespace ecommerce.Cargo.Mng.Models;

public class UpdateShipmentRequest
{
    public string ReferenceId { get; set; } = null!;

    public string ShipmentId { get; set; } = null!;

    public string BillOfLandingId { get; set; } = null!;

    public int IsCOD { get; set; }

    public double CodAmount { get; set; }

    public List<OrderPiece> OrderPieceList { get; set; } = new();
}