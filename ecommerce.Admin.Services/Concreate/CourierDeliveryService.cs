using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Extensions;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate;

public class CourierDeliveryService : ICourierDeliveryService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<CourierDeliveryService> _logger;
    private readonly ICourierNotificationService? _notificationService;

    public CourierDeliveryService(
        IUnitOfWork<ApplicationDbContext> context,
        ILogger<CourierDeliveryService> logger,
        ICourierNotificationService? notificationService = null)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<IActionResult<List<CourierDeliveryOptionDto>>> GetDeliveryOptions(int cityId, int townId, int? neighboorId = null, double? latitude = null, double? longitude = null)
    {
        var result = new IActionResult<List<CourierDeliveryOptionDto>> { Result = new List<CourierDeliveryOptionDto>() };
        try
        {
            result.Result.Add(new CourierDeliveryOptionDto
            {
                Type = DeliveryOptionType.Cargo,
                DisplayName = "Kargo"
            });

            var courierIdsFromAreas = await _context.DbContext.CourierServiceAreas
                .AsNoTracking()
                .Where(x => x.CityId == cityId && x.TownId == townId)
                .Where(x => !neighboorId.HasValue || x.NeighboorId == neighboorId)
                .Select(x => x.CourierId)
                .Distinct()
                .Take(10)
                .ToListAsync();

            // Bu bölgede hizmeti olan ana kuryelerin alt kullanıcı (sub-user) kuryelerini de ekle
            var appIdsWithArea = await _context.DbContext.Couriers
                .AsNoTracking()
                .Where(c => courierIdsFromAreas.Contains(c.Id))
                .Select(c => c.ApplicationUserId)
                .Distinct()
                .ToListAsync();
            var subUserAppIds = await _context.DbContext.AspNetUsers
                .AsNoTracking()
                .Where(u => u.ParentCourierId != null && appIdsWithArea.Contains(u.ParentCourierId.Value))
                .Select(u => u.Id)
                .ToListAsync();
            var courierIdsSub = await _context.DbContext.Couriers
                .AsNoTracking()
                .Where(c => subUserAppIds.Contains(c.ApplicationUserId) && c.Status == (int)EntityStatus.Active)
                .Select(c => c.Id)
                .ToListAsync();
            var courierIds = courierIdsFromAreas.Union(courierIdsSub).Distinct().Take(20).ToList();

            var couriers = await _context.DbContext.Couriers
                .AsNoTracking()
                .Include(x => x.ApplicationUser)
                .Include(x => x.Vehicles)
                .Where(x => courierIds.Contains(x.Id) && x.Status == (int)EntityStatus.Active)
                .ToListAsync();

            // Konum bilgisi varsa: sadece son 4 saatte konum gönderen kuryeleri al (Yakındaki Kuryeler ile aynı kural)
            const int maxLocationAgeHours = 4;
            var locationCutoff = DateTime.UtcNow.AddHours(-maxLocationAgeHours);

            Dictionary<int, (double Lat, double Lng)>? latestLocationByCourier = null;
            if (latitude.HasValue && longitude.HasValue && couriers.Count > 0)
            {
                var ids = couriers.Select(x => x.Id).ToList();
                var latestLocations = await _context.DbContext.CourierLocations
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.CourierId) && x.RecordedAt >= locationCutoff)
                    .OrderByDescending(x => x.RecordedAt)
                    .ToListAsync();
                latestLocationByCourier = latestLocations
                    .GroupBy(x => x.CourierId)
                    .ToDictionary(g => g.Key, g => (g.First().Latitude, g.First().Longitude));
            }

            foreach (var courier in couriers)
            {
                // Lat/long verildiyse sadece son 4 saatte konum gönderen kuryeleri listele (Teslimat Yöntemi = Yakındaki Kuryeler ile aynı set)
                if (latestLocationByCourier != null && !latestLocationByCourier.ContainsKey(courier.Id))
                    continue;
                var courierName = courier.ApplicationUser != null
                    ? (courier.ApplicationUser.FullName ?? $"{courier.ApplicationUser.FirstName} {courier.ApplicationUser.LastName}".Trim())
                    : "Kurye";
                if (string.IsNullOrWhiteSpace(courierName)) courierName = "Kurye";

                var firstVehicle = courier.Vehicles?.OrderBy(v => v.VehicleType).ThenBy(v => v.LicensePlate).FirstOrDefault();
                var vehicleTypeName = firstVehicle != null ? firstVehicle.VehicleType.GetDisplayName() : null;

                double? distanceKm = null;
                if (latestLocationByCourier != null && latestLocationByCourier.TryGetValue(courier.Id, out var loc))
                {
                    distanceKm = Math.Round(HaversineKm(latitude!.Value, longitude!.Value, loc.Lat, loc.Lng), 2);
                }

                // Mesafe varsa tahmini süreyi hesapla (ortalama ~25 km/h şehir içi), yoksa varsayılan 60 dk
                const double avgSpeedKmh = 25.0;
                int? estimatedMinutes = 60;
                if (distanceKm.HasValue && distanceKm.Value >= 0)
                {
                    var minutes = (distanceKm.Value / avgSpeedKmh) * 60.0;
                    estimatedMinutes = (int)Math.Clamp(Math.Round(minutes), 15, 180);
                }

                result.Result.Add(new CourierDeliveryOptionDto
                {
                    Type = DeliveryOptionType.Courier,
                    DisplayName = "Kurye – Hızlı teslimat",
                    EstimatedMinutes = estimatedMinutes,
                    CourierId = courier.Id,
                    CourierName = courierName,
                    VehicleTypeName = vehicleTypeName,
                    DistanceKm = distanceKm
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierDelivery GetDeliveryOptions Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<IActionResult<Empty>> AssignCourierToOrder(int orderId, int courierId, int? estimatedMinutes = null)
    {
        var result = new IActionResult<Empty>();
        try
        {
            var order = await _context.DbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                result.AddError("Sipariş bulunamadı.");
                return result;
            }
            var courier = await _context.DbContext.Couriers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == courierId && x.Status == (int)EntityStatus.Active);
            if (courier == null)
            {
                result.AddError("Kurye bulunamadı veya aktif değil.");
                return result;
            }

            order.CourierId = courierId;
            order.CourierDeliveryStatus = CourierDeliveryStatus.Assigned;
            order.DeliveryOptionType = DeliveryOptionType.Courier;
            order.EstimatedCourierDeliveryMinutes = estimatedMinutes;
            await _context.DbContext.SaveChangesAsync();

            if (_notificationService != null)
            {
                var courierWithUser = await _context.DbContext.Couriers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == courierId);
                if (courierWithUser != null)
                {
                    try
                    {
                        await _notificationService.SendOrderAssignedNotificationAsync(
                            courierWithUser.ApplicationUserId,
                            order.OrderNumber ?? $"#{order.Id}",
                            order.Id);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx,
                            "Kurye bildirimi gönderilemedi (sipariş atandı). OrderId: {OrderId}, CourierId: {CourierId}",
                            orderId, courierId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierDelivery AssignCourierToOrder Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<Empty>> UpdateDeliveryStatus(int orderId, CourierDeliveryStatus status, int? courierUserId = null)
    {
        var result = new IActionResult<Empty>();
        try
        {
            var order = await _context.DbContext.Orders
                .Include(x => x.Courier)
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                result.AddError("Sipariş bulunamadı.");
                return result;
            }
            if (order.CourierId == null)
            {
                result.AddError("Bu siparişe kurye atanmamış.");
                return result;
            }
            if (courierUserId.HasValue && order.Courier?.ApplicationUserId != courierUserId.Value)
            {
                result.AddError("Bu sipariş size atanmamış.");
                return result;
            }
            order.CourierDeliveryStatus = status;

            // Sipariş kabul edildiğinde: Hangi araç/şoför kabul ettiyse Order.CourierVehicleId set et (kargo takip listesinde doğru isim için)
            if (status == CourierDeliveryStatus.Accepted && courierUserId.HasValue && order.CourierId.HasValue)
            {
                var vehicle = await _context.DbContext.CourierVehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.CourierId == order.CourierId.Value && v.DriverUserId == courierUserId.Value);
                if (vehicle != null)
                    order.CourierVehicleId = vehicle.Id;
            }

            // OrderStatusType ve OrderItems kargo alanları: kurye akışına göre güncelle
            switch (status)
            {
                case CourierDeliveryStatus.Accepted:
                    order.OrderStatusType = OrderStatusType.OrderPrepare; // Hazırlanıyor
                    break;
                case CourierDeliveryStatus.PickedUp:
                    // Aldım: Sipariş Hazırlanıyor kalır, OrderItems kargo alanlarını güncelle. Müşteriye bildirim gider.
                    var now = DateTime.UtcNow;
                    foreach (var item in order.OrderItems ?? new List<OrderItems>())
                    {
                        item.CargoRequestHandled = true;
                        item.ShipmentDate = now;
                        item.CargoExternalId = $"KURYE-{orderId}";
                        item.CargoTrackNumber = order.OrderNumber ?? $"#{orderId}";
                    }
                    order.OrderStatusType = OrderStatusType.OrderPrepare; // Hazırlanıyor
                    break;
                case CourierDeliveryStatus.OnTheWay:
                    // Yola Çıktım: Sipariş Kargoda'ya geçer, OrderItems kargo alanlarını güncelle (eksikse). Müşteriye bildirim gider.
                    var onTheWayNow = DateTime.UtcNow;
                    foreach (var item in order.OrderItems ?? new List<OrderItems>())
                    {
                        if (!(item.CargoRequestHandled ?? false))
                        {
                            item.CargoRequestHandled = true;
                            item.ShipmentDate = onTheWayNow;
                            item.CargoExternalId = $"KURYE-{orderId}";
                            item.CargoTrackNumber = order.OrderNumber ?? $"#{orderId}";
                        }
                    }
                    order.OrderStatusType = OrderStatusType.OrderinCargo; // Kargoda
                    break;
                case CourierDeliveryStatus.Delivered:
                    order.OrderStatusType = OrderStatusType.OrderSuccess;
                    order.OrderCompletedDate = DateTime.UtcNow;
                    break;
            }

            await _context.DbContext.SaveChangesAsync();

            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.SendOrderStatusUpdateToCustomerAsync(
                        order.CompanyId,
                        order.OrderNumber ?? $"#{order.Id}",
                        order.Id,
                        status);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx,
                        "Müşteri bildirimi gönderilemedi (sipariş durumu kaydedildi). OrderId: {OrderId}, CompanyId: {CompanyId}, Status: {Status}",
                        orderId, order.CompanyId, status);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierDelivery UpdateDeliveryStatus Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }
}
