using ImageDownload.Abstract;
using ImageDownload.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace ImageDownload.Concreate;
public class ImageStorageService : IImageStorageService
{
    private readonly GoogleImageFetcherOptions _options;
    private readonly ILogger<ImageStorageService> _logger;
    private readonly HttpClient _httpClient;

    public ImageStorageService(IOptions<GoogleImageFetcherOptions> options, ILogger<ImageStorageService> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string?> SaveImageAsync(string imageUrl, string fileName)
    {
        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(imageUrl);

            Directory.CreateDirectory(_options.ImageSaveRootPath);

            var filePath = Path.Combine(_options.ImageSaveRootPath, fileName + ".jpg");
            await File.WriteAllBytesAsync(filePath, bytes);

            return Path.Combine(_options.ImageBaseUrl, fileName + ".jpg").Replace("\\", "/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image from URL: {ImageUrl}", imageUrl);
            return null;
        }
    }
}
