using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EP.Models;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services;
using ecommerce.Web.Domain.Services.Abstract;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// B2C Kullanıcı Yönetimi (Adres işlemleri)
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly ILogger<UserController> _logger;
        private readonly IVinService _vinService;
        private readonly IManufacturerElasticService _manufacturerService;
        private readonly IMemoryCache _cache;
        private readonly IDotIntegrationService _dotService;

        public UserController(
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            ILogger<UserController> logger,
            IVinService vinService,
            IManufacturerElasticService manufacturerService,
            IMemoryCache cache,
            IDotIntegrationService dotService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _vinService = vinService;
            _manufacturerService = manufacturerService;
            _cache = cache;
            _dotService = dotService;
        }

        /// <summary>
        /// Kullanıcının tüm adreslerini getirir
        /// </summary>
        [HttpGet("addresses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAddresses()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });
            }

            var addresses = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: a => a.ApplicationUserId == userId && a.Status == (int)EntityStatus.Active)
                .Include(a => a.City)
                .Include(a => a.Town)
                .Include(a => a.InvoiceCity)
                .Include(a => a.InvoiceTown)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedDate)
                .Select(a => new
                {
                    a.Id,
                    a.AddressName,
                    a.FullName,
                    a.Email,
                    a.PhoneNumber,
                    a.Address,
                    CityId = a.CityId,
                    CityName = a.City != null ? a.City.Name : null,
                    TownId = a.TownId,
                    TownName = a.Town != null ? a.Town.Name : null,
                    a.IdentityNumber,
                    a.IsDefault,
                    a.IsSameAsDeliveryAddress,
                    InvoiceCityId = a.InvoiceCityId,
                    InvoiceCityName = a.InvoiceCity != null ? a.InvoiceCity.Name : null,
                    InvoiceTownId = a.InvoiceTownId,
                    InvoiceTownName = a.InvoiceTown != null ? a.InvoiceTown.Name : null,
                    a.InvoiceAddress
                })
                .ToListAsync();

            return Ok(addresses);
        }

        /// <summary>
        /// Yeni adres ekler
        /// </summary>
        [HttpPost("addresses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddAddress([FromBody] UserAddressDto model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });
            }

            // Validasyon
            if (string.IsNullOrWhiteSpace(model.AddressName))
            {
                return BadRequest(new { message = "Adres adı zorunludur." });
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return BadRequest(new { message = "Ad soyad zorunludur." });
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return BadRequest(new { message = "Telefon numarası zorunludur." });
            }

            if (string.IsNullOrWhiteSpace(model.Address))
            {
                return BadRequest(new { message = "Adres detayı zorunludur." });
            }

            if (!model.CityId.HasValue || !model.TownId.HasValue)
            {
                return BadRequest(new { message = "Şehir ve ilçe seçimi zorunludur." });
            }

            // Eğer varsayılan adres olarak işaretlenmişse, diğer adreslerin varsayılan işaretini kaldır
            if (model.IsDefault)
            {
                var existingAddresses = await _unitOfWork.GetRepository<UserAddress>()
                    .GetAll(predicate: a => a.ApplicationUserId == userId && a.IsDefault, disableTracking: false)
                    .ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            var newAddress = new UserAddress
            {
                ApplicationUserId = userId,
                AddressName = model.AddressName,
                FullName = model.FullName,
                Email = model.Email ?? "",
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                CityId = model.CityId,
                TownId = model.TownId,
                IdentityNumber = model.IdentityNumber,
                IsDefault = model.IsDefault,
                IsSameAsDeliveryAddress = model.IsSameAsDeliveryAddress,
                InvoiceCityId = model.InvoiceCityId,
                InvoiceTownId = model.InvoiceTownId,
                InvoiceAddress = model.InvoiceAddress,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.Now,
                CreatedId = userId
            };

            _unitOfWork.GetRepository<UserAddress>().Insert(newAddress);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Adres başarıyla eklendi.", addressId = newAddress.Id });
        }

        /// <summary>
        /// Adresi günceller
        /// </summary>
        [HttpPut("addresses/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] UserAddressDto model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });
            }

            var address = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: a => a.Id == id && a.ApplicationUserId == userId, disableTracking: false)
                .FirstOrDefaultAsync();

            if (address == null)
            {
                return NotFound(new { message = "Adres bulunamadı." });
            }

            // Validasyon (AddAddress ile aynı alanlar)
            if (string.IsNullOrWhiteSpace(model.AddressName))
            {
                return BadRequest(new { message = "Adres adı zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return BadRequest(new { message = "Ad soyad zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return BadRequest(new { message = "Telefon numarası zorunludur." });
            }
            if (string.IsNullOrWhiteSpace(model.Address))
            {
                return BadRequest(new { message = "Adres detayı zorunludur." });
            }
            if (!model.CityId.HasValue || !model.TownId.HasValue)
            {
                return BadRequest(new { message = "Şehir ve ilçe seçimi zorunludur." });
            }

            // Eğer varsayılan adres olarak işaretlenmişse, diğer adreslerin varsayılan işaretini kaldır
            if (model.IsDefault && !address.IsDefault)
            {
                var existingAddresses = await _unitOfWork.GetRepository<UserAddress>()
                    .GetAll(predicate: a => a.ApplicationUserId == userId && a.IsDefault && a.Id != id, disableTracking: false)
                    .ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            address.AddressName = model.AddressName;
            address.FullName = model.FullName;
            address.Email = model.Email ?? address.Email;
            address.PhoneNumber = model.PhoneNumber;
            address.Address = model.Address;
            address.CityId = model.CityId;
            address.TownId = model.TownId;
            address.IdentityNumber = model.IdentityNumber;
            address.IsDefault = model.IsDefault;
            address.IsSameAsDeliveryAddress = model.IsSameAsDeliveryAddress;
            address.InvoiceCityId = model.InvoiceCityId;
            address.InvoiceTownId = model.InvoiceTownId;
            address.InvoiceAddress = model.InvoiceAddress;
            address.ModifiedDate = DateTime.Now;
            address.ModifiedId = userId;

            _unitOfWork.GetRepository<UserAddress>().Update(address);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Adres başarıyla güncellendi." });
        }

        /// <summary>
        /// Adresi siler (soft delete)
        /// </summary>
        [HttpPost("addresses/{id}/delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });
            }

            var address = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: a => a.Id == id && a.ApplicationUserId == userId, disableTracking: false)
                .FirstOrDefaultAsync();

            if (address == null)
            {
                return NotFound(new { message = "Adres bulunamadı." });
            }

            address.Status = (int)EntityStatus.Deleted;
            address.DeletedDate = DateTime.Now;
            address.DeletedId = userId;

            _unitOfWork.GetRepository<UserAddress>().Update(address);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Adres başarıyla silindi." });
        }

        /// <summary>
        /// Varsayılan adresi ayarlar
        /// </summary>
        [HttpPut("addresses/{id}/set-default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });
            }

            var address = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: a => a.Id == id && a.ApplicationUserId == userId, disableTracking: false)
                .FirstOrDefaultAsync();

            if (address == null)
            {
                return NotFound(new { message = "Adres bulunamadı." });
            }

            // Diğer adreslerin varsayılan işaretini kaldır
            var existingAddresses = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: a => a.ApplicationUserId == userId && a.IsDefault && a.Id != id, disableTracking: false)
                .ToListAsync();

            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }

            address.IsDefault = true;
            address.ModifiedDate = DateTime.Now;
            address.ModifiedId = userId;

            _unitOfWork.GetRepository<UserAddress>().Update(address);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Varsayılan adres ayarlandı." });
        }

        /// <summary>
        /// Tüm illeri getirir
        /// </summary>
        [HttpGet("cities")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _unitOfWork.GetRepository<City>()
                .GetAll(disableTracking: true)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Ok(cities);
        }

        /// <summary>
        /// İle göre ilçeleri getirir
        /// </summary>
        [HttpGet("towns/{cityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTowns(int cityId)
        {
            var towns = await _unitOfWork.GetRepository<Town>()
                .GetAll(predicate: t => t.CityId == cityId)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.CityId, t.Name })
                .ToListAsync();

            return Ok(towns);
        }

        // ==================== GARAJ ENDPOINT'LERİ ====================

        /// <summary>
        /// Kullanıcının kayıtlı araçlarını getirir
        /// </summary>
        [HttpGet("cars")]
        public async Task<IActionResult> GetUserCars()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });

            var cars = await _unitOfWork.GetRepository<UserCars>()
                .GetAll(predicate: c => c.ApplicationUserId == userId && c.Status == (int)EntityStatus.Active)
                .Include(c => c.DotManufacturer)
                .Include(c => c.DotBaseModel)
                .Include(c => c.DotSubModel)
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            // Benzersiz marka ID'lerini topla — N+1 önleme: her araç için ayrı Elasticsearch çağrısı yerine tek seferde
            var distinctManufacturerIds = cars
                .Where(c => c.DotManufacturerId.HasValue)
                .Select(c => c.DotManufacturerId!.Value)
                .Distinct()
                .ToList();
            var manufacturerCache = new Dictionary<int, ManufacturerElasticDto?>();
            foreach (var mid in distinctManufacturerIds)
            {
                var mfResult = await _manufacturerService.GetByIdAsync(mid);
                manufacturerCache[mid] = mfResult.Ok ? mfResult.Result : null;
            }

            var response = new List<UserCarResponse>();
            foreach (var car in cars)
            {
                string? logoUrl = null;
                string? vehicleImageUrl = null;

                if (car.DotManufacturerId.HasValue && manufacturerCache.TryGetValue(car.DotManufacturerId.Value, out var mf) && mf != null)
                {
                    logoUrl = mf.LogoUrl;
                    var modelKey = car.DotBaseModel?.DatKey ?? car.DotBaseModelKey;
                    if (!string.IsNullOrWhiteSpace(modelKey))
                    {
                        var model = mf.Models?.FirstOrDefault(m => m.BaseModelKey == modelKey);
                        vehicleImageUrl = model?.ImageUrl;
                    }
                }

                response.Add(new UserCarResponse
                {
                    Id = car.Id,
                    ManufacturerName = car.DotManufacturer?.Name,
                    BaseModelName = car.DotBaseModel?.Name,
                    SubModelName = car.DotSubModel?.Name,
                    ManufacturerLogoUrl = logoUrl,
                    VehicleImageUrl = vehicleImageUrl,
                    PlateNumber = car.PlateNumber,
                    CreatedDate = car.CreatedDate,
                    DotVehicleTypeId = car.DotVehicleTypeId,
                    DotManufacturerId = car.DotManufacturerId,
                    DotBaseModelId = car.DotBaseModelId,
                    DotSubModelId = car.DotSubModelId,
                    DotCarBodyOptionId = car.DotCarBodyOptionId,
                    DotEngineOptionId = car.DotEngineOptionId,
                    DotOptionId = car.DotOptionId,
                    DotManufacturerKey = car.DotManufacturerKey,
                    DotBaseModelKey = car.DotBaseModelKey,
                    DotSubModelKey = car.DotSubModelKey
                });
            }

            return Ok(response);
        }

        /// <summary>
        /// Yeni araç ekler
        /// </summary>
        [HttpPost("cars")]
        public async Task<IActionResult> AddUserCar([FromBody] AddUserCarRequest request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });

            // En az marka bilgisi zorunlu
            if (request.DotManufacturerId == null && string.IsNullOrWhiteSpace(request.DotManufacturerKey))
                return BadRequest(new { message = "Marka bilgisi zorunludur." });

            // Key'den ID çözümlemesi — VIN/SASED aramasından gelen key'ler ile FK ilişkisi kur
            var manufacturerId = request.DotManufacturerId;
            var baseModelId = request.DotBaseModelId;
            var subModelId = request.DotSubModelId;
            DotManufacturer? resolvedManufacturer = null;
            DotBaseModel? resolvedBaseModel = null;

            if (manufacturerId == null && !string.IsNullOrWhiteSpace(request.DotManufacturerKey))
            {
                resolvedManufacturer = await _unitOfWork.GetRepository<DotManufacturer>()
                    .GetAll(predicate: m => m.DatKey == request.DotManufacturerKey && m.IsActive)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                manufacturerId = resolvedManufacturer?.Id;
            }

            if (baseModelId == null && !string.IsNullOrWhiteSpace(request.DotBaseModelKey) && !string.IsNullOrWhiteSpace(request.DotManufacturerKey))
            {
                resolvedBaseModel = await _unitOfWork.GetRepository<DotBaseModel>()
                    .GetAll(predicate: m => m.DatKey == request.DotBaseModelKey && m.ManufacturerKey == request.DotManufacturerKey && m.IsActive)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                baseModelId = resolvedBaseModel?.Id;
            }

            if (subModelId == null && !string.IsNullOrWhiteSpace(request.DotSubModelKey))
            {
                var subModel = await _unitOfWork.GetRepository<DotSubModel>()
                    .GetAll(predicate: m => m.DatKey == request.DotSubModelKey)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                subModelId = subModel?.Id;
            }

            // SASED/VIN'den sadece key ile eklenen araçlarda araç tipi boş kalmasın — düzenlemede özellikler uyumlu olsun
            var resolvedVehicleTypeId = request.DotVehicleTypeId
                ?? resolvedBaseModel?.VehicleType
                ?? resolvedManufacturer?.VehicleType;

            var car = new UserCars
            {
                ApplicationUserId = userId,
                DotVehicleTypeId = resolvedVehicleTypeId,
                DotManufacturerId = manufacturerId,
                DotBaseModelId = baseModelId,
                DotSubModelId = subModelId,
                DotCarBodyOptionId = request.DotCarBodyOptionId,
                DotEngineOptionId = request.DotEngineOptionId,
                DotOptionId = request.DotOptionId,
                DotManufacturerKey = request.DotManufacturerKey,
                DotBaseModelKey = request.DotBaseModelKey,
                DotSubModelKey = request.DotSubModelKey,
                DotDatECode = request.DotDatECode,
                PlateNumber = request.PlateNumber,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedId = userId
            };

            _unitOfWork.GetRepository<UserCars>().Insert(car);
            await _unitOfWork.SaveChangesAsync();

            // Elasticsearch'ten logo ve araç görseli çek
            string? addLogoUrl = null;
            string? addVehicleImageUrl = null;
            if (car.DotManufacturerId.HasValue)
            {
                var mfResult = await _manufacturerService.GetByIdAsync(car.DotManufacturerId.Value);
                if (mfResult.Ok && mfResult.Result != null)
                {
                    addLogoUrl = mfResult.Result.LogoUrl;
                    if (!string.IsNullOrWhiteSpace(car.DotBaseModelKey))
                    {
                        var baseModel = mfResult.Result.Models.FirstOrDefault(m => m.BaseModelKey == car.DotBaseModelKey);
                        addVehicleImageUrl = baseModel?.ImageUrl;
                    }
                }
            }

            // İlişkili isimleri yükle
            var saved = await _unitOfWork.GetRepository<UserCars>()
                .GetAll(predicate: c => c.Id == car.Id)
                .Include(c => c.DotManufacturer)
                .Include(c => c.DotBaseModel)
                .Include(c => c.DotSubModel)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return Ok(new UserCarResponse
            {
                Id = car.Id,
                ManufacturerName = saved?.DotManufacturer?.Name,
                BaseModelName = saved?.DotBaseModel?.Name,
                SubModelName = saved?.DotSubModel?.Name,
                ManufacturerLogoUrl = addLogoUrl,
                VehicleImageUrl = addVehicleImageUrl,
                PlateNumber = car.PlateNumber,
                CreatedDate = car.CreatedDate,
                DotVehicleTypeId = car.DotVehicleTypeId,
                DotManufacturerId = car.DotManufacturerId,
                DotBaseModelId = car.DotBaseModelId,
                DotSubModelId = car.DotSubModelId,
                DotManufacturerKey = car.DotManufacturerKey,
                DotBaseModelKey = car.DotBaseModelKey,
                DotSubModelKey = car.DotSubModelKey
            });
        }

        /// <summary>
        /// Araç siler (soft delete)
        /// </summary>
        [HttpPost("cars/{id}/delete")]
        public async Task<IActionResult> DeleteUserCar(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });

            var car = await _unitOfWork.GetRepository<UserCars>()
                .GetAll(predicate: c => c.Id == id && c.Status == (int)EntityStatus.Active, disableTracking: false)
                .FirstOrDefaultAsync();

            if (car == null)
                return NotFound(new { message = "Araç bulunamadı." });

            // Yetkilendirme: sadece kendi aracını silebilir
            if (car.ApplicationUserId != userId)
                return StatusCode(403, new { message = "Bu araca erişim yetkiniz yok." });

            car.Status = (int)EntityStatus.Deleted;
            car.ModifiedDate = DateTime.UtcNow;
            car.ModifiedId = userId;

            _unitOfWork.GetRepository<UserCars>().Update(car);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Araç başarıyla silindi." });
        }

        /// <summary>
        /// Araç bilgilerini günceller
        /// </summary>
        [HttpPut("cars/{id}")]
        public async Task<IActionResult> UpdateUserCar(int id, [FromBody] AddUserCarRequest request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Kullanıcı kimliği belirlenemedi." });

            var car = await _unitOfWork.GetRepository<UserCars>()
                .GetAll(predicate: c => c.Id == id && c.Status == (int)EntityStatus.Active, disableTracking: false)
                .FirstOrDefaultAsync();

            if (car == null)
                return NotFound(new { message = "Araç bulunamadı." });

            if (car.ApplicationUserId != userId)
                return StatusCode(403, new { message = "Bu araca erişim yetkiniz yok." });

            // Key'den ID çözümlemesi — VIN aramasından gelen key'ler ile FK ilişkisi kur
            var manufacturerId = request.DotManufacturerId;
            var baseModelId = request.DotBaseModelId;
            var subModelId = request.DotSubModelId;

            if (manufacturerId == null && !string.IsNullOrWhiteSpace(request.DotManufacturerKey))
            {
                var manufacturer = await _unitOfWork.GetRepository<DotManufacturer>()
                    .GetAll(predicate: m => m.DatKey == request.DotManufacturerKey && m.IsActive)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                manufacturerId = manufacturer?.Id;
            }

            if (baseModelId == null && !string.IsNullOrWhiteSpace(request.DotBaseModelKey) && !string.IsNullOrWhiteSpace(request.DotManufacturerKey))
            {
                var baseModel = await _unitOfWork.GetRepository<DotBaseModel>()
                    .GetAll(predicate: m => m.DatKey == request.DotBaseModelKey && m.ManufacturerKey == request.DotManufacturerKey && m.IsActive)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                baseModelId = baseModel?.Id;
            }

            if (subModelId == null && !string.IsNullOrWhiteSpace(request.DotSubModelKey))
            {
                var subModel = await _unitOfWork.GetRepository<DotSubModel>()
                    .GetAll(predicate: m => m.DatKey == request.DotSubModelKey && m.IsActive)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                subModelId = subModel?.Id;
            }

            car.DotVehicleTypeId = request.DotVehicleTypeId;
            car.DotManufacturerId = manufacturerId;
            car.DotBaseModelId = baseModelId;
            car.DotSubModelId = subModelId;
            car.DotCarBodyOptionId = request.DotCarBodyOptionId;
            car.DotEngineOptionId = request.DotEngineOptionId;
            car.DotOptionId = request.DotOptionId;
            car.DotManufacturerKey = request.DotManufacturerKey;
            car.DotBaseModelKey = request.DotBaseModelKey;
            car.DotSubModelKey = request.DotSubModelKey;
            car.DotDatECode = request.DotDatECode;
            car.PlateNumber = request.PlateNumber;
            car.ModifiedDate = DateTime.UtcNow;
            car.ModifiedId = userId;

            _unitOfWork.GetRepository<UserCars>().Update(car);
            await _unitOfWork.SaveChangesAsync();

            // Elasticsearch'ten logo ve araç görseli çek
            string? updateLogoUrl = null;
            string? updateVehicleImageUrl = null;
            if (car.DotManufacturerId.HasValue)
            {
                var mfResult = await _manufacturerService.GetByIdAsync(car.DotManufacturerId.Value);
                if (mfResult.Ok && mfResult.Result != null)
                {
                    updateLogoUrl = mfResult.Result.LogoUrl;
                    if (!string.IsNullOrWhiteSpace(car.DotBaseModelKey))
                    {
                        var baseModel = mfResult.Result.Models.FirstOrDefault(m => m.BaseModelKey == car.DotBaseModelKey);
                        updateVehicleImageUrl = baseModel?.ImageUrl;
                    }
                }
            }

            // İlişkili isimleri yükle
            var saved = await _unitOfWork.GetRepository<UserCars>()
                .GetAll(predicate: c => c.Id == car.Id)
                .Include(c => c.DotManufacturer)
                .Include(c => c.DotBaseModel)
                .Include(c => c.DotSubModel)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return Ok(new UserCarResponse
            {
                Id = car.Id,
                ManufacturerName = saved?.DotManufacturer?.Name,
                BaseModelName = saved?.DotBaseModel?.Name,
                SubModelName = saved?.DotSubModel?.Name,
                ManufacturerLogoUrl = updateLogoUrl,
                VehicleImageUrl = updateVehicleImageUrl,
                PlateNumber = car.PlateNumber,
                CreatedDate = car.CreatedDate,
                DotVehicleTypeId = car.DotVehicleTypeId,
                DotManufacturerId = car.DotManufacturerId,
                DotBaseModelId = car.DotBaseModelId,
                DotSubModelId = car.DotSubModelId,
                DotManufacturerKey = car.DotManufacturerKey,
                DotBaseModelKey = car.DotBaseModelKey,
                DotSubModelKey = car.DotSubModelKey
            });
        }

        /// <summary>
        /// VIN (şase) numarası ile araç arar
        /// </summary>
        [HttpGet("cars/search-vin/{vinNumber}")]
        public async Task<IActionResult> SearchByVin(string vinNumber)
        {
            if (string.IsNullOrWhiteSpace(vinNumber) || vinNumber.Length != 17)
                return BadRequest(new { message = "VIN numarası 17 karakter olmalıdır." });

            try
            {
                var result = await _vinService.DecodeVinAsync(vinNumber);
                if (!result.Ok || result.Result == null)
                    return Ok(new { isSuccess = false, errorMessage = "VIN decode işlemi başarısız." });

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VIN arama hatası. VIN: {VinNumber}", vinNumber);
                return Ok(new { isSuccess = false, errorMessage = "VIN arama sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Araç markalarını getirir
        /// </summary>
        [HttpGet("cars/manufacturers")]
        public async Task<IActionResult> GetManufacturers([FromQuery] int? vehicleType)
        {
            try
            {
                // 10 dakika MemoryCache — Elasticsearch'e her seferinde sorgu atmayı önler
                var cacheKey = $"garage_manufacturers_{vehicleType ?? 0}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _manufacturerService.GetAllAsync(vehicleType, 500);
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                // Sadece özet bilgi döndür (modeller hariç)
                var manufacturers = result.Result.Select(m => new
                {
                    m.Id,
                    m.DatKey,
                    m.Name,
                    m.VehicleType,
                    m.LogoUrl,
                    m.ModelCount
                }).ToList();

                _cache.Set(cacheKey, manufacturers, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return Ok(manufacturers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marka listesi getirme hatası");
                return StatusCode(500, new { message = "Marka listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Seçilen markaya ait modelleri getirir
        /// </summary>
        [HttpGet("cars/manufacturers/{manufacturerId:int}/models")]
        public async Task<IActionResult> GetModels(int manufacturerId, [FromQuery] int? vehicleType)
        {
            try
            {
                var cacheKey = $"garage_models_{manufacturerId}_{vehicleType ?? 0}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _manufacturerService.GetModelsByManufacturerAsync(manufacturerId, vehicleType);
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                _cache.Set(cacheKey, result.Result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model listesi getirme hatası. ManufacturerId: {ManufacturerId}", manufacturerId);
                return StatusCode(500, new { message = "Model listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Seçilen modele ait alt modelleri getirir (DB'den)
        /// </summary>
        [HttpGet("cars/submodels")]
        public async Task<IActionResult> GetSubModels([FromQuery] string manufacturerKey, [FromQuery] string baseModelKey)
        {
            if (string.IsNullOrWhiteSpace(manufacturerKey) || string.IsNullOrWhiteSpace(baseModelKey))
                return BadRequest(new { message = "Marka ve model key zorunludur." });

            try
            {
                var cacheKey = $"garage_submodels_{manufacturerKey}_{baseModelKey}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var subModels = await _unitOfWork.GetRepository<DotSubModel>()
                    .GetAll(predicate: s => s.ManufacturerKey == manufacturerKey && s.BaseModelKey == baseModelKey && s.IsActive)
                    .AsNoTracking()
                    .Select(s => new { s.Id, s.Name, s.DatKey })
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, subModels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return Ok(subModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alt model listesi getirme hatası");
                return StatusCode(500, new { message = "Alt model listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Araç tiplerini getirir (Binek, Ticari vb.)
        /// </summary>
        [HttpGet("cars/vehicle-types")]
        public async Task<IActionResult> GetVehicleTypes()
        {
            try
            {
                var cacheKey = "garage_vehicle_types";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _dotService.GetVehicleTypesAsync();
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                var types = result.Result.Select(t => new { t.Id, t.Name }).ToList();

                _cache.Set(cacheKey, types, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });

                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç tipi listesi getirme hatası");
                return StatusCode(500, new { message = "Araç tipi listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Seçilen alt modele ait kasa tipi seçeneklerini getirir
        /// </summary>
        [HttpGet("cars/car-body-options")]
        public async Task<IActionResult> GetCarBodyOptions(
            [FromQuery] string manufacturerKey,
            [FromQuery] string baseModelKey,
            [FromQuery] string subModelKey,
            [FromQuery] int vehicleType)
        {
            if (string.IsNullOrWhiteSpace(manufacturerKey) || string.IsNullOrWhiteSpace(baseModelKey) || string.IsNullOrWhiteSpace(subModelKey))
                return BadRequest(new { message = "Marka, model ve alt model key zorunludur." });

            try
            {
                var cacheKey = $"garage_carbody_{manufacturerKey}_{baseModelKey}_{subModelKey}_{vehicleType}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _dotService.GetCarBodyOptionsBySubModelAsync(manufacturerKey, baseModelKey, subModelKey, vehicleType);
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                var options = result.Result.Select(o => new { o.Id, o.Name, o.DatKey }).ToList();

                _cache.Set(cacheKey, options, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa tipi listesi getirme hatası");
                return StatusCode(500, new { message = "Kasa tipi listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Seçilen alt modele ait motor seçeneklerini getirir
        /// </summary>
        [HttpGet("cars/engine-options")]
        public async Task<IActionResult> GetEngineOptions(
            [FromQuery] string manufacturerKey,
            [FromQuery] string baseModelKey,
            [FromQuery] string subModelKey,
            [FromQuery] int vehicleType)
        {
            if (string.IsNullOrWhiteSpace(manufacturerKey) || string.IsNullOrWhiteSpace(baseModelKey) || string.IsNullOrWhiteSpace(subModelKey))
                return BadRequest(new { message = "Marka, model ve alt model key zorunludur." });

            try
            {
                var cacheKey = $"garage_engine_{manufacturerKey}_{baseModelKey}_{subModelKey}_{vehicleType}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _dotService.GetEngineOptionsBySubModelAsync(manufacturerKey, baseModelKey, subModelKey, vehicleType);
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                var options = result.Result.Select(o => new { o.Id, o.Name, o.DatKey }).ToList();

                _cache.Set(cacheKey, options, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Motor listesi getirme hatası");
                return StatusCode(500, new { message = "Motor listesi alınırken hata oluştu." });
            }
        }

        /// <summary>
        /// Seçilen alt modele ait ek özellikleri getirir
        /// </summary>
        [HttpGet("cars/additional-options")]
        public async Task<IActionResult> GetAdditionalOptions(
            [FromQuery] string manufacturerKey,
            [FromQuery] string baseModelKey,
            [FromQuery] string subModelKey,
            [FromQuery] int vehicleType)
        {
            if (string.IsNullOrWhiteSpace(manufacturerKey) || string.IsNullOrWhiteSpace(baseModelKey) || string.IsNullOrWhiteSpace(subModelKey))
                return BadRequest(new { message = "Marka, model ve alt model key zorunludur." });

            try
            {
                var cacheKey = $"garage_options_{manufacturerKey}_{baseModelKey}_{subModelKey}_{vehicleType}";
                if (_cache.TryGetValue(cacheKey, out object? cached))
                    return Ok(cached);

                var result = await _dotService.GetOptionsBySubModelAsync(manufacturerKey, baseModelKey, subModelKey, vehicleType);
                if (!result.Ok || result.Result == null)
                    return Ok(new List<object>());

                var options = result.Result.Select(o => new { o.Id, o.Name, o.DatKey, o.Classification }).ToList();

                _cache.Set(cacheKey, options, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ek özellik listesi getirme hatası");
                return StatusCode(500, new { message = "Ek özellik listesi alınırken hata oluştu." });
            }
        }
    }

    public class UserAddressDto
    {
        public string AddressName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public string? IdentityNumber { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSameAsDeliveryAddress { get; set; } = true;
        public int? InvoiceCityId { get; set; }
        public int? InvoiceTownId { get; set; }
        public string? InvoiceAddress { get; set; }
    }
}
