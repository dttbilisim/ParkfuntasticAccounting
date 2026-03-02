namespace ecommerce.Cargo.Mng.Models;

public class CancelShipmentRequest
{
    public string ReferenceId { get; set; } = null!;

    public string ShipmentId { get; set; } = null!;
}