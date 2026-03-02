namespace ecommerce.EP.Services;

/// <summary>
/// Kurye başvuru belgelerini (Vergi Levhası, İmza Beyannamesi, Kimlik Fotokopisi) diske kaydeder.
/// </summary>
public interface ICourierDocumentUploadService
{
    /// <summary>
    /// Dosyayı CourierDocuments klasörüne kaydeder. Dönen yol: CourierDocuments/xxx.ext
    /// </summary>
    Task<string?> SaveAsync(IFormFile? file, CancellationToken ct = default);
}
