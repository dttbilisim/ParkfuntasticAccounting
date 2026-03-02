namespace ecommerce.EP.Services;

/// <summary>
/// Kurye belgesi yolunu görüntüleme URL'sine çevirir.
/// MinIO kullanılıyorsa presigned URL; dosya depolamada FileHelper tabanlı URL.
/// </summary>
public interface ICourierDocumentUrlProvider
{
    Task<string?> GetDocumentUrlAsync(string? path, CancellationToken ct = default);
}
