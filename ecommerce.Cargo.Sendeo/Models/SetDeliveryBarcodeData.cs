namespace ecommerce.Cargo.Sendeo.Models;

public class SetDeliveryBarcodeData
{
    public string TrackingNumber { get; set; } = null!;

    public DateTime ShipmentDate { get; set; }

    public string? SenderCustomerTitle { get; set; }

    public string? ReceiverCustomerTitle { get; set; }

    public string? CustomerDispatchNoteNumber { get; set; }

    public string? CustomerReferenceNo { get; set; }

    public decimal Deci { get; set; }

    public decimal Weight { get; set; }

    public decimal DeciWeight { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int Quantity { get; set; }

    public string? SenderAddress { get; set; }

    public string? ReceiverAddress { get; set; }

    public string? SenderCity { get; set; }

    public string? ReceiverCity { get; set; }

    public string? SenderDistrict { get; set; }

    public string? ReceiverDistrict { get; set; }

    public string? SenderPhone { get; set; }

    public string? ReceiverPhone { get; set; }

    public string? ReceiverGsm { get; set; }

    public string? PaymentType { get; set; }

    public string? ReceiverBranch { get; set; }

    public string? SenderBranch { get; set; }

    public bool DeliveryToPoint { get; set; }

    public string? ServiceType { get; set; }

    public string? ReceiverHubName { get; set; }

    public int DocumentType { get; set; }

    public string? CourierZoneName { get; set; }

    public string? PudoName { get; set; }

    public string? PudoAddress { get; set; }

    public string? PudoPhone { get; set; }

    public DateTime PlannedDate { get; set; }

    public bool IsMobileDelivery { get; set; }

    public string? AdditionalValue { get; set; }

    public bool IsCashOnDelivery { get; set; }

    public string? CashOnDeliveryType { get; set; }

    public string? CashOnDeliveryPrice { get; set; }

    public List<SetDeliveryBarcodeNo> Barcodes { get; set; } = null!;
}