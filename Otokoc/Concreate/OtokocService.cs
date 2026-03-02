using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using Otokoc.Abstract;
using Otokoc.Dto;
using Otokoc.Options;
namespace Otokoc.Concreate;
public class OtokocService : IOtokocService
{
    private readonly HttpClient _httpClient;
    private readonly OtokocOptions _options;

    public OtokocService(IOptions<OtokocOptions> options)
    {
        _options = options.Value;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
    }

    public async Task<List<Product>> GetAllProductsByPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var allProducts = new List<Product>();
        int skip = pageIndex * pageSize;

        string url = $"{_options.BaseUrl}/GetAllProductsByParts/{skip}/{pageSize}";
        using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);

        var serializer = new XmlSerializer(typeof(Result<ProductList>));
        var result = (Result<ProductList>)serializer.Deserialize(stream)!;

        allProducts.AddRange(result.ResultObject.Products);

        foreach (var product in result.ResultObject.Products)
        {
            try
            {
                string imagePath = Path.Combine(_options.ImageSavePath, $"{product.ProductCode}.jpg");
                var imageData = await _httpClient.GetByteArrayAsync(product.ImageUrl, cancellationToken);
                await File.WriteAllBytesAsync(imagePath, imageData, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resim indirilemedi: {product.ProductCode} - {ex.Message}");
            }
        }

        return allProducts;
    }
}