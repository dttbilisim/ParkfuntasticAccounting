using System.Text;
using System.Text.Json;
using ecommerce.Domain.Shared.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remar.Abstract;
using Remar.Dtos;
using Remar.Options;

namespace Remar.Concreate;
public class RemarApiService : IRemarApiService, IRealTimeStockProvider
{
    private readonly HttpClient _httpClient;
    private readonly RemarApiOptions _options;
    private readonly ILogger<RemarApiService> _logger;
    private string? _token;

    public int SellerId => 4;

    public RemarApiService(HttpClient httpClient, IOptions<RemarApiOptions> options, ILogger<RemarApiService> logger)
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

            var requestDto = new RemarProductListRequestDto
            {
                ProductCode = productCode,
                PageNo = 1,
                PageLen = 100
            };

            var products = await GetProductsAsync(token, requestDto);
            
            if (products == null || !products.Any())
            {
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
            _logger.LogError(ex, "Remar GetStockAsync error for code: {ProductCode}", productCode);
            return "Hata";
        }
    }

    private string FormatStockResponse(RemarProductDto product)
    {
        var stocks = new List<string>();
        
        if (!string.IsNullOrEmpty(product.Depo_1) && product.Depo_1 != "0") stocks.Add($"Depo_1: {product.Depo_1}");
        if (!string.IsNullOrEmpty(product.Depo_2) && product.Depo_2 != "0") stocks.Add($"Depo_2: {product.Depo_2}");

        if (stocks.Any()) return string.Join(", ", stocks);
        
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
            _logger.LogError("Remar Login failed: {StatusCode}", response.StatusCode);
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
    public async Task<List<RemarProductDto>> GetProductsAsync(string token, RemarProductListRequestDto request)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/getProductListService", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _token = null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<RemarProductResponseWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return wrapper?.data ?? new List<RemarProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remar GetProductsAsync error");
            return new List<RemarProductDto>();
        }
    }

    public async Task<RemarAddToCartResponseDto?> AddToCartAsync(List<RemarCartItemDto> items)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Remar AddToCart - Token alınamadı");
                return null;
            }

            var request = new RemarAddToCartRequestDto { Items = items };
            var jsonContent = JsonSerializer.Serialize(request);
            
            _logger.LogInformation("Remar AddToCart - Items to add: {ItemCount}, JSON: {Json}", items.Count, jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/custom_add_basket", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Remar AddToCart Response ({StatusCode}): {Content}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Remar AddToCart failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<RemarAddToCartResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remar AddToCart exception");
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
                _logger.LogError("Remar CreateOrder - Token alınamadı");
                return null;
            }

            var jsonContent = JsonSerializer.Serialize(requestDto);
            
            _logger.LogInformation("Remar CreateOrder - Request JSON: {Json}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("SessionId", token);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl.TrimEnd('/')}/api/custom_order", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Remar CreateOrder Response ({StatusCode}): {Content}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Remar CreateOrder failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<CustomOrderResponseDto>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remar CreateOrder exception");
            return null;
        }
    }

    private class RemarProductResponseWrapper
    {
        public List<RemarProductDto> data { get; set; }
    }
}
