using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Controllers;

/// <summary>Kurye paneli API — sipariş listesi, kabul, durum, konum. Track endpoint'i müşteri veya kurye erişebilir.</summary>
[Authorize]
[Route("api/courier")]
[ApiController]
public class CourierController : ControllerBase
{
    private readonly ICourierService _courierService;
    private readonly ICourierDeliveryService _deliveryService;
    private readonly ICourierLocationService _locationService;
    private readonly IRedisCacheService _redis;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CourierController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    private static readonly TimeSpan DeliveryCodeTtl = TimeSpan.FromMinutes(15);

    public CourierController(
        ICourierService courierService,
        ICourierDeliveryService deliveryService,
        ICourierLocationService locationService,
        IRedisCacheService redis,
        IEmailService emailService,
        ApplicationDbContext db,
        ILogger<CourierController> logger,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _courierService = courierService;
        _deliveryService = deliveryService;
        _locationService = locationService;
        _redis = redis;
        _emailService = emailService;
        _db = db;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    private int? GetApplicationUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            return null;
        return id;
    }

    /// <summary>
    /// Giriş yapmış kuryenin bilgisi (parentCourierId). Mobil: footer'da Harita sekmesini alt kuryede gizlemek için kullanılır.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var user = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (user == null)
            return Ok(new { parentCourierId = (int?)null });

        return Ok(new { parentCourierId = user.ParentCourierId });
    }

    /// <summary>
    /// Kuryenin kendine atanmış siparişlerini listeler (duruma göre filtre opsiyonel).
    /// Alt kurye: sadece kendi siparişleri. Ana kurye: kendisi + tüm alt kuryelerin siparişleri.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("orders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders([FromQuery] CourierDeliveryStatus? status = null)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return Ok(new List<object>()); // Kurye kaydı yoksa boş liste

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Ok(new List<object>());

        List<int> allowedCourierIds;
        if (currentUser.ParentCourierId.HasValue)
        {
            // Alt kurye: sadece kendi siparişleri
            allowedCourierIds = new List<int> { courierResult.Result.Id };
        }
        else
        {
            // Ana kurye: kendisi + tüm alt kuryelerin siparişleri
            var subUserAppIds = await _db.AspNetUsers
                .AsNoTracking()
                .Where(u => u.ParentCourierId == applicationUserId.Value)
                .Select(u => u.Id)
                .ToListAsync();
            var allAppIds = new List<int> { applicationUserId.Value };
            allAppIds.AddRange(subUserAppIds);
            allowedCourierIds = await _db.Couriers
                .AsNoTracking()
                .Where(c => allAppIds.Contains(c.ApplicationUserId))
                .Select(c => c.Id)
                .ToListAsync();
            if (allowedCourierIds.Count == 0)
                allowedCourierIds.Add(courierResult.Result.Id);
        }

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Cargo)
            .Include(o => o.UserAddress).ThenInclude(a => a!.City)
            .Include(o => o.UserAddress).ThenInclude(a => a!.Town)
            .Include(o => o.OrderItems)
            .Include(o => o.ApplicationUser)
            .Include(o => o.User)
            .Where(o => o.CourierId != null && allowedCourierIds.Contains(o.CourierId.Value));

        if (status.HasValue)
            query = query.Where(o => o.CourierDeliveryStatus == status.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

        var currentUserCourierId = courierResult.Result.Id;

        // Atanan kurye adları: Include bazen yüklemeyebilir; ayrı sorgu ile garanti alıyoruz.
        var orderCourierIds = orders.Where(o => o.CourierId.HasValue).Select(o => o.CourierId!.Value).Distinct().ToList();
        var courierNames = new Dictionary<int, string>();
        if (orderCourierIds.Count > 0)
        {
            var couriersWithUser = await _db.Couriers
                .AsNoTracking()
                .Include(c => c.ApplicationUser)
                .Where(c => orderCourierIds.Contains(c.Id))
                .ToListAsync();
            foreach (var courier in couriersWithUser)
            {
                var name = courier.ApplicationUser != null
                    ? (courier.ApplicationUser.FullName ?? $"{courier.ApplicationUser.FirstName} {courier.ApplicationUser.LastName}".Trim())
                    : "Kurye";
                if (!string.IsNullOrWhiteSpace(name))
                    courierNames[courier.Id] = name;
            }
        }

        // Sipariş özeti modalı için finansal alanlar (alt toplam, indirim, kargo, genel toplam).
        // CargoType=1 (BicoJET) ise kargo ücreti Cargo.Amount (tek ücret); diğerlerinde Order.CargoPrice.
        var list = orders.Select(o =>
        {
            var cargoPrice = o.Cargo != null && o.Cargo.CargoType == CargoType.BicopsExpress
                ? o.Cargo.Amount
                : o.CargoPrice;
            return new
            {
                o.Id,
                o.OrderNumber,
                CourierDeliveryStatus = o.CourierDeliveryStatus,
                StatusName = o.CourierDeliveryStatus != null ? o.CourierDeliveryStatus.ToString() : "",
                o.CreatedDate,
                AddressSummary = o.UserAddress != null
                    ? $"{o.UserAddress.Address}, {(o.UserAddress.Town != null ? o.UserAddress.Town.Name : "")} / {(o.UserAddress.City != null ? o.UserAddress.City.Name : "")}"
                    : "",
                RecipientName = string.IsNullOrWhiteSpace(o.UserAddress?.FullName ?? o.DeliveryTo ?? "")
                    ? (o.ApplicationUser?.FullName ?? o.User?.FullName ?? "")
                    : (o.UserAddress?.FullName ?? o.DeliveryTo ?? ""),
                ProductTotal = o.ProductTotal,
                DiscountTotal = o.DiscountTotal ?? 0,
                CargoPrice = cargoPrice,
                OrderTotal = o.OrderTotal,
                GrandTotal = o.GrandTotal,
                CanManage = o.CourierId == currentUserCourierId,
                AssignedCourierName = o.CourierId.HasValue && courierNames.TryGetValue(o.CourierId.Value, out var courierName) ? courierName : null,
                Items = (o.OrderItems ?? new List<ecommerce.Core.Entities.OrderItems>())
                    .Select(i => new
                    {
                        ProductName = i.ProductName ?? "",
                        i.Quantity,
                        CargoDesi = i.CargoDesi
                    })
                    .ToList()
            };
        })
        .ToList();

        return Ok(list);
    }

    /// <summary>
    /// Siparişi kurye kabul eder (CourierDeliveryStatus → Accepted).
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("orders/{orderId:int}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptOrder(int orderId)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var result = await _deliveryService.UpdateDeliveryStatus(orderId, CourierDeliveryStatus.Accepted, applicationUserId);
        if (!result.Ok)
            return BadRequest(new { message = result.GetMetadataMessages() });

        return Ok(new { message = "Sipariş kabul edildi." });
    }

    /// <summary>
    /// Müşteriye 4 haneli teslimat doğrulama kodu gönderir. Kod Redis'te 15 dk saklanır.
    /// Kurye "Teslim Ettim" demeden önce bu endpoint çağrılır, müşteri e-postasına kod gider.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("orders/{orderId:int}/send-delivery-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendDeliveryCode(int orderId)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Courier)
            .Include(o => o.ApplicationUser)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });
        if (order.CourierId == null || order.Courier?.ApplicationUserId != applicationUserId.Value)
            return StatusCode(403, new { message = "Bu sipariş size atanmamış." });
        if (order.CourierDeliveryStatus != CourierDeliveryStatus.OnTheWay)
            return BadRequest(new { message = "Sadece yoldaki siparişler için kod gönderilebilir." });

        var customerEmail = order.ApplicationUser?.Email;
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest(new { message = "Müşteri e-posta adresi bulunamadı." });

        var code = new Random().Next(1000, 9999).ToString();
        var redisKey = $"delivery_code:{orderId}";
        await _redis.SetAsync(redisKey, code, DeliveryCodeTtl);

        try
        {
            var fullName = order.ApplicationUser?.FullName ?? order.UserFullName ?? "Müşteri";
            await _emailService.SendDeliveryVerificationCodeEmail(
                fullName,
                customerEmail,
                order.OrderNumber ?? $"#{orderId}",
                code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teslimat kodu e-posta gönderilemedi orderId={OrderId}", orderId);
            await _redis.RemoveAsync(redisKey);
            return BadRequest(new { message = "E-posta gönderilemedi. Lütfen tekrar deneyin." });
        }

        return Ok(new { message = "Doğrulama kodu müşteri e-postasına gönderildi.", expiresInMinutes = 15 });
    }

    /// <summary>
    /// Kurye teslimat durumunu günceller (PickedUp, OnTheWay, Delivered, Cancelled).
    /// Delivered için deliveryCode zorunludur — müşteri e-postasına gönderilen 4 haneli kod.
    /// </summary>
    /// <param name="orderId">Sipariş id</param>
    /// <param name="body">{"status": "...", "deliveryCode": "1234" (sadece Delivered için zorunlu)}</param>
    [Authorize(Roles = "Courier")]
    [HttpPost("orders/{orderId:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] CourierOrderStatusRequest body)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        if (body?.Status == null)
            return BadRequest(new { message = "status alanı zorunludur." });

        if (body.Status == CourierDeliveryStatus.Delivered)
        {
            var code = (body.DeliveryCode ?? "").Trim();
            if (code.Length != 4 || !code.All(char.IsDigit))
                return BadRequest(new { message = "Teslimat için 4 haneli doğrulama kodu giriniz." });

            var redisKey = $"delivery_code:{orderId}";
            var stored = await _redis.GetAsync<string>(redisKey);
            if (string.IsNullOrEmpty(stored) || stored != code)
                return BadRequest(new { message = "Geçersiz veya süresi dolmuş kod. Lütfen müşteriden kodu alıp tekrar deneyin." });

            await _redis.RemoveAsync(redisKey);
        }

        var result = await _deliveryService.UpdateDeliveryStatus(orderId, body.Status.Value, applicationUserId);
        if (!result.Ok)
            return BadRequest(new { message = result.GetMetadataMessages() });

        return Ok(new { message = "Durum güncellendi." });
    }

    /// <summary>
    /// Kurye anlık konum gönderir (teslimat sırasında periyodik çağrılır). CourierId JWT kullanıcısından türetilir.
    /// </summary>
    /// <param name="body">latitude, longitude zorunlu; orderId ve accuracy opsiyonel.</param>
    [Authorize(Roles = "Courier")]
    [HttpPost("location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordLocation([FromBody] CourierLocationRequest body)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        if (body == null || body.Latitude < -90 || body.Latitude > 90 || body.Longitude < -180 || body.Longitude > 180)
            return BadRequest(new { message = "latitude (-90..90) ve longitude (-180..180) zorunludur ve geçerli aralıkta olmalıdır." });

        var courierResult = await _courierService.GetOrCreateByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
        {
            var msg = courierResult.GetMetadataMessages();
            return StatusCode(403, new { message = string.IsNullOrWhiteSpace(msg) ? "Kurye kaydı oluşturulamadı." : msg });
        }

        var result = await _locationService.RecordLocation(
            courierResult.Result.Id,
            body.Latitude,
            body.Longitude,
            body.Accuracy,
            body.OrderId);
        if (!result.Ok)
            return BadRequest(new { message = result.GetMetadataMessages() });

        return Ok(new { message = "Konum kaydedildi." });
    }

    /// <summary>
    /// Kuryenin kendi hizmet bölgelerini listeler (bölge bazlı çalışma saatleri dahil).
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("vehicles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyVehicles()
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return Ok(new List<object>());

        var vehiclesResult = await _courierService.GetVehicles(courierResult.Result.Id);
        if (!vehiclesResult.Ok)
            return BadRequest(new { message = vehiclesResult.GetMetadataMessages() });

        _logger.LogInformation("[Courier] GetMyVehicles ok for courier {CourierId}", courierResult.Result.Id);
        return Ok(vehiclesResult.Result ?? new List<CourierVehicleListDto>());
    }

    /// <summary>
    /// Kurye araç ekler veya günceller. Body: { "id": null | number, "vehicleType": 0-3, "licensePlate": "34 ABC 123" }
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("vehicles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveMyVehicle([FromBody] CourierVehicleUpsertDto dto)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetOrCreateByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return BadRequest(new { message = courierResult.GetMetadataMessages() ?? "Kurye kaydı oluşturulamadı." });

        var saveResult = await _courierService.SaveVehicle(courierResult.Result.Id, dto ?? new CourierVehicleUpsertDto(), applicationUserId);
        if (!saveResult.Ok)
            return BadRequest(new { message = saveResult.GetMetadataMessages() });

        _logger.LogInformation("[Courier] SaveMyVehicle ok for courier {CourierId}, plate {LicensePlate}", courierResult.Result.Id, dto?.LicensePlate);
        return Ok(saveResult.Result);
    }

    /// <summary>
    /// Kurye aracını siler. Araca bağlı hizmet bölgesi varsa silinemez.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpDelete("vehicles/{vehicleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyVehicle(int vehicleId)
    {
        return await DeleteMyVehicleCore(vehicleId);
    }

    /// <summary>
    /// Kurye aracını siler (POST). Gerçek cihazlarda CORS/DELETE sorunları nedeniyle POST alternatifi sunulur.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("vehicles/{vehicleId:int}/delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyVehiclePost(int vehicleId)
    {
        return await DeleteMyVehicleCore(vehicleId);
    }

    private async Task<IActionResult> DeleteMyVehicleCore(int vehicleId)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetOrCreateByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return BadRequest(new { message = courierResult.GetMetadataMessages() ?? "Kurye kaydı oluşturulamadı." });

        var deleteResult = await _courierService.DeleteVehicle(courierResult.Result.Id, vehicleId);
        if (!deleteResult.Ok)
            return BadRequest(new { message = deleteResult.GetMetadataMessages() });

        return Ok(new { message = "Araç silindi." });
    }

    /// <summary>
    /// Kuryenin kendi hizmet bölgelerini listeler (bölge bazlı çalışma saatleri dahil).
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("service-areas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyServiceAreas()
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return Ok(new List<object>());

        var areasResult = await _courierService.GetServiceAreas(courierResult.Result.Id);
        if (!areasResult.Ok)
            return BadRequest(new { message = areasResult.GetMetadataMessages() });

        return Ok(areasResult.Result ?? new List<CourierServiceAreaListDto>());
    }

    /// <summary>
    /// Kurye kendi hizmet bölgelerini ve bölge bazlı çalışma saatlerini günceller.
    /// Body: [{ "cityId": 34, "townId": 1, "neighboorId": null, "workStartTime": "09:00", "workEndTime": "18:00" }, ...]
    /// PUT ve POST desteklenir; canlı ortamda PUT engellenebildiği için POST kullanılabilir.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPut("service-areas")]
    [HttpPost("service-areas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveMyServiceAreas([FromBody] List<CourierServiceAreaUpsertDto> areas)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var courierResult = await _courierService.GetOrCreateByApplicationUserId(applicationUserId.Value);
        if (!courierResult.Ok || courierResult.Result == null)
            return BadRequest(new { message = courierResult.GetMetadataMessages() ?? "Kurye kaydı oluşturulamadı." });

        var saveResult = await _courierService.SaveServiceAreas(courierResult.Result.Id, areas ?? new List<CourierServiceAreaUpsertDto>());
        if (!saveResult.Ok)
            return BadRequest(new { message = saveResult.GetMetadataMessages() });

        return Ok(new { message = "Hizmet bölgeleri kaydedildi." });
    }

    /// <summary>
    /// Sipariş için kurye son konumunu döner (canlı takip). Sipariş sahibi (müşteri) veya atanmış kurye çağırabilir.
    /// </summary>
    [HttpGet("orders/{orderId:int}/track")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderTrack(int orderId)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Courier)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        // Sipariş sahibi (CompanyId = müşteri/kullanıcı) veya bu siparişe atanmış kurye erişebilir
        var isOwner = order.CompanyId == applicationUserId.Value;
        var isCourier = order.CourierId.HasValue && order.Courier != null && order.Courier.ApplicationUserId == applicationUserId.Value;
        if (!isOwner && !isCourier)
            return StatusCode(403, new { message = "Bu siparişin konum bilgisine erişim yetkiniz yok." });

        // Kurye atanmamışsa boş dön; atanmışsa siparişe özel son konumu kullan (orderId ile gönderilen varsa), yoksa kuryenin genel son konumunu al
        if (!order.CourierId.HasValue)
            return Ok(new { });

        var result = await _locationService.GetLatestForOrder(orderId);
        // Sadece gerçek hata varsa (exception, Metadata Error/SystemError) BadRequest dön. Konum henüz yoksa (Result=null) 200 OK ile boş dön.
        var hasError = result.Exception != null
            || (result.Metadata != null && (result.Metadata.Type == MetaDataType.Error || result.Metadata.Type == MetaDataType.SystemError));
        if (hasError)
        {
            var msg = result.GetMetadataMessages();
            if (string.IsNullOrWhiteSpace(msg) && result.Exception != null)
                msg = result.Exception.Message ?? result.Exception.ToString();
            if (string.IsNullOrWhiteSpace(msg))
                msg = "Konum bilgisi alınırken bir hata oluştu.";
            _logger.LogWarning("GetOrderTrack orderId={OrderId} hasError: Exception={Ex}, MetadataType={Type}",
                orderId, result.Exception?.Message, result.Metadata?.Type);
            return BadRequest(new { message = msg.Trim() });
        }

        return Ok(result.Result ?? (object)new { });
    }

    /// <summary>
    /// Ana kurye alt kullanıcı (kurye) ekler. Oluşturulan kullanıcıya e-posta ile giriş bilgileri gönderilir, Kurye rolü atanır, Courier kaydı oluşturulur.
    /// İsteğe bağlı araç ve hizmet bölgesi (il/ilçe) gönderilirse alt kullanıcıya ait araç ve bölge kaydedilir.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("sub-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddSubUser([FromBody] AddSubUserRequest model)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        if (currentUser.ParentCourierId.HasValue)
            return StatusCode(403, new { message = "Sadece ana kurye alt kurye ekleyebilir." });

        if (string.IsNullOrWhiteSpace(model.Email))
            return BadRequest(new { message = "E-posta zorunludur." });
        if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            return BadRequest(new { message = "Ad ve soyad zorunludur." });
        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            return BadRequest(new { message = "Telefon numarası zorunludur." });
        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
            return BadRequest(new { message = "Parola zorunludur (en az 6 karakter)." });

        var password = model.Password!;

        var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (existingUser != null)
            return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            RegisterDate = DateTime.UtcNow,
            EmailConfirmed = true,
            ParentCourierId = applicationUserId.Value
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Kayıt başarısız: {errors}" });
        }

        const string courierRoleName = "Courier";
        if (!await _roleManager.RoleExistsAsync(courierRoleName))
            await _roleManager.CreateAsync(new ApplicationRole { Name = courierRoleName });
        await _userManager.AddToRoleAsync(user, courierRoleName);

        var courierResult = await _courierService.GetOrCreateByApplicationUserId(user.Id);
        if (!courierResult.Ok || courierResult.Result == null)
        {
            _logger.LogWarning("Alt kullanıcı için Courier kaydı oluşturulamadı: UserId={UserId}", user.Id);
        }
        else
        {
            if (model.VehicleType.HasValue && !string.IsNullOrWhiteSpace(model.LicensePlate))
            {
                var vehicleDto = new CourierVehicleUpsertDto
                {
                    VehicleType = model.VehicleType.Value,
                    LicensePlate = model.LicensePlate!.Trim(),
                    DriverName = $"{user.FirstName} {user.LastName}".Trim(),
                    DriverPhone = user.PhoneNumber
                };
                await _courierService.SaveVehicle(courierResult.Result.Id, vehicleDto);
            }
            if (model.CityId.HasValue && model.TownId.HasValue)
            {
                var areas = new List<CourierServiceAreaUpsertDto>
                {
                    new CourierServiceAreaUpsertDto
                    {
                        CityId = model.CityId.Value,
                        TownId = model.TownId.Value,
                        CourierVehicleId = null,
                        IsActive = true
                    }
                };
                await _courierService.SaveServiceAreas(courierResult.Result.Id, areas);
            }
        }

        // Parola istemciden geldiği için e-posta göndermiyoruz; alt kurye e-posta + parola ile giriş yapar.

        _logger.LogInformation("[Courier] Alt kurye eklendi. ParentUserId={ParentId}, SubUserId={SubId}, Email={Email}",
            applicationUserId.Value, user.Id, user.Email);

        return Ok(new
        {
            message = "Alt kurye oluşturuldu. E-posta ve belirlediğiniz parola ile giriş yapabilir.",
            userId = user.Id,
            email = user.Email
        });
    }

    /// <summary>
    /// Ana kuryenin eklediği alt kullanıcıları listeler.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("sub-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubUsers()
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser?.ParentCourierId != null)
            return StatusCode(403, new { message = "Sadece ana kurye alt kullanıcı listesini görebilir." });

        var subUsers = await _db.AspNetUsers
            .AsNoTracking()
            .Where(u => u.ParentCourierId == applicationUserId.Value)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.RegisterDate
            })
            .ToListAsync();

        return Ok(subUsers);
    }

    /// <summary>
    /// Ana kurye: Kendi alt kuryelerinin listesini ve son konumlarını döner (harita ekranı için).
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpGet("sub-courier-locations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubCourierLocations()
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        if (currentUser.ParentCourierId != null)
            return StatusCode(403, new { message = "Sadece ana kurye alt kurye konumlarını görebilir." });

        var subUsers = await _db.AspNetUsers
            .AsNoTracking()
            .Where(u => u.ParentCourierId == applicationUserId.Value)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber })
            .ToListAsync();

        var list = new List<SubCourierLocationDto>();

        foreach (var sub in subUsers)
        {
            var courier = await _db.Couriers
                .AsNoTracking()
                .Where(c => c.ApplicationUserId == sub.Id)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync();

            double? lat = null;
            double? lng = null;
            DateTime? recordedAt = null;

            if (courier != null)
            {
                var locResult = await _locationService.GetLatestForCourier(courier.Id);
                if (locResult.Result != null)
                {
                    lat = locResult.Result.Latitude;
                    lng = locResult.Result.Longitude;
                    recordedAt = locResult.Result.RecordedAt;
                }
            }

            list.Add(new SubCourierLocationDto
            {
                UserId = sub.Id,
                FirstName = sub.FirstName ?? "",
                LastName = sub.LastName ?? "",
                Email = sub.Email ?? "",
                PhoneNumber = sub.PhoneNumber,
                CourierId = courier?.Id ?? 0,
                Latitude = lat,
                Longitude = lng,
                RecordedAt = recordedAt
            });
        }

        return Ok(list);
    }

    /// <summary>
    /// Ana kurye: Alt kullanıcı bilgilerini günceller (ad, soyad, e-posta, telefon).
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPut("sub-users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubUser(int userId, [FromBody] UpdateSubUserRequest model)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        if (currentUser.ParentCourierId.HasValue)
            return StatusCode(403, new { message = "Sadece ana kurye alt kurye düzenleyebilir." });

        if (string.IsNullOrWhiteSpace(model?.FirstName) || string.IsNullOrWhiteSpace(model?.LastName))
            return BadRequest(new { message = "Ad ve soyad zorunludur." });
        if (string.IsNullOrWhiteSpace(model?.Email))
            return BadRequest(new { message = "E-posta zorunludur." });
        if (string.IsNullOrWhiteSpace(model?.PhoneNumber))
            return BadRequest(new { message = "Telefon numarası zorunludur." });

        var subUser = await _userManager.FindByIdAsync(userId.ToString());
        if (subUser == null)
            return NotFound(new { message = "Alt kullanıcı bulunamadı." });

        if (subUser.ParentCourierId != applicationUserId.Value)
            return StatusCode(403, new { message = "Bu kullanıcı sizin alt kuryeniz değil." });

        var newEmail = model.Email!.Trim();
        if (!string.Equals(subUser.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await _userManager.FindByEmailAsync(newEmail);
            if (existingByEmail != null)
                return BadRequest(new { message = "Bu e-posta adresi başka bir hesapta kayıtlı." });
            subUser.Email = newEmail;
            subUser.UserName = newEmail;
        }

        subUser.FirstName = model.FirstName!.Trim();
        subUser.LastName = model.LastName!.Trim();
        subUser.PhoneNumber = model.PhoneNumber!.Trim();

        var updateResult = await _userManager.UpdateAsync(subUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Güncelleme başarısız: {errors}" });
        }

        _logger.LogInformation("[Courier] Alt kurye güncellendi. ParentUserId={ParentId}, SubUserId={SubId}",
            applicationUserId.Value, subUser.Id);

        return Ok(new { message = "Alt kurye bilgileri güncellendi." });
    }

    /// <summary>
    /// Ana kurye: Alt kullanıcının parolasını değiştirir. Mevcut parola doğrulanır.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("sub-users/{userId}/change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeSubUserPassword(int userId, [FromBody] ChangeSubUserPasswordRequest model)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        if (currentUser.ParentCourierId.HasValue)
            return StatusCode(403, new { message = "Sadece ana kurye alt kurye parolasını değiştirebilir." });

        if (string.IsNullOrWhiteSpace(model?.CurrentPassword))
            return BadRequest(new { message = "Mevcut parola girilmelidir." });

        if (string.IsNullOrWhiteSpace(model?.NewPassword) || model.NewPassword.Length < 6)
            return BadRequest(new { message = "Yeni parola en az 6 karakter olmalıdır." });

        var subUser = await _userManager.FindByIdAsync(userId.ToString());
        if (subUser == null)
            return NotFound(new { message = "Alt kullanıcı bulunamadı." });

        if (subUser.ParentCourierId != applicationUserId.Value)
            return StatusCode(403, new { message = "Bu kullanıcı sizin alt kuryeniz değil." });

        var passwordValid = await _userManager.CheckPasswordAsync(subUser, model.CurrentPassword!);
        if (!passwordValid)
            return BadRequest(new { message = "Mevcut parola hatalı." });

        var removeResult = await _userManager.RemovePasswordAsync(subUser);
        if (!removeResult.Succeeded)
        {
            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Parola kaldırılamadı: {errors}" });
        }

        var addResult = await _userManager.AddPasswordAsync(subUser, model.NewPassword!);
        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Yeni parola atanamadı: {errors}" });
        }

        _logger.LogInformation("[Courier] Alt kurye parolası değiştirildi. ParentUserId={ParentId}, SubUserId={SubId}",
            applicationUserId.Value, subUser.Id);

        return Ok(new { message = "Parola başarıyla güncellendi." });
    }

    /// <summary>
    /// Ana kurye: Alt kuryeyi siler. Sadece kuryenin hakedişi (teslim ettiği sipariş) yoksa silinebilir.
    /// </summary>
    [Authorize(Roles = "Courier")]
    [HttpPost("sub-users/{userId}/delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubUser(int userId)
    {
        var applicationUserId = GetApplicationUserId();
        if (!applicationUserId.HasValue)
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

        var currentUser = await _userManager.FindByIdAsync(applicationUserId.Value.ToString());
        if (currentUser == null)
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });

        if (currentUser.ParentCourierId.HasValue)
            return StatusCode(403, new { message = "Sadece ana kurye alt kurye silebilir." });

        var subUser = await _userManager.FindByIdAsync(userId.ToString());
        if (subUser == null)
            return NotFound(new { message = "Alt kullanıcı bulunamadı." });

        if (subUser.ParentCourierId != applicationUserId.Value)
            return StatusCode(403, new { message = "Bu kullanıcı sizin alt kuryeniz değil." });

        var courier = await _db.Couriers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationUserId == subUser.Id);
        if (courier != null)
        {
            var deliveredCount = await _db.Orders
                .AsNoTracking()
                .CountAsync(o => o.CourierId == courier.Id && o.CourierDeliveryStatus == CourierDeliveryStatus.Delivered);
            if (deliveredCount > 0)
                return BadRequest(new { message = "Bu kuryenin hakedişi (teslim ettiği siparişler) var, silinemez." });

            var ordersToUnassign = await _db.Orders
                .Where(o => o.CourierId == courier.Id)
                .ToListAsync();
            foreach (var order in ordersToUnassign)
            {
                order.CourierId = null;
                order.CourierDeliveryStatus = null;
            }
            await _db.SaveChangesAsync();

            var serviceAreas = await _db.CourierServiceAreas.Where(s => s.CourierId == courier.Id).ToListAsync();
            _db.CourierServiceAreas.RemoveRange(serviceAreas);
            var vehicles = await _db.CourierVehicles.Where(v => v.CourierId == courier.Id).ToListAsync();
            _db.CourierVehicles.RemoveRange(vehicles);
            var courierToDelete = await _db.Couriers.FindAsync(courier.Id);
            if (courierToDelete != null)
                _db.Couriers.Remove(courierToDelete);
            await _db.SaveChangesAsync();
        }

        subUser.ParentCourierId = null;
        var updateResult = await _userManager.UpdateAsync(subUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Alt kurye kaldırılamadı: {errors}" });
        }

        _logger.LogInformation("[Courier] Alt kurye silindi/kaldırıldı. ParentUserId={ParentId}, SubUserId={SubId}",
            applicationUserId.Value, subUser.Id);

        return Ok(new { message = "Alt kurye kaldırıldı." });
    }

    private static string GenerateTemporaryPassword(int length)
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var data = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(data);
        var result = new char[length];
        for (var i = 0; i < length; i++)
            result[i] = chars[data[i] % chars.Length];
        return new string(result);
    }
}

public class CourierOrderStatusRequest
{
    public CourierDeliveryStatus? Status { get; set; }
    /// <summary>Müşteri e-postasına gönderilen 4 haneli teslimat doğrulama kodu. Sadece status=Delivered için zorunlu.</summary>
    public string? DeliveryCode { get; set; }
}

public class CourierLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public int? OrderId { get; set; }
}

/// <summary>Ana kurye tarafından eklenecek alt kullanıcı bilgileri. İsteğe bağlı araç ve hizmet bölgesi. Parola gönderilmezse sunucu otomatik üretir ve e-postayla gönderir.</summary>
public class AddSubUserRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    /// <summary>Opsiyonel: Belirtilmezse veya 6 karakterden kısaysa otomatik parola üretilir ve e-postada gönderilir.</summary>
    public string? Password { get; set; }
    /// <summary>Opsiyonel: Araç tipi (0=Motosiklet, 1=Bisiklet, 2=Otomobil, 3=Kamyonet).</summary>
    public CourierVehicleType? VehicleType { get; set; }
    public string? LicensePlate { get; set; }
    /// <summary>Opsiyonel: Çalışma bölgesi il.</summary>
    public int? CityId { get; set; }
    /// <summary>Opsiyonel: Çalışma bölgesi ilçe.</summary>
    public int? TownId { get; set; }
}

/// <summary>Ana kurye: Alt kullanıcı güncelleme isteği (ad, soyad, e-posta, telefon).</summary>
public class UpdateSubUserRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
}

/// <summary>Ana kurye: Alt kullanıcı parolası değiştirme isteği. Mevcut parola doğrulanır.</summary>
public class ChangeSubUserPasswordRequest
{
    /// <summary>Alt kuryenin mevcut parolası (doğrulama için zorunlu).</summary>
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
