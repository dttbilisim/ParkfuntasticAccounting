using ecommerce.EP.Models;

namespace ecommerce.EP.Services.Abstract;

/// <summary>
/// Web Push API üzerinden tarayıcı bildirimi gönderim servisi arayüzü.
/// VAPID kimlik doğrulaması ile Web Push Protocol kullanır.
/// </summary>
public interface IWebPushService
{
    /// <summary>
    /// Web push subscription'lara bildirim gönderir.
    /// Her subscription JSON olarak saklanır ve parse edilerek gönderilir.
    /// </summary>
    /// <param name="subscriptionJsons">Web push subscription JSON listesi</param>
    /// <param name="title">Bildirim başlığı</param>
    /// <param name="body">Bildirim mesaj metni</param>
    /// <param name="url">Bildirime tıklandığında açılacak URL (opsiyonel)</param>
    /// <returns>Gönderim sonucu (başarılı/başarısız sayıları, geçersiz subscription'lar)</returns>
    Task<WebPushResult> SendAsync(List<string> subscriptionJsons, string title, string body, string? url = null);
}
