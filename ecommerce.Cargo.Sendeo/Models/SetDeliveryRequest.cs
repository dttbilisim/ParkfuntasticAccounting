namespace ecommerce.Cargo.Sendeo.Models
{
    public class SetDeliveryRequest
    {
        public DeliveryType DeliveryType { get; set; }

        public string ReferenceNo { get; set; } = null!;

        public string? Description { get; set; }

        public string? Sender { get; set; }

        public string? SenderBranchCode { get; set; }

        public string? SenderId { get; set; }

        public string? SenderAuthority { get; set; }

        public string? SenderAddress { get; set; }

        public int SenderCityId { get; set; }

        public int SenderDistrictId { get; set; }

        public string? SenderPhone { get; set; }

        public string? SenderGSM { get; set; }

        public string SenderEmail { get; set; } = null!;

        public string Receiver { get; set; } = null!;

        public int? ReceiverBranchCode { get; set; }

        public string? ReceiverId { get; set; }

        public string? ReceiverAuthority { get; set; }

        public string? ReceiverAddress { get; set; }

        public int ReceiverCityId { get; set; }

        public int ReceiverDistrictId { get; set; }

        public string? ReceiverPhone { get; set; }

        public string ReceiverGSM { get; set; } = null!;

        public string ReceiverEmail { get; set; } = null!;

        public int PaymentType => 1;

        public CollectionType CollectionType => CollectionType.TahsilatsizKargo;

        public int CollectionPrice => 0;

        public string? DispatchNoteNumber { get; set; }

        public int ServiceType => 1;

        public BarcodeLabelType BarcodeLabelType { get; set; } = BarcodeLabelType.Base64;

        public string? AdditionalValue { get; set; }

        public List<SetDeliveryProductRequest> Products { get; set; }

      //  public int PayType => 1;

        public string? CustomerReferenceType { get; set; }

        public bool IsReturn { get; set; }

        public string? SenderTaxpayerId { get; set; }

        public string? ReceiverTaxpayerId { get; set; }

        public string? IntegratorCustomerCode { get; set; }

        public string? OtherDescription { get; set; }

        public bool? IsMobilePickUp { get; set; }

        public DateTime? PickUpDate { get; set; }
    }
}