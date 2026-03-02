namespace ecommerce.Odaksodt.Options;

/// <summary>
/// Odaksoft E-Fatura entegrasyonu için konfigürasyon ayarları
/// </summary>
public class OdaksoftOptions
{
    /// <summary>
    /// Konfigürasyon section adı
    /// </summary>
    public const string SectionName = "Odaksoft";

    /// <summary>
    /// API Base URL (Test: https://integration-test.odaksoft.com.tr/api/, Canlı: https://integration.odaksoft.com.tr/api/)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Entegrasyon kullanıcı adı
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Entegrasyon şifresi
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Token geçerlilik süresi (dakika)
    /// </summary>
    public int TokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// HTTP request timeout (saniye)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Retry policy - maksimum deneme sayısı
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Test ortamı mı?
    /// </summary>
    public bool IsTestEnvironment { get; set; } = true;
}
