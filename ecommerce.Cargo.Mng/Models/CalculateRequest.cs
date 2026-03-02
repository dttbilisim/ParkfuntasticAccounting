using ecommerce.Cargo.Mng.Enums;

namespace ecommerce.Cargo.Mng.Models;

public class CalculateRequest
{
    public ShipmentServiceType ShipmentServiceType { get; set; }

    public PackagingType PackagingType { get; set; }

    public PaymentType PaymentType { get; set; }

    public PickUpType PickUpType { get; set; }

    public DeliveryType DeliveryType { get; set; }

    public long? RecipientCustomerId { get; set; }

    public int? CityCode { get; set; }

    public int? DistrictCode { get; set; }

    public string Address { get; set; } = null!;

    public int SmsPreference1 { get; set; }

    public int SmsPreference2 { get; set; }

    public int SmsPreference3 { get; set; }

    public List<OrderPiece> OrderPieceList { get; set; } = new();
}

public enum PaymentType
{
    GondericiOder = 1,
    AliciOder = 2,
    PlatformOder = 3
}

public enum PickUpType
{
    AdrestenAlim = 1,
    SubeyeGetirildi = 2
}

public enum DeliveryType
{
    AdreseTeslim = 1,
    AlicisiHaberli = 2
}