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

public class CourierLocationService : ICourierLocationService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<CourierLocationService> _logger;

    public CourierLocationService(IUnitOfWork<ApplicationDbContext> context, ILogger<CourierLocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult<Empty>> RecordLocation(int courierId, double latitude, double longitude, double? accuracy = null, int? orderId = null)
    {
        var result = new IActionResult<Empty>();
        try
        {
            var entity = new CourierLocation
            {
                CourierId = courierId,
                OrderId = orderId,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                RecordedAt = DateTime.UtcNow
            };
            _context.DbContext.CourierLocations.Add(entity);
            await _context.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation RecordLocation Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<CourierLocationDto?>> GetLatestForOrder(int orderId)
    {
        var result = new IActionResult<CourierLocationDto?>();
        try
        {
            var loc = await _context.DbContext.CourierLocations
                .AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.RecordedAt)
                .FirstOrDefaultAsync();

            // Siparişe özel konum yoksa, atanmış kuryenin son konumunu kullan (kurye orderId göndermeden de konum paylaşıyor)
            if (loc == null)
            {
                var order = await _context.DbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == orderId && o.CourierId != null)
                    .Select(o => new { o.CourierId })
                    .FirstOrDefaultAsync();
                if (order?.CourierId != null)
                {
                    loc = await _context.DbContext.CourierLocations
                        .AsNoTracking()
                        .Where(x => x.CourierId == order.CourierId)
                        .OrderByDescending(x => x.RecordedAt)
                        .FirstOrDefaultAsync();
                }
            }

            result.Result = loc == null ? null : new CourierLocationDto
            {
                Id = loc.Id,
                CourierId = loc.CourierId,
                OrderId = loc.OrderId,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                Accuracy = loc.Accuracy,
                RecordedAt = DateTime.SpecifyKind(loc.RecordedAt, DateTimeKind.Utc)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation GetLatestForOrder Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<CourierLocationDto?>> GetLatestForCourier(int courierId)
    {
        var result = new IActionResult<CourierLocationDto?>();
        try
        {
            var loc = await _context.DbContext.CourierLocations
                .AsNoTracking()
                .Where(x => x.CourierId == courierId)
                .OrderByDescending(x => x.RecordedAt)
                .FirstOrDefaultAsync();
            result.Result = loc == null ? null : new CourierLocationDto
            {
                Id = loc.Id,
                CourierId = loc.CourierId,
                OrderId = loc.OrderId,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                Accuracy = loc.Accuracy,
                RecordedAt = DateTime.SpecifyKind(loc.RecordedAt, DateTimeKind.Utc)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation GetLatestForCourier Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<List<NearbyCourierDto>>> GetNearbyCouriers(double latitude, double longitude, double radiusKm = 50)
    {
        var result = new IActionResult<List<NearbyCourierDto>> { Result = new List<NearbyCourierDto>() };
        try
        {
            // Son 12 saatte konum gönderen kuryeler (4 saat çok kısıtlayıcı olabiliyordu)
            var cutoff = DateTime.UtcNow.AddHours(-12);
            var recentLocations = await _context.DbContext.CourierLocations
                .AsNoTracking()
                .Where(x => x.RecordedAt >= cutoff)
                .Include(x => x.Courier).ThenInclude(c => c!.ApplicationUser)
                .Include(x => x.Courier).ThenInclude(c => c!.ServiceAreas).ThenInclude(s => s.City)
                .Include(x => x.Courier).ThenInclude(c => c!.ServiceAreas).ThenInclude(s => s.Town)
                .OrderByDescending(x => x.RecordedAt)
                .ToListAsync();

            var latestPerCourier = recentLocations
                .Where(x => x.Courier != null && x.Courier.Status == (int)EntityStatus.Active)
                .GroupBy(x => x.CourierId)
                .Select(g => g.First())
                .ToList();

            var nowTurkey = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)); // Türkiye UTC+3

            var list = new List<NearbyCourierDto>();
            foreach (var loc in latestPerCourier)
            {
                var distanceKm = HaversineKm(latitude, longitude, loc.Latitude, loc.Longitude);
                if (distanceKm > radiusKm) continue;
                var name = loc.Courier?.ApplicationUser != null
                    ? (loc.Courier.ApplicationUser.FullName ?? $"{loc.Courier.ApplicationUser.FirstName} {loc.Courier.ApplicationUser.LastName}".Trim() ?? "Kurye")
                    : "Kurye";

                string? workStart = null;
                string? workEnd = null;
                var withinHours = true;
                var areas = loc.Courier?.ServiceAreas?.Where(s => s.IsActive).ToList() ?? new List<CourierServiceArea>();
                if (areas.Count > 0)
                {
                    var withHours = areas.Where(a => a.WorkStartTime.HasValue && a.WorkEndTime.HasValue).ToList();
                    if (withHours.Count > 0)
                    {
                        workStart = withHours.Min(a => a.WorkStartTime!.Value).ToString("HH:mm");
                        workEnd = withHours.Max(a => a.WorkEndTime!.Value).ToString("HH:mm");
                        withinHours = withHours.Any(a => nowTurkey >= a.WorkStartTime!.Value && nowTurkey <= a.WorkEndTime!.Value);
                    }
                    else
                    {
                        var anyStart = areas.FirstOrDefault(a => a.WorkStartTime.HasValue);
                        var anyEnd = areas.FirstOrDefault(a => a.WorkEndTime.HasValue);
                        if (anyStart != null) workStart = anyStart.WorkStartTime!.Value.ToString("HH:mm");
                        if (anyEnd != null) workEnd = anyEnd.WorkEndTime!.Value.ToString("HH:mm");
                    }
                }

                // Çalışma saati dışındakileri de listeye al (mobilde uyarı ile gösterilir); tamamen gizleme
                // if (!withinHours) continue;

                string? areasSummary = null;
                if (areas.Count > 0)
                {
                    var parts = areas
                        .Where(a => a.City != null || a.Town != null)
                        .Select(a => $"{a.Town?.Name ?? ""} / {a.City?.Name ?? ""}".Trim().Trim('/').Trim())
                        .Where(s => s.Length > 0)
                        .Distinct()
                        .ToList();
                    areasSummary = parts.Count > 0 ? string.Join(", ", parts) : null;
                }

                list.Add(new NearbyCourierDto
                {
                    CourierId = loc.CourierId,
                    CourierName = name,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    DistanceKm = Math.Round(distanceKm, 2),
                    RecordedAt = DateTime.SpecifyKind(loc.RecordedAt, DateTimeKind.Utc),
                    WorkStartTime = workStart,
                    WorkEndTime = workEnd,
                    IsWithinWorkingHours = withinHours,
                    ServiceAreasSummary = areasSummary,
                    IsSubCourier = loc.Courier?.ApplicationUser?.ParentCourierId != null
                });
            }

            if (list.Count == 0)
            {
                if (recentLocations.Count == 0)
                    _logger.LogInformation("GetNearbyCouriers: Son 12 saatte hiç CourierLocation kaydı yok. Kurye uygulamasının konum gönderdiğinden emin olun.");
                else if (latestPerCourier.Count == 0)
                    _logger.LogWarning("GetNearbyCouriers: {Count} konum kaydı var ancak aktif kurye (Status=Active) yok. lat={Lat}, lng={Lng}, radiusKm={Radius}", recentLocations.Count, latitude, longitude, radiusKm);
                else
                    _logger.LogInformation("GetNearbyCouriers: {Active} aktif kurye var ancak hepsi {Radius} km dışında. lat={Lat}, lng={Lng}", latestPerCourier.Count, radiusKm, latitude, longitude);
            }

            result.Result = list.OrderBy(x => x.DistanceKm).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation GetNearbyCouriers Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<List<NearbyVehicleDto>>> GetNearbyVehicles(double latitude, double longitude, double radiusKm = 50)
    {
        var result = new IActionResult<List<NearbyVehicleDto>> { Result = new List<NearbyVehicleDto>() };
        try
        {
            var courierResult = await GetNearbyCouriers(latitude, longitude, radiusKm);
            if (!courierResult.Ok || courierResult.Result == null)
                return result;

            var nearbyCouriers = courierResult.Result;
            if (nearbyCouriers.Count == 0)
            {
                result.Result = new List<NearbyVehicleDto>();
                return result;
            }

            var courierIds = nearbyCouriers.Select(x => x.CourierId).Distinct().ToList();
            var vehiclesByCourier = await _context.DbContext.CourierVehicles
                .AsNoTracking()
                .Where(v => courierIds.Contains(v.CourierId))
                .Include(v => v.DriverUser)
                .OrderBy(v => v.CourierId).ThenBy(v => v.VehicleType).ThenBy(v => v.LicensePlate)
                .ToListAsync();

            var list = new List<NearbyVehicleDto>();
            foreach (var c in nearbyCouriers)
            {
                var vehicles = vehiclesByCourier.Where(v => v.CourierId == c.CourierId).ToList();
                if (vehicles.Count == 0)
                    continue;
                foreach (var v in vehicles)
                {
                    var driverName = !string.IsNullOrWhiteSpace(v.DriverName)
                        ? v.DriverName
                        : (v.DriverUser != null ? $"{v.DriverUser.FirstName} {v.DriverUser.LastName}".Trim() : null);
                    var driverPhone = !string.IsNullOrWhiteSpace(v.DriverPhone) ? v.DriverPhone : v.DriverUser?.PhoneNumber;
                    list.Add(new NearbyVehicleDto
                    {
                        VehicleId = v.Id,
                        CourierId = c.CourierId,
                        VehicleType = (int)v.VehicleType,
                        VehicleTypeName = v.VehicleType.GetDisplayName(),
                        LicensePlate = v.LicensePlate,
                        DriverName = driverName,
                        DriverPhone = driverPhone,
                        CourierName = c.CourierName,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        DistanceKm = c.DistanceKm,
                        RecordedAt = c.RecordedAt,
                        WorkStartTime = c.WorkStartTime,
                        WorkEndTime = c.WorkEndTime,
                        IsWithinWorkingHours = c.IsWithinWorkingHours,
                        ServiceAreasSummary = c.ServiceAreasSummary,
                        IsSubCourier = c.IsSubCourier
                    });
                }
            }

            result.Result = list.OrderBy(x => x.DistanceKm).ThenBy(x => x.VehicleTypeName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation GetNearbyVehicles Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    /// <summary>İl/ilçede hizmet veren kuryelerin son konumlarına göre araç listesi (teslimat adresi bazlı; GPS kullanılmaz).
    /// Sadece son MaxLocationAgeHours saat içinde konum gönderen kuryeler döner — böylece haritada başka şehirde (eski konum) görünme sorunu azalır.</summary>
    public async Task<IActionResult<List<NearbyVehicleDto>>> GetNearbyVehiclesByCityTown(int cityId, int townId)
    {
        var result = new IActionResult<List<NearbyVehicleDto>> { Result = new List<NearbyVehicleDto>() };
        if (cityId <= 0 || townId <= 0)
            return result;

        const int MaxLocationAgeHours = 12;

        try
        {
            var courierIds = await _context.DbContext.CourierServiceAreas
                .AsNoTracking()
                .Where(x => x.CityId == cityId && x.TownId == townId && x.IsActive)
                .Select(x => x.CourierId)
                .Distinct()
                .ToListAsync();

            if (courierIds.Count == 0)
                return result;

            var cutoff = DateTime.UtcNow.AddHours(-MaxLocationAgeHours);
            var locsWithCourier = await _context.DbContext.CourierLocations
                .AsNoTracking()
                .Where(x => courierIds.Contains(x.CourierId) && x.RecordedAt >= cutoff)
                .Include(x => x.Courier).ThenInclude(c => c!.ApplicationUser)
                .Include(x => x.Courier).ThenInclude(c => c!.ServiceAreas).ThenInclude(s => s.City)
                .Include(x => x.Courier).ThenInclude(c => c!.ServiceAreas).ThenInclude(s => s.Town)
                .OrderByDescending(x => x.RecordedAt)
                .ToListAsync();

            var latestPerCourierWithNav = locsWithCourier
                .Where(x => x.Courier != null && x.Courier.Status == (int)EntityStatus.Active)
                .GroupBy(x => x.CourierId)
                .Select(g => g.First())
                .ToList();

            var nowTurkey = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
            var nearbyCouriers = new List<NearbyCourierDto>();
            foreach (var loc in latestPerCourierWithNav)
            {
                var name = loc.Courier?.ApplicationUser != null
                    ? (loc.Courier.ApplicationUser.FullName ?? $"{loc.Courier.ApplicationUser.FirstName} {loc.Courier.ApplicationUser.LastName}".Trim() ?? "Kurye")
                    : "Kurye";
                var areas = loc.Courier?.ServiceAreas?.Where(s => s.IsActive).ToList() ?? new List<CourierServiceArea>();
                string? workStart = null, workEnd = null;
                var withinHours = true;
                if (areas.Count > 0)
                {
                    var withHours = areas.Where(a => a.WorkStartTime.HasValue && a.WorkEndTime.HasValue).ToList();
                    if (withHours.Count > 0)
                    {
                        workStart = withHours.Min(a => a.WorkStartTime!.Value).ToString("HH:mm");
                        workEnd = withHours.Max(a => a.WorkEndTime!.Value).ToString("HH:mm");
                        withinHours = withHours.Any(a => nowTurkey >= a.WorkStartTime!.Value && nowTurkey <= a.WorkEndTime!.Value);
                    }
                }
                string? areasSummary = null;
                if (areas.Count > 0)
                {
                    var parts = areas
                        .Where(a => a.City != null || a.Town != null)
                        .Select(a => $"{a.Town?.Name ?? ""} / {a.City?.Name ?? ""}".Trim().Trim('/').Trim())
                        .Where(s => s.Length > 0)
                        .Distinct()
                        .ToList();
                    areasSummary = parts.Count > 0 ? string.Join(", ", parts) : null;
                }
                // Çalışma saati dışındakileri de listeye al (mobilde uyarı ile gösterilir)
                // if (!withinHours) continue;
                nearbyCouriers.Add(new NearbyCourierDto
                {
                    CourierId = loc.CourierId,
                    CourierName = name,
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    DistanceKm = 0,
                    RecordedAt = DateTime.SpecifyKind(loc.RecordedAt, DateTimeKind.Utc),
                    WorkStartTime = workStart,
                    WorkEndTime = workEnd,
                    IsWithinWorkingHours = withinHours,
                    ServiceAreasSummary = areasSummary,
                    IsSubCourier = loc.Courier?.ApplicationUser?.ParentCourierId != null
                });
            }

            var ids = nearbyCouriers.Select(x => x.CourierId).Distinct().ToList();
            var vehiclesByCourier = await _context.DbContext.CourierVehicles
                .AsNoTracking()
                .Where(v => ids.Contains(v.CourierId))
                .Include(v => v.DriverUser)
                .OrderBy(v => v.CourierId).ThenBy(v => v.VehicleType).ThenBy(v => v.LicensePlate)
                .ToListAsync();

            var list = new List<NearbyVehicleDto>();
            foreach (var c in nearbyCouriers)
            {
                var vehicles = vehiclesByCourier.Where(v => v.CourierId == c.CourierId).ToList();
                if (vehicles.Count == 0)
                    continue;
                foreach (var v in vehicles)
                {
                    var driverName = !string.IsNullOrWhiteSpace(v.DriverName)
                        ? v.DriverName
                        : (v.DriverUser != null ? $"{v.DriverUser.FirstName} {v.DriverUser.LastName}".Trim() : null);
                    var driverPhone = !string.IsNullOrWhiteSpace(v.DriverPhone) ? v.DriverPhone : v.DriverUser?.PhoneNumber;
                    list.Add(new NearbyVehicleDto
                    {
                        VehicleId = v.Id,
                        CourierId = c.CourierId,
                        VehicleType = (int)v.VehicleType,
                        VehicleTypeName = v.VehicleType.GetDisplayName(),
                        LicensePlate = v.LicensePlate,
                        DriverName = driverName,
                        DriverPhone = driverPhone,
                        CourierName = c.CourierName,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        DistanceKm = c.DistanceKm,
                        RecordedAt = c.RecordedAt,
                        WorkStartTime = c.WorkStartTime,
                        WorkEndTime = c.WorkEndTime,
                        IsWithinWorkingHours = c.IsWithinWorkingHours,
                        ServiceAreasSummary = c.ServiceAreasSummary,
                        IsSubCourier = c.IsSubCourier
                    });
                }
            }
            result.Result = list.OrderByDescending(x => x.RecordedAt).ThenBy(x => x.VehicleTypeName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierLocation GetNearbyVehiclesByCityTown Exception");
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
}
