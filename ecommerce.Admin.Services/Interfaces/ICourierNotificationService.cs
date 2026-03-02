using ecommerce.Core.Utils;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>
/// Kurye sipariş bildirimi — sipariş atandığında kuryeye, durum değişikliklerinde müşteriye push bildirim gönderir.
/// </summary>
public interface ICourierNotificationService
{
    /// <summary>
    /// Kuryeye sipariş atandı bildirimi gönderir.
    /// </summary>
    Task SendOrderAssignedNotificationAsync(int courierApplicationUserId, string orderNumber, int orderId);

    /// <summary>
    /// Müşteriye kurye durum değişikliği bildirimi gönderir (Kabul, Aldım, Yolda, Teslim Edildi, İptal).
    /// </summary>
    /// <param name="customerApplicationUserId">Sipariş sahibi (müşteri) ApplicationUser Id</param>
    /// <param name="orderNumber">Sipariş numarası</param>
    /// <param name="orderId">Sipariş Id</param>
    /// <param name="status">Yeni kurye teslimat durumu</param>
    Task SendOrderStatusUpdateToCustomerAsync(int customerApplicationUserId, string orderNumber, int orderId, CourierDeliveryStatus status);
}
