using System.Net.Http.Headers;
using System.Net.Http.Json;
using BasbugOto.Abstract;
using BasbugOto.Dtos;
using BasbugOto.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace BasbugOto.Concreate;
public class BasbugApiService : IBasbugApiService
{
    private readonly HttpClient _httpClient;
    private readonly BasbugOptions _options;
    private readonly ILogger<BasbugApiService> _logger;

    public BasbugApiService(HttpClient httpClient, IOptions<BasbugOptions> options, ILogger<BasbugApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync()
    {
        var request = new
        {
            kullaniciAdi = _options.KullaniciAdi,
            parola = _options.Parola,
            clientSecret = _options.ClientSecret,
            clientID = _options.ClientID
        };

        var response = await _httpClient.PostAsJsonAsync($"{_options.BaseUrl}/auth/login", request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Basbug token alınamadı. StatusCode: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadFromJsonAsync<BasbugTokenResponse>();
        return content?.Token;
    }
    public async Task<List<BasbugGroupDto>> GetGroupsAsync(){
        var token = await GetTokenAsync();
        var url = $"{_options.BaseUrl}/material/ListeGrubuGetir?FirmaAdi=BASBUG";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Grup listesi alınamadı. StatusCode: {StatusCode}", response.StatusCode);
            return new();
        }

        var content = await response.Content.ReadFromJsonAsync<BasbugGroupResponseDto>();
        return content?.MalzemeGruplariListesi ?? new();
    }
    public async Task<List<BasbugProductDto>> GetProductsByGroupAsync(string grupKodu, string depo){
        var token = await GetTokenAsync();
        var url = $"{_options.BaseUrl}/material/MalzemeleriGetir?ListeGrubu={grupKodu}&FirmaAdi=BASBUG&Depo={depo}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Malzeme listesi alınamadı. StatusCode: {StatusCode}", response.StatusCode);
            return new();
        }

        var content = await response.Content.ReadFromJsonAsync<BasbugProductResponseDto>();
        return content?.MalzemeListesi ?? new();
    }
    public async Task<List<BasbugStockDto>> GetStockByGroupAsync(string grupKodu, string depo)
    {
        var token = await GetTokenAsync();
        var url = $"{_options.BaseUrl}/material/StokGetir?ListeGrubu={grupKodu}&FirmaAdi=BASBUG&Depo={depo}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stok bilgisi alınamadı. StatusCode: {StatusCode}", response.StatusCode);
            return new();
        }

        var content = await response.Content.ReadFromJsonAsync<BasbugStockResponseDto>();
        return content?.StokListesi ?? new();
    }
}