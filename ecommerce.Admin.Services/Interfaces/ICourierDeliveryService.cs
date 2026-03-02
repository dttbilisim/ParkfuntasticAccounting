using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>Sepet teslimat seçenekleri (il/ilçe → kurye uygun mu) ve siparişe kurye atama.</summary>
public interface ICourierDeliveryService
{
    /// <summary>Adres (cityId, townId, neighboorId) için teslimat seçenekleri: kargo + varsa kurye. latitude/longitude verilirse kurye için ad, araç tipi ve uzaklık (km) doldurulur.</summary>
    Task<IActionResult<List<CourierDeliveryOptionDto>>> GetDeliveryOptions(int cityId, int townId, int? neighboorId = null, double? latitude = null, double? longitude = null);

    /// <summary>Siparişe kurye ata (checkout'ta kurye seçildiğinde).</summary>
    Task<IActionResult<Empty>> AssignCourierToOrder(int orderId, int courierId, int? estimatedMinutes = null);

    /// <summary>Kurye teslimat durumunu güncelle (Accepted, PickedUp, OnTheWay, Delivered, Cancelled).</summary>
    Task<IActionResult<Empty>> UpdateDeliveryStatus(int orderId, CourierDeliveryStatus status, int? courierUserId = null);
}
