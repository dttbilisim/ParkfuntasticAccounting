using System.Text;
using System.Text.Json;
using Dega.Abstract;
using Dega.Dtos;
using Dega.Options;
using ecommerce.Domain.Shared.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dega.Concreate;

public class DegaService : IDegaService, IRealTimeStockProvider
{
    private readonly HttpClient _httpClient;
    private readonly DegaApiOptions _options;
    private readonly ILogger<DegaService> _logger;
    private string? _token;

    public int SellerId => 3;

    public DegaService(HttpClient httpClient, IOptions<DegaApiOptions> options, ILogger<DegaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetStockAsync(string productCode, string? sourceId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(productCode)) return "Stok Yok";

            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return "Hata: Token Alınamadı";

            var requestDto = new ProductListRequestDto
            {
                ProductCode = productCode,
                PageNo = 1,
                PageLen = 100
            };

            var products = await GetProductsAsync(token, requestDto);
            
            if (products == null || !products.Any())
            {
                // Eğer tam kod ile bulunamadıysa SearchText ile deneyelim (belki parça kodu aramasıdır)
                requestDto.ProductCode = "";
                requestDto.SearchText = productCode;
                products = await GetProductsAsync(token, requestDto);
            }

            if (products != null && products.Any())
            {
                var product = products.First();
                return FormatStockResponse(product);
            }

            return "Stok Yok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dega GetStockAsync error for code: {ProductCode}", productCode);
            return "Hata";
        }
    }

    private string FormatStockResponse(ProductResponse product)
    {
        var stocks = new List<string>();
        
        // Depo alanlarını kontrol et (Dega DTO'sunda Depo1..6 ve Depo_1..6 var)
        if (!string.IsNullOrEmpty(product.Depo1) && product.Depo1 != "0") stocks.Add($"Depo1: {product.Depo1}");
        else if (!string.IsNullOrEmpty(product.Depo_1) && product.Depo_1 != "0") stocks.Add($"Depo1: {product.Depo_1}");

        if (!string.IsNullOrEmpty(product.Depo2) && product.Depo2 != "0") stocks.Add($"Depo2: {product.Depo2}");
        else if (!string.IsNullOrEmpty(product.Depo_2) && product.Depo_2 != "0") stocks.Add($"Depo2: {product.Depo_2}");

        if (!string.IsNullOrEmpty(product.Depo3) && product.Depo3 != "0") stocks.Add($"Depo3: {product.Depo3}");
        else if (!string.IsNullOrEmpty(product.Depo_3) && product.Depo_3 != "0") stocks.Add($"Depo3: {product.Depo_3}");

        if (!string.IsNullOrEmpty(product.Depo4) && product.Depo4 != "0") stocks.Add($"Depo4: {product.Depo4}");
        else if (!string.IsNullOrEmpty(product.Depo_4) && product.Depo_4 != "0") stocks.Add($"Depo4: {product.Depo_4}");

        if (!string.IsNullOrEmpty(product.Depo5) && product.Depo5 != "0") stocks.Add($"Depo5: {product.Depo5}");
        else if (!string.IsNullOrEmpty(product.Depo_5) && product.Depo_5 != "0") stocks.Add($"Depo5: {product.Depo_5}");

        if (!string.IsNullOrEmpty(product.Depo6) && product.Depo6 != "0") stocks.Add($"Depo6: {product.Depo6}");
        else if (!string.IsNullOrEmpty(product.Depo_6) && product.Depo_6 != "0") stocks.Add($"Depo6: {product.Depo_6}");

        if (stocks.Any()) return string.Join(", ", stocks);
        
        // Eğer hiçbir depo bilgisi yoksa ama ürün listede geldiyse "VAR" diyebiliriz (hibrit mantık için)
        return "VAR";
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_token)) return _token;

        var request = new
        {
            Email = _options.Email,
            Password = _options.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/login", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Dega Login failed: {StatusCode}", response.StatusCode);
            return string.Empty;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        
        if (doc.RootElement.TryGetProperty("d", out var tokenElement))
        {
            _token = tokenElement.GetString();
            return _token ?? string.Empty;
        }

        return string.Empty;
    }

    public async Task<List<ProductResponse>> GetProductsAsync(string token, ProductListRequestDto request)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/getProductListService", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _token = null; // Token geçersizleşmiş olabilir
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ProductResponseWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return wrapper?.data ?? new List<ProductResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dega GetProductsAsync error");
            return new List<ProductResponse>();
        }
    }

    public async Task<DegaAddToCartResponseDto?> AddToCartAsync(List<DegaCartItemDto> items)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Dega AddToCart - Token alınamadı");
                return null;
            }

            var request = new DegaAddToCartRequestDto { Items = items };
            var jsonContent = JsonSerializer.Serialize(request);
            
            _logger.LogInformation("Dega AddToCart - Items to add: {ItemCount}, JSON: {Json}", items.Count, jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/custom_add_basket", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Dega AddToCart Response ({StatusCode}): {Content}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dega AddToCart failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<DegaAddToCartResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dega AddToCart exception");
            return null;
        }
    }

    public async Task<CustomOrderResponseDto?> CreateOrderAsync(CustomOrderRequestDto requestDto)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Dega CreateOrder - Token alınamadı");
                return null;
            }

            var jsonContent = JsonSerializer.Serialize(requestDto);
            
            _logger.LogInformation("Dega CreateOrder - Request JSON: {Json}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/custom_order", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Dega CreateOrder Response ({StatusCode}): {Content}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dega CreateOrder failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<CustomOrderResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dega CreateOrder exception");
            return null;
        }
    }

    private class ProductResponseWrapper
{
    public List<ProductResponse> data { get; set; }
}
}
