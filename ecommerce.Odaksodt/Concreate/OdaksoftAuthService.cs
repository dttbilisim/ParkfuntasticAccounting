using System.Text;
using System.Text.Json;
using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Auth;
using ecommerce.Odaksodt.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ecommerce.Odaksodt.Concreate;

/// <summary>
/// Odaksoft kimlik doğrulama servisi implementasyonu.
/// Döngüsel bağımlılığı önlemek için kendi HttpClient'ını kullanır.
/// </summary>
public class OdaksoftAuthService : IOdaksoftAuthService
{
    private readonly HttpClient _httpClient;
    private readonly OdaksoftOptions _options;
    private readonly ILogger<OdaksoftAuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _currentToken;
    private string? _currentRefreshToken;
    private DateTime _tokenExpiresAt;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public OdaksoftAuthService(
        HttpClient httpClient,
        IOptions<OdaksoftOptions> options,
        ILogger<OdaksoftAuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
            // PropertyNamingPolicy kaldırıldı - [JsonPropertyName] attribute'ları kullanıyoruz
        };
    }

    public async Task<LoginResponseDto> LoginAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Odaksoft login işlemi başlatılıyor");

            var request = new LoginRequestDto
            {
                Username = _options.Username,
                Password = _options.Password
            };

            var response = await PostJsonAsync<LoginRequestDto, LoginResponseDto>(
                "/api/IntegrationKullanici/Login", request, cancellationToken);

            // Debug: Status ve Token değerlerini logla
            _logger.LogInformation("Login Response Debug - Status: {Status}, JwtToken: {JwtToken}, AccessToken: {AccessToken}, Token property: {Token}", 
                response?.Status, 
                response?.JwtToken != null ? "var" : "null",
                response?.JwtToken?.AccessToken != null ? "var" : "null",
                response?.Token != null ? "var" : "null");

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _currentToken = response.Token;
                _currentRefreshToken = response.RefreshToken;
                _tokenExpiresAt = response.ExpiresAt;
                _logger.LogInformation("Login başarılı. Token geçerlilik süresi: {ExpiresAt}", _tokenExpiresAt);
            }
            else
            {
                _logger.LogWarning("Login başarısız. Response: Success={Success}, Status={Status}, Token={Token}, ErrorMessage={ErrorMessage}", 
                    response?.Success, 
                    response?.Status,
                    string.IsNullOrEmpty(response?.Token) ? "null/empty" : "var", 
                    response?.ErrorMessage ?? "null");
            }

            return response ?? new LoginResponseDto { Status = false, Message = "Beklenmeyen hata" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login işlemi sırasında hata oluştu");
            return new LoginResponseDto { Status = false, Message = $"Login hatası: {ex.Message}" };
        }
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Token yenileme işlemi başlatılıyor");

            var request = new RefreshTokenRequestDto { RefreshToken = refreshToken };

            var response = await PostJsonAsync<RefreshTokenRequestDto, LoginResponseDto>(
                "/api/IntegrationKullanici/RefreshToken", request, cancellationToken);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _currentToken = response.Token;
                _currentRefreshToken = response.RefreshToken;
                _tokenExpiresAt = response.ExpiresAt;
                _logger.LogInformation("Token yenileme başarılı. Yeni geçerlilik süresi: {ExpiresAt}", _tokenExpiresAt);
            }
            else
            {
                _logger.LogWarning("Token yenileme başarısız: {ErrorMessage}", response?.ErrorMessage);
            }

            return response ?? new LoginResponseDto { Status = false, Message = "Beklenmeyen hata" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token yenileme sırasında hata oluştu");
            return new LoginResponseDto { Status = false, Message = $"Token yenileme hatası: {ex.Message}" };
        }
    }

    public async Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrEmpty(_currentToken) || DateTime.UtcNow >= _tokenExpiresAt.AddMinutes(-5))
            {
                _logger.LogInformation("Token geçersiz veya süresi dolmak üzere, yeni token alınıyor");

                LoginResponseDto response;
                if (!string.IsNullOrEmpty(_currentRefreshToken))
                {
                    response = await RefreshTokenAsync(_currentRefreshToken, cancellationToken);
                    if (!response.Success)
                    {
                        _logger.LogWarning("Refresh token başarısız, yeniden login yapılıyor");
                        response = await LoginAsync(cancellationToken);
                    }
                }
                else
                {
                    response = await LoginAsync(cancellationToken);
                }

                if (!response.Success || string.IsNullOrEmpty(response.Token))
                {
                    throw new InvalidOperationException($"Token alınamadı: {response.ErrorMessage}");
                }
            }

            return _currentToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Auth servisi için basit POST JSON helper metodu
    /// </summary>
    private async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // Raw response'u logla (debugging için)
        _logger.LogInformation("Odaksoft API Response ({Endpoint}): {ResponseContent}", endpoint, responseContent);
        
        return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
    }
}
