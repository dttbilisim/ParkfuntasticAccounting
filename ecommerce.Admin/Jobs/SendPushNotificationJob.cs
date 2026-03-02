using System.Text.Json;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EP.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Jobs;

/// <summary>
/// Push bildirim gönderim job argümanları.
/// Admin panelden gönderilen bildirim bilgilerini taşır.
/// </summary>
public class SendPushNotificationJobArgs
{
    /// <summary>Bildirim başlığı</summary>
    public string Title { get; set; } = null!;

    /// <summary>Bildirim mesaj metni</summary>
    public string Body { get; set; } = null!;

    /// <summary>Deep link URL (opsiyonel)</summary>
    public string? DeepLink { get; set; }

    /// <summary>Hedef kitle: "all", "merchant", "salesman", "b2c", "user:{id}"</summary>
    public string TargetAudience { get; set; } = null!;

    /// <summary>Bildirimi gönderen admin kullanıcı ID</summary>
    public int SentByUserId { get; set; }
}

/// <summary>
/// Push bildirim gönderim Hangfire job'ı.
/// Hedef kitleye göre token'ları çeker ve IExpoPushService ile gönderir.
/// Gönderim sonuçlarını NotificationLog'a kaydeder.
/// </summary>
[Hangfire.AutomaticRetry(Attempts = 1)]
public class SendPushNotificationJob : IAsyncBackgroundJob<SendPushNotificationJobArgs>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IExpoPushService _expoPushService;
    private readonly IWebPushService _webPushService;
    private readonly ILogger<SendPushNotificationJob> _logger;

    public SendPushNotificationJob(
        ApplicationDbContext dbContext,
        IExpoPushService expoPushService,
        IWebPushService webPushService,
        ILogger<SendPushNotificationJob> logger)
    {
        _dbContext = dbContext;
        _expoPushService = expoPushService;
        _webPushService = webPushService;
        _logger = logger;
    }

    public async Task ExecuteAsync(SendPushNotificationJobArgs args)
    {
        _logger.LogInformation(
            "Push bildirim gönderimi başlatıldı. Hedef kitle: {TargetAudience}, Başlık: {Title}",
            args.TargetAudience, args.Title);

        try
        {
            // 1. Hedef kitleye göre aktif mobil token'ları çek
            var mobileTokens = await GetTargetTokensAsync(args.TargetAudience, isWeb: false);

            // 2. Hedef kitleye göre aktif web token'ları çek
            var webTokens = await GetTargetTokensAsync(args.TargetAudience, isWeb: true);

            var totalTokens = mobileTokens.Count + webTokens.Count;

            if (totalTokens == 0)
            {
                _logger.LogWarning(
                    "Hedef kitleye ait aktif token bulunamadı. Hedef kitle: {TargetAudience}",
                    args.TargetAudience);

                await SaveNotificationLogAsync(args, totalTargets: 0, successCount: 0, failureCount: 0, errorDetails: "Hedef kitleye ait aktif token bulunamadı.");
                return;
            }

            // 3. Deep link varsa data dictionary'ye ekle
            Dictionary<string, string>? data = null;
            if (!string.IsNullOrWhiteSpace(args.DeepLink))
            {
                data = new Dictionary<string, string> { { "deepLink", args.DeepLink } };
            }

            var totalSuccess = 0;
            var totalFailure = 0;
            string? errorDetails = null;
            var allInvalidTokens = new List<string>();

            // 4. Mobil token'ları IExpoPushService ile gönder
            if (mobileTokens.Count > 0)
            {
                _logger.LogInformation(
                    "Mobil bildirim gönderimi başlıyor. Token sayısı: {TokenCount}",
                    mobileTokens.Count);

                var mobileResult = await _expoPushService.SendBatchAsync(mobileTokens, args.Title, args.Body, data);
                totalSuccess += mobileResult.TotalSuccess;
                totalFailure += mobileResult.TotalFailure;
                allInvalidTokens.AddRange(mobileResult.InvalidTokens);
            }

            // 5. Web token'ları IWebPushService ile gönder
            if (webTokens.Count > 0)
            {
                _logger.LogInformation(
                    "Web bildirim gönderimi başlıyor. Token sayısı: {TokenCount}",
                    webTokens.Count);

                var webResult = await _webPushService.SendAsync(webTokens, args.Title, args.Body, args.DeepLink);
                totalSuccess += webResult.SuccessCount;
                totalFailure += webResult.FailureCount;

                // Geçersiz web subscription'ları veritabanından temizle
                if (webResult.InvalidSubscriptions.Count > 0)
                {
                    await CleanupInvalidWebSubscriptionsAsync(webResult.InvalidSubscriptions);
                    allInvalidTokens.AddRange(webResult.InvalidSubscriptions);
                }
            }

            _logger.LogInformation(
                "Push bildirim gönderimi tamamlandı. Toplam: {Total}, Başarılı: {Success}, Başarısız: {Failure}",
                totalTokens, totalSuccess, totalFailure);

            // 6. Gönderim sonucunu NotificationLog'a kaydet
            if (allInvalidTokens.Count > 0)
            {
                errorDetails = JsonSerializer.Serialize(new
                {
                    invalidTokens = allInvalidTokens,
                    invalidTokenCount = allInvalidTokens.Count
                });
            }

            // 7. NotificationLog kaydet ve kullanıcı bildirimlerini oluştur
            var logId = await SaveNotificationLogAsync(
                args,
                totalTargets: totalTokens,
                successCount: totalSuccess,
                failureCount: totalFailure,
                errorDetails: errorDetails);

            // 8. Her hedef kullanıcı için UserNotification kaydı oluştur
            await CreateUserNotificationsAsync(args, logId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Push bildirim gönderiminde hata oluştu. Hedef kitle: {TargetAudience}, Başlık: {Title}",
                args.TargetAudience, args.Title);

            await SaveNotificationLogAsync(args, totalTargets: 0, successCount: 0, failureCount: 0, errorDetails: ex.Message);
        }
    }

    /// <summary>
    /// Hedef kitleye göre aktif token'ları veritabanından çeker.
    /// isWeb parametresine göre web veya mobil token'ları filtreler.
    /// </summary>
    private async Task<List<string>> GetTargetTokensAsync(string targetAudience, bool isWeb)
    {
        // Temel filtre: aktif token'lar, platform bazlı ayırma
        var query = _dbContext.UserPushTokens
            .AsNoTracking()
            .Where(t => t.IsActive);

        // Platform bazlı filtreleme
        if (isWeb)
            query = query.Where(t => t.Platform == "web");
        else
            query = query.Where(t => t.Platform != "web");

        if (targetAudience == "all")
        {
            // Tüm aktif mobil token'lar
        }
        else if (targetAudience == "merchant")
        {
            // CustomerB2B rolündeki kullanıcıların token'ları
            var merchantUserIds = await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "CustomerB2B"))
                .Select(u => u.Id)
                .ToListAsync();

            query = query.Where(t => merchantUserIds.Contains(t.UserId));
        }
        else if (targetAudience == "salesman")
        {
            // Plasiyer rolündeki kullanıcıların token'ları
            var salesmanUserIds = await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "Plasiyer"))
                .Select(u => u.Id)
                .ToListAsync();

            query = query.Where(t => salesmanUserIds.Contains(t.UserId));
        }
        else if (targetAudience == "b2c")
        {
            // B2C rolündeki kullanıcıların token'ları
            var b2cUserIds = await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "B2C"))
                .Select(u => u.Id)
                .ToListAsync();

            query = query.Where(t => b2cUserIds.Contains(t.UserId));
        }
        else if (targetAudience.StartsWith("user:"))
        {
            // Belirli kullanıcının token'ları
            if (int.TryParse(targetAudience.AsSpan(5), out var userId))
            {
                query = query.Where(t => t.UserId == userId);
            }
            else
            {
                _logger.LogWarning("Geçersiz kullanıcı ID formatı: {TargetAudience}", targetAudience);
                return [];
            }
        }
        else
        {
            _logger.LogWarning("Bilinmeyen hedef kitle: {TargetAudience}", targetAudience);
            return [];
        }

        return await query.Select(t => t.Token).Distinct().ToListAsync();
    }

    /// <summary>
    /// Gönderim sonucunu NotificationLog tablosuna kaydeder.
    /// Oluşturulan log kaydının ID'sini döndürür.
    /// </summary>
    private async Task<int?> SaveNotificationLogAsync(
        SendPushNotificationJobArgs args,
        int totalTargets,
        int successCount,
        int failureCount,
        string? errorDetails)
    {
        try
        {
            var logEntry = new NotificationLog
            {
                Title = args.Title,
                Body = args.Body,
                DeepLink = args.DeepLink,
                TargetAudience = args.TargetAudience,
                SentByUserId = args.SentByUserId,
                SentAt = DateTime.UtcNow,
                TotalTargets = totalTargets,
                SuccessCount = successCount,
                FailureCount = failureCount,
                ErrorDetails = errorDetails
            };

            _dbContext.NotificationLogs.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Bildirim log kaydı oluşturuldu. LogId: {LogId}, Hedef kitle: {TargetAudience}",
                logEntry.Id, args.TargetAudience);

            return logEntry.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Bildirim log kaydı oluşturulurken hata oluştu. Başlık: {Title}",
                args.Title);
            return null;
        }
    }

    /// <summary>
    /// Hedef kitleye göre kullanıcı ID'lerini döndürür.
    /// </summary>
    private async Task<List<int>> GetTargetUserIdsAsync(string targetAudience)
    {
        if (targetAudience == "all")
        {
            // Aktif push token'ı olan tüm kullanıcılar
            return await _dbContext.UserPushTokens
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => t.UserId)
                .Distinct()
                .ToListAsync();
        }

        if (targetAudience == "merchant")
        {
            return await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "CustomerB2B"))
                .Select(u => u.Id)
                .ToListAsync();
        }

        if (targetAudience == "salesman")
        {
            return await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "Plasiyer"))
                .Select(u => u.Id)
                .ToListAsync();
        }

        if (targetAudience == "b2c")
        {
            return await _dbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.Name == "B2C"))
                .Select(u => u.Id)
                .ToListAsync();
        }

        if (targetAudience.StartsWith("user:") && int.TryParse(targetAudience.AsSpan(5), out var userId))
        {
            return [userId];
        }

        return [];
    }

    /// <summary>
    /// Her hedef kullanıcı için UserNotification kaydı oluşturur.
    /// Kullanıcılar kendi bildirimlerini mobil uygulamadan görüntüleyebilir.
    /// </summary>
    private async Task CreateUserNotificationsAsync(SendPushNotificationJobArgs args, int? logId)
    {
        try
        {
            var userIds = await GetTargetUserIdsAsync(args.TargetAudience);
            if (userIds.Count == 0) return;

            var now = DateTime.UtcNow;
            var notifications = userIds.Select(uid => new UserNotification
            {
                UserId = uid,
                Title = args.Title,
                Body = args.Body,
                DeepLink = args.DeepLink,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = now,
                NotificationLogId = logId
            }).ToList();

            _dbContext.UserNotifications.AddRange(notifications);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Kullanıcı bildirimleri oluşturuldu. Kullanıcı sayısı: {UserCount}, LogId: {LogId}",
                notifications.Count, logId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Kullanıcı bildirimleri oluşturulurken hata oluştu. Başlık: {Title}",
                args.Title);
        }
    }

    /// <summary>
    /// Geçersiz web push subscription'ları veritabanından siler.
    /// Süresi dolmuş veya artık geçerli olmayan subscription'lar temizlenir.
    /// </summary>
    private async Task CleanupInvalidWebSubscriptionsAsync(List<string> invalidSubscriptions)
    {
        try
        {
            var tokensToDelete = await _dbContext.UserPushTokens
                .Where(t => t.Platform == "web" && invalidSubscriptions.Contains(t.Token))
                .ToListAsync();

            if (tokensToDelete.Count > 0)
            {
                _dbContext.UserPushTokens.RemoveRange(tokensToDelete);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Geçersiz web subscription'lar silindi. Silinen sayı: {DeletedCount}",
                    tokensToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Geçersiz web subscription temizleme hatası. Subscription sayısı: {Count}",
                invalidSubscriptions.Count);
        }
    }
}
