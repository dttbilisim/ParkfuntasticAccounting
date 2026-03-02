using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>Kurye konum kaydı ve canlı takip.</summary>
public interface ICourierLocationService
{
    /// <summary>Kurye konum gönderir (mobil uygulama).</summary>
    Task<IActionResult<Empty>> RecordLocation(int courierId, double latitude, double longitude, double? accuracy = null, int? orderId = null);

    /// <summary>Sipariş için son konum (müşteri canlı takip).</summary>
    Task<IActionResult<CourierLocationDto?>> GetLatestForOrder(int orderId);

    /// <summary>Kuryenin son konumu.</summary>
    Task<IActionResult<CourierLocationDto?>> GetLatestForCourier(int courierId);

    /// <summary>Verilen koordinata göre yakındaki kuryeleri döner (B2B/B2C harita modal).</summary>
    Task<IActionResult<List<NearbyCourierDto>>> GetNearbyCouriers(double latitude, double longitude, double radiusKm = 50);

    /// <summary>Verilen koordinata göre yakındaki araçları döner (haritada araç bazlı gösterim).</summary>
    Task<IActionResult<List<NearbyVehicleDto>>> GetNearbyVehicles(double latitude, double longitude, double radiusKm = 50);

    /// <summary>Verilen il/ilçede hizmet veren kuryelerin araçlarını döner (teslimat adresi seçiliyken GPS yerine adres bazlı).</summary>
    Task<IActionResult<List<NearbyVehicleDto>>> GetNearbyVehiclesByCityTown(int cityId, int townId);
}
