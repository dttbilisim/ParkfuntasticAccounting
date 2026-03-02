using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EP.Models;
using ecommerce.EP.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Services.Concreate;

/// <summary>
/// Kurye sipariş bildirimi — sipariş atandığında kuryeye, durum değişikliklerinde müşteriye push bildirim gönderir.
/// Müşteriye giden bildirimler ayrıca UserNotifications tablosuna yazılır; böylece "Bildirimler" sayfasında listelenir.
/// </summary>
public class CourierNotificationService : ICourierNotificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IExpoPushService _expoPushService;
    private readonly ILogger<CourierNotificationService> _logger;

    public CourierNotificationService(
        ApplicationDbContext dbContext,
        IExpoPushService expoPushService,
        ILogger<CourierNotificationService> logger)
    {
        _dbContext = dbContext;
        _expoPushService = expoPushService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendOrderAssignedNotificationAsync(int courierApplicationUserId, string orderNumber, int orderId)
    {
        try
        {
            var title = "Yeni sipariş atandı";
            var body = $"#{orderNumber} siparişi size atandı. Kabul etmek için uygulamayı açın.";
            var deepLink = $"bicops://courier/orders?orderId={orderId}";

            // Kurye "Bildirimler" listesinde görsün diye UserNotification kaydı oluştur (müşteri akışıyla aynı)
            var userNotification = new ecommerce.Core.Entities.UserNotification
            {
                UserId = courierApplicationUserId,
                Title = title,
                Body = body,
                DeepLink = deepLink,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UserNotifications.Add(userNotification);
            await _dbContext.SaveChangesAsync();

            var tokens = await _dbContext.UserPushTokens
                .AsNoTracking()
                .Where(t => t.UserId == courierApplicationUserId && t.IsActive && t.Platform != "web")
                .Select(t => t.Token)
                .Distinct()
                .ToListAsync();

            if (tokens.Count == 0)
            {
                _logger.LogWarning(
                    "Kurye push atlandı — UserId {UserId} için push token yok. Kurye uygulamayı açıp bildirim izni vermeli (bildirim listeye yazıldı). Sipariş #{OrderNumber}",
                    courierApplicationUserId, orderNumber);
                return;
            }

            var request = new PushNotificationRequest
            {
                Tokens = tokens,
                Title = title,
                Body = body,
                Data = new Dictionary<string, string>
                {
                    ["deepLink"] = deepLink,
                    ["orderId"] = orderId.ToString(),
                    ["orderNumber"] = orderNumber
                }
            };

            var result = await _expoPushService.SendAsync(request);
            _logger.LogInformation(
                "Kurye bildirimi gönderildi. Sipariş #{OrderNumber}, Kurye UserId: {UserId}, Başarılı: {Success}, Başarısız: {Failure}",
                orderNumber, courierApplicationUserId, result.SuccessCount, result.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Kurye bildirimi gönderilemedi. Sipariş #{OrderNumber}, Kurye UserId: {UserId}",
                orderNumber, courierApplicationUserId);
        }
    }

    /// <inheritdoc />
    public async Task SendOrderStatusUpdateToCustomerAsync(int customerApplicationUserId, string orderNumber, int orderId, CourierDeliveryStatus status)
    {
        try
        {
            var (title, body) = GetCustomerNotificationMessage(status, orderNumber);
            var deepLink = $"bicops://orders?orderId={orderId}";

            // Her zaman UserNotification kaydı oluştur — müşteri push token olmasa bile "Bildirimler" sayfasında görsün
            var userNotification = new ecommerce.Core.Entities.UserNotification
            {
                UserId = customerApplicationUserId,
                Title = title,
                Body = body,
                DeepLink = deepLink,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UserNotifications.Add(userNotification);
            await _dbContext.SaveChangesAsync();

            var tokens = await _dbContext.UserPushTokens
                .AsNoTracking()
                .Where(t => t.UserId == customerApplicationUserId && t.IsActive && t.Platform != "web")
                .Select(t => t.Token)
                .Distinct()
                .ToListAsync();

            if (tokens.Count == 0)
            {
                _logger.LogInformation(
                    "Müşteri push atlandı — kullanıcı {UserId} için push token yok (bildirim listeye yazıldı). Sipariş #{OrderNumber}, Durum: {Status}",
                    customerApplicationUserId, orderNumber, status);
                return;
            }

            var request = new PushNotificationRequest
            {
                Tokens = tokens,
                Title = title,
                Body = body,
                Data = new Dictionary<string, string>
                {
                    ["deepLink"] = deepLink,
                    ["orderId"] = orderId.ToString(),
                    ["orderNumber"] = orderNumber,
                    ["status"] = status.ToString()
                }
            };

            var result = await _expoPushService.SendAsync(request);
            _logger.LogInformation(
                "Müşteri bildirimi gönderildi. Sipariş #{OrderNumber}, Müşteri UserId: {UserId}, Durum: {Status}, Başarılı: {Success}",
                orderNumber, customerApplicationUserId, status, result.SuccessCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Müşteri bildirimi gönderilemedi. Sipariş #{OrderNumber}, Müşteri UserId: {UserId}, Durum: {Status}",
                orderNumber, customerApplicationUserId, status);
        }
    }

    private static (string Title, string Body) GetCustomerNotificationMessage(CourierDeliveryStatus status, string orderNumber)
    {
        return status switch
        {
            CourierDeliveryStatus.Accepted => ("Kuryeniz siparişinizi kabul etti", $"#{orderNumber} siparişiniz kurye tarafından kabul edildi."),
            CourierDeliveryStatus.PickedUp => ("Siparişiniz hazırlanıyor", $"#{orderNumber} siparişiniz kurye tarafından alındı, hazırlanıyor."),
            CourierDeliveryStatus.OnTheWay => ("Kurye yolda", $"#{orderNumber} siparişiniz kurye ile yolda, size ulaşıyor."),
            CourierDeliveryStatus.Delivered => ("Siparişiniz teslim edildi", $"#{orderNumber} siparişiniz başarıyla teslim edildi."),
            CourierDeliveryStatus.Cancelled => ("Teslimat iptal edildi", $"#{orderNumber} siparişinizin teslimatı iptal edildi."),
            _ => ("Sipariş durumu güncellendi", $"#{orderNumber} siparişinizin durumu güncellendi.")
        };
    }
}
