using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ecommerce.Core.Entities;
using Microsoft.Extensions.Logging;
using OtoIsmail.Abstract;
using OtoIsmail.Dtos;

namespace OtoIsmail.Concreate;

public class ApiClient(HttpClient httpClient, ITokenService tokenService, ILogger<ApiClient> logger) : IApiClient
{
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var token = await tokenService.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Token alınamadı.");

        var requestJson = JsonSerializer.Serialize(data);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.PostAsync(url, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("PostAsync Error: Url={Url} Code={Code} Content={Content}", url, response.StatusCode, content);
            return default;
        }

        var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<TResponse>(responseStream);
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        var token = await tokenService.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Token alınamadı.");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("GetAsync Error: Url={Url} Code={Code} Content={Content}", url, response.StatusCode, content);
            return default;
        }

        var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<TResponse>(responseStream);
    }

    public async Task<List<BrandDto>?> GetBrandsAsync()
    {
        try
        {
            var token = await tokenService.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Token alınamadı.");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://ws.otoismail.com.tr/api/OISML/GetBrands");
            request.Headers.Add("token", token);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("[GetBrandsAsync] Hata: StatusCode={StatusCode}, Reason={Reason}, Content={Content}", response.StatusCode, response.ReasonPhrase, errorContent);
                return default;
            }


            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Data", out var dataElement))
            {
                var brands = JsonSerializer.Deserialize<List<BrandDto>>(dataElement.GetRawText());
                return brands;
            }

            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GetBrandsAsync] Exception: {Message}", ex.Message);
            return default;
        }
    }
    public async Task<List<ProductOtoIsmail>?> GetProductsAsync(string brand, string? tarih = null)
    {
        try
        {
            var token = await tokenService.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Token alınamadı.");

            // FIXED: API dokümantasyonuna göre tarih parametresi zorunlu
            // "tarih parametresinde belirlenen tarihten sonra değişen stok kartlarını listeler"
            // Tüm ürünler için çok eski bir tarih (19000101) gönderilmeli
            // Eğer tarih null/boşsa, tüm ürünler için 19000101 kullan
            var tarihParam = string.IsNullOrWhiteSpace(tarih) ? "19000101" : tarih;
            var url = $"http://ws.otoismail.com.tr/api/OISML/GetProducts?pBrand={brand}&pTarih={tarihParam}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("token", token);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("[GetProductsAsync] Hata: StatusCode={StatusCode}, Reason={Reason}, Content={Content}", response.StatusCode, response.ReasonPhrase, errorContent);
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Data", out var dataElement))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var productDtos = JsonSerializer.Deserialize<List<ProductOtoIsmailDto>>(dataElement.GetRawText(), options);
                if (productDtos == null)
                    return default;

                var products = productDtos.Select(p => new ProductOtoIsmail
                {
                    NetsisStokId = p.NetsisStokId,
                    Kod = p.Kod,
                    OrjinalKod = p.OrjinalKod,
                    Ad = p.Ad,
                    Marka = p.Marka,
                    MarkaFull = p.MarkaFull,
                    Birim = p.Birim,
                    GrupKodu = p.GrupKodu,
                    Barkod1 = p.Barkod1,
                    Barkod2 = p.Barkod2,
                    Barkod3 = p.Barkod3,
                    ImageUrl = p.ImageUrl,
                    // FIXED: Fiyat1 ve ParaBirimi1 - Fiyat1 objesi null değilse ParaBirimi1'i de set et (Deger 0 olsa bile)
                    Fiyat1 = p.Fiyat1?.Deger ?? 0,
                    ParaBirimi1 = (!string.IsNullOrWhiteSpace(p.Fiyat1?.ParaBirimi) ? p.Fiyat1.ParaBirimi : p.ParaBirimi),
                    Fiyat2 = p.Fiyat2?.Deger ?? 0,
                    // FIXED: Fiyat3 ve ParaBirimi3 - Fiyat3 objesi null değilse ParaBirimi3'ü de set et (Deger 0 olsa bile)
                    Fiyat3 = p.Fiyat3?.Deger ?? 0,
                    ParaBirimi3 = (!string.IsNullOrWhiteSpace(p.Fiyat3?.ParaBirimi) ? p.Fiyat3.ParaBirimi : p.ParaBirimi),
                    Fiyat4 = p.Fiyat4?.Deger ?? 0,
                    KDV = p.KDV,
                    Oem = p.Oem,
                    Payda = p.Payda,
                    StokSayisi = p.StokSayisi,
                    Plaza = p.Plaza,
                    Gebze = p.Gebze,
                    Ankara = p.Ankara,
                    Ikitelli = p.Ikitelli,
                    Izmir = p.Izmir,
                    Samsun = p.Samsun,
                    Nakliye = p.Nakliye,
                    Depo1030 = p.Depo1030,
                    Depo13 = p.Depo13,
                    ParaBirimi = p.ParaBirimi,
                    CreatedDate = DateTime.UtcNow,
                    CreatedId = 1
                }).ToList();

                return products;
            }

            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GetProductsAsync] Exception: {Message}", ex.Message);
            return default;
        }
    }
}