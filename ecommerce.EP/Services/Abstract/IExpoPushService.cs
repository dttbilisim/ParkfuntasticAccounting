using ecommerce.EP.Models;

namespace ecommerce.EP.Services.Abstract;

/// <summary>
/// Expo Push API üzerinden bildirim gönderim servisi arayüzü.
/// Tek ve toplu bildirim gönderimi ile geçersiz token temizleme işlemlerini tanımlar.
/// </summary>
public interface IExpoPushService
{
    /// <summary>
    /// Tek bir bildirim gönderir. Token listesindeki tüm cihazlara aynı bildirimi iletir.
    /// </summary>
    /// <param name="request">Bildirim isteği (token'lar, başlık, mesaj, ek veri)</param>
    /// <returns>Gönderim sonucu (başarılı/başarısız sayıları, geçersiz token'lar)</returns>
    Task<PushSendResult> SendAsync(PushNotificationRequest request);

    /// <summary>
    /// Toplu bildirim gönderir. Token listesini 100'lük batch'lere bölerek Expo Push API'ye gönderir.
    /// </summary>
    /// <param name="tokens">Hedef cihaz token listesi</param>
    /// <param name="title">Bildirim başlığı</param>
    /// <param name="body">Bildirim mesaj metni</param>
    /// <param name="data">Opsiyonel ek veri (deep link vb.)</param>
    /// <returns>Toplu gönderim sonucu</returns>
    Task<PushBatchResult> SendBatchAsync(
        List<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Geçersiz token'ları veritabanından siler.
    /// DeviceNotRegistered hatası alan token'lar için çağrılır.
    /// </summary>
    /// <param name="invalidTokens">Silinecek geçersiz token listesi</param>
    Task CleanupInvalidTokensAsync(List<string> invalidTokens);
}
