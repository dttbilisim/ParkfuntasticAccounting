namespace ecommerce.Cargo.Mng.Models;

public class GetShipmentStatusResponse
{
    public int ShipmentStatusCode { get; set; }

    public string OrderId { get; set; } = null!;

    public string ReferenceId { get; set; } = null!;

    public string ShipmentId { get; set; } = null!;

    public string? ShipmentSerialandNumber { get; set; }

    public DateTime? ShipmentDateTime { get; set; }

    public string ShipmentStatus { get; set; } = null!;

    public DateTime? StatusDateTime { get; set; }

    public string? TrackingUrl { get; set; } = null!;

    public bool IsDelivered { get; set; }

    public DateTime? DeliveryDateTime { get; set; }

    public string? DeliveryTo { get; set; }

    public string? RetrieveShipmentId { get; set; }
}