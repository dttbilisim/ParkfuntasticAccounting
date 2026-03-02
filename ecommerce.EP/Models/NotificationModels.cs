using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ecommerce.EP.Models;

/// <summary>
/// Push token kayıt isteği DTO'su
/// </summary>
public class RegisterTokenRequest
{
    /// <summary>Expo push token veya web subscription JSON</summary>
    [Required(ErrorMessage = "Token alanı zorunludur.")]
    public string Token { get; set; } = null!;

    /// <summary>Platform bilgisi: "ios", "android", "web"</summary>
    [Required(ErrorMessage = "Platform alanı zorunludur.")]
    [RegularExpression("^(ios|android|web)$", ErrorMessage = "Platform değeri sadece 'ios', 'android' veya 'web' olabilir.")]
    public string Platform { get; set; } = null!;

    /// <summary>Cihaz benzersiz tanımlayıcı</summary>
    [Required(ErrorMessage = "DeviceId alanı zorunludur.")]
    public string DeviceId { get; set; } = null!;
}

/// <summary>
/// Token kayıt/silme işlemi yanıt DTO'su
/// </summary>
public class TokenOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

// ─── Expo Push Bildirim DTO'ları ───

/// <summary>
/// Push bildirim gönderim isteği
/// </summary>
public class PushNotificationRequest
{
    /// <summary>Hedef cihaz token'ları</summary>
    public List<string> Tokens { get; set; } = new();

    /// <summary>Bildirim başlığı</summary>
    public string Title { get; set; } = null!;

    /// <summary>Bildirim mesaj metni</summary>
    public string Body { get; set; } = null!;

    /// <summary>Ek veri (deep link vb.)</summary>
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Expo Push API yanıt modeli
/// </summary>
public class ExpoPushResponse
{
    [JsonPropertyName("data")]
    public List<ExpoPushTicket> Data { get; set; } = new();
}

/// <summary>
/// Expo Push API tek bildirim sonucu (ticket)
/// </summary>
public class ExpoPushTicket
{
    /// <summary>"ok" veya "error"</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>Başarılı gönderimde ticket ID</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Hata mesajı</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Hata detayları</summary>
    [JsonPropertyName("details")]
    public ExpoPushError? Details { get; set; }
}

/// <summary>
/// Expo Push API hata detayı
/// </summary>
public class ExpoPushError
{
    /// <summary>"DeviceNotRegistered", "MessageTooBig" vb.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Tek bildirim gönderim sonucu
/// </summary>
public class PushSendResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> InvalidTokens { get; set; } = new();
}

/// <summary>
/// Toplu bildirim gönderim sonucu
/// </summary>
public class PushBatchResult
{
    public int TotalSent { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailure { get; set; }
    public List<string> InvalidTokens { get; set; } = new();
}

/// <summary>
/// Web push bildirim gönderim sonucu
/// </summary>
public class WebPushResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> InvalidSubscriptions { get; set; } = new();
}

/// <summary>
/// Web push subscription JSON parse modeli.
/// Tarayıcıdan gelen subscription nesnesinin deserialize edilmiş hali.
/// </summary>
public class WebPushSubscriptionInfo
{
    public string Endpoint { get; set; } = null!;
    public WebPushSubscriptionKeys Keys { get; set; } = null!;
}

/// <summary>
/// Web push subscription anahtarları (p256dh ve auth)
/// </summary>
public class WebPushSubscriptionKeys
{
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
}


// ─── Kullanıcı Bildirim DTO'ları ───

/// <summary>
/// Kullanıcı bildirim listesi yanıtı
/// </summary>
public class UserNotificationListResponse
{
    public List<UserNotificationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Kullanıcı bildirim DTO'su
/// </summary>
public class UserNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? DeepLink { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
