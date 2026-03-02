namespace ecommerce.Cargo.Sendeo.Models;

public class CargoListRequest
{
    public DateTime? ShipmentStartDate { get; set; }

    public DateTime? ShipmentEndDate { get; set; }

    public DateTime? DeliveryStartDate { get; set; }

    public DateTime? DeliveryEndDate { get; set; }

    public int? DocumentType { get; set; }

    public int? SenderCustomerId { get; set; }

    public int? ReceiverCustomerId { get; set; }

    public int? SenderCityId { get; set; }

    public int? ReceiverCityId { get; set; }

    public int ServiceTypeId => 1;

    public List<int>? StatusIds { get; set; }

    public List<long>? TrackingNumbers { get; set; }

    public List<int>? AtfNumbers { get; set; }

    public List<string>? CustomerReferenceNumbers { get; set; }

    public string? ReceiverCustomerTitle { get; set; }

    public string? SenderCustomerTitle { get; set; }

    public int? SenderBranchId { get; set; }

    public int? ReceiverBranchId { get; set; }

    public int? CargoStatus { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageCount { get; set; } = 10;
}