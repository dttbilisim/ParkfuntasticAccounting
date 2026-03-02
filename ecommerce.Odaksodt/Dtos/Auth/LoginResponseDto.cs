using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Auth;

/// <summary>
/// Login response DTO - Odaksoft API response yapısına uygun
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// JWT token bilgileri (nested object)
    /// </summary>
    [JsonPropertyName("jwtToken")]
    public JwtTokenInfo? JwtToken { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// API mesajı
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// İşlem başarılı mı? (API'den "status" olarak geliyor)
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    // Convenience properties (API'den gelmiyor, kod içinde kullanım için)
    
    /// <summary>
    /// Access token (JwtToken.AccessToken'dan alınır)
    /// </summary>
    [JsonIgnore]
    public string Token => JwtToken?.AccessToken ?? string.Empty;

    /// <summary>
    /// Token geçerlilik süresi (JwtToken.Expiration'dan alınır)
    /// </summary>
    [JsonIgnore]
    public DateTime ExpiresAt => JwtToken?.Expiration ?? DateTime.MinValue;

    /// <summary>
    /// Başarılı mı? (Status ile aynı)
    /// </summary>
    [JsonIgnore]
    public bool Success => Status;

    /// <summary>
    /// Hata mesajı (başarısızsa Message)
    /// </summary>
    [JsonIgnore]
    public string? ErrorMessage => !Status ? Message : null;
}

/// <summary>
/// JWT token bilgileri (nested object)
/// </summary>
public class JwtTokenInfo
{
    /// <summary>
    /// Access token
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token geçerlilik süresi
    /// </summary>
    [JsonPropertyName("expiration")]
    public DateTime Expiration { get; set; }
}
