using System.Net.Http.Json;
using System.Text.Json;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EP.Models;
using ecommerce.EP.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Services.Concreate;

/// <summary>
/// Expo Push API üzerinden bildirim gönderim servisi.
/// Token'ları 100'lük batch'lere böler, DeviceNotRegistered token'ları otomatik temizler.
/// Polly retry policy HttpClient üzerinden uygulanır (Program.cs'de yapılandırılır).
/// </summary>
public class ExpoPushService : IExpoPushService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ExpoPushService> _logger;

    /// <summary>Expo Push API tek seferde kabul ettiği maksimum token sayısı</summary>
    private const int BatchSize = 100;

    public ExpoPushService(
        HttpClient httpClient,
        ApplicationDbContext dbContext,
        ILogger<ExpoPushService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PushSendResult> SendAsync(PushNotificationRequest request)
    {
        var result = new PushSendResult();

        if (request.Tokens == null || request.Tokens.Count == 0)
        {
            _logger.LogWarning("Bildirim gönderilecek token listesi boş.");
            return result;
        }

        // Token'ları 100'lük batch'lere böl ve gönder
        var allInvalidTokens = new List<string>();

        foreach (var batch in request.Tokens.Chunk(BatchSize))
        {
            var batchResult = await SendBatchToExpoAsync(
                batch.ToList(), request.Title, request.Body, request.Data);

            result.SuccessCount += batchResult.successCount;
            result.FailureCount += batchResult.failureCount;
            allInvalidTokens.AddRange(batchResult.invalidTokens);
        }

        result.InvalidTokens = allInvalidTokens;

        // Geçersiz token'ları veritabanından temizle
        if (allInvalidTokens.Count > 0)
        {
            await CleanupInvalidTokensAsync(allInvalidTokens);
        }

        _logger.LogInformation(
            "Bildirim gönderimi tamamlandı. Başarılı: {SuccessCount}, Başarısız: {FailureCount}, Geçersiz Token: {InvalidCount}",
            result.SuccessCount, result.FailureCount, result.InvalidTokens.Count);

        // Bildirim log kaydı oluştur
        await LogNotificationAsync(
            request.Title,
            request.Body,
            request.Data,
            request.Tokens.Count,
            result.SuccessCount,
            result.FailureCount,
            result.InvalidTokens);

        return result;
    }

    /// <inheritdoc />
    public async Task<PushBatchResult> SendBatchAsync(
        List<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        var result = new PushBatchResult { TotalSent = tokens.Count };

        if (tokens.Count == 0)
        {
            _logger.LogWarning("Toplu bildirim gönderilecek token listesi boş.");
            return result;
        }

        var allInvalidTokens = new List<string>();
        var batchNumber = 0;

        // Token'ları 100'lük chunk'lara böl
        foreach (var batch in tokens.Chunk(BatchSize))
        {
            batchNumber++;
            var batchList = batch.ToList();

            _logger.LogInformation(
                "Batch {BatchNo}/{TotalBatch} gönderiliyor. Token sayısı: {TokenCount}",
                batchNumber, (int)Math.Ceiling((double)tokens.Count / BatchSize), batchList.Count);

            var batchResult = await SendBatchToExpoAsync(batchList, title, body, data);

            result.TotalSuccess += batchResult.successCount;
            result.TotalFailure += batchResult.failureCount;
            allInvalidTokens.AddRange(batchResult.invalidTokens);
        }

        result.InvalidTokens = allInvalidTokens;

        // Geçersiz token'ları veritabanından temizle
        if (allInvalidTokens.Count > 0)
        {
            await CleanupInvalidTokensAsync(allInvalidTokens);
        }

        _logger.LogInformation(
            "Toplu bildirim gönderimi tamamlandı. Toplam: {Total}, Başarılı: {SuccessCount}, Başarısız: {FailureCount}, Geçersiz Token: {InvalidCount}",
            result.TotalSent, result.TotalSuccess, result.TotalFailure, result.InvalidTokens.Count);

        // Bildirim log kaydı oluştur
        await LogNotificationAsync(
            title,
            body,
            data,
            tokens.Count,
            result.TotalSuccess,
            result.TotalFailure,
            result.InvalidTokens);

        return result;
    }

    /// <inheritdoc />
    public async Task CleanupInvalidTokensAsync(List<string> invalidTokens)
    {
        if (invalidTokens == null || invalidTokens.Count == 0)
            return;

        try
        {
            var tokensToDelete = await _dbContext.UserPushTokens
                .Where(t => invalidTokens.Contains(t.Token))
                .ToListAsync();

            if (tokensToDelete.Count > 0)
            {
                _dbContext.UserPushTokens.RemoveRange(tokensToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Geçersiz token'lar veritabanından silindi. Silinen sayı: {DeletedCount}",
                    tokensToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Geçersiz token temizleme hatası. Token sayısı: {TokenSayisi}",
                invalidTokens.Count);
        }
    }

    /// <summary>
    /// Tek bir batch'i Expo Push API'ye gönderir ve yanıtı parse eder.
    /// </summary>
    private async Task<(int successCount, int failureCount, List<string> invalidTokens)> SendBatchToExpoAsync(
        List<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data)
    {
        var successCount = 0;
        var failureCount = 0;
        var invalidTokens = new List<string>();

        try
        {
            // Expo Push API istek gövdesini oluştur
            var requestBody = tokens.Select(token =>
            {
                var message = new Dictionary<string, object>
                {
                    ["to"] = token,
                    ["title"] = title,
                    ["body"] = body,
                    ["sound"] = "default",
                    ["priority"] = "high"
                };

                // data null değilse ekle
                if (data != null && data.Count > 0)
                {
                    message["data"] = data;
                }

                return message;
            }).ToList();

            // Expo Push API'ye POST isteği gönder
            _logger.LogInformation(
                "Expo Push API'ye istek gönderiliyor. URL: {BaseAddress}send, Token sayısı: {TokenCount}, İstek: {RequestBody}",
                _httpClient.BaseAddress, tokens.Count, JsonSerializer.Serialize(requestBody));

            var response = await _httpClient.PostAsJsonAsync("send", requestBody);
            
            // Yanıtı her durumda oku (400 dahil)
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation(
                "Expo Push API yanıtı. StatusCode: {StatusCode}, Body: {ResponseBody}",
                (int)response.StatusCode, responseContent);

            // 400 ve üzeri hata durumunda response body'yi logla ve çık
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Expo Push API hata döndü. StatusCode: {StatusCode}, Body: {ResponseBody}",
                    (int)response.StatusCode, responseContent);
                failureCount = tokens.Count;
                return (successCount, failureCount, invalidTokens);
            }

            var expoPushResponse = JsonSerializer.Deserialize<ExpoPushResponse>(responseContent);

            if (expoPushResponse?.Data == null)
            {
                _logger.LogWarning("Expo Push API'den boş yanıt alındı.");
                failureCount = tokens.Count;
                return (successCount, failureCount, invalidTokens);
            }

            // Her ticket'ı ilgili token ile eşleştirerek sonuçları değerlendir
            for (var i = 0; i < expoPushResponse.Data.Count && i < tokens.Count; i++)
            {
                var ticket = expoPushResponse.Data[i];

                if (ticket.Status == "ok")
                {
                    successCount++;
                }
                else
                {
                    failureCount++;

                    // DeviceNotRegistered hatası — token geçersiz, temizlenecek
                    if (ticket.Details?.Error == "DeviceNotRegistered")
                    {
                        invalidTokens.Add(tokens[i]);
                        _logger.LogWarning(
                            "DeviceNotRegistered hatası. Token: {Token}",
                            tokens[i]);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Bildirim gönderim hatası. Token: {Token}, Hata: {Hata}, Mesaj: {Mesaj}",
                            tokens[i], ticket.Details?.Error, ticket.Message);
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            // Polly retry policy tüm denemeleri tükettikten sonra buraya düşer
            _logger.LogError(ex,
                "Expo Push API isteği başarısız oldu. Token sayısı: {TokenCount}",
                tokens.Count);
            failureCount = tokens.Count;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Expo Push API yanıtı parse edilemedi. Token sayısı: {TokenCount}",
                tokens.Count);
            failureCount = tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Bildirim gönderiminde beklenmeyen hata. Token sayısı: {TokenCount}",
                tokens.Count);
            failureCount = tokens.Count;
        }

        return (successCount, failureCount, invalidTokens);
    }

    /// <summary>
    /// Bildirim gönderim sonucunu NotificationLog tablosuna kaydeder.
    /// Gönderim başarılı veya başarısız olsa da log kaydı oluşturulur.
    /// </summary>
    private async Task LogNotificationAsync(
        string title,
        string body,
        Dictionary<string, string>? data,
        int totalTargets,
        int successCount,
        int failureCount,
        List<string> invalidTokens)
    {
        try
        {
            // Deep link bilgisini data dictionary'den al
            string? deepLink = null;
            data?.TryGetValue("deepLink", out deepLink);

            // Hata detaylarını JSON olarak hazırla
            string? errorDetails = null;
            if (invalidTokens.Count > 0)
            {
                var errorObject = new
                {
                    invalidTokens,
                    invalidTokenCount = invalidTokens.Count
                };
                errorDetails = JsonSerializer.Serialize(errorObject);
            }

            var logEntry = new NotificationLog
            {
                Title = title,
                Body = body,
                DeepLink = deepLink,
                TargetAudience = "direct",
                SentAt = DateTime.UtcNow,
                TotalTargets = totalTargets,
                SuccessCount = successCount,
                FailureCount = failureCount,
                ErrorDetails = errorDetails
            };

            _dbContext.NotificationLogs.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Bildirim log kaydı oluşturuldu. LogId: {LogId}, Başarılı: {SuccessCount}, Başarısız: {FailureCount}",
                logEntry.Id, successCount, failureCount);
        }
        catch (Exception ex)
        {
            // Log kaydı oluşturma hatası bildirim gönderimini etkilememeli
            _logger.LogError(ex,
                "Bildirim log kaydı oluşturulurken hata oluştu. Başlık: {Baslik}",
                title);
        }
    }
}
