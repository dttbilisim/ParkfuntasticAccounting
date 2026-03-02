using ecommerce.Cargo.Mng.Enums;

namespace ecommerce.Cargo.Mng.Models;

public class Order
{
    public string ReferenceId { get; set; } 

    public string Barcode { get; set; }

    public string BillOfLandingId { get; set; }

    public int IsCOD { get; set; }

    public double CodAmount { get; set; }

    public ShipmentServiceType ShipmentServiceType { get; set; }

    public PackagingType PackagingType { get; set; }

    public string Content { get; set; } 
    
    public int SmsPreference1 { get; set; }

    public int SmsPreference2 { get; set; }

    public int SmsPreference3 { get; set; }

    public PaymentType PaymentType { get; set; }

    public DeliveryType DeliveryType { get; set; }

    public string? Description { get; set; }

    public string ? MarketPlaceShortCode { get; set; } = string.Empty;

    public string ? MarketPlaceSaleCode { get; set; } = string.Empty;
    public string ? PudoId{get;set;}
}