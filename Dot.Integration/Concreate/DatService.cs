using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Dot.Integration.Abstract;
using Dot.Integration.Dtos;
using Dot.Integration.Options;
using Dot.Integration.Services;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Dot.Integration.Concreate;
    
public class DatService : IDatService
{
    private readonly HttpClient _httpClient;
    private readonly DatServiceOptions _options;
    private readonly DatTokenCache _tokenCache;
    private readonly ILogger<DatService> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IServiceProvider _serviceProvider;

    public DatService(
        HttpClient httpClient,
        IOptions<DatServiceOptions> options,
        DatTokenCache tokenCache,
        ILogger<DatService> logger,
        IUnitOfWork<ApplicationDbContext> context,
        IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tokenCache = tokenCache;
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
        
        // Retry policy: 3 deneme, exponential backoff (1s, 2s, 4s)
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.InternalServerError)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<System.IO.IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("🔄 Retry {RetryCount}/3 after {Delay}s - Reason: {Reason}", 
                        retryCount, timespan.TotalSeconds, 
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                });
    }

    public async Task<DatTokenReturn> GetTokenAsync()
    {
        try
    {
        _logger.LogInformation("Requesting DAT token with CustomerNumber: {CustomerNumber}, CustomerLogin: {CustomerLogin}", 
            _options.CustomerNumber, _options.CustomerLogin);
            
        string body = $@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:aut=""http://sphinx.dat.de/services/Authentication"">
   <soapenv:Header/>
   <soapenv:Body>
      <aut:generateToken>
         <request>
            <customerNumber>{_options.CustomerNumber}</customerNumber>
            <customerLogin>{_options.CustomerLogin}</customerLogin>
            <customerPassword>{_options.CustomerPassword}</customerPassword>
            <interfacePartnerNumber>{_options.InterfacePartnerNumber}</interfacePartnerNumber>
            <interfacePartnerSignature>{_options.InterfacePartnerSignature}</interfacePartnerSignature>
         </request>
      </aut:generateToken>
   </soapenv:Body>
</soapenv:Envelope>";

        var response = await SendWithRetryAsync(_options.AuthenticationUrl, body);
        
        var xmlResponse = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Token Response Status: {StatusCode}, Body: {ResponseBody}", response.StatusCode, xmlResponse);
        
        response.EnsureSuccessStatusCode();

        var tokenReturn = ParseTokenResponse(xmlResponse);
        // Verbose log removed
        
        return tokenReturn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DAT token");
            throw;
        }
    }

    public async Task<DatVehicleTypeReturn> GetVehicleTypesAsync()
    {
        try
        {
            // Processed check disabled - concurrent DbContext access causes issues

            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getVehicleTypes>
      <request>
         <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
         <restriction>{_options.DefaultRestriction}</restriction>
      </request>
   </veh:getVehicleTypes>");

            // Verbose request log removed

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            // Verbose response log removed
            
            response.EnsureSuccessStatusCode();
            var result = ParseVehicleTypeResponse(xmlResponse);
            
            // Save and get last entity ID
            using var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
            var lastId = await dataService.SaveVehicleTypesAsync(result.VehicleTypes.VehicleType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle types");
            throw;
        }
    }

    public async Task<DatManufacturerReturn> GetManufacturersAsync(int vehicleType, string? constructionTimeFrom = null, string? constructionTimeTo = null)
    {
        try
        {
            // Format: YYMM (e.g., 0001 = Year 1900, Month 01 means January 1900)
            var defaultFrom = constructionTimeFrom ?? "0001"; // 1900/January
            var defaultTo = constructionTimeTo ?? "9912"; // 2099/December 
            
            // Processed check disabled - concurrent DbContext access causes issues

            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getManufacturers>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <constructionTimeFrom>{defaultFrom}</constructionTimeFrom>
      <constructionTimeTo>{defaultTo}</constructionTimeTo>
      <restriction>ALL</restriction>
      <vehicleType>{vehicleType}</vehicleType>
   </request>
</veh:getManufacturers>");

            // Verbose request log removed

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            
            var responseContent = await DecompressResponseIfNeeded(response);
            // Verbose response log removed
            
            // Eğer 500 hatası ve "valueNotFound" ise, boş sonuç döndür (normal durum)
            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                if (responseContent.Contains("valueNotFound") || responseContent.Contains("manufacturers not found"))
                {
                    _logger.LogWarning("No manufacturers found for vehicle type {VehicleType}, returning empty result", vehicleType);
                    return new DatManufacturerReturn 
                    { 
                        Manufacturers = new DatManufacturers 
                        { 
                            Manufacturer = new List<DatManufacturer>() 
                        } 
                    };
                }
            }
            
            response.EnsureSuccessStatusCode();
            var mans = ParseManufacturerResponse(responseContent);
            _logger.LogInformation("🔎 Manufacturers count: {Count} for VT {VehicleType}", mans.Manufacturers.Manufacturer.Count, vehicleType);
            
            // Save and get last entity ID
            using var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
            var lastId = await dataService.SaveManufacturersAsync(mans.Manufacturers.Manufacturer, vehicleType);
            return mans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manufacturers for vehicle type {VehicleType}", vehicleType);
            // Boş sonuç döndür, işlemi durma
            return new DatManufacturerReturn 
            { 
                Manufacturers = new DatManufacturers 
                { 
                    Manufacturer = new List<DatManufacturer>() 
                } 
            };
        }
    }

    public async Task<DatBaseModelReturn> GetBaseModelsNAsync(int vehicleType, string manufacturerKey, string? constructionTimeFrom = null, string? constructionTimeTo = null, bool withRepairIncomplete = true)
    {
        try
        {
            // Processed check disabled - concurrent DbContext access causes issues
           
            var defaultFrom = constructionTimeFrom ?? "0001"; // 1900/January
            var defaultTo = constructionTimeTo ?? "9912"; // 2099/December 
            
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getBaseModelsN>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <constructionTimeFrom>{defaultFrom}</constructionTimeFrom>
      <constructionTimeTo>{defaultTo}</constructionTimeTo>
      <restriction>ALL</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
   </request>
</veh:getBaseModelsN>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            // 500 Internal Server Error ve "No result" hatalarını handle et
            if (!response.IsSuccessStatusCode && (response.StatusCode == System.Net.HttpStatusCode.InternalServerError || xmlResponse.Contains("No result") || xmlResponse.Contains("not found")))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    _logger.LogWarning("No base models found for vehicle type {VehicleType} and manufacturer {Manufacturer}, returning empty result", vehicleType, manufacturerKey);
                }
                else
                {
                    _logger.LogWarning("Ana model bulunamadı, boş liste döndürülüyor");
                }
                return new DatBaseModelReturn
                {
                    BaseModels = new DatBaseModels { BaseModel = new List<DatBaseModel>() }
                };
            }
            
            response.EnsureSuccessStatusCode();
            var bases = ParseBaseModelResponse(xmlResponse);
            _logger.LogInformation("🔎 BaseModels count: {Count} for VT {VehicleType}, M {Manufacturer}", bases.BaseModels.BaseModel.Count, vehicleType, manufacturerKey);
            
            // Save and get last entity ID
            using var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
            var lastId = await dataService.SaveBaseModelsAsync(bases.BaseModels.BaseModel, vehicleType, manufacturerKey);
            return bases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting base models for vehicle type {VehicleType} and manufacturer {Manufacturer}", vehicleType, manufacturerKey);
            throw;
        }
    }

    public async Task<DatSubModelReturn> GetSubModelsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null)
    {
        try
        {
            // Processed check disabled - concurrent DbContext access causes issues

            // Format: YYMM (e.g., 0001 = Year 1900, Month 01 means January 1900)
            var defaultFrom = constructionTimeFrom ?? "0001"; // 1900/January
            var defaultTo = constructionTimeTo ?? "9912"; // 2099/December
            
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getSubModels>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <constructionTimeFrom>{defaultFrom}</constructionTimeFrom>
      <constructionTimeTo>{defaultTo}</constructionTimeTo>
      <restriction>ALL</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
   </request>
</veh:getSubModels>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            // Verbose response log removed
            
            // 500 Internal Server Error ve "No result" hatalarını handle et
            if (!response.IsSuccessStatusCode && (response.StatusCode == System.Net.HttpStatusCode.InternalServerError || xmlResponse.Contains("No result") || xmlResponse.Contains("not found") || xmlResponse.Contains("Wrong base type code")))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    _logger.LogWarning("No sub models found for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, returning empty result", 
                        vehicleType, manufacturerKey, baseModelKey);
                }
                else
                {
                    _logger.LogWarning("Alt model bulunamadı (hatalı base type code veya sonuç yok), boş liste döndürülüyor");
                }
                return new DatSubModelReturn
                {
                    SubModels = new DatSubModels { SubModel = new List<DatSubModel>() }
                };
            }
            
            response.EnsureSuccessStatusCode();
            var subs = ParseSubModelResponse(xmlResponse);
            _logger.LogInformation("🔎 SubModels count: {Count} for VT {VehicleType}, M {Manufacturer}, B {BaseModel}", subs.SubModels.SubModel.Count, vehicleType, manufacturerKey, baseModelKey);
            
            // Save and get last entity ID
            using var scope = _serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
            var lastId = await dataService.SaveSubModelsAsync(subs.SubModels.SubModel, vehicleType, manufacturerKey, baseModelKey);
            return subs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sub models for vehicle type {VehicleType}, manufacturer {Manufacturer} and base model {BaseModel}", 
                vehicleType, manufacturerKey, baseModelKey);
            throw;
        }
    }

    public async Task<DatECodeReturn> CompileDatECodeAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, List<string>? selectedOptions = null)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            
            var optionsXml = string.Empty;
            if (selectedOptions != null && selectedOptions.Any())
            {
                optionsXml = string.Join(Environment.NewLine, selectedOptions.Select(opt => $"      <selectedOptions>{opt}</selectedOptions>"));
            }
            
            string body = BuildSoapEnvelope(token.Token, $@"<veh:compileDatECode>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <restriction>{_options.DefaultRestriction}</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
      <subModel>{subModelKey}</subModel>
{optionsXml}
   </request>
</veh:compileDatECode>");

            // Verbose request log removed

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            
            // "No result" veya "Too few/many options" hatası critical değil, boş ECode döndür
            if (!response.IsSuccessStatusCode)
            {
                // "Too few/many options" hatası NORMAL bir durum - DEBUG seviyesinde logla
                if (xmlResponse.Contains("Too few options") || xmlResponse.Contains("Too many options"))
                {
                    _logger.LogDebug("⚠️ CompileDatECode: Yetersiz/fazla option (Normal durum). VT:{VehicleType} M:{Manufacturer} B:{BaseModel} S:{SubModel}", 
                        vehicleType, manufacturerKey, baseModelKey, subModelKey);
                }
                else
                {
                    // Diğer hatalar için WARNING
                    _logger.LogWarning("❌ CompileDatECode failed with {StatusCode}. Response: {Response}", 
                        response.StatusCode, xmlResponse.Length > 1000 ? xmlResponse.Substring(0, 1000) : xmlResponse);
                }
                
                // Raw XML response'u da döndür (retry mekanizması için)
                return new DatECodeReturn 
                { 
                    DatECode = string.Empty,
                    RawXmlResponse = xmlResponse 
                };
            }
            
            // response.EnsureSuccessStatusCode(); // Artık gerek yok, zaten yukarıda kontrol ettik
            return ParseDatECodeResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling DAT E-Code for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            throw;
        }
    }

    public async Task<DatClassificationGroupReturn> GetClassificationGroupsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getClassificationGroups>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <restriction>{_options.DefaultRestriction}</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
      <subModel>{subModelKey}</subModel>
   </request>
</veh:getClassificationGroups>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ GetClassificationGroups failed with {StatusCode} for VT:{VT} M:{M} B:{B} S:{S}. Response: {Response}", 
                    response.StatusCode, vehicleType, manufacturerKey, baseModelKey, subModelKey,
                    xmlResponse.Length > 500 ? xmlResponse.Substring(0, 500) : xmlResponse);
                
                // Boş sonuç döndür, exception fırlatma
                return new DatClassificationGroupReturn { ClassificationGroups = new List<int>() };
            }
            
            return ParseClassificationGroupResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classification groups for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            
            // Boş sonuç döndür, exception fırlatma
            return new DatClassificationGroupReturn { ClassificationGroups = new List<int>() };
        }
    }

    public async Task<DatOptionReturn> GetOptionsByClassificationAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, int classification)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getOptionsbyClassification>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <restriction>{_options.DefaultRestriction}</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
      <subModel>{subModelKey}</subModel>
      <classification>{classification}</classification>
   </request>
</veh:getOptionsbyClassification>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            
            var responseContent = await DecompressResponseIfNeeded(response);
        
            if (!response.IsSuccessStatusCode && responseContent.Contains("No result for the selected options"))
            {
                _logger.LogWarning("No options found for classification {Classification}, returning empty list", classification);
                return new DatOptionReturn
                {
                    Options = new DatOptions { Option = new List<DatOption>() }
                };
            }
            
            response.EnsureSuccessStatusCode();
            return ParseOptionResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting options for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}, classification {Classification}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey, classification);
            throw;
        }
    }

    public async Task<DatEngineOptionReturn> GetEngineOptionsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null)
    {
        try
        {
           
            var defaultFrom = constructionTimeFrom ?? "0001"; // 1900/January
            var defaultTo = constructionTimeTo ?? "9912"; // 2099/December 
            
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getEngineOptions>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <constructionTimeFrom>{defaultFrom}</constructionTimeFrom>
      <constructionTimeTo>{defaultTo}</constructionTimeTo>
      <restriction>{_options.DefaultRestriction}</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
      <subModel>{subModelKey}</subModel>
   </request>
</veh:getEngineOptions>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            if (!response.IsSuccessStatusCode && (xmlResponse.Contains("No result") || xmlResponse.Contains("not found")))
            {
                _logger.LogWarning("Motor opsiyonu bulunamadı, boş liste döndürülüyor");
                return new DatEngineOptionReturn
                {
                    EngineOptions = new DatEngineOptions { EngineOption = new List<DatEngineOption>() }
                };
            }
            
            // 500 Internal Server Error - DAT API hatası, boş sonuç döndür
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                _logger.LogWarning("🔄 DAT API 500 hatası (Engine options) - boş sonuç döndürülüyor. VT:{VT}, M:{M}, B:{B}, S:{S}", 
                    vehicleType, manufacturerKey, baseModelKey, subModelKey);
                return new DatEngineOptionReturn
                {
                    EngineOptions = new DatEngineOptions { EngineOption = new List<DatEngineOption>() }
                };
            }
            
            response.EnsureSuccessStatusCode();
            return ParseEngineOptionResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engine options for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            throw;
        }
    }

    public async Task<DatCarBodyOptionReturn> GetCarBodyOptionsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null)
    {
        try
        {

            var defaultFrom = constructionTimeFrom ?? "0001"; // 1900/January
            var defaultTo = constructionTimeTo ?? "9912"; // 2099/December 
            
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"<veh:getCarBodyOptions>
   <request>
      <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
      <constructionTimeFrom>{defaultFrom}</constructionTimeFrom>
      <constructionTimeTo>{defaultTo}</constructionTimeTo>
      <restriction>{_options.DefaultRestriction}</restriction>
      <vehicleType>{vehicleType}</vehicleType>
      <manufacturer>{manufacturerKey}</manufacturer>
      <baseModel>{baseModelKey}</baseModel>
      <subModel>{subModelKey}</subModel>
   </request>
</veh:getCarBodyOptions>");

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            if (!response.IsSuccessStatusCode && (xmlResponse.Contains("No result") || xmlResponse.Contains("not found")))
            {
                _logger.LogWarning("Kasa opsiyonu bulunamadı, boş liste döndürülüyor");
                return new DatCarBodyOptionReturn
                {
                    CarBodyOptions = new DatCarBodyOptions { CarBodyOption = new List<DatCarBodyOption>() }
                };
            }
            
            // 500 Internal Server Error - DAT API hatası, boş sonuç döndür
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                _logger.LogWarning("🔄 DAT API 500 hatası (CarBody options) - boş sonuç döndürülüyor. VT:{VT}, M:{M}, B:{B}, S:{S}", 
                    vehicleType, manufacturerKey, baseModelKey, subModelKey);
                return new DatCarBodyOptionReturn
                {
                    CarBodyOptions = new DatCarBodyOptions { CarBodyOption = new List<DatCarBodyOption>() }
                };
            }
            
            response.EnsureSuccessStatusCode();
            return ParseCarBodyOptionResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting car body options for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            throw;
        }
    }

    
    public async Task<DatPartsReturn> SearchPartsAsync(
        int vehicleType,
        string manufacturerKey,
        string baseModelKey,
        string subModelKey,
        List<string>? selectedOptions = null,
        List<string>? datProcessNos = null,
        string? partNumber = null,
        string? description = null)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            
            // Processed check disabled - concurrent DbContext access causes issues
            
            if (datProcessNos == null || datProcessNos.Count == 0)
            {
                var eCodeResult = await CompileDatECodeAsync(vehicleType, manufacturerKey, baseModelKey, subModelKey, selectedOptions);
                if (!string.IsNullOrWhiteSpace(eCodeResult.DatECode))
                {
                    datProcessNos = new List<string> { eCodeResult.DatECode };
                }
            }
            
            if (datProcessNos == null || datProcessNos.Count == 0)
            {
                return new DatPartsReturn { Parts = new List<DatPartSimple>() };
            }
            
            var datProcessNosXml = string.Join("", datProcessNos.Select(p => $"<datProcessNo>{System.Security.SecurityElement.Escape(p)}</datProcessNo>"));

            // BaseModel'i 3 haneli yap (padding ile) - DAT API format requirement
            var baseModelFormatted = baseModelKey.PadLeft(3, '0');
            
            string body = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:veh=""http://sphinx.dat.de/services/VehicleRepairService"">
   <soapenv:Header>
      <DAT-AuthorizationToken>{token.Token}</DAT-AuthorizationToken>
   </soapenv:Header>
   <soapenv:Body>
      <veh:getSparePartsDetailsForDPN>
         <request>
            <locale country=""{_options.LocaleCountry}"" datCountryIndicator=""{_options.DatCountryIndicator}"" language=""{_options.Language}""/>
            <restriction>
               {datProcessNosXml}
               <vehicleType>{vehicleType}</vehicleType>
               <manufacturer>{manufacturerKey}</manufacturer>
               <baseModel>{baseModelFormatted}</baseModel>
            </restriction>
            <settings>
               <pageSize>100</pageSize>
               <pageNumber>1</pageNumber>
               <withPriceHistory>false</withPriceHistory>
               <withVehicleData>true</withVehicleData>
               <sortingCriterions>
                  <baseModel>
                     <order>asc</order>
                     <priority>1</priority>
                  </baseModel>
                  <constructionTime>
                     <order>4840</order>
                     <priority>2</priority>
                  </constructionTime>
               </sortingCriterions>
            </settings>
         </request>
      </veh:getSparePartsDetailsForDPN>
   </soapenv:Body>
</soapenv:Envelope>";


            var response = await SendWithRetryAsync(_options.VehicleRepairServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);

            // Response status ve içeriğini logla
            _logger.LogDebug("📥 SearchParts Response Status: {StatusCode}, DatProcessNos: {DatProcessNos}", 
                response.StatusCode, string.Join(",", datProcessNos ?? new List<string>()));
            
            // XML Response'u logla (ilk 2000 karakter)
            _logger.LogDebug("📥 SearchParts XML Response (first 2000 chars): {XmlResponse}", 
                xmlResponse.Length > 2000 ? xmlResponse.Substring(0, 2000) + "..." : xmlResponse);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ SearchParts failed with {StatusCode}. Response: {Response}", 
                    response.StatusCode, 
                    xmlResponse.Length > 500 ? xmlResponse.Substring(0, 500) : xmlResponse);
                
                if (xmlResponse.Contains("No result") || xmlResponse.Contains("not found"))
                {
                    return new DatPartsReturn { Parts = new List<DatPartSimple>() };
                }
            }
            
            // 500 Internal Server Error durumunda crash olmaması için kontrol et
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ SearchParts failed with {StatusCode}. Response: {Response}", 
                    response.StatusCode, xmlResponse);
                
                // SOAP Fault içeriğini kontrol et - eğer bilinen hata ise devam et
                if (xmlResponse.Contains("Cannot invoke") || 
                    xmlResponse.Contains("NullPointerException") ||
                    xmlResponse.Contains("merk") ||
                    xmlResponse.Contains("internal error"))
                {
                    _logger.LogWarning("🔄 DAT API internal hatası - atlanıyor ve boş sonuç döndürülüyor");
                    return new DatPartsReturn { Parts = new List<DatPartSimple>() };
                }
                
                // 500 Internal Server Error - DAT API geçici olarak aşırı yüklenmiş olabilir
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    _logger.LogWarning("🔄 DAT API 500 hatası - geçici aşırı yüklenme, boş sonuç döndürülüyor");
                    return new DatPartsReturn { Parts = new List<DatPartSimple>() };
                }
                
                // Diğer hatalar için exception fırlat
                response.EnsureSuccessStatusCode();
            }
            
            var parsedResult = ParsePartsResponse(xmlResponse);

            if (parsedResult.Parts?.Count > 0)
            {
                _logger.LogInformation("✅ ParsePartsResponse tamamlandı. Bulunan parça sayısı: {Count}", parsedResult.Parts.Count);
            }
            else
            {
                _logger.LogInformation("ℹ️ SearchParts başarılı idi ama 0 parça döndü. DatProcessNos: {DatProcessNos}", 
                    string.Join(",", datProcessNos ?? new List<string>()));
            }
            
            return parsedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching parts for vehicle type {VehicleType}, manufacturer {Manufacturer}, base model {BaseModel}, sub model {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            throw;
        }
    }

    public async Task<DatPartsReturn> GetPartDetailsAsync(string partNumber)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            string body = BuildSoapEnvelope(token.Token, $@"
<par:getPartDetails xmlns:par=""http://sphinx.dat.de/services/PartsService"">
   <request>
      <partNumber>{partNumber}</partNumber>
   </request>
</par:getPartDetails>", "http://sphinx.dat.de/services/PartsService");

            var response = await SendWithRetryAsync(_options.PartsServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            // 500 Internal Server Error - DAT API hatası, boş sonuç döndür
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                _logger.LogWarning("🔄 DAT API 500 hatası (GetPartDetails) - boş sonuç döndürülüyor. PartNumber: {PartNumber}", partNumber);
                return new DatPartsReturn { Parts = new List<DatPartSimple>() };
            }
            
            response.EnsureSuccessStatusCode();
            return ParsePartsResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting part details for {PartNumber}", partNumber);
            throw;
        }
    }


    public async Task<DatVehicleImageReturn> GetVehicleImagesAsync(string datECode, List<string>? aspects = null, string imageType = "PICTURE")
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            
            // Default aspects if not provided
            if (aspects == null || !aspects.Any())
            {
                aspects = new List<string> { "SIDEVIEW", "ANGULARFRONT" };
            }
            
            var aspectsXml = string.Join(Environment.NewLine, aspects.Select(a => $"            <aspect>{a}</aspect>"));
            
            string body = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:veh=""http://sphinx.dat.de/services/VehicleImagery"">
   <soapenv:Header>
      <DAT-AuthorizationToken>{token.Token}</DAT-AuthorizationToken>
   </soapenv:Header>
   <soapenv:Body>
      <veh:getVehicleImagesN>
         <request>
            <datECode>{datECode}</datECode>
            <imageType>{imageType}</imageType>
{aspectsXml}
         </request>
      </veh:getVehicleImagesN>
   </soapenv:Body>
</soapenv:Envelope>";

            var response = await SendWithRetryAsync(_options.VehicleImageryServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            // DEBUG: Response'u log'la (sadece hata durumunda)
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("📥 GetVehicleImages error for {DatECode}: {StatusCode}", datECode, response.StatusCode);
            }
            
            // "No images available" hatası normal, boş liste döndür
            if (!response.IsSuccessStatusCode && (xmlResponse.Contains("no images available") || xmlResponse.Contains("imagesNotAvailable")))
            {
                _logger.LogDebug("ℹ️ API says: no images available for {DatECode}", datECode);
                return new DatVehicleImageReturn { Images = new List<DatVehicleImageDto>() };
            }
            
            response.EnsureSuccessStatusCode();
            return ParseVehicleImageResponse(xmlResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle images for datECode {DatECode}", datECode);
            throw;
        }
    }

    private DatVehicleImageReturn ParseVehicleImageResponse(string xmlResponse)
    {
        try
        {
            // Manuel XML parsing (XmlSerializer çalışmıyor)
            var images = new List<DatVehicleImageDto>();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xmlResponse);
            
            var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", "http://sphinx.dat.de/services/VehicleImagery");
            
            var imageNodes = doc.SelectNodes("//ns:getVehicleImagesNResponse/vehicleImagesN/images", nsmgr);
            
            if (imageNodes != null)
            {
                foreach (System.Xml.XmlNode imageNode in imageNodes)
                {
                    var aspect = imageNode.SelectSingleNode("aspect")?.InnerText ?? "";
                    var imageType = imageNode.SelectSingleNode("imageType")?.InnerText ?? "";
                    var imageFormat = imageNode.SelectSingleNode("imageFormat")?.InnerText ?? "";
                    var imageBase64 = imageNode.SelectSingleNode("imageBase64")?.InnerText ?? "";
                    
                    images.Add(new DatVehicleImageDto
                    {
                        Aspect = aspect,
                        ImageType = imageType,
                        ImageFormat = imageFormat,
                        ImageBase64 = imageBase64
                    });
                }
            }
            
            // Log only if images found
            if (images.Count > 0)
            {
                _logger.LogInformation("🖼️ Parsed {Count} vehicle images", images.Count);
            }
            
            return new DatVehicleImageReturn { Images = images };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing vehicle image response");
            throw;
        }
    }

    private Task<HttpResponseMessage> SendWithRetryAsync(string url, string body)
    {
        return _retryPolicy.ExecuteAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(body, Encoding.UTF8, "text/xml");
            return _httpClient.SendAsync(request);
        });
    }

    private string BuildSoapEnvelope(string token, string bodyContent, string ns = "http://sphinx.dat.de/services/VehicleSelectionService")
    {
       return $@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:veh=""{ns}"">
   <soapenv:Header>
      <DAT-AuthorizationToken>{token}</DAT-AuthorizationToken>
   </soapenv:Header>
   <soapenv:Body>
      {bodyContent}
   </soapenv:Body>
</soapenv:Envelope>";
    }

    public async Task<DatVehicleDataVehicle> GetVehicleDataAsync(string datECode, string? container = null, string? constructionTime = null, string restriction = "ALL")
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            
            // Container optional, ConstructionTime zorunlu: Default 4667 (Postman'de başarılı olan)
            var containerXml = !string.IsNullOrWhiteSpace(container)
                ? "<container>" + System.Security.SecurityElement.Escape(container) + "</container>"
                : string.Empty;
            var defaultConstructionTime = constructionTime ?? "4667";
            var consTimeXml = "<constructionTime>" + System.Security.SecurityElement.Escape(defaultConstructionTime) + "</constructionTime>";
            
            var bodyContent = "<veh:getVehicleData>\n         <request>\n            <locale country=\"" + _options.LocaleCountry + "\" datCountryIndicator=\"" + _options.DatCountryIndicator + "\" language=\"" + _options.Language + "\"/>\n            <restriction>" + System.Security.SecurityElement.Escape(restriction) + "</restriction>\n            <datECode>" + System.Security.SecurityElement.Escape(datECode) + "</datECode>\n            " + containerXml + "\n            " + consTimeXml + "\n         </request>\n      </veh:getVehicleData>";
            var body = BuildSoapEnvelope(token.Token, bodyContent);

            // DEBUG: Gönderilen request'i logla
            _logger.LogInformation("🔍 GetVehicleData Request:\n{Request}", body);

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            // 500 hatası normal olabilir (bazı araçlar için data yok)
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ GetVehicleData returned {StatusCode} for datECode: {DatECode}\nResponse Body: {ResponseBody}", 
                    response.StatusCode, datECode, xmlResponse?.Length > 2000 ? xmlResponse.Substring(0, 2000) : xmlResponse);
                // Boş vehicle döndür, hata fırlatma
                return new DatVehicleDataVehicle();
            }

            // DEBUG: Başarılı response'u logla
            _logger.LogInformation("🎉 GetVehicleData SUCCESS for {DatECode}\nResponse Body: {ResponseBody}", 
                datECode, xmlResponse?.Length > 3000 ? xmlResponse.Substring(0, 3000) + "..." : xmlResponse);

            var serializer = new XmlSerializer(typeof(DatVehicleDataResponse));
            using var reader = new StringReader(xmlResponse);
            var deserialized = (DatVehicleDataResponse)serializer.Deserialize(reader)!;
            
            var vehicle = deserialized.Body.GetVehicleDataResponse.Vxs.Vehicle;
            _logger.LogInformation("✅ VehicleData parsed. DatECode: {ECode}, Container: {Container}, Manufacturer: {Manufacturer}, BaseModel: {BaseModel}", 
                vehicle.DatECode, vehicle.Container, vehicle.ManufacturerName, vehicle.BaseModelName);
            
            return vehicle;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Exception getting VehicleData for datECode {DatECode}", datECode);
            // Boş vehicle döndür, exception fırlatma
            return new DatVehicleDataVehicle();
        }
    }

    public async Task<string?> GetConstructionPeriodsAsync(string datECode, string? container = null)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);
            
            var containerXml = !string.IsNullOrWhiteSpace(container)
                ? "<container>" + System.Security.SecurityElement.Escape(container) + "</container>"
                : string.Empty;
            
            var bodyContent = "<veh:getConstructionPeriodsN>\n         <request>\n            <locale country=\"" + _options.LocaleCountry + "\" datCountryIndicator=\"" + _options.DatCountryIndicator + "\" language=\"" + _options.Language + "\"/>\n            <restriction>ALL</restriction>\n            <datECode>" + System.Security.SecurityElement.Escape(datECode) + "</datECode>\n            " + containerXml + "\n         </request>\n      </veh:getConstructionPeriodsN>";
            var body = BuildSoapEnvelope(token.Token, bodyContent);

            _logger.LogInformation("🔍 GetConstructionPeriods Request for {DatECode}:\n{Request}", datECode, body);

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ GetConstructionPeriods returned {StatusCode} for datECode: {DatECode}\nResponse: {Response}", 
                    response.StatusCode, datECode, xmlResponse?.Length > 1000 ? xmlResponse.Substring(0, 1000) : xmlResponse);
                return null;
            }
            
            _logger.LogInformation("✅ GetConstructionPeriods SUCCESS for {DatECode}\nResponse: {Response}", 
                datECode, xmlResponse?.Length > 1000 ? xmlResponse.Substring(0, 1000) : xmlResponse);

            // XML'den constructionTimeMax'ı çek (VehicleData için kullanacağız)
            // constructionTimeMax her zaman en güncel/en son tarihi temsil eder
            var match = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"constructionTimeMax=""(\d+)""");
            if (match.Success)
            {
                var constructionTime = match.Groups[1].Value;
                _logger.LogInformation("✅ ConstructionTimeMax bulundu: {ConstructionTime} for {DatECode}", constructionTime, datECode);
                return constructionTime;
            }
            
            // constructionTimeMax bulunamazsa ilk entry value'yi dene
            var entryMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"<entry[^>]+value=""(\d+)""");
            if (entryMatch.Success)
            {
                var constructionTime = entryMatch.Groups[1].Value;
                _logger.LogInformation("✅ ConstructionTime (from entry) bulundu: {ConstructionTime} for {DatECode}", constructionTime, datECode);
                return constructionTime;
            }
            
            _logger.LogWarning("⚠️ ConstructionTime bulunamadı for datECode: {DatECode}", datECode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Exception getting ConstructionPeriods for datECode {DatECode}", datECode);
            return null;
        }
    }

    private static int? ParseYearFromEntryNames(string xml)
    {
        // name="ab 01.01.2007" veya name="Erstpreisliste" gibi
        var matches = System.Text.RegularExpressions.Regex.Matches(xml, "name=\\\"[^\\\"]*(\\d{2}\\.\\d{2}\\.\\d{4})\\\"");
        int? min = null, max = null;
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            if (DateTime.TryParseExact(m.Groups[1].Value, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                if (min == null || dt.Year < min) min = dt.Year;
                if (max == null || dt.Year > max) max = dt.Year;
            }
        }
        return max; // helper for quick current year if needed
    }

    public async Task<Dot.Integration.Dtos.DatConstructionPeriodsInfo?> GetConstructionPeriodsInfoAsync(string datECode, string? container = null)
    {
        try
        {
            var token = await _tokenCache.GetValidTokenAsync(GetTokenAsync);

            var containerXml = !string.IsNullOrWhiteSpace(container)
                ? "<container>" + System.Security.SecurityElement.Escape(container) + "</container>"
                : string.Empty;

            var bodyContent = "<veh:getConstructionPeriodsN>\n         <request>\n            <locale country=\"" + _options.LocaleCountry + "\" datCountryIndicator=\"" + _options.DatCountryIndicator + "\" language=\"" + _options.Language + "\"/>\n            <restriction>ALL</restriction>\n            <datECode>" + System.Security.SecurityElement.Escape(datECode) + "</datECode>\n            " + containerXml + "\n         </request>\n      </veh:getConstructionPeriodsN>";
            var body = BuildSoapEnvelope(token.Token, bodyContent);

            _logger.LogInformation("🔍 GetConstructionPeriods(Request-Info) for {DatECode}:\n{Request}", datECode, body);

            var response = await SendWithRetryAsync(_options.VehicleServiceUrl, body);
            var xmlResponse = await DecompressResponseIfNeeded(response);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ GetConstructionPeriods(Info) returned {StatusCode} for datECode: {DatECode}\nResponse: {Response}",
                    response.StatusCode, datECode, xmlResponse?.Length > 1000 ? xmlResponse.Substring(0, 1000) : xmlResponse);
                return null;
            }

            // Min/Max kodları
            var minMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"constructionTimeMin=""(\d+)""");
            var maxMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"constructionTimeMax=""(\d+)""");
            var minCode = minMatch.Success ? minMatch.Groups[1].Value : null;
            var maxCode = maxMatch.Success ? maxMatch.Groups[1].Value : null;

            // Sadece entry name'lerinden tarih çıkarmayı dene (hızlı yol)
            int? yearMin = null, yearMax = null;
            
            var entryMatches = System.Text.RegularExpressions.Regex.Matches(xmlResponse, @"<entry\s+name=""([^""]+)""\s+value=""[^""]+""");
            foreach (System.Text.RegularExpressions.Match entry in entryMatches)
            {
                var nameAttr = entry.Groups[1].Value;
                var yearMatch = System.Text.RegularExpressions.Regex.Match(nameAttr, @"(19\d{2}|20\d{2})");
                if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var y))
                {
                    if (yearMin == null || y < yearMin) yearMin = y;
                    if (yearMax == null || y > yearMax) yearMax = y;
                }
            }

            return new Dot.Integration.Dtos.DatConstructionPeriodsInfo
            {
                Min = minCode,
                Max = maxCode,
                Current = maxCode,
                YearMin = yearMin,
                YearMax = yearMax
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Exception in GetConstructionPeriodsInfoAsync for {DatECode}", datECode);
            return null;
        }
    }

    public async Task<DateTime?> ConvertConstructionTimeToDateAsync(string constructionTime)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token?.Token))
            {
                _logger.LogWarning("Token alınamadı");
                return null;
            }

            var body = $@"
      <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:con=""http://sphinx.dat.de/services/ConversionFunctionsService"">
         <soapenv:Header>
            <DAT-AuthorizationToken>{System.Security.SecurityElement.Escape(token.Token)}</DAT-AuthorizationToken>
         </soapenv:Header>
         <soapenv:Body>
            <con:constructionTime2Date>
               <request>
                  <constructionTime>{System.Security.SecurityElement.Escape(constructionTime)}</constructionTime>
               </request>
            </con:constructionTime2Date>
         </soapenv:Body>
      </soapenv:Envelope>";

            var request = new HttpRequestMessage(HttpMethod.Post, _options.ConversionFunctionsServiceUrl ?? "https://www.datgroup.com/myClaim/soap/v2/ConversionFunctionsService")
            {
                Content = new StringContent(body, Encoding.UTF8, "text/xml")
            };

            _logger.LogInformation("🔍 constructionTime2Date Request: {ConstructionTime}\n{Body}", constructionTime, body);

            var response = await _httpClient.SendAsync(request);
            var xmlResponse = await DecompressResponseIfNeeded(response);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ constructionTime2Date başarısız: {ConstructionTime}, Status: {Status}\nResponse: {Response}", 
                    constructionTime, response.StatusCode, xmlResponse);
                return null;
            }

            _logger.LogInformation("✅ constructionTime2Date Response: {ConstructionTime}\n{Response}", constructionTime, xmlResponse);

            var dateMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"<Date>([^<]+)</Date>");
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, out var date))
            {
                _logger.LogInformation("✅ Parsed date: {Date} → Year: {Year}", dateMatch.Groups[1].Value, date.Year);
                return date;
            }

            _logger.LogWarning("⚠️ Date parse edilemedi: {Response}", xmlResponse);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Exception in ConvertConstructionTimeToDateAsync: {ConstructionTime}", constructionTime);
            return null;
        }
    }

    private async Task<string> DecompressResponseIfNeeded(HttpResponseMessage response)
    {
        var contentEncoding = response.Content.Headers.ContentEncoding;
        // Verbose log removed
        
        var stream = await response.Content.ReadAsStreamAsync();
        
        // Check if response is gzipped
        if (contentEncoding.Contains("gzip"))
        {
            // Decompressing GZIP
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        else if (contentEncoding.Contains("deflate"))
        {
            // Decompressing Deflate
            using var deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        else
        {
            // No compression, reading plain text
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }

    private DatTokenReturn ParseTokenResponse(string xmlResponse)
    {
        try
        {
            // Verbose log removed
            
            if (string.IsNullOrWhiteSpace(xmlResponse))
            {
                _logger.LogError("Token response XML is null or empty!");
                throw new InvalidOperationException("Token response XML is null or empty");
            }
            
            var serializer = new XmlSerializer(typeof(DatTokenResponse));
            using var reader = new StringReader(xmlResponse);
            var response = (DatTokenResponse?)serializer.Deserialize(reader);
            
            if (response == null)
            {
                _logger.LogError("Deserialized response is null!");
                throw new InvalidOperationException("Failed to deserialize token response");
            }
            
            // Verbose log removed
            
            if (response.Body == null)
            {
                _logger.LogError("Response.Body is null!");
                throw new InvalidOperationException("Response.Body is null");
            }
            
            // Verbose log removed
            
            var tokenData = response.Body.GenerateTokenResponse;
            
            if (tokenData == null)
            {
                _logger.LogError("GenerateTokenResponse is null!");
                throw new InvalidOperationException("GenerateTokenResponse is null");
            }
            
            // Verbose log removed
            
            return new DatTokenReturn
            {
                Token = tokenData.Token ?? string.Empty,
                Expires = tokenData.Expires ?? string.Empty,
                ErrorCode = tokenData.ErrorCode ?? string.Empty,
                ErrorMessage = tokenData.ErrorMessage ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing token response. XML length: {Length}, XML: {XmlResponse}", 
                xmlResponse?.Length ?? 0, xmlResponse);
            throw;
        }
    }

    private DatVehicleTypeReturn ParseVehicleTypeResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatVehicleTypeResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatVehicleTypeResponse)serializer.Deserialize(reader)!;
        return new DatVehicleTypeReturn
        {
            VehicleTypes = new DatVehicleTypes
            {
                VehicleType = response.Body.GetVehicleTypesResponse.VehicleTypes
            }
        };
    }

    private DatManufacturerReturn ParseManufacturerResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatManufacturerResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatManufacturerResponse)serializer.Deserialize(reader)!;
        return new DatManufacturerReturn
        {
            Manufacturers = new DatManufacturers
            {
                Manufacturer = response.Body.GetManufacturersResponse.Manufacturers
            }
        };
    }

    private DatBaseModelReturn ParseBaseModelResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatBaseModelResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatBaseModelResponse)serializer.Deserialize(reader)!;
        return new DatBaseModelReturn
        {
            BaseModels = new DatBaseModels
            {
                BaseModel = response.Body.GetBaseModelsNResponse.BaseModels
            }
        };
    }

    private DatSubModelReturn ParseSubModelResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatSubModelResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatSubModelResponse)serializer.Deserialize(reader)!;
        return new DatSubModelReturn
        {
            SubModels = new DatSubModels
            {
                SubModel = response.Body.GetSubModelsResponse.SubModels
            }
        };
    }

    private DatECodeReturn ParseDatECodeResponse(string xmlResponse)
    {
        try
        {
            // XmlSerializer ile tam parse
            var serializer = new XmlSerializer(typeof(DatECodeResponse));
            using var reader = new StringReader(xmlResponse);
            var response = (DatECodeResponse)serializer.Deserialize(reader)!;
            
            var result = new DatECodeReturn 
            { 
                DatECode = response?.Body?.CompileDatECodeResponse?.DatECode ?? string.Empty,
                DatProcessNos = response?.Body?.CompileDatECodeResponse?.DatProcessNos
            };
            
            if (!string.IsNullOrWhiteSpace(result.DatECode))
            {
                _logger.LogDebug("✅ Parsed datECode: {DatECode}, datProcessNos: {Count}", 
                    result.DatECode, result.DatProcessNos?.Count ?? 0);
                
                if (result.DatProcessNos != null && result.DatProcessNos.Any())
                {
                    _logger.LogInformation("🎯 Found {Count} datProcessNos: {ProcessNos}", 
                        result.DatProcessNos.Count, string.Join(", ", result.DatProcessNos));
                }
                
                return result;
            }
            
            // Fallback: Manuel XML parsing
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlResponse);
            
            var datECodeNode = xmlDoc.SelectSingleNode("//*[local-name()='datECode']");
            if (datECodeNode != null && !string.IsNullOrWhiteSpace(datECodeNode.InnerText))
            {
                result.DatECode = datECodeNode.InnerText.Trim();
                
                // datProcessNo node'larını ara
                var processNoNodes = xmlDoc.SelectNodes("//*[local-name()='datProcessNo']");
                if (processNoNodes != null && processNoNodes.Count > 0)
                {
                    result.DatProcessNos = new List<string>();
                    foreach (XmlNode node in processNoNodes)
                    {
                        if (!string.IsNullOrWhiteSpace(node.InnerText))
                        {
                            result.DatProcessNos.Add(node.InnerText.Trim());
                        }
                    }
                    
                    _logger.LogInformation("🎯 Manual parse - Found {Count} datProcessNos: {ProcessNos}", 
                        result.DatProcessNos.Count, string.Join(", ", result.DatProcessNos));
                }
                
                return result;
            }
            
            return new DatECodeReturn { DatECode = string.Empty };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DatECode response");
            return new DatECodeReturn { DatECode = string.Empty };
        }
    }

    private DatClassificationGroupReturn ParseClassificationGroupResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatClassificationGroupResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatClassificationGroupResponse)serializer.Deserialize(reader)!;
        return new DatClassificationGroupReturn
        {
            ClassificationGroups = response.Body.GetClassificationGroupsResponse.ClassificationGroups
        };
    }

    private DatOptionReturn ParseOptionResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatOptionResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatOptionResponse)serializer.Deserialize(reader)!;
        
        // Verbose log removed
        
        // Combine both Options and OptionsAlternate lists
        var allOptions = new List<DatOption>();
        if (response.Body.GetOptionsbyClassificationResponse.Options != null)
            allOptions.AddRange(response.Body.GetOptionsbyClassificationResponse.Options);
        if (response.Body.GetOptionsbyClassificationResponse.OptionsAlternate != null)
            allOptions.AddRange(response.Body.GetOptionsbyClassificationResponse.OptionsAlternate);
        
        // Verbose log removed
        
        return new DatOptionReturn
        {
            Options = new DatOptions
            {
                Option = allOptions
            }
        };
    }

    private DatEngineOptionReturn ParseEngineOptionResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatEngineOptionResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatEngineOptionResponse)serializer.Deserialize(reader)!;
        return new DatEngineOptionReturn
        {
            EngineOptions = new DatEngineOptions
            {
                EngineOption = response.Body.GetEngineOptionsResponse.EngineOptions
            }
        };
    }

    private DatCarBodyOptionReturn ParseCarBodyOptionResponse(string xmlResponse)
    {
        var serializer = new XmlSerializer(typeof(DatCarBodyOptionResponse));
        using var reader = new StringReader(xmlResponse);
        var response = (DatCarBodyOptionResponse)serializer.Deserialize(reader)!;
        return new DatCarBodyOptionReturn
        {
            CarBodyOptions = new DatCarBodyOptions
            {
                CarBodyOption = response.Body.GetCarBodyOptionsResponse.CarBodyOptions
            }
        };
    }


    private DatPartsReturn ParsePartsResponse(string xmlResponse)
    {
        try
        {
            _logger.LogDebug("🔍 Parsing parts response. XML length: {Length}", xmlResponse.Length);
            
            var serializer = new XmlSerializer(typeof(DatPartsResponse));
            using var reader = new StringReader(xmlResponse);
            var response = (DatPartsResponse)serializer.Deserialize(reader)!;

            _logger.LogDebug("📦 Deserialized response. Body is null: {BodyNull}", response?.Body == null);
            
            // Her iki namespace'i de destekle
            var dpnResponse = response?.Body?.GetSparePartsDetailsForDPNResponse 
                ?? response?.Body?.GetSparePartsDetailsForDPNResponseAlternate;
            
            _logger.LogDebug("📦 DPN Response is null: {DpnNull}", dpnResponse == null);
            
            var results = dpnResponse?.SparePartsDetailsForDPNResponse?.Results ?? new List<SparePartsResultPerDPN>();
            _logger.LogInformation("📦 Found {Count} sparePartsResultPerDPN in response", results.Count);
            
            var flattened = new List<DatPartSimple>();
            foreach (var r in results)
            {
                _logger.LogDebug("📦 Processing DatProcessNumber: {DPN}, Items: {ItemCount}", 
                    r.DatProcessNumber, r.SparePartsInformations?.Items?.Count ?? 0);
                
                // Get vehicle info from first vehicle (if exists)
                var firstVehicle = r.SparePartsVehicles?.Vehicles?.FirstOrDefault();
                _logger.LogDebug("📦 First vehicle: {Manu} {Base}", 
                    firstVehicle?.ManufacturerName ?? "null", firstVehicle?.BaseModelName ?? "null");
                
                if (r.SparePartsInformations?.Items != null)
                {
                    foreach (var item in r.SparePartsInformations.Items)
                    {
                        ExtractPartsRecursive(item, flattened, firstVehicle, r.DatProcessNumber);
                    }
                }
            }

            _logger.LogInformation("✅ ParsePartsResponse completed. Total flattened parts: {Count}", flattened.Count);
            return new DatPartsReturn { Parts = flattened };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing parts response");
            throw;
        }
    }

    private void ExtractPartsRecursive(SparePartsInformation item, List<DatPartSimple> flattened, SparePartsVehicle? firstVehicle, string datProcessNumber)
    {
        // Serialize sub models to JSON for storage
        string? subModelsJson = null;
        if (firstVehicle?.SparePartsSubModels?.SubModels?.Any() == true)
        {
            subModelsJson = System.Text.Json.JsonSerializer.Serialize(
                firstVehicle.SparePartsSubModels.SubModels.Select(sm => new 
                { 
                    Key = sm.SubModel, 
                    Name = sm.SubModelName 
                })
            );
        }
        
        DateTime? priceDate = null;
        if (!string.IsNullOrWhiteSpace(item.PriceDate) && DateTime.TryParse(item.PriceDate, out var parsedDate))
        {
            priceDate = parsedDate;
        }
        
        var part = new DatPartSimple
        {
            PartNumber = item.PartNumber,
            Description = item.Name,
            Name = item.Name,
            NetPrice = item.Price,
            Availability = item.Orderable,
            DatProcessNumber = datProcessNumber,
            PriceDate = priceDate,
            WorkTimeMin = item.WorkTimeMin,
            WorkTimeMax = item.WorkTimeMax,
            VehicleType = firstVehicle?.VehicleType,
            VehicleTypeName = firstVehicle?.VehicleTypeName,
            ManufacturerKey = firstVehicle?.Manufacturer,
            ManufacturerName = firstVehicle?.ManufacturerName,
            BaseModelKey = firstVehicle?.BaseModel,
            BaseModelName = firstVehicle?.BaseModelName,
            DescriptionIdentifier = firstVehicle?.DescriptionIdentifier,
            SubModelsJson = subModelsJson
        };
        
        flattened.Add(part);
        // _logger.LogDebug("➕ Added part: {PartNumber} - {Name}", item.PartNumber, item.Name);

        // Recursive check for RepairSet
        if (item.RepairSet?.Items != null && item.RepairSet.Items.Any())
        {
            foreach (var subItem in item.RepairSet.Items)
            {
                ExtractPartsRecursive(subItem, flattened, firstVehicle, datProcessNumber);
            }
        }
    }
    
    private DatImageReturn ParseImagesResponse(string xmlResponse)
    {
        try
        {
            // Manuel XML parsing (XmlSerializer çalışmıyor)
            var images = new List<DatImage>();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xmlResponse);
            
            var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", "http://sphinx.dat.de/services/VehicleSelectionService");
            nsmgr.AddNamespace("vxs", "http://www.dat.de/vxs");
            
            var imageNodes = doc.SelectNodes("//ns:getImagesResponse/images/vxs:image", nsmgr);
            
            if (imageNodes != null)
            {
                foreach (System.Xml.XmlNode imageNode in imageNodes)
                {
                    var aspect = imageNode.SelectSingleNode("vxs:aspect", nsmgr)?.InnerText ?? "";
                    var imageType = imageNode.SelectSingleNode("vxs:imageType", nsmgr)?.InnerText ?? "";
                    var imageFormat = imageNode.SelectSingleNode("vxs:imageFormat", nsmgr)?.InnerText ?? "";
                    var imageBase64 = imageNode.SelectSingleNode("vxs:imageBase64", nsmgr)?.InnerText ?? "";
                    
                    images.Add(new DatImage
                    {
                        Aspect = aspect,
                        ImageType = imageType,
                        ImageFormat = imageFormat,
                        ImageBase64 = imageBase64
                    });
                }
            }
            
            // Log only if images found
            if (images.Count > 0)
            {
                _logger.LogInformation("🖼️ Parsed {Count} images", images.Count);
            }
            
            return new DatImageReturn 
            { 
                Images = new DatImages { Image = images },
                RawXmlResponse = xmlResponse 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing images response");
            return new DatImageReturn { RawXmlResponse = xmlResponse };
        }
    }
    
    // AlreadyProcessedAsync removed - concurrent DbContext access was causing NpgsqlOperationInProgressException
    // For production: implement bulk check at sync start, not during parallel operations

}
