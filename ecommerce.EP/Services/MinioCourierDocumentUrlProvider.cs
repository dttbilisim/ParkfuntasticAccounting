using ecommerce.Core.Helpers;
using ecommerce.EP.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace ecommerce.EP.Services;

/// <summary>
/// MinIO'da saklanan belgeler için presigned URL; fallback ile diske kaydedilenler için FileHelper kullanır.
/// </summary>
public class MinioCourierDocumentUrlProvider : ICourierDocumentUrlProvider
{
    private readonly IMinioClient _minio;
    private readonly MinioOptions _options;
    private readonly FileHelper _fileHelper;
    private readonly ILogger<MinioCourierDocumentUrlProvider> _logger;
    private static readonly TimeSpan PresignedExpiry = TimeSpan.FromMinutes(15);

    public MinioCourierDocumentUrlProvider(
        IMinioClient minio,
        IOptions<MinioOptions> options,
        FileHelper fileHelper,
        ILogger<MinioCourierDocumentUrlProvider> logger)
    {
        _minio = minio;
        _options = options.Value;
        _fileHelper = fileHelper;
        _logger = logger;
    }

    /// <summary>
    /// Presigned URL için kullanılacak client: EndpointForPresignedUrls ayarlıysa o host/port ile
    /// yeni bir client (imza doğru host ile hesaplansın diye), değilse mevcut singleton.
    /// </summary>
    private IMinioClient GetClientForPresignedUrl()
    {
        if (string.IsNullOrWhiteSpace(_options.EndpointForPresignedUrls))
            return _minio;

        var port = _options.PortForPresignedUrls ?? _options.Port;
        var useSsl = _options.UseSSLForPresignedUrls ?? _options.UseSSL;
        return new MinioClient()
            .WithEndpoint(_options.EndpointForPresignedUrls!, port)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task<string?> GetDocumentUrlAsync(string? path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Fallback ile diske kaydedilen belgeler (CourierDocuments/...)
        if (path.StartsWith("CourierDocuments/", StringComparison.OrdinalIgnoreCase))
            return _fileHelper.GetFileUrl(path);

        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(path)
                .WithExpiry((int)PresignedExpiry.TotalSeconds);

            // Presigned URL imzası, URL'deki host ile hesaplanır. Tarayıcı linki dış IP (92.x.x.x) ile
            // açacaksa, imzanın da o host ile üretilmesi gerekir; yoksa SignatureDoesNotMatch hatası oluşur.
            var clientToUse = GetClientForPresignedUrl();
            var url = await clientToUse.PresignedGetObjectAsync(args).ConfigureAwait(false);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO presigned URL oluşturulamadı: {Path}", path);
            return null;
        }
    }
}
