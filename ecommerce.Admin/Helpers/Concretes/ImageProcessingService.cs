using ImageMagick;
namespace ecommerce.Admin.Helpers.Concretes;
public class ImageProcessingService
{
    private readonly HttpClient _httpClient;

    public ImageProcessingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Stream> DownloadAndRemoveWatermarkAsync(string imageUrl)
    {
        // Download the image
        var response = await _httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode();
        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        using (var inputStream = new MemoryStream(imageBytes))
        using (var image = new MagickImage(inputStream))
        {
            // Example of watermark removal: Crop out watermark (you may need to adjust this part)
            var watermarkRegion = new MagickGeometry(10, 10, 100, 50); // Define the region where watermark is located
            image.Crop(watermarkRegion);

            // Save the processed image to a stream
            var outputStream = new MemoryStream();
            image.Write(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
