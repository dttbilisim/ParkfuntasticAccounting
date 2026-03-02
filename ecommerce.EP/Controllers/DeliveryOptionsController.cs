using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.EP.Controllers;

/// <summary>Teslimat seçenekleri — adres (il/ilçe/mahalle) için kargo ve kurye seçenekleri.</summary>
[Route("api/[controller]")]
[ApiController]
public class DeliveryOptionsController : ControllerBase
{
    private readonly ICourierDeliveryService _deliveryService;
    private readonly ICourierLocationService _locationService;
    private readonly ILogger<DeliveryOptionsController> _logger;

    public DeliveryOptionsController(
        ICourierDeliveryService deliveryService,
        ICourierLocationService locationService,
        ILogger<DeliveryOptionsController> logger)
    {
        _deliveryService = deliveryService;
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Verilen adres (il, ilçe, opsiyonel mahalle) için teslimat seçeneklerini döner: kargo + varsa kurye.
    /// Checkout öncesi çağrılır; kurye seçeneği varsa mobil kurye radio'su gösterilir.
    /// latitude/longitude verilirse kurye için ad, araç tipi ve kullanıcıya uzaklık (km) doldurulur.
    /// </summary>
    /// <param name="cityId">İl id</param>
    /// <param name="townId">İlçe id</param>
    /// <param name="neighboorId">Mahalle id (opsiyonel)</param>
    /// <param name="latitude">Kullanıcı enlem (opsiyonel; uzaklık hesaplanır)</param>
    /// <param name="longitude">Kullanıcı boylam (opsiyonel)</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] int cityId, [FromQuery] int townId, [FromQuery] int? neighboorId = null, [FromQuery] double? latitude = null, [FromQuery] double? longitude = null)
    {
        if (cityId <= 0 || townId <= 0)
        {
            return BadRequest(new { message = "cityId ve townId zorunludur." });
        }

        var result = await _deliveryService.GetDeliveryOptions(cityId, townId, neighboorId, latitude, longitude);
        if (!result.Ok)
        {
            return BadRequest(new { message = result.GetMetadataMessages() });
        }

        return Ok(result.Result);
    }

    /// <summary>
    /// B2B/B2C kullanıcılar için: Verilen koordinata göre yakındaki kuryeleri döner (isim, konum, uzaklık).
    /// Harita modalında gösterilir.
    /// </summary>
    [HttpGet("nearby-couriers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyCouriers(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 50)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            return BadRequest(new { message = "Geçerli latitude ve longitude giriniz." });

        var result = await _locationService.GetNearbyCouriers(latitude, longitude, radiusKm);
        if (!result.Ok)
            return BadRequest(new { message = result.GetMetadataMessages() });

        return Ok(result.Result ?? new List<ecommerce.Admin.Domain.Dtos.CourierDto.NearbyCourierDto>());
    }

    /// <summary>
    /// Yakındaki araçları döner (haritada araç bazlı gösterim). Koordinat verilirse GPS ile; cityId+townId verilirse o il/ilçede hizmet veren kuryelerin araçları döner (teslimat adresi seçiliyken kullanılır).
    /// </summary>
    [HttpGet("nearby-vehicles")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyVehicles(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double radiusKm = 50,
        [FromQuery] int? cityId = null,
        [FromQuery] int? townId = null)
    {
        if (cityId.HasValue && cityId.Value > 0 && townId.HasValue && townId.Value > 0)
        {
            var resultByTown = await _locationService.GetNearbyVehiclesByCityTown(cityId.Value, townId.Value);
            if (!resultByTown.Ok)
                return BadRequest(new { message = resultByTown.GetMetadataMessages() });
            return Ok(resultByTown.Result ?? new List<ecommerce.Admin.Domain.Dtos.CourierDto.NearbyVehicleDto>());
        }

        if (!latitude.HasValue || !longitude.HasValue)
            return BadRequest(new { message = "latitude ve longitude giriniz veya cityId ve townId giriniz." });
        if (latitude.Value < -90 || latitude.Value > 90 || longitude.Value < -180 || longitude.Value > 180)
            return BadRequest(new { message = "Geçerli latitude ve longitude giriniz." });

        var result = await _locationService.GetNearbyVehicles(latitude.Value, longitude.Value, radiusKm);
        if (!result.Ok)
            return BadRequest(new { message = result.GetMetadataMessages() });

        return Ok(result.Result ?? new List<ecommerce.Admin.Domain.Dtos.CourierDto.NearbyVehicleDto>());
    }
}
