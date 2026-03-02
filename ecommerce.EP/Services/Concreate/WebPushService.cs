using System.Text.Json;
using ecommerce.EP.Models;
using ecommerce.EP.Services.Abstract;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;

namespace ecommerce.EP.Services.Concreate;

/// <summary>
/// Web Push API üzerinden tarayıcı bildirimi gönderim servisi.
/// Lib.Net.Http.WebPush kütüphanesi ile VAPID kimlik doğrulaması kullanır.
/// Geçersiz subscription'ları tespit eder ve raporlar.
/// </summary>
public class WebPushService : IWebPushService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushService> _logger;

    /// <summary>JSON serialization ayarları — camelCase property isimleri</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebPushService(
        IConfiguration configuration,
        ILogger<WebPushService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WebPushResult> SendAsync(
        List<string> subscriptionJsons,
        string title,
        string body,
        string? url = null)
    {
        var result = new WebPushResult();

        if (subscriptionJsons == null || subscriptionJsons.Count == 0)
        {
            _logger.LogWarning("Web push gönderilecek subscription listesi boş.");
            return result;
        }

        // VAPID ayarlarını oku
        var vapidSubject = _configuration["WebPush:VapidSubject"];
        var vapidPublicKey = _configuration["WebPush:VapidPublicKey"];
        var vapidPrivateKey = _configuration["WebPush:VapidPrivateKey"];

        if (string.IsNullOrEmpty(vapidSubject) ||
            string.IsNullOrEmpty(vapidPublicKey) ||
            string.IsNullOrEmpty(vapidPrivateKey))
        {
            _logger.LogError("VAPID konfigürasyonu eksik. Web push bildirimleri gönderilemez.");
            result.FailureCount = subscriptionJsons.Count;
            return result;
        }

        // Bildirim payload'ını hazırla
        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            url,
            icon = "/assets/images/logo.png"
        });

        // PushServiceClient oluştur — VAPID kimlik doğrulaması ile
        var pushClient = new PushServiceClient();
        pushClient.DefaultAuthentication = new VapidAuthentication(
            vapidPublicKey, vapidPrivateKey)
        {
            Subject = vapidSubject
        };

        // Her subscription'a bildirim gönder
        foreach (var subscriptionJson in subscriptionJsons)
        {
            try
            {
                // Subscription JSON'ı parse et
                var subInfo = JsonSerializer.Deserialize<WebPushSubscriptionInfo>(
                    subscriptionJson, JsonOptions);

                if (subInfo == null || string.IsNullOrEmpty(subInfo.Endpoint) ||
                    subInfo.Keys == null || string.IsNullOrEmpty(subInfo.Keys.P256dh) ||
                    string.IsNullOrEmpty(subInfo.Keys.Auth))
                {
                    _logger.LogWarning("Geçersiz web push subscription JSON formatı.");
                    result.FailureCount++;
                    result.InvalidSubscriptions.Add(subscriptionJson);
                    continue;
                }

                // Lib.Net.Http.WebPush subscription nesnesi oluştur
                var pushSubscription = new PushSubscription
                {
                    Endpoint = subInfo.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        ["p256dh"] = subInfo.Keys.P256dh,
                        ["auth"] = subInfo.Keys.Auth
                    }
                };

                // Bildirim mesajı oluştur ve gönder
                var pushMessage = new PushMessage(payload);
                await pushClient.RequestPushMessageDeliveryAsync(pushSubscription, pushMessage);

                result.SuccessCount++;
            }
            catch (PushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                                                         ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Subscription süresi dolmuş veya geçersiz — temizlenecek
                _logger.LogWarning(
                    "Web push subscription geçersiz (expired/gone). StatusCode: {StatusCode}",
                    ex.StatusCode);
                result.FailureCount++;
                result.InvalidSubscriptions.Add(subscriptionJson);
            }
            catch (PushServiceClientException ex)
            {
                _logger.LogError(ex,
                    "Web push gönderim hatası. StatusCode: {StatusCode}",
                    ex.StatusCode);
                result.FailureCount++;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Web push subscription JSON parse hatası.");
                result.FailureCount++;
                result.InvalidSubscriptions.Add(subscriptionJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Web push gönderiminde beklenmeyen hata.");
                result.FailureCount++;
            }
        }

        _logger.LogInformation(
            "Web push gönderimi tamamlandı. Başarılı: {SuccessCount}, Başarısız: {FailureCount}, Geçersiz: {InvalidCount}",
            result.SuccessCount, result.FailureCount, result.InvalidSubscriptions.Count);

        return result;
    }
}
