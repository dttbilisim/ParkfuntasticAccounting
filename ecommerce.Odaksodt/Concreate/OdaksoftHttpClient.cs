using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Common;
using ecommerce.Odaksodt.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ecommerce.Odaksodt.Concreate;

/// <summary>
/// Odaksoft HTTP client implementasyonu
/// </summary>
public class OdaksoftHttpClient : IOdaksoftHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly OdaksoftOptions _options;
    private readonly ILogger<OdaksoftHttpClient> _logger;
    private readonly IOdaksoftAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public OdaksoftHttpClient(
        HttpClient httpClient,
        IOptions<OdaksoftOptions> options,
        ILogger<OdaksoftHttpClient> logger,
        IOdaksoftAuthService authService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _authService = authService;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET request gönderiliyor: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request hatası: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("POST request gönderiliyor: {Endpoint}", endpoint);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request hatası: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostWithAuthAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Token'ı al (gerekirse yenile)
            var token = await _authService.GetValidTokenAsync(cancellationToken);

            _logger.LogInformation("Authenticated POST request gönderiliyor: {Endpoint}", endpoint);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Authorization header ekle
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Request body'yi logla (debugging için)
            _logger.LogInformation("Request Body ({Endpoint}): {RequestBody}", endpoint, json);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Hata durumunda response body'den mesajı çıkar ve anlamlı exception fırlat
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API Hatası - Status: {StatusCode}, Endpoint: {Endpoint}, Response: {Response}", 
                    (int)response.StatusCode, endpoint, responseContent);
                
                // Odaksoft API hata formatını parse et: {"status":false,"message":"...","exceptionMessage":"..."}
                string hataMesaji = $"API hatası ({(int)response.StatusCode})";
                try
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        var errorDto = JsonSerializer.Deserialize<OdaksoftErrorResponseDto>(responseContent, _jsonOptions);
                        if (errorDto != null && !string.IsNullOrWhiteSpace(errorDto.Message))
                        {
                            hataMesaji = errorDto.Message;
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "API hata response'u parse edilemedi: {Endpoint}", endpoint);
                }
                
                throw new HttpRequestException(hataMesaji);
            }
            
            // Başarılı response'u logla
            _logger.LogInformation("API Başarılı - Status: {StatusCode}, Endpoint: {Endpoint}, Response: {Response}", 
                (int)response.StatusCode, endpoint, responseContent);

            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authenticated POST request hatası: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<byte[]> DownloadBinaryAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            // Token'ı al
            var token = await _authService.GetValidTokenAsync(cancellationToken);

            _logger.LogInformation("Binary download başlatılıyor: {Endpoint}", endpoint);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary download hatası: {Endpoint}", endpoint);
            throw;
        }
    }
}
