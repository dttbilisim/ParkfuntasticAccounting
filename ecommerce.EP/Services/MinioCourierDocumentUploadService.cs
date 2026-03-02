using ecommerce.EP.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace ecommerce.EP.Services;

/// <summary>
/// Kurye belgelerini MinIO'ya yükler. Başarısız olursa (örn. connection refused) dosya sistemine fallback yapar.
/// </summary>
public class MinioCourierDocumentUploadService : ICourierDocumentUploadService
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".heic" };

    private readonly IMinioClient _minio;
    private readonly MinioOptions _options;
    private readonly CourierDocumentUploadService _fileFallback;
    private readonly ILogger<MinioCourierDocumentUploadService> _logger;

    public MinioCourierDocumentUploadService(
        IMinioClient minio,
        IOptions<MinioOptions> options,
        CourierDocumentUploadService fileFallback,
        ILogger<MinioCourierDocumentUploadService> logger)
    {
        _minio = minio;
        _options = options.Value;
        _fileFallback = fileFallback;
        _logger = logger;
    }

    public async Task<string?> SaveAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return null;

        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("Courier document too large: {Size} bytes", file.Length);
            return null;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            _logger.LogWarning("Courier document invalid extension: {Name}", file.FileName);
            return null;
        }

        var prefix = _options.ObjectPrefix?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(prefix))
            prefix = "courier-documents";
        var objectKey = $"{prefix}/{Guid.NewGuid():N}{ext}";

        try
        {
            await using var stream = file.OpenReadStream();
            var putArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType ?? "application/octet-stream");
            await _minio.PutObjectAsync(putArgs, ct).ConfigureAwait(false);
            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO upload failed, falling back to file storage: {Key}", objectKey);
            return await _fileFallback.SaveAsync(file, ct).ConfigureAwait(false);
        }
    }
}
