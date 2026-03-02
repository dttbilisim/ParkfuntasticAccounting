using System.Net.Http.Headers;
using System.Text;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.OtoIsmail;
using ecommerce.Domain.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EFCore.Context;
using ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Services;

public class OtoIsmailService(HttpClient httpClient, ILogger<OtoIsmailService> logger, IOptions<OtoIsmailOptions> options, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, IUnitOfWork<ApplicationDbContext>? context = null) : IOtoIsmailService, IRealTimeStockProvider
{
    private readonly OtoIsmailOptions _options = options.Value;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache = cache;
    private readonly IUnitOfWork<ApplicationDbContext>? _context = context;
    public int SellerId => 1;

    public async Task<string> GetStockAsync(string productCode, string? sourceId = null)
    {
        if (string.IsNullOrEmpty(productCode)) return "Stok Yok";

        logger.LogInformation("OtoIsmail GetStockAsync: productCode={ProductCode}, sourceId={SourceId}, hasContext={HasContext}", 
            productCode, sourceId, _context != null);

        try
        {
            // Kod üzerinden sorgulama (Bazı kodlar | ile ayrılmış liste olabilir)
            var codes = productCode.Split('|', StringSplitOptions.RemoveEmptyEntries);
            bool apiSuccess = false;
            
            foreach (var code in codes)
            {
                var trimmedCode = code.Trim();
                var result = await GetStockByCodeAsync(trimmedCode);
                
                if (result?.Result?.Success == true && result.Data != null && result.Data.Any())
                {
                    apiSuccess = true;
                    var formattedStock = FormatStockResponse(result.Data);
                    // If API returns "Stok Yok", try fallback
                    if (formattedStock == "Stok Yok")
                    {
                        logger.LogInformation("OtoIsmail GetStockAsync: API returned 'Stok Yok', trying fallback for sourceId={SourceId}", sourceId);
                        var fallbackStock = await GetStockFromDatabaseAsync(sourceId);
                        if (!string.IsNullOrEmpty(fallbackStock) && fallbackStock != "Stok Yok")
                        {
                            logger.LogInformation("OtoIsmail GetStockAsync: Fallback successful, returning: {Stock}", fallbackStock);
                            return fallbackStock;
                        }
                    }
                    return formattedStock;
                }
            }

            // API call failed or returned no data, try fallback
            if (!apiSuccess)
            {
                logger.LogInformation("OtoIsmail GetStockAsync: API call failed or no data, trying fallback for sourceId={SourceId}", sourceId);
            }
            
            var dbStock = await GetStockFromDatabaseAsync(sourceId);
            if (!string.IsNullOrEmpty(dbStock) && dbStock != "Stok Yok")
            {
                logger.LogInformation("OtoIsmail GetStockAsync: Fallback successful, returning: {Stock}", dbStock);
                return dbStock;
            }

            logger.LogWarning("OtoIsmail GetStockAsync: Both API and fallback failed, returning 'Stok Yok'");
            return "Stok Yok";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OtoIsmail GetStockAsync Exception for productCode: {ProductCode}, sourceId: {SourceId}", productCode, sourceId);
            
            // On exception, try fallback to database
            logger.LogInformation("OtoIsmail GetStockAsync: Exception occurred, trying fallback for sourceId={SourceId}", sourceId);
            var dbStock = await GetStockFromDatabaseAsync(sourceId);
            if (!string.IsNullOrEmpty(dbStock) && dbStock != "Stok Yok")
            {
                logger.LogInformation("OtoIsmail GetStockAsync: Fallback successful after exception, returning: {Stock}", dbStock);
                return dbStock;
            }

            return "Stok Yok";
        }
    }

    private string FormatStockResponse(List<OtoIsmailStockInfoDto> data)
    {
        var resultList = new List<string>();
        foreach (var item in data)
        {
            var stocks = new List<string>();
            
            if (item.StokSayisi > 0) stocks.Add($"Plaza: {item.StokSayisi}");
            if (item.Plaza > 0) stocks.Add($"Plaza (Eski): {item.Plaza}");
            if (item.Gebze > 0) stocks.Add($"Gebze: {item.Gebze}");
            if (item.Ankara > 0) stocks.Add($"Ankara: {item.Ankara}");
            if (item.Ikitelli > 0) stocks.Add($"İkitelli: {item.Ikitelli}");
            if (item.Izmir > 0) stocks.Add($"İzmir: {item.Izmir}");
            if (item.Samsun > 0) stocks.Add($"Samsun: {item.Samsun}");
            if (item.Depo1030 > 0) stocks.Add($"Depo1030: {item.Depo1030}");
            if (item.Depo13 > 0) stocks.Add($"Depo13: {item.Depo13}");

            if (stocks.Any())
            {
                resultList.Add(string.Join(", ", stocks));
            }
            else if (!string.IsNullOrEmpty(item.Miktar) && item.Miktar == "VAR")
            {
                // Eğer detay yoksa ama VAR ise, "VAR" olarak kalsın (OrderService bunu hibrit ile tamamlayacak)
                resultList.Add("VAR");
            }
        }

        return resultList.Any() ? string.Join(" | ", resultList) : "Stok Yok";
    }

    private async Task<string> GetStockFromDatabaseAsync(string? sourceId)
    {
        // If no context or sourceId, cannot query database
        if (_context == null)
        {
            logger.LogWarning("OtoIsmail GetStockFromDatabaseAsync: Context is null, cannot query database");
            return "Stok Yok";
        }
        
        if (string.IsNullOrEmpty(sourceId))
        {
            logger.LogWarning("OtoIsmail GetStockFromDatabaseAsync: sourceId is null or empty, cannot query database");
            return "Stok Yok";
        }

        try
        {
            // Parse sourceId to int (ProductOtoIsmail.Id)
            if (!int.TryParse(sourceId, out int pOismlId))
            {
                logger.LogWarning("OtoIsmail GetStockFromDatabaseAsync: Invalid sourceId format: {SourceId}", sourceId);
                return "Stok Yok";
            }

            // First get the specific item to find NetsisStokId
            var pOisml = await _context.DbContext.ProductOtoIsmails
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pOismlId);

            if (pOisml == null)
            {
                logger.LogWarning("OtoIsmail GetStockFromDatabaseAsync: ProductOtoIsmail not found for Id: {Id}", pOismlId);
                return "Stok Yok";
            }

            // Calculate fallback using Max aggregation across potential duplicates (same NetsisStokId)
            var matchingProducts = await _context.DbContext.ProductOtoIsmails
                .AsNoTracking()
                .Where(p => p.NetsisStokId == pOisml.NetsisStokId)
                .ToListAsync();

            if (!matchingProducts.Any())
            {
                return "Stok Yok";
            }

            int gebze = matchingProducts.Max(p => p.Gebze ?? 0);
            int ankara = matchingProducts.Max(p => p.Ankara ?? 0);
            int ikitelli = matchingProducts.Max(p => p.Ikitelli ?? 0);
            int izmir = matchingProducts.Max(p => p.Izmir ?? 0);
            int samsun = matchingProducts.Max(p => p.Samsun ?? 0);
            int depo1030 = matchingProducts.Max(p => p.Depo1030 ?? 0);
            int depo13 = matchingProducts.Max(p => p.Depo13 ?? 0);

            int totalStock = gebze + ankara + ikitelli + izmir + samsun + depo1030 + depo13;

            if (totalStock > 0)
            {
                return FormatStockFromDb(gebze, ankara, ikitelli, izmir, samsun, depo1030, depo13);
            }

            return "Stok Yok";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OtoIsmail GetStockFromDatabaseAsync Exception for sourceId: {SourceId}", sourceId);
            return "Stok Yok";
        }
    }

    private string FormatStockFromDb(int gebze, int ankara, int ikitelli, int izmir, int samsun, int depo1030, int depo13)
    {
        var stocks = new List<string>();

        if (gebze > 0) stocks.Add($"Gebze: {gebze}");
        if (ankara > 0) stocks.Add($"Ankara: {ankara}");
        if (ikitelli > 0) stocks.Add($"İkitelli: {ikitelli}");
        if (izmir > 0) stocks.Add($"İzmir: {izmir}");
        if (samsun > 0) stocks.Add($"Samsun: {samsun}");
        if (depo1030 > 0) stocks.Add($"Depo1030: {depo1030}");
        if (depo13 > 0) stocks.Add($"Depo13: {depo13}");

        return stocks.Any() ? string.Join(", ", stocks) : "Stok Yok";
    }

    private string? _token;

    private async Task<string?> GetTokenAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            var token = await LoginAsync(_options.Username, _options.Password);
            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("OtoIsmail login failed. Token could not be obtained.");
            }
        }
        return _token;
    }

    private async Task<T?> SendRequestAsync<T>(string url, HttpMethod method, object? body = null, string? customToken = null)
    {
        try
        {
            var token = customToken ?? await GetTokenAsync();
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("token", token);
            }

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("OtoIsmail API Error: {Url} {StatusCode} {Error}", url, response.StatusCode, content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _token = null;
                }
                
                return default;
            }

            logger.LogInformation("OtoIsmail API Response (200 OK): {Url} -> {Content}", url, content);
            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OtoIsmail API Exception: {Url}", url);
            return default;
        }
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        try 
        {
            var url = $"{_options.BaseUrl}/api/OISML/Login?pUsername={Uri.EscapeDataString(username)}&pPwd={Uri.EscapeDataString(password)}";
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode) 
            {
                logger.LogError("OtoIsmail Login Failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(content);
            
            // Hem flat hem nested yapıyı kontrol edelim
            string? token = (string?)data?.Data;
            bool isSuccess = (bool?)(data?.Result?.Success ?? data?.Result?.Ok ?? data?.Ok ?? (token != null)) ?? false;

            if (isSuccess && !string.IsNullOrEmpty(token))
            {
                _token = token;
                return _token;
            }
            
            logger.LogWarning("OtoIsmail Login Failed or Success=false. Response: {Content}", content);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OtoIsmail Login Exception");
            return null;
        }
    }

    public Task<OtoIsmailResultBrandsDto?> GetBrandsAsync()
        => SendRequestAsync<OtoIsmailResultBrandsDto>($"{_options.BaseUrl}/api/OISML/GetBrands", HttpMethod.Get);

    public Task<OtoIsmailResultProductsDto?> GetProductsAsync(string marka, string tarih = "19000101")
        => SendRequestAsync<OtoIsmailResultProductsDto>($"{_options.BaseUrl}/api/OISML/GetProducts?pBrand={HttpUtility.UrlEncode(marka)}&pTarih={HttpUtility.UrlEncode(tarih)}", HttpMethod.Get);

    public Task<OtoIsmailResultStockByCodeDto?> GetStockByCodeAsync(string productCode)
        => SendRequestAsync<OtoIsmailResultStockByCodeDto>($"{_options.BaseUrl}/api/OISML/GetStockByCode?pProductCode={Uri.EscapeDataString(productCode)}", HttpMethod.Get);

    public Task<OtoIsmailResultStockIdDto?> GetStockByIdAsync(int stokId)
        => SendRequestAsync<OtoIsmailResultStockIdDto>($"{_options.BaseUrl}/api/OISML/GetStockId?pStokId={stokId}", HttpMethod.Get);

    public Task<OtoIsmailResultStockByDateDto?> GetStockByDateAsync(string tarih = "19000101")
        => SendRequestAsync<OtoIsmailResultStockByDateDto>($"{_options.BaseUrl}/api/OISML/GetStockByDate?pTarih={HttpUtility.UrlEncode(tarih)}", HttpMethod.Get);

    public Task<OtoIsmailResultCurrencyDto?> GetCurrencyAsync()
        => SendRequestAsync<OtoIsmailResultCurrencyDto>($"{_options.BaseUrl}/api/OISML/GetCurrency", HttpMethod.Get);

    public async Task<OISMLServiceResultDto?> AddToCartAsync(List<OtoIsmailCartItemDto> items)
    {
        var url = $"{_options.BaseUrl}/api/OISML/SepeteEkle";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token)) request.Headers.Add("token", token);
        
        var sepetListJson = JsonConvert.SerializeObject(items);
        logger.LogInformation("OtoIsmail SepeteEkle - Items to add: {ItemCount}, JSON: {Json}", items.Count, sepetListJson);
        request.Headers.Add("SepetList", sepetListJson);
        
        var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        
        logger.LogInformation("OtoIsmail SepeteEkle Response ({StatusCode}): {Content}", response.StatusCode, content);
        
        if (!response.IsSuccessStatusCode) return null;
        
        return JsonConvert.DeserializeObject<OISMLServiceResultDto>(content);
    }

    public async Task<OISMLServiceResultDto?> SendOrderAsync(string teslimatCariKodu, string aciklama, bool bizAlacagiz, List<OtoIsmailCartItemDto> items)
    {
        var url = $"{_options.BaseUrl}/api/OISML/SiparisGonder?teslimatCariKodu={HttpUtility.UrlEncode(teslimatCariKodu)}&aciklama={HttpUtility.UrlEncode(aciklama)}&bizAlacagiz={bizAlacagiz}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token)) request.Headers.Add("token", token);
        
        request.Headers.Add("SepetList", JsonConvert.SerializeObject(items));
        
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<OISMLServiceResultDto>(content);
    }

    public Task<OtoIsmailResultProductsDto?> GetProductsUpdateAsync(int hour)
        => SendRequestAsync<OtoIsmailResultProductsDto>($"{_options.BaseUrl}/api/OISML/GetProductsUpdate?hour={hour}", HttpMethod.Get);

    public Task<OISMLResultDto?> CheckStockAsync(string stokKodu, int? netsisStokId, int adet)
        => SendRequestAsync<OISMLResultDto>($"{_options.BaseUrl}/api/OISML/StokSorgula?stokKodu={HttpUtility.UrlEncode(stokKodu)}&netsisStokId={netsisStokId}&adet={adet}", HttpMethod.Get);

    public Task<OtoIsmailResultCategoryDto?> GetCategoriesAsync()
        => SendRequestAsync<OtoIsmailResultCategoryDto>($"{_options.BaseUrl}/api/OISML/Kategoriler", HttpMethod.Get);

    public Task<OtoIsmailResultCategoryProductDto?> GetCategoryProductsAsync(string k1, string k2, string k3, string k4)
        => SendRequestAsync<OtoIsmailResultCategoryProductDto>($"{_options.BaseUrl}/api/OISML/KategoriUrunListesi?kategori1={HttpUtility.UrlEncode(k1)}&kategori2={HttpUtility.UrlEncode(k2)}&kategori3={HttpUtility.UrlEncode(k3)}&kategori4={HttpUtility.UrlEncode(k4)}", HttpMethod.Get);

    public Task<OtoIsmailResultOrderStatusDto?> GetOrderStatusAsync(string siparisId)
        => SendRequestAsync<OtoIsmailResultOrderStatusDto>($"{_options.BaseUrl}/api/OISML/SiparisDurumu?siparisId={HttpUtility.UrlEncode(siparisId)}", HttpMethod.Get);

    public Task<OtoIsmailResultVehicleBrandDto?> GetVehicleBrandsAsync()
        => SendRequestAsync<OtoIsmailResultVehicleBrandDto>($"{_options.BaseUrl}/api/OISML/AracMarka", HttpMethod.Get);

    public Task<OtoIsmailResultVehicleProductDto?> GetVehicleBrandProductsAsync(string aracMarka)
        => SendRequestAsync<OtoIsmailResultVehicleProductDto>($"{_options.BaseUrl}/api/OISML/AracMarkaUrunListesi?aracMarka={HttpUtility.UrlEncode(aracMarka)}", HttpMethod.Get);

    public Task<OISMLServiceResultDto?> CancelOrderAsync(string siparisId)
        => SendRequestAsync<OISMLServiceResultDto>($"{_options.BaseUrl}/api/OISML/SiparisIptalEt?siparisId={HttpUtility.UrlEncode(siparisId)}", HttpMethod.Get);

    public Task<OtoIsmailResultStockCodeChangeDto?> GetTodayChangedStockCodesAsync()
        => SendRequestAsync<OtoIsmailResultStockCodeChangeDto>($"{_options.BaseUrl}/api/OISML/BugunDegisenStokKodlari", HttpMethod.Get);

    public async Task<OtoIsmailResultCariListesiDto?> GetCariListesiAsync()
    {
        if (_cache.TryGetValue("OtoIsmail_CariListesi", out OtoIsmailResultCariListesiDto? cachedResult))
        {
            return cachedResult;
        }

        var result = await SendRequestAsync<OtoIsmailResultCariListesiDto>($"{_options.BaseUrl}/api/OISML/CariListesi", HttpMethod.Get);
        
        if (result?.Data != null && result.Data.Any())
        {
            _cache.Set("OtoIsmail_CariListesi", result, TimeSpan.FromMinutes(5));
        }

        return result;
    }

    public Task<OtoIsmailResultIlIlceListesiDto?> GetIlIlceListesiAsync()
        => SendRequestAsync<OtoIsmailResultIlIlceListesiDto>($"{_options.BaseUrl}/api/OISML/IlIlceListesi", HttpMethod.Get);
}
