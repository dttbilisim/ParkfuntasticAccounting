using ecommerce.Core.Utils;
using ecommerce.Core.Entities;

namespace ecommerce.Web.Domain.Dtos.Order;

public class CheckoutRequestDto
{
    public int? UserAddressId { get; set; }
    public CardPaymentRequest? CardPayment { get; set; }
    public OrderPlatformType? PlatformType { get; set; } // Optional: If provided, overrides automatic detection
    public int? OnBehalfOfCustomerId { get; set; } // For Plasiyer/Admin to checkout on behalf of a customer
    /// <summary>Teslimat tipi: Cargo veya Courier. Kurye seçildiyse CourierId ve EstimatedCourierDeliveryMinutes doldurulmalı.</summary>
    public DeliveryOptionType? DeliveryOptionType { get; set; }
    /// <summary>Kurye teslimat seçildiyse (DeliveryOptionType == Courier) atanacak kurye id.</summary>
    public int? CourierId { get; set; }
    /// <summary>Kurye tahmini teslimat süresi (dakika).</summary>
    public int? EstimatedCourierDeliveryMinutes { get; set; }
    /// <summary>Mobil/API checkout: Sepet tercihleri (seçili kargo + mesafe). Gönderilirse cookie yerine bu kullanılır; BicoJET kargo ücreti doğru hesaplanır.</summary>
    public CartCustomerSavedPreferences? CartPreferences { get; set; }
    /// <summary>Paket ürünler için voucher kodu (ParkFuntastic uyarlaması).</summary>
    public string? Voucher { get; set; }
    /// <summary>Paket ürünler için rehber/acenta ismi (ParkFuntastic uyarlaması).</summary>
    public string? GuideName { get; set; }
}