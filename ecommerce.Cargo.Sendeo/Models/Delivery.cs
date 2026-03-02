namespace ecommerce.Cargo.Sendeo.Models;

public class Delivery
{
    public string TrackingNo { get; set; }

    public string ReferenceNo { get; set; } = null!;

    public string SendDate { get; set; } = null!;

    public string Receiver { get; set; } = null!;

    public decimal CargoAmount { get; set; }

    public string Sender { get; set; } = null!;

    public int State { get; set; }

    public string StateText { get; set; } = null!;

    public string? UpdateDate { get; set; } = null!;

    public string? DeliveryDescription { get; set; }

    public List<DeliveryProduct> Products { get; set; } = new();

    public int DealerId { get; set; }

    public int DeciWeight { get; set; }

    public int Quantity { get; set; }

    public decimal TaxPrice { get; set; }

    public decimal GrandTotalPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string DepartureBranchName { get; set; } = null!;

    public string ArrivalBranchName { get; set; } = null!;

    public DateTime DeliveryPlannedDate { get; set; }

    public List<DeliveryStatusHistory> StatusHistories { get; set; } = new();

    public string TrackingUrl { get; set; } = null!;
}