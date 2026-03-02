namespace ecommerce.Cargo.Sendeo.Models;

public class CargoListItem
{
    public int Id { get; set; }

    public string? ServiceType { get; set; }

    public long TrackingNumber { get; set; }

    public int? AtfNumber { get; set; }

    public string? CustomerReferenceNo { get; set; }

    public string? ReceiverCustomerTitle { get; set; }

    public string? SenderCityName { get; set; }

    public string? ReceiverCityName { get; set; }

    public decimal DeciWeight { get; set; }

    public int Quantity { get; set; }

    public string? Status { get; set; }

    public decimal TotalPrice { get; set; }

    public string? CustomerDispatchNoteNumber { get; set; }

    public DateTime ShipmentDateTime { get; set; }

    public string? ShipmentDate { get; set; }

    public string? DeliveryPlannedDate { get; set; }

    public string? DeliveryDate { get; set; }

    public string? SenderCustomerTitle { get; set; }

    public string? ReceiverDistrictName { get; set; }

    public string? PaymentType { get; set; }

    public string? DeliveryDescription { get; set; }

    public int DocumentType { get; set; }

    public string? SenderBranchName { get; set; }

    public string? ReceiverBranchName { get; set; }

    public string? SenderRegionName { get; set; }

    public string? ReceiverRegionName { get; set; }

    public bool DeliveryToPoint { get; set; }

    public int? ServiceTypeId { get; set; }

    public int StatusGroupId { get; set; }

    public int PaymentCustomerId { get; set; }

    public int SenderCustomerId { get; set; }

    public int ReceiverCustomerId { get; set; }

    public string? ShipmentLineType { get; set; }

    public int? StatusLogId { get; set; }

    public int? StatusId { get; set; }

    public string? CargoStatus { get; set; }

    public string? StatusDescription { get; set; }
}