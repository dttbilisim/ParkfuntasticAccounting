using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtoIsmail.Abstract;
using ecommerce.Domain.Shared.Options;
namespace OtoIsmail.Concreate;
public class TokenService : ITokenService{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenService> _logger;
    private readonly OtoIsmailOptions _options;

    public TokenService(HttpClient httpClient, IOptions<OtoIsmailOptions> options, ILogger<TokenService> logger){
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(){
        var client = _httpClient;
        var url = $"{_options.BaseUrl}/api/OISML/Login?pUsername={_options.Username}&pPwd={_options.Password}";

        try{
            // Debug için URL'i logluyoruz (password maskelenmiş)
            var debugUrl = url.Replace(_options.Password, "***");
            _logger.LogInformation("[TokenService] İstek atılıyor: {Url}", debugUrl);

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[TokenService] API Yanıtı: {Json}", json); // API ne dönüyor görelim
            using var doc = JsonDocument.Parse(json);

            if(doc.RootElement.TryGetProperty("Data", out var dataElement)){
                var token = dataElement.GetString();
                return token ?? throw new Exception("Token null geldi.");
            } else{
                throw new Exception("Data alanı bulunamadı.");
            }
        } catch(Exception ex){
            _logger.LogError(ex, "Token alınırken hata oluştu. BaseUrl: {BaseUrl}, Username: {Username}", _options.BaseUrl, _options.Username);
            throw;
        }
    }
}