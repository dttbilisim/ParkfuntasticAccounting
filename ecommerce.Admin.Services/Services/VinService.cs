using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities;
using ecommerce.Admin.Services.Dtos.VinDto;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace ecommerce.Admin.Services.Services;

/// <summary>
/// VIN (Vehicle Identification Number) bazlı araç eşleştirme servisi
/// PostgreSQL vin_get_models() fonksiyonu kullanarak araç bilgilerini sorgular
/// </summary>
public class VinService : IVinService
{
    private readonly ecommerce.EFCore.Context.ApplicationDbContext _dbContext;
    private readonly ILogger<VinService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public VinService(
        ecommerce.EFCore.Context.ApplicationDbContext dbContext,
        ILogger<VinService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult<VinDecodeResultDto>> DecodeVinAsync(string vinNumber)
    {
        var result = OperationResult.CreateResult<VinDecodeResultDto>();
        var startTime = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("[VIN-PG] DecodeVinAsync başladı - VIN: {VinNumber}", vinNumber);
            
            // VIN validasyonu
            if (string.IsNullOrWhiteSpace(vinNumber))
            {
   
                result.AddError("VIN numarası boş olamaz");
                result.Result = new VinDecodeResultDto 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "VIN numarası boş olamaz" 
                };
                return result;
            }

            vinNumber = vinNumber.Trim().ToUpperInvariant();

            // VIN 17 karakter olmalı

            // VIN 17 karakter olmalı ama bazı durumlarda (eksik yazım) 15-16 karaktere de izin veriyoruz.
            // DB prosedürü sadece ilk 10-11 karakteri kullandığı için sorun olmaz.
            if (vinNumber.Length < 15 || vinNumber.Length > 17)
            {
                _logger.LogWarning("[VIN-PG] VIN uzunluğu hatalı: {Length}", vinNumber.Length);
                result.AddError("VIN numarası 15-17 karakter arasında olmalıdır");
                result.Result = new VinDecodeResultDto 
                { 
                    VinNumber = vinNumber,
                    IsValid = false, 
                    IsSuccess = false, 
                    ErrorMessage = "VIN numarası 15-17 karakter arasında olmalıdır" 
                };
                return result;
            }

            // VIN sadece harf ve rakam içermeli
            // Fix: I, O, Q karakterlerine izin ver (veritabanı veya kullanıcı hatası toleransı)
            if (!Regex.IsMatch(vinNumber, @"^[A-Z0-9]{15,17}$"))
            {
                _logger.LogWarning("[VIN-PG] VIN geçersiz karakterler içeriyor: {VinNumber}", vinNumber);
                result.AddError("VIN numarası geçersiz karakterler içeriyor");
                result.Result = new VinDecodeResultDto 
                { 
                    VinNumber = vinNumber,
                    IsValid = false, 
                    IsSuccess = false, 
                    ErrorMessage = "VIN numarası geçersiz karakterler içeriyor" 
                };
                return result;
            }

            // WMI (İlk 3 karakter) ve VDS (4-9 karakterler)
            var wmi = vinNumber.Substring(0, 3);
            var vds = vinNumber.Substring(3, 6);

            _logger.LogInformation("[VIN-PG] WMI: {Wmi}, VDS: {Vds}", wmi, vds);
            _logger.LogInformation("[VIN-PG] PostgreSQL vin_get_models() fonksiyonu çağrılıyor...");

            // PostgreSQL'den vin_get_models() fonksiyonu ile veri çek
            var connection = _dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State == System.Data.ConnectionState.Closed;
            
            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            var matchedVehicles = new List<VinVehicleDto>();
            string? manufacturerName = null;

            try
            {
                await using var command = connection.CreateCommand();
                // row_to_json ile PostgreSQL composite type'ı JSON'a çevir
                command.CommandText = "SELECT row_to_json(vin_get_models(@vinNumber))";
                command.CommandTimeout = 30; // 30 saniye timeout
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@vinNumber";
                parameter.Value = vinNumber;
                command.Parameters.Add(parameter);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                // PostgreSQL fonksiyonundan dönen JSON object'i parse et
                var jsonResult = reader.GetString(0);
                
                _logger.LogInformation("[VIN-PG] PostgreSQL sonucu: {JsonPreview}...", 
                    jsonResult.Substring(0, Math.Min(200, jsonResult.Length)));

                // JSON object'i parse et
                var jsonDoc = JsonDocument.Parse(jsonResult);
                var vehicleElement = jsonDoc.RootElement;

                // Üretici adını al
                manufacturerName = vehicleElement.GetProperty("manufacturer").GetString();

                // OEM parts listesini parse et
                var oemParts = new List<VinOemPartDto>();
                if (vehicleElement.TryGetProperty("oem_parts", out var oemPartsElement) && 
                    oemPartsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in oemPartsElement.EnumerateArray())
                    {
                        oemParts.Add(new VinOemPartDto
                        {
                            Oem = part.GetProperty("oem").GetString() ?? "",
                            Name = part.GetProperty("name").GetString() ?? ""
                        });
                    }
                }

                // DatProcessNumbers listesini parse et
                var datProcessNumbers = new List<string>();
                if (vehicleElement.TryGetProperty("dat_process_numbers", out var datProcessElement))
                {
                    _logger.LogInformation("[VIN-DPN] dat_process_numbers field bulundu - ValueKind: {ValueKind}", 
                        datProcessElement.ValueKind);
                    
                    if (datProcessElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("[VIN-DPN] Array olarak parse ediliyor...");
                        foreach (var processNum in datProcessElement.EnumerateArray())
                        {
                            var processNumStr = processNum.GetString();
                            if (!string.IsNullOrWhiteSpace(processNumStr))
                            {
                                datProcessNumbers.Add(processNumStr);
                            }
                        }
                        _logger.LogInformation("[VIN-DPN] Parse edildi - Toplam: {Count} kod", datProcessNumbers.Count);
                    }
                    else
                    {
                        _logger.LogWarning("[VIN-DPN] HATA: Array değil! ValueKind: {ValueKind}, RawText: {RawText}", 
                            datProcessElement.ValueKind, datProcessElement.GetRawText());
                    }
                }
                else
                {
                    _logger.LogWarning("[VIN-DPN] UYARI: dat_process_numbers field bulunamadı!");
                }

                // VinVehicleDto oluştur
                var vehicle = new VinVehicleDto
                {
                    Wmi = wmi,
                    VdsCode = vds,
                    ManufacturerKey = vehicleElement.GetProperty("manufacturer_key").GetString() ?? "",
                    ManufacturerName = vehicleElement.GetProperty("manufacturer").GetString() ?? "",
                    BaseModelKey = vehicleElement.GetProperty("base_model_key").GetString() ?? "",
                    BaseModelName = vehicleElement.GetProperty("model_name").GetString() ?? "",
                    MatchMethod = vehicleElement.TryGetProperty("match_method", out var matchProp) 
                        ? matchProp.GetString() ?? "" 
                        : "",
                    ModelYear = vehicleElement.TryGetProperty("model_year", out var yearProp) && 
                                yearProp.ValueKind == JsonValueKind.Number 
                        ? yearProp.GetInt32() 
                        : 0,
                    OemParts = oemParts,
                    DatProcessNumbers = datProcessNumbers
                };

                matchedVehicles.Add(vehicle);
            }

            startTime.Stop();
            _logger.LogInformation("[VIN-PG] PostgreSQL sorgusu tamamlandı - Süre: {Duration}ms, Araç sayısı: {Count}", 
                startTime.ElapsedMilliseconds, matchedVehicles.Count);
            }
            finally
            {
                // Sadece biz açtıysak kapat
                if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            var decodeResult = new VinDecodeResultDto
            {
                VinNumber = vinNumber,
                IsValid = true,
                Wmi = wmi,
                Vds = vds,
                ManufacturerName = manufacturerName,
                MatchedVehicles = matchedVehicles,
                IsSuccess = matchedVehicles.Any(),
                ErrorMessage = matchedVehicles.Any() ? null : "VIN numarası için araç bilgisi bulunamadı",
                // Tüm araçlardan DatProcessNumbers'ı topla ve DISTINCT yap
                DatProcessNumbers = matchedVehicles
                    .SelectMany(v => v.DatProcessNumbers)
                    .Where(dpn => !string.IsNullOrWhiteSpace(dpn))
                    .Distinct()
                    .ToList()
            };

            // Fallback: Kesin eşleşme yoksa sasisorgulama.com'dan veri çek
            // FALLBACK_BRAND = hiç model bulunamadı, YEAR_FILTERED = sadece yıl bazlı tahmin
            var isFallback = matchedVehicles.Any(v => 
                v.BaseModelName == "Bilinmeyen Model" 
                || v.MatchMethod == "FALLBACK_BRAND"
                || v.MatchMethod == "YEAR_FILTERED"
                || v.MatchMethod == "CACHE_WMI_YEAR_MISMATCH");
            var noMatch = !matchedVehicles.Any();

            if (isFallback || noMatch)
            {
                _logger.LogInformation("[VIN-SCRAPE] Model bulunamadı, sasisorgulama.com'dan veri çekilecek - VIN: {Vin}", vinNumber);
                
                var scraped = await ScrapeVinFromExternalAsync(vinNumber);
                if (scraped)
                {
                    _logger.LogInformation("[VIN-SCRAPE] Veri başarıyla kaydedildi, prosedür tekrar çağrılıyor - VIN: {Vin}", vinNumber);
                    
                    // Prosedürü tekrar çağır (artık VinExternalScrapedData tablosunda veri var)
                    var retryResult = await CallVinProcedureAsync(vinNumber, wmi, vds);
                    if (retryResult.vehicles.Any() && 
                        !retryResult.vehicles.Any(v => v.BaseModelName == "Bilinmeyen Model" || v.MatchMethod == "FALLBACK_BRAND"))
                    {
                        decodeResult.MatchedVehicles = retryResult.vehicles;
                        decodeResult.ManufacturerName = retryResult.manufacturerName ?? manufacturerName;
                        decodeResult.IsSuccess = true;
                        decodeResult.ErrorMessage = null;
                        decodeResult.DatProcessNumbers = retryResult.vehicles
                            .SelectMany(v => v.DatProcessNumbers)
                            .Where(dpn => !string.IsNullOrWhiteSpace(dpn))
                            .Distinct()
                            .ToList();
                        
                        _logger.LogInformation("[VIN-SCRAPE] Tekrar sorgu başarılı - Model: {Model}", 
                            retryResult.vehicles.FirstOrDefault()?.BaseModelName);
                    }
                    else
                    {
                        _logger.LogWarning("[VIN-SCRAPE] Tekrar sorgu sonrası hala model bulunamadı - VIN: {Vin}", vinNumber);
                    }
                }
            }

            // Eşleşen araçlara fotoğraf ekle — DotCompiledCodes → DatECode → DotVehicleImages
            if (decodeResult.MatchedVehicles.Any())
            {
                await AttachVehicleImagesAsync(decodeResult);
            }

            _logger.LogInformation("[VIN-PG] DecodeVinAsync tamamlandı - IsSuccess: {IsSuccess}, MatchedVehicles: {Count}, DatProcessNumbers: {DpnCount}", 
                decodeResult.IsSuccess, decodeResult.MatchedVehicles.Count, decodeResult.DatProcessNumbers.Count);
            result.Result = decodeResult;
        }
        catch (Exception ex)
        {
            startTime.Stop();
            _logger.LogError(ex, "[VIN-PG] DecodeVinAsync exception - Süre: {Duration}ms", startTime.ElapsedMilliseconds);
            result.AddSystemError($"VIN decode hatası: {ex.Message}");
            result.Result = new VinDecodeResultDto 
            { 
                VinNumber = vinNumber,
                IsSuccess = false, 
                ErrorMessage = ex.Message 
            };
        }

        return result;
    }

    /// <summary>
    /// PostgreSQL vin_get_models() prosedürünü çağırır ve sonuçları parse eder
    /// </summary>
    private async Task<(List<VinVehicleDto> vehicles, string? manufacturerName)> CallVinProcedureAsync(
        string vinNumber, string wmi, string vds)
    {
        var matchedVehicles = new List<VinVehicleDto>();
        string? manufacturerName = null;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == System.Data.ConnectionState.Closed;
        
        if (shouldCloseConnection)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT row_to_json(vin_get_models(@vinNumber))";
            command.CommandTimeout = 30;
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@vinNumber";
            parameter.Value = vinNumber;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var jsonResult = reader.GetString(0);
                var jsonDoc = JsonDocument.Parse(jsonResult);
                var vehicleElement = jsonDoc.RootElement;

                manufacturerName = vehicleElement.GetProperty("manufacturer").GetString();

                var oemParts = new List<VinOemPartDto>();
                if (vehicleElement.TryGetProperty("oem_parts", out var oemPartsElement) && 
                    oemPartsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in oemPartsElement.EnumerateArray())
                    {
                        oemParts.Add(new VinOemPartDto
                        {
                            Oem = part.GetProperty("oem").GetString() ?? "",
                            Name = part.GetProperty("name").GetString() ?? ""
                        });
                    }
                }

                var datProcessNumbers = new List<string>();
                if (vehicleElement.TryGetProperty("dat_process_numbers", out var datProcessElement) &&
                    datProcessElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var processNum in datProcessElement.EnumerateArray())
                    {
                        var processNumStr = processNum.GetString();
                        if (!string.IsNullOrWhiteSpace(processNumStr))
                            datProcessNumbers.Add(processNumStr);
                    }
                }

                matchedVehicles.Add(new VinVehicleDto
                {
                    Wmi = wmi,
                    VdsCode = vds,
                    ManufacturerKey = vehicleElement.GetProperty("manufacturer_key").GetString() ?? "",
                    ManufacturerName = vehicleElement.GetProperty("manufacturer").GetString() ?? "",
                    BaseModelKey = vehicleElement.GetProperty("base_model_key").GetString() ?? "",
                    BaseModelName = vehicleElement.GetProperty("model_name").GetString() ?? "",
                    MatchMethod = vehicleElement.TryGetProperty("match_method", out var matchProp) 
                        ? matchProp.GetString() ?? "" : "",
                    ModelYear = vehicleElement.TryGetProperty("model_year", out var yearProp) && 
                                yearProp.ValueKind == JsonValueKind.Number 
                        ? yearProp.GetInt32() : 0,
                    OemParts = oemParts,
                    DatProcessNumbers = datProcessNumbers
                });
            }
        }
        finally
        {
            if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
                await connection.CloseAsync();
        }

        return (matchedVehicles, manufacturerName);
    }

    /// <summary>
    /// sasisorgulama.com'dan VIN bilgilerini çeker ve VinExternalScrapedData tablosuna kaydeder
    /// </summary>
    private async Task<bool> ScrapeVinFromExternalAsync(string vinNumber)
    {
        try
        {
            // Daha önce scrape edilmiş mi kontrol et
            var existing = await _dbContext.Set<VinExternalScrapedData>()
                .FirstOrDefaultAsync(v => v.Vin == vinNumber);
            
            if (existing != null)
            {
                // Bilinen hatalı label değerleri - eski regex parser bunları değer olarak kaydediyordu
                var knownBadValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Üretici Bilgileri", "Araç Model Bilgileri", "Araç Kodu",
                    "MARKA :", "ARAÇ FABRİKA KODU :", "ÜRETİM YILI :", "ÜRETİM YERİ :"
                };
                
                var isValidScrape = !string.IsNullOrWhiteSpace(existing.ManufacturerCode) 
                    && !knownBadValues.Contains(existing.ManufacturerCode)
                    && !string.IsNullOrWhiteSpace(existing.ModelCode)
                    && !knownBadValues.Contains(existing.ModelCode);
                
                if (isValidScrape)
                {
                    _logger.LogInformation("[VIN-SCRAPE] VIN zaten doğru scrape edilmiş - VIN: {Vin}, MfrCode: {MfrCode}, ModelCode: {ModelCode}", 
                        vinNumber, existing.ManufacturerCode, existing.ModelCode);
                    return true;
                }
                
                // Hatalı parse edilmiş, sil ve tekrar çek
                _logger.LogInformation("[VIN-SCRAPE] Önceki scrape hatalı, tekrar çekilecek - VIN: {Vin}, MfrCode: {MfrCode}, ModelCode: {ModelCode}", 
                    vinNumber, existing.ManufacturerCode, existing.ModelCode);
                _dbContext.Set<VinExternalScrapedData>().Remove(existing);
                await _dbContext.SaveChangesAsync();
            }

            var client = _httpClientFactory.CreateClient("SasiSorgulama");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", 
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("Referer", "https://www.sasisorgulama.com/sasi-numarasi/");

            // Önce cookie almak için GET isteği
            await client.GetAsync("https://www.sasisorgulama.com/sasi-numarasi/");

            // POST ile VIN sorgula
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("kod", vinNumber),
                new KeyValuePair<string, string>("sorgula", "Sorgula")
            });

            var response = await client.PostAsync("https://www.sasisorgulama.com/sasi-numarasi/", formData);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[VIN-SCRAPE] HTTP hatası: {StatusCode} - VIN: {Vin}", response.StatusCode, vinNumber);
                return false;
            }

            var html = await response.Content.ReadAsStringAsync();
            var parsed = ParseSasiSorgulamaHtml(html);

            if (parsed == null || !parsed.Any(kv => !string.IsNullOrWhiteSpace(kv.Value)))
            {
                _logger.LogWarning("[VIN-SCRAPE] HTML'den veri parse edilemedi - VIN: {Vin}", vinNumber);
                return false;
            }

            // Tabloya kaydet
            var scrapedData = new VinExternalScrapedData
            {
                Vin = vinNumber,
                Brand = parsed.GetValueOrDefault("marka"),
                FactoryCode = parsed.GetValueOrDefault("fabrika_kodu"),
                ProductionYear = parsed.GetValueOrDefault("uretim_yili"),
                ProductionPlace = parsed.GetValueOrDefault("uretim_yeri"),
                ManufacturerCode = parsed.GetValueOrDefault("uretici_kodu"),
                ModelCode = parsed.GetValueOrDefault("model_kodu"),
                VehicleCode = parsed.GetValueOrDefault("arac_kodu"),
                ScrapeDate = DateTime.UtcNow
            };

            _dbContext.Set<VinExternalScrapedData>().Add(scrapedData);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[VIN-SCRAPE] Veri kaydedildi - VIN: {Vin}, Marka: {Brand}, FabrikaKodu: {FactoryCode}, ModelKodu: {ModelCode}", 
                vinNumber, scrapedData.Brand, scrapedData.FactoryCode, scrapedData.ModelCode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VIN-SCRAPE] Scraping hatası - VIN: {Vin}", vinNumber);
            return false;
        }
    }

    /// <summary>
    /// sasisorgulama.com HTML yanıtını parse eder
    /// Python SasiSorgulamaParser'ın birebir C# portu - state machine yaklaşımı
    /// HTML tag'lerini sıyırıp text node'ları sırayla işler
    /// </summary>
    private Dictionary<string, string>? ParseSasiSorgulamaHtml(string html)
    {
        var results = new Dictionary<string, string>();

        // Python'daki label eşleştirmeleri - birebir aynı
        var labels = new Dictionary<string, string>
        {
            { "MARKA :", "marka" },
            { "ARAÇ FABRİKA KODU :", "fabrika_kodu" },
            { "ÜRETİM YILI :", "uretim_yili" },
            { "ÜRETİM YERİ :", "uretim_yeri" },
            { "Üretici Bilgileri", "uretici_kodu" },
            { "Araç Model Bilgileri", "model_kodu" },
            { "Araç Kodu", "arac_kodu" }
        };

        // Box label'ları - bunlar pending queue'ya eklenir
        var boxLabelSet = new HashSet<string>
        {
            "Üretici Bilgileri",
            "Araç Model Bilgileri",
            "Araç Kodu"
        };

        // HTML tag'lerini sıyır, sadece text node'ları al
        var textNodes = ExtractTextNodes(html);

        // State machine - Python'daki handle_data mantığının birebir portu
        string? currentLabel = null;
        bool captureNext = false;
        var pendingBoxLabels = new Queue<string>();

        foreach (var rawText in textNodes)
        {
            var cleanData = rawText.Trim();
            if (string.IsNullOrEmpty(cleanData))
                continue;

            // Bu text bir label mı?
            if (labels.ContainsKey(cleanData))
            {
                if (boxLabelSet.Contains(cleanData))
                {
                    // Box label - pending queue'ya ekle
                    pendingBoxLabels.Enqueue(cleanData);
                    captureNext = false;
                }
                else
                {
                    // Normal label - sonraki text'i yakala
                    currentLabel = cleanData;
                    captureNext = true;
                }
                continue;
            }

            // Bu text bir değer mi?
            if (pendingBoxLabels.Count > 0)
            {
                // Pending box label'ın değeri
                var label = pendingBoxLabels.Dequeue();
                results[labels[label]] = cleanData;
            }
            else if (captureNext && currentLabel != null)
            {
                // Normal label'ın değeri
                results[labels[currentLabel]] = cleanData;
                captureNext = false;
                currentLabel = null;
            }
        }

        _logger.LogInformation("[VIN-PARSE] Parse sonuçları: {Results}", 
            string.Join(", ", results.Select(kv => $"{kv.Key}={kv.Value}")));

        return results.Count > 0 ? results : null;
    }

    /// <summary>
    /// HTML'den tag'leri sıyırarak text node'ları sırayla çıkarır
    /// Python HTMLParser.handle_data() davranışını taklit eder
    /// </summary>
    private static List<string> ExtractTextNodes(string html)
    {
        var textNodes = new List<string>();
        var i = 0;

        while (i < html.Length)
        {
            if (html[i] == '<')
            {
                // Tag'ı atla
                var closeIndex = html.IndexOf('>', i);
                if (closeIndex == -1)
                    break;
                i = closeIndex + 1;
            }
            else
            {
                // Text node'u topla
                var tagStart = html.IndexOf('<', i);
                if (tagStart == -1)
                    tagStart = html.Length;

                var text = html.Substring(i, tagStart - i);
                // HTML entity decode
                text = System.Net.WebUtility.HtmlDecode(text);
                if (!string.IsNullOrWhiteSpace(text))
                    textNodes.Add(text);

                i = tagStart;
            }
        }

        return textNodes;
    }

    public async Task<IActionResult<List<string>>> GetOemCodesByManufacturerAsync(string manufacturerName)
    {
        var result = OperationResult.CreateResult<List<string>>();

        try
        {
            if (string.IsNullOrWhiteSpace(manufacturerName))
            {
                result.AddError("Üretici adı boş olamaz");
                return result;
            }

            _logger.LogInformation("[VIN-PG] GetOemCodesByManufacturerAsync - Üretici: {ManufacturerName}", manufacturerName);

            // PostgreSQL'den üreticiye ait OEM kodlarını çek
            var connection = _dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State == System.Data.ConnectionState.Closed;
            
            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                // vin_data tablosundan üreticiye ait tüm OEM kodlarını çek
                command.CommandText = @"
                SELECT DISTINCT UPPER(REPLACE(REPLACE(oem_part->>'oem', ' ', ''), '-', '')) as oem_code
                FROM vin_data,
                     jsonb_array_elements(oem_parts) as oem_part
                WHERE LOWER(manufacturer) = LOWER(@manufacturerName)
                  AND oem_part->>'oem' IS NOT NULL
                  AND oem_part->>'oem' != ''
                ORDER BY oem_code";
            
                command.CommandTimeout = 30;
            
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@manufacturerName";
                parameter.Value = manufacturerName;
                command.Parameters.Add(parameter);

                var oemCodes = new List<string>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var oemCode = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(oemCode))
                    {
                        oemCodes.Add(oemCode);
                    }
                }

                _logger.LogInformation("[VIN-PG] GetOemCodesByManufacturerAsync tamamlandı - OEM kod sayısı: {Count}", oemCodes.Count);

                if (!oemCodes.Any())
                {
                    result.AddError($"Üretici '{manufacturerName}' için OEM kodu bulunamadı");
                }

                result.Result = oemCodes;
            }
            finally
            {
                // Sadece biz açtıysak kapat
                if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VIN-PG] GetOemCodesByManufacturerAsync exception");
            result.AddSystemError($"OEM kodları alma hatası: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Eşleşen araçlar için DotCompiledCodes → DatECode → DotVehicleImages zinciri ile fotoğraf çeker
    /// Öncelik: SIDEVIEW → ANGULARFRONT → herhangi biri
    /// </summary>
    private async Task AttachVehicleImagesAsync(VinDecodeResultDto decodeResult)
    {
        foreach (var vehicle in decodeResult.MatchedVehicles)
        {
            if (string.IsNullOrWhiteSpace(vehicle.ManufacturerKey) || string.IsNullOrWhiteSpace(vehicle.BaseModelKey))
                continue;

            try
            {
                // 1. DotCompiledCodes'dan DatECode bul
                var datECode = await _dbContext.Set<DotCompiledCode>()
                    .AsNoTracking()
                    .Where(c => c.ManufacturerKey == vehicle.ManufacturerKey
                             && c.BaseModelKey == vehicle.BaseModelKey
                             && c.IsActive)
                    .Select(c => c.DatECode)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(datECode))
                {
                    _logger.LogDebug("[VIN-IMG] DatECode bulunamadı - MfrKey: {MfrKey}, BaseModelKey: {BaseModelKey}",
                        vehicle.ManufacturerKey, vehicle.BaseModelKey);
                    continue;
                }

                // 2. DotVehicleImages'dan DatECode ile fotoğraf çek
                var image = await _dbContext.Set<DotVehicleImage>()
                    .AsNoTracking()
                    .Where(i => i.DatECode == datECode && i.IsActive)
                    .OrderByDescending(i => i.Aspect == "SIDEVIEW" ? 2 : i.Aspect == "ANGULARFRONT" ? 1 : 0)
                    .FirstOrDefaultAsync();

                if (image != null)
                {
                    vehicle.VehicleImageBase64 = image.ImageBase64;
                    vehicle.VehicleImageFormat = image.ImageFormat;
                    vehicle.VehicleImageAspect = image.Aspect;
                    _logger.LogInformation("[VIN-IMG] Fotoğraf bulundu - DatECode: {DatECode}, Aspect: {Aspect}",
                        datECode, image.Aspect);
                }
                else
                {
                    _logger.LogDebug("[VIN-IMG] Fotoğraf bulunamadı - DatECode: {DatECode}", datECode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[VIN-IMG] Fotoğraf çekme hatası - MfrKey: {MfrKey}, BaseModelKey: {BaseModelKey}",
                    vehicle.ManufacturerKey, vehicle.BaseModelKey);
            }
        }
    }
}
