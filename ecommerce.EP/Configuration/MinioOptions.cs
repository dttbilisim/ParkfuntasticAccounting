namespace ecommerce.EP.Configuration;

/// <summary>
/// MinIO depolama ayarları (appsettings "MinIO" bölümü).
/// Dolu ise kurye belgeleri MinIO'ya yüklenir; boş/eksik ise mevcut dosya yükleme kullanılır.
/// </summary>
public class MinioOptions
{
    public const string SectionName = "MinIO";

    public string Endpoint { get; set; } = "";
    public int Port { get; set; } = 9000;
    public bool UseSSL { get; set; }
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "courier-documents";
    /// <summary>Object key öneki (örn. "courier-documents").</summary>
    public string ObjectPrefix { get; set; } = "courier-documents";

    /// <summary>
    /// Presigned URL'lerde kullanılacak dış erişim adresi (örn. 92.204.172.3).
    /// Sunucu MinIO'ya dahili IP (192.168.1.10) ile bağlansa bile, tarayıcıda açılan link bu adreste olmalı;
    /// aksi halde imza (SignatureDoesNotMatch) hatası oluşur. Boş bırakılırsa Endpoint kullanılır.
    /// </summary>
    public string? EndpointForPresignedUrls { get; set; }
    /// <summary>Presigned URL portu; null ise Port kullanılır.</summary>
    public int? PortForPresignedUrls { get; set; }
    /// <summary>Presigned URL için HTTPS; null ise UseSSL kullanılır.</summary>
    public bool? UseSSLForPresignedUrls { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
